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

            for (int i = 0; i < spawnCount; i++)
            {
                // Random point on sphere using uniform distribution
                Vector3 randomDir = Random.onUnitSphere;
                Vector3 spawnPos = planet.position + randomDir * (planetRadius + spawnHeight);

                // Random variant
                int variant = Random.Range(0, trashPrefabs.Length);
                GameObject trash = Instantiate(trashPrefabs[variant], spawnPos, Quaternion.identity, trashParent);

                // Align to surface
                Vector3 up = randomDir;
                Vector3 randomForward = Vector3.Cross(up, Random.onUnitSphere).normalized;
                if (randomForward.sqrMagnitude < 0.001f)
                    randomForward = Vector3.Cross(up, Vector3.right).normalized;
                trash.transform.rotation = Quaternion.LookRotation(randomForward, up);

                // Random scale variation
                float scale = Random.Range(0.7f, 1.3f);
                trash.transform.localScale = Vector3.one * scale;
            }
        }
    }
}
