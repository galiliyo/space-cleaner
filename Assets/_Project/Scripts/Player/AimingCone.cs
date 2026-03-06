using UnityEngine;

namespace SpaceCleaner.Player
{
    /// <summary>
    /// Brawl Stars-style aiming cone visual. Generates a semi-transparent triangle mesh
    /// at runtime that shows the fire direction on the planet surface tangent plane.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class AimingCone : MonoBehaviour
    {
        [Header("Cone Shape")]
        [SerializeField] private float coneLength = 4f;
        [SerializeField] private float coneAngle = 30f; // total spread in degrees
        [SerializeField] private int segments = 8;       // triangles forming the arc

        [Header("Appearance")]
        [SerializeField] private Color coneColor = new Color(1f, 1f, 0.3f, 0.35f);

        [Header("References")]
        [SerializeField] private Transform planet;

        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Material material;

        /// <summary>
        /// Assign the planet reference so the cone sits on the tangent plane.
        /// </summary>
        public void SetPlanet(Transform planetTransform)
        {
            planet = planetTransform;
        }

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            BuildMesh();
            CreateMaterial();

            // Start hidden
            meshRenderer.enabled = false;
        }

        /// <summary>
        /// Called by ShootingSystem every frame with current aim state.
        /// </summary>
        /// <param name="worldAimDirection">Normalized world-space aim direction (on tangent plane).</param>
        /// <param name="isAiming">Whether the player is actively aiming.</param>
        public void UpdateCone(Vector3 worldAimDirection, bool isAiming)
        {
            meshRenderer.enabled = isAiming;

            if (!isAiming) return;
            if (planet == null) return;

            // Position at the parent ship
            Transform ship = transform.parent;
            if (ship == null) return;

            Vector3 up = (ship.position - planet.position).normalized;

            // Place cone slightly above the ship position to avoid z-fighting with planet
            transform.position = ship.position + up * 0.15f;

            // Orient: forward = aim direction, up = surface normal
            if (worldAimDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(worldAimDirection, up);
            }
        }

        private void BuildMesh()
        {
            mesh = new Mesh();
            mesh.name = "AimingCone";

            float halfAngle = coneAngle * 0.5f * Mathf.Deg2Rad;
            int vertexCount = segments + 2; // origin + arc vertices
            int triCount = segments;

            var vertices = new Vector3[vertexCount];
            var triangles = new int[triCount * 3];
            var colors = new Color[vertexCount];

            // Vertex 0: origin (at the ship)
            vertices[0] = Vector3.zero;
            colors[0] = coneColor;

            // Arc vertices along the cone edge at coneLength distance
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = Mathf.Lerp(-halfAngle, halfAngle, t);

                float x = Mathf.Sin(angle) * coneLength;
                float z = Mathf.Cos(angle) * coneLength;
                vertices[i + 1] = new Vector3(x, 0f, z);

                // Fade alpha toward the edges
                float edgeFade = 1f - Mathf.Abs(t - 0.5f) * 2f;
                Color c = coneColor;
                c.a *= edgeFade * 0.6f;
                colors[i + 1] = c;
            }

            // Triangles: fan from origin to arc
            for (int i = 0; i < segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.mesh = mesh;
        }

        private void CreateMaterial()
        {
            // Use URP Unlit shader with transparency
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                // Fallback
                shader = Shader.Find("Unlit/Color");
            }

            material = new Material(shader);
            material.color = coneColor;

            // Enable transparency
            material.SetFloat("_Surface", 1); // 0=Opaque, 1=Transparent
            material.SetFloat("_Blend", 0);   // 0=Alpha, 1=Premultiply, 2=Additive, 3=Multiply
            material.SetFloat("_AlphaClip", 0);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // Enable vertex colors
            material.EnableKeyword("_VERTEX_COLORS");

            meshRenderer.material = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (mesh != null) Destroy(mesh);
        }
    }
}
