using System.Collections.ObjectModel;
using System.Linq;
using Game.Core.Domain.Items;

namespace Game.Core.Domain;

public sealed class Inventory
{
    private readonly List<Item> _items = new();
    public IReadOnlyList<Item> Items => _items;

    public void Add(Item item) => _items.Add(item);
    public bool Remove(Item item) => _items.Remove(item);

    public bool HasKey(string keyId) => _items.OfType<KeyItem>().Any(k => k.KeyId == keyId);
}