#include "AnimeShading.hlsl"
#include "Quaternions.hlsl"

float _VeinThicknessMultiplier;
float _VeinBendingTopologyShift;

float lerpC(float x, float y, float z, float w, float t)
{

	float t2 = t * t;
	float t3 = t2 * t;
	float mt = 1 - t;
	float mt2 = mt * mt;
	float mt3 = mt2 * mt;

	return (x * mt3) + (3 * y * mt2 * t) + (3 * z * mt * t2) + (w * t3);

}

float3 cubicCurve(float3 p1, float3 p2, float3 p3, float3 p4, float t)
{

	return float3(lerpC(p1.x, p2.x, p3.x, p4.x, t), lerpC(p1.y, p2.y, p3.y, p4.y, t), lerpC(p1.z, p2.z, p3.z, p4.z, t));

}

struct veinVertInput {

	float4 vertex : POSITION;
	float4 color : COLOR;

	float4 data1 : TEXCOORD0;
	float4 data2 : TEXCOORD1;
	float4 data3 : TEXCOORD2;

};

struct veinFragInput
{

	float4 vertex : SV_POSITION;
	float4 color : COLOR;

	float3 normal : NORMAL;

	float2 uv : TEXCOORD0;

	float3 worldPos : TEXCOORD1;

	float isOutline : TEXCOORD2;

};

veinVertInput veinVertex(veinVertInput v) {

	return v;

}

void veinGeometry(triangle veinVertInput IN[3], inout TriangleStream<veinFragInput> triStream)
{

	float4 uv1_1 = IN[0].data1;
	float4 uv2_1 = IN[0].data2;
	float4 uv3_1 = IN[0].data3;

	float4 uv1_2 = IN[1].data1;
	float4 uv2_2 = IN[1].data2;
	float4 uv3_2 = IN[1].data3;

	float4 uv1_3 = IN[2].data1;
	float4 uv2_3 = IN[2].data2;
	float4 uv3_3 = IN[2].data3;


	float3 cp1_1 = float3(uv1_1.x, uv1_1.y, uv1_1.z);
	float3 cp2_1 = float3(uv1_1.w, uv2_1.x, uv2_1.y);
	float3 cp3_1 = float3(uv2_1.z, uv2_1.w, uv3_1.x);
	float3 cp4_1 = float3(uv3_1.y, uv3_1.z, uv3_1.w);

	float3 cp1_2 = float3(uv1_2.x, uv1_2.y, uv1_2.z);
	float3 cp2_2 = float3(uv1_2.w, uv2_2.x, uv2_2.y);
	float3 cp3_2 = float3(uv2_2.z, uv2_2.w, uv3_2.x);
	float3 cp4_2 = float3(uv3_2.y, uv3_2.z, uv3_2.w);

	float3 cp1_3 = float3(uv1_3.x, uv1_3.y, uv1_3.z);
	float3 cp2_3 = float3(uv1_3.w, uv2_3.x, uv2_3.y);
	float3 cp3_3 = float3(uv2_3.z, uv2_3.w, uv3_3.x);
	float3 cp4_3 = float3(uv3_3.y, uv3_3.z, uv3_3.w);


	float4 T1 = IN[0].vertex;
	float4 T2 = IN[1].vertex;
	float4 T3 = IN[2].vertex;

	float localT1 = T1.y;
	float localT2 = T2.y;
	float localT3 = T3.y;


	float3 p1 = cubicCurve(cp1_1, cp2_1, cp3_1, cp4_1, localT1);
	float3 p2 = cubicCurve(cp1_2, cp2_2, cp3_2, cp4_2, localT2);
	float3 p3 = cubicCurve(cp1_3, cp2_3, cp3_3, cp4_3, localT3);

	p1 = mul(unity_WorldToObject, float4(p1, 1)).xyz;
	p2 = mul(unity_WorldToObject, float4(p2, 1)).xyz;
	p3 = mul(unity_WorldToObject, float4(p3, 1)).xyz;

	float3 dirA = normalize(p2 - p1);
	float3 dirB = normalize(p3 - p2);

	float4 lookA = q_look_at(dirA, float3(0, 1, 0));
	float4 lookB = q_look_at(dirB, float3(0, 1, 0));


	for (int a = 0; a < 6; a++)
	{

		float t1, t2;

		t2 = a / 6.0;

		if (a == 0)
		{

			t1 = 5 / 6.0;

		}
		else
		{

			t1 = (a - 1) / 6.0;

		}

		float3 d1 = rotate_vector(float3(1, 0, 0), rotate_angle_axis(t1 * UNITY_PI * 2, float3(0, 0, 1)));
		float3 d2 = rotate_vector(float3(1, 0, 0), rotate_angle_axis(t2 * UNITY_PI * 2, float3(0, 0, 1)));

		float3 n1 = rotate_vector(d1, lookA);
		float3 n2 = rotate_vector(d1, lookB);
		float3 n3 = rotate_vector(d2, lookA);
		float3 n4 = rotate_vector(d2, lookB);

		float3 i1 = p2 + n1 * _VeinThicknessMultiplier * IN[1].color.b;
		float3 i2 = p3 + n2 * _VeinThicknessMultiplier * IN[2].color.b;
		float3 i3 = p2 + n3 * _VeinThicknessMultiplier * IN[1].color.b;
		float3 i4 = p3 + n4 * _VeinThicknessMultiplier * IN[2].color.b;

		veinFragInput v1, v2, v3, v4;

		v1.vertex = TransformObjectToHClip(i1);
		v1.color = IN[1].color;
		v1.normal = TransformObjectToWorldNormal(n1);
		v1.uv = T2.x;
		v1.worldPos = mul(unity_ObjectToWorld, float4(i1, 1)).xyz;
		v1.isOutline = 0;


		v2.vertex = TransformObjectToHClip(i2);
		v2.color = IN[2].color;
		v2.normal = TransformObjectToWorldNormal(n2);
		v2.uv = T3.x;
		v2.worldPos = mul(unity_ObjectToWorld, float4(i2, 1)).xyz;
		v2.isOutline = 0;

		v3.vertex = TransformObjectToHClip(i3);
		v3.color = IN[1].color;
		v3.normal = TransformObjectToWorldNormal(n3);
		v3.uv = T2.x;
		v3.worldPos = mul(unity_ObjectToWorld, float4(i3, 1)).xyz;
		v3.isOutline = 0;

		v4.vertex = TransformObjectToHClip(i4);
		v4.color = IN[2].color;
		v4.normal = TransformObjectToWorldNormal(n4);
		v4.uv = T3.x;
		v4.worldPos = mul(unity_ObjectToWorld, float4(i4, 1)).xyz;
		v4.isOutline = 0;

		triStream.Append(v2);
		triStream.Append(v1);
		triStream.Append(v4);
		triStream.Append(v3);

		triStream.RestartStrip();

		float lineThickness1 = GetLineThickness(v1.worldPos, float4(1, 1, 1, 1));
		float lineThickness2 = GetLineThickness(v2.worldPos, float4(1, 1, 1, 1));
		float lineThickness3 = GetLineThickness(v3.worldPos, float4(1, 1, 1, 1));
		float lineThickness4 = GetLineThickness(v4.worldPos, float4(1, 1, 1, 1));

		float3 center = (i1 + i2 + i3 + i4) / 4.0;

		v1.vertex = TransformObjectToHClip(i1 + n1 * lineThickness1 + normalize(i1 - center) * lineThickness1);
		v2.vertex = TransformObjectToHClip(i2 + n2 * lineThickness2 + normalize(i2 - center) * lineThickness2);
		v3.vertex = TransformObjectToHClip(i3 + n3 * lineThickness3 + normalize(i3 - center) * lineThickness3);
		v4.vertex = TransformObjectToHClip(i4 + n4 * lineThickness4 + normalize(i4 - center) * lineThickness4);

		v1.isOutline = 2;
		v2.isOutline = 2;
		v3.isOutline = 2;
		v4.isOutline = 2;

		triStream.Append(v1);
		triStream.Append(v2);
		triStream.Append(v3);
		triStream.Append(v4);

		triStream.RestartStrip();

	}

}

