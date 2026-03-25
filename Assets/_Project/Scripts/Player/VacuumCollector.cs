using UnityEngine;
using SpaceCleaner.Core;
using static SpaceCleaner.Core.SFXType;

namespace SpaceCleaner.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class VacuumCollector : MonoBehaviour
    {
        [Header("Collection")]
        [SerializeField] private float collectRadius = 2.5f;
        [SerializeField] private float lerpSpeed = 15f;
        [SerializeField] private LayerMask trashLayer;

        [Header("Vacuum VFX")]
        [SerializeField] private Color vacuumColorStart = new Color(0.7f, 0.95f, 1f, 0.3f);
        [SerializeField] private Color vacuumColorEnd   = new Color(1f, 1f, 1f, 0f);
        [SerializeField] private int maxParticles = 25;
        [SerializeField] private float particleSize = 0.04f;
        [SerializeField] private float particleLifetime = 0.6f;

        private static Shader s_ParticleShader;
        private static Shader s_FallbackParticleShader;

        private static Material s_ParticleMaterial;

        private PlayerController playerController;
        private SphereCollider vacuumTrigger;
        private ParticleSystem vacuumVFX;
        private ParticleSystem vortexRingPS;
        private ParticleSystem vortexGlowPS;
        private int trashInRangeCount;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();

            // Add sphere trigger for vacuum collection
            vacuumTrigger = gameObject.AddComponent<SphereCollider>();
            vacuumTrigger.isTrigger = true;
            vacuumTrigger.radius = collectRadius;

            CreateVacuumParticleSystem();
            CreateVortexRing();
            CreateVortexGlow();
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
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
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
            vel.radial = new ParticleSystem.MinMaxCurve(-collectRadius / particleLifetime * 1.8f);

            // --- Size over Lifetime: shrink as they approach center ---
            var sizeOverLife = vacuumVFX.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0.2f)
            ));

            // --- Color over Lifetime: fade with white flash at 30% ---
            var colorOverLife = vacuumVFX.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(vacuumColorStart, 0f),
                    new GradientColorKey(new Color(1f, 1f, 1f), 0.3f),
                    new GradientColorKey(vacuumColorEnd, 1f)
                },
                new[] {
                    new GradientAlphaKey(0.3f, 0f),
                    new GradientAlphaKey(0.5f, 0.3f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLife.color = new ParticleSystem.MinMaxGradient(gradient);

            // --- Renderer: use stretched billboard with additive URP-compatible material ---
            var renderer = vfxGO.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 4f;
            renderer.velocityScale = 0.5f;

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

        private void CreateVortexRing()
        {
            var ringGO = new GameObject("VortexRing");
            ringGO.transform.SetParent(transform, false);
            ringGO.transform.localPosition = Vector3.zero;

            var ringPS = ringGO.AddComponent<ParticleSystem>();
            ringPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ringPS.main;
            main.loop = true;
            main.startLifetime = 1.5f;
            main.startSpeed = 0f;
            main.startSize = 0.04f;
            main.startColor = new Color(0.5f, 0.9f, 1f, 0.25f);
            main.maxParticles = 6;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = false;

            var emission = ringPS.emission;
            emission.rateOverTime = 4f;
            emission.enabled = true;

            var shape = ringPS.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;
            shape.radiusThickness = 0f;

            // Orbital velocity for spinning
            var vel = ringPS.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.orbitalY = new ParticleSystem.MinMaxCurve(3f);

            var sizeOverLife = ringPS.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.8f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0.3f)
            ));

            var colorOverLife = ringPS.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.5f, 0.9f, 1f), 0f), new GradientColorKey(new Color(0.3f, 0.7f, 1f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.25f, 0.2f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = new ParticleSystem.MinMaxGradient(gradient);

            var renderer = ringGO.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = s_ParticleMaterial;

            // Store reference to control play/stop with main system
            vortexRingPS = ringPS;
        }

        private void CreateVortexGlow()
        {
            var glowGO = new GameObject("VortexGlow");
            glowGO.transform.SetParent(transform, false);
            glowGO.transform.localPosition = Vector3.zero;

            var glowPS = glowGO.AddComponent<ParticleSystem>();
            glowPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = glowPS.main;
            main.loop = true;
            main.startLifetime = 2f;
            main.startSpeed = 0f;
            main.startSize = 0.4f;
            main.startColor = new Color(0.4f, 0.85f, 1f, 0.1f);
            main.maxParticles = 2;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = false;

            var emission = glowPS.emission;
            emission.rateOverTime = 1f;
            emission.enabled = true;

            var shape = glowPS.shape;
            shape.enabled = false; // emit at local origin

            // Pulsing size
            var sizeOverLife = glowPS.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.7f),
                new Keyframe(0.25f, 1.2f),
                new Keyframe(0.5f, 0.8f),
                new Keyframe(0.75f, 1.1f),
                new Keyframe(1f, 0.6f)
            ));

            var colorOverLife = glowPS.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.4f, 0.85f, 1f), 0f), new GradientColorKey(new Color(0.3f, 0.7f, 1f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.1f, 0.3f), new GradientAlphaKey(0.08f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = new ParticleSystem.MinMaxGradient(gradient);

            var renderer = glowGO.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = s_ParticleMaterial;

            vortexGlowPS = glowPS;
        }

        private void LateUpdate()
        {
            // Validate stale count: trash destroyed/pooled may not fire OnTriggerExit
            if (trashInRangeCount > 0)
            {
                bool anyNearby = false;
                var pos = transform.position;
                float rSq = collectRadius * collectRadius * 1.5f;
                foreach (var trash in TrashPickup.ActiveInstances)
                {
                    if ((trash.transform.position - pos).sqrMagnitude <= rSq)
                    {
                        anyNearby = true;
                        break;
                    }
                }
                if (!anyNearby) trashInRangeCount = 0;
            }

            if (trashInRangeCount > 0 && !vacuumVFX.isPlaying)
            {
                SFXManager.Instance?.Play(VacuumStart);
                vacuumVFX.Play();
                if (vortexRingPS != null) vortexRingPS.Play();
                if (vortexGlowPS != null) vortexGlowPS.Play();
            }
            else if (trashInRangeCount <= 0 && vacuumVFX.isPlaying)
            {
                SFXManager.Instance?.Play(VacuumStop);
                vacuumVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (vortexRingPS != null) vortexRingPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (vortexGlowPS != null) vortexGlowPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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
