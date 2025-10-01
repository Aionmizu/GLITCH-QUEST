namespace Game.Core.Domain;

public sealed class Move
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Element Type { get; init; }
    public int Power { get; init; }
    public DamageKind Kind { get; init; }
    public double Accuracy { get; init; }
    public int MpCost { get; init; }
    public double CritChance { get; init; }
}