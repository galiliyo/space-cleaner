using UnityEngine;
using SpaceCleaner.Core;
using static SpaceCleaner.Core.SFXType;

namespace SpaceCleaner.Player
{
    /// <summary>
    /// Shooting juice: muzzle flash, screen shake, recoil animation.
    /// Makes shooting feel powerful like Brawl Stars.
    /// </summary>
    [RequireComponent(typeof(ShootingSystem))]
    public class ShootingJuice : MonoBehaviour
    {
        [Header("Muzzle Flash")]
        [SerializeField] private float flashDuration = 0.08f;
        [SerializeField] private float flashIntensity = 2f;
        [SerializeField] private float flashSize = 0.8f;
        [SerializeField] private Color flashColor = new Color(1f, 0.8f, 0.3f, 1f);
        
        [Header("Screen Shake")]
        [SerializeField] private bool enableShake = true;
        [SerializeField] private float shakeIntensity = 0.12f;
        [SerializeField] private float shakeDuration = 0.08f;
        
        [Header("Recoil")]
        [SerializeField] private float recoilDistance = 0.2f;
        [SerializeField] private float recoilRecoverSpeed = 10f;
        
        private ShootingSystem shooting;
        private Transform shipVisual;
        private Transform firePoint;
        private UnityEngine.Camera mainCamera;
        private SpaceCleaner.Camera.SphericalCamera sphericalCamera;
        private Vector3 originalVisualLocalPos;
        private float recoilAmount;
        private float currentShake;
        private float shakeTimer;
        private ParticleSystem muzzleFlashInstance;
        private Light muzzleFlashLight;
        private float flashTimer;
        private int lastAmmoCount;
        
        private void Awake()
        {
            shooting = GetComponent<ShootingSystem>();
            
            shipVisual = transform.Find("ShipVisual");
            if (shipVisual != null)
                originalVisualLocalPos = shipVisual.localPosition;
                
            mainCamera = UnityEngine.Camera.main;
            sphericalCamera = mainCamera != null ? mainCamera.GetComponent<SpaceCleaner.Camera.SphericalCamera>() : null;
            
            // Find fire point
            var fp = transform.Find("FirePoint");
            if (fp != null)
                firePoint = fp;
            else
            {
                var go = new GameObject("FirePoint");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0, 0, 1.5f);
                firePoint = go.transform;
            }
                
            CreateMuzzleFlash();
            
            // Track ammo to detect shots
            var player = GetComponent<PlayerController>();
            if (player != null)
            {
                lastAmmoCount = player.AmmoCount;
                player.OnAmmoChanged += OnAmmoChanged;
            }
        }
        
        private void OnDestroy()
        {
            var player = GetComponent<PlayerController>();
            if (player != null)
                player.OnAmmoChanged -= OnAmmoChanged;
        }
        
        private void OnAmmoChanged(int newAmmo)
        {
            // Ammo decreased = shot fired
            if (newAmmo < lastAmmoCount)
            {
                OnFire();
            }
            lastAmmoCount = newAmmo;
        }
        
        private void CreateMuzzleFlash()
        {
            // Create muzzle flash object
            var go = new GameObject("MuzzleFlash");
            go.transform.SetParent(firePoint, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            
            muzzleFlashInstance = go.AddComponent<ParticleSystem>();
            muzzleFlashInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            var main = muzzleFlashInstance.main;
            main.duration = 0.1f;
            main.startLifetime = 0.06f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            main.startColor = flashColor;
            main.maxParticles = 15;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = false;
            
            var emission = muzzleFlashInstance.emission;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 10));
            
            var shape = muzzleFlashInstance.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.05f;
            
            var colorOverLife = muzzleFlashInstance.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { 
                    new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0f), 
                    new GradientColorKey(new Color(1f, 0.5f, 0.1f), 1f) 
                },
                new[] { 
                    new GradientAlphaKey(1f, 0f), 
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            colorOverLife.color = gradient;
            
            // Point light for flash
            muzzleFlashLight = go.AddComponent<Light>();
            muzzleFlashLight.type = LightType.Point;
            muzzleFlashLight.range = 8f;
            muzzleFlashLight.intensity = 0f;
            muzzleFlashLight.color = new Color(1f, 0.7f, 0.3f);
            
            // Renderer
            var renderer = muzzleFlashInstance.GetComponent<ParticleSystemRenderer>();
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
            
            go.SetActive(false);
        }
        
        private void OnFire()
        {
            TriggerMuzzleFlash();
            TriggerRecoil();
            TriggerShake();
            HapticManager.Instance?.Trigger(HapticType.Shoot);
        }
        
        private void TriggerMuzzleFlash()
        {
            if (muzzleFlashInstance == null) return;
            
            // Random rotation for variety
            muzzleFlashInstance.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            
            muzzleFlashInstance.gameObject.SetActive(true);
            muzzleFlashInstance.Play(true);
            
            // Light flash
            flashTimer = flashDuration;
            
            // Auto-disable
            CancelInvoke(nameof(DisableMuzzleFlash));
            Invoke(nameof(DisableMuzzleFlash), flashDuration * 2f);
        }
        
        private void DisableMuzzleFlash()
        {
            if (muzzleFlashInstance != null)
                muzzleFlashInstance.gameObject.SetActive(false);
        }
        
        private void TriggerRecoil()
        {
            // Push visual back
            recoilAmount = recoilDistance;
        }
        
        private void TriggerShake()
        {
            if (!enableShake) return;
            currentShake = shakeIntensity;
            shakeTimer = shakeDuration;
        }
        
        private void Update()
        {
            UpdateRecoil();
            UpdateShake();
            UpdateFlashLight();
        }
        
        private void UpdateRecoil()
        {
            if (shipVisual == null) return;
            
            // Recover from recoil
            recoilAmount = Mathf.MoveTowards(recoilAmount, 0f, recoilRecoverSpeed * Time.deltaTime);
            
            // Apply to visual
            shipVisual.localPosition = originalVisualLocalPos + Vector3.back * recoilAmount;
        }
        
        private void UpdateShake()
        {
            if (!enableShake) return;
            if (shakeTimer <= 0f) return;
            
            shakeTimer -= Time.deltaTime;
            float t = shakeTimer / shakeDuration;
            float intensity = currentShake * t;
            
            // Apply to camera via SphericalCamera's shake offset
            if (sphericalCamera != null)
            {
                sphericalCamera.AddShake(intensity, shakeDuration);
            }
        }
        
        private void UpdateFlashLight()
        {
            if (muzzleFlashLight == null) return;
            
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                float t = flashTimer / flashDuration;
                muzzleFlashLight.intensity = t * flashIntensity * 2f;
            }
            else
            {
                muzzleFlashLight.intensity = 0f;
            }
        }
    }
}
