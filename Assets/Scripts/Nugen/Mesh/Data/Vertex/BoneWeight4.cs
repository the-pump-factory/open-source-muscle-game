using System.Runtime.InteropServices;
using System;
using Unity.Mathematics;

namespace nugen.mesh.data.vertex
{

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct BoneWeight4
    {

        public int4 indices;

        public float4 weights;

    }

}
