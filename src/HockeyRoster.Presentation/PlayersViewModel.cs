using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HockeyRoster.Application;
using HockeyRoster.Domain;

namespace HockeyRoster.Presentation;

public sealed record Choice<T>(T Value, string Label);
public sealed record PlayerRow(string Name, string Position, string Number, string Weight, string Height, string Hand);

public partial class PlayersViewModel : ObservableObject
{
    private readonly PlayerService _service;
    private static readonly CultureInfo Swedish = CultureInfo.GetCultureInfo("sv-SE");
    public ObservableCollection<PlayerRow> Players { get; } = [];
    public IReadOnlyList<Choice<Position>> Positions { get; } = [
        new(Position.Goalkeeper, "Målvakt"), new(Position.Defender, "Back"),
        new(Position.Center, "Center"), new(Position.LeftWing, "Vänsterforward"), new(Position.RightWing, "Högerforward")];
    public IReadOnlyList<Choice<StickHand>> Hands { get; } = [new(StickHand.Left, "Vänster"), new(StickHand.Right, "Höger")];

    [ObservableProperty] private string name = "";
    [ObservableProperty] private string number = "";
    [ObservableProperty] private string weight = "";
    [ObservableProperty] private string height = "";
    [ObservableProperty] private Choice<Position>? selectedPosition;
    [ObservableProperty] private Choice<StickHand>? selectedHand;
    [ObservableProperty] private string message = "";
    [ObservableProperty] private bool hasMessage;
    [ObservableProperty] private bool isError;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReady))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    private bool isBusy;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool isLoaded;

    public bool IsReady => !IsBusy;
    public string CountLabel => $"{Players.Count} spelare i truppen";
    public string EmptyLabel => Players.Count == 0 ? "Truppen är tom. Lägg till din första spelare i formuläret." : "";
    public PlayersViewModel(PlayerService service) => _service = service;
    private bool CanLoad() => !IsBusy;
    private bool CanSave() => !IsBusy && IsLoaded;

    [RelayCommand(CanExecute = nameof(CanLoad))]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var players = await _service.GetAllAsync();
            Players.Clear();
            foreach (var player in players.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)) Players.Add(ToRow(player));
            IsLoaded = true;
            HasMessage = false;
            RefreshCount();
        }
        catch (Exception ex) { IsLoaded = false; ShowError(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        IsBusy = true;
        try
        {
            if (SelectedPosition is null || SelectedHand is null)
                throw new DomainException("Välj position och klubbfattning.");
            if (!int.TryParse(Number, NumberStyles.None, Swedish, out var jersey))
                throw new DomainException("Ange tröjnummer som ett heltal mellan 1 och 99.");
            if (!TryMeasurement(Weight, out var kg) || !TryMeasurement(Height, out var cm))
                throw new DomainException("Ange vikt och längd som tal, till exempel 82,5 och 185.");
            var player = await _service.AddAsync(new(Name, SelectedPosition.Value, jersey, kg, cm, SelectedHand.Value));
            Players.Add(ToRow(player));
            RefreshCount();
            Name = Number = Weight = Height = "";
            SelectedPosition = null;
            SelectedHand = null;
            Message = $"{player.Name} har lagts till och sparats.";
            IsError = false;
            HasMessage = true;
        }
        catch (Exception ex) { ShowError(ex); }
        finally { IsBusy = false; }
    }

    private static bool TryMeasurement(string value, out decimal result) =>
        decimal.TryParse(value.Trim().Replace('.', ','), NumberStyles.AllowDecimalPoint, Swedish, out result);
    private void ShowError(Exception ex)
    {
        Message = ex is DomainException ? ex.Message :
            "Kunde inte läsa eller spara spelarfilen. Kontrollera filen och åtkomsten till lagringsmappen och försök igen. Dina inmatade uppgifter finns kvar.";
        IsError = true;
        HasMessage = true;
    }
    private PlayerRow ToRow(Player p) => new(p.Name, Positions.Single(x => x.Value == p.Position).Label,
        $"#{p.Number}", $"{p.WeightKg.ToString("0.#", Swedish)} kg", $"{p.HeightCm.ToString("0.#", Swedish)} cm",
        Hands.Single(x => x.Value == p.StickHand).Label);
    private void RefreshCount() { OnPropertyChanged(nameof(CountLabel)); OnPropertyChanged(nameof(EmptyLabel)); }
}
