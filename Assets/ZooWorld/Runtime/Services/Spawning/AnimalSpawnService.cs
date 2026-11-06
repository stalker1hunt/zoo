using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ZooWorld
{
    // Decides when, what and where to spawn. The factory owns how actors are created.
    public class AnimalSpawnService : IAnimalSpawner
    {
        private readonly IAnimalDatabase database;
        private readonly IWorldBounds bounds;
        private readonly IAnimalFactory factory;
        private float timeUntilSpawn;
        private bool disposed;

        public AnimalSpawnService(IAnimalDatabase animalDatabase, IWorldBounds worldBounds, IAnimalFactory animalFactory)
        {
            database = animalDatabase ?? throw new ArgumentNullException(nameof(animalDatabase));
            bounds = worldBounds ?? throw new ArgumentNullException(nameof(worldBounds));
            factory = animalFactory ?? throw new ArgumentNullException(nameof(animalFactory));
            timeUntilSpawn = Random.Range(1f, 2f);
        }

        public void Tick(float deltaTime)
        {
            if (disposed) return;
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            timeUntilSpawn -= deltaTime;
            if (timeUntilSpawn > 0f) return;
            AnimalDefinition definition = database.Definitions[Random.Range(0, database.Definitions.Count)];
            float angle = Random.Range(0f, Mathf.PI * 2f);
            factory.Spawn(definition, bounds.RandomPosition(), new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            // Do not burst-spawn to catch up after a stall: the next animal gets a new interval.
            timeUntilSpawn = Random.Range(1f, 2f);
        }

        // The composition root owns the factory lifetime, not this scheduling policy.
        public void Dispose() => disposed = true;
    }
}
