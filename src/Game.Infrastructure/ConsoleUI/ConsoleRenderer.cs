using System;
using System.Text;
using Game.Core.Abstractions;
using Game.Core.Domain;

namespace Game.Infrastructure.ConsoleUI;

public sealed class ConsoleRenderer : IRenderer
{
    public void Clear() => Console.Clear();

    public void DrawMap(Map map, int playerX, int playerY)
    {
        var sb = new StringBuilder();
        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                if (x == playerX && y == playerY)
                {
                    sb.Append('P');
                }
                else
                {
                    var tile = map[y, x];
                    // Render player spawn tiles as floor to avoid multiple persistent 'P' glyphs
                    if (tile.Type == TileType.PlayerSpawn)
                        sb.Append('.');
                    else
                        sb.Append(tile.Glyph);
                }
            }
            sb.AppendLine();
        }
        Console.SetCursorPosition(0, 0);
        Console.Write(sb.ToString());
    }

    public void DrawDialogue(params string[] lines)
    {
        // Simple dialogue: separate from map with a blank line
        Console.WriteLine();
        foreach (var l in lines)
            Console.WriteLine($"> {l}");
    }

    public void DrawBattleStatus(Character a, Character b)
    {
        DrawOne(a);
        DrawOne(b);

        static void DrawOne(Character c)
        {
            var hpBar = BuildBar(c.Current.Hp, c.BaseStats.Hp, 20);
            var mpBar = BuildBar(c.Current.Mp, c.BaseStats.Mp, 20, '=', '.');
            Console.Write($"{c.Name} HP ");
            using (new ScopedColor(ColorForHp(c)))
            {
                Console.Write(hpBar);
            }
            Console.Write($" {c.Current.Hp}/{c.BaseStats.Hp}  MP ");
            Console.Write(mpBar);
            Console.WriteLine($" {c.Current.Mp}/{c.BaseStats.Mp}");
        }

        static ConsoleColor ColorForHp(Character c)
        {
            var ratio = c.BaseStats.Hp <= 0 ? 0 : (double)c.Current.Hp / c.BaseStats.Hp;
            if (ratio > 0.5) return ConsoleColor.Green;
            if (ratio > 0.2) return ConsoleColor.Yellow;
            return ConsoleColor.Red;
        }

        static string BuildBar(int current, int max, int width, char fill = '#', char empty = '-')
        {
            if (width <= 0) return string.Empty;
            if (max <= 0) return new string(empty, width);
            var cur = Math.Clamp(current, 0, max);
            var ratio = (double)cur / max;
            var filled = (int)Math.Round(ratio * width);
            if (filled < 0) filled = 0;
            if (filled > width) filled = width;
            return new string(fill, filled) + new string(empty, width - filled);
        }
    }

    /// <summary>
    /// Draws a list of battle messages with a simple prefix.
    /// </summary>
    public void DrawMessages(IEnumerable<string> messages)
    {
        if (messages == null) return;
        foreach (var m in messages)
        {
            Console.WriteLine($"> {m}");
        }
    }

    private readonly struct ScopedColor : IDisposable
    {
        private readonly ConsoleColor _old;
        public ScopedColor(ConsoleColor color)
        {
            _old = Console.ForegroundColor;
            Console.ForegroundColor = color;
        }
        public void Dispose()
        {
            Console.ForegroundColor = _old;
        }
    }

    public void Present()
    {
        // In a buffered renderer we would flush; Console writes immediately, so nothing to do.
    }
}
