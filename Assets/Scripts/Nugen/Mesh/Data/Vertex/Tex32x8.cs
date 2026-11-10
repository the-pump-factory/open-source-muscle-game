using System.Runtime.InteropServices;
using System;

using Unity.Mathematics;

using nugen.collections;

namespace nugen.mesh.data.vertex
{

    [System.Serializable, StructLayout(LayoutKind.Sequential)]
    public struct Tex32x8 : IVertexData<Tex32x8>, IEquatable<Tex32x8>
    {

        public float u1x, u1y, u1z, u1w;

        public float u2x, u2y, u2z, u2w;

        public Tex32x8 Add(Tex32x8 val)
        {

            val.u1x = u1x + val.u1x;
            val.u1y = u1y + val.u1y;
            val.u1z = u1z + val.u1z;
            val.u1w = u1w + val.u1w;

            val.u2x = u2x + val.u2x;
            val.u2y = u2y + val.u2y;
            val.u2z = u2z + val.u2z;
            val.u2w = u2w + val.u2w;

            return val;

        }

        public bool Equals(Tex32x8 other)
        {

            return u1x == other.u1x && u1y == other.u1y && u1z == other.u1z && u1w == other.u1w && u2x == other.u2x && u2y == other.u2y && u2z == other.u2z && u2w == other.u2w;

        }

        public struct Editor : IMeshDataEditor<Tex32x8>
        {

            private WokeNativeArray<Tex32x8>.Concurrent vertexData;

            public Editor(WokeNativeArray<Tex32x8>.Concurrent vertexData)
            {

                this.vertexData = vertexData;

            }

            public void Add(ContainerId containerIndex, int index, Tex32x8 val)
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

                X = math.select(new float4(vertexData[i1].u1x, vertexData[i2].u1x, vertexData[i3].u1x, vertexData[i4].u1x), new float4(vertexData[i1].u2x, vertexData[i2].u2x, vertexData[i3].u2x, vertexData[i4].u2x), containerIndex != ContainerId.A);
                Y = math.select(new float4(vertexData[i1].u1y, vertexData[i2].u1y, vertexData[i3].u1y, vertexData[i4].u1y), new float4(vertexData[i1].u2y, vertexData[i2].u2y, vertexData[i3].u2y, vertexData[i4].u2y), containerIndex != ContainerId.A);
                Z = math.select(new float4(vertexData[i1].u1z, vertexData[i2].u1z, vertexData[i3].u1z, vertexData[i4].u1z), new float4(vertexData[i1].u2z, vertexData[i2].u2z, vertexData[i3].u2z, vertexData[i4].u2z), containerIndex != ContainerId.A);
                W = math.select(new float4(vertexData[i1].u1w, vertexData[i2].u1w, vertexData[i3].u1w, vertexData[i4].u1w), new float4(vertexData[i1].u2w, vertexData[i2].u2w, vertexData[i3].u2w, vertexData[i4].u2w), containerIndex != ContainerId.A);

            }

            public void setdataf4(ContainerId containerIndex, int startIndex, float4 X, float4 Y, float4 Z, float4 W)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                Tex32x8 data4 = vertexData[i4];
                Tex32x8 data3 = vertexData[i3];
                Tex32x8 data2 = vertexData[i2];
                Tex32x8 data1 = vertexData[i1];

                if (containerIndex == ContainerId.A)
                {

                    vertexData[i4] = new Tex32x8()
                    {

                        u1x = X.w,
                        u1y = Y.w,
                        u1z = Z.w,
                        u1w = W.w,
                        u2x = data4.u2x,
                        u2y = data4.u2y,
                        u2z = data4.u2z,
                        u2w = data4.u2w

                    };

                    vertexData[i3] = new Tex32x8()
                    {

                        u1x = X.z,
                        u1y = Y.z,
                        u1z = Z.z,
                        u1w = W.z,
                        u2x = data3.u2x,
                        u2y = data3.u2y,
                        u2z = data3.u2z,
                        u2w = data3.u2w

                    };

                    vertexData[i2] = new Tex32x8()
                    {

                        u1x = X.y,
                        u1y = Y.y,
                        u1z = Z.y,
                        u1w = W.y,
                        u2x = data2.u2x,
                        u2y = data2.u2y,
                        u2z = data2.u2z,
                        u2w = data2.u2w

                    };

                    vertexData[i1] = new Tex32x8()
                    {

                        u1x = X.x,
                        u1y = Y.x,
                        u1z = Z.x,
                        u1w = W.x,
                        u2x = data1.u2x,
                        u2y = data1.u2y,
                        u2z = data1.u2z,
                        u2w = data1.u2w

                    };

                } 
                else
                {

                    vertexData[i4] = new Tex32x8()
                    {

                        u2x = X.w,
                        u2y = Y.w,
                        u2z = Z.w,
                        u2w = W.w,
                        u1x = data4.u1x,
                        u1y = data4.u1y,
                        u1z = data4.u1z,
                        u1w = data4.u1w

                    };

                    vertexData[i3] = new Tex32x8()
                    {

                        u2x = X.z,
                        u2y = Y.z,
                        u2z = Z.z,
                        u2w = W.z,
                        u1x = data3.u1x,
                        u1y = data3.u1y,
                        u1z = data3.u1z,
                        u1w = data3.u1w

                    };

                    vertexData[i2] = new Tex32x8()
                    {

                        u2x = X.y,
                        u2y = Y.y,
                        u2z = Z.y,
                        u2w = W.y,
                        u1x = data2.u1x,
                        u1y = data2.u1y,
                        u1z = data2.u1z,
                        u1w = data2.u1w

                    };

                    vertexData[i1] = new Tex32x8()
                    {

                        u2x = X.x,
                        u2y = Y.x,
                        u2z = Z.x,
                        u2w = W.x,
                        u1x = data1.u1x,
                        u1y = data1.u1y,
                        u1z = data1.u1z,
                        u1w = data1.u1w

                    };

                }

            }

