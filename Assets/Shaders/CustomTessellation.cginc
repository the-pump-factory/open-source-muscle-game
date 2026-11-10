// Tessellation programs based on this article by Catlike Coding:
// https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/

struct vertexInput
{
	float4 vertex : POSITION;
	float4 color : COLOR;
	float3 normal : NORMAL;
	float4 tangent : TANGENT;
	float2 uv_MainTex : TEXCOORD0;
	float2 uv2_MainTex : TEXCOORD1;
};

struct vertexOutput
{
	float4 vertex : SV_POSITION;
	float4 color : COLOR;
	float3 normal : NORMAL;
	float4 tangent : TANGENT;
	float2 uv : TEXCOORD0;
	float2 uv2 : TEXCOORD1;
};

struct TessellationFactors 
{
	float edge[3] : SV_TessFactor;
	float inside : SV_InsideTessFactor;
};

vertexInput vert(vertexInput v)
{
	return v;
}

vertexOutput tessVert(vertexInput v)
{

	vertexOutput o;

	float4x4 modelMatrix = unity_ObjectToWorld;
	float4x4 modelMatrixInverse = unity_WorldToObject;

	// Note that the vertex is NOT transformed to clip
	// space here
	o.vertex = mul(unity_ObjectToWorld, v.vertex);
	o.normal = UnityObjectToWorldNormal(v.normal);
	o.tangent = v.tangent;
	o.tangent.xyz = UnityObjectToWorldDir(v.tangent.xyz);
	//o.tangent = v.tangent;
	//o.tangent.xyz = normalize(mul(unity_ObjectToWorld, float4(v.tangent.xyz, 0.0)).xyz);
	o.uv = v.uv_MainTex;
	o.uv2 = v.uv2_MainTex;
	o.color = v.color;

	return o;
}

float _TessellationUniformMax;
float _TessellationUniformMin;

float _TessellationDistanceMax;
float _TessellationDistanceMin;
float _TessellationDistancePower;

float _TessellationCutoff;

float _TessellationClipThreshold;

float _TessellationMinDot;

TessellationFactors patchConstantFunction (InputPatch<vertexInput, 3> patch)
{

	float alpha = patch[0].color.a;

	float tessellation = 0;

	if (alpha >= _TessellationClipThreshold) {

		float3 normal = UnityObjectToWorldNormal(normalize(patch[0].normal + patch[1].normal + patch[2].normal));

		float3 offset = WorldSpaceViewDir(patch[0].vertex);

		float distance = length(offset);

		float3 offsetDir = offset / distance;
		
		float camDot = _TessellationMinDot >= 1 ? 1 : (1 - (max(0, dot(normal, offsetDir) - _TessellationMinDot) / (1 - _TessellationMinDot)));

		tessellation = saturate((distance - _TessellationDistanceMin) / _TessellationDistanceMax);

		tessellation = lerp(_TessellationUniformMin, _TessellationUniformMax, pow(1 - tessellation, _TessellationDistancePower) * camDot);

		tessellation = tessellation < _TessellationCutoff ? 0 : tessellation;

	}

	TessellationFactors f;
	f.edge[0] = tessellation;
	f.edge[1] = tessellation;
	f.edge[2] = tessellation;
	f.inside = tessellation;

	return f;

}

[UNITY_domain("tri")]
[UNITY_outputcontrolpoints(3)]
[UNITY_outputtopology("triangle_cw")]
[UNITY_partitioning("integer")]
[UNITY_patchconstantfunc("patchConstantFunction")]
vertexInput hull (InputPatch<vertexInput, 3> patch, uint id : SV_OutputControlPointID)
{
	return patch[id];
}

[UNITY_domain("tri")]
vertexOutput domain(TessellationFactors factors, OutputPatch<vertexInput, 3> patch, float3 barycentricCoordinates : SV_DomainLocation)
{
	vertexInput v;

	#define MY_DOMAIN_PROGRAM_INTERPOLATE(fieldName) v.fieldName = \
		patch[0].fieldName * barycentricCoordinates.x + \
		patch[1].fieldName * barycentricCoordinates.y + \
		patch[2].fieldName * barycentricCoordinates.z;

	MY_DOMAIN_PROGRAM_INTERPOLATE(vertex)
	MY_DOMAIN_PROGRAM_INTERPOLATE(normal)
	MY_DOMAIN_PROGRAM_INTERPOLATE(tangent)
	MY_DOMAIN_PROGRAM_INTERPOLATE(uv_MainTex)
	MY_DOMAIN_PROGRAM_INTERPOLATE(uv2_MainTex)
	MY_DOMAIN_PROGRAM_INTERPOLATE(color)

	return tessVert(v);
}