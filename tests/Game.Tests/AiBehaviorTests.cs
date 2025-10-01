using System.Collections.Generic;
using FluentAssertions;
using Game.Application.Battle;
using Game.Core.Abstractions;
using Game.Core.Battle;
using Game.Core.Domain;
using Game.Core.Domain.Items;
using Xunit;

namespace Game.Tests;

public class AiBehaviorTests
{
    private sealed class DummyRng : IRandom
    {
        public double NextDouble() => 0.5;
        public double NextDouble(double minInclusive, double maxInclusive) => (minInclusive + maxInclusive) / 2.0;
    }

    private static EnemyCharacter MakeEnemy(string name, Element type, Stats stats, IAiStrategy ai)
        => new EnemyCharacter(name, 1, type, stats, ai);

    [Fact]
    public void Low_hp_enemy_uses_healing_item_when_available()
    {
        var rng = new DummyRng();
        var chart = SimpleTypeChart.Default();
        var potion = new Item("potion_hp", "Potion de vie", new HealHpEffect(20));
        var ai = new SimpleAiStrategy(new[] { potion });

        var enemyStats = new Stats(30, 5, 5, 5, 5, 1.0, 1.0);
        var enemy = MakeEnemy("Bug", Element.Normal, enemyStats, ai);
        enemy.TakeDamage(25); // current HP = 5/30 -> 0.166 <= 0.30

        var player = new PlayerCharacter("Hero", 1, Element.Normal, new Stats(30, 10, 5, 5, 5, 1.0, 1.0), Archetype.Balanced);

        var ctx = new BattleContext(player, enemy, rng, chart);
        var intent = ai.ChooseAction(ctx);

        intent.Kind.Should().Be(ActionKind.UseItem);
        intent.Actor.Should().Be(enemy);
        intent.Target.Should().Be(enemy);
        intent.Item!.Id.Should().Be("potion_hp");
    }

    [Fact]
    public void Chooses_most_effective_affordable_move_by_type_and_stab()
    {
        var rng = new DummyRng();
        var chart = SimpleTypeChart.Default();
        var ai = new SimpleAiStrategy();

        var electric = new Move { Id = "spark", Name = "Etincelle", Type = Element.Electric, Power = 40, Kind = DamageKind.Physical, Accuracy = 1.0, MpCost = 3, CritChance = 0.0 };
        var normal = new Move { Id = "tackle", Name = "Tacle", Type = Element.Normal, Power = 50, Kind = DamageKind.Physical, Accuracy = 1.0, MpCost = 2, CritChance = 0.0 };

        var enemyStats = new Stats(50, 5, 5, 5, 5, 1.0, 1.0);
        var enemy = MakeEnemy("Bug", Element.Electric, enemyStats, ai);
        enemy.Moves.Add(electric);
        enemy.Moves.Add(normal);

        var player = new PlayerCharacter("Hero", 1, Element.Water, new Stats(40, 10, 5, 5, 5, 1.0, 1.0), Archetype.Balanced);
        var ctx = new BattleContext(player, enemy, rng, chart);

        var intent = ai.ChooseAction(ctx);
        intent.Kind.Should().Be(ActionKind.Attack);
        intent.Move!.Id.Should().Be("spark"); // Electric is 2x vs Water and also STAB
    }

    [Fact]
    public void Ignores_unaffordable_moves_and_defends_if_none()
    {
        var rng = new DummyRng();
        var chart = SimpleTypeChart.Default();
        var ai = new SimpleAiStrategy();

        var costly = new Move { Id = "big", Name = "Big", Type = Element.Fire, Power = 100, Kind = DamageKind.Physical, Accuracy = 1.0, MpCost = 10, CritChance = 0.0 };
        var cheap = new Move { Id = "small", Name = "Small", Type = Element.Normal, Power = 10, Kind = DamageKind.Physical, Accuracy = 1.0, MpCost = 1, CritChance = 0.0 };

        // Case 1: can afford only cheap
        var enemyStats = new Stats(50, 1, 5, 5, 5, 1.0, 1.0);
        var enemy = MakeEnemy("Bug", Element.Normal, enemyStats, ai);
        enemy.Moves.Add(costly);
        enemy.Moves.Add(cheap);
        var player = new PlayerCharacter("Hero", 1, Element.Grass, new Stats(40, 10, 5, 5, 5, 1.0, 1.0), Archetype.Balanced);
        var ctx = new BattleContext(player, enemy, rng, chart);
        var intent = ai.ChooseAction(ctx);
        intent.Kind.Should().Be(ActionKind.Attack);
        intent.Move!.Id.Should().Be("small");

        // Case 2: cannot afford any -> Defend
        var enemy2 = MakeEnemy("Bug2", Element.Normal, new Stats(50, 0, 5, 5, 5, 1.0, 1.0), ai);
        enemy2.Moves.Add(costly);
        var ctx2 = new BattleContext(player, enemy2, rng, chart);
        var intent2 = ai.ChooseAction(ctx2);
        intent2.Kind.Should().Be(ActionKind.Defend);
    }
}
