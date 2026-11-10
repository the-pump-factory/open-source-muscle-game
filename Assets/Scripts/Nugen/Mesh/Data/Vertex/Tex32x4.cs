using System.Runtime.InteropServices;
using System;

using Unity.Mathematics;

using nugen.collections;

namespace nugen.mesh.data.vertex
{

    [System.Serializable, StructLayout(LayoutKind.Sequential)]
    public struct Tex32x4 : IVertexData<Tex32x4>, IEquatable<Tex32x4>
    {

        public float ux, uy, uz, uw;

        public Tex32x4 Add(Tex32x4 val)
        {

            val.ux = ux + val.ux;
            val.uy = uy + val.uy;
            val.uz = uz + val.uz;
            val.uw = uw + val.uw;

            return val;

        }

        public bool Equals(Tex32x4 other)
        {

            return ux == other.ux && uy == other.uy && uz == other.uz && uw == other.uw;

        }

        public struct Editor : IMeshDataEditor<Tex32x4>
        {

            private WokeNativeArray<Tex32x4>.Concurrent vertexData;

            public Editor(WokeNativeArray<Tex32x4>.Concurrent vertexData)
            {

                this.vertexData = vertexData;

            }

            public void Add(ContainerId containerIndex, int index, Tex32x4 val)
            {

                vertexData[index] = vertexData[index].Add(val);

            }

            public void GetIndices(int startIndex, out int i1, out int i2, out int i3, out int i4)
            {

                i1 = math.select(startIndex, VertexCount - 1, startIndex >= VertexCount);
                i2 = math.select(startIndex + 1, i1, startIndex + 1 >= VertexCount);
                i3 = math.select(startIndex + 2, i2, startIndex + 2 >= VertexCount);
                i4 = math.select(startIndex + 3, i3, startIndex + 3 >= VertexCount);

            }

            public void getdataf4(ContainerId containerIndex, int startIndex, out float4 X, out float4 Y, out float4 Z, out float4 W)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                X = new float4(vertexData[i1].ux, vertexData[i2].ux, vertexData[i3].ux, vertexData[i4].ux);
                Y = new float4(vertexData[i1].uy, vertexData[i2].uy, vertexData[i3].uy, vertexData[i4].uy);
                Z = new float4(vertexData[i1].uz, vertexData[i2].uz, vertexData[i3].uz, vertexData[i4].uz);
                W = new float4(vertexData[i1].uw, vertexData[i2].uw, vertexData[i3].uw, vertexData[i4].uw);

            }

            public void setdataf4(ContainerId containerIndex, int startIndex, float4 X, float4 Y, float4 Z, float4 W)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                vertexData[i4] = new Tex32x4()
                {

                    ux = X.w,
                    uy = Y.w,
                    uz = Z.w,
                    uw = W.w

                };

                vertexData[i3] = new Tex32x4()
                {

                    ux = X.z,
                    uy = Y.z,
                    uz = Z.z,
                    uw = W.z

                };

                vertexData[i2] = new Tex32x4()
                {

                    ux = X.y,
                    uy = Y.y,
                    uz = Z.y,
                    uw = W.y

                };

                vertexData[i1] = new Tex32x4()
                {

                    ux = X.x,
                    uy = Y.x,
                    uz = Z.x,
                    uw = W.x

                };

            }

            public void getdatafn(ContainerId containerIndex, int startIndex, out float4[] N)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                N = new float4[] {

                    new float4(vertexData[i1].ux, vertexData[i2].ux, vertexData[i3].ux, vertexData[i4].ux),
                    new float4(vertexData[i1].uy, vertexData[i2].uy, vertexData[i3].uy, vertexData[i4].uy),
                    new float4(vertexData[i1].uz, vertexData[i2].uz, vertexData[i3].uz, vertexData[i4].uz),
                    new float4(vertexData[i1].uw, vertexData[i2].uw, vertexData[i3].uw, vertexData[i4].uw)

                };

            }

            public void setdatafn(ContainerId containerIndex, int startIndex, params float4[] N)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                vertexData[i4] = new Tex32x4()
                {

                    ux = N[0].w,
                    uy = N[1].w,
                    uz = N[2].w,
                    uw = N[3].w

                };

                vertexData[i3] = new Tex32x4()
                {

                    ux = N[0].z,
                    uy = N[1].z,
                    uz = N[2].z,
                    uw = N[3].z

                };

                vertexData[i2] = new Tex32x4()
                {

                    ux = N[0].y,
                    uy = N[1].y,
                    uz = N[2].y,
                    uw = N[3].y

                };

                vertexData[i1] = new Tex32x4()
                {

                    ux = N[0].x,
                    uy = N[1].x,
                    uz = N[2].x,
                    uw = N[3].x

                };

            }

            public int VertexCount
            {

                get
                {

                    return GetVertexCount();

                }

            }

            public int GetVertexCount()
            {

                return vertexData.Length;

            }

            public WokeNativeArray<Tex32x4>.Concurrent GetVertexData()
            {

                return vertexData;

            }

            public void getdataf1(ContainerId containerIndex, int startIndex, out float4 X)
            {
                throw new System.NotImplementedException();
            }

            public void setdataf1(ContainerId containerIndex, int startIndex, float4 X)
            {
                throw new System.NotImplementedException();
            }

            public void getdataf2(ContainerId containerIndex, int startIndex, out float4 X, out float4 Y)
            {
                throw new System.NotImplementedException();
            }

            public void setdataf2(ContainerId containerIndex, int startIndex, float4 X, float4 Y)
            {
                throw new System.NotImplementedException();
            }

            public void getdataf3(ContainerId containerIndex, int startIndex, out float4 X, out float4 Y, out float4 Z)
            {
                throw new NotImplementedException();
            }

            public void setdataf3(ContainerId containerIndex, int startIndex, float4 X, float4 Y, float4 Z)
            {
                throw new NotImplementedException();
            }

            public void getdatai1(ContainerId containerIndex, int startIndex, out int4 X)
            {
                throw new System.NotImplementedException();
            }

            public void setdatai1(ContainerId containerIndex, int startIndex, int4 X)
            {
                throw new System.NotImplementedException();
            }

            public void getdatai2(ContainerId containerIndex, int startIndex, out int4 X, out int4 Y)
            {
                throw new System.NotImplementedException();
            }

            public void setdatai2(ContainerId containerIndex, int startIndex, int4 X, int4 Y)
            {
                throw new System.NotImplementedException();
            }

            public void getdatai3(ContainerId containerIndex, int startIndex, out int4 X, out int4 Y, out int4 Z)
            {
                throw new System.NotImplementedException();
            }

            public void setdatai3(ContainerId containerIndex, int startIndex, int4 X, int4 Y, int4 Z)
            {
                throw new System.NotImplementedException();
            }

            public void getdatai4(ContainerId containerIndex, int startIndex, out int4 X, out int4 Y, out int4 Z, out int4 W)
            {
                throw new System.NotImplementedException();
            }

            public void setdatai4(ContainerId containerIndex, int startIndex, int4 X, int4 Y, int4 Z, int4 W)
            {
                throw new System.NotImplementedException();
            }

            public void getdatain(ContainerId containerIndex, int startIndex, out int4[] N)
            {
                throw new System.NotImplementedException();
            }

            public void setdatain(ContainerId containerIndex, int startIndex, params int4[] N)
            {
                throw new System.NotImplementedException();
            }

        }

    }

}