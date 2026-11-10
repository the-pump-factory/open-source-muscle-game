using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;

using nugen.collections;
using nugen.mesh.data;
using nugen.mesh.data.vertex;

namespace nugen.mesh.morphing
{

    public class DefaultSkinnedMorphedMesh : MorphedMesh<PosNor32x3Tan32x4, Col8x4Tex32x4, Skin16x8, NullData, SkinnedMeshDefault>
    {

        public DefaultSkinnedMorphedMesh(SkinnedMeshDefault data, Mesh mesh = null) : base(data, mesh) { }

        public ShapeMorph.MorphJobBuilder<PosNor32x3Tan32x4, PosNor32x3Tan32x4.Editor> GetBuilder(ShapeMorph morph)
        {

            return morph;

        }

        public static implicit operator PosNor32x3Tan32x4.Editor(DefaultSkinnedMorphedMesh mesh)
        {

            return new PosNor32x3Tan32x4.Editor(mesh.Data.GetBus0());

        }

    }

}
