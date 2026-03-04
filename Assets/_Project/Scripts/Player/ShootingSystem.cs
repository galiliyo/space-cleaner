using UnityEngine;

namespace SpaceCleaner.Player
{
    public class ShootingSystem : MonoBehaviour
    {
        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float projectileSpeed = 40f;

        [Header("Single Shot")]
        [SerializeField] private float singleShotCooldown = 0.3f;

        [Header("Burst Shot")]
        [SerializeField] private int burstCount = 10;
        [SerializeField] private float burstInterval = 0.05f;
        [SerializeField] private float burstCooldown = 3f;

        private PlayerController playerController;
        private SphericalMovement sphericalMovement;
        private float singleShotTimer;
        private float burstCooldownTimer;
        private bool isBursting;
        private int burstShotsFired;
        private float burstIntervalTimer;

        public bool IsBursting => isBursting;
        public float BurstCooldownNormalized => burstCooldownTimer / burstCooldown;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            sphericalMovement = GetComponent<SphericalMovement>();
        }

        private void Update()
        {
            if (singleShotTimer > 0f)
                singleShotTimer -= Time.deltaTime;
            if (burstCooldownTimer > 0f)
                burstCooldownTimer -= Time.deltaTime;

            if (isBursting)
                UpdateBurst();
        }

        public void FireSingle()
        {
            if (singleShotTimer > 0f) return;
            if (!playerController.TryConsumeAmmo()) return;

            SpawnProjectile();
            singleShotTimer = singleShotCooldown;
        }

        public void FireBurst()
        {
            if (isBursting) return;
            if (burstCooldownTimer > 0f) return;
            if (playerController.AmmoCount <= 0) return;

            isBursting = true;
            burstShotsFired = 0;
            burstIntervalTimer = 0f;
        }

        private void UpdateBurst()
        {
            burstIntervalTimer -= Time.deltaTime;
            if (burstIntervalTimer > 0f) return;

            if (burstShotsFired >= burstCount || !playerController.TryConsumeAmmo())
            {
                isBursting = false;
                burstCooldownTimer = burstCooldown;
                return;
            }

            SpawnProjectile();
            burstShotsFired++;
            burstIntervalTimer = burstInterval;
        }

        private void SpawnProjectile()
        {
            if (projectilePrefab == null || firePoint == null) return;

            GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            var rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = firePoint.forward * projectileSpeed;
            }
        }
    }
}
