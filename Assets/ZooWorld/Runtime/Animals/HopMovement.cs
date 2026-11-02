using System;
using UnityEngine;

namespace ZooWorld
{
    public class HopMovement : AnimalMovement
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Vector3 restingPosition;
        private HopMovementSettings settings;
        private float elapsed;
        public override bool SupportsSettings(AnimalMovementSettings value) => value is HopMovementSettings;
        public override void Initialize(AnimalMovementSettings configuration)
        {
            settings = configuration as HopMovementSettings
                ?? throw new InvalidOperationException("HopMovement needs HopMovementSettings.");
        }

        public override void ValidateConfiguration()
        {
            if (visual == null || visual == transform || !visual.IsChildOf(transform))
                throw new InvalidOperationException("Hop visual must be a child of the animal root.");
        }

        public override void ResetState()
        {
            elapsed = 0f;
            visual.localPosition = restingPosition;
        }

        public override float Tick(float deltaTime, ref Vector3 direction)
        {
            // Integrate the fraction of this fixed step inside the hop. In free space
            // each cycle travels exactly distance, even when duration is not a multiple of dt.
            float movingTime = 0f;
            float remaining = deltaTime;
            float duration = settings.Duration;
            float cycle = duration + settings.RestDuration;
            while (remaining > 0.000001f)
            {
                float segment = Mathf.Min(remaining, (elapsed < duration ? duration : cycle) - elapsed);
                if (elapsed < duration) movingTime += segment;
                elapsed += segment;
                remaining -= segment;
                if (elapsed >= cycle - 0.000001f) elapsed = 0f;
            }
            visual.localPosition = restingPosition + Vector3.up * (elapsed < duration ? Mathf.Sin(elapsed / duration * Mathf.PI) * settings.Height : 0f);
            return settings.Distance / duration * movingTime / deltaTime;
        }
    }
}
