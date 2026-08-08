using UnityEngine;
using SpaceCleaner.Core;
using static SpaceCleaner.Core.HapticType;

namespace SpaceCleaner.Player
{
    /// <summary>
    /// Vacuum collection juice: pop effect, screen flash, ammo bump, collection particles.
    /// Makes collecting trash feel satisfying like Brawl Stars pickups.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class VacuumJuice : MonoBehaviour
    {
        [Header("Collection Pop")]
        [SerializeField] private float popScale = 1.3f;
        [SerializeField] private float popDuration = 0.15f;
        
        [Header("Collection Particles")]
        [SerializeField] private GameObject collectionBurstPrefab;
        [SerializeField] private int burstCount = 12;
        
        [Header("Hit Pause")]
        [SerializeField] private bool useHitPause = true;
        [SerializeField] private float hitPauseDuration = 0.04f;
        private static bool isHitPausing;
        
        private PlayerController player;
        private Transform shipVisual;
        private Vector3 originalScale;
        private float popTimer;
        private bool isPopping;
        private int lastAmmoCount;
        
        private void Awake()
        {
            player = GetComponent<PlayerController>();
            player.OnAmmoChanged += OnAmmoChanged;
            
            // Find visual
            shipVisual = transform.Find("ShipVisual");
            if (shipVisual != null)
                originalScale = shipVisual.localScale;
                
            lastAmmoCount = player.AmmoCount;
        }
        
        private void OnDestroy()
        {
            if (player != null)
                player.OnAmmoChanged -= OnAmmoChanged;
        }
        
        private void OnAmmoChanged(int newAmmo)
        {
            if (newAmmo > lastAmmoCount)
            {
                // Collection happened
                TriggerCollectionJuice();
            }
            lastAmmoCount = newAmmo;
        }
        
        private void TriggerCollectionJuice()
        {
            // Pop animation
            popTimer = 0f;
            isPopping = true;
            
            // Spawn burst particles
            SpawnCollectionBurst(transform.position);
            
            // Micro hit-pause for weight
            if (useHitPause)
                StartCoroutine(HitPauseCoroutine());
                
            // Haptic feedback
            HapticManager.Instance?.Trigger(HapticType.Collect);
        }
        
        private System.Collections.IEnumerator HitPauseCoroutine()
        {
            if (isHitPausing) yield break;
            isHitPausing = true;
            
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 0.15f;
            yield return new WaitForSecondsRealtime(hitPauseDuration);
            Time.timeScale = originalTimeScale;
            
            isHitPausing = false;
        }
        
        private void SpawnCollectionBurst(Vector3 position)
        {
            // Create burst on the fly
            var go = new GameObject("CollectionBurst");
            go.transform.position = position;
            
            var ps = go.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = 0.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            main.startColor = new Color(0.3f, 0.9f, 1f, 0.9f);
            main.maxParticles = 20;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            
            var emission = ps.emission;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, burstCount));
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;
            
            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { 
                    new GradientColorKey(new Color(0.4f, 1f, 1f), 0f), 
                    new GradientColorKey(new Color(0.2f, 0.6f, 1f), 1f) 
                },
                new[] { 
                    new GradientAlphaKey(0.9f, 0f), 
                    new GradientAlphaKey(0.5f, 0.5f),
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            colorOverLife.color = gradient;
            
            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, 0.2f);
            
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
            Destroy(go, 1f);
        }
        
        private void Update()
        {
            UpdatePop();
        }
        
        private void UpdatePop()
        {
            if (shipVisual == null) return;
            
            if (isPopping)
            {
                popTimer += Time.deltaTime;
                float t = popTimer / popDuration;
                
                if (t >= 1f)
                {
                    shipVisual.localScale = originalScale;
                    isPopping = false;
                }
                else
                {
                    // Scale up then back down
                    float scaleMult = 1f + (popScale - 1f) * Mathf.Sin(t * Mathf.PI);
                    shipVisual.localScale = originalScale * scaleMult;
                }
            }
        }
    }
}
