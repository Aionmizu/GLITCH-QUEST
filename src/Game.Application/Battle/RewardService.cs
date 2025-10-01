using Game.Core.Domain;
using Game.Core.Domain.Items;

namespace Game.Application.Battle;

/// <summary>
/// Simple reward applier to keep victory rewards testable and not tied to Program.cs.
/// </summary>
public static class RewardService
{
    /// <summary>
    /// Grants XP to the player and adds an optional loot item to the inventory.
    /// </summary>
    public static void ApplyVictoryRewards(PlayerCharacter player, Inventory inventory, int xp, Item? loot = null)
    {
        if (player is null) throw new ArgumentNullException(nameof(player));
        if (inventory is null) throw new ArgumentNullException(nameof(inventory));
        if (xp > 0) player.GainXp(xp);
        if (loot is not null) inventory.Add(loot);
    }
}
