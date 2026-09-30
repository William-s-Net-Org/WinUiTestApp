using System.Text.Json;
using HockeyRoster.Application;
using HockeyRoster.Domain;

namespace HockeyRoster.Infrastructure;

// Separate lock file serializes writes, including writes from another app instance.
// Failed reads never silently replace a damaged file with an empty roster.
public sealed class JsonPlayerRepository(string filePath) : IPlayerRepository
{
    private readonly string _path = Path.GetFullPath(filePath);
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public async Task<IReadOnlyList<Player>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var fileLock = await AcquireLockAsync(cancellationToken);
        return await ReadAsync(cancellationToken);
    }

    public async Task AddAsync(Player player, CancellationToken cancellationToken = default)
    {
        using var fileLock = await AcquireLockAsync(cancellationToken);
        var players = await ReadAsync(cancellationToken);
        players.Add(player);
        var temp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, players, Options, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temp, _path, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private async Task<List<Player>> ReadAsync(CancellationToken ct)
    {
        if (!File.Exists(_path)) return [];
        await using var stream = File.OpenRead(_path);
        var players = await JsonSerializer.DeserializeAsync<List<Player>>(stream, Options, ct)
            ?? throw new InvalidDataException("Spelarfilen innehåller ingen giltig lista.");
        if (players.Any(p => p is null) || players.Select(p => p.Id).Distinct().Count() != players.Count)
            throw new InvalidDataException("Spelarfilen innehåller ogiltiga eller dubbla id:n.");
        return players;
    }

    private async Task<FileStream> AcquireLockAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        for (var attempt = 0; ; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try { return new FileStream(_path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) when (attempt < 40) { await Task.Delay(50, ct); }
        }
    }
}
