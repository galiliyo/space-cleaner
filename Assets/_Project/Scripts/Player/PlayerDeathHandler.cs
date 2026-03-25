using System.Collections;
using UnityEngine;
using SpaceCleaner.Core;
using SpaceCleaner.Camera;
using SpaceCleaner.UI;

namespace SpaceCleaner.Player
{
    /// <summary>
    /// Orchestrates the player death experience: freeze frame, launch-off animation,
    /// death overlay, and respawn. Implements the spec from 2026-03-20.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(Health))]
    public class PlayerDeathHandler : MonoBehaviour
    {
        [Header("Death Animation")]
        [SerializeField] private float freezeFrameDuration = 0.05f;
        [SerializeField] private float launchSpeed = 8f;
        [SerializeField] private float launchDuration = 2f;
        [SerializeField] private float spinSpeed = 120f;

        [Header("VFX")]
        [SerializeField] private Color explosionColor = new Color(1f, 0.6f, 0.2f, 1f);
        [SerializeField] private int explosionParticleCount = 40;
        [SerializeField] private float explosionRadius = 2f;

        [Header("Respawn")]
        [SerializeField] private float dropInDuration = 0.5f;
        [SerializeField] private float dropInHeight = 3f;
        [SerializeField] private float invincibilityDuration = 2f;
        [SerializeField] private float blinkInterval = 0.1f;

        [Header("Audio (assign to override procedural SFX)")]
        [SerializeField] private AudioClip deathImpactClip;
        [SerializeField] private AudioClip launchClip;
        [SerializeField] private AudioClip respawnClip;
        [SerializeField] private AudioClip retryClickClip;

        private PlayerController playerController;
        private Health health;
        private SphericalMovement movement;
        private SphericalCamera sphericalCamera;
        private AudioSource audioSource;

        private Vector3 deathPosition;
        private Quaternion deathRotation;
        private bool isDying;

        private DeathOverlayUI deathOverlay;

        // Procedural SFX generated once
        private AudioClip proceduralExplosionClip;
        private AudioClip proceduralWhooshClip;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            health = GetComponent<Health>();
            movement = GetComponent<SphericalMovement>();
            sphericalCamera = FindAnyObjectByType<SphericalCamera>();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            GenerateProceduralSFX();
        }

        private void OnEnable()
        {
            health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            health.OnDeath -= HandleDeath;
        }

        private void HandleDeath()
        {
            if (isDying) return; // idempotent
            isDying = true;
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            // Notify systems
            playerController.IsDead = true;
            GameManager.Instance?.NotifyPlayerDeath();

            // Cache death position for respawn
            deathPosition = transform.position;
            deathRotation = transform.rotation;

            // 1. Freeze frame + explosion VFX/SFX
            PlayClip(deathImpactClip, proceduralExplosionClip);
            SpawnExplosionVFX(transform.position);
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(freezeFrameDuration);
            Time.timeScale = 1f;

            // 2. Launch off planet
            PlayClip(launchClip, proceduralWhooshClip);
            movement.enabled = false;

            Vector3 launchDir = movement.Planet != null
                ? (transform.position - movement.Planet.position).normalized
                : transform.up;

            // Add slight random spin axis
            Vector3 spinAxis = (launchDir + Random.insideUnitSphere * 0.3f).normalized;

            float elapsed = 0f;
            Vector3 startCamOffset = Vector3.zero;
            if (sphericalCamera != null)
                startCamOffset = sphericalCamera.transform.position - transform.position;

            while (elapsed < launchDuration)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                // Move ship outward
                transform.position += launchDir * launchSpeed * dt;

                // Tumble
                transform.Rotate(spinAxis, spinSpeed * dt, Space.World);

                // Simple smooth-follow camera during launch
                if (sphericalCamera != null)
                {
                    Vector3 desiredCamPos = transform.position + startCamOffset;
                    sphericalCamera.transform.position = Vector3.Lerp(
                        sphericalCamera.transform.position, desiredCamPos, 5f * dt);
                    sphericalCamera.transform.LookAt(transform.position);
                    // Disable SphericalCamera LateUpdate by temporarily nulling target
                    // (it early-returns if target == null)
                }

                yield return null;
            }

            // 3. Fade to overlay
            if (deathOverlay == null)
                deathOverlay = CreateDeathOverlay();

            // Pause gameplay behind overlay
            GameManager.Instance?.PauseGame();

