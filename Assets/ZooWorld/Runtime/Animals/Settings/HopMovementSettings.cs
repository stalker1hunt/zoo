using System;
using UnityEngine;

namespace ZooWorld
{
    [Serializable]
    public class HopMovementSettings : AnimalMovementSettings
    {
        [SerializeField, Min(0.1f)] private float distance = 2.2f;
        [SerializeField, Min(0.1f)] private float duration = 0.45f;
        [SerializeField, Min(0f)] private float restDuration = 0.85f;
        [SerializeField, Min(0f)] private float height = 0.8f;
        public float Distance => distance;
        public float Duration => duration;
        public float RestDuration => restDuration;
        public float Height => height;

        public override void ValidateConfiguration()
        {
            // Match the authoring minimum: subnormal durations can stop the fixed-step
            // integrator from making floating-point progress when it subtracts a segment.
            if (!(distance > 0f) || !(duration >= 0.1f) || !(restDuration >= 0f) || !(height >= 0f)
                || float.IsInfinity(distance + duration + restDuration + height))
                throw new InvalidOperationException("Hop settings need finite positive distance, duration >= 0.1 seconds and nonnegative rest/height.");
        }
    }
}
