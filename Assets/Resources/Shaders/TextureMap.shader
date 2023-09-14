Shader "Universal Render Pipeline/TextureMap"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "ForceNoShadowCasting" = "True" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "MainPass"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 colour : COLOR;
                float2 uv : TEXCOORD0;
                float2 light : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 light : TEXCOORD1;
                float4 vertex : SV_POSITION;
                float4 colour : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.colour = v.colour;

                // 0.0647 = normalized difference between light levels
                float minLightLevel = 0.03;
                float shade = 0;
                if (v.normal.y == -1) // bottom
                    shade = 0.1594;
                if (v.normal.z != 0) // north/south
                    shade = 0.0947;             
                if (v.normal.x != 0) // east/west
                    shade = 0.03;

                float lightLevel = minLightLevel + (1 - minLightLevel) * (v.light.x / 15);
                o.light.x = clamp(1 - lightLevel + shade, 0, 1 - minLightLevel);
                o.light.y = 0;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                if (i.colour.a != 0)
                    col = col * i.colour;
                col = lerp(col, float4(0, 0, 0, 1), i.light.x);
                return col;
            }
            ENDCG
        }
    }
}
