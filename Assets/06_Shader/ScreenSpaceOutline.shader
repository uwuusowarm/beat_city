Shader "Hidden/Custom/Screen Space Outline"
{
    Properties
    {
        [HDR] _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Thickness (px)", Float) = 1
        _DepthThreshold ("Depth Threshold", Float) = 0.02
        _NormalThreshold ("Normal Threshold", Float) = 0.4
        _GrazingFade ("Grazing Suppression", Float) = 4
        _OutlineTint ("Outline Tint From Scene", Float) = 0

        _PosterizeAmount ("Posterize Amount", Float) = 0
        _PosterizeSteps ("Posterize Steps", Float) = 6

        _HalftoneAmount ("Halftone Amount", Float) = 0
        _HalftoneScale ("Halftone Scale", Float) = 6
        _HalftoneThreshold ("Halftone Threshold", Float) = 0.5
        _HalftoneSoft ("Halftone Softness", Float) = 0.1
        [HDR] _HalftoneColor ("Halftone Color", Color) = (0,0,0,1)

        _FogAmount ("Fog Amount", Float) = 0
        [HDR] _FogColor ("Fog Color", Color) = (0.5,0.6,0.8,1)
        _FogStart ("Fog Start (0..1 of Max)", Float) = 0.1
        _FogEnd ("Fog End (0..1 of Max)", Float) = 0.6
        _FogBands ("Fog Bands", Float) = 4
        _FogMaxDistance ("Fog Max Distance (m)", Float) = 100
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "ScreenSpaceToonPost"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            half4  _OutlineColor;
            float  _OutlineThickness;
            float  _DepthThreshold;
            float  _NormalThreshold;
            float  _GrazingFade;
            float  _OutlineTint;

            float  _PosterizeAmount;
            float  _PosterizeSteps;

            float  _HalftoneAmount;
            float  _HalftoneScale;
            float  _HalftoneThreshold;
            float  _HalftoneSoft;
            half4  _HalftoneColor;

            float  _FogAmount;
            half4  _FogColor;
            float  _FogStart;
            float  _FogEnd;
            float  _FogBands;
            float  _FogMaxDistance;

            float2 Rotate(float2 p, float a)
            {
                float s = sin(a); float c = cos(a);
                return float2(p.x * c - p.y * s, p.x * s + p.y * c);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                float rawD = SampleSceneDepth(uv);

                // ---- Posterize (flatten scene into limited color steps) -------
                if (_PosterizeAmount > 0.0)
                {
                    float steps = max(_PosterizeSteps, 2.0);
                    half3 q = floor(col * steps) / steps;
                    col = lerp(col, q, saturate(_PosterizeAmount));
                }

                // ---- Halftone / screentone in shadows -------------------------
                if (_HalftoneAmount > 0.0)
                {
                    float lum = dot(col, half3(0.299, 0.587, 0.114));
                    float2 sp = uv * _ScreenParams.xy;
                    sp = Rotate(sp, 0.7853982) / max(_HalftoneScale, 1.0); // 45 deg
                    float2 cell = frac(sp) - 0.5;
                    float d = length(cell);
                    // darker pixels -> bigger dots
                    float radius = saturate(_HalftoneThreshold - lum) * 0.9;
                    float dots = smoothstep(radius, radius - _HalftoneSoft, d);
                    float mask = dots * step(lum, _HalftoneThreshold) * saturate(_HalftoneAmount);
                    col = lerp(col, _HalftoneColor.rgb, mask);
                }

                // ---- Stylized banded depth fog --------------------------------
                if (_FogAmount > 0.0)
                {
                    float eye  = LinearEyeDepth(rawD, _ZBufferParams);   // world metres
                    float maxD = max(_FogMaxDistance, 1e-3);
                    float start = _FogStart * maxD;
                    float end   = max(_FogEnd * maxD, start + 1e-3);

                    float fogT = saturate((eye - start) / (end - start));
                    float bands = max(_FogBands, 1.0);
                    fogT = saturate(floor(fogT * bands) / max(bands - 1.0, 1.0)); // hard bands, reaches full
                    fogT *= step(Linear01Depth(rawD, _ZBufferParams), 0.999);    // don't fog the skybox

                    col = lerp(col, _FogColor.rgb, fogT * saturate(_FogAmount) * _FogColor.a);
                }

                // ---- Outline edge detection -----------------------------------
                float2 texel = _CameraDepthTexture_TexelSize.xy * _OutlineThickness;
                float2 uv0 = uv + float2(-1, -1) * texel;
                float2 uv1 = uv + float2( 1,  1) * texel;
                float2 uv2 = uv + float2(-1,  1) * texel;
                float2 uv3 = uv + float2( 1, -1) * texel;

                float dC = LinearEyeDepth(rawD, _ZBufferParams);
                float d0 = LinearEyeDepth(SampleSceneDepth(uv0), _ZBufferParams);
                float d1 = LinearEyeDepth(SampleSceneDepth(uv1), _ZBufferParams);
                float d2 = LinearEyeDepth(SampleSceneDepth(uv2), _ZBufferParams);
                float d3 = LinearEyeDepth(SampleSceneDepth(uv3), _ZBufferParams);
                float depthEdge = sqrt((d1 - d0) * (d1 - d0) + (d3 - d2) * (d3 - d2));

                float3 nWS = SampleSceneNormals(uv);
                float3 nVS = mul((float3x3)UNITY_MATRIX_V, nWS);
                float facing = saturate(abs(nVS.z));
                float grazing = lerp(_GrazingFade, 1.0, facing);
                float depthThreshold = _DepthThreshold * dC * max(grazing, 1e-3);
                float depthMask = step(depthThreshold, depthEdge);

                float3 n0 = SampleSceneNormals(uv0);
                float3 n1 = SampleSceneNormals(uv1);
                float3 n2 = SampleSceneNormals(uv2);
                float3 n3 = SampleSceneNormals(uv3);
                float3 nd1 = n1 - n0;
                float3 nd2 = n3 - n2;
                float normalEdge = sqrt(dot(nd1, nd1) + dot(nd2, nd2));
                float normalMask = step(_NormalThreshold, normalEdge);

                float edge = max(depthMask, normalMask) * saturate(_OutlineColor.a);

                // Colored outline: tint the flat outline toward the scene under it.
                half3 lineCol = lerp(_OutlineColor.rgb, col * _OutlineColor.rgb, saturate(_OutlineTint));
                col = lerp(col, lineCol, edge);

                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
