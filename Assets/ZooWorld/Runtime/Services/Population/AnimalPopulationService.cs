using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZooWorld
{
    public class AnimalPopulationService : IAnimalLifecycle
    {
        private readonly HashSet<AnimalActor> animals = new HashSet<AnimalActor>();
        public int Alive => animals.Count;
        public int DeadPrey { get; private set; }
        public int DeadPredators { get; private set; }
        public event Action Changed;

        public void Register(AnimalActor animal)
        {
            if (animal == null || !animal.IsAlive || !animals.Add(animal))
                throw new InvalidOperationException("Only a live, unregistered animal can enter the population.");
            Changed?.Invoke();
        }

        public bool TryRemove(AnimalActor animal, bool eaten)
        {
            if (animal == null || !animal.IsAlive || !animals.Remove(animal)) return false;
            if (eaten)
            {
                if (animal.Definition.Role == AnimalRole.Prey) DeadPrey++; else DeadPredators++;
            }
            animal.ReturnToPool();
            Changed?.Invoke();
            return true;
        }

        public void Forget(AnimalActor animal)
        {
            if (animals.Remove(animal)) Changed?.Invoke();
        }

        public void Clear()
        {
            var remaining = new AnimalActor[animals.Count];
            animals.CopyTo(remaining);
            animals.Clear();
            foreach (AnimalActor animal in remaining)
            {
                if (animal == null || !animal.IsAlive) continue;
                try { animal.ReturnToPool(); }
                catch (Exception error) { Debug.LogException(error, animal); }
            }
            DeadPrey = 0;
            DeadPredators = 0;
            Changed?.Invoke();
        }
    }
}
