using System.Runtime.InteropServices;
using System;

using Unity.Mathematics;

namespace nugen.mesh.data.vertex
{

    [System.Serializable, StructLayout(LayoutKind.Sequential)]
    public struct Skin16x2 : IVertexData<Skin16x2>, IEquatable<Skin16x2>
    {

        public ushort bw;

        public ushort bi;

        public Skin16x2 Add(Skin16x2 val)
        {

            Skin16x2 nval = this;

            nval.bw = (ushort)(bw + math.select(0, val.bw, bi == val.bi));

            return nval;

        }

        public bool Equals(Skin16x2 other)
        {

            return
                bw == other.bw &&
                bi == other.bi;

        }

    }

}
