using System;
using FluentAssertions;
using Game.Core.Domain;
using Xunit;

namespace Game.Tests;

public class LevelingTests
{
    [Fact]
    public void XpToNext_is_50_times_Level()
    {
        var pc = new PlayerCharacter("Hero", 1, Element.Normal, new Stats(20, 10, 5, 5, 5, 1.0, 1.0), Archetype.Balanced);
        pc.XpToNext.Should().Be(50);
        pc.LevelUp();
        pc.XpToNext.Should().Be(100);
    }

    [Fact]
    public void GainXp_levels_up_and_applies_balanced_growth_with_full_heal()
    {
        var baseStats = new Stats(20, 10, 5, 5, 5, 1.0, 1.0);
        var pc = new PlayerCharacter("Hero", 1, Element.Fire, baseStats, Archetype.Balanced);

        // Hurt the player to ensure level up heals to full
        pc.TakeDamage(10);
        pc.UseMp(5);
        pc.Current.Hp.Should().Be(10);
        pc.Current.Mp.Should().Be(5);

        // Gain enough XP for 2 level ups: need 50 for L1->2 and 100 for L2->3 => 150 total
        pc.GainXp(150);

        pc.Level.Should().Be(3);
        // Balanced growth per level: +4 HP, +2 MP, +2 Atk, +2 Def, +1 Spd per level
        // After 2 levels: +8 HP, +4 MP, +4 Atk, +4 Def, +2 Spd
        pc.BaseStats.Hp.Should().Be(28);
        pc.BaseStats.Mp.Should().Be(14);
        pc.BaseStats.Atk.Should().Be(9);
        pc.BaseStats.Def.Should().Be(9);
        pc.BaseStats.Spd.Should().Be(7);

        // Full heal to new maxima
        pc.Current.Hp.Should().Be(pc.BaseStats.Hp);
        pc.Current.Mp.Should().Be(pc.BaseStats.Mp);

        // Residual XP should be 0 after exactly matching thresholds
        pc.Xp.Should().Be(0);
        pc.TotalXp.Should().Be(150);
    }
}
