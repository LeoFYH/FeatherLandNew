Shader "Custom/AdvancedToon_v2_Textured"
{
    Properties
    {
        _Color ("Main Color", Color) = (1, 1, 1, 1)
        _MainTex ("Main Texture", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0.3, 0.3, 0.3, 1)
        _ToonSteps ("Toon Steps", Range(2, 8)) = 3

        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _Shininess ("Shininess", Range(0.1, 1)) = 0.5
        
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode"="Always" }
            Cull Front
            ZWrite On
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
            };
            
            float _OutlineWidth;
            fixed4 _OutlineColor;

            v2f vert(appdata v)
            {
                v2f o;
                v.vertex.xyz += normalize(v.normal) * _OutlineWidth;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
        
        Pass
        {
            Name "MAIN"
            Tags { "LightMode"="ForwardBase" }
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float2 uv : TEXCOORD2;
                SHADOW_COORDS(3)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _ShadowColor;
            float _ToonSteps;
            fixed4 _SpecularColor;
            float _Shininess;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normalDirection = normalize(i.worldNormal);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
                
                float3 lightDirection = normalize(UnityWorldSpaceLightDir(i.worldPos));
                float attenuation = LIGHT_ATTENUATION(i);

                float NdotL = dot(normalDirection, lightDirection);
                float lightIntensity = NdotL * 0.5 + 0.5;
                
                float toonFactor = floor(lightIntensity * _ToonSteps) / _ToonSteps;
                toonFactor *= attenuation;

                fixed4 texColor = tex2D(_MainTex, i.uv);
                fixed4 celColor = lerp(_ShadowColor, _Color * texColor, toonFactor);

                float3 halfwayDir = normalize(lightDirection + viewDirection);
                float specAngle = max(0.0, dot(normalDirection, halfwayDir));
                float specular = pow(specAngle, _Shininess * 128.0);
                
                float specularIntensity = smoothstep(0.95, 1.0, specular) * step(0.5, toonFactor);
                fixed4 specularColor = specularIntensity * _SpecularColor * _LightColor0;

                fixed4 finalColor = celColor * _LightColor0 + specularColor;

                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Legacy Shaders/VertexLit"
}