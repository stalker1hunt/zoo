using System;

namespace ZooWorld
{
    public interface IAnimalSpawner : IDisposable
    {
        void Tick(float deltaTime);
    }
}
