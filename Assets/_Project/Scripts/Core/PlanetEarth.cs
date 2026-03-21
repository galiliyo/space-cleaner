using UnityEngine;
using UnityEngine.Rendering;
using SpaceCleaner.Player;

namespace SpaceCleaner.Core
{
    /// <summary>
    /// Displays Earth as the gameplay planet. Supports three modes (in priority order):
    /// 1. External mesh + materials (e.g. "Planet Earth Free" asset from the Asset Store)
    /// 2. Custom StylizedEarth shader material already assigned in the Inspector
    /// 3. Fully procedural UV sphere with generated texture (fallback)
    /// </summary>
    public class PlanetEarth : MonoBehaviour
    {
        [Header("External Model (e.g. Planet Earth Free)")]
        [Tooltip("Drag the Earth .fbx mesh here. When set, skips procedural sphere generation.")]
        [SerializeField] private Mesh externalMesh;
        [Tooltip("Materials for the external model. Leave empty to keep existing materials.")]
        [SerializeField] private Material[] externalMaterials;

        [Header("Procedural Fallback")]
        [SerializeField] private int textureSize = 512;
        [SerializeField] private int seed = 42;

        [Header("Sphere Resolution (procedural only)")]
        [SerializeField] private int longitudeSegments = 64;
        [SerializeField] private int latitudeSegments = 32;

        [Header("Colors (procedural only)")]
        [SerializeField] private Color deepOcean = new Color(0.05f, 0.12f, 0.35f);
        [SerializeField] private Color shallowOcean = new Color(0.1f, 0.25f, 0.55f);
        [SerializeField] private Color lowland = new Color(0.15f, 0.45f, 0.12f);
        [SerializeField] private Color highland = new Color(0.4f, 0.35f, 0.2f);
        [SerializeField] private Color mountain = new Color(0.55f, 0.5f, 0.4f);
        [SerializeField] private Color ice = new Color(0.85f, 0.9f, 0.95f);

        [Header("Rotation")]
        [Tooltip("Continuous self-rotation speed in degrees/sec (visual only, for external models).")]
        [SerializeField] private float selfRotationSpeed = 0f;

        private Material planetMat;
        private Texture2D planetTex;
        private Mesh sphereMesh;
        private Material activeMaterial; // whichever material is actually on the renderer
        private bool useShaderRotation; // true when StylizedEarth shader is active
        private static readonly int RotYId = Shader.PropertyToID("_RotY");

        private void Awake()
        {
            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();

            // --- Mode 1: External mesh (e.g. Planet Earth Free) ---
            if (externalMesh != null)
            {
                if (mf != null) mf.sharedMesh = externalMesh;

                if (externalMaterials != null && externalMaterials.Length > 0 && mr != null)
                    mr.sharedMaterials = externalMaterials;

                // Update collider to match external mesh bounds
                var sc = GetComponent<SphereCollider>();
                if (sc != null)
                {
                    var ext = externalMesh.bounds.extents;
                    sc.center = externalMesh.bounds.center;
                    sc.radius = Mathf.Max(ext.x, Mathf.Max(ext.y, ext.z));
                }

                useShaderRotation = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[PlanetEarth] Using external mesh: {externalMesh.name}");
#endif
            }
            else
            {
                // --- Mode 2 & 3: Procedural sphere ---
#if UNITY_ANDROID || UNITY_IOS
                longitudeSegments = Mathf.Min(longitudeSegments, 48);
                latitudeSegments = Mathf.Min(latitudeSegments, 24);
                textureSize = Mathf.Min(textureSize, 256);
#endif
                sphereMesh = GenerateUVSphere(longitudeSegments, latitudeSegments);
                if (mf != null) mf.sharedMesh = sphereMesh;

                var sc = GetComponent<SphereCollider>();
                if (sc != null)
                {
                    sc.center = Vector3.zero;
                    sc.radius = 0.5f;
                }

                // If a material is already assigned (e.g. StylizedEarth shader), keep it
                bool hasCustomMaterial = mr != null && mr.sharedMaterial != null
                    && mr.sharedMaterial.name != "Default-Material"
                    && !mr.sharedMaterial.name.StartsWith("Default");

                if (!hasCustomMaterial)
                {
                    planetTex = GenerateEarthTexture();
                    var shader = Shader.Find("Universal Render Pipeline/Lit");
                    planetMat = new Material(shader);
                    planetMat.SetTexture("_BaseMap", planetTex);
                    planetMat.SetColor("_BaseColor", Color.white);
                    planetMat.SetFloat("_Smoothness", 0.3f);
                    planetMat.SetFloat("_Metallic", 0f);
                    if (mr != null) mr.sharedMaterial = planetMat;
                }

                useShaderRotation = true;
            }

            // Disable shadow casting on planet (ship shadows look bad on curved surface)
            if (mr != null)
            {
                mr.receiveShadows = false;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                activeMaterial = mr.material;
            }
        }

        private void Update()
        {
            if (useShaderRotation)
            {
                // Feed ship movement angle to shader for visual planet rotation
                if (activeMaterial != null && activeMaterial.HasFloat(RotYId))
                    activeMaterial.SetFloat(RotYId, SphericalMovement.CumulativeAngle);
            }
            else if (selfRotationSpeed > 0f)
            {
                // Slow visual spin for external models (rotate the mesh, not the collider)
                transform.Rotate(Vector3.up, selfRotationSpeed * Time.deltaTime, Space.Self);
            }
        }

