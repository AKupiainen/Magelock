Shader "UI/RainbowSwipeEffect"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _SwipeSpeed ("Swipe Speed", Range(0.1, 5)) = 1.2
        _SwipeInterval ("Swipe Interval", Range(0.5, 10)) = 4
        _SwipeWidth ("Swipe Width", Range(0.05, 0.5)) = 0.25
        _SwipeIntensity ("Swipe Intensity", Range(0, 2)) = 1.5
        _CurveAmount ("Curve Amount", Range(0, 2)) = 0.8
        _SwipeAngle ("Swipe Angle", Range(-90, 90)) = 15
        _CenterRadius ("Center Effect Radius", Range(0.1, 1)) = 0.7
        _RainbowSaturation ("Rainbow Saturation", Range(0, 2)) = 1.3
        _RainbowBrightness ("Rainbow Brightness", Range(0, 2)) = 1.8
        // UI properties
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
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
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            
            float _SwipeSpeed;
            float _SwipeInterval;
            float _SwipeWidth;
            float _SwipeIntensity;
            float _CurveAmount;
            float _SwipeAngle;
            float _CenterRadius;
            float _RainbowSaturation;
            float _RainbowBrightness;

            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }

            float2 rotateUV(float2 uv, float angle)
            {
                float rad = radians(angle);
                float cosA = cos(rad);
                float sinA = sin(rad);
                
                uv -= 0.5;
                
                float2 rotated = float2(
                    uv.x * cosA - uv.y * sinA,
                    uv.x * sinA + uv.y * cosA
                );
                
                return rotated + 0.5;
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                float2 uv = IN.texcoord;
                
                float2 rotatedUV = rotateUV(uv, _SwipeAngle);
                
                float time = _Time.y * _SwipeSpeed;
                float swipePhase = fmod(time, _SwipeInterval) / _SwipeInterval;
                
                float2 center = float2(0.5, 0.5);
                float distanceFromCenter = length(uv - center);
                
                float centerMask = 1.0 - smoothstep(0.0, _CenterRadius, distanceFromCenter);
                
                float yNormalized = rotatedUV.y;
                float centerDistance = abs(yNormalized - 0.5) * 2.0;
                
                float circularFalloff = 1.0 - smoothstep(0.0, 1.0, centerDistance);
                circularFalloff = pow(circularFalloff, 2.0);
                
                circularFalloff *= centerMask;
                
                float centerDelay = circularFalloff * _CurveAmount * 0.4;
                
                float delayPhase = smoothstep(0.2, 0.8, swipePhase);
                float actualDelay = centerDelay * sin(delayPhase * 3.14159);
                
                float curveOffset = -actualDelay;
                
                float swipeProgress = swipePhase + curveOffset;
                
                float swipePosition = swipeProgress * (1.0 + _SwipeWidth * 2.0) - _SwipeWidth;
                
                float distanceFromSwipe = abs(rotatedUV.x - swipePosition);
                
                float swipeMask = 1.0 - smoothstep(0.0, _SwipeWidth, distanceFromSwipe);
                
                float leftEdge = swipePosition - _SwipeWidth;
                float rightEdge = swipePosition + _SwipeWidth;
                float swipeVisibility = step(leftEdge, 1.0) * step(-_SwipeWidth, rightEdge);
                swipeMask *= swipeVisibility;
                
                swipeMask *= (centerMask * 0.5 + 0.5);
                
                float hue = frac(uv.x * 0.5 + time * 0.1);
                float3 rainbowColor = hsv2rgb(float3(hue, _RainbowSaturation, _RainbowBrightness));
                
                float glowIntensity = pow(swipeMask, 0.5) * _SwipeIntensity;
                
                float3 finalColor = lerp(color.rgb, color.rgb + rainbowColor * glowIntensity, swipeMask * glowIntensity);
                color.rgb = finalColor;
                
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}