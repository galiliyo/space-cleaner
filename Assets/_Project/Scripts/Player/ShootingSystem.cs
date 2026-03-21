using UnityEngine;
using SpaceCleaner.Core;

namespace SpaceCleaner.Player
{
    public class ShootingSystem : MonoBehaviour
    {
        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float projectileSpeed = 8f;

        [Header("Single Shot (Flick)")]
        [SerializeField] private float flickThreshold = 0.3f;
        [SerializeField] private float singleShotCooldown = 0.3f;

        [Header("Auto-Fire (Hold)")]
        [SerializeField] private float autoFireRate = 6f; // shots per second

        [Header("Burst")]
        [SerializeField] private int burstShotCount = 10;
        [SerializeField] private float burstCooldown = 3f;

        [Header("Aiming Visual")]
        [SerializeField] private AimingCone aimingCone;

        private PlayerController playerController;
        private SphericalMovement sphericalMovement;
        private float singleShotTimer;
        private float autoFireTimer;
        private float aimHoldTime;
        private bool wasAiming;
        private Vector2 lastAimDirection;

        private int burstRemaining;
        private float burstCooldownTimer;
        private bool burstOnCooldown;

        public bool IsAutoFiring => wasAiming && aimHoldTime >= flickThreshold;
        public bool BurstOnCooldown => burstOnCooldown;
        public float BurstCooldownNormalized => burstOnCooldown ? burstCooldownTimer / burstCooldown : 0f;

        public void SetAimingCone(AimingCone cone) => aimingCone = cone;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            sphericalMovement = GetComponent<SphericalMovement>();
            burstRemaining = burstShotCount;

            // Auto-create fire point if not assigned
            if (firePoint == null)
            {
                var fpGo = new GameObject("FirePoint");
                fpGo.transform.SetParent(transform, false);
                fpGo.transform.localPosition = new Vector3(0f, 0f, 1.5f); // in front of ship
                firePoint = fpGo.transform;
            }
        }

        /// <summary>
        /// Called by LevelSetup to wire the projectile prefab at runtime.
        /// </summary>
        public void SetProjectilePrefab(GameObject prefab)
        {
            projectilePrefab = prefab;
        }

        private void Update()
        {
            if (singleShotTimer > 0f)
                singleShotTimer -= Time.deltaTime;
            if (autoFireTimer > 0f)
                autoFireTimer -= Time.deltaTime;

            if (burstOnCooldown)
            {
                burstCooldownTimer -= Time.deltaTime;
                if (burstCooldownTimer <= 0f)
                {
                    burstOnCooldown = false;
                    burstRemaining = burstShotCount;
                }
            }
        }

        /// <summary>
        /// Called every frame by PlayerController with the current aim stick input.
        /// </summary>
        public void UpdateAim(Vector2 aimInput)
        {
            bool isAiming = aimInput.sqrMagnitude > 0.01f;

            // Update aiming cone visibility
            if (aimingCone != null)
            {
                Vector3 worldAim = GetWorldAimDirection(isAiming ? aimInput.normalized : Vector2.zero);
                aimingCone.UpdateCone(worldAim, isAiming);
            }

            if (isAiming)
            {
                lastAimDirection = aimInput.normalized;
                aimHoldTime += Time.deltaTime;

                // Orient fire point toward aim direction on the sphere surface
                UpdateFireDirection(lastAimDirection);

                // Auto-fire when held long enough (burst limited)
                if (aimHoldTime >= flickThreshold && !burstOnCooldown)
                {
                    if (autoFireTimer <= 0f && burstRemaining > 0)
                    {
                        FireProjectile();
                        autoFireTimer = 1f / autoFireRate;
                        burstRemaining--;

                        if (burstRemaining <= 0)
                        {
                            burstOnCooldown = true;
                            burstCooldownTimer = burstCooldown;
                        }
                    }
                }
            }
            else if (wasAiming)
            {
                // Just released — flick fires a single shot
                if (aimHoldTime < flickThreshold)
                {
                    if (singleShotTimer <= 0f)
                    {
                        FireProjectile();
                        singleShotTimer = singleShotCooldown;
                    }
                }

                aimHoldTime = 0f;

                // Reset burst for next hold (unless mid-cooldown, which continues)
                if (!burstOnCooldown)
                    burstRemaining = burstShotCount;
            }

            wasAiming = isAiming;
        }

        /// <summary>
        /// Converts a 2D stick input to a world-space aim direction on the planet tangent plane.
        /// Returns Vector3.zero if planet reference is missing.
        /// </summary>
        private Vector3 GetWorldAimDirection(Vector2 aimDir)
        {
            if (sphericalMovement == null || sphericalMovement.Planet == null) return Vector3.zero;

            Vector3 up = (transform.position - sphericalMovement.Planet.position).normalized;
            Vector3 shipForward = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            Vector3 shipRight = Vector3.Cross(up, shipForward).normalized;

            return (shipForward * aimDir.y + shipRight * aimDir.x).normalized;
        }

        private void UpdateFireDirection(Vector2 aimDir)
        {
            if (sphericalMovement == null || sphericalMovement.Planet == null) return;

            Vector3 up = (transform.position - sphericalMovement.Planet.position).normalized;
            Vector3 worldAimDir = GetWorldAimDirection(aimDir);

            // Orient the fire point
            if (firePoint != null && worldAimDir.sqrMagnitude > 0.001f)
            {
                firePoint.rotation = Quaternion.LookRotation(worldAimDir, up);
            }
        }

        private void FireProjectile()
        {
            if (!playerController.TryConsumeAmmo()) return;
            if (projectilePrefab == null || firePoint == null) return;

            var pool = ObjectPool.GetPoolForPrefab(projectilePrefab);
            GameObject proj = pool != null
                ? pool.Get(firePoint.position, firePoint.rotation)
                : Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            // Prevent projectile from hitting the player who fired it
            var projectile = proj.GetComponent<Projectile>();
            if (projectile != null)
                projectile.SetShooterLayer(gameObject.layer);

            var rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = firePoint.forward * projectileSpeed;
            }
        }
    }
}
