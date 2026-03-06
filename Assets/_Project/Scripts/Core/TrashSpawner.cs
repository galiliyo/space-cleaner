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
        [SerializeField] private int spawnCount = 80;
        [SerializeField] private Transform trashParent;

        [Header("Spawn Region")]
        [Tooltip("Half-angle in degrees of the spherical cap around the player where trash spawns")]
        [SerializeField] private float spawnCapAngle = 30f;

        [Header("Clustering")]
        [Tooltip("Number of cluster centers to group trash around")]
        [SerializeField] private int clusterCount = 12;
        [Tooltip("Half-angle in degrees of each cluster's spread")]
        [SerializeField] private float clusterSpreadAngle = 5f;
        [Tooltip("Max height jitter above/below spawnHeight")]
        [SerializeField] private float heightJitter = 2f;

        [Header("Scale Variation")]
        [SerializeField] private float minScale = 0.85f;
        [SerializeField] private float maxScale = 1.15f;

        private int activeTrashCount;

        public int ActiveTrashCount => activeTrashCount;

        private void Start()
        {
            SpawnTrash();
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

            // Find the player's starting direction to center the spawn cap
            var player = FindAnyObjectByType<SpaceCleaner.Player.PlayerController>();
            Vector3 capCenter = Vector3.up; // default
            if (player != null && planet != null)
                capCenter = (player.transform.position - planet.position).normalized;

            float cosCapAngle = Mathf.Cos(spawnCapAngle * Mathf.Deg2Rad);

            // Generate cluster centers within the spawn cap
            Vector3[] clusterCenters = new Vector3[clusterCount];
            for (int c = 0; c < clusterCount; c++)
            {
                clusterCenters[c] = RandomDirectionInCap(capCenter, cosCapAngle);
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
                GameObject trash = Instantiate(trashPrefabs[variant], spawnPos, Quaternion.identity, trashParent);

                // Align to surface
                Vector3 up = randomDir;
                Vector3 randomForward = Vector3.Cross(up, Random.onUnitSphere).normalized;
                if (randomForward.sqrMagnitude < 0.001f)
                    randomForward = Vector3.Cross(up, Vector3.right).normalized;
                trash.transform.rotation = Quaternion.LookRotation(randomForward, up);

                // Random scale variation (tight range)
                float scale = Random.Range(minScale, maxScale);
                trash.transform.localScale = Vector3.one * scale;
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
