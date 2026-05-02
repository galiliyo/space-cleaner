using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using SpaceCleaner.Core;
using SpaceCleaner.Player;
using SpaceCleaner.Boss;

namespace SpaceCleaner.Enemies
{
    [RequireComponent(typeof(Health))]
    public class AIOpponent : MonoBehaviour
    {
        [Header("Planet")]
        [SerializeField] private Transform planet;
        [SerializeField] private float orbitRadius = 52f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2.3f;
        [SerializeField] private float rotationSpeed = 5f;

        [Header("Vacuum")]
        [SerializeField] private bool canVacuum = false;
        [SerializeField] private float vacuumRadius = 3.5f;
        [SerializeField] private LayerMask trashLayer;
        [SerializeField] private int startingAmmo = 8;

        [Header("Combat")]
        [SerializeField] private float shootRange = 10f;
        [SerializeField] private float shootCooldown = 3.5f;
        [SerializeField] private float shootInaccuracy = 50f; // degrees of random spread
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float projectileSpeed = 20f;

        [Header("Identity")]
        [SerializeField] private string opponentName = "Buzz";

        [Header("Behavior")]
        [SerializeField] private float aggressionRange = 20f;
        [SerializeField] private float aggressionHysteresis = 5f;

        [Header("Boss Arena")]
        public bool isCarryOver = false;
        public bool recordToCarryOver = true;

        [Header("Collision")]
        [SerializeField] private float minSeparation = 5f;
        [SerializeField] private float bounceSpeed = 8f;

        private Health health;
        private Core.BuffReceiver buffReceiver;
        private Transform playerTransform;
        private SphericalMovement playerMovement;
        private PlayerController playerController;
        private int collectedAmmo;
        private float shootTimer;
        private float trashSearchTimer;
        private Transform cachedNearestTrash;
        private Vector3 bounceVelocity;
        private int freezeMinAmmo = -1;   // -1 = not frozen; >=0 = freeze until player reaches this ammo count
        private float freezeDeadline;     // fallback: unfreeze at this Time.time regardless

        private static readonly WaitForSeconds s_BlinkWait = new WaitForSeconds(0.1f);

        private enum AIState { Vacuum, Combat }
        private AIState currentState = AIState.Vacuum;

        public string OpponentName => opponentName;
        public int CollectedAmmo => collectedAmmo;

        /// <summary>
        /// Freezes the opponent until the player has at least <paramref name="minAmmo"/> ammo,
        /// or <paramref name="maxDuration"/> seconds pass — whichever comes first.
        /// </summary>
        public void FreezeUntilPlayerReady(int minAmmo, float maxDuration)
        {
            freezeMinAmmo = minAmmo;
            freezeDeadline = Time.time + maxDuration;
        }

        /// <summary>
        /// Configures this opponent for boss arena use. Call after Instantiate, before first Update.
        /// </summary>
        public void Configure(Transform planet, float orbitRadius, int startingAmmo, bool recordToCarryOver, GameObject projectilePrefab = null)
        {
            this.planet = planet;
            this.orbitRadius = orbitRadius;
            this.startingAmmo = startingAmmo;
            this.collectedAmmo = startingAmmo;
            this.recordToCarryOver = recordToCarryOver;
            if (projectilePrefab != null)
                this.projectilePrefab = projectilePrefab;

            // Auto-find or create fire point (needed for Shoot() to work)
            if (firePoint == null)
            {
                var fp = transform.Find("FirePoint");
                if (fp == null)
                {
                    var fpGO = new UnityEngine.GameObject("FirePoint");
                    fpGO.transform.SetParent(transform, false);
                    fpGO.transform.localPosition = UnityEngine.Vector3.forward * 2f;
                    fp = fpGO.transform;
                }
                firePoint = fp;
            }
        }

        private void Awake()
        {
            health = GetComponent<Health>();
            health.OnDeath += OnDeath;
            buffReceiver = GetComponent<Core.BuffReceiver>() ?? gameObject.AddComponent<Core.BuffReceiver>();
        }

        private void Start()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
                playerMovement = player.GetComponent<SphericalMovement>();
                playerController = player;
            }

            collectedAmmo = startingAmmo;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[AIOpponent] Initialized. planet={planet?.name ?? "NULL"}, player={(playerTransform != null ? "OK" : "NULL")}, ammo={collectedAmmo}, orbitRadius={orbitRadius}, trashLayer={trashLayer.value}");
#endif

