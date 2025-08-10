Shader "MageLock/HealthBar"
{
    Properties
    {
        _Health ("Health", Range(0,1)) = 1
        _Damage ("Damage", Range(0,1)) = 1
        _HealthColor ("Health Color", Color) = (0,1,0,1)
        _Alpha ("Alpha", Range(0,1)) = 1
        
        [Header(Colors)]
        _BackgroundColor ("Background Color", Color) = (0.2, 0.2, 0.2, 0.8)
        _DamageColor ("Damage Color", Color) = (1, 0.3, 0.3, 0.8)
        _BorderColor ("Border Color", Color) = (0, 0, 0, 1)
        
        [Header(Dimensions)]
        _BorderWidth ("Border Width", Range(0, 0.1)) = 0.05
        _BarHeight ("Bar Height", Range(0, 1)) = 0.7
        _CornerRadius ("Corner Radius", Range(0, 0.5)) = 0.1
        
        [Header(Effects)]
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 0.5
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2
        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.1
        _LowHealthThreshold ("Low Health Threshold", Range(0, 0.5)) = 0.25
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent+100" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True"
            "DisableBatching"="True"
        }
        
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            float _Health;
            float _Damage;
            float4 _HealthColor;
            float _Alpha;
            
            float4 _BackgroundColor;
            float4 _DamageColor;
            float4 _BorderColor;
            
            float _BorderWidth;
            float _BarHeight;
            float _CornerRadius;
            
            float _GlowIntensity;
            float _PulseSpeed;
            float _PulseAmount;
            float _LowHealthThreshold;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float sdRoundedBox(float2 p, float2 size, float radius)
            {
                float2 q = abs(p) - size + radius;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius;
            }
            
            float smoothStep(float edge0, float edge1, float x)
            {
                float t = saturate((x - edge0) / (edge1 - edge0));
                return t * t * (3.0 - 2.0 * t);
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 centeredUV = uv - 0.5;
                
                float2 boxSize = float2(0.5, 0.5 * _BarHeight);
                float border = sdRoundedBox(centeredUV, boxSize, _CornerRadius);
                float borderMask = 1.0 - smoothStep(0, _BorderWidth * 0.01, abs(border));
                
                float innerBox = sdRoundedBox(centeredUV, boxSize - _BorderWidth, _CornerRadius * 0.8);
                float innerMask = 1.0 - smoothStep(0, 0.002, innerBox);
                
                float healthFill = step(uv.x, _Health);
                float damageFill = step(uv.x, _Damage);
                
                float4 finalColor = _BackgroundColor;
                
                finalColor = lerp(finalColor, _DamageColor, damageFill * innerMask);
                
                float4 healthColorAdjusted = _HealthColor;
                if (_Health < _LowHealthThreshold)
                {
                    float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                    healthColorAdjusted.rgb *= 1.0 + pulse * _PulseAmount;
                }
                
                finalColor = lerp(finalColor, healthColorAdjusted, healthFill * innerMask);

                if (_Health > 0 && uv.x > _Health - 0.02 && uv.x < _Health + 0.02)
                {
                    float edgeGlow = (1.0 - abs(uv.x - _Health) / 0.02) * _GlowIntensity;
                    finalColor.rgb += _HealthColor.rgb * edgeGlow;
                }
                
                finalColor = lerp(finalColor, _BorderColor, borderMask * (1.0 - innerMask));
                
                float centerDistance = length(centeredUV);
                float vignette = 1.0 - smoothStep(0.3, 0.5, centerDistance) * 0.3;
                finalColor.rgb *= vignette;
                
                finalColor.a *= _Alpha;
                finalColor.a *= 1.0 - smoothStep(-0.002, 0.002, border);
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "Sprites/Default"
}