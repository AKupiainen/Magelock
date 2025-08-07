Shader "UI/FractalSmoke_Optimized"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlueColor ("Blue Color", Color) = (0.529, 0.808, 0.980, 1)
        _OrangeColor ("Orange Color", Color) = (0.510, 0.204, 0.016, 1)
        _Speed ("Animation Speed", Float) = 1.0
        _AudioReactivity ("Audio Reactivity", Range(0, 2)) = 1.0
        _NoiseScale ("Noise Scale", Float) = 1.0
        
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
            
            #define OCTAVES 4.0 
            
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
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _BlueColor;
            fixed4 _OrangeColor;
            float _Speed;
            float _AudioReactivity;
            float _NoiseScale;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            
            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                OUT.color = v.color * _Color;
                return OUT;
            }
            
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            float valueNoise(float2 vl)
            {
                float2 grid = floor(vl);
                float2 frac_vl = frac(vl); 
                
                float s = rand(grid);
                float t = rand(grid + float2(1, 0));
                float u = rand(grid + float2(0, 1));
                float v = rand(grid + float2(1, 1));
                
                float x_interp = smoothstep(0.0, 1.0, frac_vl.x);
                float y_interp = smoothstep(0.0, 1.0, frac_vl.y);
                
                return lerp(lerp(s, t, x_interp), lerp(u, v, x_interp), y_interp);
            }
            
            float fractalNoise(float2 vl)
            {
                float persistance = 2.0; 
                float amplitude = 0.5;
                float rez = 0.0;
                float2 p = vl;
                
                for (float i = 0.0; i < OCTAVES; i++)
                {
                    rez += amplitude * valueNoise(p);
                    amplitude /= persistance;
                    p *= persistance;
                }
                return rez;
            }
            
            float complexFBM(float2 p)
            {
                float audioSimulation = (sin(_Time.y * 2.0) + 1.0) * 0.5;
                float sound = audioSimulation * _AudioReactivity;
                
                float slow = _Time.y * _Speed / 2.5;
                float fast = _Time.y * _Speed / 0.5;
                float2 offset1 = float2(slow, 0.0);
                float2 offset2 = float2(sin(fast) * 0.1, 0.0);
                
                return (1.0 + sound) * fractalNoise(p + offset1 + fractalNoise(
                    p + fractalNoise(
                        p + 2.0 * fractalNoise(p - offset2)
                    )
                ));
            }
            
            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord * _NoiseScale;
                
                float noiseValue = complexFBM(uv);
                fixed3 finalColor = lerp(_OrangeColor.rgb, _BlueColor.rgb, noiseValue);
                
                fixed4 color = fixed4(finalColor, 1.0) * IN.color;
                
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