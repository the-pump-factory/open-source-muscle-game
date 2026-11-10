using System.Collections.Generic;
using Unity.Collections;

using UnityEngine;

using Unity.Mathematics;

using nugen.mesh.data;
using nugen.collections;

namespace nugen.mesh
{

    [System.Serializable]
    public enum DataChannel
    {

        V = 1,
        N = 2,
        T = 4,
        UV = 8,
        BW = 16,
        CR = 32,
        CG = 64,
        CB = 128,
        CA = 256,
        F = 512,
        Main = V | N | T | BW | CR,
        Aux = CG | CB | CA

    }

    public struct Vector2INT
    {

        public int x, y;

        public Vector2INT(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static explicit operator Vector2(Vector2INT v) => new Vector2(v.x, v.y);

    }

    public struct Vector3INT
    {

        public int x, y, z;

        public Vector3INT(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static explicit operator Vector3(Vector3INT v) => new Vector3(v.x, v.y, v.z);

    }

    public struct Vector4INT
    {

        public int x, y, z, w;

        public Vector4INT(int x, int y, int z, int w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static explicit operator Vector4(Vector4INT v) => new Vector4(v.x, v.y, v.z, v.w);

    }

    public struct BoneWeightINT
    {

        public int i1, i2, i3, i4, w1, w2, w3, w4;

        public BoneWeightINT(int i1, int i2, int i3, int i4, int w1, int w2, int w3, int w4)
        {
            this.i1 = i1;
            this.i2 = i2;
            this.i3 = i3;
            this.i4 = i4;
            this.w1 = w1;
            this.w2 = w2;
            this.w3 = w3;
            this.w4 = w4;
        }

    }

    public static class MeshUtilities
    {

        public static List<BlendShape> GetBlendShapes(this Mesh mesh, List<BlendShape> list = null)
        {

            if (list == null) list = new List<BlendShape>();

            for (int a = 0; a < mesh.blendShapeCount; a++) list.Add(new BlendShape(mesh, mesh.GetBlendShapeName(a)));

            return list;

        }

        public static List<Vector2> GetUVsByChannelAsList(this Mesh mesh, int uvChannel)
        {

            List<Vector2> uv = new List<Vector2>();

            mesh.GetUVs(uvChannel, uv);

            return uv;

        }

        public static Vector2[] GetUVsByChannel(this Mesh mesh, int uvChannel)
        {

            return GetUVsByChannelAsList(mesh, uvChannel).ToArray();

        }

        public static List<Vector3> GetUVsByChannelAsListV3(this Mesh mesh, int uvChannel)
        {

            List<Vector3> uv = new List<Vector3>();

            mesh.GetUVs(uvChannel, uv);

            return uv;

        }

        public static Vector3[] GetUVsByChannelV3(this Mesh mesh, int uvChannel)
        {

            return GetUVsByChannelAsListV3(mesh, uvChannel).ToArray();

        }

        public static List<Vector4> GetUVsByChannelAsListV4(this Mesh mesh, int uvChannel)
        {

            List<Vector4> uv = new List<Vector4>();

            mesh.GetUVs(uvChannel, uv);

            return uv;

        }

        public static Vector4[] GetUVsByChannelV4(this Mesh mesh, int uvChannel)
        {

            return GetUVsByChannelAsListV4(mesh, uvChannel).ToArray();

        }

        public static Vector2INT[] ConvertToINTs(this Vector2[] array, int precision = 1000000)
        {

            return System.Array.ConvertAll(array, item => new Vector2INT((int)(item.x * precision), (int)(item.y * precision)));

        }

        public static Vector3INT[] ConvertToINTFloats(this Vector3[] array, int precision = 1000000)
        {

            return System.Array.ConvertAll(array, item => new Vector3INT((int)(item.x * precision), (int)(item.y * precision), (int)(item.z * precision)));

        }

        public static Vector4INT[] ConvertToINTs(this Vector4[] array, int precision = 1000000)
        {

            return System.Array.ConvertAll(array, item => new Vector4INT((int)(item.x * precision), (int)(item.y * precision), (int)(item.z * precision), (int)(item.w * precision)));

        }

        public static Vector2[] ConvertToFloats(this Vector2INT[] array, float precision = 1000000)
        {

            return System.Array.ConvertAll(array, item => new Vector2(item.x / precision, item.y / precision));

        }

        public static Vector3[] ConvertToFloats(this Vector3INT[] array, float precision = 1000000)
        {

            return System.Array.ConvertAll(array, item => new Vector3(item.x / precision, item.y / precision, item.z / precision));

        }

        public static Vector4[] ConvertToFloats(this Vector4INT[] array, float precision = 1000000)
        {

            return System.Array.ConvertAll(array, item => new Vector4(item.x / precision, item.y / precision, (item.z / precision), item.w / precision));

        }

        public static Vector2INT AsInts(this Vector2 v, int precision = 1000000)
        {

            return new Vector2INT((int)(v.x * precision), (int)(v.y * precision));

        }

