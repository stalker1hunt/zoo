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
            if (!(distance > 0f) || !(duration > 0f) || !(restDuration >= 0f) || !(height >= 0f)
                || float.IsInfinity(distance + duration + restDuration + height))
                throw new InvalidOperationException("Hop settings need finite positive distance/duration and nonnegative rest/height.");
        }
    }
}