struct VertexRing
{

	float3 center;
	float3 dir;

	int equivalentIndex;
	float paired;

};

VertexRing selectValue(int index, VertexRing r1, VertexRing r2, VertexRing r3, VertexRing r4, VertexRing r5, VertexRing r6)
{

	[branch] switch (index)
	{
	default:
		return r1;
	case 1:
		return r2;
	case 2:
		return r3;
	case 3:
		return r4;
	case 4:
		return r5;
	case 5:
		return r6;
	}

}

void setPaired(int index, int pair, inout VertexRing r1, inout VertexRing r2, inout VertexRing r3, inout VertexRing r4, inout VertexRing r5, inout VertexRing r6)
{

	[branch] switch (index)
	{
	default:
		r1.paired = 2;
		r1.equivalentIndex = pair;
		return;
	case 1:
		r2.paired = 2;
		r2.equivalentIndex = pair;
		return;
	case 2:
		r3.paired = 2;
		r3.equivalentIndex = pair;
		return;
	case 3:
		r4.paired = 2;
		r4.equivalentIndex = pair;
		return;
	case 4:
		r5.paired = 2;
		r5.equivalentIndex = pair;
		return;
	case 5:
		r6.paired = 2;
		r6.equivalentIndex = pair;
		return;
	}

}

int selectIndexFromLeastDistance(float dist1, float dist2, float dist3, float dist4, float dist5, float dist6)
{

	if (dist2 < dist1 && dist2 < dist3 && dist2 < dist4 && dist2 < dist5 && dist2 < dist6) return 1;

	if (dist3 < dist1 && dist3 < dist2 && dist3 < dist4 && dist3 < dist5 && dist3 < dist6) return 2;

	if (dist4 < dist1 && dist4 < dist2 && dist4 < dist3 && dist4 < dist5 && dist4 < dist6) return 3;

	if (dist5 < dist1 && dist5 < dist2 && dist5 < dist3 && dist5 < dist4 && dist5 < dist6) return 4;

	if (dist6 < dist1 && dist6 < dist2 && dist6 < dist3 && dist6 < dist4 && dist6 < dist5) return 5;

	return 0;

}

int selectIndexFromLeastDistanceWithConditions(float dist1, float dist2, float dist3, float dist4, float dist5, float dist6, float cond1, float cond2, float cond3, float cond4, float cond5, float cond6)
{

	dist1 = cond1 >= 1 ? FLT_MAX : dist1;
	dist2 = cond2 >= 1 ? FLT_MAX : dist2;
	dist3 = cond3 >= 1 ? FLT_MAX : dist3;
	dist4 = cond4 >= 1 ? FLT_MAX : dist4;
	dist5 = cond5 >= 1 ? FLT_MAX : dist5;
	dist6 = cond6 >= 1 ? FLT_MAX : dist6;

	return selectIndexFromLeastDistance(dist1, dist2, dist3, dist4, dist5, dist6);

}

