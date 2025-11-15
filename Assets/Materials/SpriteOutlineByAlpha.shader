Shader "Custom/SpriteOutlineByAlpha"
{
    Properties
    {
        [PerRendererData]_MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineSize("Outline Size", Range(0.001, 5)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
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
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // x = 1/width, y = 1/height
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineSize;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.uv = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, IN.uv);
                float alpha = tex.a;

                // 八方向偏移，用于检测边缘
                float2 offsets[8] = {
                    float2(_MainTex_TexelSize.x, 0),
                    float2(-_MainTex_TexelSize.x, 0),
                    float2(0, _MainTex_TexelSize.y),
                    float2(0, -_MainTex_TexelSize.y),
                    float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y),
                    float2(-_MainTex_TexelSize.x, _MainTex_TexelSize.y),
                    float2(_MainTex_TexelSize.x, -_MainTex_TexelSize.y),
                    float2(-_MainTex_TexelSize.x, -_MainTex_TexelSize.y)
                };

                float edge = 0;

                // 检查周围像素透明度
                for (int i = 0; i < 8; i++)
                {
                    float a = tex2D(_MainTex, IN.uv + offsets[i] * _OutlineSize).a;
                    edge += step(0.01, alpha) * step(a, 0.01); // 内亮外空
                }

                // 如果当前像素透明，画描边；否则画原图
                fixed4 result;
                if (alpha > 0.01)
                {
                    result = tex * IN.color;
                }
                else if (edge > 0)
                {
                    result = _OutlineColor;
                }
                else
                {
                    result = 0;
                }

                return result;
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
