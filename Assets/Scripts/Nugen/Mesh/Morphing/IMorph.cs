using System;

using System.Runtime.Serialization;

using Unity.Collections;
using Unity.Mathematics;

using nugen.animation;
using nugen.mesh.data;

namespace nugen.mesh.morphing
{

    public interface IMorph<T> where T : struct, IKeyFramed, IDisposable, ISerializable
    {

        public int Translate(int index, int frameIndex);

        public int4 Translate(int4 index, int4 frameIndex);

        public int GetVertexCount();

    }

    public static class morphutil
    {

        public static void CheckFrameWeights(ref float[] frameWeights, int frameCount)
        {

            if (frameWeights == null)
            {

                frameWeights = new float[frameCount];

                for (int a = 0; a < frameCount; a++) frameWeights[a] = (a + 1f) / frameCount;

            }
            else if (frameWeights.Length < frameCount)
            {

                float lmax = frameWeights.Length / (float)frameCount;

                float wmax = frameWeights[frameWeights.Length - 1];

                float nmax = wmax * (1 / lmax);

                float[] nframeWeights = new float[frameCount];

                for (int a = 0; a < frameCount; a++)
                {

                    if (a < frameWeights.Length)
                    {

                        nframeWeights[a] = frameWeights[a];

                        continue;

                    }

                    float t = (a - (frameWeights.Length - 1f)) / (frameCount - frameWeights.Length);

                    nframeWeights[a] = wmax + (nmax - wmax) * t;

                }

                frameWeights = nframeWeights;

            }
            else if (frameWeights.Length > frameCount)
            {

                float[] nframeWeights = new float[frameCount];

                for (int a = 0; a < frameCount; a++) nframeWeights[a] = frameWeights[a];

                frameWeights = nframeWeights;

            }

        }

        public static float3[][] GetDeltaVerticesF3(this BlendShape shape)
        {

            float3[][] deltaVertices = new float3[shape.frames.Length][];

            for (int a = 0; a < deltaVertices.Length; a++)
            {

                BlendShape.Frame frame = shape.frames[a];

                float3[] array = new float3[frame.deltaVertices.Length];

                for (int b = 0; b < array.Length; b++) array[b] = frame.deltaVertices[b];

                deltaVertices[a] = array;

            }

            return deltaVertices;

        }

        public static float3[][] GetDeltaNormalsF3(this BlendShape shape)
        {

            float3[][] deltaNormals = new float3[shape.frames.Length][];

            for (int a = 0; a < deltaNormals.Length; a++)
            {

                BlendShape.Frame frame = shape.frames[a];

                float3[] array = new float3[frame.deltaNormals.Length];

                for (int b = 0; b < array.Length; b++) array[b] = frame.deltaNormals[b];

                deltaNormals[a] = array;

            }

            return deltaNormals;

        }

        public static float3[][] GetDeltaTangentsF3(this BlendShape shape)
        {

            float3[][] deltaTangents = new float3[shape.frames.Length][];

            for (int a = 0; a < deltaTangents.Length; a++)
            {

                BlendShape.Frame frame = shape.frames[a];

                float3[] array = new float3[frame.deltaTangents.Length];

                for (int b = 0; b < array.Length; b++) array[b] = frame.deltaTangents[b];

                deltaTangents[a] = array;

            }

            return deltaTangents;

        }

    }

    public static class packedmorph
    {

        public static void GetObjectData(SerializationInfo info, StreamingContext context, int vertexCount, NativeArray<int> indexTranslator, NativeArray<float> frameWeights, NativeArray<int> frameOffsets, NativeArray<float4> deltas)
        {

            info.AddValue("vertexCount", vertexCount, typeof(int));

            info.AddValue("indexTranslator", indexTranslator.ToArray(), typeof(int[]));

            info.AddValue("frameWeights", frameWeights.ToArray(), typeof(float[]));
            info.AddValue("frameOffsets", frameOffsets.ToArray(), typeof(int[]));
            info.AddValue("deltas", deltas.ToArray(), typeof(float4[]));

        }

        public static void SetObjectData(SerializationInfo info, StreamingContext context, out int vertexCount, out NativeArray<int> indexTranslator, out NativeArray<float> frameWeights, out NativeArray<int> frameOffsets, out NativeArray<float4> deltas)
        {

            vertexCount = info.GetInt32("vertexCount");

            indexTranslator = new NativeArray<int>((int[])info.GetValue("indexTranslator", typeof(int[])), Allocator.Persistent);

            frameWeights = new NativeArray<float>((float[])info.GetValue("frameWeights", typeof(float[])), Allocator.Persistent);
            frameOffsets = new NativeArray<int>((int[])info.GetValue("frameOffsets", typeof(int[])), Allocator.Persistent);
            deltas = new NativeArray<float4>((float4[])info.GetValue("deltas", typeof(float4[])), Allocator.Persistent);

        }

        public static int Translate(NativeArray<int> indexTranslator, int index, int frameIndex, int vertexCount)
        {

            return indexTranslator[index + (frameIndex * vertexCount)];

        }

