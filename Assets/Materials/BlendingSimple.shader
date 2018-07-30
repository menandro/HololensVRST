Shader "Custom/BlendingSimple" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_SpatialMapTex("Spatial Mapping Depth Texture", 2D) = "white"{}
		_CgDepthTex("Cg Depth Texture", 2D) = "white"{}
		_VisibiliyComplex("Visiblity Complex", Range(0, 1.0)) = 0.2
		_VisibilitySimple("Visibility Simple", Range(0, 1.0)) = 0.4
	}

	SubShader{
	Pass{
		CGPROGRAM
		#pragma vertex vert_img
		#pragma fragment frag
		#include "UnityCG.cginc" // required for v2f_img

		// Properties
		sampler2D _MainTex;
		float4 _MainTex_TexelSize;
		sampler2D _SpatialMapTex;
		float4 _SpatialMapTex_TexelSize;
		sampler2D _CgDepthTex;
		float4 _CgDepthTex_TexelSize;
		float _VisibilityComplex;
		float _VisibilitySimple;

		float4 frag(v2f_img input) : COLOR{
			// sample texture for color
			float4 base = tex2D(_MainTex, input.uv);
			float4 sceneDepth = tex2D(_SpatialMapTex, input.uv);
			float4 cgDepth = tex2D(_CgDepthTex, input.uv);

			float4 output = base;
			float maxer;
			if (cgDepth.x <= sceneDepth.x) { //CG is BG
				output = _VisibilitySimple * output;
			}
			//return float4(3.0* sceneDepth.x, 3.0*sceneDepth.x, 3.0*sceneDepth.x, 1.0f);
			return output;
		}
		ENDCG
	}
	}
}
