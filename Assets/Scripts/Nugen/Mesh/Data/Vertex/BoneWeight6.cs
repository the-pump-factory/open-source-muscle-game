using System.Runtime.InteropServices;
using System;
using Unity.Mathematics;

namespace nugen.mesh.data.vertex
{

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct BoneWeight6
    {

        public int indices0, indices1, indices2, indices3, indices4, indices5;

        public float weights0, weights1, weights2, weights3, weights4, weights5;

    }

}