void addTriangles(triangle veinVertInput IN[3], inout TriangleStream<veinFragInput> triStream, float3 T2, float3 T3, VertexRing r1, VertexRing r3, VertexRing ring2_1, VertexRing ring2_2, VertexRing ring2_3, VertexRing ring2_4, VertexRing ring2_5, VertexRing ring2_6)
{

	VertexRing r2, r4;

	r2 = selectValue(r1.equivalentIndex, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	r4 = selectValue(r3.equivalentIndex, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);

	float3 i1 = r1.center + r1.dir * _VeinThicknessMultiplier * IN[1].color.b;
	float3 i2 = r2.center + r2.dir * _VeinThicknessMultiplier * IN[2].color.b;
	float3 i3 = r3.center + r3.dir * _VeinThicknessMultiplier * IN[1].color.b;
	float3 i4 = r4.center + r4.dir * _VeinThicknessMultiplier * IN[2].color.b;

	veinFragInput v1, v2, v3, v4;

	v1.vertex = TransformObjectToHClip(i1);
	v1.color = IN[1].color;
	v1.normal = TransformObjectToWorldNormal(r1.dir);
	v1.uv = T2.x;
	v1.worldPos = mul(unity_ObjectToWorld, float4(i1, 1)).xyz;
	v1.isOutline = 0;


	v2.vertex = TransformObjectToHClip(i2);
	v2.color = IN[2].color;
	v2.normal = TransformObjectToWorldNormal(r2.dir);
	v2.uv = T3.x;
	v2.worldPos = mul(unity_ObjectToWorld, float4(i2, 1)).xyz;
	v2.isOutline = 0;

	v3.vertex = TransformObjectToHClip(i3);
	v3.color = IN[1].color;
	v3.normal = TransformObjectToWorldNormal(r3.dir);
	v3.uv = T2.x;
	v3.worldPos = mul(unity_ObjectToWorld, float4(i3, 1)).xyz;
	v3.isOutline = 0;

	v4.vertex = TransformObjectToHClip(i4);
	v4.color = IN[2].color;
	v4.normal = TransformObjectToWorldNormal(r4.dir);
	v4.uv = T3.x;
	v4.worldPos = mul(unity_ObjectToWorld, float4(i4, 1)).xyz;
	v4.isOutline = 0;

	triStream.Append(v2);
	triStream.Append(v1);
	triStream.Append(v4);
	triStream.Append(v3);

	triStream.RestartStrip();

	float lineThickness1 = GetLineThickness(v1.worldPos, float4(1, 1, 1, 1));
	float lineThickness2 = GetLineThickness(v2.worldPos, float4(1, 1, 1, 1));
	float lineThickness3 = GetLineThickness(v3.worldPos, float4(1, 1, 1, 1));
	float lineThickness4 = GetLineThickness(v4.worldPos, float4(1, 1, 1, 1));

	float3 center = (i1 + i2 + i3 + i4) / 4.0;

	v1.vertex = TransformObjectToHClip(i1 + r1.dir * lineThickness1 + normalize(i1 - center) * lineThickness1);
	v2.vertex = TransformObjectToHClip(i2 + r2.dir * lineThickness2 + normalize(i2 - center) * lineThickness2);
	v3.vertex = TransformObjectToHClip(i3 + r3.dir * lineThickness3 + normalize(i3 - center) * lineThickness3);
	v4.vertex = TransformObjectToHClip(i4 + r4.dir * lineThickness4 + normalize(i4 - center) * lineThickness4);

	v1.isOutline = 2;
	v2.isOutline = 2;
	v3.isOutline = 2;
	v4.isOutline = 2;

	triStream.Append(v1);
	triStream.Append(v2);
	triStream.Append(v3);
	triStream.Append(v4);

	triStream.RestartStrip();

}

void veinGeometry2(triangle veinVertInput IN[3], inout TriangleStream<veinFragInput> triStream)
{

	float4 uv1_1 = IN[0].data1;
	float4 uv2_1 = IN[0].data2;
	float4 uv3_1 = IN[0].data3;

	float4 uv1_2 = IN[1].data1;
	float4 uv2_2 = IN[1].data2;
	float4 uv3_2 = IN[1].data3;

	float4 uv1_3 = IN[2].data1;
	float4 uv2_3 = IN[2].data2;
	float4 uv3_3 = IN[2].data3;


	float3 cp1_1 = float3(uv1_1.x, uv1_1.y, uv1_1.z);
	float3 cp2_1 = float3(uv1_1.w, uv2_1.x, uv2_1.y);
	float3 cp3_1 = float3(uv2_1.z, uv2_1.w, uv3_1.x);
	float3 cp4_1 = float3(uv3_1.y, uv3_1.z, uv3_1.w);

	float3 cp1_2 = float3(uv1_2.x, uv1_2.y, uv1_2.z);
	float3 cp2_2 = float3(uv1_2.w, uv2_2.x, uv2_2.y);
	float3 cp3_2 = float3(uv2_2.z, uv2_2.w, uv3_2.x);
	float3 cp4_2 = float3(uv3_2.y, uv3_2.z, uv3_2.w);

	float3 cp1_3 = float3(uv1_3.x, uv1_3.y, uv1_3.z);
	float3 cp2_3 = float3(uv1_3.w, uv2_3.x, uv2_3.y);
	float3 cp3_3 = float3(uv2_3.z, uv2_3.w, uv3_3.x);
	float3 cp4_3 = float3(uv3_3.y, uv3_3.z, uv3_3.w);


	float4 T1 = IN[0].vertex;
	float4 T2 = IN[1].vertex;
	float4 T3 = IN[2].vertex;

	float localT1 = T1.y;
	float localT2 = T2.y;
	float localT3 = T3.y;

	
	float3 p1 = cubicCurve(cp1_1, cp2_1, cp3_1, cp4_1, localT1);
	float3 p2 = cubicCurve(cp1_2, cp2_2, cp3_2, cp4_2, localT2);
	float3 p3 = cubicCurve(cp1_3, cp2_3, cp3_3, cp4_3, localT3);

	p1 = mul(unity_WorldToObject, float4(p1, 1)).xyz;
	p2 = mul(unity_WorldToObject, float4(p2, 1)).xyz;
	p3 = mul(unity_WorldToObject, float4(p3, 1)).xyz;

	float3 dirA = normalize(p2 - p1);
	float3 dirB = normalize(p3 - p2);

	float4 lookA = q_look_at(dirA, float3(0, 1, 0));
	float4 lookB = q_look_at(dirB, float3(0, 1, 0));

	VertexRing ring1_1, ring1_2, ring1_3, ring1_4, ring1_5, ring1_6;
		
	VertexRing ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6;

	ring1_1.center = ring1_2.center = ring1_3.center = ring1_4.center = ring1_5.center = ring1_6.center = p2;
	ring2_1.center = ring2_2.center = ring2_3.center = ring2_4.center = ring2_5.center = ring2_6.center = p3;

	ring1_1.paired = ring1_2.paired = ring1_3.paired = ring1_4.paired = ring1_5.paired = ring1_6.paired = 2;
	ring2_1.paired = ring2_2.paired = ring2_3.paired = ring2_4.paired = ring2_5.paired = ring2_6.paired = 2;

	ring1_1.equivalentIndex = ring2_1.equivalentIndex = 0;
	ring1_2.equivalentIndex = ring2_2.equivalentIndex = 1;
	ring1_3.equivalentIndex = ring2_3.equivalentIndex = 2;
	ring1_4.equivalentIndex = ring2_4.equivalentIndex = 3;
	ring1_5.equivalentIndex = ring2_5.equivalentIndex = 4;
	ring1_6.equivalentIndex = ring2_6.equivalentIndex = 5;


	float t1 = 0;
	float t2 = 1 / 6.0;
	float t3 = 2 / 6.0;
	float t4 = 3 / 6.0;
	float t5 = 4 / 6.0;
	float t6 = 5 / 6.0;


	float3 d1_1 = float3(1, 0, 0);
	float3 n1_1 = rotate_vector(d1_1, lookA);
	float3 n2_1 = rotate_vector(d1_1, lookB);

	float3 d1_2 = rotate_vector(float3(1, 0, 0), rotate_angle_axis(t2 * UNITY_PI * 2, float3(0, 0, 1)));
	float3 n1_2 = rotate_vector(d1_2, lookA);
	float3 n2_2 = rotate_vector(d1_2, lookB);

	float3 d1_3 = rotate_vector(float3(1, 0, 0), rotate_angle_axis(t3 * UNITY_PI * 2, float3(0, 0, 1)));
	float3 n1_3 = rotate_vector(d1_3, lookA);
	float3 n2_3 = rotate_vector(d1_3, lookB);

	float3 d1_4 = rotate_vector(float3(1, 0, 0), rotate_angle_axis(t4 * UNITY_PI * 2, float3(0, 0, 1)));
	float3 n1_4 = rotate_vector(d1_4, lookA);
	float3 n2_4 = rotate_vector(d1_4, lookB);

	float3 d1_5 = rotate_vector(float3(1, 0, 0), rotate_angle_axis(t5 * UNITY_PI * 2, float3(0, 0, 1)));
	float3 n1_5 = rotate_vector(d1_5, lookA);
	float3 n2_5 = rotate_vector(d1_5, lookB);

	float3 d1_6 = rotate_vector(float3(1, 0, 0), rotate_angle_axis(t6 * UNITY_PI * 2, float3(0, 0, 1)));
	float3 n1_6 = rotate_vector(d1_6, lookA);
	float3 n2_6 = rotate_vector(d1_6, lookB);



	float4 lookC = q_slerp(lookA, lookB, _VeinBendingTopologyShift);

	ring1_1.dir = rotate_vector(d1_1, lookC);
	ring2_1.dir = n2_1;

	ring1_2.dir = rotate_vector(d1_2, lookC);
	ring2_2.dir = n2_2;

	ring1_3.dir = rotate_vector(d1_3, lookC);
	ring2_3.dir = n2_3;

	ring1_4.dir = rotate_vector(d1_4, lookC);
	ring2_4.dir = n2_4;

	ring1_5.dir = rotate_vector(d1_5, lookC);
	ring2_5.dir = n2_5;

	ring1_6.dir = rotate_vector(d1_6, lookC);
	ring2_6.dir = n2_6;

	
	float3 r1p1 = (ring2_1.center + ring1_1.dir);
	float3 r1p2 = (ring2_2.center + ring1_2.dir);
	float3 r1p3 = (ring2_3.center + ring1_3.dir);
	float3 r1p4 = (ring2_4.center + ring1_4.dir);
	float3 r1p5 = (ring2_5.center + ring1_5.dir);
	float3 r1p6 = (ring2_6.center + ring1_6.dir);

	float3 r2p1 = (ring2_1.center + ring2_1.dir);
	float3 r2p2 = (ring2_2.center + ring2_2.dir);
	float3 r2p3 = (ring2_3.center + ring2_3.dir);
	float3 r2p4 = (ring2_4.center + ring2_4.dir);
	float3 r2p5 = (ring2_5.center + ring2_5.dir);
	float3 r2p6 = (ring2_6.center + ring2_6.dir);

	float3 dir_1_1 = r1p1 - r2p1;
	float3 dir_1_2 = r1p1 - r2p2;
	float3 dir_1_3 = r1p1 - r2p3;
	float3 dir_1_4 = r1p1 - r2p4;
	float3 dir_1_5 = r1p1 - r2p5;
	float3 dir_1_6 = r1p1 - r2p6;

	float3 dir_2_1 = r1p2 - r2p1;
	float3 dir_2_2 = r1p2 - r2p2;
	float3 dir_2_3 = r1p2 - r2p3;
	float3 dir_2_4 = r1p2 - r2p4;
	float3 dir_2_5 = r1p2 - r2p5;
	float3 dir_2_6 = r1p2 - r2p6;

	float3 dir_3_1 = r1p3 - r2p1;
	float3 dir_3_2 = r1p3 - r2p2;
	float3 dir_3_3 = r1p3 - r2p3;
	float3 dir_3_4 = r1p3 - r2p4;
	float3 dir_3_5 = r1p3 - r2p5;
	float3 dir_3_6 = r1p3 - r2p6;

	float3 dir_4_1 = r1p4 - r2p1;
	float3 dir_4_2 = r1p4 - r2p2;
	float3 dir_4_3 = r1p4 - r2p3;
	float3 dir_4_4 = r1p4 - r2p4;
	float3 dir_4_5 = r1p4 - r2p5;
	float3 dir_4_6 = r1p4 - r2p6;

	float3 dir_5_1 = r1p5 - r2p1;
	float3 dir_5_2 = r1p5 - r2p2;
	float3 dir_5_3 = r1p5 - r2p3;
	float3 dir_5_4 = r1p5 - r2p4;
	float3 dir_5_5 = r1p5 - r2p5;
	float3 dir_5_6 = r1p5 - r2p6;

	float3 dir_6_1 = r1p6 - r2p1;
	float3 dir_6_2 = r1p6 - r2p2;
	float3 dir_6_3 = r1p6 - r2p3;
	float3 dir_6_4 = r1p6 - r2p4;
	float3 dir_6_5 = r1p6 - r2p5;
	float3 dir_6_6 = r1p6 - r2p6;


	float distance_1_1 = length(dir_1_1);
	float distance_1_2 = length(dir_1_2);
	float distance_1_3 = length(dir_1_3);
	float distance_1_4 = length(dir_1_4);
	float distance_1_5 = length(dir_1_5);
	float distance_1_6 = length(dir_1_6);

	float distance_2_1 = length(dir_2_1);
	float distance_2_2 = length(dir_2_2);
	float distance_2_3 = length(dir_2_3);
	float distance_2_4 = length(dir_2_4);
	float distance_2_5 = length(dir_2_5);
	float distance_2_6 = length(dir_2_6);

	float distance_3_1 = length(dir_3_1);
	float distance_3_2 = length(dir_3_2);
	float distance_3_3 = length(dir_3_3);
	float distance_3_4 = length(dir_3_4);
	float distance_3_5 = length(dir_3_5);
	float distance_3_6 = length(dir_3_6);

	float distance_4_1 = length(dir_4_1);
	float distance_4_2 = length(dir_4_2);
	float distance_4_3 = length(dir_4_3);
	float distance_4_4 = length(dir_4_4);
	float distance_4_5 = length(dir_4_5);
	float distance_4_6 = length(dir_4_6);

	float distance_5_1 = length(dir_5_1);
	float distance_5_2 = length(dir_5_2);
	float distance_5_3 = length(dir_5_3);
	float distance_5_4 = length(dir_5_4);
	float distance_5_5 = length(dir_5_5);
	float distance_5_6 = length(dir_5_6);

	float distance_6_1 = length(dir_6_1);
	float distance_6_2 = length(dir_6_2);
	float distance_6_3 = length(dir_6_3);
	float distance_6_4 = length(dir_6_4);
	float distance_6_5 = length(dir_6_5);
	float distance_6_6 = length(dir_6_6);


	ring1_1.dir = n1_1;

	ring1_2.dir = n1_2;

	ring1_3.dir = n1_3;

	ring1_4.dir = n1_4;

	ring1_5.dir = n1_5;

	ring1_6.dir = n1_6;


	int closest_1 = selectIndexFromLeastDistance(distance_1_1, distance_1_2, distance_1_3, distance_1_4, distance_1_5, distance_1_6);
	setPaired(closest_1, 0, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	ring1_1.equivalentIndex = closest_1;
	ring1_1.paired = 2;

	int closest_2 = selectIndexFromLeastDistance(distance_2_1, distance_2_2, distance_2_3, distance_2_4, distance_2_5, distance_2_6);
	setPaired(closest_2, 1, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	ring1_2.equivalentIndex = closest_2;
	ring1_2.paired = 2;

	int closest_3 = selectIndexFromLeastDistance(distance_3_1, distance_3_2, distance_3_3, distance_3_4, distance_3_5, distance_3_6);
	setPaired(closest_3, 2, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	ring1_3.equivalentIndex = closest_3;
	ring1_3.paired = 2;

	int closest_4 = selectIndexFromLeastDistance(distance_4_1, distance_4_2, distance_4_3, distance_4_4, distance_4_5, distance_4_6);
	setPaired(closest_4, 3, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	ring1_4.equivalentIndex = closest_4;
	ring1_4.paired = 2;

	int closest_5 = selectIndexFromLeastDistance(distance_5_1, distance_5_2, distance_5_3, distance_5_4, distance_5_5, distance_5_6);
	setPaired(closest_5, 4, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	ring1_5.equivalentIndex = closest_5;
	ring1_5.paired = 2;

	int closest_6 = selectIndexFromLeastDistance(distance_6_1, distance_6_2, distance_6_3, distance_6_4, distance_6_5, distance_6_6);
	setPaired(closest_6, 5, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	ring1_6.equivalentIndex = closest_6;
	ring1_6.paired = 2;

	addTriangles(IN, triStream, T2, T3, ring1_6, ring1_1, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	addTriangles(IN, triStream, T2, T3, ring1_1, ring1_2, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	addTriangles(IN, triStream, T2, T3, ring1_2, ring1_3, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	addTriangles(IN, triStream, T2, T3, ring1_3, ring1_4, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	addTriangles(IN, triStream, T2, T3, ring1_4, ring1_5, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	addTriangles(IN, triStream, T2, T3, ring1_5, ring1_6, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);

}

void veinGeometry3(triangle veinVertInput IN[3], inout TriangleStream<veinFragInput> triStream)
{

	float4 uv1_1 = IN[0].data1;
	float4 uv2_1 = IN[0].data2;
	float4 uv3_1 = IN[0].data3;

	float4 uv1_2 = IN[1].data1;
	float4 uv2_2 = IN[1].data2;
	float4 uv3_2 = IN[1].data3;

	float4 uv1_3 = IN[2].data1;
	float4 uv2_3 = IN[2].data2;
	float4 uv3_3 = IN[2].data3;


	float3 cp1_1 = float3(uv1_1.x, uv1_1.y, uv1_1.z);
	float3 cp2_1 = float3(uv1_1.w, uv2_1.x, uv2_1.y);
	float3 cp3_1 = float3(uv2_1.z, uv2_1.w, uv3_1.x);
	float3 cp4_1 = float3(uv3_1.y, uv3_1.z, uv3_1.w);

	float3 cp1_2 = float3(uv1_2.x, uv1_2.y, uv1_2.z);
	float3 cp2_2 = float3(uv1_2.w, uv2_2.x, uv2_2.y);
	float3 cp3_2 = float3(uv2_2.z, uv2_2.w, uv3_2.x);
	float3 cp4_2 = float3(uv3_2.y, uv3_2.z, uv3_2.w);

	float3 cp1_3 = float3(uv1_3.x, uv1_3.y, uv1_3.z);
	float3 cp2_3 = float3(uv1_3.w, uv2_3.x, uv2_3.y);
	float3 cp3_3 = float3(uv2_3.z, uv2_3.w, uv3_3.x);
	float3 cp4_3 = float3(uv3_3.y, uv3_3.z, uv3_3.w);


	float4 T1 = IN[0].vertex;
	float4 T2 = IN[1].vertex;
	float4 T3 = IN[2].vertex;

	float localT1 = T1.y;
	float localT2 = T2.y;
	float localT3 = T3.y;


	float3 p1 = cubicCurve(cp1_1, cp2_1, cp3_1, cp4_1, localT1);
	float3 p2 = cubicCurve(cp1_2, cp2_2, cp3_2, cp4_2, localT2);
	float3 p3 = cubicCurve(cp1_3, cp2_3, cp3_3, cp4_3, localT3);

	p1 = mul(unity_WorldToObject, float4(p1, 1)).xyz;
	p2 = mul(unity_WorldToObject, float4(p2, 1)).xyz;
	p3 = mul(unity_WorldToObject, float4(p3, 1)).xyz;

	float3 dirA = normalize(p2 - p1);
	float3 dirB = normalize(p3 - p2);

	float4 lookA = q_look_at(dirA, float3(0, 1, 0));
	float4 lookB = q_look_at(dirB, float3(0, 1, 0));

	VertexRing ring1_1, ring1_2, ring1_3, ring1_4, ring1_5, ring1_6;

	VertexRing ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6;

	ring1_1.center = ring1_2.center = ring1_3.center = ring1_4.center = ring1_5.center = ring1_6.center = p2;
	ring2_1.center = ring2_2.center = ring2_3.center = ring2_4.center = ring2_5.center = ring2_6.center = p3;

	ring1_1.paired = ring1_2.paired = ring1_3.paired = ring1_4.paired = ring1_5.paired = ring1_6.paired = 2;
	ring2_1.paired = ring2_2.paired = ring2_3.paired = ring2_4.paired = ring2_5.paired = ring2_6.paired = 2;

	ring1_1.equivalentIndex = ring2_1.equivalentIndex = 0;
	ring1_2.equivalentIndex = ring2_2.equivalentIndex = 1;
	ring1_3.equivalentIndex = ring2_3.equivalentIndex = 2;
	ring1_4.equivalentIndex = ring2_4.equivalentIndex = 3;
	ring1_5.equivalentIndex = ring2_5.equivalentIndex = 4;
	ring1_6.equivalentIndex = ring2_6.equivalentIndex = 5;


	float t1 = 0;
	float t2 = 1 / 6.0;
	float t3 = 2 / 6.0;
	float t4 = 3 / 6.0;
	float t5 = 4 / 6.0;
	float t6 = 5 / 6.0;


	float3 d1_1 = float3(1, 0, 0);
	float3 n1_1 = rotate_vector(d1_1, lookA);
	float3 n2_1 = rotate_vector(d1_1, lookB);

	float3 d1_2 = rotate_vector(float3(1, 0, 0), rotate_angle_axis(t2 * UNITY_PI * 2, float3(0, 0, 1)));
	float3 n1_2 = rotate_vector(d1_2, lookA);
	float3 n2_2 = rotate_vector(d1_2, lookB);

	float3 d1_3 = rotate_vector(float3(1, 0, 0), rotate_angle_axis(t3 * UNITY_PI * 2, float3(0, 0, 1)));
	float3 n1_3 = rotate_vector(d1_3, lookA);
	float3 n2_3 = rotate_vector(d1_3, lookB);

	float3 d1_4 = rotate_vector(float3(1, 0, 0), rotate_angle_axis(t4 * UNITY_PI * 2, float3(0, 0, 1)));
	float3 n1_4 = rotate_vector(d1_4, lookA);
	float3 n2_4 = rotate_vector(d1_4, lookB);

	float3 d1_5 = rotate_vector(float3(1, 0, 0), rotate_angle_axis(t5 * UNITY_PI * 2, float3(0, 0, 1)));
	float3 n1_5 = rotate_vector(d1_5, lookA);
	float3 n2_5 = rotate_vector(d1_5, lookB);

	float3 d1_6 = rotate_vector(float3(1, 0, 0), rotate_angle_axis(t6 * UNITY_PI * 2, float3(0, 0, 1)));
	float3 n1_6 = rotate_vector(d1_6, lookA);
	float3 n2_6 = rotate_vector(d1_6, lookB);



	ring1_1.dir = n1_1;
	ring1_2.dir = n1_2;
	ring1_3.dir = n1_3;
	ring1_4.dir = n1_4;
	ring1_5.dir = n1_5;
	ring1_6.dir = n1_6;

	ring2_1.dir = n2_1;
	ring2_2.dir = n2_2;
	ring2_3.dir = n2_3;
	ring2_4.dir = n2_4;
	ring2_5.dir = n2_5;
	ring2_6.dir = n2_6;


	float3 dir_r1r2 = normalize(p3 - p2);

	float3 r1p1 = (ring1_1.center + ring1_1.dir);
	float3 r1p2 = (ring1_2.center + ring1_2.dir);
	float3 r1p3 = (ring1_3.center + ring1_3.dir);
	float3 r1p4 = (ring1_4.center + ring1_4.dir);
	float3 r1p5 = (ring1_5.center + ring1_5.dir);
	float3 r1p6 = (ring1_6.center + ring1_6.dir);

	float3 r2p1 = (ring2_1.center + ring2_1.dir);
	float3 r2p2 = (ring2_2.center + ring2_2.dir);
	float3 r2p3 = (ring2_3.center + ring2_3.dir);
	float3 r2p4 = (ring2_4.center + ring2_4.dir);
	float3 r2p5 = (ring2_5.center + ring2_5.dir);
	float3 r2p6 = (ring2_6.center + ring2_6.dir);

	float3 dir_1_1 = r1p1 - r2p1;
	float3 dir_1_2 = r1p1 - r2p2;
	float3 dir_1_3 = r1p1 - r2p3;
	float3 dir_1_4 = r1p1 - r2p4;
	float3 dir_1_5 = r1p1 - r2p5;
	float3 dir_1_6 = r1p1 - r2p6;

	float3 dir_2_1 = r1p2 - r2p1;
	float3 dir_2_2 = r1p2 - r2p2;
	float3 dir_2_3 = r1p2 - r2p3;
	float3 dir_2_4 = r1p2 - r2p4;
	float3 dir_2_5 = r1p2 - r2p5;
	float3 dir_2_6 = r1p2 - r2p6;

	float3 dir_3_1 = r1p3 - r2p1;
	float3 dir_3_2 = r1p3 - r2p2;
	float3 dir_3_3 = r1p3 - r2p3;
	float3 dir_3_4 = r1p3 - r2p4;
	float3 dir_3_5 = r1p3 - r2p5;
	float3 dir_3_6 = r1p3 - r2p6;

	float3 dir_4_1 = r1p4 - r2p1;
	float3 dir_4_2 = r1p4 - r2p2;
	float3 dir_4_3 = r1p4 - r2p3;
	float3 dir_4_4 = r1p4 - r2p4;
	float3 dir_4_5 = r1p4 - r2p5;
	float3 dir_4_6 = r1p4 - r2p6;

	float3 dir_5_1 = r1p5 - r2p1;
	float3 dir_5_2 = r1p5 - r2p2;
	float3 dir_5_3 = r1p5 - r2p3;
	float3 dir_5_4 = r1p5 - r2p4;
	float3 dir_5_5 = r1p5 - r2p5;
	float3 dir_5_6 = r1p5 - r2p6;

	float3 dir_6_1 = r1p6 - r2p1;
	float3 dir_6_2 = r1p6 - r2p2;
	float3 dir_6_3 = r1p6 - r2p3;
	float3 dir_6_4 = r1p6 - r2p4;
	float3 dir_6_5 = r1p6 - r2p5;
	float3 dir_6_6 = r1p6 - r2p6;


	float distance_1_1 = length(dir_1_1);
	float distance_1_2 = length(dir_1_2);
	float distance_1_3 = length(dir_1_3);
	float distance_1_4 = length(dir_1_4);
	float distance_1_5 = length(dir_1_5);
	float distance_1_6 = length(dir_1_6);

	float distance_2_1 = length(dir_2_1);
	float distance_2_2 = length(dir_2_2);
	float distance_2_3 = length(dir_2_3);
	float distance_2_4 = length(dir_2_4);
	float distance_2_5 = length(dir_2_5);
	float distance_2_6 = length(dir_2_6);

	float distance_3_1 = length(dir_3_1);
	float distance_3_2 = length(dir_3_2);
	float distance_3_3 = length(dir_3_3);
	float distance_3_4 = length(dir_3_4);
	float distance_3_5 = length(dir_3_5);
	float distance_3_6 = length(dir_3_6);

	float distance_4_1 = length(dir_4_1);
	float distance_4_2 = length(dir_4_2);
	float distance_4_3 = length(dir_4_3);
	float distance_4_4 = length(dir_4_4);
	float distance_4_5 = length(dir_4_5);
	float distance_4_6 = length(dir_4_6);

	float distance_5_1 = length(dir_5_1);
	float distance_5_2 = length(dir_5_2);
	float distance_5_3 = length(dir_5_3);
	float distance_5_4 = length(dir_5_4);
	float distance_5_5 = length(dir_5_5);
	float distance_5_6 = length(dir_5_6);

	float distance_6_1 = length(dir_6_1);
	float distance_6_2 = length(dir_6_2);
	float distance_6_3 = length(dir_6_3);
	float distance_6_4 = length(dir_6_4);
	float distance_6_5 = length(dir_6_5);
	float distance_6_6 = length(dir_6_6);


	distance_1_1 = (1 - abs(dot(dir_r1r2, dir_1_1 / max(0.0000001, distance_1_1)))) * min(1, distance_1_1);
	distance_1_2 = (1 - abs(dot(dir_r1r2, dir_1_2 / max(0.0000001, distance_1_2)))) * min(1, distance_1_2);
	distance_1_3 = (1 - abs(dot(dir_r1r2, dir_1_3 / max(0.0000001, distance_1_3)))) * min(1, distance_1_3);
	distance_1_4 = (1 - abs(dot(dir_r1r2, dir_1_4 / max(0.0000001, distance_1_4)))) * min(1, distance_1_4);
	distance_1_5 = (1 - abs(dot(dir_r1r2, dir_1_5 / max(0.0000001, distance_1_5)))) * min(1, distance_1_5);
	distance_1_6 = (1 - abs(dot(dir_r1r2, dir_1_6 / max(0.0000001, distance_1_6)))) * min(1, distance_1_6);

	distance_2_1 = (1 - abs(dot(dir_r1r2, dir_2_1 / max(0.0000001, distance_2_1)))) * min(1, distance_2_1);
	distance_2_2 = (1 - abs(dot(dir_r1r2, dir_2_2 / max(0.0000001, distance_2_2)))) * min(1, distance_2_2);
	distance_2_3 = (1 - abs(dot(dir_r1r2, dir_2_3 / max(0.0000001, distance_2_3)))) * min(1, distance_2_3);
	distance_2_4 = (1 - abs(dot(dir_r1r2, dir_2_4 / max(0.0000001, distance_2_4)))) * min(1, distance_2_4);
	distance_2_5 = (1 - abs(dot(dir_r1r2, dir_2_5 / max(0.0000001, distance_2_5)))) * min(1, distance_2_5);
	distance_2_6 = (1 - abs(dot(dir_r1r2, dir_2_6 / max(0.0000001, distance_2_6)))) * min(1, distance_2_6);

	distance_3_1 = (1 - abs(dot(dir_r1r2, dir_3_1 / max(0.0000001, distance_3_1)))) * min(1, distance_3_1);
	distance_3_2 = (1 - abs(dot(dir_r1r2, dir_3_2 / max(0.0000001, distance_3_2)))) * min(1, distance_3_2);
	distance_3_3 = (1 - abs(dot(dir_r1r2, dir_3_3 / max(0.0000001, distance_3_3)))) * min(1, distance_3_3);
	distance_3_4 = (1 - abs(dot(dir_r1r2, dir_3_4 / max(0.0000001, distance_3_4)))) * min(1, distance_3_4);
	distance_3_5 = (1 - abs(dot(dir_r1r2, dir_3_5 / max(0.0000001, distance_3_5)))) * min(1, distance_3_5);
	distance_3_6 = (1 - abs(dot(dir_r1r2, dir_3_6 / max(0.0000001, distance_3_6)))) * min(1, distance_3_6);

	distance_4_1 = (1 - abs(dot(dir_r1r2, dir_4_1 / max(0.0000001, distance_4_1)))) * min(1, distance_4_1);
	distance_4_2 = (1 - abs(dot(dir_r1r2, dir_4_2 / max(0.0000001, distance_4_2)))) * min(1, distance_4_2);
	distance_4_3 = (1 - abs(dot(dir_r1r2, dir_4_3 / max(0.0000001, distance_4_3)))) * min(1, distance_4_3);
	distance_4_4 = (1 - abs(dot(dir_r1r2, dir_4_4 / max(0.0000001, distance_4_4)))) * min(1, distance_4_4);
	distance_4_5 = (1 - abs(dot(dir_r1r2, dir_4_5 / max(0.0000001, distance_4_5)))) * min(1, distance_4_5);
	distance_4_6 = (1 - abs(dot(dir_r1r2, dir_4_6 / max(0.0000001, distance_4_6)))) * min(1, distance_4_6);

	distance_5_1 = (1 - abs(dot(dir_r1r2, dir_5_1 / max(0.0000001, distance_5_1)))) * min(1, distance_5_1);
	distance_5_2 = (1 - abs(dot(dir_r1r2, dir_5_2 / max(0.0000001, distance_5_2)))) * min(1, distance_5_2);
	distance_5_3 = (1 - abs(dot(dir_r1r2, dir_5_3 / max(0.0000001, distance_5_3)))) * min(1, distance_5_3);
	distance_5_4 = (1 - abs(dot(dir_r1r2, dir_5_4 / max(0.0000001, distance_5_4)))) * min(1, distance_5_4);
	distance_5_5 = (1 - abs(dot(dir_r1r2, dir_5_5 / max(0.0000001, distance_5_5)))) * min(1, distance_5_5);
	distance_5_6 = (1 - abs(dot(dir_r1r2, dir_5_6 / max(0.0000001, distance_5_6)))) * min(1, distance_5_6);

	distance_6_1 = (1 - abs(dot(dir_r1r2, dir_6_1 / max(0.0000001, distance_6_1)))) * min(1, distance_6_1);
	distance_6_2 = (1 - abs(dot(dir_r1r2, dir_6_2 / max(0.0000001, distance_6_2)))) * min(1, distance_6_2);
	distance_6_3 = (1 - abs(dot(dir_r1r2, dir_6_3 / max(0.0000001, distance_6_3)))) * min(1, distance_6_3);
	distance_6_4 = (1 - abs(dot(dir_r1r2, dir_6_4 / max(0.0000001, distance_6_4)))) * min(1, distance_6_4);
	distance_6_5 = (1 - abs(dot(dir_r1r2, dir_6_5 / max(0.0000001, distance_6_5)))) * min(1, distance_6_5);
	distance_6_6 = (1 - abs(dot(dir_r1r2, dir_6_6 / max(0.0000001, distance_6_6)))) * min(1, distance_6_6);


	int closest_1 = selectIndexFromLeastDistance(distance_1_1, distance_1_2, distance_1_3, distance_1_4, distance_1_5, distance_1_6);
	setPaired(closest_1, 0, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	ring1_1.equivalentIndex = closest_1;
	ring1_1.paired = 2;

	int closest_2 = selectIndexFromLeastDistance(distance_2_1, distance_2_2, distance_2_3, distance_2_4, distance_2_5, distance_2_6);
	setPaired(closest_2, 1, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	ring1_2.equivalentIndex = closest_2;
	ring1_2.paired = 2;

	int closest_3 = selectIndexFromLeastDistance(distance_3_1, distance_3_2, distance_3_3, distance_3_4, distance_3_5, distance_3_6);
	setPaired(closest_3, 2, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	ring1_3.equivalentIndex = closest_3;
	ring1_3.paired = 2;

	int closest_4 = selectIndexFromLeastDistance(distance_4_1, distance_4_2, distance_4_3, distance_4_4, distance_4_5, distance_4_6);
	setPaired(closest_4, 3, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	ring1_4.equivalentIndex = closest_4;
	ring1_4.paired = 2;

	int closest_5 = selectIndexFromLeastDistance(distance_5_1, distance_5_2, distance_5_3, distance_5_4, distance_5_5, distance_5_6);
	setPaired(closest_5, 4, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	ring1_5.equivalentIndex = closest_5;
	ring1_5.paired = 2;

	int closest_6 = selectIndexFromLeastDistance(distance_6_1, distance_6_2, distance_6_3, distance_6_4, distance_6_5, distance_6_6);
	setPaired(closest_6, 5, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	ring1_6.equivalentIndex = closest_6;
	ring1_6.paired = 2;

	addTriangles(IN, triStream, T2, T3, ring1_6, ring1_1, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	addTriangles(IN, triStream, T2, T3, ring1_1, ring1_2, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	addTriangles(IN, triStream, T2, T3, ring1_2, ring1_3, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	addTriangles(IN, triStream, T2, T3, ring1_3, ring1_4, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	addTriangles(IN, triStream, T2, T3, ring1_4, ring1_5, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);
	addTriangles(IN, triStream, T2, T3, ring1_5, ring1_6, ring2_1, ring2_2, ring2_3, ring2_4, ring2_5, ring2_6);

}