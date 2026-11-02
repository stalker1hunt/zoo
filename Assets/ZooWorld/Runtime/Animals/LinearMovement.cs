using System;
using UnityEngine;

namespace ZooWorld
{
    public class LinearMovement : AnimalMovement
    {
        private LinearMovementSettings settings;
        public override bool SupportsSettings(AnimalMovementSettings value) => value is LinearMovementSettings;
        public override void Initialize(AnimalMovementSettings configuration)
        {
            settings = configuration as LinearMovementSettings
                ?? throw new InvalidOperationException("LinearMovement needs LinearMovementSettings.");
        }
        public override float Tick(float deltaTime, ref Vector3 direction) => settings.Speed;
    }
}