        /// <summary>
        /// Generates a UV sphere mesh with proper normals and UVs.
        /// </summary>
        private static Mesh GenerateUVSphere(int lonSegments, int latSegments)
        {
            var mesh = new Mesh();
            mesh.name = "HighResSphere";

            int vertCount = (lonSegments + 1) * (latSegments + 1);
            var vertices = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            int idx = 0;
            for (int lat = 0; lat <= latSegments; lat++)
            {
                float theta = Mathf.PI * lat / latSegments;
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                for (int lon = 0; lon <= lonSegments; lon++)
                {
                    float phi = 2f * Mathf.PI * lon / lonSegments;
                    float sinPhi = Mathf.Sin(phi);
                    float cosPhi = Mathf.Cos(phi);

                    float x = cosPhi * sinTheta;
                    float y = cosTheta;
                    float z = sinPhi * sinTheta;

                    vertices[idx] = new Vector3(x, y, z) * 0.5f; // radius 0.5 to match Unity sphere
                    normals[idx] = new Vector3(x, y, z);
                    uvs[idx] = new Vector2((float)lon / lonSegments, 1f - (float)lat / latSegments);
                    idx++;
                }
            }

            int triCount = latSegments * lonSegments * 6;
            var triangles = new int[triCount];
            int ti = 0;
            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < lonSegments; lon++)
                {
                    int current = lat * (lonSegments + 1) + lon;
                    int next = current + lonSegments + 1;

                    triangles[ti++] = current;
                    triangles[ti++] = next + 1;
                    triangles[ti++] = current + 1;

                    triangles[ti++] = current;
                    triangles[ti++] = next;
                    triangles[ti++] = next + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            return mesh;
        }

        private Texture2D GenerateEarthTexture()
        {
            int w = textureSize;
            int h = textureSize;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, true);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;

            var pixels = new Color[w * h];

            Random.State oldState = Random.state;
            Random.InitState(seed);
            float offsetX = Random.Range(0f, 1000f);
            float offsetY = Random.Range(0f, 1000f);
            Random.state = oldState;

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / h;
                float lat = (v - 0.5f) * Mathf.PI;

                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;

                    float nx = u * 4f + offsetX;
                    float ny = v * 2f + offsetY;
                    // Domain warping — offsets coordinates by noise, creating organic continent shapes
                    float warpX = Mathf.PerlinNoise(nx * 0.7f + 100f, ny * 0.7f + 100f) * 1.5f;
                    float warpY = Mathf.PerlinNoise(nx * 0.7f + 200f, ny * 0.7f + 200f) * 1.5f;
                    nx += warpX;
                    ny += warpY;
                    float height = 0f;
                    height += Mathf.PerlinNoise(nx * 1f, ny * 1f) * 0.5f;
                    height += Mathf.PerlinNoise(nx * 2.3f, ny * 2.3f) * 0.25f;
                    height += Mathf.PerlinNoise(nx * 5.1f, ny * 5.1f) * 0.125f;
                    height += Mathf.PerlinNoise(nx * 11f, ny * 11f) * 0.0625f;

                    float seaLevel = 0.42f;
                    Color c;

                    if (height < seaLevel)
                    {
                        float oceanDepth = Mathf.InverseLerp(0f, seaLevel, height);
                        c = Color.Lerp(deepOcean, shallowOcean, oceanDepth);
                    }
                    else
                    {
                        float landHeight = Mathf.InverseLerp(seaLevel, 1f, height);
                        if (landHeight < 0.3f)
                            c = Color.Lerp(lowland, lowland * 1.1f, landHeight / 0.3f);
                        else if (landHeight < 0.65f)
                            c = Color.Lerp(lowland, highland, (landHeight - 0.3f) / 0.35f);
                        else
                            c = Color.Lerp(highland, mountain, (landHeight - 0.65f) / 0.35f);
                    }

                    // Ice caps at poles
                    float poleFactor = Mathf.Abs(lat) / (Mathf.PI * 0.5f);
                    float iceThreshold = 0.75f;
                    if (poleFactor > iceThreshold)
                    {
                        float iceBlend = (poleFactor - iceThreshold) / (1f - iceThreshold);
                        iceBlend *= iceBlend;
                        c = Color.Lerp(c, ice, iceBlend);
                    }

                    // Cloud wisps
                    float cloudNoise = Mathf.PerlinNoise(nx * 3f + 500f, ny * 3f + 500f);
                    cloudNoise += Mathf.PerlinNoise(nx * 7f + 500f, ny * 7f + 500f) * 0.3f;
                    float cloudAlpha = Mathf.Clamp01((cloudNoise - 0.55f) * 3f) * 0.35f;
                    c = Color.Lerp(c, Color.white, cloudAlpha);

                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, true);
            return tex;
        }

        private void OnDestroy()
        {
            if (planetMat != null) Destroy(planetMat);
            if (planetTex != null) Destroy(planetTex);
            if (sphereMesh != null) Destroy(sphereMesh);
        }
    }
}
