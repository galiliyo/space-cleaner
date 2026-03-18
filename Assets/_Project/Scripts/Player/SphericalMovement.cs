using UnityEngine;

namespace SpaceCleaner.Player
{
    public class SphericalMovement : MonoBehaviour
    {
        [Header("Planet Reference")]
        [SerializeField] private Transform planet;
        [SerializeField] private float orbitRadius = 52f; // planet radius + hover height

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 20f;
        [SerializeField] private float rotationSmoothSpeed = 10f;

        private Vector2 moveInput;
        private Vector3 velocity;

        public Transform Planet => planet;
        public float OrbitRadius => orbitRadius;

        public void SetPlanet(Transform planetTransform, float radius)
        {
            planet = planetTransform;
            orbitRadius = radius;
            loggedNoPlanet = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[SphericalMovement] Planet set: {planetTransform?.name}, orbitRadius={radius}");
#endif
            SnapToSurface();
        }

        public void SetMoveInput(Vector2 input)
        {
            moveInput = input;
        }

        private bool loggedNoPlanet;

        private void Update()
        {
            if (planet == null)
            {
                if (!loggedNoPlanet)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning("[SphericalMovement] Planet reference is null — waiting for LevelSetup to assign it.");
#endif
                    loggedNoPlanet = true;
                }
                return;
            }
            if (moveInput.sqrMagnitude < 0.01f) return;

            // Get local "right" and "forward" directions relative to sphere surface
            Vector3 up = (transform.position - planet.position).normalized;
            Vector3 right = Vector3.Cross(up, transform.forward).normalized;
            Vector3 forward = Vector3.Cross(right, up).normalized;

            // Recalculate right to ensure orthogonality
            right = Vector3.Cross(up, forward).normalized;

            // Build movement direction from input
            Vector3 moveDir = (forward * moveInput.y + right * moveInput.x).normalized;

            // Angular velocity on sphere surface
            float angularSpeed = moveSpeed / orbitRadius; // radians per second
            float angle = angularSpeed * Time.deltaTime;

            // Rotate position around planet center
            Vector3 fromCenter = transform.position - planet.position;
            Vector3 rotationAxis = Vector3.Cross(fromCenter.normalized, moveDir).normalized;

            if (rotationAxis.sqrMagnitude < 0.001f) return;

            Quaternion rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, rotationAxis);
            Vector3 newPos = planet.position + rotation * fromCenter;

            transform.position = newPos;

            // Align up vector to surface normal, face movement direction
            Vector3 newUp = (newPos - planet.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(moveDir, newUp);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmoothSpeed * Time.deltaTime);

            // Snap to exact radius to prevent drift
            SnapToSurface();
        }

        private void SnapToSurface()
        {
            if (planet == null) return;
            Vector3 dir = (transform.position - planet.position).normalized;
            transform.position = planet.position + dir * orbitRadius;
        }
    }
}
