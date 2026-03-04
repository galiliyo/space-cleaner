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
        [SerializeField] private float elevation = 65f; // degrees from surface tangent
        [SerializeField] private float smoothSpeed = 8f;

        private Vector3 currentVelocity;

        public void SetTarget(Transform targetTransform, Transform planetTransform)
        {
            target = targetTransform;
            planet = planetTransform;
        }

        private void LateUpdate()
        {
            if (target == null || planet == null) return;

            // Surface normal at target position
            Vector3 up = (target.position - planet.position).normalized;

            // Camera sits behind and above the target on the sphere surface
            Vector3 back = -target.forward;
            Vector3 elevatedDir = Vector3.Slerp(back, up, elevation / 90f).normalized;

            Vector3 desiredPos = target.position + elevatedDir * distance;

            // Smooth follow
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref currentVelocity, 1f / smoothSpeed);

            // Look at a point slightly ahead of the target
            Vector3 lookTarget = target.position + target.forward * 3f;
            transform.LookAt(lookTarget, up);
        }
    }
}
