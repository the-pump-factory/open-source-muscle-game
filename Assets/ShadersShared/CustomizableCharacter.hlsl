#include "InstancedMorphsAndSkinning.hlsl"

#ifdef CUSTOM_SKINNING
#ifdef SHADERGRAPH_PREVIEW

void Skin_float(int shapesID, int rigID, int characterID, int vertexIndex, int vertexCount, float2 mainUV, float bustFactor, float bustNerfFactor, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent, out float3 muscleData, out float3 fatData, out float alpha, out float midlineWeight)
{

	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;

	muscleData = float3(0,0,0);
	fatData = float3(0,0,0);

	alpha = 1;
	midlineWeight = 0;

}

void SkinNoTang_float(int shapesID, int rigID, int characterID, int vertexIndex, int vertexCount, float2 mainUV, float bustFactor, float bustNerfFactor, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal, out float3 muscleData, out float3 fatData, out float alpha, out float midlineWeight)
{
	 
	outPosition = inPosition; 
	outNormal = inNormal;
	
	muscleData = float3(0,0,0);
	fatData = float3(0,0,0);

	alpha = 1; 
	midlineWeight = 0;
}

void SkinBreasts_float(int shapesID, int rigID, int characterID, int vertexIndex, int vertexCount, float2 mainUV, float bustMix, bool hideNipples, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent, out float3 muscleData, out float3 fatData, out float bustFactor, out float bustNerfFactor, out float alpha, out float midlineWeight)
{

	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;

	muscleData = float3(0,0,0);
	fatData = float3(0,0,0);
	bustFactor = 0;
	bustNerfFactor = 0; 

	alpha = 1;
	midlineWeight = 0;

}

void SkinBreastsNoTang_float(int shapesID, int rigID, int characterID, int vertexIndex, int vertexCount, float2 mainUV, float bustMix, bool hideNipples, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal, out float3 muscleData, out float3 fatData, out float bustFactor, out float bustNerfFactor, out float alpha, out float midlineWeight)
{
	 
	outPosition = inPosition; 
	outNormal = inNormal;
	
	muscleData = float3(0,0,0);
	fatData = float3(0,0,0);
	bustFactor = 0;
	bustNerfFactor = 0;

	alpha = 1; 
	midlineWeight = 0;
}


void SkinTorso_float(int shapesID, int rigID, int characterID, int vertexIndex, int vertexCount, float2 mainUV, float bustMix, bool hideNipples, bool hideGenitals, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent, out float3 muscleData, out float3 fatData, out float bustFactor, out float bustNerfFactor, out float alpha, out float midlineWeight)
{

	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;

	muscleData = float3(0,0,0);
	fatData = float3(0,0,0);
	bustFactor = 0;
	bustNerfFactor = 0;

	alpha = 1;
	midlineWeight = 0;

}

void SkinTorsoNoTang_float(int shapesID, int rigID, int characterID, int vertexIndex, int vertexCount, float2 mainUV, float bustMix, bool hideNipples, bool hideGenitals, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal, out float3 muscleData, out float3 fatData, out float bustFactor, out float bustNerfFactor, out float alpha, out float midlineWeight)
{
	 
	outPosition = inPosition; 
	outNormal = inNormal;
	
	muscleData = float3(0,0,0);
	fatData = float3(0,0,0);
	bustFactor = 0;
	bustNerfFactor = 0;

	alpha = 1; 
	midlineWeight = 0;

}

void SampleVertexGroup_float(int groupIndex, int vertexIndex, int vertexCount, out float weight)
{
	weight = 0;
}
void SampleStandaloneVertexGroup_float(int groupIndex, int vertexIndex, int vertexCount, out float weight)
{
	weight = 0;
}
void SampleMuscleGroup_float(int muscleGroupIndex, int vertexIndex, int vertexCount, out float weight)
{
	weight = 0;
}
void SampleFatGroup_float(int fatGroupIndex, int vertexIndex, int vertexCount, out float weight)
{
	weight = 0;
}
void SampleVariationGroup_float(int variationGroupIndex, int vertexIndex, int vertexCount, out float weight)
{
	weight = 0;
}
void SampleMidlineVertexGroup_float(int vertexIndex, int vertexCount, out float weight)
{
	weight = 0;
}