        public static Vector3INT AsInts(this Vector3 v, int precision = 1000000)
        {

            return new Vector3INT((int)(v.x * precision), (int)(v.y * precision), (int)(v.z * precision));

        }

        public static Vector4INT AsInts(this Vector4 v, int precision = 1000000)
        {

            return new Vector4INT((int)(v.x * precision), (int)(v.y * precision), (int)(v.z * precision), (int)(v.w * precision));

        }

        public static Vector2 AsFloats(this Vector2INT v, float precision = 1000000)
        {

            return new Vector2(v.x / precision, v.y / precision);

        }

        public static Vector3 AsFloats(this Vector3INT v, float precision = 1000000)
        {

            return new Vector3(v.x / precision, v.y / precision, v.z / precision);

        }

        public static Vector4 AsFloats(this Vector4INT v, float precision = 1000000)
        {

            return new Vector4(v.x / precision, v.y / precision, v.z / precision, v.w / precision);

        }

        public static DataChannel AsDataChannel(this TextureUtilities.RGBAChannel channel)
        {

            int _channel = 0;

            void AddChannel(DataChannel __channel)
            {

                if (_channel == 0) _channel = (int)__channel; else _channel = (int)(((DataChannel)_channel) | __channel);

            }

            if (channel.HasFlag(TextureUtilities.RGBAChannel.R)) AddChannel(DataChannel.CR);
            if (channel.HasFlag(TextureUtilities.RGBAChannel.G)) AddChannel(DataChannel.CG);
            if (channel.HasFlag(TextureUtilities.RGBAChannel.B)) AddChannel(DataChannel.CB);
            if (channel.HasFlag(TextureUtilities.RGBAChannel.A)) AddChannel(DataChannel.CA);

            return (DataChannel)_channel;

        }

        public static TextureUtilities.RGBAChannel AsRGBAChannel(this DataChannel channel)
        {

            int _channel = 0;

            void AddChannel(TextureUtilities.RGBAChannel __channel)
            {

                if (_channel == 0) _channel = (int)__channel; else _channel = (int)(((TextureUtilities.RGBAChannel)_channel) | __channel);

            }

            if (channel.HasFlag(DataChannel.CR)) AddChannel(TextureUtilities.RGBAChannel.R);
            if (channel.HasFlag(DataChannel.CG)) AddChannel(TextureUtilities.RGBAChannel.G);
            if (channel.HasFlag(DataChannel.CB)) AddChannel(TextureUtilities.RGBAChannel.B);
            if (channel.HasFlag(DataChannel.CA)) AddChannel(TextureUtilities.RGBAChannel.A);

            return (TextureUtilities.RGBAChannel)_channel;

        }

        public static int GetStride(this DataChannel channel)
        {

            int stride = 0;

            if (channel.HasFlag(DataChannel.V)) stride += sizeof(float) * 3;
            if (channel.HasFlag(DataChannel.N)) stride += sizeof(float) * 3;
            if (channel.HasFlag(DataChannel.T)) stride += sizeof(float) * 3;

            if (channel.HasFlag(DataChannel.UV)) stride += sizeof(float) * 2;
            if (channel.HasFlag(DataChannel.BW)) stride += sizeof(float) * 4;

            if (channel.HasFlag(DataChannel.CR)) stride += sizeof(float);
            if (channel.HasFlag(DataChannel.CG)) stride += sizeof(float);
            if (channel.HasFlag(DataChannel.CB)) stride += sizeof(float);
            if (channel.HasFlag(DataChannel.CA)) stride += sizeof(float);

            if (channel.HasFlag(DataChannel.F)) stride += sizeof(float);

            return stride;

        }

        public static int GetCount(this DataChannel channel)
        {

            int count = 0;

            if (channel.HasFlag(DataChannel.V)) count += 3;
            if (channel.HasFlag(DataChannel.N)) count += 3;
            if (channel.HasFlag(DataChannel.T)) count += 3;

            if (channel.HasFlag(DataChannel.UV)) count += 2;
            if (channel.HasFlag(DataChannel.BW)) count += 4;

            if (channel.HasFlag(DataChannel.CR)) count++;
            if (channel.HasFlag(DataChannel.CG)) count++;
            if (channel.HasFlag(DataChannel.CB)) count++;
            if (channel.HasFlag(DataChannel.CA)) count++;

            if (channel.HasFlag(DataChannel.F)) count++;

            return count;

        }

    }

    [System.Serializable]
    public class BlendShapeReference
    {

        public string shapeName;

        [Range(0, 100)]
        public float weight;

        [System.NonSerialized]
        public float lastWeight = 0;

        [System.NonSerialized]
        public int shapeIndex = 0;

        [System.NonSerialized]
        public float maxWeight = 100;

        public BlendShapeReference(string shapeName, float weight = 0)
        {

            this.shapeName = shapeName;

            this.weight = weight;

        }

