using System.Linq;
using FluentAssertions;
using Game.Application.Exploration;
using Game.Core.Domain;
using Game.Core.Domain.Items;
using Xunit;

namespace Game.Tests;

public class ZoneProgressionTests
{
    [Fact]
    public void FinalDoor_requires_all_three_zone_keys()
    {
        var map = Map.FromAsciiLines(new[]
        {
            "#####",
            "#+..#",
            "#####"
        });
        var state = new ExplorationState(map, startX: 1, startY: 1);
        var inv = new Inventory();
        var svc = new ExplorationInteractionService();

        // No keys -> cannot open final door
        svc.TryOpenFinalDoorAtPlayer(state, inv).Should().BeFalse();
        map[1,1].Type.Should().Be(TileType.Door);

        // Add some but not all keys -> still cannot open
        inv.Add(new KeyItem(Progression.KeyParc, "Clé du Parc"));
        inv.Add(new KeyItem(Progression.KeyLaboratoire, "Clé du Laboratoire"));
        svc.TryOpenFinalDoorAtPlayer(state, inv).Should().BeFalse();
        map[1,1].Type.Should().Be(TileType.Door);

        // Add the last missing key -> can open
        inv.Add(new KeyItem(Progression.KeyNoyau, "Clé du Noyau"));
        svc.TryOpenFinalDoorAtPlayer(state, inv).Should().BeTrue();
        map[1,1].Type.Should().Be(TileType.Floor);
    }
}
