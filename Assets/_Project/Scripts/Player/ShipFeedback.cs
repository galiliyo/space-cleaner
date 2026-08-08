using UnityEngine;

namespace SpaceCleaner.Player
{
    /// <summary>
    /// Ship visual feedback: banking when turning, thruster VFX, speed-based FOV shifts.
    /// Makes the ship feel weighty and responsive like Everspace.
    /// </summary>
    [RequireComponent(typeof(SphericalMovement))]
    public class ShipFeedback : MonoBehaviour
    {
        [Header("Banking")]
        [SerializeField] private float maxBankAngle = 35f;
        [SerializeField] private float bankSpeed = 8f;
        [SerializeField] private float bankDecay = 5f;
        
        [Header("Thruster VFX")]
        [SerializeField] private ParticleSystem leftThruster;
        [SerializeField] private ParticleSystem rightThruster;
        [SerializeField] private ParticleSystem mainThruster;
        [SerializeField] private float minThrusterRate = 10f;
        [SerializeField] private float maxThrusterRate = 60f;
        [SerializeField] private Color thrusterColorNormal = new Color(0.3f, 0.7f, 1f, 0.8f);
        [SerializeField] private Color thrusterColorBoost = new Color(0.2f, 0.9f, 1f, 0.9f);
        
        [Header("Camera FOV")]
        [SerializeField] private UnityEngine.Camera playerCamera;
        [SerializeField] private float baseFOV = 60f;
        [SerializeField] private float maxFOV = 70f;
        [SerializeField] private float fovChangeSpeed = 3f;
        
        private SphericalMovement movement;
        private Transform shipVisual;
        private Vector3 lastPosition;
        private Vector3 velocity;
        private float currentBankAngle;
        private float targetBankAngle;
        private float currentFOV;
        
        private void Awake()
        {
            movement = GetComponent<SphericalMovement>();
            currentFOV = baseFOV;
            
            if (playerCamera == null)
                playerCamera = UnityEngine.Camera.main;
                
            // Find or create the visual child that will handle banking
            FindOrCreateShipVisual();
            CreateThrustersIfNeeded();
        }
        
        private void FindOrCreateShipVisual()
        {
            // Look for an existing visual child
            shipVisual = transform.Find("ShipVisual");
            
            // Also check for common ship model names
            if (shipVisual == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.name.ToLower().Contains("ship") || 
                        child.name.ToLower().Contains("model") ||
                        child.name.ToLower().Contains("mesh") ||
                        child.name.ToLower().Contains("craft") ||
                        child.name.ToLower().Contains("body"))
                    {
                        shipVisual = child;
                        shipVisual.name = "ShipVisual";
                        break;
                    }
                }
            }
            
            // If no visual found, create a container for it
            if (shipVisual == null)
            {
                var go = new GameObject("ShipVisual");
                go.transform.SetParent(transform, false);
                shipVisual = go.transform;
            }
            
