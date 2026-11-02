using System;
using UnityEngine;

namespace ZooWorld
{
    [Serializable]
    public class LinearMovementSettings : AnimalMovementSettings
    {
        [SerializeField, Min(0.1f)] private float speed = 2.4f;
        public float Speed => speed;

        public override void ValidateConfiguration()
        {
            if (!(speed > 0f) || float.IsInfinity(speed))
                throw new InvalidOperationException("Linear speed must be finite and positive.");
        }
    }
}
