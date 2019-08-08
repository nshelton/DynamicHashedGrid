Shader "Custom/Particle" {

	Properties
	{
		_velocityScale("_velocityScale", float) = 1.0
	}

	SubShader {
		Pass {
		Tags{ "RenderType" = "Opaque" }
		LOD 200
		Blend SrcAlpha one

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma vertex vert
		#pragma fragment frag

		#include "UnityCG.cginc"
		#include "coloring.cginc"

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 5.0

		struct Particle{
			float3 position;
			float3 velocity;
		};
		
		struct PS_INPUT{
			float4 position : SV_POSITION;
			float4 color : COLOR;
			float life : LIFE;
		};
		// particles' data
		StructuredBuffer<Particle> particleBuffer;
		float _velocityScale;

		PS_INPUT vert(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
		{
			PS_INPUT o = (PS_INPUT)0;

			//float3 c = palette(_velocityScale * length(particleBuffer[instance_id].velocity), 3);
			float3 c = magma_quintic(_velocityScale * length(particleBuffer[instance_id].velocity));
			o.color = float4(c, 1.0);
			
			o.position = UnityObjectToClipPos(float4(particleBuffer[instance_id].position, 1));

			return o;
		}

		float4 frag(PS_INPUT i) : COLOR
		{
			return i.color;
		}


		ENDCG
		}
	}
	FallBack Off
}