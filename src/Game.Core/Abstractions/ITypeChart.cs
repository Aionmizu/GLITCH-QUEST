using Game.Core.Domain;

namespace Game.Core.Abstractions;

public interface ITypeChart
{
    double GetMultiplier(Element attackType, Element defendType);
}

public sealed class SimpleTypeChart : ITypeChart
{
    private readonly Dictionary<Element, Dictionary<Element, double>> _chart;

    public SimpleTypeChart(Dictionary<Element, Dictionary<Element, double>> chart)
    {
        _chart = chart;
    }

    public static SimpleTypeChart Default()
    {
        // Minimal chart according to spec (Normal neutral, Fire>Grass>Water>Fire, Electric strong vs Water)
        var d = new Dictionary<Element, Dictionary<Element, double>>
        {
            [Element.Fire] = new()
            {
                [Element.Grass] = 2.0,
                [Element.Water] = 0.5,
                [Element.Fire] = 0.5,
                [Element.Electric] = 1.0,
                [Element.Normal] = 1.0
            },
            [Element.Water] = new()
            {
                [Element.Fire] = 2.0,
                [Element.Grass] = 0.5,
                [Element.Water] = 0.5,
                [Element.Electric] = 0.5,
                [Element.Normal] = 1.0
            },
            [Element.Grass] = new()
            {
                [Element.Water] = 2.0,
                [Element.Fire] = 0.5,
                [Element.Grass] = 0.5,
                [Element.Electric] = 1.0,
                [Element.Normal] = 1.0
            },
            [Element.Electric] = new()
            {
                [Element.Water] = 2.0,
                [Element.Grass] = 0.5,
                [Element.Electric] = 0.5,
                [Element.Fire] = 1.0,
                [Element.Normal] = 1.0
            },
            [Element.Normal] = new()
            {
                [Element.Fire] = 1.0,
                [Element.Water] = 1.0,
                [Element.Grass] = 1.0,
                [Element.Electric] = 1.0,
                [Element.Normal] = 1.0
            }
        };
        return new SimpleTypeChart(d);
    }

    public double GetMultiplier(Element attackType, Element defendType)
    {
        if (_chart.TryGetValue(attackType, out var row) && row.TryGetValue(defendType, out var mul))
            return mul;
        return 1.0;
    }
}