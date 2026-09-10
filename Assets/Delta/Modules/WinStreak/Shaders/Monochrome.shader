Shader "Unlit/Monochrome"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MonochromeAmount ("Monochrome Amount", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Mobile-friendly optimizations
            #pragma target 2.0
            #pragma only_renderers gles gles3 metal vulkan d3d11
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                half2 uv : TEXCOORD0;
            };

            struct v2f
            {
                half2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            half4 _MainTex_ST;
            half _MonochromeAmount;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // Sample the texture
                half4 col = tex2D(_MainTex, i.uv);
                
                // Optimized monochrome conversion using luminance weights
                // Using dot product is more efficient than separate multiplications
                half gray = dot(col.rgb, half3(0.299, 0.587, 0.114));
                
                // Lerp between original color and grayscale based on _MonochromeAmount
                col.rgb = lerp(col.rgb, half3(gray, gray, gray), _MonochromeAmount);
                
                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    
    // Fallback for very old devices
    FallBack "Mobile/Unlit (Supports Lightmap)"
}
