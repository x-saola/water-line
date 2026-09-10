Shader "UI/AnimateImage"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // Animation Properties
        [Header(Idle Animation)]
        _AnimSpeed ("Animation Speed", Range(0, 5)) = 1.0
        _ScaleAmount ("Vertical Scale Amount", Range(0, 1)) = 0.1
        _WaveAmount ("Wave Amount", Range(0, 0.5)) = 0.05
        _WaveFrequency ("Wave Frequency", Range(0, 20)) = 5.0
        
        [Header(Additional Effects)]
        _BounceHeight ("Bounce Height", Range(0, 1)) = 0.1
        _BounceSpeed ("Bounce Speed", Range(0, 5)) = 1.5
        _RotationAmount ("Rotation Amount", Range(0, 45)) = 5.0
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 0.3
        _GlowSpeed ("Glow Speed", Range(0, 5)) = 1.0
        _ShimmerSpeed ("Shimmer Speed", Range(0, 5)) = 2.0
        _ShimmerWidth ("Shimmer Width", Range(0, 1)) = 0.2
        _ShimmerIntensity ("Shimmer Intensity", Range(0, 1)) = 0.5
        
        // UI Properties
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
                float2 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            
            // Animation parameters
            float _AnimSpeed;
            float _ScaleAmount;
            float _WaveAmount;
            float _WaveFrequency;
            float _BounceHeight;
            float _BounceSpeed;
            float _RotationAmount;
            float _GlowIntensity;
            float _GlowSpeed;
            float _ShimmerSpeed;
            float _ShimmerWidth;
            float _ShimmerIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                
                // Center the vertex for rotation
                float2 center = float2(0, 0);
                float2 pos = v.vertex.xy - center;
                
                // Apply rotation animation
                float angle = sin(_Time.y * _AnimSpeed * 0.5) * _RotationAmount * 0.0174533; // Convert to radians
                float cosA = cos(angle);
                float sinA = sin(angle);
                float2 rotated;
                rotated.x = pos.x * cosA - pos.y * sinA;
                rotated.y = pos.x * sinA + pos.y * cosA;
                v.vertex.xy = rotated + center;
                
                // Apply vertical scale animation (breathing effect)
                float scale = 1.0 + sin(_Time.y * _AnimSpeed) * _ScaleAmount;
                v.vertex.y *= scale;
                
                // Apply bounce (vertical movement)
                float bounce = sin(_Time.y * _BounceSpeed) * _BounceHeight;
                v.vertex.y += bounce;
                
                // Apply wave animation to top of image (UV.y close to 1)
                float waveMask = smoothstep(0.5, 1.0, v.uv.y); // Only affect top half, stronger at top
                float wave = sin(_Time.y * _AnimSpeed * 2.0 + v.uv.x * _WaveFrequency) * _WaveAmount * waveMask;
                v.vertex.x += wave;
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                o.worldPos = v.vertex.xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample texture
                fixed4 color = tex2D(_MainTex, i.uv) + _TextureSampleAdd;
                color *= i.color;
                
                // Apply glow pulse effect
                float glow = 1.0 + sin(_Time.y * _GlowSpeed) * _GlowIntensity;
                color.rgb *= glow;
                
                // Apply shimmer effect (diagonal sweep)
                float shimmerPos = frac(_Time.y * _ShimmerSpeed * 0.2);
                float shimmerLine = i.uv.x + i.uv.y * 0.5; // Diagonal line
                float shimmer = smoothstep(shimmerPos - _ShimmerWidth, shimmerPos, shimmerLine) * 
                               smoothstep(shimmerPos + _ShimmerWidth, shimmerPos, shimmerLine);
                shimmer = abs(shimmer);
                color.rgb += shimmer * _ShimmerIntensity;
                
                // Clip for rect mask
                color.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                
                // Discard fully transparent pixels
                clip(color.a - 0.001);
                
                return color;
            }
            ENDCG
        }
    }
    
    FallBack "UI/Default"
}
