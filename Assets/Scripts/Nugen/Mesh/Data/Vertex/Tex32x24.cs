using System.Runtime.InteropServices;
using System;

using Unity.Mathematics;

using nugen.collections;

namespace nugen.mesh.data.vertex
{

    [System.Serializable, StructLayout(LayoutKind.Sequential)]
    public struct Tex32x24 : IVertexData<Tex32x24>, IEquatable<Tex32x24>
    {

        public float u1x, u1y, u1z, u1w;

        public float u2x, u2y, u2z, u2w;

        public float u3x, u3y, u3z, u3w;

        public float u4x, u4y, u4z, u4w;

        public float u5x, u5y, u5z, u5w;

        public float u6x, u6y, u6z, u6w;

        public Tex32x24 Add(Tex32x24 val)
        {

            val.u1x = u1x + val.u1x;
            val.u1y = u1y + val.u1y;
            val.u1z = u1z + val.u1z;
            val.u1w = u1w + val.u1w;

            val.u2x = u2x + val.u2x;
            val.u2y = u2y + val.u2y;
            val.u2z = u2z + val.u2z;
            val.u2w = u2w + val.u2w;

            val.u3x = u3x + val.u3x;
            val.u3y = u3y + val.u3y;
            val.u3z = u3z + val.u3z;
            val.u3w = u3w + val.u3w;
        
            val.u4x = u4x + val.u4x;
            val.u4y = u4y + val.u4y;
            val.u4z = u4z + val.u4z;
            val.u4w = u4w + val.u4w;

            val.u5x = u5x + val.u5x;
            val.u5y = u5y + val.u5y;
            val.u5z = u5z + val.u5z;
            val.u5w = u5w + val.u5w;

            val.u6x = u6x + val.u6x;
            val.u6y = u6y + val.u6y;
            val.u6z = u6z + val.u6z;
            val.u6w = u6w + val.u6w;

            return val;

        }

        public bool Equals(Tex32x24 other)
        {

            return u1x == other.u1x && u1y == other.u1y && u1z == other.u1z && u1w == other.u1w && 
                u2x == other.u2x && u2y == other.u2y && u2z == other.u2z && u2w == other.u2w &&
                u2x == other.u3x && u3y == other.u3y && u3z == other.u3z && u3w == other.u3w &&
                u2x == other.u4x && u4y == other.u4y && u4z == other.u4z && u4w == other.u4w &&
                u2x == other.u5x && u5y == other.u5y && u5z == other.u5z && u5w == other.u5w &&
                u2x == other.u6x && u6y == other.u6y && u6z == other.u6z && u6w == other.u6w;

        }

        public struct Editor : IMeshDataEditor<Tex32x24>
        {

            private WokeNativeArray<Tex32x24>.Concurrent vertexData;

            public Editor(WokeNativeArray<Tex32x24>.Concurrent vertexData)
            {

                this.vertexData = vertexData;

            }

            public void Add(ContainerId containerIndex, int index, Tex32x24 val)
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

                switch(containerIndex)
                {

                    default:

                        X = new float4(vertexData[i1].u1x, vertexData[i2].u1x, vertexData[i3].u1x, vertexData[i4].u1x);
                        Y = new float4(vertexData[i1].u1y, vertexData[i2].u1y, vertexData[i3].u1y, vertexData[i4].u1y);
                        Z = new float4(vertexData[i1].u1z, vertexData[i2].u1z, vertexData[i3].u1z, vertexData[i4].u1z);
                        W = new float4(vertexData[i1].u1w, vertexData[i2].u1w, vertexData[i3].u1w, vertexData[i4].u1w);

                        break;

                    case ContainerId.B:

                        X = new float4(vertexData[i1].u2x, vertexData[i2].u2x, vertexData[i3].u2x, vertexData[i4].u2x);
                        Y = new float4(vertexData[i1].u2y, vertexData[i2].u2y, vertexData[i3].u2y, vertexData[i4].u2y);
                        Z = new float4(vertexData[i1].u2z, vertexData[i2].u2z, vertexData[i3].u2z, vertexData[i4].u2z);
                        W = new float4(vertexData[i1].u2w, vertexData[i2].u2w, vertexData[i3].u2w, vertexData[i4].u2w);

                        break;

                    case ContainerId.C:

                        X = new float4(vertexData[i1].u3x, vertexData[i2].u3x, vertexData[i3].u3x, vertexData[i4].u3x);
                        Y = new float4(vertexData[i1].u3y, vertexData[i2].u3y, vertexData[i3].u3y, vertexData[i4].u3y);
                        Z = new float4(vertexData[i1].u3z, vertexData[i2].u3z, vertexData[i3].u3z, vertexData[i4].u3z);
                        W = new float4(vertexData[i1].u3w, vertexData[i2].u3w, vertexData[i3].u3w, vertexData[i4].u3w);

                        break;

                    case ContainerId.D:

                        X = new float4(vertexData[i1].u4x, vertexData[i2].u4x, vertexData[i3].u4x, vertexData[i4].u4x);
                        Y = new float4(vertexData[i1].u4y, vertexData[i2].u4y, vertexData[i3].u4y, vertexData[i4].u4y);
                        Z = new float4(vertexData[i1].u4z, vertexData[i2].u4z, vertexData[i3].u4z, vertexData[i4].u4z);
                        W = new float4(vertexData[i1].u4w, vertexData[i2].u4w, vertexData[i3].u4w, vertexData[i4].u4w);

                        break;

                    case ContainerId.E:

                        X = new float4(vertexData[i1].u5x, vertexData[i2].u5x, vertexData[i3].u5x, vertexData[i4].u5x);
                        Y = new float4(vertexData[i1].u5y, vertexData[i2].u5y, vertexData[i3].u5y, vertexData[i4].u5y);
                        Z = new float4(vertexData[i1].u5z, vertexData[i2].u5z, vertexData[i3].u5z, vertexData[i4].u5z);
                        W = new float4(vertexData[i1].u5w, vertexData[i2].u5w, vertexData[i3].u5w, vertexData[i4].u5w);

                        break;

                    case ContainerId.F:

                        X = new float4(vertexData[i1].u6x, vertexData[i2].u6x, vertexData[i3].u6x, vertexData[i4].u6x);
                        Y = new float4(vertexData[i1].u6y, vertexData[i2].u6y, vertexData[i3].u6y, vertexData[i4].u6y);
                        Z = new float4(vertexData[i1].u6z, vertexData[i2].u6z, vertexData[i3].u6z, vertexData[i4].u6z);
                        W = new float4(vertexData[i1].u6w, vertexData[i2].u6w, vertexData[i3].u6w, vertexData[i4].u6w);

                        break;

                }

            }

