Shader "Unlit/ParticleVisualize"
{
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "ParticleCommon.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
                float2 uv : TEXCOORD0;
				float4 col : TEXCOORD1;
				float4 pos : SV_POSITION;
			};
			
			StructuredBuffer<ParticleData> _Particles;
			StructuredBuffer<uint> _Active;
			uniform float _LifeTime;
			
			v2f vert (uint vid : SV_VertexID, uint iid : SV_InstanceID)
			{
				uint idx = _Active[iid];
				ParticleData p = _Particles[idx];
                float3 pos = p.position;
                float2 uv = vid<3 ? float2(saturate(vid-1.0), vid%2.0) : float2(saturate(vid-3.0), saturate(5.0-vid));

                pos = mul(UNITY_MATRIX_V, float4(pos,1)).xyz;
                float size = min(p.size / length(pos), 0.5);
                pos.xy += (uv-0.5) * p.size * saturate(p.duration / _LifeTime);

				v2f o;
				o.pos = mul(UNITY_MATRIX_P, float4(pos,1.0));
                o.uv = uv;
				o.col = p.color;

				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				half d = distance(i.uv, 0.5);
				half4 col = i.col;
				col.a *= smoothstep(0.5,0,d);
				clip(col.a - 0.5);
				return col;
			}
			ENDCG
		}
	}
}
