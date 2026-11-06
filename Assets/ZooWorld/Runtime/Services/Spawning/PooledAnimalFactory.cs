using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace ZooWorld
{
    public sealed class PooledAnimalFactory : IAnimalFactory
    {
        // Retained inactive instances per species, not a cap on the live population.
        private const int RetainedAnimalsPerSpecies = 32;
        private readonly Transform parent;
        private readonly IWorldBounds bounds;
        private readonly IAnimalCollisions collisions;
        private readonly IAnimalLifecycle lifecycle;
        private readonly Dictionary<AnimalDefinition, ObjectPool<AnimalActor>> pools = new Dictionary<AnimalDefinition, ObjectPool<AnimalActor>>();
        private readonly List<AnimalActor> pendingReturns = new List<AnimalActor>();
        private readonly Action<AnimalActor> queueReturn;
        private readonly Transform inactiveRoot;
        private bool disposed;

        public PooledAnimalFactory(Transform animalParent, IWorldBounds worldBounds,
            IAnimalCollisions collisionService, IAnimalLifecycle population)
        {
            if (animalParent == null) throw new ArgumentNullException(nameof(animalParent));
            parent = animalParent;
            bounds = worldBounds ?? throw new ArgumentNullException(nameof(worldBounds));
            collisions = collisionService ?? throw new ArgumentNullException(nameof(collisionService));
            lifecycle = population ?? throw new ArgumentNullException(nameof(population));
            queueReturn = pendingReturns.Add;
            // Instantiate under an inactive parent: custom OnEnable code must never run
            // before Activate has supplied the definition, dependencies and physics state.
            var storage = new GameObject("Inactive animals");
            storage.SetActive(false);
            inactiveRoot = storage.transform;
            inactiveRoot.SetParent(parent, false);
        }

        public AnimalActor Spawn(AnimalDefinition definition, Vector3 position, Vector3 direction)
        {
            if (disposed) throw new ObjectDisposedException(nameof(PooledAnimalFactory));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!pools.TryGetValue(definition, out ObjectPool<AnimalActor> pool))
            {
                definition.ValidateConfiguration();
                pool = new ObjectPool<AnimalActor>(() =>
                {
                    AnimalActor animal = Object.Instantiate(definition.Prefab, inactiveRoot);
                    animal.gameObject.SetActive(false);
                    animal.transform.SetParent(parent, false);
                    return animal;
                }, actionOnDestroy: animal => { if (animal != null) Object.Destroy(animal.gameObject); },
                    collectionCheck: true, defaultCapacity: 4, maxSize: RetainedAnimalsPerSpecies);
                pools.Add(definition, pool);
            }
            AnimalActor instance = pool.Get();
            try
            {
                instance.Activate(definition, position, direction, bounds, collisions, lifecycle, queueReturn);
                lifecycle.Register(instance);
                return instance;
            }
            catch
            {
                if (!lifecycle.TryRemove(instance, false)) instance.ReturnToPool();
                throw;
            }
        }

        public void FlushReturns()
        {
            if (disposed) return;
            ReleasePending();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try { lifecycle.Clear(); }
            finally
            {
                try { ReleasePending(); }
                finally
                {
                    foreach (ObjectPool<AnimalActor> pool in pools.Values) pool.Dispose();
                    pools.Clear();
                    if (inactiveRoot != null) Object.Destroy(inactiveRoot.gameObject);
                }
            }
        }

        private void ReleasePending()
        {
            // Only the composition root flushes after the physics callback batch.
            // Spawn itself must not flush: event listeners can spawn synchronously.
            foreach (AnimalActor animal in pendingReturns)
                if (animal != null) pools[animal.Definition].Release(animal);
            pendingReturns.Clear();
        }
    }
}
