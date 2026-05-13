Shader "Unlit/MapCellHighlight"
{
    Properties
    {
        [PerRendererData]_MainTex ("Texture", 2D) = "white" {}
        _HighlightColor ("Highlight Color", Color) = (1,0,0,1)
        _HighlightStrength ("Highlight Strength", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags 
		{ 
			"Queue"="Transparent"
			"RenderType"="Transparent"
			"IgnoreProjector"="True"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

			sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _HighlightColor;
            float _HighlightStrength;

            // Max polygon vertices
            #define MAX_VERTS 32

            int _VertexCount;
            float4 _Polygon[MAX_VERTS];
            // xy = uv coordinates

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            bool PointInConvexPolygon(float2 p)
            {
                float sign = 0;
				bool signInitialized = false;

				// Must have at least a triangle
				if (_VertexCount < 3)
					return false;

                for (int i = 0; i < _VertexCount; i++)
                {
                    float2 a = _Polygon[i].xy;
                    float2 b = _Polygon[(i + 1) % _VertexCount].xy;

                    float2 edge = b - a;
                    float2 toPoint = p - a;

                    float crossZ = edge.x * toPoint.y - edge.y * toPoint.x;
					        // Ignore nearly-zero edges
					if (abs(crossZ) < 0.0001)
						continue;

					if (!signInitialized)
					{
						sign = crossZ;
						signInitialized = true;
					}
					else 
					{

					}

                    if (i == 0)
                    {
                        sign = crossZ;
                    }
                    else
                    {
						bool differentSign =
							(sign > 0 && crossZ < 0) ||
							(sign < 0 && crossZ > 0);

						if (differentSign)
							return false;
                    }
                }

                return signInitialized;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);

                if (PointInConvexPolygon(i.uv))
                {
                    col.rgb = lerp(
                        col.rgb,
                        _HighlightColor.rgb,
                        _HighlightStrength
                    );
                }

                return col;
            }
            ENDCG
        }
    }
}
