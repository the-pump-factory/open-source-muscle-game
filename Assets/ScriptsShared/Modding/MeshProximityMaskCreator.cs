using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

using Swole;

using Swole.DataStructures;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// TODO: Add moveable sphere bounds that mask the area inside them
/// </summary>
[ExecuteInEditMode] 
public class MeshProximityMaskCreator : MonoBehaviour
{

    public MeshProximityMaskCreator parent;

    [Serializable]
    public struct MaskingSphere
    {
        public Vector3 worldPosition;
        public float worldRadius;
        public float fadeInRadiusAdd;
        public AnimationCurve fadeInCurve;
    }
    [Serializable]
    public class MaskingSphereGroup
    {
        public string name = "newMaskingGroup";
        public Color color = Color.yellow; 
        public MaskingSphere[] spheres;
    }

    public bool create;

    public string savePath;
    public string maskName;

    public SkinnedMeshRenderer maskableRenderer;
    public CustomSkinnedMeshRenderer customMaskableRenderer;

    [Serializable]
    public class MaskingObject
    {

        public string name;

        public float maxProximityDistanceBelowFace = 0.07f;
        public float maxProximityDistanceAboveFace = 0;

        public SkinnedMeshRenderer renderer;

    }

    public MaskingObject[] maskingObjects;

    public int2 textureDimensions = 1024;
    public bool interpolatePixels = false;
    public UVChannelURP uvChannel = UVChannelURP.UV0;
    public TextureFormat textureFormat = TextureFormat.R16;
    public bool mipMaps = false;
    public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
    public FilterMode filterMode = FilterMode.Point;

    [Tooltip("Amount of padding in pixels")]
    public int padding = 3;

    public Texture2D combinedMaskOutput;
    public Texture2D[] individualMasksOutput; 

    public bool saveIndividualMasks;

    public MaskingSphereGroup[] maskingSphereGroups;

#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        if (maskingSphereGroups != null)
        {

            foreach (var group in maskingSphereGroups)
            {
                if (group.spheres == null || group.spheres.Length <= 0) continue;
                var col = group.color;
                col.a = 0.3f * col.a;
                Gizmos.color = col;
                foreach (var sphere in group.spheres) 
                { 
                    Gizmos.DrawSphere(sphere.worldPosition, sphere.worldRadius);
                    if (sphere.fadeInRadiusAdd > 0) Gizmos.DrawRay(sphere.worldPosition, Vector3.up * (sphere.worldRadius + sphere.fadeInRadiusAdd));
                }
            }
        }
    }
