using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SpaceCleaner.Editor
{
    /// <summary>
    /// Ensures runtime-created materials (particles, trail, skybox) have their shaders
    /// in "Always Included Shaders" so they survive Android build shader stripping.
    /// Runs automatically before every build via IPreprocessBuildWithReport.
    /// </summary>
    public class ShaderIncludes : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private static readonly string[] RequiredShaders =
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Particles/Unlit",
            "Skybox/Procedural",
            "Skybox/6 Sided",
        };

        public void OnPreprocessBuild(BuildReport report)
        {
            var graphicsSettingsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphicsSettingsAssets.Length == 0)
            {
                Debug.LogWarning("[ShaderIncludes] Could not load GraphicsSettings.asset");
                return;
            }

            var so = new SerializedObject(graphicsSettingsAssets[0]);
            var alwaysIncluded = so.FindProperty("m_AlwaysIncludedShaders");

            bool dirty = false;
            foreach (var shaderName in RequiredShaders)
            {
                var shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    Debug.LogWarning($"[ShaderIncludes] Shader not found: {shaderName}");
                    continue;
                }

                bool found = false;
                for (int i = 0; i < alwaysIncluded.arraySize; i++)
                {
                    if (alwaysIncluded.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    int idx = alwaysIncluded.arraySize;
                    alwaysIncluded.InsertArrayElementAtIndex(idx);
                    alwaysIncluded.GetArrayElementAtIndex(idx).objectReferenceValue = shader;
                    dirty = true;
                    Debug.Log($"[ShaderIncludes] Added to Always Included Shaders: {shaderName}");
                }
            }

            if (dirty)
                so.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("SpaceCleaner/Setup/Ensure Shaders Included In Build")]
        public static void RunManually()
        {
            var dummy = new ShaderIncludes();
            dummy.OnPreprocessBuild(null);
            Debug.Log("[ShaderIncludes] Done. Rebuild to apply.");
        }
    }
}
