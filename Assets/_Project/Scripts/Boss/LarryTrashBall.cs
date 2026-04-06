using UnityEngine;
using SpaceCleaner.Core;

namespace SpaceCleaner.Boss
{
    [RequireComponent(typeof(Rigidbody))]
    public class LarryTrashBall : MonoBehaviour
    {
        [SerializeField] private float lifetime = 8f;
        [SerializeField] private int playerDamage = 5;
        [SerializeField] private int landingAmmoValue = 10;

        private float timer;
        private int shooterLayer = -1;

        public void SetShooterLayer(int layer) => shooterLayer = layer;

        private void OnEnable()
        {
            timer = lifetime;
            shooterLayer = -1;

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (TryGetComponent<TrailRenderer>(out var trail))
                trail.Clear();
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                ObjectPool.ReturnOrDestroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            int otherLayer = other.gameObject.layer;

            // Don't hit the entity that fired us (Larry, Enemy layer 7)
            if (otherLayer == shooterLayer) return;

            // Player hit (layer 6): deal damage
            if (otherLayer == 6)
            {
                var health = other.GetComponentInParent<Health>();
                if (health != null)
                    health.TakeDamage(playerDamage);

                SFXManager.Instance?.PlayAtPosition(SFXType.ProjectileImpact, transform.position);
                ObjectPool.ReturnOrDestroy(gameObject);
                return;
            }

            // Planet/sun surface hit (layer 10): convert to collectible trash
            if (otherLayer == 10)
            {
                SpawnLandingPickup();
                ObjectPool.ReturnOrDestroy(gameObject);
                return;
            }
        }

        private void SpawnLandingPickup()
        {
            var trashPrefab = TrashSpawner.GetRandomTrashPrefab();
            if (trashPrefab == null) return;

            var pool = ObjectPool.GetPoolForPrefab(trashPrefab);
            GameObject trash = pool != null
                ? pool.Get(transform.position, transform.rotation)
                : Object.Instantiate(trashPrefab, transform.position, transform.rotation);

            // Scale up for visibility (spec: 2x)
            trash.transform.localScale = Vector3.one * 2f;

            var pickup = trash.GetComponent<TrashPickup>();
            if (pickup != null)
            {
                pickup.CountsForProgress = false;
                pickup.AmmoValue = landingAmmoValue;
            }
        }
    }
}
