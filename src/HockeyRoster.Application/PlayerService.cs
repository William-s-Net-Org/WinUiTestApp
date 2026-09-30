using HockeyRoster.Domain;

namespace HockeyRoster.Application;

public interface IPlayerRepository
{
    Task<IReadOnlyList<Player>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Player player, CancellationToken cancellationToken = default);
}

public sealed record AddPlayerRequest(string Name, Position Position, int Number,
    decimal WeightKg, decimal HeightCm, StickHand StickHand);

public sealed class PlayerService(IPlayerRepository repository)
{
    public Task<IReadOnlyList<Player>> GetAllAsync(CancellationToken ct = default) => repository.GetAllAsync(ct);

    public async Task<Player> AddAsync(AddPlayerRequest request, CancellationToken ct = default)
    {
        var player = new Player(Guid.NewGuid(), request.Name, request.Position,
            request.Number, request.WeightKg, request.HeightCm, request.StickHand);
        await repository.AddAsync(player, ct);
        return player;
    }
}
