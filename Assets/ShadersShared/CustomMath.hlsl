uint4 as_uint4(float x)
{

    return uint4((uint)x, (uint)x, (uint)x, (uint)x);

}

uint4 as_uint4(int x)
{

    return uint4((uint)x, (uint)x, (uint)x, (uint)x);

}

uint4 as_uint4(uint x)
{

    return uint4(x, x, x, x);

}

uint as_uint(float x)
{

    return (uint)x;

}

uint4 as_uint4(float4 x)
{

    return uint4(as_uint(x.x), as_uint(x.y), as_uint(x.z), as_uint(x.w));

}

int as_int(float x)
{

    return (int)x;

}

float as_float(int x)
{

    return (float)x;

}

float as_float(uint x)
{

    return (float)x;

}

float4 as_float4(uint4 x)
{

    return float4(as_float(x.x), as_float(x.y), as_float(x.z), as_float(x.w));

}

float4 as_float4(int4 x)
{

    return float4(as_float(x.x), as_float(x.y), as_float(x.z), as_float(x.w));

}

float4 quaternion(float3x3 m)
{

    float3 c = m._m00_m10_m20;
    float3 c2 = m._m01_m11_m21;
    float3 c3 = m._m02_m12_m22;
    uint num = as_uint4(c.x) & 2147483648u;
    float x = c2.y + as_float(as_uint(c3.z) ^ num);
    uint4 uint_ = as_uint4((int)num >> 31);
    uint4 uint2_ = as_uint4(as_int(x) >> 31);
    float x2 = 1 + abs(c.x);
    uint4 rhs = uint4(0u, 2147483648u, 2147483648u, 2147483648u) ^ (uint_ & uint4(0u, 2147483648u, 0u, 2147483648u)) ^ (uint2_ & uint4(2147483648u, 2147483648u, 2147483648u, 0u));
    float4 value = float4(x2, c.y, c3.x, c2.z) + as_float4(as_uint4(float4(x, c2.x, c.z, c3.y)) ^ rhs);
    value = as_float((as_uint(value) & ~uint_) | (as_uint(value.zwxy) & uint_));
    value = as_float((as_uint(value.wzyx) & ~uint2_) | (as_uint(value) & uint2_));
    value = normalize(value);

    return value;

}

void sincos(float3 x, out float3 s, out float3 c)
{
    s = sin(x);
    c = cos(x);
}

float3 cross(float3 x, float3 y)
{
    return (x * y.yzx - x.yzx * y).yzx;
}

float3 mulq(float4 q, float3 v)
{
    float3 val = 2 * cross(q.xyz, v);
    return (v + q.w * val + cross(q.xyz, val));

}

float4 lookRotation(float3 forward, float3 up)
{
    float3 val = normalize(cross(up, forward));
    return quaternion(float3x3(val, cross(forward, val), forward));
}

float4 eulerZYX(float3 xyz)
{
    float3 s, c;
    sincos(0.5 * xyz, s, c);
    return float4(s.xyz, c.x) * c.yxxy * c.zzyz + s.yxxy * s.zzyz * float4(c.xyz, s.x) * float4(1, -1, 1, -1);
}

float4 eulerZXY(float3 xyz)
{
    float3 s, c;
    sincos(0.5 * xyz, s, c);
    return float4(s.xyz, c.x) * c.yxxy * c.zzyz + s.yxxy * s.zzyz * float4(c.xyz, s.x) * float4(1, -1, -1, 1);
}

float4 eulerZYX(float x, float y, float z)
{
    return eulerZYX(float3(x, y, z));
}

float4 eulerZXY(float x, float y, float z)
{
    return eulerZXY(float3(x, y, z));
}