void SampleCustomizationGroups_float(int vertexIndex, int vertexCount, float2 mainUV, float midlineWeight, 
	out float faceLeft, out float faceRight, 
	out float headLeft, out float headRight, 
	out float neckLeft, out float neckRight, 
	out float chestLeft, out float chestRight,
	out float backLeft, out float backRight,
	out float torsoLeft, out float torsoRight,
	out float armsLeft, out float armsRight,
	out float handsLeft, out float handsRight,
	out float legsLeft, out float legsRight,
	out float feetLeft, out float feetRight) 
{
	faceLeft = 0; faceRight = 0;
	headLeft = 0; headRight = 0;
	neckLeft = 0; neckRight = 0;
	chestLeft = 0; chestRight = 0;
	backLeft = 0; backRight = 0;
	torsoLeft = 0; torsoRight = 0;
	armsLeft = 0; armsRight = 0;
	handsLeft = 0; handsRight = 0;
	legsLeft = 0; legsRight = 0;
	feetLeft = 0; feetRight = 0;
}

#endif

#ifndef SHADERGRAPH_PREVIEW

struct MuscleData
{

	float3 valuesLeft;
	float3 valuesRight;

};

uniform int _MidlineVertexGroupIndex;
uniform int _BustVertexGroupIndex;
uniform int _BustNerfVertexGroupIndex;
uniform int _NippleMaskVertexGroupIndex;
uniform int _GenitalMaskVertexGroupIndex;

uniform int _VariationGroupCount;

uniform int _FatMuscleBlendShapeIndex;
uniform float2 _FatMuscleBlendWeightRange;

uniform float _DefaultShapeMuscleWeight;

uniform int2 _RangeVertexGroups;
uniform int2 _RangeMuscleGroups;
uniform int2 _RangeFatGroups;

uniform int2 _RangeMuscleShapes;
uniform int2 _RangeFlexShapes;
uniform int2 _RangeFatShapes;
uniform int2 _RangeVariationShapes;

#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL) || defined(SHADER_API_PSSL)

StructuredBuffer<float> _VertexGroups;

StructuredBuffer<float> _FrameWeightsMuscleShapes;
StructuredBuffer<float> _FrameWeightsFlexShapes;
StructuredBuffer<float> _FrameWeightsFatShapes;

StructuredBuffer<MuscleData> _ControlMuscleGroups;
StructuredBuffer<float3> _ControlFatGroups;

StructuredBuffer<float2> _ControlVariationShapes;
StructuredBuffer<int> _VariationGroups;

#else

StructuredBuffer<float> _VertexGroups;

StructuredBuffer<float> _FrameWeightsMuscleShapes;
StructuredBuffer<float> _FrameWeightsFlexShapes;
StructuredBuffer<float> _FrameWeightsFatShapes;

StructuredBuffer<MuscleData> _ControlMuscleGroups;
StructuredBuffer<float3> _ControlFatGroups;

StructuredBuffer<float2> _ControlVariationShapes;
StructuredBuffer<int> _VariationGroups;

#endif

void SampleVertexGroup_float(int groupIndex, int vertexIndex, int vertexCount, out float weight)
{
	const int groupIndexOffset = groupIndex * vertexCount;
	
	weight = _VertexGroups[groupIndexOffset + vertexIndex];
}
void SampleStandaloneVertexGroup_float(int groupIndex, int vertexIndex, int vertexCount, out float weight)
{
	weight = _VertexGroups[((groupIndex + _RangeVertexGroups.x) * vertexCount) + vertexIndex];
}
void SampleMuscleGroup_float(int muscleGroupIndex, int vertexIndex, int vertexCount, out float weight)
{
	const int vertexGroupIndexOffset = _RangeMuscleGroups.x * vertexCount;

	const int groupIndexOffset = muscleGroupIndex * vertexCount;
	const int groupBufferIndexStart = vertexGroupIndexOffset + groupIndexOffset;
	
	weight = _VertexGroups[groupBufferIndexStart + vertexIndex];
}
void SampleFatGroup_float(int fatGroupIndex, int vertexIndex, int vertexCount, out float weight)
{
	const int vertexGroupIndexOffset = _RangeFatGroups.x * vertexCount;

	const int groupIndexOffset = fatGroupIndex * vertexCount;
	const int groupBufferIndexStart = vertexGroupIndexOffset + groupIndexOffset;
	
	weight = _VertexGroups[groupBufferIndexStart + vertexIndex];
}
void SampleVariationGroup_float(int variationGroupIndex, int vertexIndex, int vertexCount, out float weight)
{
	const int groupIndex = _VariationGroups[variationGroupIndex];

	weight = _VertexGroups[(groupIndex * vertexCount) + vertexIndex];
}
void SampleMidlineVertexGroup_float(int vertexIndex, int vertexCount, out float weight)
{
	SampleStandaloneVertexGroup_float(_MidlineVertexGroupIndex, vertexIndex, vertexCount, weight);
}

