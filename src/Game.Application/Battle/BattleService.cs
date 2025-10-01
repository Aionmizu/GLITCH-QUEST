using Game.Core.Abstractions;
using Game.Core.Battle;
using Game.Core.Domain;
using Game.Core.Domain.Items;

namespace Game.Application.Battle;

public sealed class BattleService
{
    private readonly IRandom _rng;
    private readonly ITypeChart _chart;

    public BattleService(IRandom rng, ITypeChart chart)
    {
        _rng = rng;
        _chart = chart;
    }

    public double ComputeHitChance(Character attacker, Character defender, Move move)
    {
        var baseChance = move.Accuracy * (attacker.BaseStats.Acc / Math.Max(0.0001, defender.BaseStats.Eva));
        // clamp to [0.10, 0.99]
        var clamped = Math.Clamp(baseChance, 0.10, 0.99);
        return clamped;
    }

    public int ComputeDamage(Character attacker, Character defender, Move move)
    {
        var atkStat = attacker.BaseStats.Atk;
        // Burn reduces attack by 20%
        if (attacker.Status == StatusAilment.Burn)
            atkStat = (int)Math.Round(atkStat * 0.8);
        var defStat = Math.Max(1, defender.BaseStats.Def);
        var baseVal = (atkStat * move.Power) / (double)defStat;

        var rand = _rng.NextDouble(0.85, 1.00);
        var crit = _rng.NextDouble() < move.CritChance ? 1.5 : 1.0;
        var stab = move.Type == attacker.Type ? 1.2 : 1.0;
        var typeMul = _chart.GetMultiplier(move.Type, defender.Type);

        var mult = rand * crit * stab * typeMul;
        var raw = baseVal * mult;
        var dmg = (int)Math.Round(raw);
        return Math.Max(1, dmg);
    }

    public TurnResult ResolveTurn(ActionIntent a1, ActionIntent a2)
    {
        // Determine order by effective speed (Paralysis halves speed)
        int EffSpd(Character c) => c.Status == StatusAilment.Paralysis ? (int)Math.Floor(c.BaseStats.Spd * 0.5) : c.BaseStats.Spd;
        var speed1 = EffSpd(a1.Actor);
        var speed2 = EffSpd(a2.Actor);
        var first = speed1 >= speed2 ? a1 : a2; // tie -> first param wins
        var second = ReferenceEquals(first, a1) ? a2 : a1;

        var r1 = Execute(first, second.Actor);
        // If first actor fled successfully, skip second action
        var r2 = (r1?.Fled ?? false) ? null : Execute(second, first.Actor, skipIfDead: r1?.TargetDeadAfter ?? false);

        // End of turn: regen 10% MP (even if someone fled; keeps deterministic resource flow)
        first.Actor.RegenMpPercent(0.10);
        second.Actor.RegenMpPercent(0.10);

        // End of turn: Burn DoT (5% of base HP)
        void ApplyBurnDot(Character c)
        {
            if (c.Status == StatusAilment.Burn && c.Current.Hp > 0)
            {
                var dot = Math.Max(1, (int)Math.Round(c.BaseStats.Hp * 0.05));
                c.TakeDamage(dot);
            }
        }
        ApplyBurnDot(first.Actor);
        ApplyBurnDot(second.Actor);

        return new TurnResult(r1, r2, first.Actor, second.Actor);
    }

    private TurnActionResult? Execute(ActionIntent intent, Character opponent, bool skipIfDead = false)
    {
        var actor = intent.Actor;
        var target = intent.Target;

        switch (intent.Kind)
        {
            case ActionKind.Attack:
            {
                var attacker = actor;
                var tgt = target!;

                if (skipIfDead || tgt.Current.Hp <= 0)
                    return new TurnActionResult(attacker, tgt, intent.Move, false, 0, tgt.Current.Hp <= 0);

                // Paralysis: 25% chance to be fully paralyzed (skip turn) BEFORE paying MP
                if (attacker.Status == StatusAilment.Paralysis)
                {
                    if (_rng.NextDouble() < 0.25)
                    {
                        return new TurnActionResult(attacker, tgt, intent.Move, false, 0, tgt.Current.Hp <= 0);
                    }
                }

                // Pay MP if needed
                if (!attacker.UseMp(intent.Move!.MpCost))
                    return new TurnActionResult(attacker, tgt, intent.Move, false, 0, false);

                // Roll hit
                var hitChance = ComputeHitChance(attacker, tgt, intent.Move);
                var roll = _rng.NextDouble();
                var hit = roll <= hitChance;
                int damage = 0;
                if (hit)
                {
                    damage = ComputeDamage(attacker, tgt, intent.Move);
                    tgt.TakeDamage(damage);
                }
                return new TurnActionResult(attacker, tgt, intent.Move, hit, damage, tgt.Current.Hp <= 0);
            }
            case ActionKind.UseItem:
            {
                // Apply item effect; no MP cost and no hit/damage
                intent.Item?.UseOn(target ?? actor);
                return new TurnActionResult(actor, target, move: null, hit: false, damageDealt: 0, targetDeadAfter: (target ?? actor).Current.Hp <= 0)
                {
                    UsedItem = intent.Item
                };
            }
            case ActionKind.Defend:
            {
                // No concrete effect yet; can be extended later
                return new TurnActionResult(actor, target, move: null, hit: false, damageDealt: 0, targetDeadAfter: target?.Current.Hp <= 0)
                {
                    Defended = true
                };
            }
            case ActionKind.Flee:
            {
                // Success if actor effective SPD strictly greater than opponent; else 30% chance
                bool success;
                if (opponent is null)
                {
                    success = _rng.NextDouble() < 0.30;
                }
                else
                {
                    int EffSpd(Character c) => c.Status == StatusAilment.Paralysis ? (int)Math.Floor(c.BaseStats.Spd * 0.5) : c.BaseStats.Spd;
                    var faster = EffSpd(actor) > EffSpd(opponent);
                    success = faster || _rng.NextDouble() < 0.30;
                }
                return new TurnActionResult(actor, target, move: null, hit: false, damageDealt: 0, targetDeadAfter: target?.Current.Hp <= 0)
                {
                    Fled = success
                };
            }
            default:
                return new TurnActionResult(actor, target, intent.Move, false, 0, target?.Current.Hp <= 0);
        }
    }
}

public sealed class TurnActionResult
{
    public Character Actor { get; }
    public Character? Target { get; }
    public Move? Move { get; }
    public bool Hit { get; }
    public int DamageDealt { get; }
    public bool TargetDeadAfter { get; }

    // Additional outcome flags for richer battle flow
    public bool Fled { get; init; }
    public bool Defended { get; init; }
    public Item? UsedItem { get; init; }

    public TurnActionResult(Character actor, Character? target, Move? move, bool hit, int damageDealt, bool targetDeadAfter)
    {
        Actor = actor;
        Target = target;
        Move = move;
        Hit = hit;
        DamageDealt = damageDealt;
        TargetDeadAfter = targetDeadAfter;
    }
}

public sealed class TurnResult
{
    public TurnActionResult? FirstAction { get; }
    public TurnActionResult? SecondAction { get; }
    public Character FirstActor { get; }
    public Character SecondActor { get; }

    public TurnResult(TurnActionResult? firstAction, TurnActionResult? secondAction, Character firstActor, Character secondActor)
    {
        FirstAction = firstAction;
        SecondAction = secondAction;
        FirstActor = firstActor;
        SecondActor = secondActor;
    }
}