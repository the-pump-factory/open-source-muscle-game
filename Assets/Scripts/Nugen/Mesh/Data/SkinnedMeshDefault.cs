using System;

using UnityEngine;
using UnityEngine.Rendering;

using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;

using nugen.collections;
using nugen.mesh.data.vertex;

namespace nugen.mesh.data
{

    public struct SkinnedMeshDefault : IMeshData<PosNor32x3Tan32x4, Col8x4Tex32x4, Skin16x8, NullData, SkinnedMeshDefault>
    {

        public Type GetType0()
        {
            return typeof(PosNor32x3Tan32x4);
        }

        public Type GetType1()
        {
            return typeof(Col8x4Tex32x4);
        }

        public Type GetType2()
        {
            return typeof(Skin16x8);
        }

        public Type GetType3()
        {
            return typeof(NullData);
        }

        public SkinnedMeshDefault(int vertexCount)
        {

            bus0 = new WokeNativeArray<PosNor32x3Tan32x4>(vertexCount, Allocator.Persistent);
            bus1 = new WokeNativeArray<Col8x4Tex32x4>(vertexCount, Allocator.Persistent);
            bus2 = new WokeNativeArray<Skin16x8>(vertexCount, Allocator.Persistent);

        }

        public VertexAttributeDescriptor[] GetLayout()
        {

            return new VertexAttributeDescriptor[] {

            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 0),
            new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4, 0),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 1),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 1),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 2, 1),
            new VertexAttributeDescriptor(VertexAttribute.BlendWeight, VertexAttributeFormat.UNorm16, 4, 2),
            new VertexAttributeDescriptor(VertexAttribute.BlendIndices, VertexAttributeFormat.UInt16, 4, 2)

            };

        }

        private WokeNativeArray<PosNor32x3Tan32x4> bus0;
        private WokeNativeArray<Col8x4Tex32x4> bus1;
        private WokeNativeArray<Skin16x8> bus2;

        public WokeNativeArray<PosNor32x3Tan32x4> Bus0 { get { return bus0; } }
        public WokeNativeArray<Col8x4Tex32x4> Bus1 { get { return bus1; } }
        public WokeNativeArray<Skin16x8> Bus2 { get { return bus2; } }

        public WokeNativeArray<PosNor32x3Tan32x4> GetBus0() { return Bus0; }
        public WokeNativeArray<Col8x4Tex32x4> GetBus1() { return Bus1; }
        public WokeNativeArray<Skin16x8> GetBus2() { return Bus2; }
        public WokeNativeArray<NullData> GetBus3() { return new WokeNativeArray<NullData>(0, Allocator.None); }

        public void SetVertexBufferData(Mesh mesh, int dataStart, int meshBufferStart, int count, MeshUpdateFlags flags = MeshUpdateFlags.Default)
        {

            if (bus0.CheckForUpdate()) mesh.SetVertexBufferData(bus0.Array, dataStart, meshBufferStart, count, 0, flags);
            if (bus1.CheckForUpdate()) mesh.SetVertexBufferData(bus1.Array, dataStart, meshBufferStart, count, 1, flags);
            if (bus2.CheckForUpdate()) mesh.SetVertexBufferData(bus2.Array, dataStart, meshBufferStart, count, 2, flags);

        }

        public void ForceSetVertexBufferData(Mesh mesh, int dataStart, int meshBufferStart, int count, MeshUpdateFlags flags = MeshUpdateFlags.Default)
        {

            mesh.SetVertexBufferData(bus0.Array, dataStart, meshBufferStart, count, 0, flags);
            mesh.SetVertexBufferData(bus1.Array, dataStart, meshBufferStart, count, 1, flags);
            mesh.SetVertexBufferData(bus2.Array, dataStart, meshBufferStart, count, 2, flags);

        }

        public void Dispose()
        {

            bus0.Dispose();
            bus1.Dispose();
            bus2.Dispose();

        }

        public void Dispose(JobHandle handle)
        {

            bus0.Dispose(handle);
            bus1.Dispose(handle);
            bus2.Dispose(handle);

        }

        public bool HasColors() { return true; }

        public bool HasUV(int level) { return level <= 1 && level >= 0; }

        public bool IsSkinned() { return true; }

        public void SetPositions(Vector3[] vertices)
        {
            
            for(int a = 0; a < Mathf.Min(vertices == null ? 0 : vertices.Length, bus0.Length); a++)
            {

                Vector3 vertex = vertices[a];

                PosNor32x3Tan32x4 data = bus0[a];

                data.px = vertex.x;
                data.py = vertex.y;
                data.pz = vertex.z;

                bus0[a] = data;

            }

        }

        public void SetNormals(Vector3[] normals)
        {

            for (int a = 0; a < Mathf.Min(normals == null ? 0 : normals.Length, bus0.Length); a++)
            {

                Vector3 normal = normals[a];

                PosNor32x3Tan32x4 data = bus0[a];

                data.nx = normal.x;
                data.ny = normal.y;
                data.nz = normal.z;

                bus0[a] = data;

            }

        }

        public void SetTangents(Vector4[] tangents)
        {

            for (int a = 0; a < Mathf.Min(tangents == null ? 0 : tangents.Length, bus0.Length); a++)
            {

                Vector4 tangent = tangents[a];

                PosNor32x3Tan32x4 data = bus0[a];

                data.tx = tangent.x;
                data.ty = tangent.y;
                data.tz = tangent.z;
                data.tw = tangent.w;

                bus0[a] = data;

            }

        }

        public void SetColors(Color[] colors)
        {

            for (int a = 0; a < Mathf.Min(colors == null ? 0 : colors.Length, bus1.Length); a++)
            {

                Color color = colors[a];

                Col8x4Tex32x4 data = bus1[a];

                data.cr = color.r.AsByte();
                data.cg = color.g.AsByte();
                data.cb = color.b.AsByte();
                data.ca = color.a.AsByte();

                bus1[a] = data;

            }

        }

        public void SetBoneWeights(byte[] bonesPerVertex, BoneWeight1[] boneWeights)
        {

            if (bonesPerVertex == null || boneWeights == null) return;

            int boneIndex = 0;

            for(int a = 0; a < Mathf.Min(bonesPerVertex.Length, bus2.Length); a++)
            {

                Skin16x8 data = bus2[a];

                int count = bonesPerVertex[a];

                if (count > 0)
                {

                    BoneWeight1 bw = boneWeights[boneIndex];

                    data.bi0 = (ushort)bw.boneIndex;
                    data.bw0 = bw.weight.AsUShort();

                } 

                if (count > 1)
                {

                    BoneWeight1 bw = boneWeights[boneIndex + 1];

                    data.bi1 = (ushort)bw.boneIndex;
                    data.bw1 = bw.weight.AsUShort();

                }

                if (count > 2)
                {

                    BoneWeight1 bw = boneWeights[boneIndex + 2];

                    data.bi2 = (ushort)bw.boneIndex;
                    data.bw2 = bw.weight.AsUShort();

                }

                if (count > 3)
                {

                    BoneWeight1 bw = boneWeights[boneIndex + 3];

                    data.bi3 = (ushort)bw.boneIndex;
                    data.bw3 = bw.weight.AsUShort();

                }

                boneIndex += count;

                bus2[a] = data;

            }

        }

        public void SetUV(int level, Vector2[] uvs)
        {

            if (level < 0 || level > 1 || uvs == null) return;

            if (level == 0)
            {

                for (int a = 0; a < Mathf.Min(uvs.Length, bus1.Length); a++)
                {

                    Vector2 uv = uvs[a];

                    Col8x4Tex32x4 data = bus1[a];

                    data.ux = uv.x;
                    data.uy = uv.y;

                    bus1[a] = data;

                }

            } 
            else
            {

                for (int a = 0; a < Mathf.Min(uvs.Length, bus1.Length); a++)
                {

                    Vector2 uv = uvs[a];

                    Col8x4Tex32x4 data = bus1[a];

                    data.uz = uv.x;
                    data.uw = uv.y;

                    bus1[a] = data;

                }

            }

        }

    }
}
