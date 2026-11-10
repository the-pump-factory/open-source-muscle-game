using System;

using Unity.Mathematics;

using nugen.collections;
using nugen.mesh.data.vertex;

namespace nugen.mesh.data
{
    public interface IMeshDataEditor<T> where T : struct, IVertexData<T>
    {

        public void Add(ContainerId containerIndex, int index, T val);

        public int GetVertexCount();

        public WokeNativeArray<T>.Concurrent GetVertexData();

        public void GetIndices(int startIndex, out int i1, out int i2, out int i3, out int i4);

        public void getdataf1(ContainerId containerIndex, int startIndex, out float4 X);

        public void setdataf1(ContainerId containerIndex, int startIndex, float4 X);

        public void getdataf2(ContainerId containerIndex, int startIndex, out float4 X, out float4 Y);

        public void setdataf2(ContainerId containerIndex, int startIndex, float4 X, float4 Y);

        public void getdataf3(ContainerId containerIndex, int startIndex, out float4 X, out float4 Y, out float4 Z);

        public void setdataf3(ContainerId containerIndex, int startIndex, float4 X, float4 Y, float4 Z);

        public void getdataf4(ContainerId containerIndex, int startIndex, out float4 X, out float4 Y, out float4 Z, out float4 W);

        public void setdataf4(ContainerId containerIndex, int startIndex, float4 X, float4 Y, float4 Z, float4 W);

        public void getdatafn(ContainerId containerIndex, int startIndex, out float4[] N);

        public void setdatafn(ContainerId containerIndex, int startIndex, params float4[] N);


        public void getdatai1(ContainerId containerIndex, int startIndex, out int4 X);

        public void setdatai1(ContainerId containerIndex, int startIndex, int4 X);

        public void getdatai2(ContainerId containerIndex, int startIndex, out int4 X, out int4 Y);

        public void setdatai2(ContainerId containerIndex, int startIndex, int4 X, int4 Y);

        public void getdatai3(ContainerId containerIndex, int startIndex, out int4 X, out int4 Y, out int4 Z);

        public void setdatai3(ContainerId containerIndex, int startIndex, int4 X, int4 Y, int4 Z);

        public void getdatai4(ContainerId containerIndex, int startIndex, out int4 X, out int4 Y, out int4 Z, out int4 W);

        public void setdatai4(ContainerId containerIndex, int startIndex, int4 X, int4 Y, int4 Z, int4 W);

        public void getdatain(ContainerId containerIndex, int startIndex, out int4[] N);

        public void setdatain(ContainerId containerIndex, int startIndex, params int4[] N);


    }

}
