#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "ColorManipulation.hlsl"
#include "FloatEncoding.hlsl"
 
sampler2D _MainTex;
sampler2D _ShadeTex;
sampler2D _SpecTex;
sampler2D _UVLineTex;
sampler2D _DetailTex;

sampler2D _DepthOverlapTex;

float4 _Color;
float4 _BaseColor;
float4 _ShadeColor;
float4 _SpecularColor;
float4 _LineColor;
float4 _DetailShadeColor;

float4 _LighterGradient;
float4 _DarkerGradient;
float4 _BackLight;

float3 _CustomLightColor;
float3 _CustomLightDirection;

float _ShadeMix;
float _ShadeThreshold;
float _ClipThreshold;
float _LighterGradientStart;
float _LighterGradientEnd;
float _DarkerGradientStart;
float _DarkerGradientEnd;
float _BackLightStart;
float _BackLightEnd;
float _BackLightIntensity;

float _OutlineThickness;
float _OutlineThicknessMax;

float _OutlineThicknessExp;
float _OutlineThicknessMinDistance;
float _OutlineThicknessMaxDistance;

float _OutlineFadePower;
float _OutlineFadeThreshold;

float _SpecularDot;

float _LightStep;

float _DepthOverlapThreshold;

#define UNITY_PI 3.141592653589793

float fov()  
{

	float t = unity_CameraProjection._m11;

	const float Rad2Deg = 180 / UNITY_PI;

	return atan(1.0f / t) * 2.0 * Rad2Deg;

}

float ObjectDepthLinear(float4 screenPos) 
{

	return 1 - (screenPos.w / _ProjectionParams.z);

}

float4 ScreenPositionScaled(float4 screenPos)
{

	return float4(screenPos.xy / screenPos.w, 0, 0);

}

float CalculateDepthOverlap(float4 screenPos)
{

	float4 screenPosScaled = ScreenPositionScaled(screenPos);

	float4 overlapSample = tex2Dlod(_DepthOverlapTex, float4(screenPosScaled.xy, 0, 0));

	float overlapDepth = DecodeFloatRG(overlapSample.rg);

	return abs(overlapDepth - ObjectDepthLinear(screenPos));

}

struct baseVertInput
{

	float4 vertex : POSITION;
	float4 color : COLOR;

	float4 normal : NORMAL;

	float2 uv : TEXCOORD0;
	float2 uv2 : TEXCOORD1;

};

struct baseFragInput
{

	float4 vertex : SV_POSITION;
	float4 color : COLOR;

	float3 normal : NORMAL;

	float2 uv : TEXCOORD0;
	float2 uv2 : TEXCOORD1;

	float3 worldPos : TEXCOORD2;

	float4 screenPos : TEXCOORD3;

};

baseFragInput animeShadingVertex(baseVertInput v)
{

	baseFragInput o;

	o.vertex = TransformObjectToHClip(v.vertex);

	o.uv = v.uv;
	o.uv2 = v.uv2;

	o.color = v.color;

	o.normal = normalize(TransformObjectToWorldNormal(v.normal));

	o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

	o.screenPos = ComputeScreenPos(o.vertex);
	
	return o;


}

float LDotBase(float3 normal, float3 lightDirection) {

	//lightDirection = floor(lightDirection / _LightStep) * _LightStep;

	return saturate((dot(lightDirection, normal) + 1) / 2.0);

}

float LDotBase(float3 normal) {

	return LDotBase(normal, -normalize(_CustomLightDirection));

}

float LightContribution(float3 normalWS, Light light) {

	return LDotBase(normalWS, light.direction) * light.distanceAttenuation * light.shadowAttenuation;

}

float ModShadeThresholdNoShift(float threshold, float4 vertexColors)
{

	float v_r = (vertexColors.r - 0.5) * -2;

	float sub = min(v_r, 0) * (1 + threshold);

	float add = max(v_r, 0) * (1.5 - threshold);

	return (threshold + add + sub);

}

float ModShadeThreshold(float threshold, float4 vertexColors)
{

	float shadeThreshold_shifted = lerp(-1, 1, threshold);

	return (ModShadeThresholdNoShift(shadeThreshold_shifted, vertexColors) + 1) / 2;

}

float ModShadeThreshold(float4 vertexColors)
{

	return ModShadeThreshold(_ShadeThreshold, vertexColors);

}

