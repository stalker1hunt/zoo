using System;
using UnityEngine;

namespace ZooWorld
{
    // Advance state, optionally turn, and return planar speed.
    // Actor moves the Rigidbody and faces this same direction.
    public abstract class AnimalMovement : MonoBehaviour
    {
        public abstract bool SupportsSettings(AnimalMovementSettings settings);
        public abstract void Initialize(AnimalMovementSettings settings);
        public virtual void ValidateConfiguration() { }
        public virtual void ResetState() { }
        public abstract float Tick(float deltaTime, ref Vector3 direction);

        public void ValidateSettings(AnimalMovementSettings settings)
        {
            if (!SupportsSettings(settings))
                throw new InvalidOperationException($"{GetType().Name} needs matching movement settings in the species definition.");
            settings.ValidateConfiguration();
        }
    }
}
