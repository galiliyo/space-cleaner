using UnityEngine;

namespace SpaceCleaner.Player
{
    public class SphericalMovement : MonoBehaviour
    {
        [Header("Planet Reference")]
        [SerializeField] private Transform planet;
        [SerializeField] private float orbitRadius = 52f; // planet radius + hover height

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2.7f;
        [Tooltip("Max degrees per second the ship can turn. 50 ≈ 7.2 s for a full 360°.")]
        [SerializeField] private float turnSpeed = 50f;

        private Vector2 moveInput;
        private Vector3 velocity;
        private Vector3 bounceVelocity;

        [SerializeField] private float bounceDrag = 6f;

        /// <summary>Cumulative angular displacement (radians) for visual planet rotation.</summary>
        private static float s_cumulativeAngle;
        public static float CumulativeAngle => s_cumulativeAngle;

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

        public void ApplyBounce(Vector3 surfaceDir, float speed)
        {
            bounceVelocity = surfaceDir * speed;
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
            // Apply bounce impulse (decays each frame)
            if (bounceVelocity.sqrMagnitude > 0.001f)
            {
                Vector3 bUp = (transform.position - planet.position).normalized;
                Vector3 bDir = Vector3.ProjectOnPlane(bounceVelocity, bUp).normalized;
                float bAngSpeed = bounceVelocity.magnitude / orbitRadius;
                float bAngle = bAngSpeed * Time.deltaTime;
                Vector3 bFromCenter = transform.position - planet.position;
                Vector3 bAxis = Vector3.Cross(bFromCenter.normalized, bDir).normalized;
                if (bAxis.sqrMagnitude > 0.001f)
                {
                    Quaternion bRot = Quaternion.AngleAxis(bAngle * Mathf.Rad2Deg, bAxis);
                    transform.position = planet.position + bRot * bFromCenter;
                }
                bounceVelocity = Vector3.MoveTowards(bounceVelocity, Vector3.zero, bounceDrag * Time.deltaTime);
                SnapToSurface();
            }

            // Clamp: no backward movement (only forward + strafe)
            Vector2 clampedInput = new Vector2(moveInput.x, Mathf.Max(0f, moveInput.y));
            if (clampedInput.sqrMagnitude < 0.01f) return;

            // Get local "right" and "forward" directions relative to sphere surface
            Vector3 up = (transform.position - planet.position).normalized;
            Vector3 right = Vector3.Cross(up, transform.forward).normalized;
            Vector3 forward = Vector3.Cross(right, up).normalized;

            // Recalculate right to ensure orthogonality
            right = Vector3.Cross(up, forward).normalized;

            // Build movement direction from input
            Vector3 moveDir = (forward * clampedInput.y + right * clampedInput.x).normalized;

            // Angular velocity on sphere surface
            float angularSpeed = moveSpeed / orbitRadius; // radians per second
            float angle = angularSpeed * Time.deltaTime;

            // Rotate position around planet center
            Vector3 fromCenter = transform.position - planet.position;
            Vector3 rotationAxis = Vector3.Cross(fromCenter.normalized, moveDir).normalized;

            if (rotationAxis.sqrMagnitude < 0.001f) return;

            float angleDeg = angle * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.AngleAxis(angleDeg, rotationAxis);
            Vector3 newPos = planet.position + rotation * fromCenter;

            transform.position = newPos;

            // Accumulate angle for shader-based planet visual rotation (safe, no transform mutation)
            s_cumulativeAngle += angle;

            // Align up vector to surface normal, turn heading at capped speed
            Vector3 newUp = (newPos - planet.position).normalized;

            // Project current forward onto tangent plane (keeps heading grounded)
            Vector3 currentForward = Vector3.ProjectOnPlane(transform.forward, newUp).normalized;
            if (currentForward.sqrMagnitude < 0.001f)
                currentForward = Vector3.ProjectOnPlane(Vector3.forward, newUp).normalized;

            // Project desired moveDir onto same tangent plane
            Vector3 desiredForward = Vector3.ProjectOnPlane(moveDir, newUp).normalized;
            if (desiredForward.sqrMagnitude < 0.001f)
                desiredForward = currentForward;

            // Rotate heading at capped angular speed — up stays locked to surface normal
            Vector3 newForward = Vector3.RotateTowards(
                currentForward, desiredForward,
                turnSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f);

            transform.rotation = Quaternion.LookRotation(newForward, newUp);

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
