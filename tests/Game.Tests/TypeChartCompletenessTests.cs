using System;
using FluentAssertions;
using Game.Core.Abstractions;
using Game.Core.Domain;
using Xunit;

namespace Game.Tests;

public class TypeChartCompletenessTests
{
    [Fact]
    public void Normal_is_neutral_against_all_types()
    {
        ITypeChart chart = SimpleTypeChart.Default();
        foreach (Element def in Enum.GetValues<Element>())
        {
            chart.GetMultiplier(Element.Normal, def).Should().Be(1.0);
        }
    }
}
