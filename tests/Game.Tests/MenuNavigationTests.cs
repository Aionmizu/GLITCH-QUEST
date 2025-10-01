using System.Collections.Generic;
using FluentAssertions;
using Game.Application.Menu;
using Game.Core.Abstractions;
using Xunit;

namespace Game.Tests;

public class MenuNavigationTests
{
    private sealed class ScriptedInput : IInput
    {
        private readonly Queue<InputKey> _keys;
        public ScriptedInput(IEnumerable<InputKey> keys) { _keys = new Queue<InputKey>(keys); }
        public InputKey ReadKey() => _keys.Count == 0 ? InputKey.None : _keys.Dequeue();
    }

    [Fact]
    public void Down_wrapping_and_skip_disabled_then_enter_selects()
    {
        var options = new List<MenuOption>
        {
            new("Attaque", true, BattleRootChoice.Attaque),
            new("Magie", true, BattleRootChoice.Magie),
            new("Objet", false, BattleRootChoice.Objet),
            new("Fuite", true, BattleRootChoice.Fuite),
        };
        var input = new ScriptedInput(new[] { InputKey.Down, InputKey.Enter });
        var nav = new MenuNavigator();
        var selected = nav.Navigate(input, options, startIndex: 0);
        // Move from index 0 -> 1 (Magie), then Down -> 2 (disabled, skip) -> 3 (Fuite), then Enter
        selected.Should().NotBeNull();
        selected!.Label.Should().Be("Fuite");
        selected.Tag.Should().Be(BattleRootChoice.Fuite);
    }

    [Fact]
    public void Up_wraps_to_last_enabled_and_escape_cancels()
    {
        var options = new List<MenuOption>
        {
            new("A", false, 1),
            new("B", true, 2),
            new("C", false, 3),
            new("D", true, 4),
        };
        // Up from start index 1 (B) should wrap to last enabled (D), but we then hit Escape to cancel
        var input = new ScriptedInput(new[] { InputKey.Up, InputKey.Escape });
        var nav = new MenuNavigator();
        var selected = nav.Navigate(input, options, startIndex: 1);
        selected.Should().BeNull();
    }

    [Fact]
    public void Returns_null_when_all_options_disabled()
    {
        var options = new List<MenuOption>
        {
            new("A", false), new("B", false)
        };
        var input = new ScriptedInput(new[] { InputKey.Enter });
        var nav = new MenuNavigator();
        var selected = nav.Navigate(input, options);
        selected.Should().BeNull();
    }
}
