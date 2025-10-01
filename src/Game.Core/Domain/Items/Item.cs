namespace Game.Core.Domain.Items;

using Game.Core.Domain;

public class Item
{
    public string Id { get; }
    public string Name { get; }
    public IItemEffect? Effect { get; }

    public Item(string id, string name, IItemEffect? effect = null)
    {
        Id = id;
        Name = name;
        Effect = effect;
    }

    public virtual bool CanUseOn(Character target) => Effect is not null;

    public virtual void UseOn(Character target)
    {
        Effect?.Apply(target);
    }
}