            public void getdatafn(ContainerId containerIndex, int startIndex, out float4[] N)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                N = new float4[] {

                    math.select(new float4(vertexData[i1].u1x, vertexData[i2].u1x, vertexData[i3].u1x, vertexData[i4].u1x), new float4(vertexData[i1].u2x, vertexData[i2].u2x, vertexData[i3].u2x, vertexData[i4].u2x), containerIndex != ContainerId.A),
                    math.select(new float4(vertexData[i1].u1y, vertexData[i2].u1y, vertexData[i3].u1y, vertexData[i4].u1y), new float4(vertexData[i1].u2y, vertexData[i2].u2y, vertexData[i3].u2y, vertexData[i4].u2y), containerIndex != ContainerId.A),
                    math.select(new float4(vertexData[i1].u1z, vertexData[i2].u1z, vertexData[i3].u1z, vertexData[i4].u1z), new float4(vertexData[i1].u2z, vertexData[i2].u2z, vertexData[i3].u2z, vertexData[i4].u2z), containerIndex != ContainerId.A),
                    math.select(new float4(vertexData[i1].u1w, vertexData[i2].u1w, vertexData[i3].u1w, vertexData[i4].u1w), new float4(vertexData[i1].u2w, vertexData[i2].u2w, vertexData[i3].u2w, vertexData[i4].u2w), containerIndex != ContainerId.A)

                };

            }

            public void setdatafn(ContainerId containerIndex, int startIndex, params float4[] N)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                Tex32x8 data4 = vertexData[i4];
                Tex32x8 data3 = vertexData[i3];
                Tex32x8 data2 = vertexData[i2];
                Tex32x8 data1 = vertexData[i1];

                if (containerIndex == ContainerId.A)
                {

                    vertexData[i4] = new Tex32x8()
                    {

                        u1x = N[0].w,
                        u1y = N[1].w,
                        u1z = N[2].w,
                        u1w = N[3].w,
                        u2x = data4.u2x,
                        u2y = data4.u2y,
                        u2z = data4.u2z,
                        u2w = data4.u2w

                    };

                    vertexData[i3] = new Tex32x8()
                    {

                        u1x = N[0].z,
                        u1y = N[1].z,
                        u1z = N[2].z,
                        u1w = N[3].z,
                        u2x = data3.u2x,
                        u2y = data3.u2y,
                        u2z = data3.u2z,
                        u2w = data3.u2w

                    };

                    vertexData[i2] = new Tex32x8()
                    {

                        u1x = N[0].y,
                        u1y = N[1].y,
                        u1z = N[2].y,
                        u1w = N[3].y,
                        u2x = data2.u2x,
                        u2y = data2.u2y,
                        u2z = data2.u2z,
                        u2w = data2.u2w

                    };

                    vertexData[i1] = new Tex32x8()
                    {

                        u1x = N[0].x,
                        u1y = N[1].x,
                        u1z = N[2].x,
                        u1w = N[3].x,
                        u2x = data1.u2x,
                        u2y = data1.u2y,
                        u2z = data1.u2z,
                        u2w = data1.u2w

                    };

                } 
                else
                {

                    vertexData[i4] = new Tex32x8()
                    {

                        u2x = N[0].w,
                        u2y = N[1].w,
                        u2z = N[2].w,
                        u2w = N[3].w,
                        u1x = data4.u1x,
                        u1y = data4.u1y,
                        u1z = data4.u1z,
                        u1w = data4.u1w

                    };

                    vertexData[i3] = new Tex32x8()
                    {

                        u2x = N[0].z,
                        u2y = N[1].z,
                        u2z = N[2].z,
                        u2w = N[3].z,
                        u1x = data3.u1x,
                        u1y = data3.u1y,
                        u1z = data3.u1z,
                        u1w = data3.u1w

                    };

                    vertexData[i2] = new Tex32x8()
                    {

                        u2x = N[0].y,
                        u2y = N[1].y,
                        u2z = N[2].y,
                        u2w = N[3].y,
                        u1x = data2.u1x,
                        u1y = data2.u1y,
                        u1z = data2.u1z,
                        u1w = data2.u1w

                    };

                    vertexData[i1] = new Tex32x8()
                    {

                        u2x = N[0].x,
                        u2y = N[1].x,
                        u2z = N[2].x,
                        u2w = N[3].x,
                        u1x = data1.u1x,
                        u1y = data1.u1y,
                        u1z = data1.u1z,
                        u1w = data1.u1w

                    };

                }

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

            public WokeNativeArray<Tex32x8>.Concurrent GetVertexData()
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