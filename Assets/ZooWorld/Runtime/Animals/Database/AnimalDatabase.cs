using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZooWorld
{
    public enum AnimalInteraction { PhysicalContact, Bounce, FirstEatsSecond, SecondEatsFirst, OneEatsOther }

    [Serializable]
    public class SpeciesInteraction
    {
        [SerializeField] private AnimalDefinition first;
        [SerializeField] private AnimalDefinition second;
        [SerializeField] private AnimalInteraction interaction;
        public AnimalDefinition First => first;
        public AnimalDefinition Second => second;
        public AnimalInteraction Interaction => interaction;
    }

    [CreateAssetMenu(menuName = "Zoo World/Animal database")]
    public class AnimalDatabase : ScriptableObject, IAnimalDatabase
    {
        [SerializeField] private List<AnimalDefinition> definitions = new List<AnimalDefinition>();
        [SerializeField] private List<SpeciesInteraction> exceptions = new List<SpeciesInteraction>();
        private IReadOnlyList<AnimalDefinition> readOnlyDefinitions;
        private Dictionary<(AnimalDefinition First, AnimalDefinition Second), AnimalInteraction> interactionLookup;
        private static uint playSession;
        private uint validatedPlaySession;
        public IReadOnlyList<AnimalDefinition> Definitions => readOnlyDefinitions ??= definitions.AsReadOnly();

        private void OnEnable() => InvalidateCache();
        private void OnValidate() => InvalidateCache();

        // Loaded ScriptableObjects can survive Play sessions when domain reload is disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void StartPlaySession() => playSession++;

        private void InvalidateCache()
        {
            readOnlyDefinitions = null;
            interactionLookup = null;
        }

        // Exceptions are unordered pairs; an eater is relative to the authored First/Second slots.
        // Reversing the query reverses directional results. No exception means the PDF role rules.
        public AnimalInteraction GetInteraction(AnimalDefinition first, AnimalDefinition second)
        {
            if (interactionLookup == null || validatedPlaySession != playSession) ValidateConfiguration();
            if (interactionLookup.TryGetValue((first, second), out AnimalInteraction interaction)) return interaction;
            if (first.Role == AnimalRole.Prey && second.Role == AnimalRole.Prey) return AnimalInteraction.Bounce;
            if (first.Role == AnimalRole.Predator && second.Role == AnimalRole.Predator) return AnimalInteraction.OneEatsOther;
            return first.Role == AnimalRole.Predator ? AnimalInteraction.FirstEatsSecond : AnimalInteraction.SecondEatsFirst;
        }

        public void ValidateConfiguration()
        {
            interactionLookup = null;
            if (definitions.Count == 0) throw new InvalidOperationException("Animal database needs at least one species.");
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var assets = new HashSet<AnimalDefinition>();
            foreach (AnimalDefinition definition in definitions)
            {
                if (definition == null || !assets.Add(definition)) throw new InvalidOperationException("Database contains a missing or duplicate species.");
                definition.ValidateConfiguration();
                if (!names.Add(definition.DisplayName)) throw new InvalidOperationException("Species names must be unique (case insensitive).");
            }
            var pairs = new HashSet<(AnimalDefinition, AnimalDefinition)>();
            foreach (SpeciesInteraction rule in exceptions)
            {
                if (rule == null || !assets.Contains(rule.First) || !assets.Contains(rule.Second)
                    || !Enum.IsDefined(typeof(AnimalInteraction), rule.Interaction))
                    throw new InvalidOperationException("Interaction exceptions must reference registered species and a valid outcome.");
                if (rule.First == rule.Second && (rule.Interaction == AnimalInteraction.FirstEatsSecond || rule.Interaction == AnimalInteraction.SecondEatsFirst))
                    throw new InvalidOperationException("A same-species pair needs OneEatsOther, Bounce or PhysicalContact.");
                if (pairs.Contains((rule.Second, rule.First)) || !pairs.Add((rule.First, rule.Second)))
                    throw new InvalidOperationException("Each unordered species pair may have only one exception.");
            }

            var lookup = new Dictionary<(AnimalDefinition, AnimalDefinition), AnimalInteraction>(exceptions.Count * 2);
            foreach (SpeciesInteraction rule in exceptions)
            {
                lookup.Add((rule.First, rule.Second), rule.Interaction);
                if (rule.First != rule.Second) lookup.Add((rule.Second, rule.First), Reverse(rule.Interaction));
            }
            interactionLookup = lookup;
            validatedPlaySession = playSession;
        }

        private static AnimalInteraction Reverse(AnimalInteraction interaction)
        {
            if (interaction == AnimalInteraction.FirstEatsSecond) return AnimalInteraction.SecondEatsFirst;
            if (interaction == AnimalInteraction.SecondEatsFirst) return AnimalInteraction.FirstEatsSecond;
            return interaction;
        }
    }
}
