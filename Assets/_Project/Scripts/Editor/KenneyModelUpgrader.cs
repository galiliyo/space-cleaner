using UnityEditor;
using UnityEngine;

namespace SpaceCleaner.EditorTools
{
    public static class KenneyModelUpgrader
    {
        private struct ModelMapping
        {
            public string prefabPath;
            public string fbxPath;
            public Vector3 scale;
        }

        [MenuItem("SpaceCleaner/Upgrade to Kenney Models")]
        public static void UpgradeAll()
        {
            var mappings = new[]
            {
                new ModelMapping
                {
                    prefabPath = "Assets/_Project/Prefabs/Player/PlayerShip.prefab",
                    fbxPath = "Assets/_Project/Models/Ships/craft_speederA.fbx",
                    scale = new Vector3(1.5f, 1.5f, 1.5f)
                },
                new ModelMapping
                {
                    prefabPath = "Assets/_Project/Prefabs/Enemies/AIShip.prefab",
                    fbxPath = "Assets/_Project/Models/Ships/craft_miner.fbx",
                    scale = new Vector3(1.5f, 1.5f, 1.5f)
                },
                new ModelMapping
                {
                    prefabPath = "Assets/_Project/Prefabs/Trash/Trash_A.prefab",
                    fbxPath = "Assets/_Project/Models/Trash/barrel.fbx",
                    scale = new Vector3(0.5f, 0.5f, 0.5f)
                },
                new ModelMapping
                {
                    prefabPath = "Assets/_Project/Prefabs/Trash/Trash_B.prefab",
                    fbxPath = "Assets/_Project/Models/Trash/satelliteDish_detailed.fbx",
                    scale = new Vector3(0.4f, 0.4f, 0.4f)
                },
                new ModelMapping
                {
                    prefabPath = "Assets/_Project/Prefabs/Trash/Trash_C.prefab",
                    fbxPath = "Assets/_Project/Models/Trash/meteor_half.fbx",
                    scale = new Vector3(0.35f, 0.35f, 0.35f)
                },
                new ModelMapping
                {
                    prefabPath = "Assets/_Project/Prefabs/Projectiles/Projectile.prefab",
                    fbxPath = "Assets/_Project/Models/Trash/rocks_smallA.fbx",
                    scale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            };

            int upgraded = 0;
            foreach (var mapping in mappings)
            {
                if (UpgradePrefab(mapping.prefabPath, mapping.fbxPath, mapping.scale))
                    upgraded++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[KenneyUpgrader] Upgraded {upgraded}/{mappings.Length} prefabs to Kenney models.");
        }

        private static bool UpgradePrefab(string prefabPath, string fbxPath, Vector3 scale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[KenneyUpgrader] Prefab not found: {prefabPath}");
                return false;
            }

            Mesh mesh = FindMeshInFBX(fbxPath);
            if (mesh == null)
            {
                Debug.LogWarning($"[KenneyUpgrader] No mesh found in: {fbxPath}");
                return false;
            }

            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            var mf = contents.GetComponent<MeshFilter>();
            if (mf == null)
            {
                Debug.LogWarning($"[KenneyUpgrader] No MeshFilter on: {prefabPath}");
                PrefabUtility.UnloadPrefabContents(contents);
                return false;
            }

            mf.sharedMesh = mesh;
            contents.transform.localScale = scale;

            // Fill ALL material slots with the prefab's existing material
            // (FBX meshes often have multiple submeshes needing a material each)
            var mr = contents.GetComponent<MeshRenderer>();
            if (mr != null && mesh.subMeshCount > 1)
            {
                var baseMat = mr.sharedMaterial;
                var mats = new Material[mesh.subMeshCount];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = baseMat;
                mr.sharedMaterials = mats;
            }

            // Fix FirePoint child scale if present
            var firePoint = contents.transform.Find("FirePoint");
            if (firePoint != null)
                firePoint.localScale = Vector3.one;

            FitCollider(contents, mesh);

            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);

            Debug.Log($"[KenneyUpgrader] Upgraded: {prefabPath} → {fbxPath}");
            return true;
        }

        private static Mesh FindMeshInFBX(string fbxPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in assets)
            {
                if (asset is Mesh m && !m.name.Contains("__preview"))
                    return m;
            }
            return null;
        }

        private static void FitCollider(GameObject go, Mesh mesh)
        {
            var bounds = mesh.bounds;

            var box = go.GetComponent<BoxCollider>();
            if (box != null)
            {
                box.center = bounds.center;
                box.size = bounds.size;
                return;
            }

            var capsule = go.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.center = bounds.center;
                float maxXZ = Mathf.Max(bounds.extents.x, bounds.extents.z);
                capsule.radius = maxXZ;
                capsule.height = bounds.size.y;
                capsule.direction = 1; // Y-axis
                return;
            }

            var sphere = go.GetComponent<SphereCollider>();
            if (sphere != null)
            {
                sphere.center = bounds.center;
                sphere.radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            }
        }
    }
}
