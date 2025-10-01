using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Battle;
using Game.Core.Domain;
using Game.Core.Domain.Items;

namespace Game.Application.Menu;

public enum BattleRootChoice
{
    Attaque,
    Magie,
    Objet,
    Fuite
}

public sealed record MenuOption(string Label, bool Enabled, object? Tag = null);

public sealed class MenuService
{
    /// <summary>
    /// Build the root battle menu: Attaque / Magie / Objet / Fuite.
    /// Pure function: does not depend on I/O.
    /// </summary>
    public IReadOnlyList<MenuOption> BuildRootBattleMenu(Character player, Inventory inventory)
    {
        if (player is null) throw new ArgumentNullException(nameof(player));
        if (inventory is null) throw new ArgumentNullException(nameof(inventory));
        return new List<MenuOption>
        {
            new("Attaque", true, BattleRootChoice.Attaque),
            new("Magie", true, BattleRootChoice.Magie),
            new("Objet", inventory.Items.Count > 0, BattleRootChoice.Objet),
            new("Fuite", true, BattleRootChoice.Fuite)
        };
    }

    /// <summary>
    /// Returns physical moves for the Attaque submenu. Disabled when MP is insufficient.
    /// </summary>
    public IReadOnlyList<MenuOption> BuildAttackSubmenu(Character player)
    {
        if (player is null) throw new ArgumentNullException(nameof(player));
        return player.Moves
            .Where(m => m.Kind == DamageKind.Physical)
            .Select(m => new MenuOption(LabelForMove(m), player.Current.Mp >= m.MpCost, m))
            .ToList();
    }

    /// <summary>
    /// Returns magical moves for the Magie submenu. Disabled when MP is insufficient.
    /// </summary>
    public IReadOnlyList<MenuOption> BuildMagicSubmenu(Character player)
    {
        if (player is null) throw new ArgumentNullException(nameof(player));
        return player.Moves
            .Where(m => m.Kind == DamageKind.Magic)
            .Select(m => new MenuOption(LabelForMove(m), player.Current.Mp >= m.MpCost, m))
            .ToList();
    }

    /// <summary>
    /// Returns inventory items usable in battle. Disabled if item cannot be used on the player now.
    /// </summary>
    public IReadOnlyList<MenuOption> BuildItemSubmenu(Inventory inventory, Character player)
    {
        if (inventory is null) throw new ArgumentNullException(nameof(inventory));
        if (player is null) throw new ArgumentNullException(nameof(player));
        return inventory.Items
            .Select(i => new MenuOption(LabelForItem(i), i.CanUseOn(player), i))
            .ToList();
    }

    public ActionIntent CreateIntentForMove(Character player, Character target, Move move)
    {
        if (player is null) throw new ArgumentNullException(nameof(player));
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (move is null) throw new ArgumentNullException(nameof(move));
        return ActionIntent.Attack(player, target, move);
    }

    public ActionIntent CreateIntentForItem(Character player, Character target, Item item)
    {
        if (player is null) throw new ArgumentNullException(nameof(player));
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (item is null) throw new ArgumentNullException(nameof(item));
        return ActionIntent.UseItem(player, target, item);
    }

    public ActionIntent CreateIntentForFlee(Character player)
    {
        if (player is null) throw new ArgumentNullException(nameof(player));
        return ActionIntent.Flee(player);
    }

    private static string LabelForMove(Move m)
        => $"{m.Name} ({m.Type}, Pow {m.Power}, MP {m.MpCost})";

    private static string LabelForItem(Item i)
        => i.Effect is null ? i.Name : $"{i.Name} - {i.Effect.Description}";
}
