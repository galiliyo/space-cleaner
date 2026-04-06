using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SpaceCleaner.Core;
using SpaceCleaner.Player;
using SpaceCleaner.Camera;
using SpaceCleaner.Enemies;
using SpaceCleaner.UI;

namespace SpaceCleaner.Boss
{
    public class BossFightManager : MonoBehaviour
    {
        [Header("Sun")]
        [SerializeField] private Transform sun;
        [SerializeField] private float sunRadius = 60f;
        [SerializeField] private float hoverHeight = 2f;

        [Header("Larry")]
        [SerializeField] private int larryMaxHealth = 50;
        [SerializeField] private Vector3 larryOffset = Vector3.up;

        [Header("Minions")]
        [SerializeField] private GameObject minionPrefab;
        [SerializeField] private int baseMinionCount = 2;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private int projectilePoolSize = 20;

        [Header("Player Setup")]
        [SerializeField] private PlayerController player;
        [SerializeField] private SphericalCamera sphericalCamera;

        // Static events — subscribe in any script's Start(), fired one frame after BossFightManager.Start()
        public static event Action<Health, string> OnBossHealthReady;
        public static event Action OnBossArenaComplete;

        private LarryBoss larry;
        private readonly List<Health> aliveEntities = new();
        private bool arenaComplete;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearStaticState()
        {
            OnBossHealthReady = null;
            OnBossArenaComplete = null;
        }

        private void Start()
        {
            float orbitRadius = sunRadius + hoverHeight;

            SetupPlayer(orbitRadius);
            SetupProjectilePool();

            // Retrieve carry-over data from planet run
            var carryOvers = CarryOverData.GetAndClear();

            // Spawn Larry on the sun surface
            SpawnLarry(orbitRadius);

            // Spawn base minions + carry-over minions
            SpawnMinions(orbitRadius, carryOvers);

            // Enable Projectile-Planet collision for LarryTrashBall landing detection
            Physics.IgnoreLayerCollision(9, 10, false);

            // Fire ready event one frame later (so all Start() subscriptions are in place)
            StartCoroutine(FireReadyEvent());
        }

        private void SetupPlayer(float orbitRadius)
        {
            if (player == null) return;

            var movement = player.GetComponent<SphericalMovement>();
            if (movement != null)
                movement.SetPlanet(sun, orbitRadius);

            player.transform.position = sun.position + Vector3.up * orbitRadius;
            player.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

            var aimingCone = player.GetComponentInChildren<AimingCone>();
            if (aimingCone != null)
                aimingCone.SetPlanet(sun);

            var shooting = player.GetComponent<ShootingSystem>();
            if (shooting != null)
            {
                if (aimingCone != null)
                    shooting.SetAimingCone(aimingCone);
                if (projectilePrefab != null)
                    shooting.SetProjectilePrefab(projectilePrefab);
            }

            foreach (var r in player.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = ShadowCastingMode.Off;

            if (player.GetComponent<PlayerDeathHandler>() == null)
                player.gameObject.AddComponent<PlayerDeathHandler>();

            if (sphericalCamera != null)
                sphericalCamera.SetTarget(player.transform, sun);
        }

        private void SetupProjectilePool()
        {
            if (projectilePrefab == null) return;
            if (ObjectPool.GetPoolForPrefab(projectilePrefab) != null) return;

            var poolGO = new GameObject("ProjectilePool");
            poolGO.SetActive(false);
            poolGO.transform.SetParent(transform);

            var pool = poolGO.AddComponent<ObjectPool>();
            pool.InitializeRuntime(projectilePrefab, projectilePoolSize, poolGO.transform);
            poolGO.SetActive(true);
        }

        private void SpawnLarry(float orbitRadius)
        {
            var larryGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            larryGO.name = "Larry";
            larryGO.layer = 7; // Enemy
            larryGO.transform.localScale = Vector3.one * 4f;

            Vector3 surfaceDir = larryOffset.normalized;
            if (surfaceDir.sqrMagnitude < 0.01f) surfaceDir = Vector3.up;
            larryGO.transform.position = sun.position + surfaceDir * orbitRadius;
            larryGO.transform.rotation = Quaternion.LookRotation(
                Vector3.Cross(surfaceDir, Vector3.right).normalized, surfaceDir);

            var collider = larryGO.GetComponent<BoxCollider>();
            collider.isTrigger = true;

            var health = larryGO.AddComponent<Health>();
            health.SetMaxHealth(larryMaxHealth);

            larry = larryGO.AddComponent<LarryBoss>();
            larry.OnDefeated += () => HandleEntityDeath(health);

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                var mat = new Material(shader);
                Color c = new Color(0.9f, 0.2f, 0.2f, 1f);
                mat.SetColor("_BaseColor", c);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c * 2f);
                var mr = larryGO.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = mat;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            aliveEntities.Add(health);
        }

        private void SpawnMinions(float orbitRadius, List<(string name, int ammo)> carryOvers)
        {
            if (minionPrefab == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[BossFightManager] minionPrefab is null — no minions spawned.");
#endif
                return;
            }

            int totalMinions = baseMinionCount + carryOvers.Count;

            for (int i = 0; i < totalMinions; i++)
            {
                float angle = (360f / totalMinions) * i;
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                Vector3 pos = sun.position + dir * orbitRadius;

                var minionGO = Instantiate(minionPrefab, pos, Quaternion.identity);
                minionGO.name = i < baseMinionCount ? $"Minion_Base_{i}" : $"Minion_CarryOver_{i - baseMinionCount}";
                minionGO.layer = 7; // Enemy

                var opponent = minionGO.GetComponent<AIOpponent>();
                if (opponent != null)
                {
                    int ammo = 30;
                    bool isCarryOver = false;

                    if (i >= baseMinionCount)
                    {
                        var co = carryOvers[i - baseMinionCount];
                        ammo = co.ammo;
                        isCarryOver = true;
                    }

                    opponent.Configure(sun, orbitRadius, ammo, false, projectilePrefab);
                    opponent.isCarryOver = isCarryOver;
                }

                var health = minionGO.GetComponent<Health>();
                if (health != null)
                {
                    aliveEntities.Add(health);
                    health.OnDeath += () => HandleEntityDeath(health);
                }

                if (minionGO.GetComponent<OpponentBanner>() == null)
                    minionGO.AddComponent<OpponentBanner>();
            }
        }

        private void HandleEntityDeath(Health entity)
        {
            aliveEntities.Remove(entity);

            if (aliveEntities.Count == 0 && !arenaComplete)
            {
                arenaComplete = true;
                OnBossArenaComplete?.Invoke();
                SFXManager.Instance?.Play(SFXType.LevelComplete);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[BossFightManager] Boss arena complete! All enemies defeated.");
#endif
            }
        }

        private IEnumerator FireReadyEvent()
        {
            yield return null; // Wait one frame so all Start() subscriptions are in place
            if (larry != null)
                OnBossHealthReady?.Invoke(larry.Health, "Larry");
        }
    }
}
