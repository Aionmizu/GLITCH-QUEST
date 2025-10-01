using Game.Core.Battle;

namespace Game.Core.Abstractions;

public interface IAiStrategy
{
    ActionIntent ChooseAction(BattleContext context);
}