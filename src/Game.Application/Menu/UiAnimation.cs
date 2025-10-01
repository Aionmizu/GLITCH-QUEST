using System;
using System.Collections.Generic;

namespace Game.Application.Menu;

public static class UiAnimation
{
    /// <summary>
    /// Builds intermediate frames to animate a progress bar from a previous value to a current value.
    /// Frames are inclusive of both start and end values. Steps must be >= 1.
    /// Uses UiFormat.BuildBar semantics (but reuses only basic math to avoid coupling in tests).
    /// </summary>
    public static IReadOnlyList<string> BuildBarFrames(int previous, int current, int max, int width, int steps, char fillChar = '#', char emptyChar = '-')
    {
        if (steps < 1) throw new ArgumentOutOfRangeException(nameof(steps), "steps must be >= 1");
        if (width <= 0) return Array.Empty<string>();
        if (max <= 0)
        {
            // All empty frames
            var empty = new string(emptyChar, width);
            var frames = new List<string>(steps + 1);
            for (int i = 0; i <= steps; i++) frames.Add(empty);
            return frames;
        }

        var start = Math.Clamp(previous, 0, max);
        var end = Math.Clamp(current, 0, max);
        var framesOut = new List<string>(steps + 1);
        for (int i = 0; i <= steps; i++)
        {
            // Linear interpolation between start and end
            var t = (double)i / steps;
            var v = (int)Math.Round(start + (end - start) * t);
            framesOut.Add(BuildBar(v, max, width, fillChar, emptyChar));
        }
        return framesOut;
    }

    private static string BuildBar(int cur, int max, int width, char fillChar, char emptyChar)
    {
        var ratio = (double)cur / max;
        var filled = (int)Math.Round(ratio * width);
        if (filled < 0) filled = 0;
        if (filled > width) filled = width;
        return new string(fillChar, filled) + new string(emptyChar, width - filled);
    }
}
