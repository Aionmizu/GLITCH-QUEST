using System.Text.Json.Serialization;

namespace Game.Core.Domain;

public sealed record SavePlayer(
    string Name,
    int Level,
    Element Type,
    Stats BaseStats,
    Stats Current,
    List<string> MoveIds,
    string Archetype
);

public sealed record SaveGame(
    string MapId,
    int PlayerX,
    int PlayerY,
    SavePlayer Player,
    List<string> InventoryItemIds,
    List<string> Keys,
    int RngSeed,
    DateTime SavedAtUtc
)
{
    [JsonIgnore]
    public string SlotId { get; init; } = "slot1"; // metadata not persisted by default
}
