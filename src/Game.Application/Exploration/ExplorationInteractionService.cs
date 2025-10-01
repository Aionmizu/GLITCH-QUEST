using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Domain.Items;

namespace Game.Application.Exploration;

/// <summary>
/// Provides interaction logic for exploration layer (NPC dialogue, chests, doors).
/// Pure Application logic: no I/O here. Changes the map tiles accordingly when interactions succeed.
/// </summary>
public sealed class ExplorationInteractionService
{
    /// <summary>
    /// If the player currently stands on an NPC tile, returns true and outputs the provided message.
    /// Otherwise, returns false.
    /// </summary>
    public bool TryTalkToNpcAtPlayer(ExplorationState state, out string message, string npcMessage = "...")
    {
        var (x, y) = state.PlayerPos;
        var tile = state.Map[y, x];
        // Talk if standing on the NPC tile
        if (tile.Type == TileType.Npc)
        {
            message = npcMessage;
            return true;
        }
        message = string.Empty;
        return false;
    }

    /// <summary>
    /// If the player currently stands on a Chest tile, converts it to Floor and adds the given item to inventory.
    /// Returns true if an item was taken, false otherwise.
    /// </summary>
    public bool TryOpenChestAtPlayer(ExplorationState state, Inventory inventory, Item item)
    {
        var (x, y) = state.PlayerPos;
        // Check current tile and 4-neighbour adjacency for a chest
        var candidates = new List<(int X,int Y)> { (x, y), (x+1,y), (x-1,y), (x,y+1), (x,y-1) };
        foreach (var (cx, cy) in candidates)
        {
            if (cy < 0 || cy >= state.Map.Height || cx < 0 || cx >= state.Map.Width) continue;
            var t = state.Map[cy, cx];
            if (t.Type == TileType.Chest)
            {
                // Convert chest to floor to indicate it's been opened
                state.Map.Tiles[cy, cx] = new Tile(TileType.Floor, '.', true);
                // Add item to inventory
                inventory.Add(item);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// If the player currently stands on a Door tile and has the required key, opens the door (turns to Floor).
    /// Returns true if opened, false if not on a door or key missing.
    /// </summary>
    public bool TryOpenDoorAtPlayer(ExplorationState state, Inventory inventory, string requiredKeyId)
    {
        var (x, y) = state.PlayerPos;
        if (!inventory.HasKey(requiredKeyId))
            return false;

        // Check current tile and 4-neighbour adjacency for a door
        var candidates = new List<(int X,int Y)> { (x, y), (x+1,y), (x-1,y), (x,y+1), (x,y-1) };
        foreach (var (cx, cy) in candidates)
        {
            if (cy < 0 || cy >= state.Map.Height || cx < 0 || cx >= state.Map.Width) continue;
            var t = state.Map[cy, cx];
            if (t.Type == TileType.Door)
            {
                state.Map.Tiles[cy, cx] = new Tile(TileType.Floor, '.', true);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Opens the final door if and only if the player has all three zone keys (Parc, Laboratoire, Noyau).
    /// Returns true if opened; false otherwise.
    /// </summary>
    public bool TryOpenFinalDoorAtPlayer(ExplorationState state, Inventory inventory)
    {
        var (x, y) = state.PlayerPos;
        if (!Progression.HasAllZoneKeys(inventory))
            return false;

        // Check current tile and 4-neighbour adjacency for a final door
        var candidates = new List<(int X,int Y)> { (x, y), (x+1,y), (x-1,y), (x,y+1), (x,y-1) };
        foreach (var (cx, cy) in candidates)
        {
            if (cy < 0 || cy >= state.Map.Height || cx < 0 || cx >= state.Map.Width) continue;
            var t = state.Map[cy, cx];
            if (t.Type == TileType.Door)
            {
                state.Map.Tiles[cy, cx] = new Tile(TileType.Floor, '.', true);
                return true;
            }
        }
        return false;
    }
}
