using FluentAssertions;
using Game.Application.Battle;
using Game.Core.Abstractions;
using Game.Core.Battle;
using Game.Core.Domain;
using Game.Core.Domain.Items;
using Xunit;

namespace Game.Tests;

public class BattleItemUsageTests
{
    private sealed class FixedRandom : IRandom
    {
        private readonly double _v;
        public FixedRandom(double v) => _v = v;
        public double NextDouble() => _v;
        public double NextDouble(double minInclusive, double maxInclusive) => minInclusive + _v * (maxInclusive - minInclusive);
    }

    private sealed class DummyAi : IAiStrategy
    {
        public ActionIntent ChooseAction(BattleContext context) => throw new System.NotImplementedException();
    }

    private static EnemyCharacter Enemy(string name, Element type, Stats stats)
        => new EnemyCharacter(name, 1, type, stats, new DummyAi());

    [Fact]
    public void UseItem_heals_without_consuming_mp_and_marks_result()
    {
        var rng = new FixedRandom(0.5);
        var chart = SimpleTypeChart.Default();
        var service = new BattleService(rng, chart);

        var healer = Enemy("Healer", Element.Normal, new Stats(50, 10, 5, 5, 7, 1.0, 1.0));
        var other = Enemy("Other", Element.Normal, new Stats(50, 10, 5, 5, 5, 1.0, 1.0));

        // Hurt healer and reduce MP to check that MP is not consumed on item use
        healer.TakeDamage(20); // 30/50
        healer.UseMp(3); // 7/10
        var mpBefore = healer.Current.Mp;

        var potion = new Item("potion_hp", "Potion", new HealHpEffect(15));
        var a1 = ActionIntent.UseItem(healer, healer, potion);
        var a2 = ActionIntent.Defend(other);
        var turn = service.ResolveTurn(a1, a2);

        // Actor should have gained HP and kept same MP (plus end-of-turn regen)
        healer.Current.Hp.Should().Be(30 + 15);
        // ResolveTurn regenerates 10% MP; assert that only regen changed it
        var expectedAfter = System.Math.Min(healer.BaseStats.Mp, mpBefore + (int)System.Math.Round(healer.BaseStats.Mp * 0.10));
        healer.Current.Mp.Should().Be(expectedAfter);

        turn.FirstAction!.UsedItem.Should().NotBeNull();
        turn.FirstAction.UsedItem!.Id.Should().Be("potion_hp");
        turn.FirstAction.DamageDealt.Should().Be(0);
        turn.FirstAction.Hit.Should().BeFalse();
    }
}