#endif

    public void Update()
    {

        if (create)
        {

            create = false;
             
            if (customMaskableRenderer == null)
            {

                if (parent == null)
                {
                    CreateMask(maskName, out combinedMaskOutput, out individualMasksOutput, maskableRenderer, maskingObjects, maskingSphereGroups, textureDimensions, interpolatePixels, uvChannel, textureFormat, mipMaps, wrapMode, filterMode, padding);
                } 
                else
                {
                    CreateMask(maskName, out combinedMaskOutput, out individualMasksOutput, maskableRenderer, parent.maskingObjects, parent.maskingSphereGroups, textureDimensions, interpolatePixels, uvChannel, textureFormat, mipMaps, wrapMode, filterMode, padding);
                }

            } 
            else
            {

                if (parent == null)
                {
                    CreateMask(maskName, out combinedMaskOutput, out individualMasksOutput, customMaskableRenderer, maskingObjects, maskingSphereGroups, textureDimensions, interpolatePixels, uvChannel, textureFormat, mipMaps, wrapMode, filterMode, padding);
                }
                else
                {
                    CreateMask(maskName, out combinedMaskOutput, out individualMasksOutput, customMaskableRenderer, parent.maskingObjects, parent.maskingSphereGroups, textureDimensions, interpolatePixels, uvChannel, textureFormat, mipMaps, wrapMode, filterMode, padding);
                }

            }

#if UNITY_EDITOR

            if (string.IsNullOrEmpty(savePath)) savePath = "";

            AssetDatabase.CreateAsset(combinedMaskOutput, (savePath.StartsWith("Assets") ? "" : "Assets/") + savePath + (savePath.EndsWith("/") ? "" : "/") + $"{combinedMaskOutput.name}.asset");

            AssetDatabase.SaveAssetIfDirty(combinedMaskOutput);

#endif

        }

#if UNITY_EDITOR

        if (saveIndividualMasks)
        {

            if (individualMasksOutput != null)
            {

                for (int a = 0; a < individualMasksOutput.Length; a++)
                {

                    Texture2D tex = individualMasksOutput[a];
                    if (!AssetDatabase.Contains(tex))
                    {

                        if (string.IsNullOrEmpty(savePath)) savePath = "";

                        AssetDatabase.CreateAsset(tex, (savePath.StartsWith("Assets") ? "" : "Assets/") + savePath + (savePath.EndsWith("/") ? "" : "/") + $"{tex.name}.asset");

                    } 
                    else
                    {

                        AssetDatabase.SaveAssetIfDirty(tex);

                    }

                }

            }

        }

#endif

    }

    public static void CreateMask(string maskName, out Texture2D combinedMask, out Texture2D[] individualMasks, SkinnedMeshRenderer maskableRenderer, MaskingObject[] maskingObjects, MaskingSphereGroup[] maskingSphereGroups, int2 textureDimensions, bool interpolatePixels = false, UVChannelURP uvChannel = UVChannelURP.UV0, TextureFormat textureFormat = TextureFormat.R16, bool mipMaps = false, TextureWrapMode wrapMode = TextureWrapMode.Clamp, FilterMode filterMode = FilterMode.Bilinear, int padding = 0)
    {

        Vector3[] maskableVertices = RealtimeMesh.GetTransformedVertices(maskableRenderer);
        int[] maskableTriangles = maskableRenderer.sharedMesh.triangles;
        Vector2[] maskableUVs = Swole.MeshUtils.GetUVsByChannel(maskableRenderer.sharedMesh, (int)uvChannel);

        CreateMask(maskName, out combinedMask, out individualMasks, maskableVertices, maskableTriangles, maskableUVs, maskingObjects, maskingSphereGroups, textureDimensions, interpolatePixels, textureFormat, mipMaps, wrapMode, filterMode, padding);

    }

    public static void CreateMask(string maskName, out Texture2D combinedMask, out Texture2D[] individualMasks, CustomSkinnedMeshRenderer maskableRenderer, MaskingObject[] maskingObjects, MaskingSphereGroup[] maskingSphereGroups, int2 textureDimensions, bool interpolatePixels = false, UVChannelURP uvChannel = UVChannelURP.UV0, TextureFormat textureFormat = TextureFormat.R16, bool mipMaps = false, TextureWrapMode wrapMode = TextureWrapMode.Clamp, FilterMode filterMode = FilterMode.Bilinear, int padding = 0)
    {

        Mesh mesh = maskableRenderer.GetMesh();

        Matrix4x4 toWorld = maskableRenderer.meshRenderer == null ? maskableRenderer.transform.localToWorldMatrix : maskableRenderer.transform.localToWorldMatrix;
        Vector3[] maskableVertices = mesh.vertices;
        for (int a = 0; a < maskableVertices.Length; a++) maskableVertices[a] = toWorld.MultiplyPoint(maskableVertices[a]);
        int[] maskableTriangles = mesh.triangles;
        Vector2[] maskableUVs = Swole.MeshUtils.GetUVsByChannel(mesh, (int)uvChannel);

        CreateMask(maskName, out combinedMask, out individualMasks, maskableVertices, maskableTriangles, maskableUVs, maskingObjects, maskingSphereGroups, textureDimensions, interpolatePixels, textureFormat, mipMaps, wrapMode, filterMode, padding);

    }

    public static void CreateMask(string maskName, out Texture2D combinedMask, out Texture2D[] individualMasks, Vector3[] maskableVertices, int[] maskableTriangles, Vector2[] maskableUVs, MaskingObject[] maskingObjects, MaskingSphereGroup[] maskingSphereGroups, int2 textureDimensions, bool interpolatePixels = false, TextureFormat textureFormat = TextureFormat.R16, bool mipMaps = false, TextureWrapMode wrapMode = TextureWrapMode.Clamp, FilterMode filterMode = FilterMode.Bilinear, int padding = 0)
    {

        combinedMask = null;
        individualMasks = null;

        if (maskableVertices == null || maskableTriangles == null || maskableUVs == null || maskingObjects == null || maskableUVs.Length < maskableVertices.Length) return;

        int2 GetPixelCoordinates(Vector2 uv)
        {

            //int x = Mathf.RoundToInt(uv.x * textureDimensions.x);
            //int y = Mathf.RoundToInt(uv.y * textureDimensions.y);
            int x = Mathf.FloorToInt(uv.x * textureDimensions.x);
            int y = Mathf.FloorToInt(uv.y * textureDimensions.y);

            return new int2(x, y);

        }

        int GetPixelIndex(int pixelCoordX, int pixelCoordY) => (pixelCoordY * textureDimensions.x) + pixelCoordX;
        //int GetPixelIndex2(int2 pixelCoordinates) => GetPixelIndex(pixelCoordinates.x, pixelCoordinates.y);

        float[][] masking = new float[maskingObjects.Length + (maskingSphereGroups != null ? maskingSphereGroups.Length : 0)][];
        for (int a = 0; a < maskingObjects.Length; a++)
        {

            masking[a] = new float[maskableVertices.Length];

            var maskingObject = maskingObjects[a];
            if (maskingObject.renderer == null || maskingObject.renderer.sharedMesh == null) continue;
            Vector3[] maskingVertices = RealtimeMesh.GetTransformedVertices(maskingObject.renderer);
            int[] maskingTriangles = maskingObject.renderer.sharedMesh.triangles;

            for (int b = 0; b < maskableVertices.Length; b++)
            {
                Vector3 maskableVertex = maskableVertices[b];

                for (int c = 0; c < maskingTriangles.Length; c += 3)
                {

                    int i0 = maskingTriangles[c];
                    int i1 = maskingTriangles[c + 1];
                    int i2 = maskingTriangles[c + 2];

                    Vector3 v0 = maskingVertices[i0];
                    Vector3 v1 = maskingVertices[i1];
                    Vector3 v2 = maskingVertices[i2];

                    float3 coords = Maths.BarycentricCoords(maskableVertex, v0, v1, v2);

                    if (coords.x < 0 || coords.y < 0 || coords.z < 0) continue;

                    Vector3 normal = Maths.CalcNormal(v0, v1, v2);
                    float mag = normal.magnitude;
                    if (mag <= 0) continue;

                    normal = normal / mag;

                    Vector3 facePosition = (v0 * coords.x) + (v1 * coords.y) + (v2 * coords.z);

                    Quaternion toNormalSpace = Quaternion.FromToRotation(normal, Vector3.up);

                    Vector3 normalSpaceVertex = toNormalSpace * (maskableVertex - facePosition);
                    float proximity = -normalSpaceVertex.y;

                    if (proximity > maskingObject.maxProximityDistanceBelowFace || proximity < -maskingObject.maxProximityDistanceAboveFace) continue;

                    masking[a][b] = 1;

                    break;

                }
            }
        }
        if (maskingSphereGroups != null && maskingSphereGroups.Length > 0)
        {
            for (int g = 0; g < maskingSphereGroups.Length; g++)
            {
                var mask = new float[maskableVertices.Length];
                masking[maskingObjects.Length + g] = mask;

                var group = maskingSphereGroups[g];
                if (group.spheres == null || group.spheres.Length <= 0) continue; 

                for (int a = 0; a < group.spheres.Length; a++)
                {
                    var sphere = group.spheres[a];
                    if (sphere.worldRadius <= 0) continue;

                    for (int b = 0; b < maskableVertices.Length; b++)
                    {
                        Vector3 maskableVertex = maskableVertices[b];
                        float dist = (maskableVertex - sphere.worldPosition).magnitude;
                        mask[b] = Mathf.Max(mask[b], (dist <= sphere.worldRadius ? 1 : (sphere.fadeInRadiusAdd > 0 ? (sphere.fadeInCurve == null ? (1 - Mathf.Clamp01((dist - sphere.worldRadius) / sphere.fadeInRadiusAdd)) : sphere.fadeInCurve.Evaluate(1 - Mathf.Clamp01((dist - sphere.worldRadius) / sphere.fadeInRadiusAdd))) : 0)));
                    }
                }
            }
        }

        Color[][] maskPixels = new Color[masking.Length][];
        for (int a = 0; a < masking.Length; a++)
        {

            Color[] pixels = new Color[textureDimensions.x * textureDimensions.y];
            maskPixels[a] = pixels;
            for (int b = 0; b < maskPixels[a].Length; b++) pixels[b] = new Color(0, 0, 0, 0);

        }

        for (int a = 0; a < masking.Length; a++)
        {

            float[] maskData = masking[a];
            if (maskData == null) continue;

            Color[] pixels = maskPixels[a];
            bool[] writeStates = new bool[pixels.Length];
            if (interpolatePixels)
            {

                for (int b = 0; b < maskableTriangles.Length; b += 3)
                {

                    int i0 = maskableTriangles[b];
                    int i1 = maskableTriangles[b + 1];
                    int i2 = maskableTriangles[b + 2];

                    float values0 = maskData[i0];// ? 1 : 0;
                    float values1 = maskData[i1];// ? 1 : 0;
                    float values2 = maskData[i2];// ? 1 : 0;

                    var uv0 = maskableUVs[i0];
                    int2 pixelCoords0 = GetPixelCoordinates(uv0);
                    var uv1 = maskableUVs[i1];
                    int2 pixelCoords1 = GetPixelCoordinates(uv1);
                    var uv2 = maskableUVs[i2];
                    int2 pixelCoords2 = GetPixelCoordinates(uv2);

                    int2 min = math.max(0, math.min(math.min(pixelCoords0, pixelCoords1), pixelCoords2));
                    int2 max = math.max(math.max(pixelCoords0, pixelCoords1), pixelCoords2);
                    max.x = math.min(max.x, textureDimensions.x - 1);
                    max.y = math.min(max.y, textureDimensions.y - 1); 

                    float2 range = max - min;

                    float stepX = 0.5f / range.x;
                    float stepY = 0.5f / range.y;

                    for (float c = 0; c <= 1; c += stepX)
                    {

                        for (float d = 0; d <= 1; d += stepY)
                        {

                            float2 uv3 = math.lerp(math.lerp(uv0, uv1, c), uv2, d);

                            int pixelIndex = GetPixelIndex(Mathf.Min(textureDimensions.x - 1, Mathf.FloorToInt(uv3.x * textureDimensions.x)), Mathf.Min(textureDimensions.y - 1, Mathf.FloorToInt(uv3.y * textureDimensions.y)));

                            float maskValue = math.lerp(math.lerp(values0, values1, c), values2, d);
                            float maxValue = math.max(pixels[pixelIndex].r, maskValue);

                            pixels[pixelIndex] = new Color(maxValue, maxValue, maxValue, maxValue);
                            writeStates[pixelIndex] = true;

                        }

                    }

                }

                if (padding > 0)
                {
                    for(int step = 0; step < padding; step++)
                    {
                        for(int i = 0; i < pixels.Length; i++)
                        {
                            if (writeStates[i]) continue;

                            var pixel = pixels[i];

                            int pixY = i / textureDimensions.x;
                            int pixX = i % textureDimensions.x;

                            int j = 0;
                            for(int x = -1; x <= 1; x++)
                            {
                                for (int y = -1; y <= 1; y++)
                                {
                                    int pixXX = pixX + x;
                                    int pixYY = pixY + y; 

                                    if ((x == 0 && y == 0) || (pixXX < 0 || pixXX >= textureDimensions.x || pixYY < 0 || pixYY >= textureDimensions.y)) continue;

                                    int pixIndex = (pixYY * textureDimensions.x) + pixXX;
                                    if (!writeStates[pixIndex]) continue;

                                    var pix = pixels[pixIndex];
                                    pixel = pixel + pix;
                                    j++;
                                }
                            }

                            if (j > 0)
                            {
                                pixel = pixel / j;
                                pixels[i] = pixel;  
                            }
                        }
                        for (int i = 0; i < pixels.Length; i++)
                        {
                            var pixel = pixels[i];
                            writeStates[i] = writeStates[i] || (pixel.r > 0 || pixel.g > 0 || pixel.b > 0 || pixel.a > 0);
                        }
                    }
                }

            }
            else
            {

                for (int b = 0; b < maskableVertices.Length; b++)
                {

                    Vector2 uv = maskableUVs[b];
                    int2 pixelCoords = GetPixelCoordinates(uv);
                    int pixelIndex = GetPixelIndex(pixelCoords.x, pixelCoords.y);

                    float maskValue = maskData[b];// ? 1 : 0;

                    if (maskData[b] > 0) Debug.DrawRay(maskableVertices[b], Vector3.up * 0.1f, Color.Lerp(Color.green, Color.red, maskData[b]), 30);

                    pixels[pixelIndex] = new Color(maskValue, maskValue, maskValue, maskValue);
                    writeStates[pixelIndex] = true;

                }

            }

        }

        individualMasks = new Texture2D[masking.Length];
        for (int a = 0; a < individualMasks.Length; a++)
        {

            individualMasks[a] = new Texture2D(textureDimensions.x, textureDimensions.y, textureFormat, mipMaps, true);
            individualMasks[a].wrapMode = wrapMode;
            individualMasks[a].filterMode = filterMode;
            individualMasks[a].name = maskName + "_" + (a >= maskingObjects.Length ? (string.IsNullOrWhiteSpace(maskingSphereGroups[a - maskingObjects.Length].name) ? "null" : maskingSphereGroups[a - maskingObjects.Length].name) : (string.IsNullOrWhiteSpace(maskingObjects[a].name) ? (maskingObjects[a].renderer == null ? "null" : maskingObjects[a].renderer.name) : maskingObjects[a].name));
            individualMasks[a].SetPixels(maskPixels[a]); 
            individualMasks[a].Apply();

        }

        combinedMask = CombineMasks(textureDimensions, maskPixels, textureFormat, mipMaps, wrapMode, filterMode);
        combinedMask.name = maskName;

    }

    public static Texture2D CombineMasks(int2 textureDimensions, Texture2D[] masks, TextureFormat textureFormat = TextureFormat.R16, bool mipMaps = false, TextureWrapMode wrapMode = TextureWrapMode.Clamp, FilterMode filterMode = FilterMode.Bilinear)
    {

        Color[][] maskPixels = new Color[masks.Length][];
        for (int a = 0; a < maskPixels.Length; a++) maskPixels[a] = masks[a].GetPixels();
        return CombineMasks(textureDimensions, maskPixels, textureFormat, mipMaps, wrapMode, filterMode); 

    }

    public static Texture2D CombineMasks(int2 textureDimensions, Color[][] maskPixels, TextureFormat textureFormat = TextureFormat.R16, bool mipMaps = false, TextureWrapMode wrapMode = TextureWrapMode.Clamp, FilterMode filterMode = FilterMode.Bilinear)
    {

        if (maskPixels == null || maskPixels.Length == 0) return null; 

        int pixelCount = textureDimensions.x * textureDimensions.y;

        Color[] combinedPixels = new Color[pixelCount];
        for(int a = 0; a < maskPixels.Length; a++)
        {

            Color[] pixels = maskPixels[a];
            for(int b = 0; b < pixels.Length; b++)
            {

                Color pixel = combinedPixels[b];
                Color maskPixel = pixels[b];

                combinedPixels[b] = new Color(math.max(pixel.r, maskPixel.r), math.max(pixel.g, maskPixel.g), math.max(pixel.b, maskPixel.b), math.max(pixel.a, maskPixel.a));

            }

        }

        Texture2D combinedMask = new Texture2D(textureDimensions.x, textureDimensions.y, textureFormat, mipMaps);
        combinedMask.wrapMode = wrapMode;
        combinedMask.filterMode = filterMode;
        combinedMask.SetPixels(combinedPixels);
        combinedMask.Apply();

        return combinedMask;

    }

}
