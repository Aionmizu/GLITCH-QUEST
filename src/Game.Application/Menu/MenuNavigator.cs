using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Abstractions;

namespace Game.Application.Menu;

/// <summary>
/// Handles keyboard navigation for a simple vertical menu made of MenuOption entries.
/// Pure application logic: depends only on IInput abstraction for key events.
/// </summary>
public sealed class MenuNavigator
{
    /// <summary>
    /// Navigate the given options using the provided input until the user selects an enabled item with Enter,
    /// or cancels with Escape. Returns the selected MenuOption, or null on cancel/no selectable entries.
    /// Up/Down wrap around and skip disabled options.
    /// </summary>
    public MenuOption? Navigate(IInput input, IReadOnlyList<MenuOption> options, int startIndex = 0)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (options.Count == 0) return null;

        // If all are disabled, nothing to select
        if (!options.Any(o => o.Enabled)) return null;

        int index = Math.Clamp(startIndex, 0, options.Count - 1);
        index = NextEnabledIndex(options, index, forward: true);

        while (true)
        {
            var key = input.ReadKey();
            switch (key)
            {
                case InputKey.Up:
                    index = PrevEnabledIndex(options, index);
                    break;
                case InputKey.Down:
                    index = NextEnabledIndex(options, index);
                    break;
                case InputKey.Enter:
                    if (options[index].Enabled)
                        return options[index];
                    // If somehow disabled (race), move to next enabled
                    index = NextEnabledIndex(options, index);
                    break;
                case InputKey.Escape:
                    return null;
                default:
                    // ignore other keys
                    break;
            }
        }
    }

    private static int NextEnabledIndex(IReadOnlyList<MenuOption> options, int from, bool forward = true)
    {
        int i = from;
        for (int n = 0; n < options.Count; n++)
        {
            i = (i + 1) % options.Count;
            if (options[i].Enabled) return i;
        }
        return from; // fallback (should not happen; all-disabled handled earlier)
    }

    private static int PrevEnabledIndex(IReadOnlyList<MenuOption> options, int from)
    {
        int i = from;
        for (int n = 0; n < options.Count; n++)
        {
            i = (i - 1 + options.Count) % options.Count;
            if (options[i].Enabled) return i;
        }
        return from;
    }
}
