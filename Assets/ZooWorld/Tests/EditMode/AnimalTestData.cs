using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    // Author transient fixtures through the same serialization surface as the Inspector.
    // Tests never edit or save the project's species assets.
    internal sealed class AnimalTestData : IDisposable
    {
        private readonly List<Object> owned = new List<Object>();

        public T Own<T>(T value) where T : Object
        {
            owned.Add(value);
            return value;
        }

        public AnimalDefinition Species(string name, AnimalRole role)
        {
            var definition = Own(ScriptableObject.CreateInstance<AnimalDefinition>());
            using (var serialized = new SerializedObject(definition))
            {
                serialized.FindProperty("displayName").stringValue = name;
                serialized.FindProperty("role").enumValueIndex = (int)role;
                serialized.FindProperty("prefab").objectReferenceValue = Prefab("Snake");
                serialized.FindProperty("movementSettings").managedReferenceValue = new LinearMovementSettings();
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            return definition;
        }

        public AnimalDatabase Database(params AnimalDefinition[] definitions)
        {
            var database = Own(ScriptableObject.CreateInstance<AnimalDatabase>());
            using (var serialized = new SerializedObject(database))
            {
                SerializedProperty entries = serialized.FindProperty("definitions");
                entries.arraySize = definitions.Length;
                for (int i = 0; i < definitions.Length; i++)
                    entries.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            return database;
        }

        public static void SetRules(AnimalDatabase database,
            params (AnimalDefinition first, AnimalDefinition second, AnimalInteraction result)[] rules)
        {
            using (var serialized = new SerializedObject(database))
            {
                SerializedProperty entries = serialized.FindProperty("exceptions");
                entries.arraySize = rules.Length;
                for (int i = 0; i < rules.Length; i++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("first").objectReferenceValue = rules[i].first;
                    entry.FindPropertyRelative("second").objectReferenceValue = rules[i].second;
                    entry.FindPropertyRelative("interaction").enumValueIndex = (int)rules[i].result;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static AnimalActor Prefab(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ZooWorld/Prefabs/{name}.prefab");
            Assert.That(prefab, Is.Not.Null, $"Missing required prefab: {name}");
            var actor = prefab.GetComponent<AnimalActor>();
            Assert.That(actor, Is.Not.Null);
            return actor;
        }

        public void Dispose()
        {
            for (int i = owned.Count - 1; i >= 0; i--)
                if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }
    }
}
