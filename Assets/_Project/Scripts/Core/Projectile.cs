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

        /// <summary>
        /// Called after spawning to prevent the projectile from hitting the entity that fired it.
        /// </summary>
        public void SetShooterLayer(int layer) => shooterLayer = layer;

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
                var trailShader = Shader.Find("Universal Render Pipeline/Unlit");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (trailShader == null) Debug.LogError("Projectile: URP Unlit shader not found");
#endif
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

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & hitLayers) == 0) return;

            // Don't hit the entity that fired us
            if (other.gameObject.layer == shooterLayer) return;

            var health = other.GetComponentInParent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }

            SFXManager.Instance?.PlayAtPosition(SFXType.ProjectileImpact, transform.position);
            ObjectPool.ReturnOrDestroy(gameObject);
        }

        private void SpawnTrashIfPlayerProjectile()
        {
            // Only player projectiles (layer 6) convert to trash
            if (shooterLayer != 6) return;

            var trashPrefab = TrashSpawner.GetRandomTrashPrefab();
            if (trashPrefab == null) return;

            var pool = ObjectPool.GetPoolForPrefab(trashPrefab);
            GameObject trash = pool != null
                ? pool.Get(transform.position, Quaternion.identity)
                : Object.Instantiate(trashPrefab, transform.position, Quaternion.identity);

            var pickup = trash.GetComponent<TrashPickup>();
            if (pickup != null)
                pickup.CountsForProgress = false;
        }
    }
}
