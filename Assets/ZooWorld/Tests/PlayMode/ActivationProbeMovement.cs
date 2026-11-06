using System;
using UnityEngine;

namespace ZooWorld.Tests
{
    // A new movement implementation: factory tests must not rely only on built-in components.
    public sealed class ActivationProbeMovement : AnimalMovement
    {
        public static bool ThrowOnNextInitialize;
        private bool initialized;
        public int EnableCount { get; private set; }
        public bool SawFullyInitializedActor { get; private set; }

        public override bool SupportsSettings(AnimalMovementSettings settings) => settings is LinearMovementSettings;

        public override void Initialize(AnimalMovementSettings settings)
        {
            if (ThrowOnNextInitialize)
            {
                ThrowOnNextInitialize = false;
                throw new InvalidOperationException("Test movement rejected initialization.");
            }
            initialized = true;
        }

        public override float Tick(float deltaTime, ref Vector3 direction) => 0f;

        private void OnEnable()
        {
            EnableCount++;
            AnimalActor actor = GetComponent<AnimalActor>();
            SawFullyInitializedActor = initialized && actor.IsAlive && actor.Definition != null
                && actor.Body.detectCollisions && actor.Body.linearVelocity == Vector3.zero
                && actor.Body.angularVelocity == Vector3.zero;
        }
    }
}
