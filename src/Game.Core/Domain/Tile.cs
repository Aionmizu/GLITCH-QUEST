namespace Game.Core.Domain;

public enum TileType
{
    Wall,
    Floor,
    Grass,
    Corruption,
    Chest,
    Npc,
    Door,
    PlayerSpawn
}

public sealed class Tile
{
    public TileType Type { get; }
    public char Glyph { get; }
    public bool Walkable { get; }

    public Tile(TileType type, char glyph, bool walkable)
    {
        Type = type;
        Glyph = glyph;
        Walkable = walkable;
    }

    public static Tile FromGlyph(char ch)
    {
        return ch switch
        {
            '#' => new Tile(TileType.Wall, '#', false),
            '.' => new Tile(TileType.Floor, '.', true),
            '~' => new Tile(TileType.Grass, '~', true),
            '§' => new Tile(TileType.Chest, '§', true),
            '@' => new Tile(TileType.Npc, '@', true),
            '+' => new Tile(TileType.Door, '+', true),
            'P' => new Tile(TileType.PlayerSpawn, 'P', true),
            // Treat unknown glyphs as floor to be permissive during early content creation
            _ => new Tile(TileType.Floor, ch, true)
        };
    }
}