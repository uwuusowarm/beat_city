// Soft drop shadow blob. Procedural radial falloff, no texture needed.
//
// Meant for a quad laid flat on the ground under a character. Alpha is driven
// per renderer through a MaterialPropertyBlock so one material can serve every
// character - see BlobShadow.cs.

Shader "Custom/Blob Shadow"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _Strength ("Strength", Range(0,1)) = 0.55
        _Softness ("Edge Softness", Range(0.01,1)) = 0.5
        _EdgePower ("Falloff Shape", Range(0.25,4)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "BlobShadow"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            // Off, not Back: which way the quad's normal ends up pointing after
            // being aligned to the ground normal is not worth relying on.
            Cull Off
            // Nudges the quad towards the camera in depth so it never fights
            // with the floor it is lying on.
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half  _Strength;
                half  _Softness;
                half  _EdgePower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = posInputs.positionCS;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Distance from the centre of the quad, 0 in the middle, 1 at the edge.
                half2 centred = input.uv * 2.0h - 1.0h;
                half dist = length(centred);

                half alpha = 1.0h - smoothstep(1.0h - _Softness, 1.0h, dist);
                alpha = pow(alpha, _EdgePower) * _Strength * _Color.a;

                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
