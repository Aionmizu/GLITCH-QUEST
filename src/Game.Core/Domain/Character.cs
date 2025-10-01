using Game.Core.Abstractions;
using Game.Core.Battle;

namespace Game.Core.Domain;

public abstract class Character
{
    public string Name { get; }
    public int Level { get; protected set; }
    public Element Type { get; }
    public Stats BaseStats { get; protected set; }
    public Stats Current { get; protected set; }
    public List<Move> Moves { get; } = new();

    // Simple status system for combat effects
    public StatusAilment Status { get; protected set; } = StatusAilment.None;

    protected Character(string name, int level, Element type, Stats baseStats)
    {
        Name = name;
        Level = level;
        Type = type;
        BaseStats = baseStats;
        Current = baseStats;
    }

    public virtual ActionIntent ChooseAction(BattleContext ctx)
        => throw new NotImplementedException();

    public void Heal(int hp)
    {
        var newHp = Math.Min(BaseStats.Hp, Current.Hp + Math.Max(0, hp));
        Current = Current with { Hp = newHp };
    }

    public void TakeDamage(int hp)
    {
        var newHp = Math.Max(0, Current.Hp - Math.Max(0, hp));
        Current = Current with { Hp = newHp };
    }

    public bool UseMp(int mp)
    {
        if (Current.Mp < mp) return false;
        Current = Current with { Mp = Current.Mp - mp };
        return true;
    }

    public void RegenMpPercent(double percent)
    {
        var regen = (int)Math.Round(BaseStats.Mp * percent);
        var newMp = Math.Min(BaseStats.Mp, Current.Mp + regen);
        Current = Current with { Mp = newMp };
    }

    public void RestoreMp(int mp)
    {
        var newMp = Math.Min(BaseStats.Mp, Current.Mp + Math.Max(0, mp));
        Current = Current with { Mp = newMp };
    }

    public void ApplyStatus(StatusAilment status) => Status = status;
    public void ClearStatus() => Status = StatusAilment.None;
}

public sealed class EnemyCharacter : Character
{
    private readonly IAiStrategy _ai;

    public EnemyCharacter(string name, int level, Element type, Stats baseStats, IAiStrategy ai)
        : base(name, level, type, baseStats)
    {
        _ai = ai;
    }

    public override ActionIntent ChooseAction(BattleContext ctx) => _ai.ChooseAction(ctx);
}