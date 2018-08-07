Shader "Custom/VisibilityBlendingOverlay" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_SpatialMapTex("Spatial Mapping Depth Texture", 2D) = "white"{}
		_CgDepthTex("Cg Depth Texture", 2D) = "white"{}
		_WebcamTex("Webcam Texture", 2D) = "white"{}
		_VisibilityComplex("Visiblity Complex", Range(0, 1.0)) = 0.1
		_VisibilitySimple("Visibility Simple", Range(0, 1.0)) = 0.01

	}

		SubShader{
		Pass{
		CGPROGRAM
		#pragma vertex vert_img
		#pragma fragment frag
		#include "UnityCG.cginc" // required for v2f_img

		// Properties
		uniform Texture2D _MainTex;
		//uniform SamplerState sampler_MainTex;
		float4 _MainTex_TexelSize;

		uniform Texture2D _SpatialMapTex;
		//uniform SamplerState sampler_SpatialMapTex;
		float4 _SpatialMapTex_TexelSize;

		uniform Texture2D _CgDepthTex;
		//uniform SamplerState sampler_CgDepthTex;
		float4 _CgDepthTex_TexelSize;

		uniform Texture2D _WebcamTex;
		uniform SamplerState sampler_WebcamTex;
		float4 _WebcamTex_TexelSize;

		SamplerState sampler_MainTex;
		//SamplerState my_point_repeat_sampler;

		float _VisibilityComplex;
		float _VisibilitySimple;

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
		//Parameters:
		int windowSize = 5;
		int windowScale = 5; // if Complex, set to high, if Simple, set to low
		float xm = (float)((2 * windowSize + 1)*(2 * windowSize + 1));
		float xd = (xm / 2);
		float pm = 0.2f;
		float pd = 0.90f;
		float k = (log(pm / (1 - pm)) - log(pd / (1 - pd))) / (xd - xm);
		float x0 = log(pm / (1 - pm)) / k + xm;
		
		float4 base;
		float4 sceneDepthC;
		float4 cgDepthC;
		int binTotal = 0;
	
		base = _MainTex.Sample(sampler_MainTex, input.tex0);
		float4 centerCgDepth = _CgDepthTex.Sample(sampler_MainTex, input.tex0);
		float4 centerSceneDepth = _SpatialMapTex.Sample(sampler_MainTex, input.tex0);
		fixed2 wOffset = fixed2(0.18994f, 0.36f);
		//float xoffset = 0.18994f;
		//float yoffset = 0.36f;
		fixed2 wScale = fixed2(0.62012f, 0.6246f);
		//float xscale = 0.62012f;
		//float yscale = 0.6246f;
		float4 webcamCenter = _WebcamTex.Sample(sampler_MainTex, wScale *(input.tex0 + wOffset));
		float4 webcamC;
		float4 webcamTotal = float4(0.0f, 0.0f, 0.0f, 0.0f);
		

		int maxBin = (2 * windowSize + 1)*(2 * windowSize + 1);

						//if (centerSceneDepth.x > centerCgDepth.x) {
		if (centerCgDepth.x != 0.0f) {
			for (int j = -windowSize; j <= windowSize; j++) {
				for (int i = -windowSize; i <= windowSize; i++) {
					sceneDepthC = _SpatialMapTex.Sample(sampler_MainTex, input.tex0 +
						fixed2(_SpatialMapTex_TexelSize.x * i * windowScale, _SpatialMapTex_TexelSize.y * j * windowScale));
					cgDepthC = _CgDepthTex.Sample(sampler_MainTex, input.tex0 +
						fixed2(_CgDepthTex_TexelSize.x * i * windowScale, _CgDepthTex_TexelSize.y * j * windowScale));
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

		float webcamGrayTotal = 0.0f;
		int webcamWindowSize = 2;
		int maxBinWebcam = (2 * webcamWindowSize + 1)*(2 * webcamWindowSize + 1);
		int webcamWindowScale = 1;

		for (int j = -webcamWindowSize; j <= webcamWindowSize; j++) {
			for (int i = -webcamWindowSize; i <= webcamWindowSize; i++) {
				webcamC = _WebcamTex.Sample(sampler_WebcamTex, wScale * (input.tex0 + wOffset) +
					wScale * fixed2(_WebcamTex_TexelSize.x * i * webcamWindowScale, _WebcamTex_TexelSize.y * j * webcamWindowScale));
				//webcamTotal += webcamC;
				webcamGrayTotal += (webcamC.x + webcamC.y + webcamC.z) / 3.0f;
			}
		}

		webcamGrayTotal = webcamGrayTotal / (float)maxBinWebcam;
		float maxValue = 1.0f;
		float minValue = _VisibilityComplex * webcamGrayTotal;
		float L = maxValue - minValue;
		//float weight = _VisibilityComplex + (L / (1.0f + exp(-0.2986f*((float)binTotal - 20.3578f))));
		//float weight = _VisibilityComplex + (L / (1.0f + exp(-k*((float)binTotal - x0))));
		float weight = minValue + (L / (1.0f + exp(-k * ((float)binTotal - x0))));

		//float weight = (1 / (1 + exp(-0.3609f*((float)binTotal - 16.8413f))));

		/*float x = (sceneDepth - cgDepth);
		float x0 = -log((1 - _Beta) / _Beta) / _Slope;
		float f = 1 - 1 / (1 + exp(-_Slope*(x - x0)));*/

		float4 output = base;
		float maxer;

		// TODO: Smoothing function comparison

		output = (1.0f - weight) * output;
		//output = ((1.0f - weight) + weight * _VisibilityComplex) * output;
		//if (cgDepth <= sceneDepth) { //CG is BG
		//	output = _VisibilitySimple * output;
		//}
		//return float4(sceneDepth.x, cgDepth.x, 0.0f, 1.0f);
		//return float4(x, x, 0.0f, 1.0f);
		return output;
		//return webcamTotal/(float)maxBin;
		//return float4(webcamGrayTotal, webcamGrayTotal, webcamGrayTotal, 1.0f);
		}
		ENDCG
	}
	}
}
