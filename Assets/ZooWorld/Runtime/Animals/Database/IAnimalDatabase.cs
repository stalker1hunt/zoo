using System.Collections.Generic;

namespace ZooWorld
{
    public interface IAnimalDatabase
    {
        IReadOnlyList<AnimalDefinition> Definitions { get; }
        AnimalInteraction GetInteraction(AnimalDefinition first, AnimalDefinition second);
    }
}
