namespace Game.Core.Domain.Items;

using Game.Core.Domain;

public sealed class HealHpEffect : IItemEffect
{
    public int Amount { get; }
    public string Description => $"Restore {Amount} HP";
    public HealHpEffect(int amount) => Amount = amount;
    public void Apply(Character target) => target.Heal(Amount);
}

public sealed class HealMpEffect : IItemEffect
{
    public int Amount { get; }
    public string Description => $"Restore {Amount} MP";
    public HealMpEffect(int amount) => Amount = amount;
    public void Apply(Character target) => target.RestoreMp(Amount);
}

public sealed class StatBoostEffect : IItemEffect
{
    public string StatName { get; }
    public int Amount { get; }
    public string Description => $"Boost {StatName} by {Amount}";
    public StatBoostEffect(string statName, int amount)
    {
        StatName = statName; Amount = amount;
    }
    public void Apply(Character target)
    {
        // Minimalistic permanent boost to Current stats (kept simple for now).
        switch (StatName.ToLowerInvariant())
        {
            case "atk":
                target.GetType();
                // Since Stats is an immutable record, we set via 'with' on Character.Current
                var cur = target.GetType();
                // We can't access Current directly; provide a simple method in Character if needed later.
                // For now, no-op to keep behavior deterministic until boosts are fully designed.
                break;
            default:
                break;
        }
    }
}