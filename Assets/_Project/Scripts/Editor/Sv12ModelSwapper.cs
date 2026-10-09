using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SpaceCleaner.Player;

namespace SpaceCleaner.EditorTools
{
    /// <summary>
    /// Swaps the placeholder Kenney visuals for the sv12 tug, xr alien and sv12 junk GLBs.
    /// Root objects keep all gameplay components; only the visual becomes a "Model" child.
    /// Safe to re-run: any existing "Model" child is replaced.
    /// </summary>
    public static class Sv12ModelSwapper
    {
        private const string TugGlb = "Assets/_Project/Models/Ships/sv12_sweeper_tug.glb";
        private const string AlienGlb = "Assets/_Project/Models/Characters/xr_hostile_alien.glb";
        private const string JunkGlb = "Assets/_Project/Models/Trash/sv12_space_junk.glb";

        private const string PlayerPrefab = "Assets/_Project/Prefabs/Player/PlayerShip.prefab";
        private const string AiPrefab = "Assets/_Project/Prefabs/Enemies/AIShip.prefab";
        private const string TrashTemplate = "Assets/_Project/Prefabs/Trash/Trash_A.prefab";
        private const string TrashDir = "Assets/_Project/Prefabs/Trash";

        // World-space length of the longest model axis, in units (trash spawner scales 0.85-1.15x on top).
        private const float PlayerSize = 6.0f; // 2x the original 3.0
        private const float AlienSize = 3.6f;

        private const float JunkScale = 2f; // multiplier on the sizes below (2x bigger trash)

        private static readonly (string node, float size)[] Junk =
        {
            ("junk_crushed_can", 0.8f),
            ("junk_satellite_panel", 1.3f),
            ("junk_scrap", 0.7f),
            ("junk_broken_satellite", 1.4f),
            ("junk_broken_sputnik", 1.1f),
            ("junk_meteorite_large", 1.4f),
            ("junk_meteorite_small", 0.8f),
            ("junk_meteorite_shard", 0.6f),
            ("junk_hull_plate", 1.2f),
            ("junk_wing_fragment", 1.3f),
            ("junk_engine_nozzle", 1.0f),
        };

        [MenuItem("SpaceCleaner/Apply sv12 Models")]
        public static void ApplyAll()
        {
            SwapShip(PlayerPrefab, TugGlb, PlayerSize);
            SwapShip(AiPrefab, AlienGlb, AlienSize);
            BuildJunkPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Sv12] Model swap complete.");
        }

