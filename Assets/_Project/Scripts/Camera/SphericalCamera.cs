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

        private Vector3 smoothVelocity;

        /// <summary>Adjust elevation at runtime (e.g. from UI slider or scroll wheel).</summary>
        public float Elevation { get => elevation; set => elevation = Mathf.Clamp(value, minElevation, maxElevation); }
        /// <summary>Adjust distance at runtime.</summary>
        public float Distance { get => distance; set => distance = Mathf.Clamp(value, minDistance, maxDistance); }

        public void SetTarget(Transform targetTransform, Transform planetTransform)
        {
            target = targetTransform;
            planet = planetTransform;
        }

        private void LateUpdate()
        {
            if (target == null || planet == null) return;

            // Runtime camera controls: scroll to zoom, Q/E to change elevation
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

            Vector3 up = (target.position - planet.position).normalized;

            // Position behind and above the ship using elevation angle
            Vector3 back = -target.forward;
            float elevRad = elevation * Mathf.Deg2Rad;
            Vector3 desiredPos = target.position
                + up * (distance * Mathf.Sin(elevRad))
                + back * (distance * Mathf.Cos(elevRad));
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref smoothVelocity, smoothTime);

            // Look at ship
            transform.LookAt(target.position, up);
        }
    }
}
