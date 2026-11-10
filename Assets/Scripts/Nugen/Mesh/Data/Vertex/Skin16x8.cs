using System.Runtime.InteropServices;
using System;

using Unity.Mathematics;

namespace nugen.mesh.data.vertex
{

    [System.Serializable, StructLayout(LayoutKind.Sequential)]
    public struct Skin16x8 : IVertexData<Skin16x8>, IEquatable<Skin16x8>
    {

        public ushort bw0, bw1, bw2, bw3;

        public ushort bi0, bi1, bi2, bi3;

        public Skin16x8 Add(Skin16x8 val)
        {

            Skin16x8 nval = this;

            nval.bw0 = (ushort)(bw0 
                + math.select(0, val.bw0, val.bi0 == bi0) 
                + math.select(0, val.bw1, val.bi1 == bi0)
                + math.select(0, val.bw2, val.bi2 == bi0)
                + math.select(0, val.bw3, val.bi3 == bi0)
                );

            nval.bw1 = (ushort)(bw1
                + math.select(0, val.bw0, val.bi0 == bi1)
                + math.select(0, val.bw1, val.bi1 == bi1)
                + math.select(0, val.bw2, val.bi2 == bi1)
                + math.select(0, val.bw3, val.bi3 == bi1)
                );

            nval.bw2 = (ushort)(bw2
                + math.select(0, val.bw0, val.bi0 == bi2)
                + math.select(0, val.bw1, val.bi1 == bi2)
                + math.select(0, val.bw2, val.bi2 == bi2)
                + math.select(0, val.bw3, val.bi3 == bi2)
                );

            nval.bw3 = (ushort)(bw3
                + math.select(0, val.bw0, val.bi0 == bi3)
                + math.select(0, val.bw1, val.bi1 == bi3)
                + math.select(0, val.bw2, val.bi2 == bi3)
                + math.select(0, val.bw3, val.bi3 == bi3)
                );

            return nval;

        }

        public bool Equals(Skin16x8 other)
        {

            return bw0 == other.bw0 && bw1 == other.bw1 && bw2 == other.bw2 && bw3 == other.bw3 && bi0 == other.bi0 && bi1 == other.bi1 && bi2 == other.bi2 && bi3 == other.bi3;

        }
    }

}
