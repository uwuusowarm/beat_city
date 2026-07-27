// Fresnel rim used as an override material by FresnelHighlightFeature.
// Drawn as a second additive pass over characters after the opaque queue,
// so it never replaces their real shading. Deliberately mirrors the rim
// math in Custom/Toon Lit so both looks match.
Shader "Hidden/Custom/Fresnel Highlight"
{
    Properties
    {
        [HDR] _FresnelColor ("Fresnel Color", Color) = (0,1,1,1)
        _FresnelPower ("Fresnel Power", Range(0.5,8)) = 2
        _FresnelThreshold ("Rim Size", Range(0,1)) = 0.5
        _FresnelSmoothness ("Rim Softness", Range(0.001,1)) = 0.25
        _FresnelIntensity ("Intensity", Range(0,4)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "FresnelHighlight"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One   // additive glow on top of the existing shading
            ZWrite Off
            ZTest LEqual    // equal depth passes; occluded fragments drop out
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FresnelColor;
                half  _FresnelPower;
                half  _FresnelThreshold;
                half  _FresnelSmoothness;
                half  _FresnelIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(input.normalOS);

                output.positionHCS = posInputs.positionCS;
                output.normalWS    = nrmInputs.normalWS;
                output.viewDirWS   = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half3 normalWS = normalize(input.normalWS);
                half3 viewDir  = normalize(input.viewDirWS);

                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDir)), _FresnelPower);
                half rimMask = smoothstep(_FresnelThreshold - _FresnelSmoothness,
                                          _FresnelThreshold + _FresnelSmoothness, fresnel);

                return half4(_FresnelColor.rgb * rimMask * _FresnelIntensity, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
