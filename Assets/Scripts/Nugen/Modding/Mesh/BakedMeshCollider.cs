using UnityEngine;

using nugen.mesh;

namespace nugen.modding
{

    public class BakedMeshCollider : MonoBehaviour
    {

        public SkinnedMeshRenderer referenceRenderer;

        public MeshFilter referenceFilter;

        void Start()
        {

            if (referenceRenderer == null && referenceFilter == null) referenceRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();

            if (referenceRenderer == null && referenceFilter == null) referenceFilter = gameObject.GetComponent<MeshFilter>();

        }

        private MeshCollider meshCollider;

        private Mesh bakedMesh;

        public Vector3[] GetVertices()
        {

            if (bakedMesh == null) UpdateMesh();

            return bakedMesh.vertices;

        }

        public Vector3[] GetNormals()
        {

            if (bakedMesh == null) UpdateMesh();

            return bakedMesh.normals;

        }

        public int[] GetTriangles()
        {

            if (bakedMesh == null) UpdateMesh();

            return bakedMesh.triangles;

        }

        public MeshCollider GetMeshCollider()
        {

            if (meshCollider != null) return meshCollider;

            meshCollider = gameObject.AddOrGetComponent<MeshCollider>();

            meshCollider.convex = false;

            return meshCollider;

        }

        public void UpdateMesh()
        {

            bool isFilter = referenceRenderer == null;

            if (isFilter)
            {

                if (referenceFilter == null) return;

                bakedMesh = referenceFilter.sharedMesh;

                GetMeshCollider().sharedMesh = bakedMesh;

            }
            else
            {

                if (bakedMesh == null) bakedMesh = MeshHelper.DuplicateMesh(referenceRenderer.sharedMesh);

                referenceRenderer.BakeMesh(bakedMesh);

                GetMeshCollider().sharedMesh = bakedMesh;

            }

        }

    }

}
