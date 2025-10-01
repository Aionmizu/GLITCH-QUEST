using System.Collections.Generic;
using Game.Core.Domain;

namespace Game.Core.Abstractions;

public interface ISaveRepository
{
    /// <summary>
    /// Persist the save game into a named slot. Implementations decide where/how to store it.
    /// </summary>
    void Save(string slotId, SaveGame save);

    /// <summary>
    /// Load a save game from a named slot. Returns null if the slot does not exist.
    /// </summary>
    SaveGame? Load(string slotId);

    /// <summary>
    /// List available slots known by the repository.
    /// </summary>
    IEnumerable<string> ListSlots();
}
