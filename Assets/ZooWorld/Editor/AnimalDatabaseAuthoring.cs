using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ZooWorld.Editor
{
    public static class AnimalDatabaseAuthoring
    {
        public static void AddSpecies(AnimalDatabase database, AnimalDefinition definition)
        {
            ValidateAddition(database, definition);
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Add species to database");
            Append(database, definition);
        }

        public static void RemoveSpecies(AnimalDatabase database, AnimalDefinition definition)
        {
            if (database == null) throw new ArgumentException("Choose a database first.");
            int index = database.Definitions.ToList().IndexOf(definition);
            if (index < 0) throw new ArgumentException("This species is not in the database.");
            if (definition != null && database.Definitions.Count(entry => entry != null) <= 1)
                throw new InvalidOperationException("Keep at least one species in the database.");
            var serialized = new SerializedObject(database);
            SerializedProperty rules = serialized.FindProperty("exceptions");
            for (int i = 0; definition != null && i < rules.arraySize; i++)
            {
                SerializedProperty rule = rules.GetArrayElementAtIndex(i);
                if (rule.FindPropertyRelative("first").objectReferenceValue == definition
                    || rule.FindPropertyRelative("second").objectReferenceValue == definition)
                    throw new InvalidOperationException("This species is used in pair exceptions. Remove those rules in Interactions first.");
            }
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Remove species from database");
            SerializedProperty entries = serialized.FindProperty("definitions");
            int count = entries.arraySize;
            entries.DeleteArrayElementAtIndex(index);
            if (entries.arraySize == count) entries.DeleteArrayElementAtIndex(index);
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(database);
        }

        public static void MoveSpecies(AnimalDatabase database, int from, int to)
        {
            if (database == null || from < 0 || to < 0 || from >= database.Definitions.Count || to >= database.Definitions.Count)
                throw new ArgumentOutOfRangeException("Choose registered species to reorder.");
            if (from == to) return;
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Reorder species");
            var serialized = new SerializedObject(database);
            serialized.FindProperty("definitions").MoveArrayElement(from, to);
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(database);
        }

        public static AnimalDefinition CreateSpeciesAsset(AnimalDatabase database, string path, AnimalActor prefab,
            string displayName = null, AnimalRole role = AnimalRole.Prey)
        {
            if (database == null || string.IsNullOrWhiteSpace(path) || !path.StartsWith("Assets/", StringComparison.Ordinal)
                || path.Contains("..") || !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Choose a new .asset path inside Assets.");
            if (prefab == null) throw new ArgumentException("Choose an animal prefab.");
            AnimalPrefabValidation.Validate(prefab);
            string name = string.IsNullOrWhiteSpace(displayName) ? Path.GetFileNameWithoutExtension(path) : displayName.Trim();
            if (database.Definitions.Any(entry => entry != null && string.Equals(entry.DisplayName, name, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Choose a unique species name.");
            string absolutePath = Path.GetFullPath(path);
            if (!AssetDatabase.IsValidFolder(Path.GetDirectoryName(path).Replace('\\', '/')))
                throw new ArgumentException("Choose an existing folder inside Assets.");
            if (File.Exists(absolutePath) || File.Exists(absolutePath + ".meta") || Directory.Exists(absolutePath)
                || AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new InvalidOperationException("Path already exists; choose a new path.");

            var definition = ScriptableObject.CreateInstance<AnimalDefinition>();
            var data = new SerializedObject(definition);
            data.FindProperty("displayName").stringValue = name;
            data.FindProperty("role").enumValueIndex = (int)role;
            data.FindProperty("prefab").objectReferenceValue = prefab;
            data.FindProperty("movementSettings").managedReferenceValue = MovementSettingsAuthoring.CreateDefaults(prefab.Movement);
            data.ApplyModifiedPropertiesWithoutUndo();
            try { definition.ValidateConfiguration(); }
            catch { UnityEngine.Object.DestroyImmediate(definition); throw; }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create species and add to database");
            AssetDatabase.CreateAsset(definition, path);
            Undo.RegisterCreatedObjectUndo(definition, "Create species");
            Append(database, definition);
            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssetIfDirty(definition);
            return definition;
        }

        private static void ValidateAddition(AnimalDatabase database, AnimalDefinition definition)
        {
            if (database == null) throw new ArgumentException("Choose a database first.");
            if (definition == null || !AssetDatabase.Contains(definition))
                throw new ArgumentException("Choose an existing AnimalDefinition asset.");
            if (database.Definitions.Contains(definition)) throw new InvalidOperationException("This species is already in the database.");
            if (database.Definitions.Any(entry => entry != null && string.Equals(entry.DisplayName, definition.DisplayName, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("A species with this name is already in the database.");
            definition.ValidateConfiguration();
            AnimalPrefabValidation.Validate(definition.Prefab);
        }

        private static void Append(AnimalDatabase database, AnimalDefinition definition)
        {
            var serialized = new SerializedObject(database);
            SerializedProperty entries = serialized.FindProperty("definitions");
            entries.InsertArrayElementAtIndex(entries.arraySize);
            entries.GetArrayElementAtIndex(entries.arraySize - 1).objectReferenceValue = definition;
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(database);
        }
    }
}
