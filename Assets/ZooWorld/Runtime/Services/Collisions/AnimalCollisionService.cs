using System;
using UnityEngine;

namespace ZooWorld
{
    public class AnimalCollisionService : IAnimalCollisions
    {
        private readonly IAnimalDatabase database;
        private readonly IAnimalLifecycle lifecycle;
        private readonly float knockbackSpeed;
        public event Action<AnimalActor> AnimalEaten;

        public AnimalCollisionService(IAnimalDatabase animalDatabase, IAnimalLifecycle population, float preyKnockbackSpeed)
        {
            database = animalDatabase;
            lifecycle = population;
            knockbackSpeed = preyKnockbackSpeed;
        }

        public void Resolve(AnimalActor first, AnimalActor second)
        {
            if (first == null || second == null || first == second || !first.IsAlive || !second.IsAlive) return;
            AnimalInteraction interaction = database.GetInteraction(first.Definition, second.Definition);
            if (interaction == AnimalInteraction.PhysicalContact) return;
            if (interaction == AnimalInteraction.Bounce)
            {
                // Each animal receives OnCollisionEnter. Only the canonical callback adds the impulse.
                if (first.GetInstanceID() > second.GetInstanceID()) return;
                Vector3 away = first.Body.position - second.Body.position;
                away.y = 0f;
                if (away.sqrMagnitude < 0.0001f) away = Vector3.right;
                away.Normalize();
                first.ApplyKnockback(away * knockbackSpeed);
                second.ApplyKnockback(-away * knockbackSpeed);
                return;
            }
            AnimalActor eater;
            if (interaction == AnimalInteraction.FirstEatsSecond) eater = first;
            else if (interaction == AnimalInteraction.SecondEatsFirst) eater = second;
            else eater = first.GetInstanceID() < second.GetInstanceID() ? first : second;
            AnimalActor victim = eater == first ? second : first;
            if (lifecycle.TryRemove(victim, true)) AnimalEaten?.Invoke(eater);
        }
    }
}
