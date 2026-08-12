// Character shader for dark scenes.
//
// Two things make characters read against a dark environment:
//
//  1) Hemisphere Fill - a fake sky/ground light baked into the material. It
//     lifts the character off black without adding a single light to the scene,
//     so the environment stays exactly as dark as it is.
//  2) Rendering layer aware lighting - a key light whose Rendering Layer Mask
//     is set to the character layers only affects characters. Requires
//     "Use Rendering Layers" on the URP asset (already on for PC_RPAsset and
//     Mobile_RPAsset) and the character renderers to carry that layer.
//
// The rim light on top separates the silhouette from the background.

Shader "Custom/Character Lit"
{
    Properties
    {
        [Header(Surface)]
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor]   _BaseColor ("Base Color", Color) = (1,1,1,1)
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0,2)) = 1
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clipping", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2

        [Header(Material Maps (import with sRGB off))]
        [Toggle(_OCCLUSIONMAP)] _UseOcclusionMap ("Use Occlusion Map", Float) = 0
        _OcclusionMap ("Occlusion / AO", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0,1)) = 1

        [Toggle(_ROUGHNESSMAP)] _UseRoughnessMap ("Use Roughness Map", Float) = 0
        _RoughnessMap ("Roughness", 2D) = "black" {}

        [Toggle(_METALLICMAP)] _UseMetallicMap ("Use Metallic Map", Float) = 0
        _MetallicMap ("Metallic", 2D) = "white" {}
        _Metallic ("Metallic Scale", Range(0,1)) = 0

        [Header(Fill Light (no scene lights needed))]
        [HDR] _FillSky ("Fill From Above", Color) = (0.34,0.38,0.5,1)
        [HDR] _FillGround ("Fill From Below", Color) = (0.14,0.12,0.11,1)
        _FillStrength ("Fill Strength", Range(0,4)) = 1
        _AmbientStrength ("Scene Ambient", Range(0,4)) = 1

        [Header(Light Response)]
        _LightWrap ("Light Wrap (soft terminator)", Range(0,1)) = 0.25
        [Toggle] _ReceiveShadows ("Receive Shadows", Float) = 1
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 1
        [Toggle] _AdditionalLights ("Additional Lights", Float) = 1

        [Header(Specular)]
        [Toggle(_SPECULAR_ON)] _SpecularEnabled ("Enable Specular", Float) = 1
        [HDR] _SpecularColor ("Specular Color", Color) = (0.4,0.4,0.4,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.5

        [Header(Rim Light)]
        [Toggle(_RIM_ON)] _RimEnabled ("Enable Rim", Float) = 1
        [HDR] _RimColor ("Rim Color", Color) = (0.6,0.75,1,1)
        _RimThreshold ("Rim Size", Range(0,1)) = 0.6
        _RimSmoothness ("Rim Softness", Range(0.001,1)) = 0.25
        _RimLitOnly ("Rim Follows Key Light", Range(0,1)) = 0

        [Header(Emission)]
        [Toggle(_EMISSION_ON)] _EmissionEnabled ("Enable Emission", Float) = 0
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionMap ("Emission Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
            "UniversalMaterialType" = "Lit"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            half   _BumpScale;
            half   _Cutoff;
            half   _Cull;
            half4  _FillSky;
            half4  _FillGround;
            half   _FillStrength;
            half   _AmbientStrength;
            half   _LightWrap;
            half   _ReceiveShadows;
            half   _ShadowStrength;
            half   _AdditionalLights;
            half4  _SpecularColor;
            half   _Smoothness;
            half   _OcclusionStrength;
            half   _Metallic;
            half4  _RimColor;
            half   _RimThreshold;
            half   _RimSmoothness;
            half   _RimLitOnly;
            half4  _EmissionColor;
        CBUFFER_END
        ENDHLSL

        // ---------------------------------------------------------------------
        // Forward lit.
        // ---------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _ROUGHNESSMAP
            #pragma shader_feature_local_fragment _METALLICMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SPECULAR_ON
            #pragma shader_feature_local_fragment _RIM_ON
            #pragma shader_feature_local_fragment _EMISSION_ON

            // URP lighting keywords. _LIGHT_LAYERS is what makes a light with a
            // restricted Rendering Layer Mask skip everything else.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);      SAMPLER(sampler_BumpMap);
            TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_RoughnessMap); SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_MetallicMap);  SAMPLER(sampler_MetallicMap);
            TEXTURE2D(_EmissionMap);  SAMPLER(sampler_EmissionMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                half3  normalWS    : TEXCOORD2;
                half4  tangentWS   : TEXCOORD3;
                half3  viewDirWS   : TEXCOORD4;
                half   fogFactor   : TEXCOORD5;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 6);
            };

            // Diffuse response. _LightWrap pushes the terminator past 90 degrees
            // so the shadow side keeps some shape instead of dropping to black -
            // the single most useful knob for readability in a dark scene.
            half DiffuseResponse(half ndl)
            {
                half wrap = _LightWrap;
                return saturate((ndl + wrap) / (1.0h + wrap));
            }

            // Contribution of one light. Shared by the main light and both
            // additional light loops so they cannot drift apart.
            half3 ShadeLight(Light light, half3 normalWS, half3 viewDir, half3 albedo,
                             half3 specColor, half smoothness)
            {
                half shadow = (_ReceiveShadows > 0.5) ? light.shadowAttenuation : 1.0h;
                shadow = lerp(1.0h, shadow, _ShadowStrength);

                half atten = shadow * light.distanceAttenuation;
                half ndl   = dot(normalWS, light.direction);
                half3 result = albedo * light.color * DiffuseResponse(ndl) * atten;

            #if defined(_SPECULAR_ON)
                half3 halfVec = normalize(light.direction + viewDir);
                half  ndh     = saturate(dot(normalWS, halfVec));
                half  power   = exp2(10.0h * smoothness + 1.0h);
                result += pow(ndh, power) * specColor * light.color
                          * atten * saturate(ndl);
            #endif

                return result;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionHCS = posInputs.positionCS;
                output.positionWS  = posInputs.positionWS;
                output.normalWS    = nrmInputs.normalWS;
                output.tangentWS   = half4(nrmInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.viewDirWS   = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                output.uv          = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

            #if defined(_ALPHATEST_ON)
                clip(albedo.a - _Cutoff);
            #endif

                half3 vertexNormalWS = normalize(input.normalWS);

            #if defined(_NORMALMAP)
                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                half3 tangentWS   = normalize(input.tangentWS.xyz);
                half3 bitangentWS = input.tangentWS.w * cross(vertexNormalWS, tangentWS);
                half3 normalWS = normalize(mul(normalTS,
                    half3x3(tangentWS, bitangentWS, vertexNormalWS)));
            #else
                half3 normalWS = vertexNormalWS;
            #endif

                half3 viewDir = normalize(input.viewDirWS);

                // --- Material maps ---------------------------------------------
                half occlusion = 1.0h;
            #if defined(_OCCLUSIONMAP)
                half ao = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).r;
                occlusion = lerp(1.0h, ao, _OcclusionStrength);
            #endif

                // Roughness is the inverse of smoothness; the default black map
                // keeps materials without a map at the slider value.
                half smoothness = _Smoothness;
            #if defined(_ROUGHNESSMAP)
                half roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, input.uv).r;
                smoothness = saturate((1.0h - roughness) * _Smoothness * 2.0h);
            #endif

                half metallic = _Metallic;
            #if defined(_METALLICMAP)
                metallic *= SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, input.uv).r;
            #endif

                // Stylised metal: highlight takes the surface colour and the
                // diffuse drops back, without going full PBR.
                half3 specColor = lerp(_SpecularColor.rgb, albedo.rgb, metallic);
                half3 diffuse = albedo.rgb * (1.0h - metallic * 0.5h);

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionHCS);
                AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(screenUV);

                // Which rendering layers this renderer belongs to. Lights whose
                // mask does not overlap are skipped below.
                uint meshRenderingLayers = GetMeshRenderingLayer();

                // --- Fill ------------------------------------------------------
                // Fake hemisphere light: sky colour from above, bounce from
                // below. Costs nothing, needs no scene light, and only affects
                // materials using this shader.
                half upFacing = normalWS.y * 0.5h + 0.5h;
                half3 fill = lerp(_FillGround.rgb, _FillSky.rgb, upFacing) * _FillStrength;

                half3 ambient = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS)
                                * _AmbientStrength;

                // Baked occlusion belongs on the indirect light, same as URP Lit.
                half3 color = diffuse * (fill + ambient)
                              * aoFactor.indirectAmbientOcclusion * occlusion;

                // --- Main light ------------------------------------------------
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half keyNdl = dot(normalWS, mainLight.direction);

            #ifdef _LIGHT_LAYERS
                if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
            #endif
                {
                    color += ShadeLight(mainLight, normalWS, viewDir, diffuse, specColor, smoothness);
                }

                // --- Additional lights -----------------------------------------
                if (_AdditionalLights > 0.5)
                {
                    uint count = GetAdditionalLightsCount();

                #if USE_CLUSTER_LIGHT_LOOP
                    InputData inputData = (InputData)0;
                    inputData.positionWS = input.positionWS;
                    inputData.normalizedScreenSpaceUV = screenUV;

                    // Forward+ keeps extra directional lights outside the cluster
                    // loop; without this pass a second directional light would be
                    // silently dropped.
                    [loop] for (uint lightIndex = 0u;
                                lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS);
                                lightIndex++)
                    {
                        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                        Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    #ifdef _LIGHT_LAYERS
                        if (!IsMatchingLightLayer(light.layerMask, meshRenderingLayers)) continue;
                    #endif
                        color += ShadeLight(light, normalWS, viewDir, diffuse, specColor, smoothness);
                    }
                #endif

                    LIGHT_LOOP_BEGIN(count)
                        Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    #ifdef _LIGHT_LAYERS
                        if (!IsMatchingLightLayer(light.layerMask, meshRenderingLayers)) continue;
                    #endif
                        color += ShadeLight(light, normalWS, viewDir, diffuse, specColor, smoothness);
                    LIGHT_LOOP_END
                }

                // --- Rim --------------------------------------------------------
            #if defined(_RIM_ON)
                half rim = 1.0h - saturate(dot(normalWS, viewDir));
                half rimMask = smoothstep(_RimThreshold - _RimSmoothness,
                                          _RimThreshold + _RimSmoothness, rim);
                rimMask *= lerp(1.0h, saturate(keyNdl), _RimLitOnly);
                color += rimMask * _RimColor.rgb;
            #endif

                // --- Emission ---------------------------------------------------
            #if defined(_EMISSION_ON)
                color += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb
                         * _EmissionColor.rgb;
            #endif

                color = MixFog(color, input.fogFactor);
                return half4(color, albedo.a);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------------
        // Shadow caster.
        // ---------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                output.positionHCS = positionCS;
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
            #if defined(_ALPHATEST_ON)
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
            #endif
                return 0;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------------
        // Depth only.
        // ---------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
            #if defined(_ALPHATEST_ON)
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
            #endif
                return 0;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------------
        // Depth normals (SSAO and the screen space outline feature read this).
        // ---------------------------------------------------------------------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
            };

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFrag(Varyings input) : SV_Target
            {
            #if defined(_ALPHATEST_ON)
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
            #endif
                float3 normalWS = normalize(input.normalWS);
                return half4(normalWS * 0.5 + 0.5, 0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
