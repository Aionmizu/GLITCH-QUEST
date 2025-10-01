using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Game.Core.Abstractions;
using Game.Core.Domain;

namespace Game.Infrastructure.Persistence;

public sealed class JsonSaveRepository : ISaveRepository
{
    private readonly string _baseDirectory;
    private readonly JsonSerializerOptions _options;

    public JsonSaveRepository(string baseDirectory, JsonSerializerOptions? options = null)
    {
        _baseDirectory = baseDirectory;
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        Directory.CreateDirectory(_baseDirectory);
    }

    private string SlotPath(string slotId) => Path.Combine(_baseDirectory, slotId + ".json");

    public void Save(string slotId, SaveGame save)
    {
        if (string.IsNullOrWhiteSpace(slotId)) throw new ArgumentException("slotId is required", nameof(slotId));
        // Ensure the in-memory metadata reflects the slot used
        save = save with { SlotId = slotId };
        var path = SlotPath(slotId);
        var json = JsonSerializer.Serialize(save, _options);
        File.WriteAllText(path, json);
    }

    public SaveGame? Load(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId)) throw new ArgumentException("slotId is required", nameof(slotId));
        var path = SlotPath(slotId);
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        var obj = JsonSerializer.Deserialize<SaveGame>(json, _options);
        // Re-attach slot metadata (it is ignored in JSON by design)
        if (obj is not null) obj = obj with { SlotId = slotId };
        return obj;
    }

    public IEnumerable<string> ListSlots()
    {
        if (!Directory.Exists(_baseDirectory)) yield break;
        foreach (var file in Directory.GetFiles(_baseDirectory, "*.json"))
        {
            yield return Path.GetFileNameWithoutExtension(file);
        }
    }
}
