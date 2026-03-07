using System.Collections.Generic;
using UnityEngine;

namespace SpaceCleaner.Core
{
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialSize = 20;
        [SerializeField] private Transform poolParent;

        private readonly Queue<GameObject> pool = new();
        private bool initialized;

        // --- Static registry so pooled objects can find their pool ---
        private static readonly Dictionary<GameObject, ObjectPool> prefabToPool = new();
        private static readonly Dictionary<GameObject, ObjectPool> instanceToPool = new();

        private void Awake()
        {
            if (initialized) return; // Already set up via InitializeRuntime
            Initialize();
        }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;

            if (poolParent == null)
                poolParent = transform;

            // Register this pool by its prefab
            if (prefab != null)
                prefabToPool[prefab] = this;

            for (int i = 0; i < initialSize; i++)
                AddToPool(CreateInstance());
        }

        /// <summary>
        /// Configure and initialize a pool created at runtime (e.g. via AddComponent)
        /// where serialized fields cannot be set before Awake.
        /// Call this BEFORE activating the GameObject if added to an inactive GO,
        /// or right after AddComponent on an active GO (Awake will skip if already initialized).
        /// </summary>
        public void InitializeRuntime(GameObject poolPrefab, int size, Transform parent)
        {
            prefab = poolPrefab;
            initialSize = size;
            poolParent = parent;
            Initialize();
        }

        private void OnDestroy()
        {
            // Clean up static references
            if (prefab != null && prefabToPool.ContainsKey(prefab) && prefabToPool[prefab] == this)
                prefabToPool.Remove(prefab);

            // Remove all instance mappings that belong to this pool
            var toRemove = new List<GameObject>();
            foreach (var kvp in instanceToPool)
            {
                if (kvp.Value == this)
                    toRemove.Add(kvp.Key);
            }
            foreach (var key in toRemove)
                instanceToPool.Remove(key);
        }

        /// <summary>
        /// Get the pool registered for a given prefab. Returns null if no pool exists.
        /// </summary>
        public static ObjectPool GetPoolForPrefab(GameObject prefab)
        {
            prefabToPool.TryGetValue(prefab, out var pool);
            return pool;
        }

        /// <summary>
        /// Get the pool that owns a given instance. Returns null if the instance was not pooled.
        /// </summary>
        public static ObjectPool GetPoolForInstance(GameObject instance)
        {
            instanceToPool.TryGetValue(instance, out var pool);
            return pool;
        }

        /// <summary>
        /// Convenience: return an instance to its owning pool, or Destroy it if not pooled.
        /// </summary>
        public static void ReturnOrDestroy(GameObject instance)
        {
            if (instance == null) return;

            if (instanceToPool.TryGetValue(instance, out var pool) && pool != null)
                pool.Return(instance);
            else
                Destroy(instance);
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj = pool.Count > 0 ? pool.Dequeue() : CreateInstance();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        public void Return(GameObject obj)
        {
            obj.SetActive(false);
            AddToPool(obj);
        }

        private GameObject CreateInstance()
        {
            GameObject obj = Instantiate(prefab, poolParent);
            obj.SetActive(false);
            instanceToPool[obj] = this;
            return obj;
        }

        private void AddToPool(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(poolParent);
            pool.Enqueue(obj);
        }
    }
}
