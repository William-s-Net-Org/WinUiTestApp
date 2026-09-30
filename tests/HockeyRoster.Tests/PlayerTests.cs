using HockeyRoster.Application;
using HockeyRoster.Domain;
using HockeyRoster.Infrastructure;
using HockeyRoster.Presentation;
using Xunit;

namespace HockeyRoster.Tests;

public sealed class PlayerTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "HockeyRosterTests", Guid.NewGuid().ToString("N"));
    private string DataPath => Path.Combine(_directory, "players.json");
    private static Player ValidPlayer() => new(Guid.NewGuid(), "  Anna Andersson  ", Position.Center, 21, 82.5m, 185, StickHand.Left);

    [Theory]
    [InlineData("", 21, 80, 180)]
    [InlineData("Anna", 0, 80, 180)]
    [InlineData("Anna", 100, 80, 180)]
    [InlineData("Anna", 21, 0, 180)]
    [InlineData("Anna", 21, 80, 251)]
    public void Invalid_input_is_rejected(string name, int number, decimal weight, decimal height) =>
        Assert.Throws<DomainException>(() => new Player(Guid.NewGuid(), name, Position.Center, number, weight, height, StickHand.Left));

    [Fact]
    public void Invalid_enums_are_rejected()
    {
        Assert.Throws<DomainException>(() => new Player(Guid.NewGuid(), "Anna", (Position)99, 1, 80, 180, StickHand.Left));
        Assert.Throws<DomainException>(() => new Player(Guid.NewGuid(), "Anna", Position.Center, 1, 80, 180, (StickHand)99));
    }

    [Fact]
    public async Task Saves_and_reloads_all_fields()
    {
        var player = ValidPlayer();
        await new JsonPlayerRepository(DataPath).AddAsync(player);
        var saved = Assert.Single(await new JsonPlayerRepository(DataPath).GetAllAsync());
        Assert.Equal(player.Id, saved.Id);
        Assert.Equal("Anna Andersson", saved.Name);
        Assert.Equal(player.Position, saved.Position);
        Assert.Equal(player.Number, saved.Number);
        Assert.Equal(player.WeightKg, saved.WeightKg);
        Assert.Equal(player.HeightCm, saved.HeightCm);
        Assert.Equal(player.StickHand, saved.StickHand);
    }

    [Fact]
    public async Task Concurrent_repository_instances_do_not_lose_players()
    {
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => new JsonPlayerRepository(DataPath).AddAsync(ValidPlayer())));
        Assert.Equal(8, (await new JsonPlayerRepository(DataPath).GetAllAsync()).Count);
    }

    [Fact]
    public async Task Corrupt_file_is_not_overwritten()
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(DataPath, "broken json");
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => new JsonPlayerRepository(DataPath).AddAsync(ValidPlayer()));
        Assert.Equal("broken json", await File.ReadAllTextAsync(DataPath));
    }

    [Fact]
    public async Task View_model_accepts_swedish_decimals_and_clears_form_after_save()
    {
        var vm = new PlayersViewModel(new PlayerService(new JsonPlayerRepository(DataPath)));
        await vm.LoadCommand.ExecuteAsync(null);
        vm.Name = "Anna Andersson"; vm.Number = "21"; vm.Weight = "82,5"; vm.Height = "185";
        vm.SelectedPosition = vm.Positions[2]; vm.SelectedHand = vm.Hands[0];
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.False(vm.IsError);
        Assert.Single(vm.Players);
        Assert.Equal("", vm.Name);
        Assert.Equal(82.5m, Assert.Single(await new JsonPlayerRepository(DataPath).GetAllAsync()).WeightKg);
    }

    [Fact]
    public async Task Validation_failure_preserves_form_and_does_not_save()
    {
        var vm = new PlayersViewModel(new PlayerService(new JsonPlayerRepository(DataPath)));
        await vm.LoadCommand.ExecuteAsync(null);
        vm.Name = "Anna"; vm.Number = "100"; vm.Weight = "80"; vm.Height = "180";
        vm.SelectedPosition = vm.Positions[2]; vm.SelectedHand = vm.Hands[0];
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(vm.IsError); Assert.Equal("Anna", vm.Name); Assert.Empty(vm.Players);
        Assert.False(File.Exists(DataPath));
    }

    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
}
