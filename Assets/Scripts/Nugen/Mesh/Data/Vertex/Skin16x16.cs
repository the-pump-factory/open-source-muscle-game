using System.Runtime.InteropServices;
using System;

using Unity.Mathematics;

namespace nugen.mesh.data.vertex
{

    [System.Serializable, StructLayout(LayoutKind.Sequential)]
    public struct Skin16x16 : IVertexData<Skin16x16>, IEquatable<Skin16x16>
    {

        public ushort bw0, bw1, bw2, bw3, bw4, bw5, bw6, bw7;

        public ushort bi0, bi1, bi2, bi3, bi4, bi5, bi6, bi7;

        public Skin16x16 Add(Skin16x16 val)
        {

            Skin16x16 nval = this;

            int4 v1 = (new int4(bw0, bw1, bw2, bw3) + math.select(0, new int4(val.bw0, val.bw1, val.bw2, val.bw3), new bool4(val.bi0 == bi0, val.bi1 == bi1, val.bi2 == bi2, val.bi3 == bi3)));

            int4 v2 = (new int4(bw4, bw5, bw6, bw7) + math.select(0, new int4(val.bw4, val.bw5, val.bw6, val.bw7), new bool4(val.bi4 == bi4, val.bi5 == bi5, val.bi6 == bi6, val.bi7 == bi7)));

            nval.bw0 = (ushort)v1.x;
            nval.bw1 = (ushort)v1.y;
            nval.bw2 = (ushort)v1.z;
            nval.bw3 = (ushort)v1.w;

            nval.bw4 = (ushort)v2.x;
            nval.bw5 = (ushort)v2.y;
            nval.bw6 = (ushort)v2.z;
            nval.bw7 = (ushort)v2.w;

            return nval;

        }

        public bool Equals(Skin16x16 other)
        {

            return
                bw0 == other.bw0 &&
                bw1 == other.bw1 &&
                bw2 == other.bw2 &&
                bw3 == other.bw3 &&
                bw4 == other.bw4 &&
                bw5 == other.bw5 &&
                bw6 == other.bw6 &&
                bw7 == other.bw7 &&
                bi0 == other.bi0 &&
                bi1 == other.bi1 &&
                bi2 == other.bi2 &&
                bi3 == other.bi3 &&
                bi4 == other.bi4 &&
                bi5 == other.bi5 &&
                bi6 == other.bi6 &&
                bi7 == other.bi7;

        }

    }

}
