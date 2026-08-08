using UnityEngine;

namespace SpaceCleaner.Core
{
    /// <summary>
    /// Idle sparkle VFX for trash pickups. Makes them more visible and appealing.
    /// </summary>
    public class TrashSparkle : MonoBehaviour
    {
        [Header("Sparkle")]
        [SerializeField] private float sparkleInterval = 0.8f;
        [SerializeField] private float sparkleIntervalVariance = 0.3f;
        [SerializeField] private float sparkleSize = 0.15f;
        [SerializeField] private float sparkleDuration = 0.5f;
        [SerializeField] private Color sparkleColor = new Color(0.4f, 0.9f, 1f, 0.9f);
        
        [Header("Idle Animation")]
        [SerializeField] private float bobHeight = 0.1f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float rotationSpeed = 30f;
        
        private float nextSparkleTime;
        private float bobOffset;
        private Vector3 basePosition;
        private static Material sparkleMaterial;
        
        private void Awake()
        {
            basePosition = transform.position;
            bobOffset = Random.Range(0f, Mathf.PI * 2f);
            nextSparkleTime = Time.time + Random.Range(0f, sparkleInterval);
        }
        
        private void Update()
        {
            UpdateIdleAnimation();
            UpdateSparkles();
        }
        
        private void UpdateIdleAnimation()
        {
            // Bobbing motion
            float bob = Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobHeight;
            transform.position = basePosition + Vector3.up * bob;
            
            // Slow rotation
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
        
        private void UpdateSparkles()
        {
            if (Time.time < nextSparkleTime) return;
            
            SpawnSparkle();
            nextSparkleTime = Time.time + sparkleInterval + Random.Range(-sparkleIntervalVariance, sparkleIntervalVariance);
        }
        
        private void SpawnSparkle()
        {
            var go = new GameObject("TrashSparkle");
            go.transform.position = transform.position + Random.insideUnitSphere * 0.3f;
            
            var ps = go.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.duration = sparkleDuration;
            main.startLifetime = sparkleDuration;
            main.startSpeed = 0f;
            main.startSize = sparkleSize;
            main.startColor = sparkleColor;
            main.maxParticles = 1;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            
            var emission = ps.emission;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 1));
            
            var shape = ps.shape;
            shape.enabled = false;
            
            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(sparkleColor, 0f), new GradientColorKey(sparkleColor, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = gradient;
            
            // Renderer
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            
            if (sparkleMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") 
                    ?? Shader.Find("Universal Render Pipeline/Unlit");
                if (shader != null)
                {
                    sparkleMaterial = new Material(shader);
                    sparkleMaterial.SetFloat("_Surface", 1f);
                    sparkleMaterial.SetFloat("_Blend", 1f);
                }
            }
            if (sparkleMaterial != null)
                renderer.material = sparkleMaterial;
            
            ps.Play();
            Destroy(go, sparkleDuration + 0.1f);
        }
        
        private void OnDisable()
        {
            // Reset position when pooled
            transform.position = basePosition;
        }
    }
}