        public bool Check(SkinnedMeshRenderer renderer)
        {

            if (shapeIndex <= 0)
            {

                shapeIndex = renderer.sharedMesh.GetBlendShapeIndex(shapeName) + 1;

                if (shapeIndex <= 0) return false;

                maxWeight = renderer.sharedMesh.GetBlendShapeFrameWeight(shapeIndex - 1, renderer.sharedMesh.GetBlendShapeFrameCount(shapeIndex - 1) - 1);

            }

            return true;

        }

    }

    [System.Serializable]
    public class LockableNativeArray<T> where T : struct
    {

        public bool IsEmpty()
        {

            return array == null || array.Length == 0;

        }

        public NativeArray<T> array;

        public object lock_;

        private bool disposed = false;

        public void Dispose()
        {

            if (disposed) return;

            if (array.IsCreated) { 
                
                array.Dispose();

                disposed = true;
            
            }

        }

        public LockableNativeArray(NativeArray<T> array)
        {

            this.array = array;

            this.lock_ = new object();

        }

        public T this[int i]
        {

            get
            {

                return array[i];

            }

            set
            {

                array[i] = value;

            }

        }

    }

    [System.Serializable]
    public struct WeightedTriangle
    {

        /// <summary>
        /// Vertex indices
        /// </summary>
        public int i1, i2, i3;

        /// <summary>
        /// Weights
        /// </summary>
        public float w1, w2, w3;

        public WeightedTriangle(int i1, int i2, int i3, float w1, float w2, float w3)
        {
            this.i1 = i1;
            this.i2 = i2;
            this.i3 = i3;
            this.w1 = w1;
            this.w2 = w2;
            this.w3 = w3;
        }

    }

    /// <summary>
    /// Represents a triangle in a Unity mesh.triangles array. The "startIndex" is a multiple of 3, and represents the first index of the triangle in the original array. 
    /// The "index" is an offset that represents which corner of the triangle is being referenced. The two together add up to the original index of the corner in the triangles array.
    /// </summary>
    [System.Serializable]
    public struct TriangleIndex
    {

        public int startIndex;

        public int index;

        public TriangleIndex(int startIndex, int index)
        {
            this.startIndex = startIndex;
            this.index = index;
        }

        public int GetIndex()
        {

            return startIndex + index;

        }

    }

    [System.Serializable]
    public struct SubMeshTriangle
    {

        public int subMesh;

        public int index;

        public SubMeshTriangle(int subMesh, int index)
        {
            this.subMesh = subMesh;
            this.index = index;
        }

    }

    [System.Serializable]
    public struct VertexConnection
    {

        public TriangleIndex triIndex;

        public int index;

        public VertexConnection(TriangleIndex triIndex, int index)
        {
            this.triIndex = triIndex;
            this.index = index;
        }

    }

    [System.Serializable]
    public struct VertexClone
    {

        public int first;

        public int[] indices;

        public VertexClone(int first, int[] indices)
        {
            this.first = first;
            this.indices = indices;
        }

        public Vector3 Average(BlendShape.FrameData data)
        {

            Vector3 val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = Vector3.zero;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

        public Vector2 Average(Vector2[] data)
        {

            Vector2 val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = Vector3.zero;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

        public Vector3 Average(Vector3[] data)
        {

            Vector3 val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = Vector3.zero;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

        public Vector4 Average(Vector4[] data)
        {

            Vector4 val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = Vector4.zero;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

        public Color Average(Color[] data)
        {

            Color val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = Color.clear;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

        public float Average(float[] data)
        {

            float val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = 0;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

        public float2 Average(float2[] data)
        {

            float2 val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = float2.zero;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

        public float3 Average(float3[] data)
        {

            float3 val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = float3.zero;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

        public float4 Average(float4[] data)
        {

            float4 val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = float4.zero;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

    }

    [System.Serializable]
    public struct ConsolidatedWeights
    {

        public WeightIndexPair[] weights;

        public ConsolidatedWeights(WeightIndexPair[] weights)
        {

            this.weights = weights;

        }

        public bool IsEmpty()
        {

            return weights == null || weights.Length == 0;

        }

        public void Normalize()
        {

            float total = 0;

            foreach (WeightIndexPair pair in weights) total += pair.weight;

            for (int a = 0; a < weights.Length; a++)
            {

                WeightIndexPair pair = weights[a];

                pair.weight = pair.weight / total;

                weights[a] = pair;

            }

        }

        public ConsolidatedWeights Combine()
        {

            Dictionary<int, float> combined = new Dictionary<int, float>();

            foreach (WeightIndexPair pair in weights)
            {

                combined.TryGetValue(pair.index, out float w);

                combined[pair.index] = w + pair.weight;

            }

            List<WeightIndexPair> newWeights = new List<WeightIndexPair>();

            foreach (var pair in combined) newWeights.Add(new WeightIndexPair(pair.Key, pair.Value));

            return new ConsolidatedWeights(newWeights.ToArray());

        }

        public ConsolidatedWeights CombineAndNormalize()
        {

            ConsolidatedWeights cw = Combine();

            cw.Normalize();

            return cw;

        }

    }

}
