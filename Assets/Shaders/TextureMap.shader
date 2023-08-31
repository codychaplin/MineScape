Shader "Universal Render Pipeline/TextureMap"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Albedo", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "ForceNoShadowCasting" = "True" "RenderPipeline" = "UniversalRenderPipeline" }
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
                float2 uv : TEXCOORD0;
                float2 light : TEXCOORD1;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 light : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                float minLightLevel = 0.1;
                float shade = 0;
                if (v.normal.y == -1) // bottom
                    shade = 0.6;
                if (v.normal.z != 0) // north/south
                    shade = 0.5;             
                if (v.normal.x != 0) // east/west
                    shade = 0.4;
                
                float lightLevel = minLightLevel + (1 - minLightLevel) * (v.light.x / 16);
                o.light.x = clamp(1 - lightLevel + shade, 0, 1 - minLightLevel);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col = lerp(col, float4(0, 0, 0, 1), i.light.x);
                return col;
            }
            ENDCG
        }
    }
}
