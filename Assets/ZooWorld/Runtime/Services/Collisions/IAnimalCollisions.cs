using System;

namespace ZooWorld
{
    public interface IAnimalCollisions
    {
        event Action<AnimalActor> AnimalEaten;
        void Resolve(AnimalActor first, AnimalActor second);
    }
}
