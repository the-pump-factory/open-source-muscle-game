#ifndef CUSTOM_SKINNING
#define CUSTOM_SKINNING

#ifdef SHADERGRAPH_PREVIEW

void Skin_float(int index, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{

	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;

}

void SkinNoTang_float(int index, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal)
{
	 
	outPosition = inPosition;
	outNormal = inNormal;

}

#endif

#ifndef SHADERGRAPH_PREVIEW

struct SkinInfluence
{ 
	
	float4 indicesA;
	float4 indicesB;

	float4 weightsA;
	float4 weightsB;

};

struct BlendShapeDelta
{

	float4 deltaVertex;
	float4 deltaNormal;
	float4 deltaTangent;

};

uniform int _BlendShapeCount;

CBUFFER_START(BlendShapeHeaderData)
	float4 _BlendShapeWeights[16];
CBUFFER_END

CBUFFER_START(BlendShapeFrameData)
	float _BlendShapeFrameCounts[64];
	float _BlendShapeFrameWeights[512];
CBUFFER_END

#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL) || defined(SHADER_API_PSSL)
StructuredBuffer<SkinInfluence> _SkinBindings;
StructuredBuffer<float4x4> _SkinningMatrices;
StructuredBuffer<BlendShapeDelta> _BlendShapeData;
#else
StructuredBuffer<SkinInfluence> _SkinBindings;
StructuredBuffer<float4x4> _SkinningMatrices;
StructuredBuffer<BlendShapeDelta> _BlendShapeData;
#endif

float UnpackBlendShapeWeight(int shapeIndex) 
{
	
	int subIndex = shapeIndex / 4;
	int compIndex = shapeIndex % 4;

	float4 rawValue = _BlendShapeWeights[subIndex];

	return compIndex == 0 ? rawValue.x : compIndex == 1 ? rawValue.y : compIndex == 2 ? rawValue.z : rawValue.w;

}

void SkinNoShapes_float(int index, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{

SkinInfluence si = _SkinBindings[index];
 
const float4x4 blendedMatrix = 
_SkinningMatrices[int(si.indicesA.x)] * si.weightsA.x 
+ _SkinningMatrices[int(si.indicesA.y)] * si.weightsA.y 
+ _SkinningMatrices[int(si.indicesA.z)] * si.weightsA.z  
+ _SkinningMatrices[int(si.indicesA.w)] * si.weightsA.w 
+ _SkinningMatrices[int(si.indicesB.x)] * si.weightsB.x 
+ _SkinningMatrices[int(si.indicesB.y)] * si.weightsB.y 
+ _SkinningMatrices[int(si.indicesB.z)] * si.weightsB.z 
+ _SkinningMatrices[int(si.indicesB.w)] * si.weightsB.w;

outPosition = mul(blendedMatrix, float4(inPosition, 1)).xyz;
outNormal = mul(blendedMatrix, float4(inNormal, 0)).xyz;
outTangent = mul(blendedMatrix, float4(inTangent, 0)).xyz;

}

void SkinNoShapesNoTang_float(int index, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal)
{

SkinInfluence si = _SkinBindings[index];

const float4x4 blendedMatrix = 
_SkinningMatrices[int(si.indicesA.x)] * si.weightsA.x 
+ _SkinningMatrices[int(si.indicesA.y)] * si.weightsA.y 
+ _SkinningMatrices[int(si.indicesA.z)] * si.weightsA.z  
+ _SkinningMatrices[int(si.indicesA.w)] * si.weightsA.w 
+ _SkinningMatrices[int(si.indicesB.x)] * si.weightsB.x 
+ _SkinningMatrices[int(si.indicesB.y)] * si.weightsB.y 
+ _SkinningMatrices[int(si.indicesB.z)] * si.weightsB.z 
+ _SkinningMatrices[int(si.indicesB.w)] * si.weightsB.w;

outPosition = mul(blendedMatrix, float4(inPosition, 1)).xyz;
outNormal = mul(blendedMatrix, float4(inNormal, 0)).xyz;

}

void CalculateBlendShapePosition(int indexOffset, int frameCount, float weight, out int2 frames, out float interp) 
{

	frames = 0;
	
	int fCm1 = frameCount - 1;

	for (int a = 0; a < frameCount; a++)
	{
		frames = int2(a - 1, min(a, fCm1));
		if (weight <= _BlendShapeFrameWeights[frames.y + indexOffset]) break; 
	
	}
	
	float w1 = (frames.x < 0 ? 0 : _BlendShapeFrameWeights[frames.x + indexOffset]);
	float w2 = _BlendShapeFrameWeights[frames.y + indexOffset];

	interp = (weight - w1) / (w2 - w1);

}

void Skin_float(int index, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{

	BlendShapeDelta defaultShapeData;
	defaultShapeData.deltaVertex = 0;
	defaultShapeData.deltaNormal = 0;
	defaultShapeData.deltaTangent = 0;

	int i = 0;
	for (int a = 0; a < _BlendShapeCount; a++)
	{

		int frameCount = int(_BlendShapeFrameCounts[a]);
		
		int2 frames; 
		float interp;
		CalculateBlendShapePosition(i, frameCount, UnpackBlendShapeWeight(a), frames, interp);
		
		int indexA = ((i + frames.x) * _VertexCount) + index;
		int indexB = ((i + frames.y) * _VertexCount) + index;
		
		BlendShapeDelta dataA = _BlendShapeData[max(0, indexA)];
		if (frames.x < 0) 
		{		
			dataA.deltaVertex = 0;
			dataA.deltaNormal = 0;
			dataA.deltaTangent = 0;
		}
		BlendShapeDelta dataB = _BlendShapeData[indexB];
		
		inPosition = inPosition + lerp(dataA.deltaVertex.xyz, dataB.deltaVertex.xyz, interp);
		inNormal = inNormal + lerp(dataA.deltaNormal.xyz, dataB.deltaNormal.xyz, interp);
		inTangent = inTangent + lerp(dataA.deltaTangent.xyz, dataB.deltaTangent.xyz, interp);
		
		i += frameCount;
	
	}

	SkinNoShapes_float(index, inPosition, inNormal, inTangent, outPosition, outNormal, outTangent);

}

void SkinNoTang_float(int index, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal)
{
	
	BlendShapeDelta defaultShapeData;
	defaultShapeData.deltaVertex = 0;
	defaultShapeData.deltaNormal = 0;
	defaultShapeData.deltaTangent = 0;  

	int i = 0;
	for (int a = 0; a < _BlendShapeCount; a++)
	{

		int frameCount = int(_BlendShapeFrameCounts[a]);
		
		int2 frames; 
		float interp;
		CalculateBlendShapePosition(i, frameCount, UnpackBlendShapeWeight(a), frames, interp);
		
		int indexA = ((i + frames.x) * _VertexCount) + index;
		int indexB = ((i + frames.y) * _VertexCount) + index;
		
		BlendShapeDelta dataA = _BlendShapeData[max(0, indexA)];
		if (frames.x < 0) 
		{		
			dataA.deltaVertex = 0;
			dataA.deltaNormal = 0;
		}
		BlendShapeDelta dataB = _BlendShapeData[indexB];
		
		inPosition = inPosition + lerp(dataA.deltaVertex.xyz, dataB.deltaVertex.xyz, interp);
		inNormal = inNormal + lerp(dataA.deltaNormal.xyz, dataB.deltaNormal.xyz, interp);
		
		i += frameCount;
	
	}

	SkinNoShapesNoTang_float(index, inPosition, inNormal, outPosition, outNormal);

}

#endif
#endif