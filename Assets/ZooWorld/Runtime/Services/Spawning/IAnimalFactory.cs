using System;
using UnityEngine;

namespace ZooWorld
{
    // Owns animal instances; spawning policy does not need to know about pools.
    public interface IAnimalFactory : IDisposable
    {
        AnimalActor Spawn(AnimalDefinition definition, Vector3 position, Vector3 direction);
        void FlushReturns();
    }
}