            // Disable shadows on opponent
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void Update()
        {
            if (health.IsDead) return;

            if (freezeMinAmmo >= 0)
            {
                bool playerReady = playerController != null && playerController.AmmoCount >= freezeMinAmmo;
                bool timedOut = Time.time >= freezeDeadline;
                if (playerReady || timedOut)
                    freezeMinAmmo = -1;
                else
                    return;
            }

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

            HandlePlayerProximity();
            ApplyBounceVelocity();
            SnapToSurface();
        }

        private void UpdateState()
        {
            if (playerTransform == null) return;

            // Without vacuum capability, always stay in combat
            if (!canVacuum) { currentState = AIState.Combat; return; }

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
            else if (shootTimer <= 0f)
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
            float speedMult = buffReceiver != null ? buffReceiver.SpeedMultiplier : 1f;
            float angularSpeed = (moveSpeed * speedMult) / orbitRadius;
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

            shootTimer = shootCooldown;
            SFXManager.Instance?.Play(SFXType.AIShoot);

            Vector3 dir = (playerTransform.position - firePoint.position).normalized;
            dir = Quaternion.Euler(
                Random.Range(-shootInaccuracy, shootInaccuracy),
                Random.Range(-shootInaccuracy, shootInaccuracy),
                0f) * dir;
            dir.Normalize();
            Quaternion rot = Quaternion.LookRotation(dir);
            var pool = ObjectPool.GetPoolForPrefab(projectilePrefab);
            GameObject proj = pool != null
                ? pool.Get(firePoint.position, rot)
                : Instantiate(projectilePrefab, firePoint.position, rot);
            // Prevent projectile from hitting the AI who fired it
            var projectile = proj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.SetShooterLayer(gameObject.layer);
                if (buffReceiver != null && buffReceiver.DamageMultiplier > 1f)
                    projectile.OverrideDamage(Mathf.RoundToInt(1 * buffReceiver.DamageMultiplier));
            }

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

        private void HandlePlayerProximity()
        {
            if (playerTransform == null || planet == null) return;
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist >= minSeparation || dist < 0.01f) return;

            Vector3 up = (transform.position - planet.position).normalized;
            Vector3 away = Vector3.ProjectOnPlane(transform.position - playerTransform.position, up).normalized;
            if (away.sqrMagnitude < 0.001f) return;

            float overlap = minSeparation - dist;
            transform.position += away * (overlap * 0.5f);
            SnapToSurface();

            bounceVelocity = away * bounceSpeed;
            SFXManager.Instance?.Play(SFXType.AIPlayerBounce);
            if (playerMovement != null)
                playerMovement.ApplyBounce(-away, bounceSpeed);
        }

        private void ApplyBounceVelocity()
        {
            if (bounceVelocity.sqrMagnitude <= 0.001f) return;
            if (planet == null) return;

            Vector3 up = (transform.position - planet.position).normalized;
            Vector3 bDir = Vector3.ProjectOnPlane(bounceVelocity, up).normalized;
            float angSpeed = bounceVelocity.magnitude / orbitRadius;
            float angle = angSpeed * Time.deltaTime;
            Vector3 fromCenter = transform.position - planet.position;
            Vector3 rotAxis = Vector3.Cross(fromCenter.normalized, bDir).normalized;
            if (rotAxis.sqrMagnitude > 0.001f)
            {
                Quaternion rot = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, rotAxis);
                transform.position = planet.position + rot * fromCenter;
            }
            bounceVelocity = Vector3.MoveTowards(bounceVelocity, Vector3.zero, 6f * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!canVacuum) return;
            if (((1 << other.gameObject.layer) & trashLayer) == 0) return;

            var trash = other.GetComponent<TrashPickup>();
            if (trash != null && !trash.IsBeingCollected)
            {
                bool countsForProgress = trash.CountsForProgress;
                int ammo = trash.AmmoValue;
                if (buffReceiver != null && buffReceiver.AmmoGainMultiplier > 1f)
                    ammo = Mathf.RoundToInt(ammo * buffReceiver.AmmoGainMultiplier);
                ObjectPool.ReturnOrDestroy(other.gameObject);
                collectedAmmo += ammo;
                SFXManager.Instance?.Play(SFXType.AICollectTrash);
                if (countsForProgress)
                    GameManager.Instance?.RegisterTrashCollected();
            }
        }

        private void OnDeath()
        {
            SFXManager.Instance?.Play(SFXType.AIDeath);

            if (recordToCarryOver)
                CarryOverData.Record(opponentName, collectedAmmo);

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
