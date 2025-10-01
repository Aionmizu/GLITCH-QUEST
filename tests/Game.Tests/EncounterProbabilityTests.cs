using System;
using System.Collections.Generic;
using FluentAssertions;
using Game.Application.Exploration;
using Game.Core.Abstractions;
using Game.Core.Domain;
using Xunit;

namespace Game.Tests;

public sealed class EncounterProbabilityTests
{
    private sealed class CycleRandom : IRandom
    {
        private int _i;
        public double NextDouble()
        {
            // Produce values 0.00, 0.01, 0.02, ..., 0.99, then repeat
            var v = (_i % 100) / 100.0;
            _i++;
            return v;
        }
        public double NextDouble(double minInclusive, double maxInclusive)
        {
            var unit = NextDouble();
            return minInclusive + unit * (maxInclusive - minInclusive);
        }
    }

    [Fact]
    public void Grass_tiles_trigger_encounter_around_12_percent_on_average()
    {
        var rng = new CycleRandom();
        var svc = new ExplorationService(rng);
        int N = 10_000;
        int hits = 0;
        for (int i = 0; i < N; i++)
        {
            if (svc.ShouldTriggerEncounter(TileType.Grass)) hits++;
        }
        var ratio = hits / (double)N;
        ratio.Should().BeApproximately(0.12, 1e-9);
    }

    [Fact]
    public void Non_grass_corruption_tiles_do_not_trigger()
    {
        var rng = new CycleRandom();
        var svc = new ExplorationService(rng);
        for (int i = 0; i < 500; i++)
        {
            svc.ShouldTriggerEncounter(TileType.Floor).Should().BeFalse();
            svc.ShouldTriggerEncounter(TileType.Wall).Should().BeFalse();
            svc.ShouldTriggerEncounter(TileType.Door).Should().BeFalse();
        }
    }

    [Fact]
    public void Corruption_tiles_trigger_like_grass()
    {
        var rng = new CycleRandom();
        var svc = new ExplorationService(rng);
        int N = 10_000;
        int hits = 0;
        for (int i = 0; i < N; i++)
        {
            if (svc.ShouldTriggerEncounter(TileType.Corruption)) hits++;
        }
        var ratio = hits / (double)N;
        ratio.Should().BeApproximately(0.12, 1e-9);
    }
}