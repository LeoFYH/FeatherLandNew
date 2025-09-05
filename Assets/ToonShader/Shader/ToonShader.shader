Shader "Advanced/ToonShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Ramp ("Ramp Texture", 2D) = "white" {}
        _ToonSpecColor ("Specular Color", Color) = (1,1,1,1)
        _SpecSize ("Specular Size", Range(0,1)) = 0.1
        _SpecSmooth ("Specular Smooth", Range(0,1)) = 0.05
        _RimColor ("Rim Color", Color) = (0.5,0.5,1,1)
        _RimPower ("Rim Power", Range(0.5,8.0)) = 3.0
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.001,0.1)) = 0.01
        _NormalMap ("Normal Map", 2D) = "bump" {}
        [Toggle(TOON_STEP)] _UseStep ("Use Toon Step", Float) = 1
        _StepAmount ("Step Amount", Range(1, 20)) = 5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        // Improved outline pass
        Pass
        {
            Name "OUTLINE"
            Cull Front
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            float _OutlineWidth;
            float4 _OutlineColor;

            v2f vert (appdata v)
            {
                v2f o;
                
                // Improved view-independent outline
                float4 clipPos = UnityObjectToClipPos(v.vertex);
                float3 clipNormal = mul((float3x3) UNITY_MATRIX_VP, mul((float3x3) UNITY_MATRIX_M, v.normal));
                float2 offset = normalize(clipNormal.xy) * _OutlineWidth * clipPos.w;
                
                // Adjust for aspect ratio
                float aspect = _ScreenParams.x / _ScreenParams.y;
                offset.x /= aspect;
                
                o.pos = clipPos;
                o.pos.xy += offset;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        // Main toon shading pass
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma shader_feature TOON_STEP
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                float3 tspace0 : TEXCOORD4;
                float3 tspace1 : TEXCOORD5;
                float3 tspace2 : TEXCOORD6;
                LIGHTING_COORDS(7,8)
            };

            sampler2D _MainTex;
            sampler2D _Ramp;
            sampler2D _NormalMap;
            float4 _BaseColor;
            float4 _ToonSpecColor;
            float _SpecSize;
            float _SpecSmooth;
            float4 _RimColor;
            float _RimPower;
            float _StepAmount;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = WorldSpaceViewDir(v.vertex);
                
                float3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                float3 worldBitangent = cross(o.worldNormal, worldTangent) * v.tangent.w;
                o.tspace0 = float3(worldTangent.x, worldBitangent.x, o.worldNormal.x);
                o.tspace1 = float3(worldTangent.y, worldBitangent.y, o.worldNormal.y);
                o.tspace2 = float3(worldTangent.z, worldBitangent.z, o.worldNormal.z);
                
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Reconstruct tangent space
                float3 worldNormal;
                worldNormal.x = dot(i.tspace0, i.worldNormal);
                worldNormal.y = dot(i.tspace1, i.worldNormal);
                worldNormal.z = dot(i.tspace2, i.worldNormal);
                
                // Sample and unpack normal map
                float3 normalMap = UnpackNormal(tex2D(_NormalMap, i.uv));
                float3 normal = normalize(worldNormal + normalMap);
                
                // Calculate lighting vectors
                float3 viewDir = normalize(i.viewDir);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float atten = LIGHT_ATTENUATION(i);
                
                // Sample main texture
                fixed4 mainTex = tex2D(_MainTex, i.uv) * _BaseColor;
                
                // Diffuse lighting
                float ndotl = dot(normal, lightDir) * 0.5 + 0.5;
                
                // Toon step effect
                #ifdef TOON_STEP
                    ndotl = floor(ndotl * _StepAmount) / (_StepAmount - 0.5);
                #endif
                
                // Apply ramp
                float2 rampUV = float2(saturate(ndotl), 0.5);
                fixed3 ramp = tex2D(_Ramp, rampUV).rgb;
                fixed3 diffuse = _LightColor0.rgb * ramp * mainTex.rgb * atten;
                
                // Specular highlight
                float3 halfVec = normalize(lightDir + viewDir);
                float ndoth = saturate(dot(normal, halfVec));
                float spec = smoothstep(0.5 - _SpecSmooth, 0.5 + _SpecSmooth, ndoth - (1 - _SpecSize));
                fixed3 specular = _ToonSpecColor.rgb * spec * atten;
                
                // Rim lighting
                float ndotv = 1 - saturate(dot(normal, viewDir));
                float rim = pow(ndotv, _RimPower);
                fixed3 rimLight = _RimColor.rgb * rim;
                
                // Combine components
                fixed4 finalColor;
                finalColor.rgb = diffuse + specular + rimLight;
                finalColor.a = mainTex.a;
                
                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}