Shader "Universal Render Pipeline/Plant"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        Cull Off

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
                float4 vertex : SV_POSITION;
                float4 colour : COLOR;
                float2 uv : TEXCOORD0;
                float2 light : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.colour = v.colour;

                float minLightLevel = 0.03;
                float shade = 0;
                float lightLevel = minLightLevel + (1 - minLightLevel) * (v.light.x / 15);
                o.light.x = clamp(1 - lightLevel + shade, 0, 1 - minLightLevel);
                o.light.y = 0;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                clip(col.a - 0.5);

                col *= i.colour - fixed4(0.2,0.2,0.2,1); // slightly darker than grass
                col = lerp(col, float4(0, 0, 0, 1), i.light.x);

                return col;
            }
            ENDCG
        }
    }
}
