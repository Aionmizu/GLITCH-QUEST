using System;
using System.Collections.Generic;
using FluentAssertions;
using Game.Application.Exploration;
using Game.Core.Abstractions;
using Game.Core.Domain;
using Xunit;

namespace Game.Tests;

public class EncounterWeightedSelectionTests
{
    private sealed class StepRandom : IRandom
    {
        private int _i;
        private readonly int _n;
        public StepRandom(int n = 10000) { _n = n; _i = 0; }
        public double NextDouble()
        {
            var v = ((double)(_i % _n)) / _n;
            _i++;
            return v;
        }
        public double NextDouble(double minInclusive, double maxInclusive)
        {
            var u = NextDouble();
            return minInclusive + u * (maxInclusive - minInclusive);
        }
    }

    [Fact]
    public void Weighted_roll_prefers_higher_weight_entries_over_many_trials()
    {
        var rng = new StepRandom(10000);
        var table = new EncounterTable(new[]
        {
            new EncounterEntry("slime", 1, 2, 1),
            new EncounterEntry("wolf", 2, 3, 3)
        });

        var counts = new Dictionary<string, int> { ["slime"] = 0, ["wolf"] = 0 };
        for (int i = 0; i < 10000; i++)
        {
            var (id, lvl) = table.Roll(rng);
            counts[id]++;
            // Validate level bounds
            if (id == "slime") lvl.Should().BeInRange(1, 2);
            if (id == "wolf") lvl.Should().BeInRange(2, 3);
        }

        // With weights 1 vs 3, wolf should appear significantly more often
        counts["wolf"].Should().BeGreaterThan(counts["slime"]);

        // And roughly around the 3:1 ratio within a tolerance window
        var ratio = (double)counts["wolf"] / Math.Max(1, counts["slime"]);
        ratio.Should().BeGreaterThan(2.5);
        ratio.Should().BeLessThan(3.5);
    }

    [Fact]
    public void ExplorationService_can_pick_encounter_from_map_table()
    {
        var rng = new StepRandom(10);
        var svc = new ExplorationService(rng);
        var map = Map.FromAsciiLines(new[]
        {
            "#####",
            "#P..#",
            "#####"
        });
        // Attach an encounter table to the map
        map = new Map(map.Tiles, map.PlayerSpawn, new EncounterTable(new[]
        {
            new EncounterEntry("bugling", 2, 4, 1)
        }));

        var picked = svc.PickRandomEncounter(map);
        picked.Should().NotBeNull();
        picked?.EnemyId.Should().Be("bugling");
        picked?.Level.Should().BeInRange(2, 4);
    }
}
