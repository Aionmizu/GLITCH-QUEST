using FluentAssertions;
using Game.Application.Exploration;
using Game.Core.Abstractions;
using Game.Core.Domain;
using Xunit;

namespace Game.Tests;

public class ExplorationMovementTests
{
    private sealed class DummyRng : IRandom
    {
        public double NextDouble() => 0.5;
        public double NextDouble(double minInclusive, double maxInclusive) => (minInclusive + maxInclusive) / 2.0;
    }

    private static string[] SmallMap = new[]
    {
        "#####",
        "#P..#",
        "#.#.#",
        "#...#",
        "#####"
    };

    [Fact]
    public void Player_spawns_on_P_and_cannot_walk_through_walls()
    {
        var map = Map.FromAsciiLines(SmallMap);
        var state = new ExplorationState(map);
        var svc = new ExplorationService(new DummyRng());

        // Spawn should be at (1,1)
        state.PlayerPos.Should().Be((1,1));

        // Try to walk into wall above
        svc.TryMove(state, 0, -1).Should().BeFalse();
        state.PlayerPos.Should().Be((1,1));

        // Move right onto floor
        svc.TryMove(state, 1, 0).Should().BeTrue();
        state.PlayerPos.Should().Be((2,1));

        // Try to walk into vertical wall
        svc.TryMove(state, 0, 1).Should().BeFalse();
        state.PlayerPos.Should().Be((2,1));

        // Walk down around wall
        svc.TryMove(state, 1, 0).Should().BeTrue(); // to (3,1)
        svc.TryMove(state, 0, 1).Should().BeTrue(); // to (3,2)
        svc.TryMove(state, 0, 1).Should().BeTrue(); // to (3,3)
        state.PlayerPos.Should().Be((3,3));

        // Out of bounds blocked
        svc.TryMove(state, 1, 0).Should().BeFalse(); // attempting to move into '#'
    }
}