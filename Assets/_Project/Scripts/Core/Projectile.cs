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

        private float timer;

        private void Awake()
        {
            // --- Scale up for visibility ---
            if (transform.localScale.x < 0.5f)
            {
                transform.localScale = Vector3.one * 0.5f;
            }

            // --- Bright emissive material on the mesh ---
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                var bodyMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                Color baseColor = new Color(1f, 0.7f, 0.1f, 1f); // warm yellow-orange
                bodyMat.SetColor("_BaseColor", baseColor);
                bodyMat.EnableKeyword("_EMISSION");
                bodyMat.SetColor("_EmissionColor", baseColor * 3f); // bright glow
                bodyMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                meshRenderer.material = bodyMat;
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            // --- Trail renderer for motion visibility ---
            var trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.15f;
            trail.startWidth = 0.3f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;

            // Additive unlit material for glow trail
            var trailMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            trailMat.SetColor("_BaseColor", new Color(1f, 0.8f, 0.2f, 1f));
            // Set surface type to transparent and blending to additive
            trailMat.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
            trailMat.SetFloat("_Blend", 1f);   // 0 = Alpha, 1 = Additive (URP Unlit)
            trailMat.SetFloat("_SrcBlend", (float)BlendMode.One);
            trailMat.SetFloat("_DstBlend", (float)BlendMode.One);
            trailMat.SetFloat("_ZWrite", 0f);
            trailMat.renderQueue = (int)RenderQueue.Transparent;
            trailMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            trailMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            trail.material = trailMat;

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
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                Destroy(gameObject); // TODO: Return to pool
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & hitLayers) == 0) return;

            var health = other.GetComponentInParent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }

            Destroy(gameObject); // TODO: Return to pool
        }
    }
}
