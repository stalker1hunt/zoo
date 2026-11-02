using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ZooWorld
{
    public class AnimalSpawnService : IAnimalSpawner
    {
        private readonly Transform parent;
        private readonly IAnimalDatabase database;
        private readonly IWorldBounds bounds;
        private readonly IAnimalCollisions collisions;
        private readonly IAnimalLifecycle lifecycle;
        private readonly Dictionary<AnimalDefinition, ObjectPool<AnimalActor>> pools = new Dictionary<AnimalDefinition, ObjectPool<AnimalActor>>();
        private readonly List<AnimalActor> pendingReturns = new List<AnimalActor>();
        private readonly Action<AnimalActor> queueReturn;
        private float timeUntilSpawn;
        private bool disposed;

        public AnimalSpawnService(Transform animalParent, IAnimalDatabase animalDatabase, IWorldBounds worldBounds,
            IAnimalCollisions collisionService, IAnimalLifecycle population)
        {
            parent = animalParent;
            database = animalDatabase;
            bounds = worldBounds;
            collisions = collisionService;
            lifecycle = population;
            queueReturn = pendingReturns.Add;
            timeUntilSpawn = Random.Range(1f, 2f);
        }

        public void Tick(float deltaTime)
        {
            if (disposed) return;
            ReleasePending();
            timeUntilSpawn -= deltaTime;
            if (timeUntilSpawn > 0f) return;
            AnimalDefinition definition = database.Definitions[Random.Range(0, database.Definitions.Count)];
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Spawn(definition, bounds.RandomPosition(), new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            timeUntilSpawn = Random.Range(1f, 2f);
        }

        private void Spawn(AnimalDefinition definition, Vector3 position, Vector3 direction)
        {
            if (!pools.TryGetValue(definition, out ObjectPool<AnimalActor> pool))
            {
                definition.ValidateConfiguration();
                pool = new ObjectPool<AnimalActor>(() =>
                {
                    AnimalActor animal = Object.Instantiate(definition.Prefab, parent);
                    animal.gameObject.SetActive(false);
                    return animal;
                }, actionOnDestroy: animal => { if (animal != null) Object.Destroy(animal.gameObject); },
                    collectionCheck: true, defaultCapacity: 16, maxSize: 10000);
                pools.Add(definition, pool);
            }
            AnimalActor instance = pool.Get();
            try
            {
                instance.Activate(definition, position, direction, bounds, collisions, lifecycle, queueReturn);
                lifecycle.Register(instance);
            }
            catch
            {
                if (!lifecycle.TryRemove(instance, false)) instance.ReturnToPool();
                throw;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            lifecycle.Clear();
            ReleasePending();
            foreach (ObjectPool<AnimalActor> pool in pools.Values) pool.Dispose();
            pools.Clear();
        }

        private void ReleasePending()
        {
            // Tick is called by Bootstrap.Update, after PhysX has delivered the current contacts.
            // A synchronous respawn from an event cannot reuse a victim during that callback batch.
            foreach (AnimalActor animal in pendingReturns)
                if (animal != null) pools[animal.Definition].Release(animal);
            pendingReturns.Clear();
        }
    }
}