        public static int4 Translate(NativeArray<int> indexTranslator, int4 index, int4 frameIndex, int vertexCount)
        {

            return new int4(
                indexTranslator[index.x + (frameIndex.x * vertexCount)],
                indexTranslator[index.y + (frameIndex.y * vertexCount)],
                indexTranslator[index.z + (frameIndex.z * vertexCount)],
                indexTranslator[index.w + (frameIndex.w * vertexCount)]);

        }

        public static float Get(NativeArray<int> indexTranslator, NativeArray<float4> deltas, int index, int frameIndex, int dataOffset, int vertexCount)
        {

            int pioffset = index % 4;

            int pi = Translate(indexTranslator, index, frameIndex, vertexCount);

            int i = pi + dataOffset;

            return math.select(deltas[i].x, math.select(deltas[i].y, math.select(deltas[i].z, deltas[i].w, pioffset < 3), pioffset < 2), pioffset < 1);

        }

        public static float4 GetPacked(NativeArray<int> indexTranslator, NativeArray<float4> deltas, int index, int frameIndex, int dataOffset, int vertexCount)
        {

            int pi = Translate(indexTranslator, index, frameIndex, vertexCount);

            return deltas[pi + dataOffset];

        }

        public static void GetPacked(NativeArray<int> indexTranslator, NativeArray<float4> deltas, int index, int frameIndex, int vertexCount, out float4 x, out float4 y, out float4 z, out float4 w)
        {

            int ti = Translate(indexTranslator, index, frameIndex, vertexCount);

            x = deltas[ti];
            y = deltas[ti + 1];
            z = deltas[ti + 2];
            w = deltas[ti + 3];

        }

        public static void GetPacked(NativeArray<float4> deltas, int4 indicesX, out float4 x, out float4 y, out float4 z, out float4 w)
        {

            int4 indicesY = indicesX + 1;
            int4 indicesZ = indicesX + 2;
            int4 indicesW = indicesX + 3;

            x = new float4(deltas[indicesX.x].x, deltas[indicesX.y].y, deltas[indicesX.z].z, deltas[indicesX.w].w);
            y = new float4(deltas[indicesY.x].x, deltas[indicesY.y].y, deltas[indicesY.z].z, deltas[indicesY.w].w);
            z = new float4(deltas[indicesZ.x].x, deltas[indicesZ.y].y, deltas[indicesZ.z].z, deltas[indicesZ.w].w);
            w = new float4(deltas[indicesW.x].x, deltas[indicesW.y].y, deltas[indicesW.z].z, deltas[indicesW.w].w);

        }

        public static void GetPacked(NativeArray<int> indexTranslator, NativeArray<float4> deltas, int index, int frameIndex, int vertexCount, out float4 pX, out float4 pY, out float4 pZ, out float4 nX, out float4 nY, out float4 nZ, out float4 tX, out float4 tY, out float4 tZ)
        {

            int ti = Translate(indexTranslator, index, frameIndex, vertexCount);

            pX = deltas[ti];
            pY = deltas[ti + 1];
            pZ = deltas[ti + 2];

            nX = deltas[ti + 3];
            nY = deltas[ti + 4];
            nZ = deltas[ti + 5];

            tX = deltas[ti + 6];
            tY = deltas[ti + 7];
            tZ = deltas[ti + 8];

        }

        public static void GetPacked(NativeArray<float4> deltas, int4 indicesXp, out float4 pX, out float4 pY, out float4 pZ, out float4 nX, out float4 nY, out float4 nZ, out float4 tX, out float4 tY, out float4 tZ)
        {

            int4 indicesYp = indicesXp + 1;
            int4 indicesZp = indicesXp + 2;

            int4 indicesXn = indicesXp + 3;
            int4 indicesYn = indicesXn + 1;
            int4 indicesZn = indicesXn + 2;

            int4 indicesXt = indicesXp + 6;
            int4 indicesYt = indicesXt + 1;
            int4 indicesZt = indicesXt + 2;

            pX = new float4(deltas[indicesXp.x].x, deltas[indicesXp.y].y, deltas[indicesXp.z].z, deltas[indicesXp.w].w);
            pY = new float4(deltas[indicesYp.x].x, deltas[indicesYp.y].y, deltas[indicesYp.z].z, deltas[indicesYp.w].w);
            pZ = new float4(deltas[indicesZp.x].x, deltas[indicesZp.y].y, deltas[indicesZp.z].z, deltas[indicesZp.w].w);

            nX = new float4(deltas[indicesXn.x].x, deltas[indicesXn.y].y, deltas[indicesXn.z].z, deltas[indicesXn.w].w);
            nY = new float4(deltas[indicesYn.x].x, deltas[indicesYn.y].y, deltas[indicesYn.z].z, deltas[indicesYn.w].w);
            nZ = new float4(deltas[indicesZn.x].x, deltas[indicesZn.y].y, deltas[indicesZn.z].z, deltas[indicesZn.w].w);

            tX = new float4(deltas[indicesXt.x].x, deltas[indicesXt.y].y, deltas[indicesXt.z].z, deltas[indicesXt.w].w);
            tY = new float4(deltas[indicesYt.x].x, deltas[indicesYt.y].y, deltas[indicesYt.z].z, deltas[indicesYt.w].w);
            tZ = new float4(deltas[indicesZt.x].x, deltas[indicesZt.y].y, deltas[indicesZt.z].z, deltas[indicesZt.w].w);

        }

    }

}
