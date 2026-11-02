using System;

namespace ZooWorld
{
    [Serializable]
    public abstract class AnimalMovementSettings
    {
        public abstract void ValidateConfiguration();
    }
}
