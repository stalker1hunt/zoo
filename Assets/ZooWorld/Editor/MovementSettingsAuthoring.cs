using System;

namespace ZooWorld.Editor
{
    // Default configuration creation belongs to authoring, not the runtime movement contract.
    internal static class MovementSettingsAuthoring
    {
        public static AnimalMovementSettings CreateDefaults(AnimalMovement movement)
        {
            switch (movement)
            {
                case HopMovement _: return new HopMovementSettings();
                case LinearMovement _: return new LinearMovementSettings();
                case WanderMovement _: return new WanderMovementSettings();
                default:
                    throw new InvalidOperationException("Add this movement's default settings to MovementSettingsAuthoring.");
            }
        }
    }
}
