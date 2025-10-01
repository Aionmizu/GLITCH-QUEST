using FluentAssertions;
using Game.Application.Battle;
using Game.Core.Abstractions;
using Game.Core.Battle;
using Game.Core.Domain;
using Xunit;

namespace Game.Tests;

public class FleeMechanicsTests
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

    private static EnemyCharacter Enemy(string name, int spd, Element type)
        => new EnemyCharacter(name, 1, type, new Stats(50, 10, 5, 5, spd, 1.0, 1.0), new DummyAi());

    private static Move Tackle => new Move { Id = "tackle", Name = "Tacle", Type = Element.Normal, Power = 10, Kind = DamageKind.Physical, Accuracy = 1.0, MpCost = 0, CritChance = 0.0 };

    [Fact]
    public void Faster_actor_flees_successfully_and_second_action_is_skipped()
    {
        var rng = new FixedRandom(0.99); // high roll shouldn't matter when faster
        var chart = SimpleTypeChart.Default();
        var service = new BattleService(rng, chart);

        var fast = Enemy("Fast", 10, Element.Normal);
        var slow = Enemy("Slow", 5, Element.Normal);

        var a1 = ActionIntent.Flee(fast);
        var a2 = ActionIntent.Attack(slow, fast, Tackle);

        var turn = service.ResolveTurn(a1, a2);
        // Determine which action corresponds to the fleeing actor
        var fleeAction = ReferenceEquals(turn.FirstActor, fast) ? turn.FirstAction : turn.SecondAction;
        fleeAction!.Fled.Should().BeTrue();
        // If the fleeing actor went first, second action is skipped
        if (ReferenceEquals(turn.FirstActor, fast))
            turn.SecondAction.Should().BeNull("second action should be skipped when first actor flees successfully");
    }

    [Fact]
    public void Slower_actor_can_flee_with_30_percent_random_chance()
    {
        var rngSuccess = new FixedRandom(0.0); // force success (<0.30)
        var chart = SimpleTypeChart.Default();
        var service = new BattleService(rngSuccess, chart);

        var slow = Enemy("Slow", 5, Element.Normal);
        var fast = Enemy("Fast", 10, Element.Normal);

        var a1 = ActionIntent.Flee(slow);
        var a2 = ActionIntent.Attack(fast, slow, Tackle);

        var turn = service.ResolveTurn(a1, a2);
        var fleeAction = ReferenceEquals(turn.FirstActor, slow) ? turn.FirstAction : turn.SecondAction;
        fleeAction!.Fled.Should().BeTrue();
        // If the fleeing actor acted first, second will be skipped; otherwise first action still occurred
        if (ReferenceEquals(turn.FirstActor, slow))
            turn.SecondAction.Should().BeNull();
    }

    [Fact]
    public void Slower_actor_fails_to_flee_and_both_actions_execute()
    {
        var rngFail = new FixedRandom(0.99); // fail (>0.30)
        var chart = SimpleTypeChart.Default();
        var service = new BattleService(rngFail, chart);

        var slow = Enemy("Slow", 5, Element.Normal);
        var fast = Enemy("Fast", 10, Element.Normal);

        var a1 = ActionIntent.Flee(slow);
        var a2 = ActionIntent.Attack(fast, slow, Tackle);

        var turn = service.ResolveTurn(a1, a2);
        turn.FirstAction!.Fled.Should().BeFalse();
        turn.SecondAction.Should().NotBeNull();
    }
}
