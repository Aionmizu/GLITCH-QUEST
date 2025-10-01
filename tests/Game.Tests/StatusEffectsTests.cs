using System;
using System.Collections.Generic;
using FluentAssertions;
using Game.Application.Battle;
using Game.Core.Abstractions;
using Game.Core.Battle;
using Game.Core.Domain;
using Xunit;

namespace Game.Tests;

public class StatusEffectsTests
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

    private static EnemyCharacter MakeEnemy(string name, int spd, Element type, Stats stats)
        => new EnemyCharacter(name, 1, type, stats with { Spd = spd }, new DummyAi());

    private sealed class DummyAi : IAiStrategy
    {
        public ActionIntent ChooseAction(BattleContext context) => throw new NotImplementedException();
    }

    [Fact]
    public void Burn_reduces_attack_damage_by_20_percent_and_applies_end_of_turn_dot()
    {
        var rng = new ScriptedRandom(1.0, 1.0); // rand=1.0, crit roll high (no crit)
        var chart = SimpleTypeChart.Default();
        var service = new BattleService(rng, chart);
        var move = new Move { Id = "hit", Name = "Hit", Type = Element.Normal, Power = 50, Kind = DamageKind.Physical, Accuracy = 1.0, MpCost = 0, CritChance = 0.0 };

        var atkStats = new Stats(100, 10, 10, 5, 10, 1.0, 1.0); // base = (10*50)/5 = 100
        var defStats = new Stats(100, 10, 10, 5, 10, 1.0, 1.0);
        var attacker = MakeEnemy("A", 10, Element.Fire, atkStats);
        var defender = MakeEnemy("D", 10, Element.Normal, defStats);

        var normal = service.ComputeDamage(attacker, defender, move);
        normal.Should().Be(100);

        attacker.ApplyStatus(StatusAilment.Burn);
        var burned = service.ComputeDamage(attacker, defender, move);
        burned.Should().Be(80); // 20% less

        // End-of-turn DoT: 5% of base HP = 5 on each burned unit
        var a1 = ActionIntent.Attack(attacker, defender, move);
        var a2 = ActionIntent.Attack(defender, attacker, move);
        var beforeHp = attacker.Current.Hp;
        var res = service.ResolveTurn(a1, a2);
        attacker.Current.Hp.Should().BeLessThan(beforeHp);
        (beforeHp - attacker.Current.Hp).Should().BeGreaterOrEqualTo(5);
    }

    [Fact]
    public void Paralysis_halves_speed_for_turn_order()
    {
        var rng = new ScriptedRandom(1.0, 1.0);
        var chart = SimpleTypeChart.Default();
        var service = new BattleService(rng, chart);
        var move = new Move { Id = "tackle", Name = "Tacle", Type = Element.Normal, Power = 10, Kind = DamageKind.Physical, Accuracy = 1.0, MpCost = 0, CritChance = 0.0 };

        var normal = MakeEnemy("Normal", 10, Element.Normal, new Stats(50, 0, 5, 5, 10, 1.0, 1.0));
        var para = MakeEnemy("Para", 10, Element.Normal, new Stats(50, 0, 5, 5, 10, 1.0, 1.0));
        para.ApplyStatus(StatusAilment.Paralysis);

        var a1 = ActionIntent.Attack(para, normal, move);
        var a2 = ActionIntent.Attack(normal, para, move);
        var result = service.ResolveTurn(a1, a2);
        result.FirstActor.Should().Be(normal); // paralysed actor acts second due to halved speed
    }

    [Fact]
    public void Paralysis_can_cause_turn_skip_before_mp_is_consumed()
    {
        // First RNG call is used for paralysis check; return 0.0 to force skip (<0.25)
        var rng = new ScriptedRandom(0.0);
        var chart = SimpleTypeChart.Default();
        var service = new BattleService(rng, chart);
        var move = new Move { Id = "zap", Name = "Zap", Type = Element.Electric, Power = 10, Kind = DamageKind.Physical, Accuracy = 1.0, MpCost = 3, CritChance = 0.0 };

        var para = MakeEnemy("Para", 10, Element.Normal, new Stats(50, 10, 5, 5, 10, 1.0, 1.0));
        var target = MakeEnemy("Target", 5, Element.Normal, new Stats(50, 10, 5, 5, 5, 1.0, 1.0));
        para.ApplyStatus(StatusAilment.Paralysis);

        var mpBefore = para.Current.Mp;
        var a1 = ActionIntent.Attack(para, target, move);
        var a2 = ActionIntent.Attack(target, para, move);
        var result = service.ResolveTurn(a1, a2);

        // The paralysed actor should have skipped its action without consuming MP
        para.Current.Mp.Should().Be(mpBefore);
        // Ensure no damage was dealt by the paralysed actor (either first or second)
        if (ReferenceEquals(result.FirstActor, para))
        {
            result.FirstAction!.DamageDealt.Should().Be(0);
            result.FirstAction.Hit.Should().BeFalse();
        }
        else
        {
            result.SecondAction!.DamageDealt.Should().Be(0);
            result.SecondAction.Hit.Should().BeFalse();
        }
    }
}
