using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Game.Core.Domain;
using Xunit;

namespace Game.Tests;

public class SerializationTests
{
    [Fact]
    public void SaveGame_round_trip_JSON_should_preserve_data()
    {
        var save = new SaveGame(
            MapId: "parc",
            PlayerX: 5,
            PlayerY: 7,
            Player: new SavePlayer(
                Name: "Hero",
                Level: 3,
                Type: Element.Fire,
                BaseStats: new Stats(28, 14, 9, 9, 7, 1.0, 1.0),
                Current: new Stats(28, 14, 9, 9, 7, 1.0, 1.0),
                MoveIds: new List<string> { "ember", "tackle" },
                Archetype: Archetype.Balanced.ToString()
            ),
            InventoryItemIds: new List<string> { "potion_hp", "potion_mp" },
            Keys: new List<string> { "key_parc" },
            RngSeed: 12345,
            SavedAtUtc: DateTime.UnixEpoch
        )
        {
            // Not serialized by default due to [JsonIgnore], but we can still carry it in memory
            SlotId = "slot2"
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var json = JsonSerializer.Serialize(save, options);
        var clone = JsonSerializer.Deserialize<SaveGame>(json, options)!;

        // SlotId is ignored in JSON; default value should be used on deserialization
        clone.SlotId.Should().Be("slot1");

        // Use structural equivalence because collections are re-instantiated on deserialize
        clone.Should().BeEquivalentTo(save, options => options
            .Excluding(sg => sg.SlotId));
    }
}
