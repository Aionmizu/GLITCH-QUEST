using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Core.Abstractions;
using Game.Core.Domain;

namespace Game.Infrastructure.Data;

/// <summary>
/// Loads game data from the local file system (data/ directory by default).
/// This class belongs to Infrastructure and performs I/O; it converts raw files to domain-ready shapes.
/// </summary>
public sealed class FileDataLoader
{
    private readonly string _root;

    public FileDataLoader(string root)
    {
        _root = root;
    }

    public sealed record EnemyTemplate(string Id, string Name, Element Type, Stats BaseStats, List<string> MoveIds);

    public IEnumerable<string> ReadMapLines(string mapId)
    {
        var path = Path.Combine(_root, "data", "maps", mapId + ".txt");
        if (!File.Exists(path)) throw new FileNotFoundException($"Map file not found: {path}");
        return File.ReadAllLines(path);
    }

    /// <summary>
    /// Load the type chart from data/typechart.json and convert it to a SimpleTypeChart.
    /// The JSON is expected to be a nested object of string keys to double values.
    /// </summary>
    public ITypeChart LoadTypeChart()
    {
        var path = Path.Combine(_root, "data", "typechart.json");
        if (!File.Exists(path)) return SimpleTypeChart.Default();
        var json = File.ReadAllText(path);
        var raw = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, double>>>(json)
                  ?? new Dictionary<string, Dictionary<string, double>>();
        var dict = new Dictionary<Element, Dictionary<Element, double>>();
        foreach (var atkKv in raw)
        {
            if (!Enum.TryParse<Element>(atkKv.Key, ignoreCase: true, out var atk)) continue;
            var row = new Dictionary<Element, double>();
            foreach (var defKv in atkKv.Value)
            {
                if (!Enum.TryParse<Element>(defKv.Key, ignoreCase: true, out var def)) continue;
                row[def] = defKv.Value;
            }
            dict[atk] = row;
        }
        return new SimpleTypeChart(dict);
    }

    /// <summary>
    /// Load all moves from data/moves.json into a dictionary by id.
    /// </summary>
    public Dictionary<string, Move> LoadMoves()
    {
        var path = Path.Combine(_root, "data", "moves.json");
        if (!File.Exists(path)) return new Dictionary<string, Move>(StringComparer.OrdinalIgnoreCase);
        var json = File.ReadAllText(path);
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var list = JsonSerializer.Deserialize<List<MoveDto>>(json, opts) ?? new List<MoveDto>();
        var dict = new Dictionary<string, Move>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in list)
        {
            if (string.IsNullOrWhiteSpace(m.Id)) continue;
            if (!Enum.TryParse<Element>(m.Type, true, out var elem)) elem = Element.Normal;
            if (!Enum.TryParse<DamageKind>(m.Kind, true, out var kind)) kind = DamageKind.Physical;
            var move = new Move
            {
                Id = m.Id,
                Name = m.Name ?? m.Id,
                Type = elem,
                Power = m.Power,
                Kind = kind,
                Accuracy = m.Accuracy,
                MpCost = m.MpCost,
                CritChance = m.CritChance
            };
            dict[m.Id] = move;
        }
        return dict;
    }

    /// <summary>
    /// Load all enemies from data/enemies.json. Returns a dictionary by id.
    /// </summary>
    public Dictionary<string, EnemyTemplate> LoadEnemies()
    {
        var path = Path.Combine(_root, "data", "enemies.json");
        var result = new Dictionary<string, EnemyTemplate>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path)) return result;
        var json = File.ReadAllText(path);
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var list = JsonSerializer.Deserialize<List<EnemyDto>>(json, opts) ?? new List<EnemyDto>();
        foreach (var e in list)
        {
            if (string.IsNullOrWhiteSpace(e.Id)) continue;
            if (!Enum.TryParse<Element>(e.Type, true, out var elem)) elem = Element.Normal;
            var baseStats = new Stats(e.BaseStats.Hp, e.BaseStats.Mp, e.BaseStats.Atk, e.BaseStats.Def, e.BaseStats.Spd, e.BaseStats.Acc, e.BaseStats.Eva);
            var tmpl = new EnemyTemplate(e.Id, e.Name ?? e.Id, elem, baseStats, e.Moves?.ToList() ?? new List<string>());
            result[e.Id] = tmpl;
        }
        return result;
    }

    private sealed class MoveDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Normal";
        public int Power { get; set; }
        public string Kind { get; set; } = "Physical";
        public double Accuracy { get; set; } = 1.0;
        public int MpCost { get; set; }
        public double CritChance { get; set; } = 0.1;
    }

    private sealed class EnemyDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Normal";
        public int MinLvl { get; set; }
        public int MaxLvl { get; set; }
        public List<string> Moves { get; set; } = new();
        public StatBlock BaseStats { get; set; } = new();
    }

    public sealed class StatBlock
    {
        public int Hp { get; set; }
        public int Mp { get; set; }
        public int Atk { get; set; }
        public int Def { get; set; }
        public int Spd { get; set; }
        public double Acc { get; set; } = 1.0;
        public double Eva { get; set; } = 1.0;
    }
}
