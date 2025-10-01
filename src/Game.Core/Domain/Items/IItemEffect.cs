namespace Game.Core.Domain.Items;

using Game.Core.Domain;

public interface IItemEffect
{
    string Description { get; }
    void Apply(Character target);
}