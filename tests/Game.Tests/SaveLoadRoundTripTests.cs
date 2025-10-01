using System;
using System.IO;
using FluentAssertions;
using Game.Application.Save;
using Game.Core.Domain;
using Game.Core.Domain.Items;
using Game.Infrastructure.Persistence;
using Xunit;

namespace Game.Tests;

public class SaveLoadRoundTripTests
{
    [Fact]
    public void SaveLoad_RoundTrip_PreservesMapInventoryKeysAndPlayer()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "glitchquest_tests_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        try
        {
            var repo = new JsonSaveRepository(tempDir);
            var svc = new SaveService(repo);

            var player = new SavePlayer(
                Name: "Hero",
                Level: 3,
                Type: Element.Fire,
                BaseStats: new Stats(40, 15, 8, 6, 6, 1.0, 1.0),
                Current: new Stats(28, 7, 8, 6, 6, 1.0, 1.0),
                MoveIds: new() { "tackle", "ember" },
                Archetype: Archetype.Balanced.ToString()
            );

            var save = new SaveGame(
                MapId: "parc",
                PlayerX: 5,
                PlayerY: 7,
                Player: player,
                InventoryItemIds: new() { new PotionHP(20).Id, new PotionMP(10).Id },
                Keys: new() { Progression.KeyParc },
                RngSeed: 12345,
                SavedAtUtc: DateTime.UtcNow
            ) { SlotId = "slot2" };

            svc.Save("slot2", save);

            var loaded = svc.Load("slot2");
            loaded.Should().NotBeNull();
            loaded!.MapId.Should().Be("parc");
            loaded.PlayerX.Should().Be(5);
            loaded.PlayerY.Should().Be(7);
            loaded.InventoryItemIds.Should().Contain(new PotionHP(20).Id);
            loaded.InventoryItemIds.Should().Contain(new PotionMP(10).Id);
            loaded.Keys.Should().Contain(Progression.KeyParc);
            loaded.Player.Name.Should().Be("Hero");
            loaded.Player.MoveIds.Should().Contain(new[] { "tackle", "ember" });
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* ignore */ }
        }
    }
}
