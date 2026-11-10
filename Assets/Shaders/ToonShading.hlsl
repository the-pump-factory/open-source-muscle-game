void ToonOutlineVertex_float(float3 vertexOS, float3 normalOS, float outlineThickness, out float3 outputVertex)
{
    
    float4 clipPosition = mul(UNITY_MATRIX_MVP, float4(vertexOS, 1.0));
    float3 clipNormal = mul((float3x3) UNITY_MATRIX_VP, mul((float3x3) UNITY_MATRIX_M, normalOS));

    clipPosition.xyz += normalize(clipNormal) * outlineThickness;
    
    outputVertex = clipPosition;
    
}