namespace Game.Core.Domain;

public readonly record struct Stats(
    int Hp,
    int Mp,
    int Atk,
    int Def,
    int Spd,
    double Acc,
    double Eva)
{
    public int Hp { get; init; } = Hp;
    public int Mp { get; init; } = Mp;
    public int Atk { get; init; } = Atk;
    public int Def { get; init; } = Def;
    public int Spd { get; init; } = Spd;
    public double Acc { get; init; } = Acc;
    public double Eva { get; init; } = Eva;
}