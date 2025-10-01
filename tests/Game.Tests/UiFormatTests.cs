using FluentAssertions;
using Game.Application.Menu;
using Xunit;

namespace Game.Tests;

public class UiFormatTests
{
    [Fact]
    public void BuildBar_clamps_and_builds_expected_strings()
    {
        UiFormat.BuildBar(50, 100, 10).Should().Be("#####-----");
        UiFormat.BuildBar(0, 100, 10).Should().Be("----------");
        UiFormat.BuildBar(100, 100, 10).Should().Be("##########");
        // Over max -> clamped to full
        UiFormat.BuildBar(150, 100, 10).Should().Be("##########");
        // Custom chars
        UiFormat.BuildBar(25, 100, 8, '=', '.').Should().Be("==......");
    }

    [Fact]
    public void BuildBar_handles_zero_or_negative_width_and_max()
    {
        UiFormat.BuildBar(5, 0, 10).Should().Be("----------");
        UiFormat.BuildBar(5, -1, 10).Should().Be("----------");
        UiFormat.BuildBar(5, 10, 0).Should().Be("");
    }
}