            public void setdataf4(ContainerId containerIndex, int startIndex, float4 X, float4 Y, float4 Z, float4 W)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                Tex32x24 data4 = vertexData[i4];
                Tex32x24 data3 = vertexData[i3];
                Tex32x24 data2 = vertexData[i2];
                Tex32x24 data1 = vertexData[i1];

                switch(containerIndex)
                {

                    default:

                        data4.u1x = X.w;
                        data4.u1y = Y.w;
                        data4.u1z = Z.w;
                        data4.u1w = W.w;

                        data3.u1x = X.z;
                        data3.u1y = Y.z;
                        data3.u1z = Z.z;
                        data3.u1w = W.z;

                        data2.u1x = X.y;
                        data2.u1y = Y.y;
                        data2.u1z = Z.y;
                        data2.u1w = W.y;

                        data1.u1x = X.x;
                        data1.u1y = Y.x;
                        data1.u1z = Z.x;
                        data1.u1w = W.x;

                        break;

                    case ContainerId.B:

                        data4.u2x = X.w;
                        data4.u2y = Y.w;
                        data4.u2z = Z.w;
                        data4.u2w = W.w;

                        data3.u2x = X.z;
                        data3.u2y = Y.z;
                        data3.u2z = Z.z;
                        data3.u2w = W.z;

                        data2.u2x = X.y;
                        data2.u2y = Y.y;
                        data2.u2z = Z.y;
                        data2.u2w = W.y;

                        data1.u2x = X.x;
                        data1.u2y = Y.x;
                        data1.u2z = Z.x;
                        data1.u2w = W.x;

                        break;

                    case ContainerId.C:

                        data4.u3x = X.w;
                        data4.u3y = Y.w;
                        data4.u3z = Z.w;
                        data4.u3w = W.w;

                        data3.u3x = X.z;
                        data3.u3y = Y.z;
                        data3.u3z = Z.z;
                        data3.u3w = W.z;

                        data2.u3x = X.y;
                        data2.u3y = Y.y;
                        data2.u3z = Z.y;
                        data2.u3w = W.y;

                        data1.u3x = X.x;
                        data1.u3y = Y.x;
                        data1.u3z = Z.x;
                        data1.u3w = W.x;

                        break;

                    case ContainerId.D:

                        data4.u4x = X.w;
                        data4.u4y = Y.w;
                        data4.u4z = Z.w;
                        data4.u4w = W.w;

                        data3.u4x = X.z;
                        data3.u4y = Y.z;
                        data3.u4z = Z.z;
                        data3.u4w = W.z;

                        data2.u4x = X.y;
                        data2.u4y = Y.y;
                        data2.u4z = Z.y;
                        data2.u4w = W.y;

                        data1.u4x = X.x;
                        data1.u4y = Y.x;
                        data1.u4z = Z.x;
                        data1.u4w = W.x;

                        break;

                    case ContainerId.E:

                        data4.u5x = X.w;
                        data4.u5y = Y.w;
                        data4.u5z = Z.w;
                        data4.u5w = W.w;

                        data3.u5x = X.z;
                        data3.u5y = Y.z;
                        data3.u5z = Z.z;
                        data3.u5w = W.z;

                        data2.u5x = X.y;
                        data2.u5y = Y.y;
                        data2.u5z = Z.y;
                        data2.u5w = W.y;

                        data1.u5x = X.x;
                        data1.u5y = Y.x;
                        data1.u5z = Z.x;
                        data1.u5w = W.x;

                        break;

                    case ContainerId.F:

                        data4.u6x = X.w;
                        data4.u6y = Y.w;
                        data4.u6z = Z.w;
                        data4.u6w = W.w;

                        data3.u6x = X.z;
                        data3.u6y = Y.z;
                        data3.u6z = Z.z;
                        data3.u6w = W.z;

                        data2.u6x = X.y;
                        data2.u6y = Y.y;
                        data2.u6z = Z.y;
                        data2.u6w = W.y;

                        data1.u6x = X.x;
                        data1.u6y = Y.x;
                        data1.u6z = Z.x;
                        data1.u6w = W.x;

                        break;

                }

