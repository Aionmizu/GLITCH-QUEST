using System.Linq;
using FluentAssertions;
using Game.Application.Menu;
using Xunit;

namespace Game.Tests;

public class UiAnimationTests
{
    [Fact]
    public void BuildBarFrames_includes_start_and_end_and_respects_steps()
    {
        var frames = UiAnimation.BuildBarFrames(previous: 50, current: 30, max: 100, width: 10, steps: 4);
        frames.Count.Should().Be(5); // steps + 1
        frames.First().Should().Be("#####-----"); // 50%
        frames.Last().Should().Be("###-------"); // 30%
    }

    [Fact]
    public void BuildBarFrames_handles_zero_max_as_empty_frames()
    {
        var frames = UiAnimation.BuildBarFrames(previous: 5, current: 1, max: 0, width: 6, steps: 3, fillChar: '=', emptyChar: '.');
        frames.Should().OnlyContain(f => f == "......".Replace('.', '.'));
    }

    [Fact]
    public void BuildBarFrames_returns_empty_when_width_is_non_positive()
    {
        var frames = UiAnimation.BuildBarFrames(previous: 10, current: 0, max: 100, width: 0, steps: 3);
        frames.Should().BeEmpty();
    }
}
