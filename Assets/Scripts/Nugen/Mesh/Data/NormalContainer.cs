using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Collections;
using Unity.Mathematics;

namespace nugen.mesh.data
{
    public class NormalContainer : MonoBehaviour
    {

        private NativeArray<float3> normals;

        public bool HasData()
        {

            return normals.IsCreated;

        }

        public void SetNormals(NativeArray<float3> normals)
        {

            if (this.normals.IsCreated) this.normals.Dispose();

            this.normals = normals;

        }

        public void SetNormals(Vector3[] normals)
        {

            if (this.normals.IsCreated) this.normals.Dispose();

            this.normals = new NativeArray<float3>(normals.Length, Allocator.Persistent);

            for (int a = 0; a < normals.Length; a++) this.normals[a] = normals[a];

        }

        public NativeArray<float3> GetNormals() { return normals; }

        private void OnDestroy()
        {
            
            if (normals.IsCreated)
            {

                normals.Dispose();

                normals = default(NativeArray<float3>);

            }

        }

    }

}
