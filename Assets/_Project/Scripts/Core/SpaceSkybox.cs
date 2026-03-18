using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceCleaner.Core
{
    /// <summary>
    /// Sets up a space skybox. Prefers a pre-made skybox material from the project
    /// (e.g. from SpaceSkies Free asset pack). Falls back to fast procedural generation.
    /// </summary>
    public class SpaceSkybox : MonoBehaviour
    {
        [Header("Custom Skybox (drag a material here to skip procedural generation)")]
        [SerializeField] private Material customSkybox;

        [Header("Fallback Procedural Settings")]
        [SerializeField] private Color _skyTint = new Color(0.02f, 0.02f, 0.06f, 1f);
        [SerializeField] private Color _ambientColor = new Color(0.35f, 0.35f, 0.45f, 1f);
        [SerializeField] private int _starCount = 1200;
        [SerializeField] private int _dimStarCount = 2500;
        [SerializeField] private int _textureSize = 256;

        private static Shader s_SkyboxShader;
        private static Shader s_ProceduralShader;

        private Material _skyboxMaterial;
        private Texture2D[] _faceTextures;

        private void Awake()
        {
            // Try to use a custom skybox material first
            if (TryApplyCustomSkybox())
            {
                ApplyLighting();
                return;
            }

            // Try to find a skybox material in the project at known paths
            if (TryFindProjectSkybox())
            {
                ApplyLighting();
                return;
            }

            // Fall back to fast procedural generation
            CreateProceduralSkybox();
            ApplyLighting();
        }

        private bool TryApplyCustomSkybox()
        {
            if (customSkybox == null) return false;
            RenderSettings.skybox = customSkybox;
            DynamicGI.UpdateEnvironment();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[SpaceSkybox] Using custom skybox material.");
#endif
            return true;
        }

        private bool TryFindProjectSkybox()
        {
            // Check if a skybox material is already assigned in RenderSettings
            var existing = RenderSettings.skybox;
            if (existing != null && existing.shader != null
                && existing.shader.name.Contains("Skybox")
                && existing.shader.name != "Skybox/Procedural")
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[SpaceSkybox] Using existing skybox material: {existing.name}");
#endif
                return true;
            }

#if UNITY_EDITOR
            // In editor, search for skybox materials in known paths
            string[] searchPaths = {
                "Assets/_Project/Materials/Skybox",
                "Assets/SpaceSkies Free",
                "Assets/Skybox",
                "Assets/Materials"
            };

            foreach (var path in searchPaths)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Material", new[] { path });
                foreach (var guid in guids)
                {
                    var matPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat != null && mat.shader != null && mat.shader.name.Contains("Skybox"))
                    {
                        RenderSettings.skybox = mat;
                        DynamicGI.UpdateEnvironment();
                        Debug.Log($"[SpaceSkybox] Found skybox material at: {matPath}");
                        return true;
                    }
                }
            }
#endif
            return false;
        }

        private void CreateProceduralSkybox()
        {
            if (s_SkyboxShader == null) s_SkyboxShader = Shader.Find("Skybox/6 Sided");
            if (s_SkyboxShader == null)
            {
                SetFallbackSkybox();
                return;
            }

            _skyboxMaterial = new Material(s_SkyboxShader);

            string[] faceProperties = {
                "_FrontTex", "_BackTex", "_LeftTex", "_RightTex", "_UpTex", "_DownTex"
            };

            _faceTextures = new Texture2D[6];
            for (int i = 0; i < 6; i++)
            {
                _faceTextures[i] = GenerateStarTexture(i * 7919);
                _skyboxMaterial.SetTexture(faceProperties[i], _faceTextures[i]);
            }

            _skyboxMaterial.SetColor("_Tint", new Color(0.5f, 0.5f, 0.55f, 1f));
            _skyboxMaterial.SetFloat("_Exposure", 0.8f);
            _skyboxMaterial.SetFloat("_Rotation", 0f);

            RenderSettings.skybox = _skyboxMaterial;
            DynamicGI.UpdateEnvironment();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[SpaceSkybox] Using procedural skybox. For better visuals, import 'SpaceSkies Free' from Unity Asset Store.");
#endif
        }

        /// <summary>
        /// Fast star-only texture generation. No heavy Perlin noise loops.
        /// </summary>
        private Texture2D GenerateStarTexture(int seed)
        {
            int size = _textureSize;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color[size * size];

            // Fill with dark sky
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = _skyTint;

            Random.State oldState = Random.state;
            Random.InitState(seed);

            // Bright stars with color variation
            for (int i = 0; i < _starCount; i++)
            {
                int x = Random.Range(0, size);
                int y = Random.Range(0, size);
                float brightness = Random.Range(0.6f, 1f);

                // Star color variety: white, blue-white, yellow-white
                float colorType = Random.Range(0f, 1f);
                Color starColor;
                if (colorType < 0.6f)
                    starColor = new Color(brightness, brightness, brightness * 1.05f); // white-blue
                else if (colorType < 0.85f)
                    starColor = new Color(brightness, brightness * 0.95f, brightness * 0.8f); // warm
                else
                    starColor = new Color(brightness * 0.8f, brightness * 0.9f, brightness); // blue

                pixels[y * size + x] = starColor;

                // Glow for bright stars
                if (brightness > 0.85f)
                {
                    Color glow = starColor * 0.35f;
                    SetPixelSafe(pixels, size, x + 1, y, glow);
                    SetPixelSafe(pixels, size, x - 1, y, glow);
                    SetPixelSafe(pixels, size, x, y + 1, glow);
                    SetPixelSafe(pixels, size, x, y - 1, glow);
                }
            }

            // Dim background stars
            for (int i = 0; i < _dimStarCount; i++)
            {
                int x = Random.Range(0, size);
                int y = Random.Range(0, size);
                float b = Random.Range(0.08f, 0.25f);
                pixels[y * size + x] = new Color(b, b, b * 1.1f);
            }

            Random.state = oldState;

            tex.SetPixels(pixels);
            tex.Apply(false, true); // no mipmaps, make non-readable
            return tex;
        }

        private void SetPixelSafe(Color[] pixels, int size, int x, int y, Color color)
        {
            if (x >= 0 && x < size && y >= 0 && y < size)
            {
                int idx = y * size + x;
                if (pixels[idx].grayscale < color.grayscale)
                    pixels[idx] = color;
            }
        }

        private void SetFallbackSkybox()
        {
            if (s_ProceduralShader == null) s_ProceduralShader = Shader.Find("Skybox/Procedural");
            if (s_ProceduralShader != null)
            {
                _skyboxMaterial = new Material(s_ProceduralShader);
                _skyboxMaterial.SetColor("_SkyTint", _skyTint);
                _skyboxMaterial.SetColor("_GroundColor", _skyTint);
                _skyboxMaterial.SetFloat("_Exposure", 0.1f);
                RenderSettings.skybox = _skyboxMaterial;
            }
            else
            {
                RenderSettings.skybox = null;
                var cam = UnityEngine.Camera.main;
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = _skyTint;
                }
            }
        }

        private void ApplyLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientSkyColor = _ambientColor;
            RenderSettings.ambientLight = _ambientColor;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.fog = false;
        }

        private void OnDestroy()
        {
            if (_skyboxMaterial != null)
                Destroy(_skyboxMaterial);

            if (_faceTextures != null)
            {
                foreach (var tex in _faceTextures)
                {
                    if (tex != null)
                        Destroy(tex);
                }
            }
        }
    }
}
