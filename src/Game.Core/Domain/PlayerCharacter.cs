using Game.Core.Battle;

namespace Game.Core.Domain;

public enum Archetype
{
    Balanced,
    Warrior,
    Mage,
    Rogue
}

public sealed class PlayerCharacter : Character
{
    public Archetype Archetype { get; }

    // Total accumulated XP (across levels)
    public int TotalXp { get; private set; }

    // XP currently stored towards next level
    public int Xp { get; private set; }

    public int XpToNext => 50 * Level;

    public PlayerCharacter(string name, int level, Element type, Stats baseStats, Archetype archetype)
        : base(name, level, type, baseStats)
    {
        Archetype = archetype;
        TotalXp = 0;
        Xp = 0;
    }

    public override ActionIntent ChooseAction(BattleContext ctx)
        => throw new NotImplementedException("Player chooses via input/UI");

    public void GainXp(int amount)
    {
        if (amount <= 0) return;
        TotalXp += amount;
        Xp += amount;
        while (Xp >= XpToNext)
        {
            Xp -= XpToNext;
            LevelUp();
        }
    }

    public void LevelUp()
    {
        LevelUpInternal();
    }

    private void LevelUpInternal()
    {
        // Increase level
        Level += 1;
        // Growth per archetype
        var growth = GetGrowth(Archetype);

        var newBase = new Stats(
            Hp: BaseStats.Hp + growth.hp,
            Mp: BaseStats.Mp + growth.mp,
            Atk: BaseStats.Atk + growth.atk,
            Def: BaseStats.Def + growth.def,
            Spd: BaseStats.Spd + growth.spd,
            Acc: BaseStats.Acc, // keep
            Eva: BaseStats.Eva  // keep
        );

        BaseStats = newBase;
        // On level up, fully heal to new maxima (simple, deterministic behavior)
        Current = newBase;
    }

    private static (int hp, int mp, int atk, int def, int spd) GetGrowth(Archetype a) => a switch
    {
        Archetype.Balanced => (hp: 4, mp: 2, atk: 2, def: 2, spd: 1),
        Archetype.Warrior  => (hp: 6, mp: 1, atk: 3, def: 3, spd: 1),
        Archetype.Mage     => (hp: 3, mp: 4, atk: 1, def: 2, spd: 1),
        Archetype.Rogue    => (hp: 3, mp: 2, atk: 2, def: 1, spd: 3),
        _ => (4, 2, 2, 2, 1)
    };
}
