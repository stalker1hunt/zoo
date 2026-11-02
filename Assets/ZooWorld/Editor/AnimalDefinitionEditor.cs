using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ZooWorld.Editor
{
    [CustomEditor(typeof(AnimalDefinition))]
    public class AnimalDefinitionEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Add(new PropertyField(serializedObject.FindProperty("displayName")));
            root.Add(new PropertyField(serializedObject.FindProperty("role")));
            var prefabField = new PropertyField(serializedObject.FindProperty("prefab"));
            root.Add(prefabField);
            var settingsPanel = new VisualElement();
            root.Add(settingsPanel);
            AnimalActor displayedPrefab = null;
            Type displayedSettingsType = null;
            void RefreshSettings()
            {
                settingsPanel.Unbind();
                settingsPanel.Clear();
                serializedObject.Update();
                var definition = (AnimalDefinition)target;
                displayedPrefab = definition.Prefab;
                displayedSettingsType = definition.MovementSettings?.GetType();
                AnimalMovement movement = definition.Prefab != null ? definition.Prefab.Movement : null;
                if (movement == null)
                {
                    settingsPanel.Add(new HelpBox("Assign an animal prefab with its movement reference first.", HelpBoxMessageType.Info));
                    return;
                }
                if (!movement.SupportsSettings(definition.MovementSettings))
                {
                    settingsPanel.Add(new HelpBox("This prefab needs " + movement.GetType().Name + " settings. Existing settings will change only when you press the button.", HelpBoxMessageType.Warning));
                    settingsPanel.Add(new Button(() =>
                    {
                        InitializeMovementSettings(definition);
                        serializedObject.Update();
                        RefreshSettings();
                    }) { text = "Use prefab movement settings" });
                    return;
                }
                settingsPanel.Add(new PropertyField(serializedObject.FindProperty("movementSettings"), "Movement settings"));
                settingsPanel.Bind(serializedObject);
            }
            root.TrackSerializedObjectValue(serializedObject, _ =>
            {
                var definition = (AnimalDefinition)target;
                if (definition.Prefab != displayedPrefab || definition.MovementSettings?.GetType() != displayedSettingsType)
                    RefreshSettings();
            });
            RefreshSettings();
            return root;
        }

        public static void InitializeMovementSettings(AnimalDefinition definition)
        {
            if (definition == null || definition.Prefab == null || definition.Prefab.Movement == null)
                throw new InvalidOperationException("Assign a prefab with movement before initializing settings.");
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("movementSettings").managedReferenceValue = MovementSettingsAuthoring.CreateDefaults(definition.Prefab.Movement);
            serialized.ApplyModifiedProperties();
        }
    }
}