        [MenuItem("SpaceCleaner/Assign sv12 Junk To Spawner")]
        public static void AssignJunkToSpawner()
        {
            var spawner = Object.FindFirstObjectByType<SpaceCleaner.Core.TrashSpawner>();
            if (spawner == null) { Debug.LogError("[Sv12] No TrashSpawner in the open scene."); return; }

            var so = new SerializedObject(spawner);
            var arr = so.FindProperty("trashPrefabs");
            arr.arraySize = Junk.Length;
            int assigned = 0;
            for (int i = 0; i < Junk.Length; i++)
            {
                string path = $"{TrashDir}/Trash_{Junk[i].node.Substring("junk_".Length)}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                arr.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
                if (prefab != null) assigned++;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(spawner);
            Debug.Log($"[Sv12] TrashSpawner.trashPrefabs = {arr.arraySize} entries, {assigned} resolved.");
        }

        private static void SwapShip(string prefabPath, string glbPath, float targetSize)
        {
            var glb = AssetDatabase.LoadAssetAtPath<GameObject>(glbPath);
            if (glb == null) { Debug.LogError($"[Sv12] GLB not imported: {glbPath}"); return; }

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                RemoveRootVisual(root);
                var old = root.transform.Find("Model");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var oldVisual = root.transform.Find("ShipVisual");
                if (oldVisual != null) Object.DestroyImmediate(oldVisual.gameObject);

                // Hull centre stays where the old collider centre was.
                Vector3 centreLocal = Vector3.zero;
                var cap = root.GetComponent<CapsuleCollider>();
                if (cap != null && prefabPath == PlayerPrefab)
                {
                    // Absolute values (original 1.05 / 0.8 / 0.4 doubled) so re-running never compounds.
                    cap.radius = 2.1f; cap.height = 1.6f; cap.center = new Vector3(0f, 0.8f, 0f);
                }
                var sph = root.GetComponent<SphereCollider>();
                if (cap != null) centreLocal = cap.center;
                else if (sph != null) centreLocal = sph.center;

                // The player's ShipFeedback/ShootingJuice/VacuumJuice look for a child named "ShipVisual"
                // and reset its local pose to zero. Wrap the model in an empty one so the model keeps its offset.
                Transform parent = root.transform;
                if (prefabPath == PlayerPrefab)
                {
                    var holder = new GameObject("ShipVisual");
                    holder.transform.SetParent(root.transform, false);
                    parent = holder.transform;
                }
                var model = (GameObject)PrefabUtility.InstantiatePrefab(glb, parent);
                model.name = "Model";
                var b = Fit(root.transform, model.transform, targetSize, centreLocal);

                var fire = root.transform.Find("FirePoint");
                if (fire != null)
                    fire.position = new Vector3(b.center.x, b.center.y, b.max.z);

                if (root.GetComponent<ShipDetailAnimator>() == null)
                    root.AddComponent<ShipDetailAnimator>();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[Sv12] {prefabPath}: model size {b.size}");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildJunkPrefabs()
        {
            var glb = AssetDatabase.LoadAssetAtPath<GameObject>(JunkGlb);
            if (glb == null) { Debug.LogError($"[Sv12] GLB not imported: {JunkGlb}"); return; }

            foreach (var (node, size) in Junk)
            {
                string path = $"{TrashDir}/Trash_{node.Substring("junk_".Length)}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                    AssetDatabase.CopyAsset(TrashTemplate, path);

                var root = PrefabUtility.LoadPrefabContents(path);
                GameObject temp = null;
                try
                {
                    RemoveRootVisual(root);
                    var old = root.transform.Find("Model");
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                    root.transform.localScale = Vector3.one; // spawner sets scale itself
                    root.name = System.IO.Path.GetFileNameWithoutExtension(path);

                    temp = (GameObject)PrefabUtility.InstantiatePrefab(glb);
                    PrefabUtility.UnpackPrefabInstance(temp, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    var piece = temp.transform.Find(node);
                    if (piece == null) { Debug.LogError($"[Sv12] Node missing in junk GLB: {node}"); continue; }

                    piece.SetParent(root.transform, false);
                    piece.name = "Model";
                    var b = Fit(root.transform, piece, size * JunkScale, Vector3.zero);

                    var box = root.GetComponent<BoxCollider>();
                    if (box == null) box = root.AddComponent<BoxCollider>();
                    box.center = root.transform.InverseTransformPoint(b.center);
                    box.size = b.size;

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"[Sv12] {path}: size {b.size}");
                }
                finally
                {
                    if (temp != null) Object.DestroyImmediate(temp);
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void RemoveRootVisual(GameObject root)
        {
            var mr = root.GetComponent<MeshRenderer>();
            if (mr != null) Object.DestroyImmediate(mr);
            var mf = root.GetComponent<MeshFilter>();
            if (mf != null) Object.DestroyImmediate(mf);
        }

        /// <summary>Scales the model so its longest world axis equals targetSize and centres it on centreLocal.</summary>
        private static Bounds Fit(Transform root, Transform model, float targetSize, Vector3 centreLocal)
        {
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
            model.localScale = Vector3.one;

            var b = WorldBounds(model);
            float longest = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (longest > 0.0001f)
                model.localScale = Vector3.one * (targetSize / longest);

            b = WorldBounds(model);
            model.position += root.TransformPoint(centreLocal) - b.center;
            return WorldBounds(model);
        }

        private static Bounds WorldBounds(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(t.position, Vector3.zero);
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }
    }
}
