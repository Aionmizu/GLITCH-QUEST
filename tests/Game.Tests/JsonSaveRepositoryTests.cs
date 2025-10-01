using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Game.Core.Domain;
using Game.Infrastructure.Persistence;
using Xunit;

namespace Game.Tests;

public class JsonSaveRepositoryTests
{
    private static string MakeTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "GlitchQuestTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static SaveGame MakeSampleSave()
    {
        return new SaveGame(
            MapId: "parc",
            PlayerX: 2,
            PlayerY: 3,
            Player: new SavePlayer(
                Name: "Hero",
                Level: 2,
                Type: Element.Fire,
                BaseStats: new Stats(24, 12, 7, 7, 6, 1.0, 1.0),
                Current: new Stats(22, 10, 7, 7, 6, 1.0, 1.0),
                MoveIds: new List<string> { "ember", "tackle" },
                Archetype: Archetype.Balanced.ToString()
            ),
            InventoryItemIds: new List<string> { "potion_hp" },
            Keys: new List<string> { "key_parc" },
            RngSeed: 42,
            SavedAtUtc: DateTime.UnixEpoch
        );
    }

    [Fact]
    public void Save_and_Load_round_trip_with_slot_metadata_and_list_slots()
    {
        var dir = MakeTempDir();
        try
        {
            var repo = new JsonSaveRepository(dir, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            var save = MakeSampleSave();

            repo.Save("slotA", save);

            // Verify file exists and slots are listed
            repo.ListSlots().Should().Contain("slotA");

            var loaded = repo.Load("slotA")!;
            loaded.SlotId.Should().Be("slotA");

            // Structural comparison excluding SlotId (metadata)
            loaded.Should().BeEquivalentTo(save, opt => opt.Excluding(sg => sg.SlotId));
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void Load_missing_slot_returns_null()
    {
        var dir = MakeTempDir();
        try
        {
            var repo = new JsonSaveRepository(dir);
            repo.Load("does_not_exist").Should().BeNull();
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }
}