            // Ensure shipVisual has zero local position/rotation
            shipVisual.localPosition = Vector3.zero;
            shipVisual.localRotation = Quaternion.identity;
        }
        
        private void CreateThrustersIfNeeded()
        {
            // Thrusters are attached to the visual child so they bank with it
            if (shipVisual == null) return;
            
            // Main thruster at back of ship
            if (mainThruster == null)
            {
                var go = new GameObject("MainThruster");
                go.transform.SetParent(shipVisual, false);
                go.transform.localPosition = new Vector3(0, 0, -1.2f);
                go.transform.localRotation = Quaternion.Euler(0, 180, 0);
                mainThruster = CreateThrusterParticles(go);
            }
            
            // Side thrusters for banking visualization
            if (leftThruster == null)
            {
                var go = new GameObject("LeftThruster");
                go.transform.SetParent(shipVisual, false);
                go.transform.localPosition = new Vector3(-0.6f, 0, -0.8f);
                go.transform.localRotation = Quaternion.Euler(0, 150, 0);
                leftThruster = CreateThrusterParticles(go);
            }
            
            if (rightThruster == null)
            {
                var go = new GameObject("RightThruster");
                go.transform.SetParent(shipVisual, false);
                go.transform.localPosition = new Vector3(0.6f, 0, -0.8f);
                go.transform.localRotation = Quaternion.Euler(0, 210, 0);
                rightThruster = CreateThrusterParticles(go);
            }
        }
        
        private ParticleSystem CreateThrusterParticles(GameObject parent)
        {
            var ps = parent.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 0.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.25f);
            main.startColor = new Color(0.3f, 0.8f, 1f, 0.7f);
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            
            var emission = ps.emission;
            emission.rateOverTime = minThrusterRate;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.1f;
            
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            
            // Trail/color over lifetime
            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.4f, 0.9f, 1f), 0f), new GradientColorKey(new Color(0.2f, 0.5f, 0.8f), 1f) },
                new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0.3f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = gradient;
            
            // Size shrinks over time
            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, 0.3f);
            
            // URP renderer
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
            
            return ps;
        }
        
        private void Update()
        {
            CalculateVelocity();
            UpdateBanking();
            UpdateThrusters();
            UpdateFOV();
        }
        
        private void CalculateVelocity()
        {
            if (lastPosition != Vector3.zero)
            {
                velocity = (transform.position - lastPosition) / Time.deltaTime;
            }
            lastPosition = transform.position;
        }
        
        private void UpdateBanking()
        {
            if (shipVisual == null) return;
            
            // Calculate bank based on turning velocity relative to forward
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);
            
            // Bank when turning left/right (x component of local velocity)
            float turnAmount = Mathf.Clamp(localVelocity.x / 3f, -1f, 1f);
            targetBankAngle = -turnAmount * maxBankAngle; // Negative because left turn = bank left (negative Z rotation)
            
            // Smooth interpolation
            float bankDelta = targetBankAngle - currentBankAngle;
            float speed = Mathf.Abs(bankDelta) > 0.1f ? bankSpeed : bankDecay;
            currentBankAngle = Mathf.MoveTowards(currentBankAngle, targetBankAngle, speed * Time.deltaTime * Mathf.Abs(bankDelta));
            
            // Apply banking as local Z rotation to the visual child only
            shipVisual.localRotation = Quaternion.Euler(0, 0, currentBankAngle);
        }
        
        private void UpdateThrusters()
        {
            float speed = velocity.magnitude;
            float normalizedSpeed = Mathf.Clamp01(speed / 5f);
            
            // Emission rate based on speed
            float emissionRate = Mathf.Lerp(minThrusterRate, maxThrusterRate, normalizedSpeed);
            
            if (mainThruster != null)
            {
                SetEmissionRate(mainThruster, emissionRate * 1.5f);
                
                if (normalizedSpeed > 0.1f && !mainThruster.isPlaying)
                    mainThruster.Play();
                else if (normalizedSpeed <= 0.1f && mainThruster.isPlaying)
                    mainThruster.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            
            // Side thrusters fire when banking
            float bankFactor = Mathf.Abs(currentBankAngle) / maxBankAngle;
            
            if (leftThruster != null)
            {
                float leftEmission = currentBankAngle < -5f ? emissionRate * bankFactor : minThrusterRate * 0.3f;
                SetEmissionRate(leftThruster, leftEmission);
                
                if (leftEmission > minThrusterRate && !leftThruster.isPlaying)
                    leftThruster.Play();
                else if (leftEmission <= minThrusterRate && leftThruster.isPlaying)
                    leftThruster.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            
            if (rightThruster != null)
            {
                float rightEmission = currentBankAngle > 5f ? emissionRate * bankFactor : minThrusterRate * 0.3f;
                SetEmissionRate(rightThruster, rightEmission);
                
                if (rightEmission > minThrusterRate && !rightThruster.isPlaying)
                    rightThruster.Play();
                else if (rightEmission <= minThrusterRate && rightThruster.isPlaying)
                    rightThruster.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
        
        private void UpdateFOV()
        {
            if (playerCamera == null) return;
            
            float speed = velocity.magnitude;
            float targetFOV = Mathf.Lerp(baseFOV, maxFOV, Mathf.Clamp01((speed - 3f) / 4f));
            
            currentFOV = Mathf.MoveTowards(currentFOV, targetFOV, fovChangeSpeed * Time.deltaTime);
            playerCamera.fieldOfView = currentFOV;
        }
        
        private void OnDisable()
        {
            // Stop all thrusters when disabled
            if (mainThruster != null) mainThruster.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (leftThruster != null) leftThruster.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (rightThruster != null) rightThruster.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        
        /// <summary>
        /// Helper to set emission rate without CS1612 error
        /// </summary>
        private void SetEmissionRate(ParticleSystem ps, float rate)
        {
            var em = ps.emission;
            em.rateOverTime = rate;
        }
    }
}
