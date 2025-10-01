using System;
using System.Collections.Generic;
using FluentAssertions;
using Game.Application.GameState;
using Game.Application.Save;
using Game.Core.Abstractions;
using Game.Core.Domain;
using Xunit;

namespace Game.Tests;

public class GameFlowIntegrationTests
{
    private sealed class InMemoryRepo : ISaveRepository
    {
        private readonly Dictionary<string, SaveGame> _store = new();
        public void Save(string slotId, SaveGame save) => _store[slotId] = save with { SlotId = slotId };
        public SaveGame? Load(string slotId) => _store.TryGetValue(slotId, out var s) ? s : null;
        public IEnumerable<string> ListSlots() => _store.Keys;
    }

    private static SaveGame SampleSave() => new SaveGame(
        MapId: "parc",
        PlayerX: 1,
        PlayerY: 1,
        Player: new SavePlayer(
            Name: "Hero",
            Level: 1,
            Type: Element.Normal,
            BaseStats: new Stats(20, 10, 5, 5, 5, 1.0, 1.0),
            Current: new Stats(20, 10, 5, 5, 5, 1.0, 1.0),
            MoveIds: new List<string> { "tackle" },
            Archetype: Archetype.Balanced.ToString()
        ),
        InventoryItemIds: new List<string>(),
        Keys: new List<string>(),
        RngSeed: 1,
        SavedAtUtc: DateTime.UnixEpoch
    );

    [Fact]
    public void NewGame_transitions_to_exploration()
    {
        var repo = new InMemoryRepo();
        var saveSvc = new SaveService(repo);
        var sm = new GameStateMachine();
        var flow = new GameFlow(sm, saveSvc);

        sm.State.Should().Be(GameState.MainMenu);
        flow.NewGame(SampleSave());
        sm.State.Should().Be(GameState.Exploration);
    }

    [Fact]
    public void LoadGame_transitions_to_exploration_and_returns_payload()
    {
        var repo = new InMemoryRepo();
        var saveSvc = new SaveService(repo);
        var sm = new GameStateMachine();
        var flow = new GameFlow(sm, saveSvc);

        // preload slot
        var payload = SampleSave();
        saveSvc.Save("slot1", payload);

        sm.State.Should().Be(GameState.MainMenu);
        var loaded = flow.LoadGame("slot1");
        loaded.Should().NotBeNull();
        loaded!.Player.Name.Should().Be("Hero");
        sm.State.Should().Be(GameState.Exploration);
    }

    [Fact]
    public void Save_requires_exploration_state_and_persists_data()
    {
        var repo = new InMemoryRepo();
        var saveSvc = new SaveService(repo);
        var sm = new GameStateMachine();
        var flow = new GameFlow(sm, saveSvc);
        var payload = SampleSave();

        // Not in exploration -> throws
        Action act = () => flow.SaveFromExploration("slotX", payload);
        act.Should().Throw<InvalidOperationException>();

        // Enter exploration and save
        flow.NewGame(payload);
        flow.SaveFromExploration("slotX", payload);
        repo.Load("slotX").Should().NotBeNull();

        // If we leave exploration (e.g., enter battle), saving should be forbidden
        sm.EnterBattle();
        Action act2 = () => flow.SaveFromExploration("slotX", payload);
        act2.Should().Throw<InvalidOperationException>();
    }
}
