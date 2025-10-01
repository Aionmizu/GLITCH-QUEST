using System;

namespace Game.Application.Menu;

public static class UiFormat
{
    /// <summary>
    /// Builds a simple ASCII progress bar string of fixed width using fill and empty characters.
    /// Values are clamped between 0 and max. When max is 0, returns all empty characters.
    /// </summary>
    public static string BuildBar(int current, int max, int width, char fillChar = '#', char emptyChar = '-')
    {
        if (width <= 0) return string.Empty;
        if (max <= 0) return new string(emptyChar, width);
        var cur = Math.Clamp(current, 0, max);
        var ratio = (double)cur / max;
        var filled = (int)Math.Round(ratio * width);
        if (filled < 0) filled = 0;
        if (filled > width) filled = width;
        return new string(fillChar, filled) + new string(emptyChar, width - filled);
    }
}
