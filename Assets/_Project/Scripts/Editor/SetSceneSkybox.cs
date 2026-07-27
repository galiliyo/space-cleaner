using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceCleaner.EditorTools
{
    /// <summary>
    /// Menu items that assign a project skybox material to the active scene's RenderSettings
    /// and mark the scene dirty so it can be saved. Avoids manual drag-and-drop into the
    /// Lighting → Environment slot, which URP sometimes refuses for legacy Skybox/6 Sided materials.
    /// </summary>
    public static class SetSceneSkybox
    {
        private const string Menu = "Tools/Space Cleaner/Set Scene Skybox/";

        [MenuItem(Menu + "SpaceSkies — Purple")]
        public static void SetPurple() => Apply("Assets/SpaceSkies Free/Skybox_3/Purple_2K_Resolution.mat");

        [MenuItem(Menu + "SpaceSkies — Green")]
        public static void SetGreen() => Apply("Assets/SpaceSkies Free/Skybox_2/Green_2K_Resoution.mat");

        [MenuItem(Menu + "SpaceSkies — Pink")]
        public static void SetPink() => Apply("Assets/SpaceSkies Free/Skybox_1/Pink_2K_Resolution.mat");

        [MenuItem(Menu + "Planet Earth — Stars")]
        public static void SetStars() => Apply("Assets/Planet Earth Free/Materials/SkyboxMaterial.mat");

        [MenuItem(Menu + "Clear (use procedural at runtime)")]
        public static void Clear()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogWarning("[SetSceneSkybox] No active scene.");
                return;
            }
            RenderSettings.skybox = null;
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[SetSceneSkybox] Cleared skybox on '{scene.name}'. Save the scene (Ctrl+S) to persist.");
        }

        private static void Apply(string materialPath)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat == null)
            {
                Debug.LogError($"[SetSceneSkybox] Material not found at: {materialPath}");
                return;
            }
            if (mat.shader == null || !mat.shader.name.StartsWith("Skybox/"))
            {
                Debug.LogError($"[SetSceneSkybox] '{materialPath}' is not a Skybox material (shader: {mat.shader?.name ?? "null"}).");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogWarning("[SetSceneSkybox] No active scene.");
                return;
            }

            RenderSettings.skybox = mat;
            DynamicGI.UpdateEnvironment();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[SetSceneSkybox] Assigned '{mat.name}' to scene '{scene.name}'. Save the scene (Ctrl+S) to persist for builds.");
        }
    }
}
