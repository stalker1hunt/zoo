using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ZooWorld
{
    // The third species adds turning without changes to Actor, spawning or contacts.
    public class WanderMovement : AnimalMovement
    {
        private WanderMovementSettings settings;
        private float timeUntilTurn;
        public override bool SupportsSettings(AnimalMovementSettings value) => value is WanderMovementSettings;
        public override void Initialize(AnimalMovementSettings configuration)
        {
            settings = configuration as WanderMovementSettings
                ?? throw new InvalidOperationException("WanderMovement needs WanderMovementSettings.");
        }
        public override void ResetState() => timeUntilTurn = settings?.TurnInterval ?? 0f;

        public override float Tick(float deltaTime, ref Vector3 direction)
        {
            timeUntilTurn -= deltaTime;
            if (timeUntilTurn <= 0f)
            {
                direction = Quaternion.Euler(0f, Random.Range(-settings.MaximumTurnAngle, settings.MaximumTurnAngle), 0f) * direction;
                timeUntilTurn += settings.TurnInterval;
            }
            return settings.Speed;
        }
    }
}
