using UnityEngine;
using SpaceCleaner.Core;
using SpaceCleaner.Player;

namespace SpaceCleaner.Enemies
{
    [RequireComponent(typeof(Health))]
    public class AIOpponent : MonoBehaviour
    {
        [Header("Planet")]
        [SerializeField] private Transform planet;
        [SerializeField] private float orbitRadius = 52f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 20f;
        [SerializeField] private float rotationSpeed = 5f;

        [Header("Vacuum")]
        [SerializeField] private float vacuumRadius = 6f;
        [SerializeField] private LayerMask trashLayer;

        [Header("Combat")]
        [SerializeField] private float shootRange = 25f;
        [SerializeField] private float shootCooldown = 1.5f;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float projectileSpeed = 30f;

        [Header("Behavior")]
        [SerializeField] private float aggressionRange = 30f;

        private Health health;
        private Transform playerTransform;
        private int collectedAmmo;
        private float shootTimer;

        private enum AIState { Vacuum, Combat }
        private AIState currentState = AIState.Vacuum;

        public int CollectedAmmo => collectedAmmo;

        private void Awake()
        {
            health = GetComponent<Health>();
            health.OnDeath += OnDeath;
        }

        private void Start()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                playerTransform = player.transform;
        }

        private void Update()
        {
            if (health.IsDead) return;

            shootTimer -= Time.deltaTime;

            UpdateState();

            switch (currentState)
            {
                case AIState.Vacuum:
                    UpdateVacuumBehavior();
                    break;
                case AIState.Combat:
                    UpdateCombatBehavior();
                    break;
            }

            SnapToSurface();
        }

        private void UpdateState()
        {
            if (playerTransform == null) return;

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            currentState = distToPlayer < aggressionRange ? AIState.Combat : AIState.Vacuum;
        }

        private void UpdateVacuumBehavior()
        {
            // Find nearest trash
            Collider[] nearby = Physics.OverlapSphere(transform.position, vacuumRadius * 3f, trashLayer);
            if (nearby.Length == 0) return;

            Transform nearest = null;
            float nearestDist = float.MaxValue;
            foreach (var col in nearby)
            {
                var trash = col.GetComponent<TrashPickup>();
                if (trash != null && trash.IsBeingCollected) continue;

                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = col.transform;
                }
            }

            if (nearest != null)
                MoveToward(nearest.position);
        }

        private void UpdateCombatBehavior()
        {
            if (playerTransform == null) return;

            float dist = Vector3.Distance(transform.position, playerTransform.position);

            if (dist > shootRange)
            {
                MoveToward(playerTransform.position);
            }
            else if (shootTimer <= 0f && collectedAmmo > 0)
            {
                Shoot();
            }
        }

        private void MoveToward(Vector3 targetPos)
        {
            Vector3 up = (transform.position - planet.position).normalized;
            Vector3 toTarget = (targetPos - transform.position);
            Vector3 projectedDir = Vector3.ProjectOnPlane(toTarget, up).normalized;

            if (projectedDir.sqrMagnitude < 0.001f) return;

            // Rotate position around planet
            float angularSpeed = moveSpeed / orbitRadius;
            float angle = angularSpeed * Time.deltaTime;

            Vector3 fromCenter = transform.position - planet.position;
            Vector3 rotAxis = Vector3.Cross(fromCenter.normalized, projectedDir).normalized;

            if (rotAxis.sqrMagnitude < 0.001f) return;

            Quaternion rot = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, rotAxis);
            transform.position = planet.position + rot * fromCenter;

            // Face movement direction
            Quaternion targetRot = Quaternion.LookRotation(projectedDir, up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        private void Shoot()
        {
            if (projectilePrefab == null || firePoint == null) return;

            collectedAmmo--;
            shootTimer = shootCooldown;

            Vector3 dir = (playerTransform.position - firePoint.position).normalized;
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(dir));
            var rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = dir * projectileSpeed;
        }

        private void SnapToSurface()
        {
            if (planet == null) return;
            Vector3 dir = (transform.position - planet.position).normalized;
            transform.position = planet.position + dir * orbitRadius;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & trashLayer) == 0) return;

            var trash = other.GetComponent<TrashPickup>();
            if (trash != null && !trash.IsBeingCollected)
            {
                Destroy(other.gameObject);
                collectedAmmo++;
                GameManager.Instance?.RegisterTrashCollected();
            }
        }

        private void OnDeath()
        {
            // Transfer collected ammo to player
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                player.AddAmmo(collectedAmmo);

            GameManager.Instance?.RegisterOpponentKilled();
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnDeath -= OnDeath;
        }
    }
}
