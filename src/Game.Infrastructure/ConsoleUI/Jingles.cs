using System;

namespace Game.Infrastructure.ConsoleUI;

public static class Jingles
{
    /// <summary>
    /// Plays a simple victory jingle using Console.Beep. Safe to call on platforms that support it.
    /// </summary>
    public static void PlayVictory()
    {
        try
        {
            // Simple ascending triad
            Console.Beep(440, 120);
            Console.Beep(554, 120);
            Console.Beep(659, 200);
        }
        catch
        {
            // Some environments may not support Console.Beep; ignore errors.
        }
    }
}
