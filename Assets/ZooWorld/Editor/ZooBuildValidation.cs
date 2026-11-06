using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace ZooWorld.Editor
{
    public static class ZooBuildValidation
    {
        // Batch-mode entry point; preserves the project's configured scene list.
        public static void BuildWindows()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("No enabled build scenes.");
            foreach (string guid in AssetDatabase.FindAssets("t:AnimalDefinition", new[] { "Assets/ZooWorld" }))
            {
                AnimalDefinition definition = AssetDatabase.LoadAssetAtPath<AnimalDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                definition.ValidateConfiguration();
                AnimalPrefabValidation.Validate(definition.Prefab);
            }
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/Validation/Zoo.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Player build failed: {report.summary.result}, {report.summary.totalErrors} errors.");
        }
    }
}
