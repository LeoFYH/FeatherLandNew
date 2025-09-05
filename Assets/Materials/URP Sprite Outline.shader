Shader "Custom/URP Sprite Outline"
{
    Properties
    {
        [MainTexture] _MainTex("Main Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _OutlineColor("Outline Color", Color) = (1,1,0,1)
        _OutlineThickness("Outline Thickness", Range(0, 10)) = 2
        _AlphaThreshold("Alpha Threshold", Range(0, 1)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float4 screenPos : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                float _OutlineThickness;
                float _AlphaThreshold;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 采样主纹理
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half alpha = texColor.a * IN.color.a;
                
                // 如果当前像素不透明，则直接返回主纹理颜色
                if (alpha > _AlphaThreshold)
                {
                    return texColor * _Color * IN.color;
                }
                
                // 计算描边
                float2 texelSize = _MainTex_TexelSize.xy * _OutlineThickness;
                half maxAlpha = 0;
                
                // 检查周围像素
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0) continue;
                        
                        float2 offset = float2(x, y) * texelSize;
                        half sampleAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + offset).a;
                        maxAlpha = max(maxAlpha, sampleAlpha);
                    }
                }
                
                // 如果周围有透明像素，则应用描边
                if (maxAlpha > _AlphaThreshold)
                {
                    return _OutlineColor;
                }
                
                // 否则返回透明
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Main"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return texColor * _Color * IN.color;
            }
            ENDHLSL
        }
    }
}