void LDotWorld(float4 vcolor, float3 normalWS, float3 positionWS, out float LDot, out float3 LColor)
{

	// To ensure backward compatibility we have to avoid using shadowMask input, as it is not present in older shaders
#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
	half4 shadowMask = inputData.shadowMask;
#elif !defined (LIGHTMAP_ON)
	half4 shadowMask = unity_ProbesOcclusion;
#else
	half4 shadowMask = half4(1, 1, 1, 1);
#endif

	float lightColorWeight = 0;
	float3 lightColor = float3(0, 0, 0);

	//Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS), positionWS, shadowMask);
	Light mainLight = GetMainLight();

	LDot = LightContribution(normalWS, mainLight);

	float moddedThreshold = ModShadeThreshold(vcolor);

	int weight = LDot < moddedThreshold ? 0 : 1;

	lightColorWeight += weight;
	lightColor += mainLight.color.rgb * weight;

//#ifdef _ADDITIONAL_LIGHTS
	uint pixelLightCount = GetAdditionalLightsCount();
	for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
	{
		//Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
		Light light = GetAdditionalLight(lightIndex, positionWS);

		float ldot = LightContribution(normalWS, light);

		LDot = max(LDot, ldot);

		weight = ldot < moddedThreshold ? 0 : 1;

		lightColorWeight += weight;
		lightColor += light.color.rgb * weight;

	}
//#else
//	uint pixelLightCount = 0;
//#endif

	LColor = lightColor;

	float3 hsv = RGBToHSV(LColor);

	LColor = HSVToRGB(float3(hsv.x, hsv.y, lerp(1, hsv.z, 0.1)));

}

float4 animeShadingFragmentSimple(baseFragInput i, float4 base_sample, float4 shade_sample, float4 specular_sample)
{

	float3 lightCol = _CustomLightColor;

	float4 base = base_sample;

	clip((i.color.a * base.a) - _ClipThreshold);

	float3 normal = normalize(i.normal);

	float4 shade = shade_sample;

	float l_dot = 0;

	LDotWorld(i.color, normal, i.worldPos, l_dot, lightCol);

	float moddedThreshold = ModShadeThreshold(i.color);

	bool isShaded = l_dot < moddedThreshold;

	base = isShaded ? (base * (1 - _ShadeMix)) + (shade * _ShadeMix) : (base * float4(lightCol, 1));

	base = (base * (1 - specular_sample.a)) + ((base + float4(specular_sample.rgb, 1)) * specular_sample.a);

	l_dot = l_dot - (moddedThreshold - _ShadeThreshold);

	if (_LighterGradientStart < 1) {

		float range = saturate((l_dot - _LighterGradientStart) / (_LighterGradientEnd - _LighterGradientStart)) * _LighterGradient.a;

		base = base + float4(_LighterGradient.rgb, 1) * range;

	}

	if (_DarkerGradientStart > 0) {

		float range = saturate((l_dot - _DarkerGradientStart) / (_DarkerGradientEnd - _DarkerGradientStart)) * _DarkerGradient.a;

		base = (base * (1 - range)) + (base * float4(_DarkerGradient.rgb, 1) * range);

	}

	float bls = saturate(_BackLightStart);
	float ble = saturate(_BackLightEnd);

	if (bls > 0 && isShaded) {

		float3 camNormal = normalize(_WorldSpaceCameraPos - i.worldPos);

		float camDot = (dot(camNormal, normal) + 1) / 2.0;

		float range = saturate((camDot - bls) / (ble - bls)) * _BackLight.a;

		base = base + float4(_BackLight.rgb, 1) * _BackLightIntensity * range;

	}

	return base * _Color;

}

float4 animeShadingFragmentBase(baseFragInput i, float4 base_sample, float4 shade_sample, float4 lines_sample, float4 specular_sample, float line_strength_multiplier = 1, float4 decal_sample = (1, 1, 1, 0), float alpha_multiplier = 1)
{

	clip((i.color.a * base_sample.a * alpha_multiplier) - _ClipThreshold);

	//float3 shade_offset = shade_sample.rgb - base_sample.rgb;

	//float3 decal_shaded_sample = decal_sample.rgb + shade_offset;
	float3 decal_shaded_sample = decal_sample.rgb * _DetailShadeColor.rgb;

	base_sample = lerp(base_sample, float4(decal_sample.rgb, base_sample.a), decal_sample.a);
	shade_sample = lerp(shade_sample, float4(decal_shaded_sample, shade_sample.a), decal_sample.a);

	float4 base = animeShadingFragmentSimple(i, base_sample, shade_sample, specular_sample);

	float4 lines = lines_sample;

	float camDist = length(_WorldSpaceCameraPos - i.worldPos);

	float line_strength = (lines.a * _LineColor.a) * line_strength_multiplier;

	float fade = _OutlineFadeThreshold * pow(camDist, _OutlineFadePower);

	line_strength = line_strength > fade ? ((line_strength - fade) / (1 - fade)) : 0;

	float4 col = (base * (1 - line_strength)) + (float4(_LineColor.r * lines.r, _LineColor.g * lines.g, _LineColor.b * lines.b, 1) * _Color * line_strength);

	return col;

}

float4 animeShadingFragmentBaseAndLines(baseFragInput i, float4 specular_sample, float line_strength_multiplier = 1, float4 decal_sample = (1, 1, 1, 0), float alpha_multiplier = 1)
{

	float4 base = tex2D(_MainTex, i.uv);

	float4 shade = tex2D(_ShadeTex, i.uv);

	//float4 lines = tex2Dlod(_UVLineTex, float4(i.uv, 0, 0));

	float4 lines = tex2D(_UVLineTex, i.uv);

	//float2 uv_dx = clamp(ddx(i.uv), 0, 2);
	//float2 uv_dy = clamp(ddy(i.uv), 0, 2);
	//float4 lines = tex2D(_UVLineTex, i.uv, uv_dx, uv_dy);

	return animeShadingFragmentBase(i, base, shade, lines, specular_sample, line_strength_multiplier, decal_sample, alpha_multiplier);

}

