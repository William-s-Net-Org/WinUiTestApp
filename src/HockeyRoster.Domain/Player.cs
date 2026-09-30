namespace HockeyRoster.Domain;

public enum Position { Goalkeeper, Defender, Center, LeftWing, RightWing }
public enum StickHand { Left, Right }

public sealed class DomainException(string message) : Exception(message);

// Aggregate root. All creation and rehydration passes through the same invariants.
public sealed class Player
{
    public Guid Id { get; }
    public string Name { get; }
    public Position Position { get; }
    public int Number { get; }
    public decimal WeightKg { get; }
    public decimal HeightCm { get; }
    public StickHand StickHand { get; }

    public Player(Guid id, string name, Position position, int number,
        decimal weightKg, decimal heightCm, StickHand stickHand)
    {
        if (id == Guid.Empty) throw new DomainException("Spelaren måste ha ett id.");
        name = name?.Trim() ?? "";
        if (name.Length is < 2 or > 100) throw new DomainException("Namn måste innehålla 2–100 tecken.");
        if (!Enum.IsDefined(position)) throw new DomainException("Välj en giltig position.");
        if (number is < 1 or > 99) throw new DomainException("Tröjnummer måste vara 1–99.");
        if (weightKg is < 15 or > 250) throw new DomainException("Vikt måste vara 15–250 kg.");
        if (heightCm is < 80 or > 250) throw new DomainException("Längd måste vara 80–250 cm.");
        if (!Enum.IsDefined(stickHand)) throw new DomainException("Välj en giltig klubbfattning.");
        (Id, Name, Position, Number, WeightKg, HeightCm, StickHand) =
            (id, name, position, number, weightKg, heightCm, stickHand);
    }
}
