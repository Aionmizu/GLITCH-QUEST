namespace Game.Core.Abstractions;

public enum InputKey
{
    None,
    Up,
    Down,
    Left,
    Right,
    Enter,
    Escape,
    Menu
}

public interface IInput
{
    /// <summary>
    /// Blocks until a supported key is pressed and returns the mapped InputKey.
    /// Unsupported keys return InputKey.None.
    /// </summary>
    InputKey ReadKey();
}
