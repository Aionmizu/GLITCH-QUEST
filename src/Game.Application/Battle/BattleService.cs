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

    private DamageDetails ComputeDamageDetailed(Character attacker, Character defender, Move move)
    {
        var atkStat = attacker.BaseStats.Atk;
        // Burn reduces attack by 20%
        if (attacker.Status == StatusAilment.Burn)
            atkStat = (int)Math.Round(atkStat * 0.8);
        var defStat = Math.Max(1, defender.BaseStats.Def);
        var baseVal = (atkStat * move.Power) / (double)defStat;

        var rand = _rng.NextDouble(0.85, 1.00);
        var isCrit = _rng.NextDouble() < move.CritChance;
        var crit = isCrit ? 1.5 : 1.0;
        var hasStab = move.Type == attacker.Type;
        var stab = hasStab ? 1.2 : 1.0;
        var typeMul = _chart.GetMultiplier(move.Type, defender.Type);

        var mult = rand * crit * stab * typeMul;
        var raw = baseVal * mult;
        var dmg = Math.Max(1, (int)Math.Round(raw));
        return new DamageDetails(dmg, isCrit, typeMul, rand, hasStab);
    }

    public int ComputeDamage(Character attacker, Character defender, Move move)
    {
        // Preserve public API used by tests
        var details = ComputeDamageDetailed(attacker, defender, move);
        return details.Damage;
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

        // End of turn: Burn DoT (5% of base HP) and messages
        var endMessages = new List<string>();
        void ApplyBurnDot(Character c)
        {
            if (c.Status == StatusAilment.Burn && c.Current.Hp > 0)
            {
                var dot = Math.Max(1, (int)Math.Round(c.BaseStats.Hp * 0.05));
                c.TakeDamage(dot);
                endMessages.Add($"{c.Name} souffre de brûlure (-{dot} PV).");
            }
        }
        ApplyBurnDot(first.Actor);
        ApplyBurnDot(second.Actor);

        var tr = new TurnResult(r1, r2, first.Actor, second.Actor)
        {
            EndOfTurnMessages = endMessages
        };
        return tr;
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
                    return new TurnActionResult(attacker, tgt, intent.Move, false, 0, tgt.Current.Hp <= 0)
                    {
                        Messages = new List<string> { $"{attacker.Name} ne peut pas attaquer, la cible est à terre." }
                    };

                // Paralysis: 25% chance to be fully paralyzed (skip turn) BEFORE paying MP
                if (attacker.Status == StatusAilment.Paralysis)
                {
                    if (_rng.NextDouble() < 0.25)
                    {
                        return new TurnActionResult(attacker, tgt, intent.Move, false, 0, tgt.Current.Hp <= 0)
                        {
                            Messages = new List<string> { $"{attacker.Name} est paralysé ! L'action échoue." }
                        };
                    }
                }

                // Pay MP if needed
                if (!attacker.UseMp(intent.Move!.MpCost))
                    return new TurnActionResult(attacker, tgt, intent.Move, false, 0, false)
                    {
                        Messages = new List<string> { $"{attacker.Name} n'a pas assez de MP pour {intent.Move!.Name}." }
                    };

                var messages = new List<string> { $"{attacker.Name} utilise {intent.Move!.Name} !" };

                // Roll hit
                var hitChance = ComputeHitChance(attacker, tgt, intent.Move);
                var roll = _rng.NextDouble();
                var hit = roll <= hitChance;
                int damage = 0;
                if (hit)
                {
                    var details = ComputeDamageDetailed(attacker, tgt, intent.Move);
                    damage = details.Damage;
                    tgt.TakeDamage(damage);

                    // Effectiveness
                    if (details.TypeMultiplier >= 1.5)
                        messages.Add("C'est super efficace !");
                    else if (details.TypeMultiplier <= 0.5)
                        messages.Add("Ce n'est pas très efficace…");

                    if (details.IsCrit)
                        messages.Add("Coup critique !");

                    messages.Add($"{tgt.Name} subit {damage} dégâts.");
                }
                else
                {
                    messages.Add("L'attaque manque sa cible !");
                }
                return new TurnActionResult(attacker, tgt, intent.Move, hit, damage, tgt.Current.Hp <= 0)
                {
                    Messages = messages
                };
            }
            case ActionKind.UseItem:
            {
                // Apply item effect; no MP cost and no hit/damage
                var tgt = target ?? actor;
                intent.Item?.UseOn(tgt);
                return new TurnActionResult(actor, target, move: null, hit: false, damageDealt: 0, targetDeadAfter: tgt.Current.Hp <= 0)
                {
                    UsedItem = intent.Item,
                    Messages = new List<string> { $"{actor.Name} utilise {intent.Item?.Name}." }
                };
            }
            case ActionKind.Defend:
            {
                // No concrete effect yet; can be extended later
                return new TurnActionResult(actor, target, move: null, hit: false, damageDealt: 0, targetDeadAfter: target?.Current.Hp <= 0)
                {
                    Defended = true,
                    Messages = new List<string> { $"{actor.Name} se met en garde." }
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
                    Fled = success,
                    Messages = new List<string> { success ? $"{actor.Name} prend la fuite !" : $"{actor.Name} n'arrive pas à fuir !" }
                };
            }
            default:
                return new TurnActionResult(actor, target, intent.Move, false, 0, target?.Current.Hp <= 0)
                {
                    Messages = new List<string>()
                };
        }
    }
}

public readonly record struct DamageDetails(int Damage, bool IsCrit, double TypeMultiplier, double RandomFactor, bool HasStab);

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

    // New: messages describing what happened during this action
    public List<string> Messages { get; init; } = new();

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

    // New: end-of-turn messages (e.g., status DoT)
    public List<string> EndOfTurnMessages { get; set; } = new();

    public TurnResult(TurnActionResult? firstAction, TurnActionResult? secondAction, Character firstActor, Character secondActor)
    {
        FirstAction = firstAction;
        SecondAction = secondAction;
        FirstActor = firstActor;
        SecondActor = secondActor;
    }
}
