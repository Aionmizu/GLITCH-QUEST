using FluentAssertions;
using Game.Core.Abstractions;
using Game.Core.Domain;
using Xunit;

namespace Game.Tests;

public class TypeChartTests
{
    [Fact]
    public void Default_chart_matches_design_relations()
    {
        var chart = SimpleTypeChart.Default();

        chart.GetMultiplier(Element.Fire, Element.Grass).Should().Be(2.0);
        chart.GetMultiplier(Element.Fire, Element.Water).Should().Be(0.5);
        chart.GetMultiplier(Element.Water, Element.Fire).Should().Be(2.0);
        chart.GetMultiplier(Element.Grass, Element.Water).Should().Be(2.0);
        chart.GetMultiplier(Element.Electric, Element.Water).Should().Be(2.0);

        chart.GetMultiplier(Element.Normal, Element.Fire).Should().Be(1.0);
        chart.GetMultiplier(Element.Normal, Element.Water).Should().Be(1.0);
        chart.GetMultiplier(Element.Normal, Element.Grass).Should().Be(1.0);
        chart.GetMultiplier(Element.Normal, Element.Electric).Should().Be(1.0);
        chart.GetMultiplier(Element.Normal, Element.Normal).Should().Be(1.0);
    }
}