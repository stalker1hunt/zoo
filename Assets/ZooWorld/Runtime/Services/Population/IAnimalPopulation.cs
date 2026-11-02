using System;

namespace ZooWorld
{
    public interface IAnimalPopulation
    {
        int Alive { get; }
        int DeadPrey { get; }
        int DeadPredators { get; }
        event Action Changed;
    }
}
