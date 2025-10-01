using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Abstractions;
using Game.Core.Battle;
using Game.Core.Domain;
using Game.Core.Domain.Items;

namespace Game.Application.Battle;

/// <summary>
/// A simple deterministic AI:
/// - If actor HP ≤ 30% and a healing item is available and usable, use the strongest healing item on self.
/// - Else select the most effective affordable move against the target based on TypeChart and STAB.
/// - If no move is affordable, Defend.
/// </summary>
public sealed class SimpleAiStrategy : IAiStrategy
{
    private readonly List<Item> _items;

    public SimpleAiStrategy(IEnumerable<Item>? items = null)
    {
        _items = items?.ToList() ?? new List<Item>();
    }

    public ActionIntent ChooseAction(BattleContext context)
    {
        // This strategy is intended for the Enemy side
        var actor = context.Enemy;
        var target = context.Player;

        // 1) Low HP -> try to use a healing item
        var hpRatio = actor.BaseStats.Hp == 0 ? 0 : (double)actor.Current.Hp / actor.BaseStats.Hp;
        if (hpRatio <= 0.30)
        {
            var usableHeals = _items
                .Where(i => i.Effect is HealHpEffect && i.CanUseOn(actor))
                .Cast<Item>()
                .ToList();
            if (usableHeals.Count > 0)
            {
                // Prefer the largest heal first
                var best = usableHeals
                    .OrderByDescending(i => ((HealHpEffect)i.Effect!).Amount)
                    .First();
                return ActionIntent.UseItem(actor, actor, best);
            }
        }

        // 2) Choose best affordable move by multiplier and STAB
        var candidates = actor.Moves
            .Where(m => m.MpCost <= actor.Current.Mp)
            .Select(m => new
            {
                Move = m,
                Score = ComputeMoveScore(context.TypeChart, actor, target, m)
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        if (candidates.Count > 0)
        {
            var best = candidates.First().Move;
            return ActionIntent.Attack(actor, target, best);
        }

        // 3) Nothing affordable -> Defend
        return ActionIntent.Defend(actor);
    }

    private static double ComputeMoveScore(ITypeChart chart, Character actor, Character target, Move m)
    {
        var typeMul = chart.GetMultiplier(m.Type, target.Type);
        var stab = m.Type == actor.Type ? 1.2 : 1.0;
        // A simple heuristic: power * type * stab. Could include accuracy, but we keep it simple and deterministic.
        return m.Power * typeMul * stab;
    }
}
