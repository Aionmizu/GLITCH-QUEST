namespace Game.Core.Abstractions;

public interface IRandom
{
    // Returns a double in [0.0, 1.0)
    double NextDouble();
    // Returns a double in [min, max]
    double NextDouble(double minInclusive, double maxInclusive);
}

public sealed class DefaultRandom : IRandom
{
    private readonly Random _rng = new();
    public double NextDouble() => _rng.NextDouble();
    public double NextDouble(double minInclusive, double maxInclusive)
        => minInclusive + _rng.NextDouble() * (maxInclusive - minInclusive);
}