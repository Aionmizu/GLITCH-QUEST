using System;
using System.Collections.Generic;
using Game.Application.Save;
using Game.Core.Domain;

namespace Game.Application.GameState;

/// <summary>
/// High-level orchestration bridging GameStateMachine and SaveService.
/// This class keeps I/O out (no console), offering pure operations that can be unit-tested.
/// </summary>
public sealed class GameFlow
{
    private readonly GameStateMachine _sm;
    private readonly SaveService _saveService;

    public GameFlow(GameStateMachine stateMachine, SaveService saveService)
    {
        _sm = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
    }

    /// <summary>
    /// Start a new game session and transition to Exploration state.
    /// The provided initial save payload can be used by the caller to seed runtime state.
    /// </summary>
    public void NewGame(SaveGame initial)
    {
        if (initial is null) throw new ArgumentNullException(nameof(initial));
        _sm.StartNewGame();
    }

    /// <summary>
    /// Load an existing game from a slot. If successful, transitions to Exploration state and returns the save.
    /// Returns null if the slot does not exist.
    /// </summary>
    public SaveGame? LoadGame(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId)) throw new ArgumentException("slotId is required", nameof(slotId));
        var save = _saveService.Load(slotId);
        if (save is not null)
        {
            // Enter the playable state
            _sm.StartNewGame();
        }
        return save;
    }

    /// <summary>
    /// Save from Exploration state into the given slot. Throws InvalidOperationException if not in Exploration.
    /// </summary>
    public void SaveFromExploration(string slotId, SaveGame payload)
    {
        if (_sm.State != GameState.Exploration)
            throw new InvalidOperationException("Cannot save when not in Exploration state.");
        _saveService.Save(slotId, payload);
    }

    public IEnumerable<string> ListSlots() => _saveService.ListSlots();
}