float4 animeShadingFragment(baseFragInput i, float line_strength_multiplier = 1, float4 decal_sample = (1, 1, 1, 0), float alpha_multiplier = 1)
{

	return animeShadingFragmentBaseAndLines(i, float4(0, 0, 0, 0), line_strength_multiplier, decal_sample, alpha_multiplier);

}

float4 animeShadingFragmentDetail(baseFragInput i, float line_strength_multiplier = 1, float alpha_multiplier = 1)
{

	float4 decal = tex2D(_DetailTex, i.uv2);

	return animeShadingFragment(i, line_strength_multiplier, decal, alpha_multiplier);

}

float4 animeShadingSpecColor(baseFragInput i, float4 spec) {

	float3 viewDir = normalize(i.worldPos - _WorldSpaceCameraPos.xyz);

	//float viewDot = saturate(dot(-viewDir, normalize(i.normal)) - _SpecularDot) / (1 - _SpecularDot);

	float viewDot = saturate(dot(-viewDir, normalize(i.normal)) - _SpecularDot);

	viewDot = viewDot > _SpecularDot ? 1 : 0;

	return float4(spec.rgb, spec.a * viewDot);

}

float4 animeShadingFragmentSpec(baseFragInput i, float line_strength_multiplier = 1, float4 decal_sample = (1, 1, 1, 0), float alpha_multiplier = 1) {

	float4 spec = tex2D(_SpecTex, i.uv);

	return animeShadingFragmentBaseAndLines(i, animeShadingSpecColor(i, spec), line_strength_multiplier, decal_sample, alpha_multiplier);

}

float4 animeShadingFragmentSpecDetail(baseFragInput i, float line_strength_multiplier = 1, float alpha_multiplier = 1) {

	float4 decal = tex2D(_DetailTex, i.uv2);

	return animeShadingFragmentSpec(i, line_strength_multiplier, decal, alpha_multiplier);

}

struct outlineVertInput {

	float4 vertex : POSITION;
	float4 normal : NORMAL;
	float4 color : COLOR;

};

struct outlineFragInput {
	 
	float4 position : SV_POSITION;
	float4 color : COLOR;

	float4 screenPos : TEXCOORD0;

};

outlineVertInput outlineVertex(outlineVertInput v) {

	return v;

}

float4 outlineFragment(outlineFragInput i) 
{

	clip(min(i.color.g <= 0 ? -1 : 1, (i.color.a * _LineColor.a) - _ClipThreshold));

	return _LineColor * _Color;

}

float GetLineThickness(float3 vertexWS, float4 color) 
{

	float camDist = length(GetWorldSpaceViewDir(vertexWS));

	float curve = pow(saturate((camDist - _OutlineThicknessMinDistance) / _OutlineThicknessMaxDistance), _OutlineThicknessExp);

	return lerp(_OutlineThickness, _OutlineThicknessMax, curve) * (fov() / 60.0f) * color.g;

}

void outlineGeometryRecalculate(triangle outlineVertInput IN[3], inout TriangleStream<outlineFragInput> triStream)
{

	float4 p1 = IN[0].vertex;
	float4 p2 = IN[1].vertex;
	float4 p3 = IN[2].vertex;

	float4 p4 = (p1 + p2 + p3) / 3.0;

	float3 n = normalize(cross(p2.xyz - p1.xyz, p3.xyz - p1.xyz));

	for (int a = 0; a < 3; a++) {

		outlineFragInput i;

		float4 vertex = IN[a].vertex;
		float4 color = IN[a].color;

		i.screenPos = ComputeScreenPos(TransformObjectToHClip(vertex));

		float lineThickness = GetLineThickness(mul(unity_ObjectToWorld, vertex).xyz, color);

		i.position = TransformObjectToHClip(vertex + (n * lineThickness) + normalize(vertex - p4).xyz * lineThickness);

		i.color = color;

		if (color.g > 0.01) triStream.Append(i);

	}

	triStream.RestartStrip();

}

void outlineGeometry(triangle outlineVertInput IN[3], inout TriangleStream<outlineFragInput> triStream)
{

	for (int a = 0; a < 3; a++) {

		outlineFragInput i;

		float4 vertex = IN[a].vertex;
		float4 color = IN[a].color;

		i.screenPos = ComputeScreenPos(TransformObjectToHClip(vertex));

		float lineThickness = GetLineThickness(mul(unity_ObjectToWorld, vertex).xyz, color);

		i.position = TransformObjectToHClip(vertex + normalize(IN[a].normal.xyz) * lineThickness);

		i.color = color;

		if (color.g > 0.01) triStream.Append(i);

	}

	triStream.RestartStrip();

}