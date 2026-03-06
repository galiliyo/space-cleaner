using UnityEngine;

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
        public void SetTarget(Transform targetTransform, Transform planetTransform)
        {
            target = targetTransform;
            planet = planetTransform;
        }

        private void LateUpdate()
        {
            if (target == null || planet == null) return;

            Vector3 up = (target.position - planet.position).normalized;

            // Position behind and above the ship using elevation angle
            Vector3 back = -target.forward;
            float elevRad = elevation * Mathf.Deg2Rad;
            transform.position = target.position
                + up * (distance * Mathf.Sin(elevRad))
                + back * (distance * Mathf.Cos(elevRad));

            // Look at ship
            transform.LookAt(target.position, up);
        }
    }
}
