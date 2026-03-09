using UnityEngine;
using UnityEditor;

namespace SpaceCleaner.Editor
{
    public static class ShipShowcase
    {
        [MenuItem("SpaceCleaner/Show Ships Showcase")]
        public static void CreateShowcase()
        {
            // Clean up any previous showcase
            var old = GameObject.Find("___ShipShowcase___");
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject("___ShipShowcase___");

            string[] shipFiles = new[]
            {
                "Assets/_Project/Models/Ships/craft_speederA.fbx",
                "Assets/_Project/Models/Ships/craft_speederB.fbx",
                "Assets/_Project/Models/Ships/craft_speederC.fbx",
                "Assets/_Project/Models/Ships/craft_speederD.fbx",
                "Assets/_Project/Models/Ships/craft_racer.fbx",
                "Assets/_Project/Models/Ships/craft_miner.fbx",
                "Assets/_Project/Models/Ships/craft_cargoA.fbx",
                "Assets/_Project/Models/Ships/craft_cargoB.fbx",
            };

            // 2 rows of 4, well spaced
            int cols = 4;
            float spacingX = 5f;
            float spacingZ = 6f;

            for (int i = 0; i < shipFiles.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(shipFiles[i]);
                if (prefab == null)
                {
                    Debug.LogWarning($"Could not load: {shipFiles[i]}");
                    continue;
                }

                int col = i % cols;
                int row = i / cols;
                var pos = new Vector3(col * spacingX, 0f, -row * spacingZ);

                // Instantiate ship
                var ship = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                ship.transform.parent = root.transform;
                ship.transform.position = pos;
                ship.transform.rotation = Quaternion.Euler(0, 180, 0); // face camera
                ship.name = prefab.name;

                // Create label above
                var labelGO = new GameObject($"Label_{prefab.name}");
                labelGO.transform.parent = root.transform;
                labelGO.transform.position = pos + Vector3.up * 2.5f;

                var tm = labelGO.AddComponent<TextMesh>();
                tm.text = prefab.name;
                tm.fontSize = 48;
                tm.characterSize = 0.15f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = Color.white;
                tm.fontStyle = FontStyle.Bold;
            }

            // Frame the showcase in scene view
            Selection.activeGameObject = root;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log("Ship showcase created! Select ___ShipShowcase___ in hierarchy.");
        }

        [MenuItem("SpaceCleaner/Remove Ships Showcase")]
        public static void RemoveShowcase()
        {
            // Also remove individually placed ships from earlier
            string[] shipNames = { "craft_speederA", "craft_speederB", "craft_speederC", "craft_speederD",
                                   "craft_racer", "craft_miner", "craft_cargoA", "craft_cargoB" };
            foreach (var name in shipNames)
            {
                var go = GameObject.Find(name);
                if (go != null) Object.DestroyImmediate(go);
            }

            var old = GameObject.Find("___ShipShowcase___");
            if (old != null)
            {
                Object.DestroyImmediate(old);
                Debug.Log("Ship showcase removed.");
            }
        }
    }
}