                vertexData[i4] = data4;
                vertexData[i3] = data3;
                vertexData[i2] = data2;
                vertexData[i1] = data1;

            }

            public void getdatafn(ContainerId containerIndex, int startIndex, out float4[] N)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                switch (containerIndex)
                {

                    default:

                        N = new float4[] {

                            new float4(vertexData[i1].u1x, vertexData[i2].u1x, vertexData[i3].u1x, vertexData[i4].u1x),
                            new float4(vertexData[i1].u1y, vertexData[i2].u1y, vertexData[i3].u1y, vertexData[i4].u1y),
                            new float4(vertexData[i1].u1z, vertexData[i2].u1z, vertexData[i3].u1z, vertexData[i4].u1z),
                            new float4(vertexData[i1].u1w, vertexData[i2].u1w, vertexData[i3].u1w, vertexData[i4].u1w)

                        };

                        break;

                    case ContainerId.B:

                        N = new float4[] {

                            new float4(vertexData[i1].u2x, vertexData[i2].u2x, vertexData[i3].u2x, vertexData[i4].u2x),
                            new float4(vertexData[i1].u2y, vertexData[i2].u2y, vertexData[i3].u2y, vertexData[i4].u2y),
                            new float4(vertexData[i1].u2z, vertexData[i2].u2z, vertexData[i3].u2z, vertexData[i4].u2z),
                            new float4(vertexData[i1].u2w, vertexData[i2].u2w, vertexData[i3].u2w, vertexData[i4].u2w)

                        };

                        break;

                    case ContainerId.C:

                        N = new float4[] {

                            new float4(vertexData[i1].u3x, vertexData[i2].u3x, vertexData[i3].u3x, vertexData[i4].u3x),
                            new float4(vertexData[i1].u3y, vertexData[i2].u3y, vertexData[i3].u3y, vertexData[i4].u3y),
                            new float4(vertexData[i1].u3z, vertexData[i2].u3z, vertexData[i3].u3z, vertexData[i4].u3z),
                            new float4(vertexData[i1].u3w, vertexData[i2].u3w, vertexData[i3].u3w, vertexData[i4].u3w)

                        };

                        break;

                    case ContainerId.D:

                        N = new float4[] {

                            new float4(vertexData[i1].u4x, vertexData[i2].u4x, vertexData[i3].u4x, vertexData[i4].u4x),
                            new float4(vertexData[i1].u4y, vertexData[i2].u4y, vertexData[i3].u4y, vertexData[i4].u4y),
                            new float4(vertexData[i1].u4z, vertexData[i2].u4z, vertexData[i3].u4z, vertexData[i4].u4z),
                            new float4(vertexData[i1].u4w, vertexData[i2].u4w, vertexData[i3].u4w, vertexData[i4].u4w)

                        };

                        break;

                    case ContainerId.E:

                        N = new float4[] {

                            new float4(vertexData[i1].u5x, vertexData[i2].u5x, vertexData[i3].u5x, vertexData[i4].u5x),
                            new float4(vertexData[i1].u5y, vertexData[i2].u5y, vertexData[i3].u5y, vertexData[i4].u5y),
                            new float4(vertexData[i1].u5z, vertexData[i2].u5z, vertexData[i3].u5z, vertexData[i4].u5z),
                            new float4(vertexData[i1].u5w, vertexData[i2].u5w, vertexData[i3].u5w, vertexData[i4].u5w)

                        };

                        break;

                    case ContainerId.F:

                        N = new float4[] {

                            new float4(vertexData[i1].u6x, vertexData[i2].u6x, vertexData[i3].u6x, vertexData[i4].u6x),
                            new float4(vertexData[i1].u6y, vertexData[i2].u6y, vertexData[i3].u6y, vertexData[i4].u6y),
                            new float4(vertexData[i1].u6z, vertexData[i2].u6z, vertexData[i3].u6z, vertexData[i4].u6z),
                            new float4(vertexData[i1].u6w, vertexData[i2].u6w, vertexData[i3].u6w, vertexData[i4].u6w)

                        };

                        break;

                }

            }

            public void setdatafn(ContainerId containerIndex, int startIndex, params float4[] N)
            {

                setdataf4(containerIndex, startIndex, N[0], N[1], N[2], N[3]);

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

            public WokeNativeArray<Tex32x24>.Concurrent GetVertexData()
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