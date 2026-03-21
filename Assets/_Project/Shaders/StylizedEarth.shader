Shader "SpaceCleaner/StylizedEarth"
{
    Properties
    {
        [Header(Ocean)]
        _OceanShallow ("Shallow Ocean", Color) = (0.18, 0.55, 0.92, 1)
        _OceanDeep ("Deep Ocean", Color) = (0.04, 0.15, 0.50, 1)

        [Header(Land)]
        _GreenColor ("Green (Forests)", Color) = (0.20, 0.62, 0.18, 1)
        _YellowColor ("Yellow (Savanna)", Color) = (0.82, 0.72, 0.28, 1)
        _BrownColor ("Brown (Mountains)", Color) = (0.52, 0.36, 0.20, 1)

        [Header(Polar)]
        _PolarColor ("Polar Caps", Color) = (0.94, 0.96, 1.0, 1)
        _PolarStart ("Polar Start", Range(0.55, 0.95)) = 0.75

        [Header(Clouds)]
        _CloudColor ("Cloud Color", Color) = (1, 1, 1, 1)
        _CloudThreshold ("Cloud Threshold", Range(0.3, 0.8)) = 0.55
        _CloudSoftness ("Cloud Softness", Range(0.01, 0.3)) = 0.1
        _CloudOpacity ("Cloud Opacity", Range(0, 0.6)) = 0.3

        [Header(Atmosphere)]
        _AtmoColor ("Atmosphere Color", Color) = (0.35, 0.65, 1.0, 1)
        _AtmoPower ("Atmosphere Power", Range(1, 8)) = 2.5
        _AtmoIntensity ("Atmosphere Intensity", Range(0, 3)) = 0.8

        [Header(Toon Shading)]
        _ShadowColor ("Shadow Tint", Color) = (0.15, 0.12, 0.25, 1)
        _ShadowThreshold ("Shadow Threshold", Range(-0.5, 0.5)) = 0.0
        _ShadowSoftness ("Shadow Softness", Range(0.01, 0.3)) = 0.06

        [Header(Continent Shape)]
        _Scale ("Continent Scale", Float) = 1.8
        _Threshold ("Land Threshold", Range(0.2, 0.7)) = 0.42
        _CoastBlend ("Coastline Softness", Range(0.01, 0.15)) = 0.04

        [Header(Visual Rotation)]
        _RotY ("Y Rotation (radians)", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ─── Properties ──────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                half4 _OceanShallow;
                half4 _OceanDeep;
                half4 _GreenColor;
                half4 _YellowColor;
                half4 _BrownColor;
                half4 _PolarColor;
                half4 _CloudColor;
                half4 _AtmoColor;
                half4 _ShadowColor;
                half  _PolarStart;
                half  _CloudThreshold;
                half  _CloudSoftness;
                half  _CloudOpacity;
                half  _AtmoPower;
                half  _AtmoIntensity;
                half  _ShadowThreshold;
                half  _ShadowSoftness;
                half  _Scale;
                half  _Threshold;
                half  _CoastBlend;
                half  _RotY;
            CBUFFER_END

            // ─── Structs ────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir     : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
                float3 objectNormal : TEXCOORD4;
            };

            // ─── Noise (hash-based, mobile friendly) ────
            float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float4 mod289(float4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float4 permute(float4 x) { return mod289(((x * 34.0) + 1.0) * x); }
            float4 taylorInvSqrt(float4 r) { return 1.79284291400159 - 0.85373472095314 * r; }

            float snoise(float3 v)
            {
                const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);
                const float4 D = float4(0.0, 0.5, 1.0, 2.0);

                float3 i = floor(v + dot(v, C.yyy));
                float3 x0 = v - i + dot(i, C.xxx);

                float3 g = step(x0.yzx, x0.xyz);
                float3 l = 1.0 - g;
                float3 i1 = min(g, l.zxy);
                float3 i2 = max(g, l.zxy);

                float3 x1 = x0 - i1 + C.xxx;
                float3 x2 = x0 - i2 + C.yyy;
                float3 x3 = x0 - D.yyy;

                i = mod289(i);
                float4 p = permute(permute(permute(
                    i.z + float4(0.0, i1.z, i2.z, 1.0))
                  + i.y + float4(0.0, i1.y, i2.y, 1.0))
                  + i.x + float4(0.0, i1.x, i2.x, 1.0));

                float n_ = 0.142857142857;
                float3 ns = n_ * D.wyz - D.xzx;

                float4 j = p - 49.0 * floor(p * ns.z * ns.z);

                float4 x_ = floor(j * ns.z);
                float4 y_ = floor(j - 7.0 * x_);

                float4 x = x_ * ns.x + ns.yyyy;
                float4 y = y_ * ns.x + ns.yyyy;
                float4 h = 1.0 - abs(x) - abs(y);

                float4 b0 = float4(x.xy, y.xy);
                float4 b1 = float4(x.zw, y.zw);

                float4 s0 = floor(b0) * 2.0 + 1.0;
                float4 s1 = floor(b1) * 2.0 + 1.0;
                float4 sh = -step(h, float4(0.0, 0.0, 0.0, 0.0));

                float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
                float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

                float3 p0 = float3(a0.xy, h.x);
                float3 p1 = float3(a0.zw, h.y);
                float3 p2 = float3(a1.xy, h.z);
                float3 p3 = float3(a1.zw, h.w);

                float4 norm = taylorInvSqrt(float4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
                p0 *= norm.x; p1 *= norm.y; p2 *= norm.z; p3 *= norm.w;

                float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
                m = m * m;
                return 42.0 * dot(m * m, float4(dot(p0, x0), dot(p1, x1), dot(p2, x2), dot(p3, x3)));
            }

            // Rotate a 3D point around the Y axis
            float3 rotateY(float3 p, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float3(c * p.x + s * p.z, p.y, -s * p.x + c * p.z);
            }

            // FBM: 3 octaves for richer continents
            float continentNoise(float3 pos, float scale)
            {
                float n = snoise(pos * scale)        * 0.55
                        + snoise(pos * scale * 2.1)  * 0.30
                        + snoise(pos * scale * 4.3)  * 0.15;
                return n * 0.5 + 0.5; // remap to 0-1
            }

            // Detail noise for land variation
            float detailNoise(float3 pos, float scale)
            {
                return snoise(pos * scale * 3.7) * 0.5 + 0.5;
            }

            // Cloud noise (different frequency)
            float cloudNoise(float3 pos)
            {
                float n = snoise(pos * 2.5 + float3(100, 200, 300)) * 0.6
                        + snoise(pos * 5.0 + float3(100, 200, 300)) * 0.4;
                return n * 0.5 + 0.5;
            }

            // ─── Vertex ─────────────────────────────────
            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS   = posInputs.positionCS;
                output.worldPos     = posInputs.positionWS;
                output.worldNormal  = normInputs.normalWS;
                output.viewDir      = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                output.fogFactor    = ComputeFogFactor(posInputs.positionCS.z);
                output.objectNormal = normalize(input.normalOS);
                return output;
            }

            // ─── Fragment ───────────────────────────────
            half4 frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.worldNormal);
                float3 V = normalize(input.viewDir);

                // Apply visual Y-rotation to noise sampling coordinates
                float3 samplePos = rotateY(normalize(input.objectNormal), _RotY);

                // Latitude: 0 at equator, 1 at poles (based on rotated coords)
                float latitude = abs(samplePos.y);

                // ── Continent mask ──
                float continent = continentNoise(samplePos, _Scale);
                float landMask = smoothstep(_Threshold - _CoastBlend, _Threshold + _CoastBlend, continent);

                // ── Land color by latitude + detail noise ──
                float detail = detailNoise(samplePos, _Scale);

                // Equator: bright green-yellow mix
                half3 equatorial = lerp(_GreenColor.rgb, _YellowColor.rgb, detail * 0.7);
                // Mid latitude: lush green
                half3 temperate = _GreenColor.rgb * lerp(0.9, 1.1, detail);
                // High latitude: brown-green tundra
                half3 highland = lerp(_BrownColor.rgb, _GreenColor.rgb * 0.7, detail * 0.4);

                half3 landColor;
                landColor = lerp(equatorial, temperate, smoothstep(0.12, 0.35, latitude));
                landColor = lerp(landColor, highland, smoothstep(0.50, 0.70, latitude));

                // Mountains at high noise values
                landColor = lerp(landColor, _BrownColor.rgb * 1.1, smoothstep(0.62, 0.82, continent) * 0.5);

                // ── Ocean color with depth variation ──
                float oceanDepth = smoothstep(0.15, _Threshold, continent);
                half3 oceanColor = lerp(_OceanDeep.rgb, _OceanShallow.rgb, oceanDepth);
                // Specular highlight hint on ocean
                oceanColor += 0.03 * pow(saturate(dot(N, V)), 8.0);

                // ── Combine land + ocean ──
                half3 surfaceColor = lerp(oceanColor, landColor, landMask);

                // ── Polar caps (crisp edge) ──
                float polarMask = smoothstep(_PolarStart, _PolarStart + 0.08, latitude);
                // Ice also appears on high mountains near poles
                float mountainIce = smoothstep(0.55, 0.70, latitude) * smoothstep(0.6, 0.8, continent) * 0.4;
                surfaceColor = lerp(surfaceColor, _PolarColor.rgb, saturate(polarMask + mountainIce));

                // ── Clouds ──
                float clouds = cloudNoise(samplePos);
                float cloudMask = smoothstep(_CloudThreshold - _CloudSoftness, _CloudThreshold + _CloudSoftness, clouds);
                surfaceColor = lerp(surfaceColor, _CloudColor.rgb, cloudMask * _CloudOpacity);

                // ── Toon lighting (sharp two-step) ──
                Light mainLight = GetMainLight();
                float NdotL = dot(N, mainLight.direction);
                float toonLight = smoothstep(_ShadowThreshold - _ShadowSoftness, _ShadowThreshold + _ShadowSoftness, NdotL);

                // Lit side: full color. Shadow side: tinted darker
                half3 shadowTint = lerp(_ShadowColor.rgb, half3(1, 1, 1), 0.5);
                half3 litColor = surfaceColor * mainLight.color.rgb;
                half3 shadColor = surfaceColor * shadowTint * 0.45;
                surfaceColor = lerp(shadColor, litColor, toonLight);

                // ── Atmosphere Fresnel rim ──
                float fresnel = pow(1.0 - saturate(dot(V, N)), _AtmoPower);
                surfaceColor += _AtmoColor.rgb * fresnel * _AtmoIntensity;

                // Fog
                surfaceColor = MixFog(surfaceColor, input.fogFactor);

                return half4(surfaceColor, 1.0);
            }

            ENDHLSL
        }

    }

    Fallback "Universal Render Pipeline/Unlit"
}
