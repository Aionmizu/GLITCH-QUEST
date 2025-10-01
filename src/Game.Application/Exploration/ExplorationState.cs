using Game.Core.Domain;

namespace Game.Application.Exploration;

public sealed class ExplorationState
{
    public Map Map { get; }
    public int PlayerX { get; private set; }
    public int PlayerY { get; private set; }

    public ExplorationState(Map map, int? startX = null, int? startY = null)
    {
        Map = map;
        if (map.PlayerSpawn is { } spawn && startX is null && startY is null)
        {
            PlayerX = spawn.X;
            PlayerY = spawn.Y;
        }
        else
        {
            PlayerX = startX ?? 0;
            PlayerY = startY ?? 0;
        }
    }

    public (int X, int Y) PlayerPos => (PlayerX, PlayerY);

    public void SetPlayerPos(int x, int y)
    {
        PlayerX = x;
        PlayerY = y;
    }
}