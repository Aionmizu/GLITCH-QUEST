using Game.Core.Abstractions;
using Game.Core.Domain;

namespace Game.Core.Battle;

public sealed class BattleContext
{
    public Character Player { get; }
    public Character Enemy { get; }
    public IRandom Rng { get; }
    public ITypeChart TypeChart { get; }

    public BattleContext(Character player, Character enemy, IRandom rng, ITypeChart typeChart)
    {
        Player = player;
        Enemy = enemy;
        Rng = rng;
        TypeChart = typeChart;
    }
}