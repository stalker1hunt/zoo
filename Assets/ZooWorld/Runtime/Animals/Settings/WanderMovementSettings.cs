using System;
using UnityEngine;

namespace ZooWorld
{
    [Serializable]
    public class WanderMovementSettings : AnimalMovementSettings
    {
        [SerializeField, Min(0.1f)] private float speed = 1.8f;
        [SerializeField, Min(0.1f)] private float turnInterval = 1.5f;
        [SerializeField, Range(0f, 180f)] private float maximumTurnAngle = 70f;
        public float Speed => speed;
        public float TurnInterval => turnInterval;
        public float MaximumTurnAngle => maximumTurnAngle;

        public override void ValidateConfiguration()
        {
            if (!(speed > 0f) || !(turnInterval > 0f) || float.IsInfinity(speed + turnInterval)
                || !(maximumTurnAngle >= 0f && maximumTurnAngle <= 180f))
                throw new InvalidOperationException("Wander settings need positive finite speed/interval and an angle between 0 and 180.");
        }
    }
}
