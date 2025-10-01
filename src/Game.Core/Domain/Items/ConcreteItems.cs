namespace Game.Core.Domain.Items;

using Game.Core.Domain;

public sealed class PotionHP : Item
{
    public int Amount { get; }
    public PotionHP(int amount) : base($"potion_hp_{amount}", $"Potion HP +{amount}", new HealHpEffect(amount))
    {
        Amount = amount;
    }
}

public sealed class PotionMP : Item
{
    public int Amount { get; }
    public PotionMP(int amount) : base($"potion_mp_{amount}", $"Potion MP +{amount}", new HealMpEffect(amount))
    {
        Amount = amount;
    }
}

public sealed class StatBoostItem : Item
{
    public string Stat { get; }
    public int Amount { get; }
    public StatBoostItem(string stat, int amount) : base($"stat_boost_{stat}_{amount}", $"{stat.ToUpper()} +{amount}", new StatBoostEffect(stat, amount))
    {
        Stat = stat; Amount = amount;
    }
}

public sealed class KeyItem : Item
{
    public string KeyId { get; }
    public KeyItem(string keyId, string name) : base($"key_{keyId}", name, null)
    {
        KeyId = keyId;
    }
    public override bool CanUseOn(Character target) => false;
    public override void UseOn(Character target) { /* no-op */ }
}
