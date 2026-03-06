using UnityEngine;

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

        private PlayerController playerController;
        private SphericalMovement sphericalMovement;
        private float singleShotTimer;
        private float autoFireTimer;
        private float aimHoldTime;
        private bool wasAiming;
        private Vector2 lastAimDirection;

        public bool IsAutoFiring => wasAiming && aimHoldTime >= flickThreshold;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            sphericalMovement = GetComponent<SphericalMovement>();
        }

        private void Update()
        {
            if (singleShotTimer > 0f)
                singleShotTimer -= Time.deltaTime;
            if (autoFireTimer > 0f)
                autoFireTimer -= Time.deltaTime;
        }

        /// <summary>
        /// Called every frame by PlayerController with the current aim stick input.
        /// </summary>
        public void UpdateAim(Vector2 aimInput)
        {
            bool isAiming = aimInput.sqrMagnitude > 0.01f;

            if (isAiming)
            {
                lastAimDirection = aimInput.normalized;
                aimHoldTime += Time.deltaTime;

                // Orient fire point toward aim direction on the sphere surface
                UpdateFireDirection(lastAimDirection);

                // Auto-fire when held long enough
                if (aimHoldTime >= flickThreshold)
                {
                    if (autoFireTimer <= 0f)
                    {
                        FireProjectile();
                        autoFireTimer = 1f / autoFireRate;
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
            }

            wasAiming = isAiming;
        }

        private void UpdateFireDirection(Vector2 aimDir)
        {
            if (sphericalMovement == null || sphericalMovement.Planet == null) return;

            // Get the ship's surface-relative axes
            Vector3 up = (transform.position - sphericalMovement.Planet.position).normalized;
            Vector3 shipForward = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            Vector3 shipRight = Vector3.Cross(up, shipForward).normalized;

            // Build world aim direction from 2D stick input
            Vector3 worldAimDir = (shipForward * aimDir.y + shipRight * aimDir.x).normalized;

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

            GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            var rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = firePoint.forward * projectileSpeed;
            }
        }
    }
}
