using System;
using Game.Core.Abstractions;

namespace Game.Infrastructure.ConsoleUI;

public sealed class ConsoleInput : IInput
{
    public InputKey ReadKey()
    {
        var key = Console.ReadKey(intercept: true);
        return key.Key switch
        {
            ConsoleKey.UpArrow => InputKey.Up,
            ConsoleKey.DownArrow => InputKey.Down,
            ConsoleKey.LeftArrow => InputKey.Left,
            ConsoleKey.RightArrow => InputKey.Right,
            ConsoleKey.Enter => InputKey.Enter,
            ConsoleKey.Escape => InputKey.Escape,
            ConsoleKey.M => InputKey.Menu,
            _ => InputKey.None
        };
    }
}
