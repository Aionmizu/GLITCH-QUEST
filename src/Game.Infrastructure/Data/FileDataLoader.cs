using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
}
