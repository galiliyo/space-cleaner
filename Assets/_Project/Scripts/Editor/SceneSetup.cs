using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using SpaceCleaner.Core;
using SpaceCleaner.Player;
using SpaceCleaner.Camera;
using SpaceCleaner.Enemies;
using SpaceCleaner.UI;

namespace SpaceCleaner.EditorTools
{
    public static class SceneSetup
    {
        private const string PrefabRoot = "Assets/_Project/Prefabs";
        private const string MaterialRoot = "Assets/_Project/Materials";

        [MenuItem("SpaceCleaner/1. Create Prefabs")]
        public static void CreatePrefabs()
        {
            EnsureFolders();
            CreateProjectilePrefab();
            CreateTrashPrefabs();
            CreatePlayerShipPrefab();
            CreateAIShipPrefab();
            CreatePlanetPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneSetup] All prefabs created!");
        }

        [MenuItem("SpaceCleaner/2. Setup Gameplay Scene")]
        public static void SetupGameplayScene()
        {
            // Ensure we're in the Gameplay scene
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.name.Contains("Gameplay"))
            {
                Debug.LogError("[SceneSetup] Please open the Gameplay scene first!");
                return;
            }

            // Clean up existing objects (except camera and light)
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root.name == "Main Camera" || root.name == "Directional Light")
                    continue;
                Undo.DestroyObjectImmediate(root);
            }

            // Load prefabs
            var planetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/Planets/Planet.prefab");
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/Player/PlayerShip.prefab");
            var aiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/Enemies/AIShip.prefab");
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/Projectiles/Projectile.prefab");
            var trashA = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/Trash/Trash_A.prefab");
            var trashB = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/Trash/Trash_B.prefab");
            var trashC = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/Trash/Trash_C.prefab");
            var inputActions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                "Assets/_Project/Input/SpaceCleaner_Actions.inputactions");

            if (planetPrefab == null || playerPrefab == null)
            {
                Debug.LogError("[SceneSetup] Prefabs not found! Run 'SpaceCleaner/1. Create Prefabs' first.");
                return;
            }

            // --- GameManager ---
            var gmGo = new GameObject("GameManager");
            gmGo.AddComponent<GameManager>();
            Undo.RegisterCreatedObjectUndo(gmGo, "Create GameManager");

            // --- Planet ---
            var planet = (GameObject)PrefabUtility.InstantiatePrefab(planetPrefab);
            planet.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(planet, "Create Planet");

            // --- Player ---
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(0, 52, 0);
            player.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            Undo.RegisterCreatedObjectUndo(player, "Create Player");

            // Wire player references
            var pc = player.GetComponent<PlayerController>();
            if (pc != null && inputActions != null)
            {
                var so = new SerializedObject(pc);
                so.FindProperty("inputActions").objectReferenceValue = inputActions;
                so.ApplyModifiedProperties();
            }

            var shooting = player.GetComponent<ShootingSystem>();
            if (shooting != null && projectilePrefab != null)
            {
                var so = new SerializedObject(shooting);
                so.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
                var firePoint = player.transform.Find("FirePoint");
                if (firePoint != null)
                    so.FindProperty("firePoint").objectReferenceValue = firePoint;
                so.ApplyModifiedProperties();
            }

            var vacuum = player.GetComponent<VacuumCollector>();
            if (vacuum != null)
            {
                var so = new SerializedObject(vacuum);
                so.FindProperty("trashLayer").intValue = 1 << 8; // Trash layer
                so.ApplyModifiedProperties();
            }

            // --- AI Opponent ---
            var ai = (GameObject)PrefabUtility.InstantiatePrefab(aiPrefab);
            ai.transform.position = new Vector3(0, -52, 0);
            ai.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.down);
            Undo.RegisterCreatedObjectUndo(ai, "Create AI");

            var aiComp = ai.GetComponent<AIOpponent>();
            if (aiComp != null)
            {
                var so = new SerializedObject(aiComp);
                so.FindProperty("planet").objectReferenceValue = planet.transform;
                so.FindProperty("trashLayer").intValue = 1 << 8;
                if (projectilePrefab != null)
                    so.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
                var aiFirePoint = ai.transform.Find("FirePoint");
                if (aiFirePoint != null)
                    so.FindProperty("firePoint").objectReferenceValue = aiFirePoint;
                so.ApplyModifiedProperties();
            }

            // --- Trash Spawner ---
            var spawnerGo = new GameObject("TrashSpawner");
            var spawner = spawnerGo.AddComponent<TrashSpawner>();
            Undo.RegisterCreatedObjectUndo(spawnerGo, "Create TrashSpawner");

            var spawnerSo = new SerializedObject(spawner);
            spawnerSo.FindProperty("planet").objectReferenceValue = planet.transform;
            spawnerSo.FindProperty("planetRadius").floatValue = 50f;
            var trashArray = spawnerSo.FindProperty("trashPrefabs");
            trashArray.arraySize = 3;
            if (trashA != null) trashArray.GetArrayElementAtIndex(0).objectReferenceValue = trashA;
            if (trashB != null) trashArray.GetArrayElementAtIndex(1).objectReferenceValue = trashB;
            if (trashC != null) trashArray.GetArrayElementAtIndex(2).objectReferenceValue = trashC;
            spawnerSo.ApplyModifiedProperties();

            // --- Camera ---
            var cam = GameObject.Find("Main Camera");
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 70, -15);
                cam.transform.LookAt(player.transform);

                var sphericalCam = cam.GetComponent<SphericalCamera>();
                if (sphericalCam == null)
                    sphericalCam = Undo.AddComponent<SphericalCamera>(cam);

                var camSo = new SerializedObject(sphericalCam);
                camSo.FindProperty("target").objectReferenceValue = player.transform;
                camSo.FindProperty("planet").objectReferenceValue = planet.transform;
                camSo.ApplyModifiedProperties();
            }

            // --- LevelSetup ---
            var levelGo = new GameObject("LevelSetup");
            var levelSetup = levelGo.AddComponent<LevelSetup>();
            Undo.RegisterCreatedObjectUndo(levelGo, "Create LevelSetup");

            var lsSo = new SerializedObject(levelSetup);
            lsSo.FindProperty("planet").objectReferenceValue = planet.transform;
            lsSo.FindProperty("player").objectReferenceValue = pc;
            lsSo.FindProperty("sphericalCamera").objectReferenceValue = cam?.GetComponent<SphericalCamera>();
            lsSo.FindProperty("aiOpponent").objectReferenceValue = aiComp;
            lsSo.ApplyModifiedProperties();

            // --- HUD Canvas ---
            CreateHUDCanvas();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[SceneSetup] Gameplay scene setup complete!");
        }

        #region Prefab Creation

        private static void CreateProjectilePrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile";
            go.layer = 9; // Projectile
            go.tag = "Projectile";
            go.transform.localScale = Vector3.one * 0.3f;

            // Collider as trigger
            var col = go.GetComponent<SphereCollider>();
            col.isTrigger = true;

            // Rigidbody (kinematic = false, no gravity, for velocity-based movement)
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;

            // Projectile script
            var proj = go.AddComponent<Projectile>();
            // Set hitLayers to Player(6) + Enemy(7)
            var so = new SerializedObject(proj);
            so.FindProperty("hitLayers").intValue = (1 << 6) | (1 << 7);
            so.ApplyModifiedProperties();

            // Material
            ApplyMaterial(go, "Projectile_Mat");

            SavePrefab(go, $"{PrefabRoot}/Projectiles/Projectile.prefab");
            Object.DestroyImmediate(go);
            Debug.Log("  Created Projectile prefab");
        }

        private static void CreateTrashPrefabs()
        {
            CreateSingleTrashPrefab("Trash_A", PrimitiveType.Cube, "Trash_A_Mat", 0.5f);
            CreateSingleTrashPrefab("Trash_B", PrimitiveType.Cylinder, "Trash_B_Mat", 0.4f);
            CreateSingleTrashPrefab("Trash_C", PrimitiveType.Capsule, "Trash_C_Mat", 0.35f);
        }

        private static void CreateSingleTrashPrefab(string name, PrimitiveType shape, string matName, float scale)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            go.layer = 8; // Trash
            go.tag = "Trash";
            go.transform.localScale = Vector3.one * scale;

            // Collider stays as-is (non-trigger, for physics collision)
            // Add Rigidbody (kinematic so trash doesn't fall)
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            // TrashPickup component
            go.AddComponent<TrashPickup>();

            ApplyMaterial(go, matName);

            SavePrefab(go, $"{PrefabRoot}/Trash/{name}.prefab");
            Object.DestroyImmediate(go);
            Debug.Log($"  Created {name} prefab");
        }

        private static void CreatePlayerShipPrefab()
        {
            // Root object (capsule as placeholder ship body)
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "PlayerShip";
            go.layer = 6; // Player
            go.tag = "PlayerShip";
            go.transform.localScale = new Vector3(1f, 0.5f, 1.5f);

            // Rotate capsule collider to face forward (Z axis)
            var capsuleCol = go.GetComponent<CapsuleCollider>();
            capsuleCol.direction = 2; // Z-axis

            ApplyMaterial(go, "Player_Mat");

            // Add components
            go.AddComponent<SphericalMovement>();
            go.AddComponent<PlayerController>();
            go.AddComponent<ShootingSystem>();
            go.AddComponent<VacuumCollector>();
            go.AddComponent<Health>();

            // Add Rigidbody (kinematic — movement is handled manually)
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            // FirePoint child
            var firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(go.transform);
            firePoint.transform.localPosition = new Vector3(0, 0, 1.2f); // front of ship
            firePoint.transform.localRotation = Quaternion.identity;

            SavePrefab(go, $"{PrefabRoot}/Player/PlayerShip.prefab");
            Object.DestroyImmediate(go);
            Debug.Log("  Created PlayerShip prefab");
        }

        private static void CreateAIShipPrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "AIShip";
            go.layer = 7; // Enemy
            go.tag = "EnemyShip";
            go.transform.localScale = new Vector3(1f, 0.5f, 1.5f);

            var capsuleCol = go.GetComponent<CapsuleCollider>();
            capsuleCol.direction = 2; // Z-axis

            ApplyMaterial(go, "Enemy_Mat");

            // Components
            go.AddComponent<Health>();
            go.AddComponent<AIOpponent>();

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            // Vacuum trigger (sphere collider)
            var vacTrigger = go.AddComponent<SphereCollider>();
            vacTrigger.isTrigger = true;
            vacTrigger.radius = 3f;

            // FirePoint child
            var firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(go.transform);
            firePoint.transform.localPosition = new Vector3(0, 0, 1.2f);
            firePoint.transform.localRotation = Quaternion.identity;

            SavePrefab(go, $"{PrefabRoot}/Enemies/AIShip.prefab");
            Object.DestroyImmediate(go);
            Debug.Log("  Created AIShip prefab");
        }

        private static void CreatePlanetPrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Planet";
            go.layer = 10; // Planet
            go.tag = "Planet";
            go.transform.localScale = Vector3.one * 100f; // radius 50 (scale 100 for diameter)
            go.isStatic = true;

            ApplyMaterial(go, "Planet_Mat");

            SavePrefab(go, $"{PrefabRoot}/Planets/Planet.prefab");
            Object.DestroyImmediate(go);
            Debug.Log("  Created Planet prefab");
        }

        #endregion

        #region HUD

        private static void CreateHUDCanvas()
        {
            // Canvas
            var canvasGo = new GameObject("HUD Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create HUD");

            // GameplayHUD component
            var hud = canvasGo.AddComponent<GameplayHUD>();

            // --- Ammo Text (top-right) ---
            var ammoText = CreateTMPText("AmmoText", canvasGo.transform,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -50),
                new Vector2(150, 50), "0", 36, TextAlignmentOptions.Right);

            // Ammo label
            CreateTMPText("AmmoLabel", canvasGo.transform,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -20),
                new Vector2(150, 30), "AMMO", 18, TextAlignmentOptions.Right);

            // --- Cleanup Bar (top-center) ---
            var cleanupPanel = new GameObject("CleanupPanel");
            cleanupPanel.transform.SetParent(canvasGo.transform, false);
            var cleanupRect = cleanupPanel.AddComponent<RectTransform>();
            cleanupRect.anchorMin = new Vector2(0.5f, 1);
            cleanupRect.anchorMax = new Vector2(0.5f, 1);
            cleanupRect.anchoredPosition = new Vector2(0, -40);
            cleanupRect.sizeDelta = new Vector2(300, 30);

            var cleanupBar = CreateSlider("CleanupBar", cleanupPanel.transform, new Vector2(300, 20));

            var cleanupText = CreateTMPText("CleanupText", cleanupPanel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20),
                new Vector2(100, 25), "0%", 16, TextAlignmentOptions.Center);

            // --- Level Complete Panel (hidden by default) ---
            var lcPanel = new GameObject("LevelCompletePanel");
            lcPanel.transform.SetParent(canvasGo.transform, false);
            var lcRect = lcPanel.AddComponent<RectTransform>();
            lcRect.anchorMin = new Vector2(0.5f, 0.5f);
            lcRect.anchorMax = new Vector2(0.5f, 0.5f);
            lcRect.sizeDelta = new Vector2(400, 200);
            lcPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);

            CreateTMPText("CompleteText", lcPanel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(380, 100), "LEVEL COMPLETE!", 40, TextAlignmentOptions.Center);

            lcPanel.SetActive(false);

            // Wire HUD references via SerializedObject
            var so = new SerializedObject(hud);
            so.FindProperty("ammoText").objectReferenceValue = ammoText;
            so.FindProperty("cleanupBar").objectReferenceValue = cleanupBar;
            so.FindProperty("cleanupPercentText").objectReferenceValue = cleanupText;
            so.FindProperty("levelCompletePanel").objectReferenceValue = lcPanel;
            so.ApplyModifiedProperties();

            Debug.Log("  Created HUD Canvas");
        }

        #endregion

        #region Helpers

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder("Assets/_Project/Prefabs/Player");
            EnsureFolder("Assets/_Project/Prefabs/Enemies");
            EnsureFolder("Assets/_Project/Prefabs/Projectiles");
            EnsureFolder("Assets/_Project/Prefabs/Trash");
            EnsureFolder("Assets/_Project/Prefabs/Planets");
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts = path.Split('/');
                var current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }

        private static void ApplyMaterial(GameObject go, string matName)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/{matName}.mat");
            if (mat != null)
            {
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = mat;
            }
        }

        private static void SavePrefab(GameObject go, string path)
        {
            var dir = System.IO.Path.GetDirectoryName(path);
            EnsureFolder(dir.Replace("\\", "/"));
            PrefabUtility.SaveAsPrefabAsset(go, path);
        }

        private static TextMeshProUGUI CreateTMPText(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
            Vector2 size, string text, float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        private static Slider CreateSlider(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            var slider = go.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0;
            slider.interactable = false;

            // Background
            var bg = new GameObject("Background");
            bg.transform.SetParent(go.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Fill area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fillRect = fillArea.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fRect = fill.AddComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero;
            fRect.anchorMax = Vector2.one;
            fRect.sizeDelta = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.3f, 0.8f, 0.3f, 1f);

            slider.fillRect = fRect;

            return slider;
        }

        #endregion
    }
}
