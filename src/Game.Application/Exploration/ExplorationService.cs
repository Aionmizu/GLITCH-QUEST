using System.Linq;
using Game.Core.Abstractions;
using Game.Core.Domain;

namespace Game.Application.Exploration;

public sealed class ExplorationService
{
    private readonly IRandom _rng;

    public ExplorationService(IRandom rng)
    {
        _rng = rng;
    }

    /// <summary>
    /// Returns true if a random encounter should trigger on this tile according to design (12% on Grass/Corruption).
    /// </summary>
    public bool ShouldTriggerEncounter(TileType tileType)
    {
        if (tileType != TileType.Grass && tileType != TileType.Corruption)
            return false;
        var roll = _rng.NextDouble();
        return roll < 0.12;
    }

    /// <summary>
    /// Attempt to move the player by (dx,dy). Returns true if moved, false if blocked by walls/bounds.
    /// </summary>
    public bool TryMove(ExplorationState state, int dx, int dy)
    {
        var nx = state.PlayerX + dx;
        var ny = state.PlayerY + dy;
        if (ny < 0 || ny >= state.Map.Height || nx < 0 || nx >= state.Map.Width)
            return false;
        var tile = state.Map[ny, nx];
        if (!tile.Walkable) return false;
        state.SetPlayerPos(nx, ny);
        return true;
    }

    /// <summary>
    /// Select a random encounter from the current map's encounter table using weighted probabilities.
    /// Returns null if the map has no encounter table configured.
    /// </summary>
    public (string EnemyId, int Level)? PickRandomEncounter(Map map)
    {
        if (map.Encounters is null) return null;
        return map.Encounters.Roll(_rng);
    }

    /// <summary>
    /// Helper to create a Map from ASCII lines. File IO belongs to infrastructure; this keeps domain/Application pure.
    /// </summary>
    public Map LoadMapFromAscii(IEnumerable<string> lines)
    {
        var list = lines.ToList();
        return Map.FromAsciiLines(list);
    }
}