using System.Runtime.InteropServices;
using System;

using Unity.Mathematics;

using nugen.collections;

namespace nugen.mesh.data.vertex
{

    [System.Serializable, StructLayout(LayoutKind.Sequential)]
    public struct PosNor32x3Tan32x4 : IVertexData<PosNor32x3Tan32x4>, IEquatable<PosNor32x3Tan32x4>
    {

        public float px, py, pz;

        public float nx, ny, nz;

        public float tx, ty, tz, tw;

        public PosNor32x3Tan32x4 Add(PosNor32x3Tan32x4 val)
        {

            val.px = px + val.px;
            val.py = py + val.py;
            val.pz = pz + val.pz;

            val.nx = nx + val.nx;
            val.ny = ny + val.ny;
            val.nz = nz + val.nz;

            val.tx = tx + val.tx;
            val.ty = ty + val.ty;
            val.tz = tz + val.tz;
            val.tw = tw + val.tw;

            return val;

        }

        public bool Equals(PosNor32x3Tan32x4 other)
        {

            return px == other.px && py == other.py && pz == other.pz && nx == other.nx && ny == other.ny && nz == other.nz && tx == other.tx && ty == other.ty && tz == other.tz && tw == other.tw;

        }

        public struct Editor : IMeshDataEditor<PosNor32x3Tan32x4>
        {

            private WokeNativeArray<PosNor32x3Tan32x4>.Concurrent vertexData;

            public Editor(WokeNativeArray<PosNor32x3Tan32x4>.Concurrent vertexData)
            {

                this.vertexData = vertexData;

            }

            public void Add(ContainerId containerIndex, int index, PosNor32x3Tan32x4 val)
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

            public void getdataf3(ContainerId containerIndex, int startIndex, out float4 px, out float4 py, out float4 pz)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                switch (containerIndex)
                {

                    default:

                        px = new float4(vertexData[i1].px, vertexData[i2].px, vertexData[i3].px, vertexData[i4].px);
                        py = new float4(vertexData[i1].py, vertexData[i2].py, vertexData[i3].py, vertexData[i4].py);
                        pz = new float4(vertexData[i1].pz, vertexData[i2].pz, vertexData[i3].pz, vertexData[i4].pz);

                        return;

                    case ContainerId.B:

                        px = new float4(vertexData[i1].nx, vertexData[i2].nx, vertexData[i3].nx, vertexData[i4].nx);
                        py = new float4(vertexData[i1].ny, vertexData[i2].ny, vertexData[i3].ny, vertexData[i4].ny);
                        pz = new float4(vertexData[i1].nz, vertexData[i2].nz, vertexData[i3].nz, vertexData[i4].nz);

                        return;

                }

            }

