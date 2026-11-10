#ifndef CUSTOM_SKINNING
#define CUSTOM_SKINNING

#ifdef SHADERGRAPH_PREVIEW

void SkinAndShapes_float(int shapesID, int rigID, int vertexIndex, int vertexCount, float2 mainUV, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{

	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;

}

void SkinAndShapesNoTang_float(int shapesID, int rigID, int vertexIndex, int vertexCount, float2 mainUV, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal)
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

	float3 deltaVertex;
	float3 deltaNormal;
	float3 deltaTangent;

};

uniform int _BoneCount;

uniform int2 _RangeStandaloneShapes; 

#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL) || defined(SHADER_API_PSSL)
StructuredBuffer<SkinInfluence> _SkinBindings;
StructuredBuffer<float4x4> _SkinningMatrices;

StructuredBuffer<BlendShapeDelta> _MorphShapes;

StructuredBuffer<float> _ControlStandaloneShapes;
#else
StructuredBuffer<SkinInfluence> _SkinBindings;
StructuredBuffer<float4x4> _SkinningMatrices;

StructuredBuffer<BlendShapeDelta> _MorphShapes;

StructuredBuffer<float> _ControlStandaloneShapes;
#endif

void ApplyShapeDelta_float(int indexInBuffer, float weight, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent) 
{
	const BlendShapeDelta delta = _MorphShapes[indexInBuffer];

	float weight_clamped = saturate(weight);

	outPosition = inPosition + delta.deltaVertex.xyz * weight;
	outNormal = inNormal + delta.deltaNormal.xyz * weight_clamped;
	outTangent = inTangent + delta.deltaTangent.xyz * weight_clamped;
}
void ApplyShapeDeltaNoTang_float(int indexInBuffer, float weight, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal) 
{
	const BlendShapeDelta delta = _MorphShapes[indexInBuffer];

	float weight_clamped = saturate(weight);

	outPosition = inPosition + delta.deltaVertex.xyz * weight;
	outNormal = inNormal + delta.deltaNormal.xyz * weight_clamped;
}

