using System;
using System.Collections.Generic;
using FluentAssertions;
using Game.Application.Battle;
using Game.Core.Abstractions;
using Game.Core.Battle;
using Game.Core.Domain;
using Xunit;

namespace Game.Tests;

public class BattleServiceTests
{
    private sealed class ScriptedRandom : IRandom
    {
        private readonly Queue<double> _queue;
        private double _last = 0.5;
        public ScriptedRandom(params double[] values) => _queue = new Queue<double>(values);
        public double NextDouble()
        {
            if (_queue.Count > 0) _last = _queue.Dequeue();
            return _last;
        }
        public double NextDouble(double minInclusive, double maxInclusive)
        {
            var v = NextDouble();
            return minInclusive + v * (maxInclusive - minInclusive);
        }
    }

    private sealed class DummyAi : IAiStrategy
    {
        private readonly Move _move;
        private readonly Func<BattleContext, (Character a, Character t)> _who;
        public DummyAi(Move move, Func<BattleContext, (Character a, Character t)> who)
        {
            _move = move; _who = who;
        }
        public ActionIntent ChooseAction(BattleContext context)
        {
            var (a, t) = _who(context);
            return ActionIntent.Attack(a, t, _move);
        }
    }

    private static EnemyCharacter MakeEnemy(string name, int spd, Element type, Stats stats, IAiStrategy ai)
        => new EnemyCharacter(name, 1, type, stats with { Spd = spd }, ai);

    [Fact]
    public void ComputeDamage_includes_rand_crit_stab_and_typechart()
    {
        // Arrange
        var move = new Move { Id = "ember", Name = "Flammèche", Type = Element.Fire, Power = 40, Kind = DamageKind.Physical, Accuracy = 0.95, MpCost = 0, CritChance = 0.1 };
        var atkStats = new Stats(100, 10, 10, 5, 5, 1.0, 1.0);
        var defStats = new Stats(100, 10, 10, 5, 5, 1.0, 1.0);
        var rng = new ScriptedRandom(1.0, 0.05); // rand=1.0, crit triggers (0.05 < 0.1)
        var chart = SimpleTypeChart.Default();
        var service = new BattleService(rng, chart);
        var attacker = MakeEnemy("Hero", 5, Element.Fire, atkStats, new DummyAi(move, ctx => (null!, null!)));
        var defender = MakeEnemy("Bug", 5, Element.Grass, defStats, new DummyAi(move, ctx => (null!, null!)));

        // Act
        var damage = service.ComputeDamage(attacker, defender, move);

        // Base = (10 * 40) / 5 = 80
        // Mult = rand(1.0) * crit(1.5) * stab(1.2) * type(2.0) = 3.6
        // Damage = round(80 * 3.6) = 288
        damage.Should().Be(288);
    }

    [Fact]
    public void Accuracy_is_clamped_between_0_10_and_0_99()
    {
        var rng = new ScriptedRandom(0.5);
        var chart = SimpleTypeChart.Default();
        var service = new BattleService(rng, chart);
        var move = new Move { Id = "spark", Name = "Etincelle", Type = Element.Electric, Power = 40, Kind = DamageKind.Physical, Accuracy = 1.0, MpCost = 0, CritChance = 0.0 };

        var lowAccAtt = new EnemyCharacter("A", 1, Element.Normal, new Stats(10, 0, 1, 1, 1, 0.001, 1.0), new DummyAi(move, ctx => (null!, null!)));
        var highEvaDef = new EnemyCharacter("D", 1, Element.Normal, new Stats(10, 0, 1, 1, 1, 1.0, 100.0), new DummyAi(move, ctx => (null!, null!)));
        var lowChance = service.ComputeHitChance(lowAccAtt, highEvaDef, move);
        lowChance.Should().Be(0.10);

        var highAccAtt = new EnemyCharacter("A2", 1, Element.Normal, new Stats(10, 0, 1, 1, 1, 1000.0, 1.0), new DummyAi(move, ctx => (null!, null!)));
        var lowEvaDef = new EnemyCharacter("D2", 1, Element.Normal, new Stats(10, 0, 1, 1, 1, 1.0, 0.001), new DummyAi(move, ctx => (null!, null!)));
        var highChance = service.ComputeHitChance(highAccAtt, lowEvaDef, move);
        highChance.Should().Be(0.99);
    }

    [Fact]
    public void Turn_order_is_determined_by_speed_tie_breaker_first_param()
    {
        var rng = new ScriptedRandom(1.0, 1.0);
        var chart = SimpleTypeChart.Default();
        var service = new BattleService(rng, chart);
        var move = new Move { Id = "tackle", Name = "Tacle", Type = Element.Normal, Power = 10, Kind = DamageKind.Physical, Accuracy = 1.0, MpCost = 0, CritChance = 0.0 };

        var slow = MakeEnemy("Slow", 5, Element.Normal, new Stats(10, 0, 5, 5, 5, 1.0, 1.0), new DummyAi(move, ctx => (null!, null!)));
        var fast = MakeEnemy("Fast", 10, Element.Normal, new Stats(10, 0, 5, 5, 10, 1.0, 1.0), new DummyAi(move, ctx => (null!, null!)));

        var a1 = ActionIntent.Attack(slow, fast, move);
        var a2 = ActionIntent.Attack(fast, slow, move);

        var result = service.ResolveTurn(a1, a2);
        result.FirstActor.Should().Be(fast);

        // tie -> first param wins
        var sameSpeedA = MakeEnemy("A", 10, Element.Normal, new Stats(10, 0, 5, 5, 10, 1.0, 1.0), new DummyAi(move, ctx => (null!, null!)));
        var sameSpeedB = MakeEnemy("B", 10, Element.Normal, new Stats(10, 0, 5, 5, 10, 1.0, 1.0), new DummyAi(move, ctx => (null!, null!)));
        var t1 = ActionIntent.Attack(sameSpeedA, sameSpeedB, move);
        var t2 = ActionIntent.Attack(sameSpeedB, sameSpeedA, move);
        var tieRes = service.ResolveTurn(t1, t2);
        tieRes.FirstActor.Should().Be(sameSpeedA);
    }
}
