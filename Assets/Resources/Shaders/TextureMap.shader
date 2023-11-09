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
                int2 vertexData : POSITION;
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

                float3 vertex;
                vertex.x = (v.vertexData.x >> 13) & 0x1F;
                vertex.y = (v.vertexData.x >> 5) & 0xFF;
                vertex.z = v.vertexData.x & 0x1F;

                float3 normal;
                int x = (v.vertexData.x >> 22) & 0x3;
                int y = (v.vertexData.x >> 20) & 0x3;
                int z = (v.vertexData.x >> 18) & 0x3;
                // 11 in binary = 3 but represents -1 in my case
                normal.x = x == 3 ? -1 : x;
                normal.y = y == 3 ? -1 : y;
                normal.z = z == 3 ? -1 : z;

                float2 light;
                light.x = (v.vertexData.x >> 28) & 0xF; // sky light
                light.y = (v.vertexData.x >> 24) & 0xF; // block light

                // converts RGB565 to RGB888
                // (R8/B8 * 527 + 23) >> 6
                // (G8 * 259 + 33) >> 6
                float4 colour;
                int packedColour = v.vertexData.y & 0xFFFF;
                colour.r = (((packedColour >> 11) & 0x1F) * 527 + 23 ) >> 6;
                colour.g = (((packedColour >> 5) & 0x3F) * 259 + 33 ) >> 6;
                colour.b = ((packedColour & 0x1F) * 527 + 23 ) >> 6;
                colour.a = packedColour == 0 ? 0 : 1;

                float2 uv;
                uv.x = (v.vertexData.y >> 21) & 0x1F;
                uv.x /= 16;
                uv.y = (v.vertexData.y >> 16) & 0x1F;
                uv.y /= 16;

                o.vertex = UnityObjectToClipPos(vertex);
                o.uv = TRANSFORM_TEX(uv, _MainTex);
                o.colour = colour / 255; // normalize between 0-1

                // 0.0647 = normalized difference between light levels
                float minLightLevel = 0.03;
                float shade = 0;
                if (false)
                {
                    if (normal.y == 1) // top
                        shade = 0;
                    else if (normal.y == -1) // bottom 
                        shade = 0.1594;
                    else if (normal.z != 0) // north/south
                        shade = 0.0947;             
                    else if (normal.x != 0) // east/west
                        shade = 0.03;
                }
                else
                {
                    if (normal.y == -1) // bottom 
                        shade = 0.6;
                    else if (normal.z != 0) // north/south
                        shade = 0.4;             
                    else if (normal.x != 0) // east/west
                        shade = 0.2;
                }

                float lightLevel = minLightLevel + (1 - minLightLevel) * (light.x / 15);
                o.light.x = clamp(1 - lightLevel + shade, 0, 1 - minLightLevel);
                o.light.y = 0;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                if (i.colour.a != 0)
                    col = col * i.colour;
                col = lerp(col, fixed4(0, 0, 0, 1), i.light.x);
                return col;
            }
            ENDCG
        }
    }
}
