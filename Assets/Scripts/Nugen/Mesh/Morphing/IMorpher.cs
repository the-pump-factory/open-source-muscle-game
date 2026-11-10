using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Jobs;

namespace nugen.mesh.morphing
{
    public unsafe interface IMorpher
    {

        public JobHandle GetJobDependency();

        public void* GetVertexBufferReadOnlyPtr();

        public int GetVertexBufferStride();

        public int GetVertexBufferOffset();

        public void* GetNormalBufferReadOnlyPtr();

        public int GetNormalBufferStride();

        public int GetNormalBufferOffset();

        public Mesh GetInstancedMesh();

        public SkinnedMeshRenderer GetRenderer();

    }

}
