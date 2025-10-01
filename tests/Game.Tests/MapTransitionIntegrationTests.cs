using System;
using System.Collections.Generic;
using FluentAssertions;
using Game.Application.Exploration;
using Game.Core.Domain;
using Game.Core.Domain.Items;
using Xunit;

namespace Game.Tests;

public class MapTransitionIntegrationTests
{
    private static IReadOnlyList<string> MapA => new[]
    {
        "#####",
        "#P.+#",
        "#####",
    };

    private static IReadOnlyList<string> MapB => new[]
    {
        "#####",
        "#..P#",
        "#####",
    };

    [Fact]
    public void OpeningDoor_AllowsTransition_AndPlayerSpawnsAtNewMapSpawn()
    {
        var rng = new TestRng();
        var exploration = new ExplorationService(rng);
        var interaction = new ExplorationInteractionService();

        // Load first map and place player on tile just left of door '+'
        var map1 = exploration.LoadMapFromAscii(MapA);
        var state = new ExplorationState(map1);
        // Ensure spawn from 'P'
        state.PlayerX.Should().Be(1);
        state.PlayerY.Should().Be(1);

        // Move next to the door (at x=2,y=1) and give key, then open
        state.SetPlayerPos(2, 1);
        var inv = new Inventory();
        inv.Add(new KeyItem("parc", "Clé du Parc"));
        interaction.TryOpenDoorAtPlayer(state, inv, "parc").Should().BeTrue("should open adjacent door and convert to floor");
        map1[1, 3].Type.Should().Be(TileType.Floor);

        // Simulate program transition: load map B and create new exploration state -> should respawn at P in map B
        var map2 = exploration.LoadMapFromAscii(MapB);
        var state2 = new ExplorationState(map2);
        state2.PlayerPos.Should().Be((3, 1));
    }

    private sealed class TestRng : Game.Core.Abstractions.IRandom
    {
        private double _next = 0.5;
        public double NextDouble() => _next;
        public double NextDouble(double minInclusive, double maxExclusive) => minInclusive;
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }
}
