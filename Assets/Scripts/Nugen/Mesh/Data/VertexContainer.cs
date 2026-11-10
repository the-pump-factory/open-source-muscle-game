using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Collections;
using Unity.Mathematics;

namespace nugen.mesh.data
{
    public class VertexContainer : MonoBehaviour
    {

        private NativeArray<float3> vertices;

        public bool HasData()
        {

            return vertices.IsCreated;

        }

        public void SetVertices(NativeArray<float3> vertices)
        {

            if (this.vertices.IsCreated) this.vertices.Dispose();

            this.vertices = vertices;

        }

        public void SetVertices(Vector3[] vertices)
        {

            if (this.vertices.IsCreated) this.vertices.Dispose();

            this.vertices = new NativeArray<float3>(vertices.Length, Allocator.Persistent);

            for (int a = 0; a < vertices.Length; a++) this.vertices[a] = vertices[a];

        }

        public NativeArray<float3> GetVertices() { return vertices; }

        private void OnDestroy()
        {
            
            if (vertices.IsCreated)
            {

                vertices.Dispose();

                vertices = default(NativeArray<float3>);

            }

        }

    }

}
