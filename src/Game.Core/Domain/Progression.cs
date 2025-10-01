namespace Game.Core.Domain;

/// <summary>
/// Defines zone key identifiers and simple progression checks.
/// </summary>
public static class Progression
{
    public const string KeyParc = "parc";
    public const string KeyLaboratoire = "labo";
    public const string KeyNoyau = "noyau";

    /// <summary>
    /// Returns true when the player holds all three zone keys.
    /// </summary>
    public static bool HasAllZoneKeys(Inventory inventory)
        => inventory.HasKey(KeyParc) && inventory.HasKey(KeyLaboratoire) && inventory.HasKey(KeyNoyau);
}
