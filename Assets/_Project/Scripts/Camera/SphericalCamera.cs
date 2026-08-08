using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceCleaner.Camera
{
    public class SphericalCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Transform planet;

        [Header("Camera Settings")]
        [SerializeField] private float distance = 20f;
        [SerializeField] private float elevation = 45f; // degrees from surface tangent
        [SerializeField] private float zoomSpeed = 20f;
        [SerializeField] private float elevationSpeed = 30f;
        [SerializeField] private float minElevation = 10f;
        [SerializeField] private float maxElevation = 85f;
        [SerializeField] private float minDistance = 5f;
        [SerializeField] private float maxDistance = 60f;
        [SerializeField] private float smoothTime = 0.1f;
        
        [Header("Lookahead")]
        [SerializeField] private float lookaheadDistance = 5f;
        [SerializeField] private float lookaheadSmoothTime = 0.3f;
        [SerializeField] private bool useAimForLookahead = true;
        
        [Header("Dynamic Framing")]
        [SerializeField] private float framingOffset = 0.5f; // 0 = center, 1 = full offset toward movement

        private Vector3 smoothVelocity;
        private bool _checkDesktopInput;
        
        // Camera shake
        private Vector3 shakeOffset;
        private float shakeIntensity;
        private float shakeDuration;
        private float shakeTimer;
        
        // Lookahead
        private Vector3 lookaheadOffset;
        private Vector3 lookaheadVelocity;
        private Vector3 currentLookahead;
        
        private Vector3 CalculateLookahead()
        {
            if (target == null || planet == null) return Vector3.zero;
            
            Vector3 lookahead = Vector3.zero;
            Vector3 up = (target.position - planet.position).normalized;
            
            // Calculate velocity from position delta
            Vector3 velocity = (target.position - lastTargetPos) / Time.deltaTime;
            lastTargetPos = target.position;
            
            // Project onto tangent plane and scale by speed
            Vector3 tangentVel = Vector3.ProjectOnPlane(velocity, up);
            float speed = tangentVel.magnitude;
            if (speed > 0.1f)
            {
                lookahead += tangentVel.normalized * lookaheadDistance * Mathf.Clamp01(speed / 8f);
            }
            
            return lookahead;
        }
        
        private Vector3 lastTargetPos;
        
        public void AddShake(float intensity, float duration)
        {
            shakeIntensity = Mathf.Max(shakeIntensity, intensity);
            shakeDuration = Mathf.Max(shakeDuration, duration);
            shakeTimer = shakeDuration;
        }

        /// <summary>Adjust elevation at runtime (e.g. from UI slider or scroll wheel).</summary>
        public float Elevation { get => elevation; set => elevation = Mathf.Clamp(value, minElevation, maxElevation); }
        /// <summary>Adjust distance at runtime.</summary>
        public float Distance { get => distance; set => distance = Mathf.Clamp(value, minDistance, maxDistance); }

        public void SetTarget(Transform targetTransform, Transform planetTransform)
        {
            target = targetTransform;
            planet = planetTransform;
        }

        /// <summary>
        /// Zeroes smoothing velocity and snaps the camera to the correct position
        /// immediately. Call after teleporting the target (e.g. respawn).
        /// </summary>
        public void SnapImmediate()
        {
            smoothVelocity = Vector3.zero;
            if (target == null || planet == null) return;

            Vector3 up = (target.position - planet.position).normalized;
            Vector3 back = -target.forward;
            float elevRad = elevation * Mathf.Deg2Rad;
            transform.position = target.position
                + up * (distance * Mathf.Sin(elevRad))
                + back * (distance * Mathf.Cos(elevRad));
            transform.LookAt(target.position, up);
        }

        private void Start()
        {
#if UNITY_ANDROID || UNITY_IOS
            _checkDesktopInput = false;
#else
            _checkDesktopInput = true;
#endif
        }

        private void LateUpdate()
        {
            if (target == null || planet == null) return;

            // Runtime camera controls: scroll to zoom, Q/E to change elevation
            if (_checkDesktopInput)
            {
                var keyboard = Keyboard.current;
                var mouse = Mouse.current;

                if (mouse != null)
                {
                    float scroll = mouse.scroll.y.ReadValue();
                    if (scroll != 0f)
                        Distance -= scroll * zoomSpeed * Time.deltaTime;
                }

                if (keyboard != null)
                {
                    if (keyboard.qKey.isPressed)
                        Elevation += elevationSpeed * Time.deltaTime;
                    if (keyboard.eKey.isPressed)
                        Elevation -= elevationSpeed * Time.deltaTime;
                }
            }

            Vector3 up = (target.position - planet.position).normalized;

            // Position behind and above the ship using elevation angle
            Vector3 back = -target.forward;
            float elevRad = elevation * Mathf.Deg2Rad;
            Vector3 desiredPos = target.position
                + up * (distance * Mathf.Sin(elevRad))
                + back * (distance * Mathf.Cos(elevRad));
            Vector3 finalPos = Vector3.SmoothDamp(transform.position, desiredPos, ref smoothVelocity, smoothTime);
            
            // Calculate lookahead based on movement and aim
            Vector3 lookahead = CalculateLookahead();
            currentLookahead = Vector3.SmoothDamp(currentLookahead, lookahead, ref lookaheadVelocity, lookaheadSmoothTime);
            
            // Apply lookahead to target point
            Vector3 targetPos = target.position + currentLookahead;
            
            // Apply shake
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                float t = shakeTimer / shakeDuration;
                shakeOffset = Random.insideUnitSphere * shakeIntensity * t;
                shakeOffset.z = 0;
                finalPos += shakeOffset;
            }

            transform.position = finalPos;
            transform.LookAt(targetPos, up);
        }
    }
}