            yield return deathOverlay.FadeIn(0.5f);
        }

        /// <summary>Called by DeathOverlayUI when player taps Retry.</summary>
        public void OnRetry()
        {
            PlayClip(retryClickClip);
            StartCoroutine(RespawnSequence());
        }

        private IEnumerator RespawnSequence()
        {
            // 1. Fade out overlay
            yield return deathOverlay.FadeOut(0.3f);

            // 2. Reset player state
            health.Revive();
            playerController.ResetAmmo();
            playerController.IsDead = false;

            // 3. Reposition at death location, slightly above surface
            Vector3 aboveDeath = deathPosition;
            if (movement.Planet != null)
            {
                Vector3 upDir = (deathPosition - movement.Planet.position).normalized;
                aboveDeath = deathPosition + upDir * dropInHeight;
            }
            transform.position = aboveDeath;
            transform.rotation = deathRotation;

            // Re-enable movement and unpause BEFORE drop-in so deltaTime works
            movement.enabled = true;
            GameManager.Instance?.ResumeGame();

            // 4. Drop-in animation
            PlayClip(respawnClip);
            float elapsed = 0f;
            while (elapsed < dropInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dropInDuration);
                // Ease-out quadratic
                float eased = 1f - (1f - t) * (1f - t);
                transform.position = Vector3.Lerp(aboveDeath, deathPosition, eased);
                yield return null;
            }
            transform.position = deathPosition;

            // Restore camera
            if (sphericalCamera != null)
                sphericalCamera.SetTarget(transform, movement.Planet);

            // Re-show HUD
            var hud = FindAnyObjectByType<GameplayHUD>();
            if (hud != null) hud.ShowHUD();

            // 5. Invincibility window with blinking
            yield return InvincibilityCoroutine();

            isDying = false;
        }

        private IEnumerator InvincibilityCoroutine()
        {
            float elapsed = 0f;
            var renderers = GetComponentsInChildren<Renderer>();
            bool visible = true;

            while (elapsed < invincibilityDuration)
            {
                elapsed += Time.deltaTime;

                // Blink all renderers
                visible = !visible;
                foreach (var r in renderers)
                    r.enabled = visible;

                yield return new WaitForSeconds(blinkInterval);
            }

            // Ensure visible at end
            foreach (var r in renderers)
                r.enabled = true;
        }

        private DeathOverlayUI CreateDeathOverlay()
        {
            // Always create a dedicated ScreenSpaceOverlay canvas so the death lobby
            // covers everything regardless of what other canvases exist in the scene.
            var canvasGO = new GameObject("DeathCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            // Add CanvasScaler for consistent layout across resolutions
            var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var overlayGO = new GameObject("DeathOverlay");
            overlayGO.transform.SetParent(canvas.transform, false);
            var overlay = overlayGO.AddComponent<DeathOverlayUI>();
            overlay.Initialize(this);
            return overlay;
        }

        /// <summary>Plays the assigned clip, or falls back to a procedural clip if none assigned.</summary>
        private void PlayClip(AudioClip assignedClip, AudioClip proceduralFallback = null)
        {
            if (audioSource == null) return;
            var clip = assignedClip != null ? assignedClip : proceduralFallback;
            if (clip != null)
                audioSource.PlayOneShot(clip);
        }

        // ───── VFX ─────

        private void SpawnExplosionVFX(Vector3 position)
        {
            var vfxGO = new GameObject("DeathExplosionVFX");
            vfxGO.transform.position = position;

            var ps = vfxGO.AddComponent<ParticleSystem>();

            // Stop auto-play so we can configure first
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startColor = explosionColor;
            main.maxParticles = explosionParticleCount;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)explosionParticleCount) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(explosionColor, 0f),
                    new GradientColorKey(new Color(1f, 0.2f, 0f), 0.5f),
                    new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 1f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.4f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            // Use default particle material (works with URP)
            var renderer = vfxGO.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            renderer.material.color = explosionColor;

            ps.Play();
        }

        // ───── Procedural SFX Generation ─────

        private void GenerateProceduralSFX()
        {
            proceduralExplosionClip = GenerateExplosionClip();
            proceduralWhooshClip = GenerateWhooshClip();
        }

        private static AudioClip GenerateExplosionClip()
        {
            int sampleRate = 22050;
            float duration = 0.6f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                // White noise with fast exponential decay
                float envelope = Mathf.Exp(-t * 6f);
                // Mix noise + low-frequency rumble
                float noise = (Random.value * 2f - 1f) * 0.7f;
                float rumble = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.3f;
                samples[i] = (noise + rumble) * envelope * 0.8f;
            }

            var clip = AudioClip.Create("ProceduralExplosion", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateWhooshClip()
        {
            int sampleRate = 22050;
            float duration = 1.0f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                // Whoosh = filtered noise with rising then falling envelope
                float envelope = Mathf.Sin(t * Mathf.PI) * 0.6f;
                // Frequency-modulated noise for a "sweeping" feel
                float freq = Mathf.Lerp(200f, 800f, t);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                float noise = (Random.value * 2f - 1f) * 0.3f;
                samples[i] = (wave * 0.5f + noise) * envelope;
            }

            var clip = AudioClip.Create("ProceduralWhoosh", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
