using System.Runtime.InteropServices;
using System;
using Unity.Mathematics;

namespace nugen.mesh.data.vertex
{

    [System.Serializable, StructLayout(LayoutKind.Sequential)]
    public struct Col8x4Tex32x4 : IVertexData<Col8x4Tex32x4>, IEquatable<Col8x4Tex32x4>
    {

        public byte cr, cg, cb, ca;

        public float ux, uy, uz, uw;

        public Col8x4Tex32x4 Add(Col8x4Tex32x4 val)
        {

            val.cr = (byte)(cr + val.cr);
            val.cg = (byte)(cg + val.cg);
            val.cb = (byte)(cb + val.cb);
            val.ca = (byte)(ca + val.ca);

            val.ux = ux + val.ux;
            val.uy = uy + val.uy;
            val.uz = uz + val.uz;
            val.uw = uw + val.uw;

            return val;

        }

        public bool Equals(Col8x4Tex32x4 other)
        {

            return cr == other.cr && cg == other.cg && cb == other.cb && ca == other.ca && ux == other.ux && uy == other.uy && uz == other.uz && uw == other.uw;

        }

    }

}
