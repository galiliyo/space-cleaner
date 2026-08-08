using UnityEngine;
using UnityEngine.Rendering;
using SpaceCleaner.Player;
using SpaceCleaner.Camera;
using SpaceCleaner.Enemies;
using SpaceCleaner.UI;

namespace SpaceCleaner.Core
{
    public class LevelSetup : MonoBehaviour
    {
        [Header("Planet")]
        [SerializeField] private Transform planet;
        [SerializeField] private float planetRadius = 50f;
        [SerializeField] private float hoverHeight = 2f;

        [Header("References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private SphericalCamera sphericalCamera;
        [SerializeField] private AIOpponent aiOpponent;

        [Header("Pooling")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private int projectilePoolSize = 20;

        private void Start()
        {
            float orbitRadius = planetRadius + hoverHeight;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[LevelSetup] Starting. planet={planet?.name ?? "NULL"}, player={(player != null ? "OK" : "NULL")}, orbitRadius={orbitRadius}");
#endif

            // Setup player
            if (player != null)
            {
                var movement = player.GetComponent<SphericalMovement>();
                if (movement != null)
                    movement.SetPlanet(planet, orbitRadius);
                else
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError("[LevelSetup] SphericalMovement not found on player!");
#endif

                // Initialize aiming cone — create one if not present on the prefab
                var aimingCone = player.GetComponentInChildren<AimingCone>();
                if (aimingCone == null)
                {
                    var coneGo = new GameObject("AimingCone");
                    coneGo.AddComponent<MeshFilter>();
                    coneGo.AddComponent<MeshRenderer>();
                    coneGo.transform.SetParent(player.transform, false);
                    aimingCone = coneGo.AddComponent<AimingCone>();
                }
                aimingCone.SetPlanet(planet);

                // Wire aiming cone and projectile prefab into shooting system
                var shooting = player.GetComponent<ShootingSystem>();
                if (shooting != null)
                {
                    shooting.SetAimingCone(aimingCone);
                    if (projectilePrefab != null)
                        shooting.SetProjectilePrefab(projectilePrefab);
                }

                // Position player on top of planet
                player.transform.position = planet.position + Vector3.up * orbitRadius;
                player.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

                // Disable shadow casting on the player ship
                foreach (var r in player.GetComponentsInChildren<Renderer>())
                    r.shadowCastingMode = ShadowCastingMode.Off;

                // Add death handler if not present
                if (player.GetComponent<PlayerDeathHandler>() == null)
                    player.gameObject.AddComponent<PlayerDeathHandler>();
                
                // Add polish components if not present
                if (player.GetComponent<ShipFeedback>() == null)
                    player.gameObject.AddComponent<ShipFeedback>();
                if (player.GetComponent<VacuumJuice>() == null)
                    player.gameObject.AddComponent<VacuumJuice>();
                if (player.GetComponent<ShootingJuice>() == null)
                    player.gameObject.AddComponent<ShootingJuice>();
            }

            // Setup camera
            if (sphericalCamera != null && player != null)
            {
                sphericalCamera.SetTarget(player.transform, planet);
            }

            // Setup AI opponent on opposite side of planet
            if (aiOpponent != null)
            {
                aiOpponent.transform.position = planet.position + Vector3.down * orbitRadius;
                aiOpponent.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.down);

                // Add opponent banner (name + health bar)
                if (aiOpponent.GetComponent<OpponentBanner>() == null)
                    aiOpponent.gameObject.AddComponent<OpponentBanner>();
            }

            // Setup projectile pool
            SetupProjectilePool();

            // Setup space skybox
            if (GetComponent<SpaceSkybox>() == null)
                gameObject.AddComponent<SpaceSkybox>();

            // Setup moon
            SetupMoon();
            
            // Setup planet atmosphere glow (Fresnel shader)
            if (planet != null && planet.GetComponent<PlanetAtmosphere>() == null)
                planet.gameObject.AddComponent<PlanetAtmosphere>();

            // Setup radar minimap
            SetupRadar();
            
            // Setup trash spawner
            SetupTrashSpawner();
            
            // Setup combo system (manager + on-screen UI)
            SetupCombo();
            
            // HUD animations are now auto-added by PolishSetup component
        }
        
        private void SetupCombo()
        {
            if (ComboManager.Instance == null)
            {
                var comboGO = new GameObject("ComboManager");
                comboGO.transform.SetParent(transform);
                comboGO.AddComponent<ComboManager>();
            }

            var hud = FindAnyObjectByType<GameplayHUD>();
            if (hud != null && hud.GetComponent<ComboUI>() == null)
                hud.gameObject.AddComponent<ComboUI>();
        }

        private void SetupMoon()
        {
            if (planet == null) return;
            var moonGo = new GameObject("Moon");
            var moon = moonGo.AddComponent<Moon>();
            moon.SetPlanet(planet);
        }

        private void SetupProjectilePool()
        {
            if (projectilePrefab == null) return;
            if (ObjectPool.GetPoolForPrefab(projectilePrefab) != null) return; // already exists

            var poolGO = new GameObject("ProjectilePool");
            poolGO.SetActive(false);
            poolGO.transform.SetParent(transform);

            var pool = poolGO.AddComponent<ObjectPool>();
            pool.InitializeRuntime(projectilePrefab, projectilePoolSize, poolGO.transform);
            poolGO.SetActive(true);
        }

        private void SetupRadar()
        {
            if (player == null || planet == null) return;

            // Find existing HUD canvas
            var hudCanvas = FindAnyObjectByType<GameplayHUD>();
            if (hudCanvas == null) return;

            // Create radar container
            var radarGo = new GameObject("RadarMinimap");
            radarGo.transform.SetParent(hudCanvas.transform, false);

            var radarRt = radarGo.AddComponent<RectTransform>();
            radarRt.anchorMin = new Vector2(0, 0);
            radarRt.anchorMax = new Vector2(0, 0);
            radarRt.pivot = new Vector2(0, 0);
            radarRt.anchoredPosition = new Vector2(20, 180); // above joystick area
            radarRt.sizeDelta = new Vector2(140, 140);

            // Circular background
            var bgImage = radarGo.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.2f, 0.3f);

            // Add mask for circular clipping
            var mask = radarGo.AddComponent<UnityEngine.UI.Mask>();
            mask.showMaskGraphic = true;

            // Radar script
            var radar = radarGo.AddComponent<RadarMinimap>();

            // Use reflection-free approach: set via serialized fields using a helper
            // Since we're creating at runtime, we set public-accessible references
            radar.SetReferences(planet, player.transform, radarRt, bgImage);
        }
        
        private void SetupTrashSpawner()
        {
            // Check if TrashSpawner already exists
            var existingSpawner = FindAnyObjectByType<TrashSpawner>();
            if (existingSpawner != null) return;
            
            // Create TrashSpawner GameObject
            var spawnerGO = new GameObject("TrashSpawner");
            spawnerGO.transform.SetParent(transform);
            
            var spawner = spawnerGO.AddComponent<TrashSpawner>();
            
            // Set references via reflection or public methods
            // First try to find trash prefabs
            var trashPrefabs = new System.Collections.Generic.List<GameObject>();
            
            // Look in common locations
            string[] possiblePaths = new[] { "Trash_A", "Trash_B", "Trash_C" };
            foreach (var path in possiblePaths)
            {
                var prefab = Resources.Load<GameObject>($"Prefabs/Trash/{path}");
                if (prefab == null)
                {
                    // Try direct find
                    prefab = GameObject.Find($"{path}");
                }
                if (prefab != null)
                    trashPrefabs.Add(prefab);
            }
            
            // If no prefabs found, we'll need them assigned in Inspector
            // Store them in a temporary list for now
            Debug.Log($"[LevelSetup] TrashSpawner created. Found {trashPrefabs.Count} trash prefabs. Assign trash prefabs in Inspector if needed.");
        }
    }
}