void SkinNoShapes_float(int instanceID, int vertexIndex, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{

	SkinInfluence si = _SkinBindings[vertexIndex];
	
	const int boneIndexOffset = instanceID * _BoneCount;
	const float4x4 blendedMatrix = 
	_SkinningMatrices[boneIndexOffset + int(si.indicesA.x)] * si.weightsA.x 
	+ _SkinningMatrices[boneIndexOffset + int(si.indicesA.y)] * si.weightsA.y 
	+ _SkinningMatrices[boneIndexOffset + int(si.indicesA.z)] * si.weightsA.z  
	+ _SkinningMatrices[boneIndexOffset + int(si.indicesA.w)] * si.weightsA.w 
	+ _SkinningMatrices[boneIndexOffset + int(si.indicesB.x)] * si.weightsB.x 
	+ _SkinningMatrices[boneIndexOffset + int(si.indicesB.y)] * si.weightsB.y 
	+ _SkinningMatrices[boneIndexOffset + int(si.indicesB.z)] * si.weightsB.z 
	+ _SkinningMatrices[boneIndexOffset + int(si.indicesB.w)] * si.weightsB.w;

	outPosition = mul(blendedMatrix, float4(inPosition, 1)).xyz; 
	outNormal = mul(blendedMatrix, float4(inNormal, 0)).xyz;
	outTangent = mul(blendedMatrix, float4(inTangent, 0)).xyz;

}

void SkinNoShapesNoTang_float(int instanceID, int vertexIndex, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal)
{

	SkinInfluence si = _SkinBindings[vertexIndex];
	
	const int boneIndexOffset = instanceID * _BoneCount;
	const float4x4 blendedMatrix = 
	_SkinningMatrices[boneIndexOffset + int(si.indicesA.x)] * si.weightsA.x 
	+ _SkinningMatrices[boneIndexOffset + int(si.indicesA.y)] * si.weightsA.y 
	+ _SkinningMatrices[boneIndexOffset + int(si.indicesA.z)] * si.weightsA.z  
	+ _SkinningMatrices[boneIndexOffset + int(si.indicesA.w)] * si.weightsA.w 
	+ _SkinningMatrices[boneIndexOffset + int(si.indicesB.x)] * si.weightsB.x 
	+ _SkinningMatrices[boneIndexOffset + int(si.indicesB.y)] * si.weightsB.y 
	+ _SkinningMatrices[boneIndexOffset + int(si.indicesB.z)] * si.weightsB.z 
	+ _SkinningMatrices[boneIndexOffset + int(si.indicesB.w)] * si.weightsB.w;

	outPosition = mul(blendedMatrix, float4(inPosition, 1)).xyz;
	outNormal = mul(blendedMatrix, float4(inNormal, 0)).xyz;

}



void ApplyStandaloneShapes_float(int instanceID, int vertexIndex, int vertexCount, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{

	const int standaloneShapeCount = max(0, (_RangeStandaloneShapes.y - _RangeStandaloneShapes.x) + 1);
	const int indexOffset = instanceID * standaloneShapeCount;
	
	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;

	for(int a = 0; a < standaloneShapeCount; a++) 
	{
		float weight = _ControlStandaloneShapes[a + indexOffset];

		int shapeStartIndex = ((_RangeStandaloneShapes.x + a) * vertexCount);
		ApplyShapeDelta_float(shapeStartIndex + vertexIndex, weight, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent); 
	}

}
void ApplyStandaloneShapesNoTang_float(int instanceID, int vertexIndex, int vertexCount, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal)
{

	const int standaloneShapeCount = max(0, (_RangeStandaloneShapes.y - _RangeStandaloneShapes.x) + 1);
	const int indexOffset = instanceID * standaloneShapeCount;
	
	outPosition = inPosition;
	outNormal = inNormal;

	for(int a = 0; a < standaloneShapeCount; a++) 
	{
		float weight = _ControlStandaloneShapes[a + indexOffset];

		int shapeStartIndex = ((_RangeStandaloneShapes.x + a) * vertexCount);
		ApplyShapeDeltaNoTang_float(shapeStartIndex + vertexIndex, weight, outPosition, outNormal, outPosition, outNormal);
	}

}

void ApplyMultiShape_float(StructuredBuffer<float> frameWeights, int shapeCount, int multiShapeStartIndex, float weight, float multiplier, int vertexIndex, int vertexCount, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent) 
{

	outPosition = inPosition;
	outNormal = inNormal; 
	outTangent = inTangent;

	if (shapeCount > 1) 
	{
		int shapeCountM1 = shapeCount - 1;
		for(int shapeIndexA = 0; shapeIndexA < shapeCountM1; shapeIndexA++) 
		{
			float frameWeightA = frameWeights[shapeIndexA];

			int shapeIndexB = shapeIndexA + 1;
			float frameWeightB = frameWeights[shapeIndexB];

			float weightRange = frameWeightB - frameWeightA;

			float weightA = 0;
			float weightB = (weight - frameWeightA) / weightRange;
			if (weightB < 0) 
			{
				if (shapeIndexA == 0) 
				{
					if (frameWeightA != 0) 
					{
						weightA = weight / frameWeightA; 
						weightB = 0;
					} 
					else 
					{
						weightA = 1 + abs(weight / weightRange);
						weightB = 0;
					}
				} 
				else 
				{
					weightA = abs(weightB);
					weightB = 0;
				}
			} 
			else 
			{
				weightA = 1 - weightB;
				if (weightA < 0 && shapeIndexB < shapeCountM1) continue;
				weightA = max(0, weightA);
			}

			int shapeStartIndex;
			shapeStartIndex = ((multiShapeStartIndex + shapeIndexA) * vertexCount);
			ApplyShapeDelta_float(shapeStartIndex + vertexIndex, weightA, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent); 
			shapeStartIndex = ((multiShapeStartIndex + shapeIndexB) * vertexCount);
			ApplyShapeDelta_float(shapeStartIndex + vertexIndex, weightB, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent); 

			break;
		}
	} 
	else if (shapeCount > 0)
	{
		int shapeStartIndex = multiShapeStartIndex * vertexCount;
		float maxWeight = frameWeights[0];
		ApplyShapeDelta_float(shapeStartIndex + vertexIndex, weight / maxWeight, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent); 
	}

	outPosition = lerp(inPosition, outPosition, multiplier);
	outNormal = lerp(inNormal, outNormal, multiplier);
	outTangent = lerp(inTangent, outTangent, multiplier);
}
void ApplyMultiShapeNoTang_float(StructuredBuffer<float> frameWeights, int shapeCount, int multiShapeStartIndex, float weight, float multiplier, int vertexIndex, int vertexCount, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal) 
{

	outPosition = inPosition;
	outNormal = inNormal;

	if (shapeCount > 1) 
	{
		int shapeCountM1 = shapeCount - 1;
		for(int shapeIndexA = 0; shapeIndexA < shapeCountM1; shapeIndexA++) 
		{
			float frameWeightA = frameWeights[shapeIndexA];

			int shapeIndexB = shapeIndexA + 1;
			float frameWeightB = frameWeights[shapeIndexB];

			float weightRange = frameWeightB - frameWeightA;

			float weightA = 0;
			float weightB = (weight - frameWeightA) / weightRange;
			if (weightB < 0) 
			{
				if (shapeIndexA == 0) 
				{
					if (frameWeightA != 0) 
					{
						weightA = weight / frameWeightA; 
						weightB = 0;
					} 
					else 
					{
						weightA = 1 + abs(weight / weightRange);
						weightB = 0;
					}
				} 
				else 
				{
					weightA = abs(weightB);
					weightB = 0;
				}
			} 
			else 
			{
				weightA = 1 - weightB;
				if (weightA < 0 && shapeIndexB < shapeCountM1) continue;
				weightA = max(0, weightA);
			}

			int shapeStartIndex;
			shapeStartIndex = ((multiShapeStartIndex + shapeIndexA) * vertexCount);
			ApplyShapeDeltaNoTang_float(shapeStartIndex + vertexIndex, weightA, outPosition, outNormal, outPosition, outNormal); 
			shapeStartIndex = ((multiShapeStartIndex + shapeIndexB) * vertexCount);
			ApplyShapeDeltaNoTang_float(shapeStartIndex + vertexIndex, weightB, outPosition, outNormal, outPosition, outNormal); 

			break;
		}
	} 
	else if (shapeCount > 0)
	{
		int shapeStartIndex = multiShapeStartIndex * vertexCount;
		float maxWeight = frameWeights[0];
		ApplyShapeDeltaNoTang_float(shapeStartIndex + vertexIndex, weight / maxWeight, outPosition, outNormal, outPosition, outNormal); 
	}

	outPosition = lerp(inPosition, outPosition, multiplier);
	outNormal = lerp(inNormal, outNormal, multiplier);
}

void SkinAndShapes_float(int shapesID, int rigID, int vertexIndex, int vertexCount, float2 mainUV, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{

	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;

	ApplyStandaloneShapes_float(shapesID, vertexIndex, vertexCount, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);

	outNormal = normalize(outNormal);
	outTangent = normalize(outTangent);

	SkinNoShapes_float(rigID, vertexIndex, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);

}

void SkinAndShapesNoTang_float(int shapesID, int rigID, int vertexIndex, int vertexCount, float2 mainUV, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal)
{

	outPosition = inPosition;
	outNormal = inNormal;

	ApplyStandaloneShapesNoTang_float(shapesID, vertexIndex, vertexCount, outPosition, outNormal, outPosition, outNormal);

	outNormal = normalize(outNormal);

	SkinNoShapesNoTang_float(rigID, vertexIndex, outPosition, outNormal, outPosition, outNormal);

}


#endif
#endif