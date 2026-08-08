Shader "SpaceCleaner/FresnelAtmosphere"
{
    Properties
    {
        [Header(Atmosphere)]
        _Color ("Glow Color", Color) = (0.3, 0.7, 1.0, 1)
        _Power ("Fresnel Power", Range(0.5, 8)) = 2.5
        _Intensity ("Intensity", Range(0, 3)) = 1.0
        _RimOffset ("Rim Offset (expand glow inward)", Range(-1, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Atmosphere"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half  _Power;
                half  _Intensity;
                half  _RimOffset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS  = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal  = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);

                // Fresnel: bright at silhouette (view grazing), dark facing camera
                float fresnel = saturate(1.0 - dot(normal, viewDir) + _RimOffset);
                fresnel = pow(fresnel, _Power);

                half alpha = fresnel * _Intensity * _Color.a;
                return half4(_Color.rgb * _Intensity, alpha);
            }
            ENDHLSL
        }
    }

    // Fallback for built-in render pipeline
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
