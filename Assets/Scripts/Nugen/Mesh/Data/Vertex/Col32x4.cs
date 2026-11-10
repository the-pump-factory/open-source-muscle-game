using System.Runtime.InteropServices;
using System;

using Unity.Mathematics;

using nugen.collections;

namespace nugen.mesh.data.vertex
{

    [System.Serializable, StructLayout(LayoutKind.Sequential)]
    public struct Col32x4 : IVertexData<Col32x4>, IEquatable<Col32x4>
    {

        public float cr, cg, cb, ca;

        public Col32x4 Add(Col32x4 val)
        {

            val.cr = cr + val.cr;
            val.cg = cg + val.cg;
            val.cb = cb + val.cb;
            val.ca = ca + val.ca;

            return val;

        }

        public bool Equals(Col32x4 other)
        {

            return cr == other.cr && cg == other.cg && cb == other.cb && ca == other.ca;

        }

        public struct Editor : IMeshDataEditor<Col32x4>
        {

            private WokeNativeArray<Col32x4>.Concurrent vertexData;

            public Editor(WokeNativeArray<Col32x4>.Concurrent vertexData)
            {

                this.vertexData = vertexData;

            }

            public void Add(ContainerId containerIndex, int index, Col32x4 val)
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

                X = new float4(vertexData[i1].cr, vertexData[i2].cr, vertexData[i3].cr, vertexData[i4].cr);
                Y = new float4(vertexData[i1].cg, vertexData[i2].cg, vertexData[i3].cg, vertexData[i4].cg);
                Z = new float4(vertexData[i1].cb, vertexData[i2].cb, vertexData[i3].cb, vertexData[i4].cb);
                W = new float4(vertexData[i1].ca, vertexData[i2].ca, vertexData[i3].ca, vertexData[i4].ca);

            }

            public void setdataf4(ContainerId containerIndex, int startIndex, float4 X, float4 Y, float4 Z, float4 W)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                vertexData[i4] = new Col32x4()
                {

                    cr = X.w,
                    cg = Y.w,
                    cb = Z.w,
                    ca = W.w

                };

                vertexData[i3] = new Col32x4()
                {

                    cr = X.z,
                    cg = Y.z,
                    cb = Z.z,
                    ca = W.z

                };

                vertexData[i2] = new Col32x4()
                {

                    cr = X.y,
                    cg = Y.y,
                    cb = Z.y,
                    ca = W.y

                };

                vertexData[i1] = new Col32x4()
                {

                    cr = X.x,
                    cg = Y.x,
                    cb = Z.x,
                    ca = W.x

                };

            }

            public void getdatafn(ContainerId containerIndex, int startIndex, out float4[] N)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                N = new float4[] {

                    new float4(vertexData[i1].cr, vertexData[i2].cr, vertexData[i3].cr, vertexData[i4].cr),
                    new float4(vertexData[i1].cg, vertexData[i2].cg, vertexData[i3].cg, vertexData[i4].cg),
                    new float4(vertexData[i1].cb, vertexData[i2].cb, vertexData[i3].cb, vertexData[i4].cb),
                    new float4(vertexData[i1].ca, vertexData[i2].ca, vertexData[i3].ca, vertexData[i4].ca)

                };

            }

            public void setdatafn(ContainerId containerIndex, int startIndex, params float4[] N)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                vertexData[i4] = new Col32x4()
                {

                    cr = N[0].w,
                    cg = N[1].w,
                    cb = N[2].w,
                    ca = N[3].w

                };

                vertexData[i3] = new Col32x4()
                {

                    cr = N[0].z,
                    cg = N[1].z,
                    cb = N[2].z,
                    ca = N[3].z

                };

                vertexData[i2] = new Col32x4()
                {

                    cr = N[0].y,
                    cg = N[1].y,
                    cb = N[2].y,
                    ca = N[3].y

                };

                vertexData[i1] = new Col32x4()
                {

                    cr = N[0].x,
                    cg = N[1].x,
                    cb = N[2].x,
                    ca = N[3].x

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

            public WokeNativeArray<Col32x4>.Concurrent GetVertexData()
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
