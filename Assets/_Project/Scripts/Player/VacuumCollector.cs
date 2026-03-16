using UnityEngine;
using SpaceCleaner.Core;

namespace SpaceCleaner.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class VacuumCollector : MonoBehaviour
    {
        [Header("Collection")]
        [SerializeField] private float collectRadius = 5f;
        [SerializeField] private float lerpSpeed = 15f;
        [SerializeField] private LayerMask trashLayer;

        [Header("Vacuum VFX")]
        [SerializeField] private Color vacuumColorStart = new Color(0.6f, 0.85f, 1f, 0.35f);
        [SerializeField] private Color vacuumColorEnd   = new Color(0.4f, 0.7f, 1f, 0f);
        [SerializeField] private int maxParticles = 25;
        [SerializeField] private float particleSize = 0.15f;
        [SerializeField] private float particleLifetime = 0.5f;

        private static Shader s_ParticleShader;
        private static Shader s_FallbackParticleShader;

        private static Material s_ParticleMaterial;

        private PlayerController playerController;
        private SphereCollider vacuumTrigger;
        private ParticleSystem vacuumVFX;
        private int trashInRangeCount;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();

            // Add sphere trigger for vacuum collection
            vacuumTrigger = gameObject.AddComponent<SphereCollider>();
            vacuumTrigger.isTrigger = true;
            vacuumTrigger.radius = collectRadius;

            CreateVacuumParticleSystem();
        }

        private void OnEnable()
        {
            trashInRangeCount = 0;
        }

        private void CreateVacuumParticleSystem()
        {
            // Create child GameObject for the particle system
            var vfxGO = new GameObject("VacuumVFX");
            vfxGO.transform.SetParent(transform, false);
            vfxGO.transform.localPosition = Vector3.zero;

            vacuumVFX = vfxGO.AddComponent<ParticleSystem>();

            // Stop to configure safely
            vacuumVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // --- Main module ---
            var main = vacuumVFX.main;
            main.loop = true;
            main.startLifetime = particleLifetime;
            main.startSpeed = 0f; // movement handled by velocity-over-lifetime
            main.startSize = particleSize;
            main.startColor = vacuumColorStart;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = false;

            // --- Emission ---
            var emission = vacuumVFX.emission;
            emission.rateOverTime = maxParticles / particleLifetime; // sustain ~maxParticles alive
            emission.enabled = true;

            // --- Shape: emit from the edge of a sphere matching vacuum radius ---
            var shape = vacuumVFX.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = collectRadius;
            shape.radiusThickness = 0f; // emit only on the surface

            // --- Velocity over Lifetime: pull particles inward toward center ---
            var vel = vacuumVFX.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            // Radial velocity: negative = inward. Particles travel ~collectRadius in their lifetime.
            vel.radial = new ParticleSystem.MinMaxCurve(-collectRadius / particleLifetime * 1.2f);

            // --- Size over Lifetime: shrink as they approach center ---
            var sizeOverLife = vacuumVFX.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0.2f)
            ));

            // --- Color over Lifetime: fade from cyan to transparent blue ---
            var colorOverLife = vacuumVFX.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(vacuumColorStart, 0f), new GradientColorKey(vacuumColorEnd, 1f) },
                new[] { new GradientAlphaKey(vacuumColorStart.a, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = new ParticleSystem.MinMaxGradient(gradient);

            // --- Renderer: use default particle with additive URP-compatible material ---
            var renderer = vfxGO.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            // Create a simple additive unlit particle material (URP compatible, shared across instances)
            if (s_ParticleMaterial == null)
            {
                if (s_ParticleShader == null) s_ParticleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                var mat = new Material(s_ParticleShader);
                if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                {
                    // Fallback if URP particles shader not found
                    if (s_FallbackParticleShader == null) s_FallbackParticleShader = Shader.Find("Particles/Standard Unlit");
                    mat = new Material(s_FallbackParticleShader);
                }
                mat.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
                mat.SetFloat("_Blend", 1f);   // 0 = Alpha, 1 = Additive
                mat.SetColor("_BaseColor", Color.white);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.EnableKeyword("_BLENDMODE_ADD");
                mat.renderQueue = 3000;
                mat.SetTexture("_BaseMap", CreateSoftCircleTexture(32));
                s_ParticleMaterial = mat;
            }
            renderer.sharedMaterial = s_ParticleMaterial;
        }

        private void LateUpdate()
        {
            if (trashInRangeCount > 0 && !vacuumVFX.isPlaying)
            {
                vacuumVFX.Play();
            }
            else if (trashInRangeCount <= 0 && vacuumVFX.isPlaying)
            {
                vacuumVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & trashLayer) == 0) return;
            trashInRangeCount++;
            var trash = other.GetComponent<TrashPickup>();
            if (trash != null && !trash.IsBeingCollected)
                trash.StartCollection(transform, lerpSpeed);
        }

        private void OnTriggerExit(Collider other)
        {
            if (((1 << other.gameObject.layer) & trashLayer) == 0) return;
            trashInRangeCount = Mathf.Max(0, trashInRangeCount - 1);
        }

        private static Texture2D CreateSoftCircleTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float center = size * 0.5f;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center + 0.5f) / center;
                    float dy = (y - center + 0.5f) / center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha *= alpha; // quadratic falloff for soft edges
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return tex;
        }
    }
}
