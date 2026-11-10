#ifndef CUSTOM_SKINNING
#define CUSTOM_SKINNING

#ifdef SHADERGRAPH_PREVIEW

void Skin_float(int instanceID, int vertexIndex, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{

	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;

}

void SkinNoTang_float(int instanceID, int vertexIndex, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal)
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


uniform int _BoneCount;

#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL) || defined(SHADER_API_PSSL)
StructuredBuffer<SkinInfluence> _SkinBindings;
StructuredBuffer<float4x4> _SkinningMatrices;
StructuredBuffer<BlendShapeDelta> _BlendShapeData;
#else
StructuredBuffer<SkinInfluence> _SkinBindings;
StructuredBuffer<float4x4> _SkinningMatrices;
StructuredBuffer<BlendShapeDelta> _BlendShapeData;
#endif

void SkinNoShapes_float(int instanceID, int vertexIndex, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{

	SkinInfluence si = _SkinBindings[vertexIndex];
	
	int boneIndexOffset = instanceID * _BoneCount;
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
	
	int boneIndexOffset = instanceID * _BoneCount;
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

void Skin_float(int instanceID, int vertexIndex, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{

	SkinNoShapes_float(instanceID, vertexIndex, inPosition, inNormal, inTangent, outPosition, outNormal, outTangent);

}

void SkinNoTang_float(int instanceID, int vertexIndex, float3 inPosition, float3 inNormal, out float3 outPosition, out float3 outNormal)
{

	SkinNoShapesNoTang_float(instanceID, vertexIndex, inPosition, inNormal, outPosition, outNormal);

}

#endif
#endif