#include "AnimeShading.hlsl"

struct muscleVertInput
{

	float4 vertex : POSITION;
	float4 color : COLOR;

	float4 normal : NORMAL;

	float4 uv1 : TEXCOORD0;
	float4 uv2 : TEXCOORD1;
	float4 uv3 : TEXCOORD2;
	float2 uv4 : TEXCOORD3;
	float2 uv5 : TEXCOORD4;
	float2 uv6 : TEXCOORD5;
	float2 uv7 : TEXCOORD6;

};

struct muscleFragInput
{

	float4 vertex : SV_POSITION;
	float4 color : COLOR;

	float3 normal : NORMAL;

	float2 uv1 : TEXCOORD0;
	float2 uv2 : TEXCOORD1;

	float2 linesUV : TEXCOORD2;

	float3 worldPos : TEXCOORD3;

	float4 screenPos : TEXCOORD4;

};

float _MuscleDetailStart;
float _MuscleDefinitionEnd;
float _MuscleStriationEnd;

float4 animeShadingMuscle(muscleFragInput i, float line_strength_multiplier = 1)
{

	float4 base = tex2D(_MainTex, i.uv1);

	clip((i.color.a * base.a) - _ClipThreshold);

	float4 shade = tex2D(_ShadeTex, i.uv1);

	float4 lines = tex2D(_UVLineTex, i.linesUV);


	baseFragInput baseI;

	baseI.vertex = i.vertex;
	baseI.color = i.color;
	baseI.normal = i.normal;
	baseI.uv = i.uv1.xy;
	baseI.uv2 = i.uv2.xy;
	baseI.worldPos = i.worldPos;
	baseI.screenPos = i.screenPos;

	float4 decal = tex2D(_DetailTex, baseI.uv2);

	return animeShadingFragmentBase(baseI, base, shade, lines, float4(0, 0, 0, 0), line_strength_multiplier, decal);

}

muscleFragInput animeShadingMuscleVertex(muscleVertInput v)
{

	baseVertInput temp;

	temp.vertex = v.vertex;
	temp.color = v.color;
	temp.normal = v.normal;
	temp.uv = v.uv1.xy;

	baseFragInput tempO = animeShadingVertex(temp);

	muscleFragInput o;

	o.vertex = tempO.vertex;
	o.color = tempO.color;
	o.normal = tempO.normal;
	o.worldPos = tempO.worldPos;
	o.screenPos = tempO.screenPos;


	float muscleSize = clamp(tempO.color.g, 0, 1.1);
	float muscleFlex = clamp(tempO.color.b, 0, 1.06);

	float2 linesStriatedFlex = v.uv4;
	float2 linesStriated = v.uv5;

	float2 linesDefinedFlex = v.uv6;
	float2 linesDefined = v.uv7;


	float definedMuscle = saturate((muscleSize - _MuscleDetailStart) / (_MuscleDefinitionEnd - _MuscleDetailStart));
	float striatedMuscle = saturate((muscleSize - _MuscleDefinitionEnd) / (_MuscleStriationEnd - _MuscleDefinitionEnd));

	float2 linesUV = lerp(v.uv1.xy, lerp(lerp(linesDefined, linesDefinedFlex, muscleFlex), lerp(linesStriated, linesStriatedFlex, muscleFlex), striatedMuscle), definedMuscle);

	o.uv1 = v.uv1;
	o.uv2 = v.uv2;
	o.linesUV = linesUV;

	return o;

}