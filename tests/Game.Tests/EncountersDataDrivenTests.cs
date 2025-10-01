using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Game.Core.Abstractions;
using Game.Core.Domain;
using Game.Infrastructure.Data;
using Xunit;

namespace Game.Tests;

public class EncountersDataDrivenTests
{
    [Fact]
    public void WeightedSelection_ApproximateDistribution_OnLargeSamples()
    {
        var rng = new SystemRandomWrapper(new Random(123));
        var table = new EncounterTable(new[]
        {
            new EncounterEntry("A", 1, 1, 1),
            new EncounterEntry("B", 1, 1, 3)
        });

        int n = 10_000;
        int a = 0, b = 0;
        for (int i = 0; i < n; i++)
        {
            var pick = table.Roll(rng);
            if (pick.EnemyId == "A") a++; else if (pick.EnemyId == "B") b++; else throw new Exception("Unexpected id");
        }

        var ratioA = (double)a / n;
        var ratioB = (double)b / n;
        // Expected 0.25 and 0.75 with tolerance
        ratioA.Should().BeApproximately(0.25, 0.03);
        ratioB.Should().BeApproximately(0.75, 0.03);
    }

    [Fact]
    public void EnemyInstantiation_MapsMovesById_FromDataFiles()
    {
        // Locate repo root where /data exists
        var root = FindDataRoot();
        root.Should().NotBeNullOrEmpty();
        var loader = new FileDataLoader(root!);
        var moveCatalog = loader.LoadMoves();
        var enemyCatalog = loader.LoadEnemies();

        enemyCatalog.ContainsKey("bugling").Should().BeTrue("sample enemies.json should define 'bugling'");
        var tmpl = enemyCatalog["bugling"];
        tmpl.MoveIds.Should().Contain("spark");

        // Instantiate EnemyCharacter with mapped moves like the app does
        var enemy = new Game.Core.Domain.EnemyCharacter(tmpl.Name, 3, tmpl.Type, tmpl.BaseStats, new Game.Application.Battle.SimpleAiStrategy());
        foreach (var mvId in tmpl.MoveIds)
        {
            if (moveCatalog.TryGetValue(mvId, out var mv)) enemy.Moves.Add(mv);
        }

        enemy.Moves.Should().NotBeEmpty();
        enemy.Moves.Select(m => m.Id).Should().Contain("spark");
    }

    private static string? FindDataRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(dir, "data"))) return dir;
            var parent = Directory.GetParent(dir)?.FullName;
            if (parent is null) break;
            dir = parent;
        }
        return null;
    }

    private sealed class SystemRandomWrapper : IRandom
    {
        private readonly Random _r;
        public SystemRandomWrapper(Random r) { _r = r; }
        public double NextDouble() => _r.NextDouble();
        public double NextDouble(double minInclusive, double maxExclusive) => minInclusive + _r.NextDouble() * (maxExclusive - minInclusive);
        public int Next(int minInclusive, int maxExclusive) => _r.Next(minInclusive, maxExclusive);
    }
}
