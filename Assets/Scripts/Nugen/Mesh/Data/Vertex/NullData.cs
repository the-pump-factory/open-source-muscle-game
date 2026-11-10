using System.Runtime.InteropServices;
using System;
using Unity.Mathematics;

namespace nugen.mesh.data.vertex
{

    public struct NullData : IVertexData<NullData>, IEquatable<NullData>
    {

        public NullData Add(NullData val)
        {

            return val;

        }

        public bool Equals(NullData other)
        {

            return true;

        }

    }

}
