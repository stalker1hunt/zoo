using UnityEngine;

namespace ZooWorld
{
    public enum AnimalRole { Prey, Predator }

    [CreateAssetMenu(menuName = "Zoo World/Animal species")]
    public class AnimalDefinition : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private AnimalRole role;
        [SerializeField] private AnimalActor prefab;
        [SerializeReference] private AnimalMovementSettings movementSettings;
        public string DisplayName => displayName;
        public AnimalRole Role => role;
        public AnimalActor Prefab => prefab;
        public AnimalMovementSettings MovementSettings => movementSettings;

        public void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(displayName) || prefab == null)
                throw new System.InvalidOperationException($"Species '{name}' needs a display name and prefab.");
            if (!System.Enum.IsDefined(typeof(AnimalRole), role))
                throw new System.InvalidOperationException("Species has an invalid role.");
            prefab.ValidateConfiguration();
            prefab.Movement.ValidateSettings(movementSettings);
        }
    }
}
