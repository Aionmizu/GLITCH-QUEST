using FluentAssertions;
using Game.Core.Battle;
using Game.Core.Domain;
using Game.Core.Domain.Items;
using Xunit;

namespace Game.Tests;

public class ItemsAndInventoryTests
{
    private sealed class TestChar : Character
    {
        public TestChar(string name, Stats baseStats) : base(name, 1, Element.Normal, baseStats) { }
        public override ActionIntent ChooseAction(BattleContext ctx) => ActionIntent.Defend(this);
    }

    [Fact]
    public void PotionHP_and_PotionMP_restore_with_clamp()
    {
        var stats = new Stats(100, 50, 10, 10, 10, 1.0, 1.0);
        var c = new TestChar("Hero", stats);
        c.TakeDamage(80); // HP=20
        c.UseMp(45);      // MP=5

        var hp = new PotionHP(50);
        var mp = new PotionMP(40);

        hp.CanUseOn(c).Should().BeTrue();
        mp.CanUseOn(c).Should().BeTrue();

        hp.UseOn(c); // HP becomes 70
        mp.UseOn(c); // MP becomes 45

        c.Current.Hp.Should().Be(70);
        c.Current.Mp.Should().Be(45);

        // Overheal/overfill clamps to max
        new PotionHP(100).UseOn(c);
        new PotionMP(100).UseOn(c);
        c.Current.Hp.Should().Be(stats.Hp);
        c.Current.Mp.Should().Be(stats.Mp);
    }

    [Fact]
    public void Inventory_add_remove_and_has_key()
    {
        var inv = new Inventory();
        var key = new KeyItem("lab", "Clé du labo");
        var hp = new PotionHP(20);

        inv.HasKey("lab").Should().BeFalse();
        inv.Add(key);
        inv.Add(hp);
        inv.HasKey("lab").Should().BeTrue();

        inv.Remove(hp).Should().BeTrue();
        inv.Remove(new KeyItem("other", "Other")).Should().BeFalse();
    }
}