            public void setdataf3(ContainerId containerIndex, int startIndex, float4 px, float4 py, float4 pz)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                switch (containerIndex)
                {

                    default:

                        vertexData[i4] = new PosNor32x3Tan32x4()
                        {

                            px = px.w,
                            py = py.w,
                            pz = pz.w,
                            nx = vertexData[i4].nx,
                            ny = vertexData[i4].ny,
                            nz = vertexData[i4].nz,
                            tx = vertexData[i4].tx,
                            ty = vertexData[i4].ty,
                            tz = vertexData[i4].tz,
                            tw = vertexData[i4].tw

                        };

                        vertexData[i3] = new PosNor32x3Tan32x4()
                        {

                            px = px.z,
                            py = py.z,
                            pz = pz.z,
                            nx = vertexData[i3].nx,
                            ny = vertexData[i3].ny,
                            nz = vertexData[i3].nz,
                            tx = vertexData[i3].tx,
                            ty = vertexData[i3].ty,
                            tz = vertexData[i3].tz,
                            tw = vertexData[i3].tw

                        };

                        vertexData[i2] = new PosNor32x3Tan32x4()
                        {

                            px = px.y,
                            py = py.y,
                            pz = pz.y,
                            nx = vertexData[i2].nx,
                            ny = vertexData[i2].ny,
                            nz = vertexData[i2].nz,
                            tx = vertexData[i2].tx,
                            ty = vertexData[i2].ty,
                            tz = vertexData[i2].tz,
                            tw = vertexData[i2].tw

                        };

                        vertexData[i1] = new PosNor32x3Tan32x4()
                        {

                            px = px.x,
                            py = py.x,
                            pz = pz.x,
                            nx = vertexData[i1].nx,
                            ny = vertexData[i1].ny,
                            nz = vertexData[i1].nz,
                            tx = vertexData[i1].tx,
                            ty = vertexData[i1].ty,
                            tz = vertexData[i1].tz,
                            tw = vertexData[i1].tw

                        };

                        return;

                    case ContainerId.B:

                        vertexData[i4] = new PosNor32x3Tan32x4()
                        {

                            px = vertexData[i4].px,
                            py = vertexData[i4].py,
                            pz = vertexData[i4].pz,
                            nx = px.w,
                            ny = py.w,
                            nz = pz.w,
                            tx = vertexData[i4].tx,
                            ty = vertexData[i4].ty,
                            tz = vertexData[i4].tz,
                            tw = vertexData[i4].tw

                        };

                        vertexData[i3] = new PosNor32x3Tan32x4()
                        {

                            px = vertexData[i3].px,
                            py = vertexData[i3].py,
                            pz = vertexData[i3].pz,
                            nx = px.z,
                            ny = py.z,
                            nz = pz.z,
                            tx = vertexData[i3].tx,
                            ty = vertexData[i3].ty,
                            tz = vertexData[i3].tz,
                            tw = vertexData[i3].tw

                        };

                        vertexData[i2] = new PosNor32x3Tan32x4()
                        {

                            px = vertexData[i2].px,
                            py = vertexData[i2].py,
                            pz = vertexData[i2].pz,
                            nx = px.y,
                            ny = py.y,
                            nz = pz.y,
                            tx = vertexData[i2].tx,
                            ty = vertexData[i2].ty,
                            tz = vertexData[i2].tz,
                            tw = vertexData[i2].tw

                        };

                        vertexData[i1] = new PosNor32x3Tan32x4()
                        {

                            px = vertexData[i1].px,
                            py = vertexData[i1].py,
                            pz = vertexData[i1].pz,
                            nx = px.x,
                            ny = py.x,
                            nz = pz.x,
                            tx = vertexData[i1].tx,
                            ty = vertexData[i1].ty,
                            tz = vertexData[i1].tz,
                            tw = vertexData[i1].tw

                        };

                        return;

                }

            }

