Shader "UI/Fire"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // Fire Animation
        _Speed ("Animation Speed", Range(0.1, 5)) = 1.5
        _Scale ("Noise Scale", Range(1, 20)) = 6.0
        _Distortion ("Distortion Amount", Range(0, 2)) = 0.5
        _FireHeight ("Fire Height", Range(0, 2)) = 1.2
        
        // UI Material Properties
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Speed;
            float _Scale;
            float _Distortion;
            float _FireHeight;
            float4 _ClipRect;

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // Simple hash function for noise
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            // Simple 2D noise function
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Fractional Brownian Motion for better fire effect
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                
                for(int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }
                
                return value;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y * _Speed;
                
                // Create upward moving fire effect
                float2 noiseUV = float2(uv.x * _Scale, uv.y * _Scale - time);
                float2 noiseUV2 = float2(uv.x * _Scale * 1.3, uv.y * _Scale * 0.8 - time * 1.5);
                
                // Generate noise patterns
                float n1 = fbm(noiseUV);
                float n2 = fbm(noiseUV2);
                float noise_combined = (n1 + n2) * 0.5;
                
                // Distort UVs for flame movement
                float2 distortedUV = uv;
                distortedUV.x += (noise_combined - 0.5) * _Distortion * 0.1;
                distortedUV.y += (noise_combined - 0.5) * _Distortion * 0.05;
                
                // Sample original texture
                fixed4 texColor = tex2D(_MainTex, distortedUV);
                
                // Add flickering effect
                float flicker = 0.85 + 0.15 * sin(time * 10.0 + noise_combined * 6.28);
                
                // Apply fire animation to texture
                fixed4 col = texColor;
                col.a *= i.color.a;
                
                // Apply clipping
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                
                return col;
            }
            ENDCG
        }
    }
}
