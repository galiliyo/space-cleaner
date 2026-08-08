using UnityEngine;

namespace SpaceCleaner.Core
{
    /// <summary>
    /// Atmospheric glow effect for planets using a secondary mesh with fresnel shader.
    /// </summary>
    [ExecuteInEditMode]
    public class PlanetAtmosphere : MonoBehaviour
    {
        [Header("Atmosphere")]
        [SerializeField] private Color atmosphereColor = new Color(0.3f, 0.7f, 1f, 0.4f);
        [SerializeField] private float atmosphereScale = 1.05f;
        [SerializeField] private float fresnelPower = 2f;
        [SerializeField] private float intensity = 1f;
        
        [Header("Optional")]
        [SerializeField] private bool useCustomMesh = false;
        [SerializeField] private Mesh customMesh;
        
        private MeshRenderer atmosphereRenderer;
        private Material atmosphereMaterial;
        private static Shader fresnelShader;
        
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int PowerId = Shader.PropertyToID("_Power");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int RimOffsetId = Shader.PropertyToID("_RimOffset");
        
        private void Awake()
        {
            CreateAtmosphere();
        }
        
        private void CreateAtmosphere()
        {
            // Find or create atmosphere child
            var atmoTransform = transform.Find("Atmosphere");
            if (atmoTransform == null)
            {
                var atmoGO = new GameObject("Atmosphere");
                atmoGO.transform.SetParent(transform, false);
                atmoTransform = atmoGO.transform;
            }
            
            // Get or add mesh renderer
            atmosphereRenderer = atmoTransform.GetComponent<MeshRenderer>();
            if (atmosphereRenderer == null)
                atmosphereRenderer = atmoTransform.gameObject.AddComponent<MeshRenderer>();
            
            // Get or add mesh filter
            var meshFilter = atmoTransform.GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = atmoTransform.gameObject.AddComponent<MeshFilter>();
            
            // Use sphere mesh scaled up
            if (!useCustomMesh)
            {
                meshFilter.sharedMesh = GetSphereMesh();
                atmoTransform.localScale = Vector3.one * atmosphereScale;
            }
            else if (customMesh != null)
            {
                meshFilter.sharedMesh = customMesh;
            }
            
            // Create fresnel material
            atmosphereMaterial = CreateFresnelMaterial();
            atmosphereRenderer.sharedMaterial = atmosphereMaterial;
            
            // No shadows, render before planet
            atmosphereRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            atmosphereRenderer.receiveShadows = false;
        }
        
        private Mesh GetSphereMesh()
        {
            // Try to find a built-in sphere mesh
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Destroy(go);
            return mesh;
        }
        
        private Material CreateFresnelMaterial()
        {
            if (fresnelShader == null)
            {
                // Prefer custom fresnel shader, fall back to URP Unlit
                fresnelShader = Shader.Find("SpaceCleaner/FresnelAtmosphere") 
                    ?? Shader.Find("Universal Render Pipeline/Unlit");
            }
            
            var mat = new Material(fresnelShader);
            
            if (fresnelShader.name.Contains("FresnelAtmosphere"))
            {
                // Custom shader: use its native properties
                mat.SetColor(ColorId, atmosphereColor);
                mat.SetFloat(PowerId, fresnelPower);
                mat.SetFloat(IntensityId, intensity);
                mat.SetFloat(RimOffsetId, 0.15f); // slight inward rim for visible edge glow
            }
            else
            {
                // URP Unlit fallback: configure transparency
                mat.SetColor("_BaseColor", atmosphereColor);
                mat.SetFloat("_Surface", 1f); // Transparent
                mat.SetFloat("_Blend", 0f);   // Alpha blend
                mat.SetFloat("_ZWrite", 0f);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            
            return mat;
        }
        
        private void OnDestroy()
        {
            if (atmosphereMaterial != null)
                Destroy(atmosphereMaterial);
        }
    }
}
