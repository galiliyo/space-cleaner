using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceCleaner.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private int damage = 1;
        [SerializeField] private LayerMask hitLayers;

        private static Material s_BodyMaterial;
        private static Material s_TrailMaterial;

        private float timer;
        private int shooterLayer = -1;
        private int defaultDamage;

        // Spherical flight: shots follow the planet's curvature instead of flying
        // off on a tangent. Armed per launch by the shooter; unarmed shots fly straight.
        private Vector3 planetCenter;
        private bool curveAroundPlanet;
        private float flightRadius;
        private Rigidbody body;

        /// <summary>
        /// Makes this shot ride a great circle around <paramref name="center"/> instead of
        /// travelling in a straight line. On a planet of radius R a tangent shot climbs
        /// d^2/(2R) above the surface after d units of travel, so unaided shots sail over
        /// targets that are only a handful of units away.
        /// </summary>
        public void SetPlanetCenter(Vector3 center)
        {
            planetCenter = center;
            flightRadius = Vector3.Distance(transform.position, center);
            curveAroundPlanet = flightRadius > 0.001f;
        }

        /// <summary>
        /// Called after spawning to prevent the projectile from hitting the entity that fired it.
        /// </summary>
        public void SetShooterLayer(int layer) => shooterLayer = layer;

        /// <summary>
        /// Scales damage relative to the prefab-authored value. Safe on pooled instances:
        /// OnEnable restores the default on every retrieval, so a buffed shot cannot leak
        /// its damage into the next user of this instance.
        /// </summary>
        public void ApplyDamageMultiplier(float multiplier) =>
            damage = Mathf.Max(1, Mathf.RoundToInt(defaultDamage * multiplier));

        private static void EnsureSharedMaterials()
        {
            if (s_BodyMaterial == null)
            {
                var bodyShader = Shader.Find("Universal Render Pipeline/Lit");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (bodyShader == null) Debug.LogError("Projectile: URP Lit shader not found");
#endif
                s_BodyMaterial = new Material(bodyShader);
                Color baseColor = new Color(1f, 0.7f, 0.1f, 1f); // warm yellow-orange
                s_BodyMaterial.SetColor("_BaseColor", baseColor);
                s_BodyMaterial.EnableKeyword("_EMISSION");
                s_BodyMaterial.SetColor("_EmissionColor", baseColor * 3f); // bright glow
                s_BodyMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }

            if (s_TrailMaterial == null)
            {
                var trailShader = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Universal Render Pipeline/Lit");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (trailShader == null) Debug.LogError("Projectile: No URP shader found for trail");
#endif
                if (trailShader == null) return;
                s_TrailMaterial = new Material(trailShader);
                s_TrailMaterial.SetColor("_BaseColor", new Color(1f, 0.8f, 0.2f, 1f));
                // Set surface type to transparent and blending to additive
                s_TrailMaterial.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
                s_TrailMaterial.SetFloat("_Blend", 1f);   // 0 = Alpha, 1 = Additive (URP Unlit)
                s_TrailMaterial.SetFloat("_SrcBlend", (float)BlendMode.One);
                s_TrailMaterial.SetFloat("_DstBlend", (float)BlendMode.One);
                s_TrailMaterial.SetFloat("_ZWrite", 0f);
                s_TrailMaterial.renderQueue = (int)RenderQueue.Transparent;
                s_TrailMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                s_TrailMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }
        }

        private void Awake()
        {
            // Cache the prefab-authored damage before any buff can override it.
            // Awake always runs before the first OnEnable, including on pooled instances.
            defaultDamage = damage;
            body = GetComponent<Rigidbody>();

            EnsureSharedMaterials();

            // --- Scale up for visibility ---
            if (transform.localScale.x < 0.5f)
            {
                transform.localScale = Vector3.one * 0.5f;
            }

            // --- Bright emissive material on the mesh ---
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = s_BodyMaterial;
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            // --- Trail renderer for motion visibility ---
            var trail = GetComponent<TrailRenderer>();
            if (trail == null) trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.4f;
            trail.startWidth = 0.4f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sharedMaterial = s_TrailMaterial;

            // Color gradient: bright yellow-orange fading to transparent
            var colorGrad = new Gradient();
            colorGrad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1f, 0.85f, 0.3f), 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0.1f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trail.colorGradient = colorGrad;
        }

        private void OnEnable()
        {
            timer = lifetime;
            shooterLayer = -1;
            damage = defaultDamage; // clear any buff override from this instance's previous life
            curveAroundPlanet = false; // shooter re-arms this every launch

            // Reset velocity so stale motion from a previous life doesn't carry over
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Clear trail so old positions don't draw a streak to the new spawn point
            // Note: OnEnable fires before Awake on first pool retrieval, so trail may not exist yet
            if (TryGetComponent<TrailRenderer>(out var trail))
                trail.Clear();
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                SpawnTrashIfPlayerProjectile();
                ObjectPool.ReturnOrDestroy(gameObject);
            }
        }

        private void FixedUpdate()
        {
            if (!curveAroundPlanet || body == null) return;

            ApplySphericalCurvature();
        }

        /// <summary>
        /// Bends the shot's path so it stays at a constant distance from the planet centre,
        /// i.e. it travels along a great circle instead of a straight tangent line.
        ///
        /// The rigidbody is non-kinematic, so the engine already integrates the straight
        /// chord for this step; sweeping the position ourselves as well would advance the
        /// shot twice. Instead we correct at the top of each step: seat the shot back on
        /// its launch shell and re-aim the velocity along the tangent. Over a step the
        /// only error is the chord-vs-arc sagitta (~2.5e-4 units at speed 8 on r=52), and
        /// because it is cancelled every step it never accumulates into a climb.
        /// </summary>
        private void ApplySphericalCurvature()
        {
            Vector3 offset = body.position - planetCenter;
            Vector3 velocity = body.linearVelocity;
            float speed = velocity.magnitude;
            if (offset.sqrMagnitude < 0.000001f || speed < 0.001f) return;

            Vector3 up = offset.normalized;
            Vector3 axis = Vector3.Cross(up, velocity);
            // Fired straight at or away from the planet — there is no great circle to ride.
            if (axis.sqrMagnitude < 0.000001f) return;

            body.position = planetCenter + up * flightRadius;
            body.linearVelocity = Vector3.Cross(axis.normalized, up) * speed;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Sensor volumes are never hitboxes. VacuumCollector adds a 5-unit trigger sphere
            // to the player root — the same GameObject that carries Health — so without this
            // guard damage resolves through GetComponentInParent<Health>() and every shot
            // passing within 7.5 world units "hits" while visibly missing the ship.
            // Both ships carry a non-trigger capsule, so real hits still register.
            if (other.isTrigger) return;

            if (((1 << other.gameObject.layer) & hitLayers) == 0) return;

            // Don't hit the entity that fired us
            if (other.gameObject.layer == shooterLayer) return;

            var health = other.GetComponentInParent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
            else
            {
                // Missed a damageable target (hit planet/ground) — recycle back to trash
                SpawnTrashIfPlayerProjectile();
            }

            SpawnImpactVFX(transform.position);
            SFXManager.Instance?.PlayAtPosition(SFXType.ProjectileImpact, transform.position);
            ObjectPool.ReturnOrDestroy(gameObject);
        }

        private void SpawnImpactVFX(Vector3 position)
        {
            // Create impact burst
            var go = new GameObject("ImpactBurst");
            go.transform.position = position;
            
            var ps = go.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.duration = 0.3f;
            main.startLifetime = 0.2f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startColor = new Color(1f, 0.7f, 0.2f, 1f);
            main.maxParticles = 15;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            
            var emission = ps.emission;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 10));
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            
            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { 
                    new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0f), 
                    new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f) 
                },
                new[] { 
                    new GradientAlphaKey(1f, 0f), 
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            colorOverLife.color = gradient;
            
            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, 0.1f);
            
            // Renderer
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") 
                ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 1f);
                renderer.material = mat;
            }
            
            ps.Play();
            Destroy(go, 0.5f);
        }
        
        private void SpawnTrashIfPlayerProjectile()
        {
            // Only player projectiles (layer 6) convert to trash
            if (shooterLayer != 6) return;

            var trashPrefab = TrashSpawner.GetRandomTrashPrefab();
            if (trashPrefab == null) return;

            Vector3 spawnPos = TrashSpawner.GetOrbitPosition(transform.position);

            var pool = ObjectPool.GetPoolForPrefab(trashPrefab);
            GameObject trash = pool != null
                ? pool.Get(spawnPos, Quaternion.identity)
                : Object.Instantiate(trashPrefab, spawnPos, Quaternion.identity);

            var pickup = trash.GetComponent<TrashPickup>();
            if (pickup != null)
                pickup.CountsForProgress = false;
        }
    }
}
