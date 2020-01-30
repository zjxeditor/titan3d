#ifndef __VARBASE_PERVIEWPORT_SHADERINC__
#define __VARBASE_PERVIEWPORT_SHADERINC__
#include "../Inc/LightCommon.cginc"
#include "../Inc/Math.cginc"

cbuffer cbPerViewport : register( b1 )
{
	// ShaderParamAnalyse Start
	
	float2 ViewportPos; // leftup position

	float mDirLightSpecularIntensity;
	float4 mDirLightingAmbient;
	float4 mDirLightingDiffuse;
	float4 mDirLightingSpecular;
	float mDirLightShadingSSS;

	float FogStart;// = 30;
	float FogHorizontalRange;// = 170;
	float FogCeil;// = 70;
	float FogVerticalRange;// = 150;
	float FogDensity;// = 1.57;

	float3 mSkyLightColor;
	float3 mGroundLightColor;

	float4 gDirLightColor_Intensity;
	float4 gDirLightDirection_Leak;
	
	float4 gViewportSizeAndRcp;
	
	float2 gDepthBiasAndZFarRcp;
	float2 gFadeParam;
	float4 gShadowMapSizeAndRcp;
	matrix gViewer2ShadowMtx[1];
	matrix gViewer2ShadowMtxArrayEditor[4];
	float4 gCsmDistanceArray;
	float4 gShadowTransitionScaleArrayEditor;
	float gShadowTransitionScale;
	float gShadowDistance;
	int gCsmNum;
	float gEnvMapMaxMipLevel;
	float gEyeEnvMapMaxMipLevel;

	float4 gSunPosNDC;
	float4 gAoParam;//radius_platform_bias_dark;

	// ShaderParamAnalyse End

	float4 PointLightPos_RadiusInv[MaxPointLightNumber];
	float4 PointLightColor_Intensity[MaxPointLightNumber];
	
	#ifndef UserDef_Viewport
		#define UserDef_Viewport
	#endif

	UserDef_Viewport

};

#endif