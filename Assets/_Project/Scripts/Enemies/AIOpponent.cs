using System.Collections;
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
        [SerializeField] private int startingAmmo = 30;

        [Header("Combat")]
        [SerializeField] private float shootRange = 25f;
        [SerializeField] private float shootCooldown = 1.5f;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float projectileSpeed = 30f;

        [Header("Identity")]
        [SerializeField] private string opponentName = "Buzz";

        [Header("Behavior")]
        [SerializeField] private float aggressionRange = 30f;
        [SerializeField] private float aggressionHysteresis = 5f;

        private Health health;
        private Transform playerTransform;
        private int collectedAmmo;
        private float shootTimer;
        private float trashSearchTimer;
        private Transform cachedNearestTrash;

        private static readonly WaitForSeconds s_BlinkWait = new WaitForSeconds(0.1f);

        private enum AIState { Vacuum, Combat }
        private AIState currentState = AIState.Vacuum;

        public string OpponentName => opponentName;
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

            collectedAmmo = startingAmmo;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[AIOpponent] Initialized. planet={planet?.name ?? "NULL"}, player={(playerTransform != null ? "OK" : "NULL")}, ammo={collectedAmmo}, orbitRadius={orbitRadius}, trashLayer={trashLayer.value}");
#endif
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

            if (currentState == AIState.Vacuum && distToPlayer < aggressionRange)
                currentState = AIState.Combat;
            else if (currentState == AIState.Combat && distToPlayer > aggressionRange + aggressionHysteresis)
                currentState = AIState.Vacuum;
        }

        private void UpdateVacuumBehavior()
        {
            // Only re-evaluate target when current one is invalid, inactive (pooled), or reached
            bool needNewTarget = cachedNearestTrash == null
                                 || !cachedNearestTrash.gameObject.activeInHierarchy;

            if (!needNewTarget)
            {
                var trash = cachedNearestTrash.GetComponent<TrashPickup>();
                if (trash != null && trash.IsBeingCollected)
                    needNewTarget = true;
            }

            if (!needNewTarget)
            {
                float distToTarget = Vector3.Distance(transform.position, cachedNearestTrash.position);
                if (distToTarget < vacuumRadius)
                    needNewTarget = true;
            }

            if (needNewTarget)
            {
                cachedNearestTrash = FindNearestTrash();
            }

            if (cachedNearestTrash != null)
                MoveToward(cachedNearestTrash.position);
        }

        private Transform FindNearestTrash()
        {
            var instances = Core.TrashPickup.ActiveInstances;
            if (instances.Count == 0) return null;

            Transform nearest = null;
            float nearestDist = float.MaxValue;
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i].IsBeingCollected) continue;
                float dist = Vector3.Distance(transform.position, instances[i].transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = instances[i].transform;
                }
            }
            return nearest;
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
            Quaternion rot = Quaternion.LookRotation(dir);
            var pool = ObjectPool.GetPoolForPrefab(projectilePrefab);
            GameObject proj = pool != null
                ? pool.Get(firePoint.position, rot)
                : Instantiate(projectilePrefab, firePoint.position, rot);
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
                ObjectPool.ReturnOrDestroy(other.gameObject);
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
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    meshRenderer.enabled = false;
                    yield return s_BlinkWait;
                    meshRenderer.enabled = true;
                    yield return s_BlinkWait;
                }
            }

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnDeath -= OnDeath;
        }
    }
}