            public void getdataf4(ContainerId containerIndex, int startIndex, out float4 X, out float4 Y, out float4 Z, out float4 W)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                X = new float4(vertexData[i1].tx, vertexData[i2].tx, vertexData[i3].tx, vertexData[i4].tx);
                Y = new float4(vertexData[i1].ty, vertexData[i2].ty, vertexData[i3].ty, vertexData[i4].ty);
                Z = new float4(vertexData[i1].tz, vertexData[i2].tz, vertexData[i3].tz, vertexData[i4].tz);
                W = new float4(vertexData[i1].tw, vertexData[i2].tw, vertexData[i3].tw, vertexData[i4].tw);

            }

            public void setdataf4(ContainerId containerIndex, int startIndex, float4 X, float4 Y, float4 Z, float4 W)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                vertexData[i4] = new PosNor32x3Tan32x4()
                {

                    px = vertexData[i4].px,
                    py = vertexData[i4].py,
                    pz = vertexData[i4].pz,
                    nx = vertexData[i4].nx,
                    ny = vertexData[i4].ny,
                    nz = vertexData[i4].nz,
                    tx = X.w,
                    ty = Y.w,
                    tz = Z.w,
                    tw = W.w

                };

                vertexData[i3] = new PosNor32x3Tan32x4()
                {

                    px = vertexData[i3].px,
                    py = vertexData[i3].py,
                    pz = vertexData[i3].pz,
                    nx = vertexData[i3].nx,
                    ny = vertexData[i3].ny,
                    nz = vertexData[i3].nz,
                    tx = X.z,
                    ty = Y.z,
                    tz = Z.z,
                    tw = W.z

                };

                vertexData[i2] = new PosNor32x3Tan32x4()
                {

                    px = vertexData[i2].px,
                    py = vertexData[i2].py,
                    pz = vertexData[i2].pz,
                    nx = vertexData[i2].nx,
                    ny = vertexData[i2].ny,
                    nz = vertexData[i2].nz,
                    tx = X.y,
                    ty = Y.y,
                    tz = Z.y,
                    tw = W.y

                };

                vertexData[i1] = new PosNor32x3Tan32x4()
                {

                    px = vertexData[i1].px,
                    py = vertexData[i1].py,
                    pz = vertexData[i1].pz,
                    nx = vertexData[i1].nx,
                    ny = vertexData[i1].ny,
                    nz = vertexData[i1].nz,
                    tx = X.x,
                    ty = Y.x,
                    tz = Z.x,
                    tw = W.x

                };

            }

            public void getdatafn(ContainerId containerIndex, int startIndex, out float4[] N)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                N = new float4[] {

                    new float4(vertexData[i1].px, vertexData[i2].px, vertexData[i3].px, vertexData[i4].px),
                    new float4(vertexData[i1].py, vertexData[i2].py, vertexData[i3].py, vertexData[i4].py),
                    new float4(vertexData[i1].pz, vertexData[i2].pz, vertexData[i3].pz, vertexData[i4].pz),

                    new float4(vertexData[i1].nx, vertexData[i2].nx, vertexData[i3].nx, vertexData[i4].px),
                    new float4(vertexData[i1].ny, vertexData[i2].ny, vertexData[i3].ny, vertexData[i4].py),
                    new float4(vertexData[i1].nz, vertexData[i2].nz, vertexData[i3].nz, vertexData[i4].pz),

                    new float4(vertexData[i1].tx, vertexData[i2].tx, vertexData[i3].tx, vertexData[i4].tx),
                    new float4(vertexData[i1].ty, vertexData[i2].ty, vertexData[i3].ty, vertexData[i4].ty),
                    new float4(vertexData[i1].tz, vertexData[i2].tz, vertexData[i3].tz, vertexData[i4].tz),
                    new float4(vertexData[i1].tw, vertexData[i2].tw, vertexData[i3].tw, vertexData[i4].tw)

                };

            }

            public void setdatafn(ContainerId containerIndex, int startIndex, params float4[] N)
            {

                GetIndices(startIndex, out int i1, out int i2, out int i3, out int i4);

                vertexData[i4] = new PosNor32x3Tan32x4()
                {

                    px = N[0].w,
                    py = N[1].w,
                    pz = N[2].w,
                    nx = N[3].w,
                    ny = N[4].w,
                    nz = N[5].w,
                    tx = N[6].w,
                    ty = N[7].w,
                    tz = N[8].w,
                    tw = N[9].w

                };

                vertexData[i3] = new PosNor32x3Tan32x4()
                {

                    px = N[0].z,
                    py = N[1].z,
                    pz = N[2].z,
                    nx = N[3].z,
                    ny = N[4].z,
                    nz = N[5].z,
                    tx = N[6].z,
                    ty = N[7].z,
                    tz = N[8].z,
                    tw = N[9].z

                };

                vertexData[i2] = new PosNor32x3Tan32x4()
                {

                    px = N[0].y,
                    py = N[1].y,
                    pz = N[2].y,
                    nx = N[3].y,
                    ny = N[4].y,
                    nz = N[5].y,
                    tx = N[6].y,
                    ty = N[7].y,
                    tz = N[8].y,
                    tw = N[9].y

                };

                vertexData[i1] = new PosNor32x3Tan32x4()
                {

                    px = N[0].x,
                    py = N[1].x,
                    pz = N[2].x,
                    nx = N[3].x,
                    ny = N[4].x,
                    nz = N[5].x,
                    tx = N[6].x,
                    ty = N[7].x,
                    tz = N[8].x,
                    tw = N[9].x

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

            public WokeNativeArray<PosNor32x3Tan32x4>.Concurrent GetVertexData()
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
