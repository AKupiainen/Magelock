Shader "BrawlLine/AdvancedToonShader"
{
    Properties
    {
        [Header(Main)]
        _MainTex ("Albedo Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        
        [Header(Shading Mode)]
        [KeywordEnum(Step, Ramp, Posterize)] _ShadingMode("Shading Mode", Float) = 0
        
        [Header(Step Shading)]
        _ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.5
        _ShadowSmoothness ("Shadow Smoothness", Range(0, 0.5)) = 0.1
        _ShadowColor ("Shadow Color", Color) = (0.3, 0.3, 0.3, 1)
        
        [Header(Ramp Shading)]
        _LightRampTex ("Light Ramp Texture", 2D) = "white" {}
        _RampOffset ("Ramp Offset", Range(-1, 1)) = 0
        
        [Header(Posterize Shading)]
        _PosterizeLevels ("Posterize Levels", Range(2, 10)) = 3
        _PosterizePower ("Posterize Power", Range(0.5, 3)) = 1
        
        [Header(Rim Lighting)]
        [Toggle] _UseRimLight ("Use Rim Light", Float) = 1
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(0, 10)) = 2
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 1
        _RimSmoothness ("Rim Smoothness", Range(0, 1)) = 0.5
        
        [Header(Specular)]
        [Toggle] _UseSpecular ("Use Specular", Float) = 1
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularSize ("Specular Size", Range(0, 1)) = 0.1
        _SpecularSmoothness ("Specular Smoothness", Range(0, 1)) = 0.5
        _SpecularSteps ("Specular Steps", Range(1, 10)) = 1
        
        [Header(Outline)]
        [Toggle] _UseOutline ("Use Outline", Float) = 1
        _OutlineWidth ("Outline Width", Range(0, 0.02)) = 0.005
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineAdaptive ("Outline Adaptive", Range(0, 1)) = 0
        
        [Header(Halftone)]
        [Toggle] _UseHalftone ("Use Halftone", Float) = 0
        _HalftoneTex ("Halftone Texture", 2D) = "white" {}
        _HalftoneScale ("Halftone Scale", Float) = 10
        _HalftoneThreshold ("Halftone Threshold", Range(0, 1)) = 0.5
        _HalftoneSmoothness ("Halftone Smoothness", Range(0, 1)) = 0.1
        
        [Header(Advanced)]
        _IndirectLightStrength ("Indirect Light Strength", Range(0, 1)) = 0.3
        _LightWrapAround ("Light Wrap Around", Range(0, 1)) = 0
        [Toggle] _ReceiveShadows ("Receive Shadows", Float) = 1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
        }
        
        LOD 300
        
        // Outline Pass
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            
            Cull Front
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            #pragma shader_feature_local _USEOUTLINE_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _ShadowColor;
                float4 _RimColor;
                float4 _SpecularColor;
                float4 _OutlineColor;
                float4 _LightRampTex_ST;
                float4 _HalftoneTex_ST;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float _RampOffset;
                float _PosterizeLevels;
                float _PosterizePower;
                float _UseRimLight;
                float _RimPower;
                float _RimIntensity;
                float _RimSmoothness;
                float _UseSpecular;
                float _SpecularSize;
                float _SpecularSmoothness;
                float _SpecularSteps;
                float _UseOutline;
                float _OutlineWidth;
                float _OutlineAdaptive;
                float _UseHalftone;
                float _HalftoneScale;
                float _HalftoneThreshold;
                float _HalftoneSmoothness;
                float _IndirectLightStrength;
                float _LightWrapAround;
                float _ReceiveShadows;
            CBUFFER_END
            
            Varyings OutlineVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                // Calculate outline position
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                // Adaptive outline width based on distance
                float distance = length(_WorldSpaceCameraPos - positionWS);
                float adaptiveWidth = lerp(_OutlineWidth, _OutlineWidth * distance * 0.1, _OutlineAdaptive);
                
                positionWS += normalWS * adaptiveWidth;
                
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }
            
            half4 OutlineFragment(Varyings input) : SV_Target
            {
                if (_UseOutline < 0.5) discard;
                
                // Sample main texture for outline texture modulation
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return _OutlineColor * mainTex.a;
            }
            ENDHLSL
        }
        
        // Main Shading Pass
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex ToonVertex
            #pragma fragment ToonFragment
            
            #pragma shader_feature_local _SHADINGMODE_STEP _SHADINGMODE_RAMP _SHADINGMODE_POSTERIZE
            #pragma shader_feature_local _USERIMLIGHT_ON
            #pragma shader_feature_local _USESPECULAR_ON
            #pragma shader_feature_local _USEHALFTONE_ON
            #pragma shader_feature_local _RECEIVESHADOWS_ON
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                float fogFactor : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_LightRampTex);
            SAMPLER(sampler_LightRampTex);
            TEXTURE2D(_HalftoneTex);
            SAMPLER(sampler_HalftoneTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _ShadowColor;
                float4 _RimColor;
                float4 _SpecularColor;
                float4 _OutlineColor;
                float4 _LightRampTex_ST;
                float4 _HalftoneTex_ST;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float _RampOffset;
                float _PosterizeLevels;
                float _PosterizePower;
                float _UseRimLight;
                float _RimPower;
                float _RimIntensity;
                float _RimSmoothness;
                float _UseSpecular;
                float _SpecularSize;
                float _SpecularSmoothness;
                float _SpecularSteps;
                float _UseOutline;
                float _OutlineWidth;
                float _OutlineAdaptive;
                float _UseHalftone;
                float _HalftoneScale;
                float _HalftoneThreshold;
                float _HalftoneSmoothness;
                float _IndirectLightStrength;
                float _LightWrapAround;
                float _ReceiveShadows;
            CBUFFER_END
            
            Varyings ToonVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.screenPos = ComputeScreenPos(vertexInput.positionCS);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            // Posterize function
            float Posterize(float value, float levels)
            {
                return floor(value * levels) / levels;
            }
            
            // Step shading
            float StepShading(float NdotL, float threshold, float smoothness)
            {
                float wrappedNdotL = (NdotL + _LightWrapAround) / (1.0 + _LightWrapAround);
                return smoothstep(threshold - smoothness, threshold + smoothness, wrappedNdotL * 0.5 + 0.5);
            }
            
            // Ramp shading using texture
            float3 RampShading(float NdotL)
            {
                float wrappedNdotL = (NdotL + _LightWrapAround) / (1.0 + _LightWrapAround);
                float rampSample = saturate((wrappedNdotL * 0.5 + 0.5) + _RampOffset);
                return SAMPLE_TEXTURE2D(_LightRampTex, sampler_LightRampTex, float2(rampSample, 0.5)).rgb;
            }
            
            // Posterize shading
            float PosterizeShading(float NdotL)
            {
                float wrappedNdotL = (NdotL + _LightWrapAround) / (1.0 + _LightWrapAround);
                float lighting = pow(saturate(wrappedNdotL * 0.5 + 0.5), _PosterizePower);
                return Posterize(lighting, _PosterizeLevels);
            }
            
            // Rim lighting calculation
            float3 CalculateRimLight(float3 normalWS, float3 viewDirectionWS, float3 lightColor)
            {
                float rimDot = 1.0 - saturate(dot(normalize(normalWS), normalize(viewDirectionWS)));
                float rimIntensity = pow(rimDot, _RimPower);
                rimIntensity = smoothstep(0.5 - _RimSmoothness, 0.5 + _RimSmoothness, rimIntensity);
                return rimIntensity * _RimIntensity * _RimColor.rgb * lightColor;
            }
            
            // Specular highlight calculation
            float3 CalculateSpecular(float3 normalWS, float3 lightDirectionWS, float3 viewDirectionWS, float3 lightColor, float lightAttenuation)
            {
                float3 halfVector = normalize(lightDirectionWS + viewDirectionWS);
                float NdotH = saturate(dot(normalWS, halfVector));
                
                // Calculate specular with toon-style falloff
                float specularPower = (1.0 - _SpecularSize) * 100.0;
                float specular = pow(NdotH, specularPower);
                
                // Apply smoothstep for toon look
                specular = smoothstep(0.5 - _SpecularSmoothness, 0.5 + _SpecularSmoothness, specular);
                
                // Posterize specular if steps > 1
                if (_SpecularSteps > 1.0)
                {
                    specular = Posterize(specular, _SpecularSteps);
                }
                
                return specular * _SpecularColor.rgb * lightColor * lightAttenuation;
            }
            
            // Halftone effect
            float3 ApplyHalftone(float3 color, float2 screenUV, float lightingValue)
            {
                if (_UseHalftone < 0.5) return color;
                
                float2 halftoneUV = screenUV * _HalftoneScale;
                float halftonePattern = SAMPLE_TEXTURE2D(_HalftoneTex, sampler_HalftoneTex, halftoneUV).r;
                
                float halftoneThreshold = _HalftoneThreshold * lightingValue;
                float halftone = smoothstep(halftoneThreshold - _HalftoneSmoothness, 
                                          halftoneThreshold + _HalftoneSmoothness, 
                                          halftonePattern);
                
                return lerp(color * 0.5, color, halftone);
            }
            
            half4 ToonFragment(Varyings input) : SV_Target
            {
                // Sample main texture
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                
                // Get screen UV for halftone
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                // Get main light data
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                // Apply shadow receiving
                float shadowAttenuation = _ReceiveShadows > 0.5 ? mainLight.shadowAttenuation : 1.0;
                
                // Calculate basic lighting
                float3 normalWS = normalize(input.normalWS);
                float3 lightDirectionWS = normalize(mainLight.direction);
                float3 viewDirectionWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                
                float NdotL = dot(normalWS, lightDirectionWS);
                
                // Apply different shading modes
                float3 diffuse = float3(1, 1, 1);
                float lightingValue = 1.0;
                
                #if defined(_SHADINGMODE_STEP)
                    float toonShade = StepShading(NdotL, _ShadowThreshold, _ShadowSmoothness);
                    diffuse = lerp(_ShadowColor.rgb, mainLight.color, toonShade) * shadowAttenuation;
                    lightingValue = toonShade;
                #elif defined(_SHADINGMODE_RAMP)
                    float3 rampColor = RampShading(NdotL);
                    diffuse = rampColor * mainLight.color * shadowAttenuation;
                    lightingValue = (NdotL * 0.5 + 0.5);
                #elif defined(_SHADINGMODE_POSTERIZE)
                    float posterizeShade = PosterizeShading(NdotL);
                    diffuse = lerp(_ShadowColor.rgb, mainLight.color, posterizeShade) * shadowAttenuation;
                    lightingValue = posterizeShade;
                #else
                    // Default to step shading
                    float toonShade = StepShading(NdotL, _ShadowThreshold, _ShadowSmoothness);
                    diffuse = lerp(_ShadowColor.rgb, mainLight.color, toonShade) * shadowAttenuation;
                    lightingValue = toonShade;
                #endif
                
                // Add ambient/indirect lighting
                float3 indirectLight = SampleSH(normalWS) * _IndirectLightStrength;
                diffuse += indirectLight;
                
                // Calculate rim lighting
                float3 rimLight = float3(0, 0, 0);
                #ifdef _USERIMLIGHT_ON
                if (_UseRimLight > 0.5)
                {
                    rimLight = CalculateRimLight(normalWS, viewDirectionWS, mainLight.color);
                }
                #endif
                
                // Calculate specular highlights
                float3 specular = float3(0, 0, 0);
                #ifdef _USESPECULAR_ON
                if (_UseSpecular > 0.5)
                {
                    specular = CalculateSpecular(normalWS, lightDirectionWS, viewDirectionWS, 
                                               mainLight.color, shadowAttenuation);
                }
                #endif
                
                // Handle additional lights
                #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    float3 additionalLightDir = normalize(light.direction);
                    float additionalNdotL = dot(normalWS, additionalLightDir);
                    
                    float lightAttenuation = light.distanceAttenuation * light.shadowAttenuation;
                    
                    #if defined(_SHADINGMODE_STEP)
                        float additionalToonShade = StepShading(additionalNdotL, _ShadowThreshold, _ShadowSmoothness);
                        diffuse += light.color * additionalToonShade * lightAttenuation * 0.5;
                    #elif defined(_SHADINGMODE_RAMP)
                        float3 additionalRampColor = RampShading(additionalNdotL);
                        diffuse += additionalRampColor * light.color * lightAttenuation * 0.5;
                    #elif defined(_SHADINGMODE_POSTERIZE)
                        float additionalPosterizeShade = PosterizeShading(additionalNdotL);
                        diffuse += light.color * additionalPosterizeShade * lightAttenuation * 0.5;
                    #endif
                    
                    #ifdef _USESPECULAR_ON
                    if (_UseSpecular > 0.5)
                    {
                        specular += CalculateSpecular(normalWS, additionalLightDir, viewDirectionWS, 
                                                    light.color, lightAttenuation) * 0.5;
                    }
                    #endif
                }
                #endif
                
                // Combine lighting
                float3 finalColor = albedo.rgb * diffuse + rimLight + specular;
                
                // Apply halftone effect
                #ifdef _USEHALFTONE_ON
                finalColor = ApplyHalftone(finalColor, screenUV, lightingValue);
                #endif
                
                // Apply fog
                finalColor = MixFog(finalColor, input.fogFactor);
                
                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
        
        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        
        // Depth only pass
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }
    
    CustomEditor "MageLock.Graphics.Editor.ToonShaderGUI"
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}