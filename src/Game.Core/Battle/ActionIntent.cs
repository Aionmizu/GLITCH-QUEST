using Game.Core.Domain;
using Game.Core.Domain.Items;

namespace Game.Core.Battle;

public enum ActionKind
{
    Attack,
    Defend,
    UseItem,
    Flee
}

public sealed class ActionIntent
{
    public ActionKind Kind { get; }
    public Character Actor { get; }
    public Character? Target { get; }
    public Move? Move { get; }
    public Item? Item { get; }

    private ActionIntent(ActionKind kind, Character actor, Character? target = null, Move? move = null, Item? item = null)
    {
        Kind = kind;
        Actor = actor;
        Target = target;
        Move = move;
        Item = item;
    }

    public static ActionIntent Attack(Character actor, Character target, Move move)
        => new(ActionKind.Attack, actor, target, move);

    public static ActionIntent UseItem(Character actor, Character target, Item item)
        => new(ActionKind.UseItem, actor, target, item: item);

    public static ActionIntent Defend(Character actor)
        => new(ActionKind.Defend, actor);

    public static ActionIntent Flee(Character actor)
        => new(ActionKind.Flee, actor);
}