uniform int _FaceVertexGroupIndex;
uniform int _HeadVertexGroupIndex;
uniform int _NeckVertexGroupIndex;
uniform int _ChestVertexGroupIndex;
uniform int _BackVertexGroupIndex;
uniform int _TorsoVertexGroupIndex;
uniform int _ArmsVertexGroupIndex;
uniform int _HandsVertexGroupIndex;
uniform int _LegsVertexGroupIndex;
uniform int _FeetVertexGroupIndex;

void SampleCustomizationGroups_float(int vertexIndex, int vertexCount, float2 mainUV, float midlineWeight, 
	out float faceLeft, out float faceRight, 
	out float headLeft, out float headRight, 
	out float neckLeft, out float neckRight, 
	out float chestLeft, out float chestRight,
	out float backLeft, out float backRight,
	out float torsoLeft, out float torsoRight,
	out float armsLeft, out float armsRight,
	out float handsLeft, out float handsRight,
	out float legsLeft, out float legsRight,
	out float feetLeft, out float feetRight) 
{
	float maskLeft = 1.0;
	float maskRight = 1.0;
	if (mainUV.x > 0.5) 
	{
		maskLeft = 1.0;
		maskRight = midlineWeight;
	} 
	else if (mainUV.x < 0.5) 
	{		
		maskLeft = midlineWeight;
		maskRight = 1.0;
	}

	float maskTotal = maskLeft + maskRight;
	if (maskTotal > 1.0) 
	{
		maskLeft = maskLeft / maskTotal;
		maskRight = maskRight / maskTotal;
	}

	SampleVertexGroup_float(_FaceVertexGroupIndex, vertexIndex, vertexCount, faceLeft);
	faceRight = faceLeft * maskRight;
	faceLeft = faceLeft * maskLeft;

	SampleVertexGroup_float(_HeadVertexGroupIndex, vertexIndex, vertexCount, headLeft);
	headRight = headLeft * maskRight;
	headLeft = headLeft * maskLeft;

	SampleVertexGroup_float(_NeckVertexGroupIndex, vertexIndex, vertexCount, neckLeft);
	neckRight = neckLeft * maskRight;
	neckLeft = neckLeft * maskLeft;

	SampleVertexGroup_float(_ChestVertexGroupIndex, vertexIndex, vertexCount, chestLeft);
	chestRight = chestLeft * maskRight;
	chestLeft = chestLeft * maskLeft;

	SampleVertexGroup_float(_BackVertexGroupIndex, vertexIndex, vertexCount, backLeft);
	backRight = backLeft * maskRight;
	backLeft = backLeft * maskLeft;

	SampleVertexGroup_float(_TorsoVertexGroupIndex, vertexIndex, vertexCount, torsoLeft);
	torsoRight = torsoLeft * maskRight;
	torsoLeft = torsoLeft * maskLeft;

	SampleVertexGroup_float(_ArmsVertexGroupIndex, vertexIndex, vertexCount, armsLeft);
	armsRight = armsLeft * maskRight;
	armsLeft = armsLeft * maskLeft;

	SampleVertexGroup_float(_HandsVertexGroupIndex, vertexIndex, vertexCount, handsLeft);
	handsRight = handsLeft * maskRight;
	handsLeft = handsLeft * maskLeft;

	SampleVertexGroup_float(_LegsVertexGroupIndex, vertexIndex, vertexCount, legsLeft);
	legsRight = legsLeft * maskRight;
	legsLeft = legsLeft * maskLeft;

	SampleVertexGroup_float(_FeetVertexGroupIndex, vertexIndex, vertexCount, feetLeft);
	feetRight = feetLeft * maskRight;
	feetLeft = feetLeft * maskLeft;
}

