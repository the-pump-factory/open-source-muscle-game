using System;

using UnityEngine;
using UnityEngine.Rendering;

using Unity.Mathematics;
using Unity.Jobs;

using nugen.collections;
using nugen.mesh.data.vertex;

namespace nugen.mesh.data
{

    public static class util
    {

        public static byte AsByte(this float f) { return (byte)math.round(math.clamp(f, 0, 1) * byte.MaxValue); }
        public static float AsFloat(this byte b) { return b / ((float)byte.MaxValue); }

        public static ushort AsUShort(this float f) { return (ushort)math.round(math.clamp(f, 0, 1) * ushort.MaxValue); }
        public static float AsFloat(this ushort s) { return s / ((float)ushort.MaxValue); }

        public static bool IsZero(this float4 val)
        {

            return val.x == 0 && val.y == 0 && val.z == 0 && val.w == 0;

        }

    }

    public interface IMeshData<T0, T1, T2, T3, T4> : IDisposable where T0 : struct, IVertexData<T0> where T1 : struct, IVertexData<T1> where T2 : struct, IVertexData<T2> where T3 : struct, IVertexData<T3> where T4 : struct, IMeshData<T0, T1, T2, T3, T4>
    {

        public Type GetType0();
        public Type GetType1();
        public Type GetType2();
        public Type GetType3();

        public void SetVertexBufferData(Mesh mesh, int dataStart, int meshBufferStart, int count, MeshUpdateFlags flags = MeshUpdateFlags.Default);

        public void ForceSetVertexBufferData(Mesh mesh, int dataStart, int meshBufferStart, int count, MeshUpdateFlags flags = MeshUpdateFlags.Default);

        public void Dispose(JobHandle handle);

        public VertexAttributeDescriptor[] GetLayout();

        public WokeNativeArray<T0> GetBus0();
        public WokeNativeArray<T1> GetBus1();
        public WokeNativeArray<T2> GetBus2();
        public WokeNativeArray<T3> GetBus3();

        public bool HasColors();

        public bool HasUV(int level);

        public bool IsSkinned();


        public void SetPositions(Vector3[] vertices);

        public void SetNormals(Vector3[] normals);

        public void SetTangents(Vector4[] tangents);

        public void SetColors(Color[] colors);

        public void SetBoneWeights(byte[] bonesPerVertex, BoneWeight1[] boneWeights);

        public void SetUV(int level, Vector2[] uvs);

    }

}
