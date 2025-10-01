using System.Linq;

namespace Game.Core.Domain;

public sealed class Map
{
    public int Width { get; }
    public int Height { get; }
    public Tile[,] Tiles { get; }
    public (int X, int Y)? PlayerSpawn { get; }
    public EncounterTable? Encounters { get; init; }

    public Map(Tile[,] tiles, (int X, int Y)? playerSpawn, EncounterTable? encounters = null)
    {
        Tiles = tiles;
        Height = tiles.GetLength(0);
        Width = tiles.GetLength(1);
        PlayerSpawn = playerSpawn;
        Encounters = encounters;
    }

    public Tile this[int y, int x] => Tiles[y, x];

    public static Map FromAsciiLines(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
            throw new ArgumentException("Map must have at least one line", nameof(lines));

        var height = lines.Count;
        var width = lines.Max(l => l.Length);
        if (width == 0)
            throw new ArgumentException("Map lines must not be empty", nameof(lines));

        var tiles = new Tile[height, width];
        (int X, int Y)? spawn = null;

        for (var y = 0; y < height; y++)
        {
            var line = lines[y];
            for (var x = 0; x < width; x++)
            {
                char ch = x < line.Length ? line[x] : '.'; // pad shorter lines with floor
                var tile = Tile.FromGlyph(ch);
                tiles[y, x] = tile;
                if (ch == 'P')
                    spawn = (x, y);
            }
        }
        return new Map(tiles, spawn);
    }
}