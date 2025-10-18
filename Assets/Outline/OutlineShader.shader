Shader "Sprites/OutlineShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0, 10)) = 1.0
        _AlphaThreshold ("Alpha Threshold", Range(0, 1)) = 0.5
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
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
        
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"
            
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
            };
            
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _AlphaThreshold;
            
            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif
                
                return OUT;
            }
            
            fixed4 frag(v2f IN) : SV_Target
            {
                // Sample the main texture
                fixed4 c = tex2D(_MainTex, IN.texcoord);
                
                // If the current pixel is above the alpha threshold, render it normally
                if (c.a > _AlphaThreshold)
                {
                    c *= IN.color;
                    c.rgb *= c.a;
                    return c;
                }
                
                // Otherwise, check if we should draw an outline
                // Calculate texel size for sampling neighbors
                float2 pixelSize = _MainTex_TexelSize.xy * _OutlineWidth;
                
                // Sample 8 directions around the current pixel
                float outline = 0.0;
                outline += tex2D(_MainTex, IN.texcoord + float2(pixelSize.x, 0)).a;           // Right
                outline += tex2D(_MainTex, IN.texcoord + float2(-pixelSize.x, 0)).a;          // Left
                outline += tex2D(_MainTex, IN.texcoord + float2(0, pixelSize.y)).a;           // Up
                outline += tex2D(_MainTex, IN.texcoord + float2(0, -pixelSize.y)).a;          // Down
                outline += tex2D(_MainTex, IN.texcoord + float2(pixelSize.x, pixelSize.y)).a;   // Top-Right
                outline += tex2D(_MainTex, IN.texcoord + float2(-pixelSize.x, pixelSize.y)).a;  // Top-Left
                outline += tex2D(_MainTex, IN.texcoord + float2(pixelSize.x, -pixelSize.y)).a;  // Bottom-Right
                outline += tex2D(_MainTex, IN.texcoord + float2(-pixelSize.x, -pixelSize.y)).a; // Bottom-Left
                
                // If any neighbor has alpha above threshold, draw the outline
                if (outline > _AlphaThreshold)
                {
                    fixed4 outlineColor = _OutlineColor * IN.color;
                    outlineColor.rgb *= outlineColor.a;
                    return outlineColor;
                }
                
                // No outline needed, discard pixel
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
