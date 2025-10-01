using System;
using System.Linq;
using Game.Core.Abstractions;

namespace Game.Core.Domain;

public sealed record EncounterEntry(string EnemyId, int MinLevel, int MaxLevel, int Weight)
{
    public override string ToString() => $"{EnemyId} [{MinLevel}-{MaxLevel}] (w={Weight})";
}

public sealed class EncounterTable
{
    public IReadOnlyList<EncounterEntry> Entries { get; }

    public EncounterTable(IEnumerable<EncounterEntry> entries)
    {
        var list = entries?.ToList() ?? throw new ArgumentNullException(nameof(entries));
        if (list.Count == 0) throw new ArgumentException("EncounterTable must have at least one entry", nameof(entries));
        if (list.Any(e => e.Weight <= 0)) throw new ArgumentException("Weights must be positive", nameof(entries));
        if (list.Any(e => e.MinLevel <= 0 || e.MaxLevel < e.MinLevel)) throw new ArgumentException("Invalid level range", nameof(entries));
        Entries = list;
    }

    public (string EnemyId, int Level) Roll(IRandom rng)
    {
        if (rng is null) throw new ArgumentNullException(nameof(rng));
        var totalWeight = Entries.Sum(e => e.Weight);
        var r = rng.NextDouble(0.0, totalWeight);
        int cumulative = 0;
        EncounterEntry pick = Entries[0];
        foreach (var e in Entries)
        {
            cumulative += e.Weight;
            if (r < cumulative)
            {
                pick = e;
                break;
            }
        }
        // pick level uniformly from [MinLevel, MaxLevel] inclusive using NextDouble
        var lvlSpan = pick.MaxLevel - pick.MinLevel + 1;
        var roll = rng.NextDouble();
        var lvlOffset = (int)Math.Floor(roll * lvlSpan);
        var level = pick.MinLevel + Math.Clamp(lvlOffset, 0, Math.Max(0, lvlSpan - 1));
        return (pick.EnemyId, level);
    }
}