void CalculateMuscleData_float(int instanceID, int vertexIndex, int vertexCount, float maskLeft, float maskRight, out float3 muscleData)
{

	const int muscleGroupCount = max(0, (_RangeMuscleGroups.y - _RangeMuscleGroups.x) + 1);
	const int vertexGroupIndexOffset = _RangeMuscleGroups.x * vertexCount;
	const int controlIndexOffset = instanceID * muscleGroupCount;

	muscleData = float3(0, 0, 0);
	for(int muscleGroupIndex = 0; muscleGroupIndex < muscleGroupCount; muscleGroupIndex++) 
	{
		int muscleGroupIndexOffset = muscleGroupIndex * vertexCount;
		int muscleGroupBufferIndexStart = vertexGroupIndexOffset + muscleGroupIndexOffset;
		float weight = _VertexGroups[muscleGroupBufferIndexStart + vertexIndex];

		MuscleData muscleVals = _ControlMuscleGroups[controlIndexOffset + muscleGroupIndex];

		muscleData = muscleData + (((muscleVals.valuesLeft * maskLeft) + (muscleVals.valuesRight * maskRight)) * weight);
	}

	muscleData.y = muscleData.y * saturate(muscleData.x / 0.35);
}
void ApplyMuscleShapes_float(float3 muscleData, float3 fatData, float bustFactor, int vertexIndex, int vertexCount, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{

	const int muscleShapeCount = max(0, (_RangeMuscleShapes.y - _RangeMuscleShapes.x) + 1);
	const int flexShapeCount = max(0, (_RangeFlexShapes.y - _RangeFlexShapes.x) + 1);
	
	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;

	float fatSat = saturate(fatData.x);
	float bustNerf = 1 - saturate(bustFactor) * 0.45;
	float fatNerf = lerp(1, fatData.y, fatSat);
	float nerf = bustNerf * fatNerf;
	float massWeight = max(muscleData.x, _DefaultShapeMuscleWeight * fatSat);
	ApplyMultiShape_float(_FrameWeightsMuscleShapes, muscleShapeCount, _RangeMuscleShapes.x, massWeight, nerf, vertexIndex, vertexCount, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);
	ApplyMultiShape_float(_FrameWeightsFlexShapes, flexShapeCount, _RangeFlexShapes.x, muscleData.y, nerf, vertexIndex, vertexCount, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);

}
void ApplyMuscleShapesNoTang_float(float3 muscleData, float3 fatData, float bustFactor, int vertexIndex, int vertexCount, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal)
{

	const int muscleShapeCount = max(0, (_RangeMuscleShapes.y - _RangeMuscleShapes.x) + 1);
	const int flexShapeCount = max(0, (_RangeFlexShapes.y - _RangeFlexShapes.x) + 1);
	
	outPosition = inPosition;
	outNormal = inNormal;

	float bustNerf = 1 - saturate(bustFactor) * 0.45;
	float fatNerf = lerp(1, fatData.y, saturate(fatData.x)); 
	float nerf = bustNerf * fatNerf;
	ApplyMultiShapeNoTang_float(_FrameWeightsMuscleShapes, muscleShapeCount, _RangeMuscleShapes.x, muscleData.x, nerf, vertexIndex, vertexCount, outPosition, outNormal, outPosition, outNormal);
	ApplyMultiShapeNoTang_float(_FrameWeightsFlexShapes, flexShapeCount, _RangeFlexShapes.x, muscleData.y, nerf, vertexIndex, vertexCount, outPosition, outNormal, outPosition, outNormal);

}


void CalculateFatData_float(int instanceID, int vertexIndex, int vertexCount, out float3 fatData)
{

	const int fatGroupCount = max(0, (_RangeFatGroups.y - _RangeFatGroups.x) + 1);
	const int vertexGroupIndexOffset = _RangeFatGroups.x * vertexCount;
	const int controlIndexOffset = instanceID * fatGroupCount;

	fatData = float3(0,0,0);
	for(int fatGroupIndex = 0; fatGroupIndex < fatGroupCount; fatGroupIndex++) 
	{
		int fatGroupIndexOffset = fatGroupIndex * vertexCount;
		int fatGroupBufferIndexStart = vertexGroupIndexOffset + fatGroupIndexOffset;
		float weight = _VertexGroups[fatGroupBufferIndexStart + vertexIndex];

		fatData = fatData + _ControlFatGroups[controlIndexOffset + fatGroupIndex] * weight; 
	}

}
void ApplyFatShapes_float(float3 fatData, float3 muscleData, float bustFactor, int vertexIndex, int vertexCount, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{

	const int fatShapeCount = max(0, (_RangeFatShapes.y - _RangeFatShapes.x) + 1);
	
	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;

	float bustNerf = 1 - saturate(bustFactor) * 0.6;
	ApplyMultiShape_float(_FrameWeightsFatShapes, fatShapeCount, _RangeFatShapes.x, fatData.x, bustNerf, vertexIndex, vertexCount, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);
	
	float fatMuscleBlend = (max(0, (fatData.x - _FatMuscleBlendWeightRange.x) / (_FatMuscleBlendWeightRange.y - _FatMuscleBlendWeightRange.x)) * saturate(muscleData.x) * saturate(fatData.y)) * saturate(1 - bustFactor);  
	ApplyShapeDelta_float((_FatMuscleBlendShapeIndex * vertexCount) + vertexIndex, fatMuscleBlend, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);
}
void ApplyFatShapesNoTang_float(float3 fatData, float3 muscleData, float bustFactor, int vertexIndex, int vertexCount, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal)
{

	const int fatShapeCount = max(0, (_RangeFatShapes.y - _RangeFatShapes.x) + 1);
	
	outPosition = inPosition;
	outNormal = inNormal;

	float bustNerf = 1 - saturate(bustFactor) * 0.6;
	ApplyMultiShapeNoTang_float(_FrameWeightsFatShapes, fatShapeCount, _RangeFatShapes.x, fatData.x, bustNerf, vertexIndex, vertexCount, outPosition, outNormal, outPosition, outNormal);

	float fatMuscleBlend = (max(0, (fatData.x - _FatMuscleBlendWeightRange.x) / (_FatMuscleBlendWeightRange.y - _FatMuscleBlendWeightRange.x)) * saturate(muscleData.x) * saturate(fatData.y)) * saturate(1 - bustFactor);
	ApplyShapeDeltaNoTang_float((_FatMuscleBlendShapeIndex * vertexCount) + vertexIndex, fatMuscleBlend, outPosition, outNormal, outPosition, outNormal);
}


void ApplyVariationShapes_float(int instanceID, int vertexIndex, int vertexCount, float maskLeft, float maskRight, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{

	const int variationShapeCount = (_RangeVariationShapes.y - _RangeVariationShapes.x) + 1;
	const int controlCount = variationShapeCount * _VariationGroupCount;
	const int controlIndexOffset = instanceID * controlCount;

	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;

	for(int variationGroupIndex = 0; variationGroupIndex < controlCount; variationGroupIndex++)
	{

		float2 controlWeightLR = _ControlVariationShapes[controlIndexOffset + variationGroupIndex]; 

		int groupIndex = _VariationGroups[variationGroupIndex / variationShapeCount];
		float weight = _VertexGroups[(groupIndex * vertexCount) + vertexIndex];

		int shapeIndex = _RangeVariationShapes.x + (variationGroupIndex % variationShapeCount); 
		int shapeIndexInBuffer = (shapeIndex * vertexCount) + vertexIndex; 

		ApplyShapeDelta_float(shapeIndexInBuffer, controlWeightLR.x * maskLeft * weight, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);
		ApplyShapeDelta_float(shapeIndexInBuffer, controlWeightLR.y * maskRight * weight, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);

	}

}
void ApplyVariationShapesNoTang_float(int instanceID, int vertexIndex, int vertexCount, float maskLeft, float maskRight, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal)
{

	const int variationShapeCount = (_RangeVariationShapes.y - _RangeVariationShapes.x) + 1;
	const int controlCount = variationShapeCount * _VariationGroupCount;
	const int controlIndexOffset = instanceID * controlCount;
	 
	outPosition = inPosition;
	outNormal = inNormal;

	for(int variationGroupIndex = 0; variationGroupIndex < controlCount; variationGroupIndex++)
	{

		float2 controlWeightLR = _ControlVariationShapes[controlIndexOffset + variationGroupIndex];

		int groupIndex = _VariationGroups[variationGroupIndex / variationShapeCount];
		float weight = _VertexGroups[(groupIndex * vertexCount) + vertexIndex];

		int shapeIndex = _RangeVariationShapes.x + (variationGroupIndex % variationShapeCount);
		int shapeIndexInBuffer = (shapeIndex * vertexCount) + vertexIndex;

		ApplyShapeDeltaNoTang_float(shapeIndexInBuffer, controlWeightLR.x * maskLeft * weight, outPosition, outNormal, outPosition, outNormal);
		ApplyShapeDeltaNoTang_float(shapeIndexInBuffer, controlWeightLR.y * maskRight * weight, outPosition, outNormal, outPosition, outNormal);

	}

}


void Skin_float(int shapesID, int rigID, int characterID, int vertexIndex, int vertexCount, float2 mainUV, float bustFactor, float bustNerfFactor, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent, out float3 muscleData, out float3 fatData, out float alpha, out float midlineWeight)
{

	alpha = 1;

	midlineWeight = 0;
	SampleMidlineVertexGroup_float(vertexIndex, vertexCount, midlineWeight);
	float maskLeft = 1;
	float maskRight = 1;
	if (mainUV.x > 0.5) 
	{
		maskLeft = 1;
		maskRight = midlineWeight;
	} 
	else if (mainUV.x < 0.5) 
	{		
		maskLeft = midlineWeight;
		maskRight = 1;
	}
	float maskTotal = maskLeft + maskRight; 
	if (maskTotal > 1.0) 
	{
		maskLeft = maskLeft / maskTotal;
		maskRight = maskRight / maskTotal;
	}

	CalculateMuscleData_float(characterID, vertexIndex, vertexCount, maskLeft, maskRight, muscleData);

	CalculateFatData_float(characterID, vertexIndex, vertexCount, fatData);

	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;

	ApplyVariationShapes_float(characterID, vertexIndex, vertexCount, maskLeft, maskRight, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);

	ApplyStandaloneShapes_float(shapesID, vertexIndex, vertexCount, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);

	ApplyMuscleShapes_float(muscleData, fatData, bustNerfFactor, vertexIndex, vertexCount, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);

	ApplyFatShapes_float(fatData, muscleData, bustNerfFactor, vertexIndex, vertexCount, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);

	outNormal = normalize(outNormal);
	outTangent = normalize(outTangent);

	SkinNoShapes_float(rigID, vertexIndex, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);

}

void SkinNoTang_float(int shapesID, int rigID, int characterID, int vertexIndex, int vertexCount, float2 mainUV, float bustFactor, float bustNerfFactor, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal, out float3 muscleData, out float3 fatData, out float alpha, out float midlineWeight)
{

	alpha = 1;

	midlineWeight = 0;
	SampleMidlineVertexGroup_float(vertexIndex, vertexCount, midlineWeight);
	float maskLeft = 1;
	float maskRight = 1;
	if (mainUV.x > 0.5) 
	{
		maskLeft = 1;
		maskRight = midlineWeight;
	} 
	else if (mainUV.x < 0.5) 
	{		
		maskLeft = midlineWeight;
		maskRight = 1;
	}
	float maskTotal = maskLeft + maskRight;
	if (maskTotal > 1.0) 
	{
		maskLeft = maskLeft / maskTotal;
		maskRight = maskRight / maskTotal;
	}

	CalculateMuscleData_float(characterID, vertexIndex, vertexCount, maskLeft, maskRight, muscleData);

	CalculateFatData_float(characterID, vertexIndex, vertexCount, fatData);

	outPosition = inPosition;
	outNormal = inNormal;

	ApplyVariationShapesNoTang_float(characterID, vertexIndex, vertexCount, maskLeft, maskRight, outPosition, outNormal, outPosition, outNormal);

	ApplyStandaloneShapesNoTang_float(shapesID, vertexIndex, vertexCount, outPosition, outNormal, outPosition, outNormal);

	ApplyMuscleShapesNoTang_float(muscleData, fatData, bustNerfFactor, vertexIndex, vertexCount, outPosition, outNormal, outPosition, outNormal);

	ApplyFatShapesNoTang_float(fatData, muscleData, bustNerfFactor, vertexIndex, vertexCount, outPosition, outNormal, outPosition, outNormal);

	outNormal = normalize(outNormal);

	SkinNoShapesNoTang_float(rigID, vertexIndex, outPosition, outNormal, outPosition, outNormal);

}


void SkinBreasts_float(int shapesID, int rigID, int characterID, int vertexIndex, int vertexCount, float2 mainUV, float bustMix, bool hideNipples, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent, out float3 muscleData, out float3 fatData, out float bustFactor, out float bustNerfFactor, out float alpha, out float midlineWeight)
{

	bustFactor = bustMix * min(1, pow(_VertexGroups[((_BustVertexGroupIndex + _RangeVertexGroups.x) * vertexCount) + vertexIndex], 0.2)); 
	bustNerfFactor = bustMix * min(1, pow(_VertexGroups[((_BustNerfVertexGroupIndex + _RangeVertexGroups.x) * vertexCount) + vertexIndex], 0.5));

	Skin_float(shapesID, rigID, characterID, vertexIndex, vertexCount, mainUV, bustFactor, bustNerfFactor, inPosition, inNormal, inTangent, outPosition, outNormal, outTangent, muscleData, fatData, alpha, midlineWeight);

	float nippleWeight = _VertexGroups[((_NippleMaskVertexGroupIndex + _RangeVertexGroups.x) * vertexCount) + vertexIndex];
	//nippleWeight = hideNipples ? (1 - nippleWeight) : nippleWeight;
	//alpha = (nippleWeight > 0 ? 1 : 0) * alpha;
	alpha = (nippleWeight > 0 ? (hideNipples ? 1 : 0) : 1) * alpha; 

}

void SkinBreastsNoTang_float(int shapesID, int rigID, int characterID, int vertexIndex, int vertexCount, float2 mainUV, float bustMix, bool hideNipples, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal, out float3 muscleData, out float3 fatData, out float bustFactor, out float bustNerfFactor, out float alpha, out float midlineWeight)
{

	bustFactor = bustMix * min(1, pow(_VertexGroups[((_BustVertexGroupIndex + _RangeVertexGroups.x) * vertexCount) + vertexIndex], 0.2)); 
	bustNerfFactor = bustMix * min(1, pow(_VertexGroups[((_BustNerfVertexGroupIndex + _RangeVertexGroups.x) * vertexCount) + vertexIndex], 0.5));

	SkinNoTang_float(shapesID, rigID, characterID, vertexIndex, vertexCount, mainUV, bustFactor, bustNerfFactor, inPosition, inNormal, outPosition, outNormal, muscleData, fatData, alpha, midlineWeight);

	float nippleWeight = _VertexGroups[((_NippleMaskVertexGroupIndex + _RangeVertexGroups.x) * vertexCount) + vertexIndex];
	//nippleWeight = hideNipples ? (1 - nippleWeight) : nippleWeight;
	//alpha = (nippleWeight > 0 ? 1 : 0) * alpha;
	alpha = (nippleWeight > 0 ? (hideNipples ? 1 : 0) : 1) * alpha; 

}


void SkinTorso_float(int shapesID, int rigID, int characterID, int vertexIndex, int vertexCount, float2 mainUV, float bustMix, bool hideNipples, bool hideGenitals, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent, out float3 muscleData, out float3 fatData, out float bustFactor, out float bustNerfFactor, out float alpha, out float midlineWeight)
{

	SkinBreasts_float(shapesID, rigID, characterID, vertexIndex, vertexCount, mainUV, bustMix, hideNipples, inPosition, inNormal, inTangent, outPosition, outNormal, outTangent, muscleData, fatData, bustFactor, bustNerfFactor, alpha, midlineWeight);

	float genitalWeight = _VertexGroups[((_GenitalMaskVertexGroupIndex + _RangeVertexGroups.x) * vertexCount) + vertexIndex];
	alpha = (genitalWeight > 0 ? (hideGenitals ? 1 : 0) : 1) * alpha;  

}

void SkinTorsoNoTang_float(int shapesID, int rigID, int characterID, int vertexIndex, int vertexCount, float2 mainUV, float bustMix, bool hideNipples, bool hideGenitals, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal, out float3 muscleData, out float3 fatData, out float bustFactor, out float bustNerfFactor, out float alpha, out float midlineWeight)
{

	SkinBreastsNoTang_float(shapesID, rigID, characterID, vertexIndex, vertexCount, mainUV, bustMix, hideNipples, inPosition, inNormal, outPosition, outNormal, muscleData, fatData, bustFactor, bustNerfFactor, alpha, midlineWeight);

	float genitalWeight = _VertexGroups[((_GenitalMaskVertexGroupIndex + _RangeVertexGroups.x) * vertexCount) + vertexIndex]; 
	alpha = (genitalWeight > 0 ? (hideGenitals ? 1 : 0) : 1) * alpha;

}

#endif
#endif