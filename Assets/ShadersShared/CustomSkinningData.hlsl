#ifndef CUSTOM_SKINNING
#define CUSTOM_SKINNING

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

#endif