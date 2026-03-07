using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceCleaner.Core
{
    /// <summary>
    /// Creates a procedural space skybox at runtime: a star field texture on a 6-sided skybox material.
    /// Attach to any GameObject in the scene (e.g. GameManager). Runs in Awake so it applies before first frame.
    /// </summary>
    public class SpaceSkybox : MonoBehaviour
    {
        [Header("Sky Colors")]
        [SerializeField] private Color _skyTint = new Color(0.02f, 0.02f, 0.06f, 1f);
        [SerializeField] private Color _ambientColor = new Color(0.15f, 0.15f, 0.25f, 1f);

        [Header("Stars")]
        [SerializeField] private int _starCount = 2000;
        [SerializeField] [Range(0.5f, 1f)] private float _starBrightness = 0.9f;
        [SerializeField] private int _textureSize = 1024;

        [Header("Dim Stars")]
        [SerializeField] private int _dimStarCount = 4000;
        [SerializeField] [Range(0.1f, 0.6f)] private float _dimStarBrightness = 0.3f;

        private Material _skyboxMaterial;
        private Texture2D[] _faceTextures;

        private void Awake()
        {
            CreateSkybox();
            ApplyLighting();
        }

        private void CreateSkybox()
        {
            // Use the built-in 6-sided skybox shader (works with URP)
            Shader skyboxShader = Shader.Find("Skybox/6 Sided");
            if (skyboxShader == null)
            {
                Debug.LogWarning("SpaceSkybox: Could not find Skybox/6 Sided shader. Falling back to solid color.");
                SetFallbackSkybox();
                return;
            }

            _skyboxMaterial = new Material(skyboxShader);

            // Generate 6 face textures (one per cube face) with different star patterns
            string[] faceProperties = {
                "_FrontTex", "_BackTex", "_LeftTex", "_RightTex", "_UpTex", "_DownTex"
            };

            _faceTextures = new Texture2D[6];
            for (int i = 0; i < 6; i++)
            {
                _faceTextures[i] = GenerateStarTexture(i * 7919); // different seed per face
                _skyboxMaterial.SetTexture(faceProperties[i], _faceTextures[i]);
            }

            // Tint and exposure
            _skyboxMaterial.SetColor("_Tint", Color.white);
            _skyboxMaterial.SetFloat("_Exposure", 1f);
            _skyboxMaterial.SetFloat("_Rotation", 0f);

            RenderSettings.skybox = _skyboxMaterial;
            DynamicGI.UpdateEnvironment();
        }

        private Texture2D GenerateStarTexture(int seed)
        {
            int size = _textureSize;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            // Fill with dark space color
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = _skyTint;
            }

            // Use a seeded random for reproducibility
            Random.State oldState = Random.state;
            Random.InitState(seed);

            // Bright stars
            for (int i = 0; i < _starCount; i++)
            {
                int x = Random.Range(0, size);
                int y = Random.Range(0, size);
                float brightness = Random.Range(_starBrightness * 0.7f, _starBrightness);

                // Slight color variation: some stars are warm, some cool
                float r = brightness * Random.Range(0.85f, 1f);
                float g = brightness * Random.Range(0.85f, 1f);
                float b = brightness * Random.Range(0.9f, 1f);
                Color starColor = new Color(r, g, b, 1f);

                pixels[y * size + x] = starColor;

                // Some bright stars get a small glow (2x2)
                if (brightness > _starBrightness * 0.9f)
                {
                    Color glowColor = starColor * 0.4f;
                    SetPixelSafe(pixels, size, x + 1, y, glowColor);
                    SetPixelSafe(pixels, size, x - 1, y, glowColor);
                    SetPixelSafe(pixels, size, x, y + 1, glowColor);
                    SetPixelSafe(pixels, size, x, y - 1, glowColor);
                }
            }

            // Dim / distant stars
            for (int i = 0; i < _dimStarCount; i++)
            {
                int x = Random.Range(0, size);
                int y = Random.Range(0, size);
                float brightness = Random.Range(_dimStarBrightness * 0.5f, _dimStarBrightness);
                Color dimColor = new Color(brightness, brightness, brightness * 1.1f, 1f);
                pixels[y * size + x] = dimColor;
            }

            Random.state = oldState;

            tex.SetPixels(pixels);
            tex.Apply(false, true); // makeNoLongerReadable = true to save memory
            return tex;
        }

        private void SetPixelSafe(Color[] pixels, int size, int x, int y, Color color)
        {
            if (x >= 0 && x < size && y >= 0 && y < size)
            {
                int idx = y * size + x;
                // Blend: keep whichever is brighter
                if (pixels[idx].grayscale < color.grayscale)
                    pixels[idx] = color;
            }
        }

        private void SetFallbackSkybox()
        {
            // If the 6-sided shader is missing, just use a solid color skybox
            Shader solidShader = Shader.Find("Skybox/Procedural");
            if (solidShader != null)
            {
                _skyboxMaterial = new Material(solidShader);
                _skyboxMaterial.SetColor("_SkyTint", _skyTint);
                _skyboxMaterial.SetColor("_GroundColor", _skyTint);
                _skyboxMaterial.SetFloat("_Exposure", 0.1f);
                RenderSettings.skybox = _skyboxMaterial;
            }
            else
            {
                // Last resort: clear camera to solid color
                RenderSettings.skybox = null;
                UnityEngine.Camera cam = UnityEngine.Camera.main;
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = _skyTint;
                }
            }
        }

        private void ApplyLighting()
        {
            // Set ambient lighting to dark space tones
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientSkyColor = _ambientColor;
            RenderSettings.ambientLight = _ambientColor;

            // Disable default reflection (no bright reflections in space)
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

            // Fog off
            RenderSettings.fog = false;
        }

        private void OnDestroy()
        {
            // Clean up runtime materials and textures
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
