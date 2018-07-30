﻿Shader "Custom/VisibilityBlending" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_SpatialMapTex("Spatial Mapping Depth Texture", 2D) = "white"{}
		_CgDepthTex("Cg Depth Texture", 2D) = "white"{}
		_VisibiliyComplex("Visiblity Complex", Range(0, 1.0)) = 0.2
		_VisibilitySimple("Visibility Simple", Range(0, 1.0)) = 0.4
		_Beta("Amount of Visibility at Intersection", Range(0, 1.0)) = 0.9
		_Slope("Steepness of Sigmoid", Range(0, 10.0)) = 1.0
		_DepthMultiplier("Depth Multiplier", Range(1.0, 255.0)) = 5.0
	}

	SubShader{
		Pass{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc" // required for v2f_img

			// Properties
			uniform Texture2D _MainTex;
			uniform SamplerState sampler_MainTex;
			float4 _MainTex_TexelSize;

			uniform Texture2D _SpatialMapTex;
			uniform SamplerState sampler_SpatialMapTex;
			float4 _SpatialMapTex_TexelSize;
			
			uniform Texture2D _CgDepthTex;
			uniform SamplerState sampler_CgDepthTex;
			float4 _CgDepthTex_TexelSize;

			float _VisibilityComplex;
			float _VisibilitySimple;
			float _Beta;
			float _Slope;
			float _DepthMultiplier;

			struct vertexInput {
				float4 pos : POSITION;
				float4 tex0 : TEXCOORD0;
				float4 tex1 : TEXCOORD1;
			};

			struct vertexOutput {
				float4 pos: SV_POSITION;
				float4 tex0: TEXCOORD0;
				float4 tex1: TEXCOORD1;
			};

			float4 frag(vertexOutput input) : COLOR{
				float4 base;
				float4 sceneDepthC;
				float4 cgDepthC;
				int binTotal = 0;
				int range = 2;
				int scale = 4; // if Complex, set to high, if Simple, set to low

				base = _MainTex.Sample(sampler_MainTex, input.tex0);
				float4 centerCgDepth = _CgDepthTex.Sample(sampler_CgDepthTex, input.tex0);
				float4 centerSceneDepth = _SpatialMapTex.Sample(sampler_SpatialMapTex, input.tex0);
				int maxBin = (2 * range + 1)*(2 * range + 1);

				//if (centerSceneDepth.x > centerCgDepth.x) {
				if (centerCgDepth.x != 0.0f) {
					for (int j = -range; j <= range; j++) {
						for (int i = -range; i <= range; i++) {
							sceneDepthC = _SpatialMapTex.Sample(sampler_SpatialMapTex, input.tex0 + fixed2(_SpatialMapTex_TexelSize.x * i * scale, _SpatialMapTex_TexelSize.y * j * scale));
							cgDepthC = _CgDepthTex.Sample(sampler_CgDepthTex, input.tex0 + fixed2(_CgDepthTex_TexelSize.x * i * scale, _CgDepthTex_TexelSize.y * j * scale));
							if ((sceneDepthC.x > cgDepthC.x) && (sceneDepthC.x != 0.0f)) {
								if (cgDepthC.x == 0.0f) {
									if (centerSceneDepth.x > centerCgDepth.x) {
										binTotal++;
									}
								}
								else {
									binTotal++;
								}
								
							}
						}
					}
				}
				
				float weight = (float) binTotal / (float) maxBin;

				/*float x = (sceneDepth - cgDepth);
				float x0 = -log((1 - _Beta) / _Beta) / _Slope;
				float f = 1 - 1 / (1 + exp(-_Slope*(x - x0)));*/

				float4 output = base;
				float maxer;

				// TODO: Smoothing function comparison

				output = (1.0f - weight) * output;
				//if (cgDepth <= sceneDepth) { //CG is BG
				//	output = _VisibilitySimple * output;
				//}
				//return float4(sceneDepth.x, cgDepth.x, 0.0f, 1.0f);
				//return float4(x, x, 0.0f, 1.0f);
				return output;
			}
			ENDCG
		}
	}
}