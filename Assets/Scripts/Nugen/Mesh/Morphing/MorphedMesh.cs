using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Collections;

using nugen.mesh.data;
using nugen.mesh.data.vertex;

namespace nugen.mesh.morphing
{

    public class MorphedMesh<T0, T1, T2, T3, T4> where T0 : struct, IVertexData<T0> where T1 : struct, IVertexData<T1> where T2 : struct, IVertexData<T2> where T3 : struct, IVertexData<T3> where T4 : struct, IMeshData<T0, T1, T2, T3, T4>
    {

        private readonly IMeshData<T0, T1, T2, T3, T4> data;

        public IMeshData<T0, T1, T2, T3, T4> Data
        {

            get { return data; }

        }

        public MorphedMesh(IMeshData<T0, T1, T2, T3, T4> data, Mesh mesh = null)
        {

            this.data = data;

            if (mesh != null) Inject(mesh);

        }

        public void Dispose()
        {

            data.Dispose();

        }

        public void Inject(Mesh mesh)
        {

            data.SetPositions(mesh.vertices);
            data.SetNormals(mesh.normals);
            data.SetTangents(mesh.tangents);

            if (data.HasColors()) data.SetColors(mesh.colors);

            if (data.IsSkinned()) data.SetBoneWeights(mesh.GetBonesPerVertex().ToArray(), mesh.GetAllBoneWeights().ToArray());

            if (data.HasUV(0)) data.SetUV(0, mesh.uv);
            if (data.HasUV(1)) data.SetUV(1, mesh.uv2);
            if (data.HasUV(2)) data.SetUV(2, mesh.uv3);
            if (data.HasUV(3)) data.SetUV(3, mesh.uv4);
            if (data.HasUV(4)) data.SetUV(4, mesh.uv5);
            if (data.HasUV(5)) data.SetUV(5, mesh.uv6);
            if (data.HasUV(6)) data.SetUV(6, mesh.uv7);
            if (data.HasUV(7)) data.SetUV(7, mesh.uv8);

        }

    }

}
