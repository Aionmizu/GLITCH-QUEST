using Game.Core.Abstractions;
using Game.Core.Domain;

namespace Game.Application.Save;

/// <summary>
/// Orchestrates saving/loading using an ISaveRepository.
/// This keeps the Application layer free of concrete I/O.
/// </summary>
public sealed class SaveService
{
    private readonly ISaveRepository _repo;

    public SaveService(ISaveRepository repo)
    {
        _repo = repo;
    }

    public void Save(string slotId, SaveGame save) => _repo.Save(slotId, save);

    public SaveGame? Load(string slotId) => _repo.Load(slotId);

    public IEnumerable<string> ListSlots() => _repo.ListSlots();
}
