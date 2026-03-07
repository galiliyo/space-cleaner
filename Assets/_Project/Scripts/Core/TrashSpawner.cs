using System.Collections;
using UnityEngine;

namespace SpaceCleaner.Core
{
    public class TrashSpawner : MonoBehaviour
    {
        [Header("Planet")]
        [SerializeField] private Transform planet;
        [SerializeField] private float planetRadius = 50f;
        [SerializeField] private float spawnHeight = 1f; // height above surface

        [Header("Spawning")]
        [SerializeField] private GameObject[] trashPrefabs;
        [SerializeField] private int spawnCount = 200;
        [SerializeField] private Transform trashParent;

        [Header("Clustering")]
        [Tooltip("Number of cluster centers distributed across the entire planet surface")]
        [SerializeField] private int clusterCount = 20;
        [Tooltip("Half-angle in degrees of each cluster's spread (~8° ≈ 80 units arc at orbit radius)")]
        [SerializeField] private float clusterSpreadAngle = 8f;
        [Tooltip("Max height jitter above/below spawnHeight")]
        [SerializeField] private float heightJitter = 2f;

        [Header("Scale Variation")]
        [SerializeField] private float minScale = 0.85f;
        [SerializeField] private float maxScale = 1.15f;

        private int activeTrashCount;

        /// <summary>One pool per trash prefab variant, created at runtime.</summary>
        private ObjectPool[] trashPools;

        public int ActiveTrashCount => activeTrashCount;

        private void Start()
        {
            InitializePools();
            SpawnTrash();
        }

        /// <summary>
        /// Creates one ObjectPool per trash prefab variant so the spawner and
        /// TrashPickup/AIOpponent can all share the same pools via the static registry.
        /// </summary>
        private void InitializePools()
        {
            if (trashPrefabs == null || trashPrefabs.Length == 0) return;

            if (trashParent == null)
            {
                var go = new GameObject("TrashContainer");
                trashParent = go.transform;
            }

            trashPools = new ObjectPool[trashPrefabs.Length];

            for (int i = 0; i < trashPrefabs.Length; i++)
            {
                // Skip if a pool was already registered for this prefab (e.g. placed in the scene)
                if (ObjectPool.GetPoolForPrefab(trashPrefabs[i]) != null)
                {
                    trashPools[i] = ObjectPool.GetPoolForPrefab(trashPrefabs[i]);
                    continue;
                }

                // Create the GO inactive so Awake doesn't fire before we configure the pool
                var poolGO = new GameObject($"TrashPool_{trashPrefabs[i].name}");
                poolGO.SetActive(false);
                poolGO.transform.SetParent(trashParent);

                var pool = poolGO.AddComponent<ObjectPool>();

                // Configure via runtime initializer (sets prefab, size, parent and pre-warms)
                int preWarmCount = Mathf.CeilToInt((float)spawnCount / trashPrefabs.Length);
                pool.InitializeRuntime(trashPrefabs[i], preWarmCount, trashParent);

                poolGO.SetActive(true); // Awake will no-op since already initialized

                trashPools[i] = pool;
            }
        }

        public void SpawnTrash()
        {
            if (trashPrefabs == null || trashPrefabs.Length == 0) return;

            if (trashParent == null)
            {
                var go = new GameObject("TrashContainer");
                trashParent = go.transform;
            }

            activeTrashCount = spawnCount;
            GameManager.Instance?.InitializeLevel(spawnCount);

            StartCoroutine(SpawnTrashCoroutine());
        }

        private IEnumerator SpawnTrashCoroutine()
        {
            // Generate cluster centers uniformly across the entire planet surface
            Vector3[] clusterCenters = new Vector3[clusterCount];
            for (int c = 0; c < clusterCount; c++)
            {
                clusterCenters[c] = Random.onUnitSphere;
            }

            float cosClusterAngle = Mathf.Cos(clusterSpreadAngle * Mathf.Deg2Rad);

            for (int i = 0; i < spawnCount; i++)
            {
                // Pick a random cluster center
                Vector3 cluster = clusterCenters[Random.Range(0, clusterCount)];

                // Random direction within that cluster's spread
                Vector3 randomDir = RandomDirectionInCap(cluster, cosClusterAngle);

                // Spawn at orbit height with small jitter
                float height = spawnHeight + Random.Range(-heightJitter, heightJitter);
                Vector3 spawnPos = planet.position + randomDir * (planetRadius + height);

                // Random variant
                int variant = Random.Range(0, trashPrefabs.Length);

                // Get from pool or fallback to Instantiate
                GameObject trash;
                if (trashPools != null && trashPools[variant] != null)
                {
                    trash = trashPools[variant].Get(spawnPos, Quaternion.identity);
                }
                else
                {
                    trash = Instantiate(trashPrefabs[variant], spawnPos, Quaternion.identity, trashParent);
                }

                // Align to surface
                Vector3 up = randomDir;
                Vector3 randomForward = Vector3.Cross(up, Random.onUnitSphere).normalized;
                if (randomForward.sqrMagnitude < 0.001f)
                    randomForward = Vector3.Cross(up, Vector3.right).normalized;
                trash.transform.rotation = Quaternion.LookRotation(randomForward, up);

                // Random scale variation (tight range)
                float scale = Random.Range(minScale, maxScale);
                trash.transform.localScale = Vector3.one * scale;

                if ((i + 1) % 20 == 0)
                    yield return null; // spread across frames
            }
        }

        private Vector3 RandomDirectionInCap(Vector3 center, float cosAngle)
        {
            // Uniform random point on a spherical cap using cos-based sampling
            float z = Random.Range(cosAngle, 1f);
            float phi = Random.Range(0f, 2f * Mathf.PI);
            float sinTheta = Mathf.Sqrt(1f - z * z);

            // Local direction (cap centered on +Z)
            Vector3 localDir = new Vector3(sinTheta * Mathf.Cos(phi), sinTheta * Mathf.Sin(phi), z);

            // Rotate from +Z to capCenter
            Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, center);
            return rotation * localDir;
        }
    }
}
