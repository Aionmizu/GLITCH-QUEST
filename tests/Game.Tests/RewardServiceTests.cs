using System.Linq;
using FluentAssertions;
using Game.Application.Battle;
using Game.Core.Domain;
using Game.Core.Domain.Items;
using Xunit;

namespace Game.Tests;

public class RewardServiceTests
{
    [Fact]
    public void ApplyVictoryRewards_GrantsXpAndAddsLoot()
    {
        var player = new PlayerCharacter("Hero", 1, Element.Fire, new Stats(30, 10, 5, 5, 5, 1.0, 1.0), Archetype.Balanced);
        var inv = new Inventory();

        RewardService.ApplyVictoryRewards(player, inv, xp: 20, loot: new PotionHP(10));

        player.TotalXp.Should().Be(20);
        inv.Items.OfType<PotionHP>().Count(p => p.Amount == 10).Should().Be(1);
    }

    [Fact]
    public void ApplyVictoryRewards_CanTriggerLevelUp()
    {
        var player = new PlayerCharacter("Hero", 1, Element.Fire, new Stats(30, 10, 5, 5, 5, 1.0, 1.0), Archetype.Balanced);
        var inv = new Inventory();

        // Level 1 -> XPToNext = 50
        RewardService.ApplyVictoryRewards(player, inv, xp: 60);

        player.Level.Should().Be(2);
        player.Xp.Should().Be(10); // carryover
    }
}
