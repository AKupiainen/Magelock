Shader "UI/DeformedSphere"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        _SphereRadius ("Sphere Radius", Range(0.5, 3.0)) = 1.75
        _NoiseScale ("Noise Scale", Range(0.1, 3.0)) = 1.0
        _NoiseAmplitude ("Noise Amplitude", Range(0.0, 1.0)) = 0.5
        _AnimationSpeed ("Animation Speed", Range(0.0, 2.0)) = 0.55
        _RotationSpeed ("Rotation Speed", Range(0.0, 2.0)) = 0.3
        _RimPower ("Rim Power", Range(1.0, 10.0)) = 6.0
        _BaseColor ("Base Color", Color) = (0.9, 0.9, 0.9, 1.0)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _SphereRadius;
            float _NoiseScale;
            float _NoiseAmplitude;
            float _AnimationSpeed;
            float _RotationSpeed;
            float _RimPower;
            fixed4 _BaseColor;

            static const float INTERSECTION_PRECISION = 0.01;
            static const int NUM_OF_TRACE_STEPS = 15;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            float hash(float n) 
            { 
                return frac(sin(n) * 43758.5453); 
            }

            float noise(float3 x)
            {
                float3 p = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                float n = p.x + p.y * 57.0 + 113.0 * p.z;
                return lerp(lerp(lerp(hash(n), hash(n + 1.0), f.x),
                                 lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
                            lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
                                 lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
            }

            float2 map(float3 pos)
            {
                float t = _Time.y * _RotationSpeed;
                float c = cos(t), s = sin(t);
                float3 rp = float3(c * pos.x + s * pos.z, pos.y, -s * pos.x + c * pos.z);
                float sphere = length(rp) - _SphereRadius + noise(rp * _NoiseScale + _Time.y * _AnimationSpeed) * _NoiseAmplitude;
                return float2(sphere, 1.0);
            }

            float3 calcNormal(float3 pos)
            {
                float2 e = float2(0.01, 0.0);
                return normalize(float3(
                    map(pos + e.xyy).x - map(pos - e.xyy).x,
                    map(pos + e.yxy).x - map(pos - e.yxy).x,
                    map(pos + e.yyx).x - map(pos - e.yyx).x));
            }

            bool renderRayMarch(float3 ro, float3 rd, inout float3 color)
            {
                float t = 0.0;
                for (int i = 0; i < NUM_OF_TRACE_STEPS; ++i)
                {
                    float d = map(ro + rd * t).x;
                    if (d < INTERSECTION_PRECISION)
                    {
                        float3 pos = ro + rd * t;
                        float3 normal = calcNormal(pos);
                        float rim = pow(1.0 - abs(dot(-rd, normal)), _RimPower);
                        color = lerp(color, _BaseColor.rgb + rim, rim + 0.1);
                        return true;
                    }
                    t += d;
                    if (t > 10.0) break;
                }
                return false;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 p = IN.texcoord * 2.0 - 1.0;
                float3 ro = float3(0.0, 0.0, -4.0);
                float3 rd = normalize(float3(p.xy, 2.0));
                float3 col = 0;
                float alpha = 0.0;

                if (renderRayMarch(ro, rd, col))
                {
                    alpha = _BaseColor.a;
                    col *= 1.0 - smoothstep(1.0, 2.5, length(p)) * 0.3;
                }

                half4 color = half4(col, alpha) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}