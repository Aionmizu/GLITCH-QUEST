using System.Linq;
using FluentAssertions;
using Game.Application.Exploration;
using Game.Core.Domain;
using Game.Core.Domain.Items;
using Xunit;

namespace Game.Tests;

public class ExplorationInteractionsTests
{
    [Fact]
    public void OpenChest_at_player_tile_adds_item_and_turns_tile_to_floor()
    {
        var map = Map.FromAsciiLines(new[]
        {
            "#####",
            "#...#",
            "#.§.#",
            "#...#",
            "#####",
        });
        // Start at chest position (x=2, y=2)
        var state = new ExplorationState(map, startX: 2, startY: 2);
        var inv = new Inventory();
        var item = new PotionHP(20);

        var svc = new ExplorationInteractionService();
        var ok = svc.TryOpenChestAtPlayer(state, inv, item);

        ok.Should().BeTrue();
        inv.Items.OfType<PotionHP>().Any(p => p.Amount == 20).Should().BeTrue();
        map[2,2].Type.Should().Be(TileType.Floor);
        map[2,2].Glyph.Should().Be('.');
        map[2,2].Walkable.Should().BeTrue();
    }

    [Fact]
    public void TalkToNpc_returns_message_when_on_npc_tile()
    {
        var map = Map.FromAsciiLines(new[]
        {
            "#####",
            "#@..#",
            "#####",
        });
        var state = new ExplorationState(map, startX: 1, startY: 1);
        var svc = new ExplorationInteractionService();

        var ok = svc.TryTalkToNpcAtPlayer(state, out var msg, "Bonjour aventurier !");
        ok.Should().BeTrue();
        msg.Should().Be("Bonjour aventurier !");

        // Move off NPC and try again
        state.SetPlayerPos(2, 1);
        var ok2 = svc.TryTalkToNpcAtPlayer(state, out var msg2, "ignored");
        ok2.Should().BeFalse();
        msg2.Should().BeEmpty();
    }

    [Fact]
    public void OpenDoor_requires_matching_key_item_and_turns_tile_to_floor()
    {
        var map = Map.FromAsciiLines(new[]
        {
            "#####",
            "#+..#",
            "#####",
        });
        var state = new ExplorationState(map, startX: 1, startY: 1);
        var inv = new Inventory();
        var svc = new ExplorationInteractionService();

        // Without key, fails
        var fail = svc.TryOpenDoorAtPlayer(state, inv, requiredKeyId: "parc");
        fail.Should().BeFalse();
        map[1,1].Type.Should().Be(TileType.Door);

        // Add correct key and succeed
        inv.Add(new KeyItem("parc", "Clé du Parc"));
        var ok = svc.TryOpenDoorAtPlayer(state, inv, requiredKeyId: "parc");
        ok.Should().BeTrue();
        map[1,1].Type.Should().Be(TileType.Floor);
    }
}
