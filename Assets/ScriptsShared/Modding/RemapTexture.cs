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

[ExecuteInEditMode]
public class RemapTexture : MonoBehaviour
{

    [Serializable]
    public struct UVChannelInfo
    {

        public UVChannelURP channelIndex;

        public bool useZW;

    }

    public bool bake;

    public string savePath;

    public TextureFormat outputFormat = TextureFormat.RGBA32;

    public bool flipIndexUV_V = false;

    public int padding = 5;

    public SkinnedMeshRenderer referenceRenderer;

    public UVChannelInfo referenceUVChannel;

    public SkinnedMeshRenderer targetRenderer;

    public UVChannelInfo targetUVChannel;

    public Texture2D[] inputTextures;

    public Texture2D[] outputTextures;

    public void Update()
    {

        if (bake)
        {

            bake = false;

            Bake();

        }

    }

    protected struct RemapData
    {

        public int3 indices;

        public float3 weights;

    }

    public void Bake()
    {

#if UNITY_EDITOR

        if (inputTextures == null || inputTextures.Length <= 0 || referenceRenderer == null || targetRenderer == null) return;

        if (outputTextures == null || outputTextures.Length != inputTextures.Length)
        {

            Texture2D[] newArray = new Texture2D[inputTextures.Length];

            for (int a = 0; a < Mathf.Min(newArray.Length, outputTextures.Length); a++) newArray[a] = outputTextures[a];

            outputTextures = newArray;

        }

        Mesh referenceMesh = referenceRenderer.sharedMesh;
        Mesh targetMesh = targetRenderer.sharedMesh;

        Vector3[] referenceWorldVertices = RealtimeMesh.GetTransformedVertices(referenceRenderer);
        Vector3[] targetWorldVertices = RealtimeMesh.GetTransformedVertices(targetRenderer);

        int[] referenceTriangles = referenceMesh.triangles;
        int[] targetTriangles = targetMesh.triangles;

        RemapData[] remapData = new RemapData[targetMesh.vertexCount];

        for (int a = 0; a < targetWorldVertices.Length; a++)
        {

            Vector3 targetVertex = targetWorldVertices[a];
            RemapData data = new RemapData();

            float minDist = float.MaxValue;

            for (int b = 0; b < referenceTriangles.Length; b += 3)
            {

                int i0 = referenceTriangles[b];
                int i1 = referenceTriangles[b + 1];
                int i2 = referenceTriangles[b + 2];

                Vector3 referenceVertexA = referenceWorldVertices[i0];
                Vector3 referenceVertexB = referenceWorldVertices[i1];
                Vector3 referenceVertexC = referenceWorldVertices[i2];

                float dist = (((referenceVertexA + referenceVertexB + referenceVertexC) / 3f) - targetVertex).sqrMagnitude;

                if (dist < minDist)
                {

                    minDist = dist;
                    data.indices = new int3(i0, i1, i2);

                }

            }

            data.weights = Maths.BarycentricCoords(targetVertex, referenceWorldVertices[data.indices.x], referenceWorldVertices[data.indices.y], referenceWorldVertices[data.indices.z]);

            remapData[a] = data;

        }

        List<Vector4> referenceUV = Swole.MeshUtils.GetUVsByChannelAsListV4(referenceMesh, (int)referenceUVChannel.channelIndex);
        List<Vector4> targetUV = Swole.MeshUtils.GetUVsByChannelAsListV4(targetMesh, (int)targetUVChannel.channelIndex);

        for(int a = 0; a < inputTextures.Length; a++)
        {

            var texIn = inputTextures[a];
            var texOut = outputTextures[a];

            bool isNew = false;
            if (texOut == null)
            {

                isNew = true;

                texOut = new Texture2D(texIn.width, texIn.height, outputFormat/*texIn.format*/, texIn.mipmapCount > 1, texIn.graphicsFormat.ToString().ToLower().IndexOf("srgb") < 0);
                texOut.name = texIn.name + "_remap_" + targetMesh.name;

                outputTextures[a] = texOut;

            }

            Color[] referencePixels = texIn.GetPixels();

            int width = texOut.width;
            int height = texOut.height;
            int FlattenIndex(int pixelX, int pixelY)
            {

                while (pixelX < 0) pixelX += width;
                while (pixelX >= width) pixelX -= width;

                while (pixelY < 0) pixelY += height;
                while (pixelY >= height) pixelY -= height;

                return (pixelY * width) + pixelX;

            }

            Color SampleReference(float2 uv)
            {

                int ix = Mathf.RoundToInt(uv.x * width);
                int iy = Mathf.RoundToInt((flipIndexUV_V ? (1 - uv.y) : uv.y) * height);

                return referencePixels[FlattenIndex(ix, iy)];

            }

            Color[] pixels = texOut.GetPixels();
            for (int b = 0; b < pixels.Length; b++) pixels[b] = new Color(0, 0, 0, -1);

            for (int b = 0; b < targetTriangles.Length; b += 3)
            {

                int i0 = targetTriangles[b];
                int i1 = targetTriangles[b + 1];
                int i2 = targetTriangles[b + 2];

                RemapData data0 = remapData[i0];
                RemapData data1 = remapData[i1];
                RemapData data2 = remapData[i2];

                var refUV0 = (referenceUV[data0.indices.x] * data0.weights.x) + (referenceUV[data0.indices.y] * data0.weights.y) + (referenceUV[data0.indices.z] * data0.weights.z);
                var refUV1 = (referenceUV[data1.indices.x] * data1.weights.x) + (referenceUV[data1.indices.y] * data1.weights.y) + (referenceUV[data1.indices.z] * data1.weights.z);
                var refUV2 = (referenceUV[data2.indices.x] * data2.weights.x) + (referenceUV[data2.indices.y] * data2.weights.y) + (referenceUV[data2.indices.z] * data2.weights.z);

                var uv0 = targetUVChannel.useZW ? new Vector2(targetUV[i0].z, targetUV[i0].w) : new Vector2(targetUV[i0].x, targetUV[i0].y);
                var uv1 = targetUVChannel.useZW ? new Vector2(targetUV[i1].z, targetUV[i1].w) : new Vector2(targetUV[i1].x, targetUV[i1].y);
                var uv2 = targetUVChannel.useZW ? new Vector2(targetUV[i2].z, targetUV[i2].w) : new Vector2(targetUV[i2].x, targetUV[i2].y);

                Color pixel0 = SampleReference(referenceUVChannel.useZW ? new float2(refUV0.z, refUV0.w) : new float2(refUV0.x, refUV0.y));
                Color pixel1 = SampleReference(referenceUVChannel.useZW ? new float2(refUV1.z, refUV1.w) : new float2(refUV1.x, refUV1.y));
                Color pixel2 = SampleReference(referenceUVChannel.useZW ? new float2(refUV2.z, refUV2.w) : new float2(refUV2.x, refUV2.y));

                int minPixelX = Mathf.FloorToInt(Mathf.Min(uv0.x * width, uv1.x * width, uv2.x * width));
                int maxPixelX = Mathf.CeilToInt(Mathf.Max(uv0.x * width, uv1.x * width, uv2.x * width));

                int minPixelY = Mathf.FloorToInt(Mathf.Min(uv0.y * height, uv1.y * height, uv2.y * height));
                int maxPixelY = Mathf.CeilToInt(Mathf.Max(uv0.y * height, uv1.y * height, uv2.y * height)); 

                for (int x = minPixelX; x <= maxPixelX; x++)
                {

                    for (int y = minPixelY; y <= maxPixelY; y++)
                    {

                        float uvx = x / (float)width;
                        float uvy = y / (float)height;

                        float3 coords = Maths.BarycentricCoords(new float3(uvx, uvy, 0), new float3(uv0.x, uv0.y, 0), new float3(uv1.x, uv1.y, 0), new float3(uv2.x, uv2.y, 0));

                        if (coords.x < 0 || coords.y < 0 || coords.z < 0) continue;

                        int index = FlattenIndex(x, flipIndexUV_V ? (height - y) : y);

                        pixels[index] = (pixel0 * coords.x) + (pixel1 * coords.y) + (pixel2 * coords.z);

                    } 

                }

            }

            Color[] finalPixels = new Color[pixels.Length];

            Color Pad(int startX, int startY, int paddingSize)
            {

                Color pixel = Color.clear;

                float totalWeight = 0;
                for(int x = -paddingSize; x <= paddingSize; x++)
                {

                    for (int y = -paddingSize; y <= paddingSize; y++)
                    {

                        float weight = math.max(0, 1 - (math.length(new float2(x, y)) / paddingSize));

                        int indexB = FlattenIndex(startX + x, startY + y);

                        Color pix = pixels[indexB];
                        pix.a = math.max(0, pix.a);

                        pixel = pixel + pix * weight;

                        totalWeight += weight;

                    }

                }

                return totalWeight > 0 ? (pixel / totalWeight) : pixel;

            }

            for (int x = 0; x < width; x++)
            {

                for (int y = 0; y < height; y++)
                {

                    int index = FlattenIndex(x, y);

                    Color pixel = pixels[index];

                    if (pixel.a >= 0)
                    {

                        finalPixels[index] = pixel;

                        continue;

                    }

                    finalPixels[index] = Pad(x, y, padding);

                }

            } 

            texOut.SetPixels(finalPixels);
            texOut.Apply();


            if (isNew)
            {

                if (string.IsNullOrEmpty(savePath)) savePath = "";

                AssetDatabase.CreateAsset(texOut, (savePath.StartsWith("Assets") ? "" : "Assets/") + savePath + (savePath.EndsWith("/") ? "" : "/") + $"{texOut.name}.asset");

            }
            else
            {

                AssetDatabase.SaveAssetIfDirty(texOut);

            } 

        }
         
        AssetDatabase.Refresh();

#endif

    }

}
