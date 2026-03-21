using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceCleaner.Core
{
    /// <summary>
    /// Creates a procedural moon that orbits the planet. Generates a cratered
    /// gray texture and a smooth sphere mesh at runtime.
    /// </summary>
    public class Moon : MonoBehaviour
    {
        [Header("Orbit")]
        [SerializeField] private Transform planet;
        [SerializeField] private float orbitRadius = 80f;
        [SerializeField] private float orbitSpeed = 3f; // degrees per second

        [Header("Appearance")]
        [SerializeField] private float moonScale = 8f;
        [SerializeField] private int textureSize = 256;
        [SerializeField] private int sphereSegments = 32;
        [SerializeField] private Color moonLight = new Color(0.75f, 0.73f, 0.7f);
        [SerializeField] private Color moonDark = new Color(0.35f, 0.33f, 0.3f);

        private Material moonMat;
        private Texture2D moonTex;
        private Mesh moonMesh;
        private float orbitAngle;

        public void SetPlanet(Transform planetTransform)
        {
            planet = planetTransform;

            // Position at a fixed offset (called after Awake, so planet is now valid)
            if (planet != null)
            {
                float startAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(startAngle) * orbitRadius,
                    orbitRadius * 0.15f,
                    Mathf.Sin(startAngle) * orbitRadius
                );
                transform.position = planet.position + offset;
            }
        }

        private void Awake()
        {
            // Generate mesh
            moonMesh = GenerateUVSphere(sphereSegments, sphereSegments / 2);
            var mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = moonMesh;

            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // Generate texture
            moonTex = GenerateMoonTexture();

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            moonMat = new Material(shader);
            moonMat.SetTexture("_BaseMap", moonTex);
            moonMat.SetColor("_BaseColor", Color.white);
            moonMat.SetFloat("_Smoothness", 0.1f);
            moonMat.SetFloat("_Metallic", 0f);
            mr.sharedMaterial = moonMat;

            transform.localScale = Vector3.one * moonScale;
        }

        private void Update()
        {
            // Slow self-rotation only (stationary orbit)
            transform.Rotate(Vector3.up, 5f * Time.deltaTime, Space.Self);
        }

        private Texture2D GenerateMoonTexture()
        {
            int size = textureSize;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;

            var pixels = new Color[size * size];
            Random.State oldState = Random.state;
            Random.InitState(77);

            float ox = Random.Range(0f, 1000f);
            float oy = Random.Range(0f, 1000f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;

                    // Base terrain noise
                    float n = Mathf.PerlinNoise(u * 4f + ox, v * 4f + oy) * 0.5f;
                    n += Mathf.PerlinNoise(u * 8f + ox, v * 8f + oy) * 0.25f;
                    n += Mathf.PerlinNoise(u * 16f + ox, v * 16f + oy) * 0.125f;

                    Color c = Color.Lerp(moonDark, moonLight, n);

                    // Craters: darker circular depressions
                    float craterNoise = Mathf.PerlinNoise(u * 20f + ox + 300f, v * 20f + oy + 300f);
                    if (craterNoise > 0.7f)
                    {
                        float depth = (craterNoise - 0.7f) / 0.3f;
                        c = Color.Lerp(c, moonDark * 0.7f, depth * 0.5f);
                    }

                    pixels[y * size + x] = c;
                }
            }

            Random.state = oldState;
            tex.SetPixels(pixels);
            tex.Apply(true, true);
            return tex;
        }

        private static Mesh GenerateUVSphere(int lonSegs, int latSegs)
        {
            var mesh = new Mesh();
            mesh.name = "MoonSphere";

            int vertCount = (lonSegs + 1) * (latSegs + 1);
            var verts = new Vector3[vertCount];
            var norms = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            int idx = 0;
            for (int lat = 0; lat <= latSegs; lat++)
            {
                float theta = Mathf.PI * lat / latSegs;
                float sinT = Mathf.Sin(theta);
                float cosT = Mathf.Cos(theta);

                for (int lon = 0; lon <= lonSegs; lon++)
                {
                    float phi = 2f * Mathf.PI * lon / lonSegs;
                    float x = Mathf.Cos(phi) * sinT;
                    float y = cosT;
                    float z = Mathf.Sin(phi) * sinT;

                    verts[idx] = new Vector3(x, y, z) * 0.5f;
                    norms[idx] = new Vector3(x, y, z);
                    uvs[idx] = new Vector2((float)lon / lonSegs, 1f - (float)lat / latSegs);
                    idx++;
                }
            }

            var tris = new int[latSegs * lonSegs * 6];
            int ti = 0;
            for (int lat = 0; lat < latSegs; lat++)
            {
                for (int lon = 0; lon < lonSegs; lon++)
                {
                    int cur = lat * (lonSegs + 1) + lon;
                    int next = cur + lonSegs + 1;

                    tris[ti++] = cur;
                    tris[ti++] = next + 1;
                    tris[ti++] = cur + 1;

                    tris[ti++] = cur;
                    tris[ti++] = next;
                    tris[ti++] = next + 1;
                }
            }

            mesh.vertices = verts;
            mesh.normals = norms;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (moonMat != null) Destroy(moonMat);
            if (moonTex != null) Destroy(moonTex);
            if (moonMesh != null) Destroy(moonMesh);
        }
    }
}
