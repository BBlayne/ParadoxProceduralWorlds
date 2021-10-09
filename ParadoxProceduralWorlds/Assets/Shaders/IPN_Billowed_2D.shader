
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'



Shader "Noise/IPN_Billowed_2D"
{
	Properties 
	{
		_Frequency("Frequency", float) = 1
		_Lacunarity("Lacunarity", float) = 2
		_Gain("Persistence", float) = 0.5
		_Octaves("Octaves", int) = 4
		_Scale("Scaling", Vector) = (1,1,0,0)
		_Offset("Offset", Vector) = (0,0,0,0)
	}
	SubShader
	{
		Pass
		{

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			sampler2D _PermTable1D, _Gradient2D;
			float _Frequency, _Lacunarity, _Gain;
			int _Octaves;
			float2 _Scale;
			float2 _Offset;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = _Scale * (v.texcoord - 0.5) + _Offset / _Frequency;

				return o;
			}

			float2 fade(float2 t)
			{
				return t * t * t * (t * (t * 6 - 15) + 10);
			}

			float perm(float x)
			{
				return tex2D(_PermTable1D, float2(x,0)).a;
			}

			float grad(float x, float2 p)
			{
				float2 g = tex2D(_Gradient2D, float2(x*8.0, 0)).rg *2.0 - 1.0;
				return dot(g, p);
			}

			float inoise(float2 p)
			{
				//p += _Offset; // adds panning but results in parallax
				float2 P = fmod(floor(p), 256.0);	// FIND UNIT SQUARE THAT CONTAINS POINT
				p -= floor(p);                      // FIND RELATIVE X,Y OF POINT IN SQUARE.
				float2 f = fade(p);                 // COMPUTE FADE CURVES FOR EACH OF X,Y.

				P = P / 256.0;
				const float one = 1.0 / 256.0;

				// HASH COORDINATES OF THE 4 SQUARE CORNERS
				float A = perm(P.x) + P.y;
				float B = perm(P.x + one) + P.y;

				// AND ADD BLENDED RESULTS FROM 4 CORNERS OF SQUARE
				return lerp(lerp(grad(perm(A), p),
								   grad(perm(B), p + float2(-1, 0)), f.x),
							 lerp(grad(perm(A + one), p + float2(0, -1)),
								   grad(perm(B + one), p + float2(-1, -1)), f.x), f.y);

			}

			// fractal sum, range -1.0 - 1.0
			float fBm(float2 p, int octaves)
			{
				float freq = _Frequency, amp = 0.5;
				float sum = 0;
				for (int i = 0; i < octaves; i++)
				{
					sum += inoise(p * freq) * amp;
					freq *= _Lacunarity;
					amp *= _Gain;
				}
				return sum;
			}

			// fractal abs sum, range 0.0 - 1.0
			float turbulence(float2 p, int octaves)
			{				
				float sum = 0;
				float freq = _Frequency, amp = 1.0;
				for (int i = 0; i < octaves; i++)
				{
					sum += abs(inoise(p * freq))*amp;
					freq *= _Lacunarity;
					amp *= _Gain;
				}
				return sum;
			}

			// Ridged multifractal, range 0.0 - 1.0
			// See "Texturing & Modeling, A Procedural Approach", Chapter 12
			float ridge(float h, float offset)
			{
				h = abs(h);
				h = offset - h;
				h = h * h;
				return h;
			}

			float ridgedmf(float2 p, int octaves, float offset)
			{
				float sum = 0;
				float freq = _Frequency, amp = 0.5;
				float prev = 1.0;
				for (int i = 0; i < octaves; i++)
				{
					float n = ridge(inoise(p*freq), offset);
					sum += n * amp*prev;
					prev = n;
					freq *= _Lacunarity;
					amp *= _Gain;
				}
				return sum;
			}

			half4 frag(v2f i) : COLOR
			{
				float n = turbulence(i.uv, _Octaves);

				return half4(n,n,n,1);
			}

			ENDCG

		}
	}
	Fallback "VertexLit"
}