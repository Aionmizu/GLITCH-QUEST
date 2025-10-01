using System.Linq;
using FluentAssertions;
using Game.Application.Menu;
using Game.Core.Battle;
using Game.Core.Domain;
using Game.Core.Domain.Items;
using Xunit;

namespace Game.Tests;

public class MenuServiceTests
{
    private static PlayerCharacter MakePlayer(int mp)
        => new PlayerCharacter("Hero", 1, Element.Fire, new Stats(30, mp, 6, 5, 5, 1.0, 1.0), Archetype.Balanced);

    [Fact]
    public void RootMenu_contains_four_entries_and_items_disabled_when_empty()
    {
        var svc = new MenuService();
        var pc = MakePlayer(10);
        var inv = new Inventory();

        var root = svc.BuildRootBattleMenu(pc, inv);
        root.Should().HaveCount(4);
        root[0].Label.Should().Be("Attaque");
        root[1].Label.Should().Be("Magie");
        root[2].Label.Should().Be("Objet");
        root[2].Enabled.Should().BeFalse();
        root[3].Label.Should().Be("Fuite");
    }

    [Fact]
    public void Attack_and_Magic_submenus_filter_by_kind_and_affordability()
    {
        var svc = new MenuService();
        var pc = MakePlayer(3);
        var phys = new Move { Id = "tackle", Name = "Tacle", Type = Element.Normal, Power = 10, Kind = DamageKind.Physical, Accuracy = 1.0, MpCost = 0, CritChance = 0.0 };
        var magicCheap = new Move { Id = "ember", Name = "Flammèche", Type = Element.Fire, Power = 40, Kind = DamageKind.Magic, Accuracy = 1.0, MpCost = 3, CritChance = 0.0 };
        var magicCostly = new Move { Id = "fireball", Name = "Boule de feu", Type = Element.Fire, Power = 70, Kind = DamageKind.Magic, Accuracy = 1.0, MpCost = 6, CritChance = 0.1 };
        pc.Moves.Add(phys);
        pc.Moves.Add(magicCheap);
        pc.Moves.Add(magicCostly);

        var atkMenu = svc.BuildAttackSubmenu(pc);
        atkMenu.Should().HaveCount(1);
        atkMenu.Single().Label.Should().Contain("Tacle");
        atkMenu.Single().Enabled.Should().BeTrue();

        var magMenu = svc.BuildMagicSubmenu(pc);
        magMenu.Should().HaveCount(2);
        // Cheap one enabled, costly disabled due to MP 3 < 6
        magMenu.First(m => m.Tag == magicCheap).Enabled.Should().BeTrue();
        magMenu.First(m => m.Tag == magicCostly).Enabled.Should().BeFalse();
    }

    [Fact]
    public void Item_submenu_lists_items_and_disables_unusable()
    {
        var svc = new MenuService();
        var pc = MakePlayer(5);
        var inv = new Inventory();
        var potion = new Item("potion_hp", "Potion", new HealHpEffect(15));
        var dud = new Item("rock", "Pierre", effect: null); // cannot be used (no effect)
        inv.Add(potion);
        inv.Add(dud);

        var items = svc.BuildItemSubmenu(inv, pc);
        items.Should().HaveCount(2);
        items.First(i => i.Tag == potion).Enabled.Should().BeTrue();
        items.First(i => i.Tag == dud).Enabled.Should().BeFalse();
    }

    [Fact]
    public void Create_intents_for_move_item_and_flee_are_correct()
    {
        var svc = new MenuService();
        var pc = MakePlayer(10);
        var enemy = new PlayerCharacter("Bug", 1, Element.Grass, new Stats(20, 5, 4, 3, 3, 1.0, 1.0), Archetype.Balanced);
        var move = new Move { Id = "ember", Name = "Flammèche", Type = Element.Fire, Power = 40, Kind = DamageKind.Magic, Accuracy = 1.0, MpCost = 3, CritChance = 0.0 };
        var item = new Item("potion_hp", "Potion", new HealHpEffect(10));

        var a1 = svc.CreateIntentForMove(pc, enemy, move);
        a1.Kind.Should().Be(ActionKind.Attack);
        a1.Move.Should().Be(move);
        a1.Actor.Should().Be(pc);
        a1.Target.Should().Be(enemy);

        var a2 = svc.CreateIntentForItem(pc, pc, item);
        a2.Kind.Should().Be(ActionKind.UseItem);
        a2.Item.Should().Be(item);
        a2.Actor.Should().Be(pc);
        a2.Target.Should().Be(pc);

        var a3 = svc.CreateIntentForFlee(pc);
        a3.Kind.Should().Be(ActionKind.Flee);
        a3.Actor.Should().Be(pc);
        a3.Target.Should().BeNull();
    }
}
