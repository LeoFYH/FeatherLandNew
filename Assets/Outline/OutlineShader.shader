Shader "Sprites/OutlineShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0, 20)) = 1.0
        _UVScale ("UV Scale", Range(1, 1.5)) = 1.0
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
            float _UVScale;
            float _AlphaThreshold;
            
            v2f vert(appdata_t IN)
            {
                v2f OUT;
                // No mesh modification - pass through original vertex
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
                // Scale UV towards center to shrink sprite, leaving space for outline
                float2 texCenter = float2(0.5, 0.5);
                float2 uvDir = IN.texcoord - texCenter;
                float2 scaledUV = texCenter + uvDir * _UVScale;
                scaledUV = clamp(scaledUV, float2(0.0, 0.0), float2(1.0, 1.0));
                
                // Sample the main texture with scaled UV
                fixed4 c = tex2D(_MainTex, scaledUV);
                
                // If the current pixel is above the alpha threshold, render it normally
                if (c.a > _AlphaThreshold)
                {
                    c *= IN.color;
                    c.rgb *= c.a;  // Premultiply alpha for sprite blending
                    return c;
                }
                
                // Current pixel is transparent (or below threshold)
                // Simple outline algorithm: sample surrounding pixels in a circle
                float2 pixelSize = _MainTex_TexelSize.xy;
                int outlineWidth = (int)clamp(_OutlineWidth, 1, 20); // Limit max iterations to avoid unroll issues
                
                // Sample 8 directions around the current pixel
                const float2 directions[8] = {
                    float2(1, 0), float2(-1, 0), float2(0, 1), float2(0, -1),
                    float2(0.707, 0.707), float2(-0.707, 0.707), float2(0.707, -0.707), float2(-0.707, -0.707)
                };
                
                bool foundForeground = false;
                
                // Check pixels at increasing distances up to outline width
                // Use loop instead of unroll to handle variable iteration count
                // Use scaledUV for outline detection to match the scaled sprite
                [loop]
                for (int dist = 1; dist <= outlineWidth && !foundForeground; dist++)
                {
                    [unroll(8)]
                    for (int dir = 0; dir < 8; dir++)
                    {
                        float2 offset = directions[dir] * float(dist) * pixelSize;
                        float2 sampleUV = scaledUV + offset;
                        sampleUV = clamp(sampleUV, float2(0.0, 0.0), float2(1.0, 1.0));
                        
                        if (tex2D(_MainTex, sampleUV).a > _AlphaThreshold)
                        {
                            foundForeground = true;
                            break;
                        }
                    }
                }
                
                // Draw outline if foreground found
                if (foundForeground)
                {
                    fixed4 result = _OutlineColor * IN.color;
                    result.a = _OutlineColor.a * IN.color.a;
                    result.rgb *= result.a;
                    return result;
                }
                
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
