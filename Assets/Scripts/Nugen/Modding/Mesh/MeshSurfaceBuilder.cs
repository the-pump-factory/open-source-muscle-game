using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using nugen.mesh;
using nugen.mesh.data;

namespace nugen.modding
{

    public class MeshSurfaceBuilder : MonoBehaviour
    {

        public MeshSurfaceData surfaceData;

        new public SkinnedMeshRenderer renderer;

        public MeshFilter filter;

        public Mesh mesh
        {

            get
            {

                return renderer == null ? filter == null ? null : filter.sharedMesh : renderer.sharedMesh;

            }

        }

        public bool generate = false;

        private void Awake()
        {

            if (renderer == null) renderer = gameObject.GetComponent<SkinnedMeshRenderer>();

            if (filter == null) filter = gameObject.GetComponent<MeshFilter>();

        }

        private void Update()
        {

            if (generate)
            {

                generate = false;

                GenerateSurfaceData(mesh);

            }

        }

        public static MeshSurfaceData GenerateSurfaceData(Mesh mesh, string clusterTag = "_CLUSTER", string preservationTag = "_PRESERVE", bool UVgrouping = true, TextureUtilities.RGBAChannel smoothingChannel = TextureUtilities.RGBAChannel.R, TextureUtilities.RGBAChannel neighborMixChannel = TextureUtilities.RGBAChannel.G, TextureUtilities.RGBAChannel clusterMixChannel = TextureUtilities.RGBAChannel.B)
        {

            if (mesh == null) return new MeshSurfaceData();

            // Initialize Surface Data Vertex Array

            MeshSurfaceData surfaceData = new MeshSurfaceData()
            {

                vertices = new MeshSurfaceData.Vertex[mesh.vertexCount]

            };

            for (int a = 0; a < surfaceData.vertices.Length; a++)
            {

                MeshSurfaceData.Vertex vertex = surfaceData.vertices[a];

                vertex.index = a;
                vertex.parent = -1;
                vertex.clusterId = -1;

                surfaceData.vertices[a] = vertex;

            }

            //

            List<MeshSurfaceData.Cluster> clusters = new List<MeshSurfaceData.Cluster>();

            MeshSurfaceData.Cluster CreateNewCluster()
            {

                MeshSurfaceData.Cluster cluster = new MeshSurfaceData.Cluster()
                {

                    id = clusters.Count

                };

                clusters.Add(cluster);

                return cluster;

            }

            mesh.CreateEvaluationStack(out Vector3[] vertices, out VertexClone[] vertexClones, out VertexConnection[][] vertexConnections);

            Vector2[] uvs = mesh.uv;

            Color[] colors = mesh.colors;

            float ExtractSmoothingValue(Color col)
            {

                switch (smoothingChannel)
                {

                    case TextureUtilities.RGBAChannel.R:
                        return col.r;
                    case TextureUtilities.RGBAChannel.G:
                        return col.g;
                    case TextureUtilities.RGBAChannel.B:
                        return col.b;
                    case TextureUtilities.RGBAChannel.A:
                        return col.a;

                }

                return col.r;

            }

            float ExtractNeighborMixValue(Color col)
            {

                switch (neighborMixChannel)
                {

                    case TextureUtilities.RGBAChannel.R:
                        return col.r;
                    case TextureUtilities.RGBAChannel.G:
                        return col.g;
                    case TextureUtilities.RGBAChannel.B:
                        return col.b;
                    case TextureUtilities.RGBAChannel.A:
                        return col.a;

                }

                return col.r;

            }

            float ExtractClusterMixValue(Color col)
            {

                switch (clusterMixChannel)
                {

                    case TextureUtilities.RGBAChannel.R:
                        return col.r;
                    case TextureUtilities.RGBAChannel.G:
                        return col.g;
                    case TextureUtilities.RGBAChannel.B:
                        return col.b;
                    case TextureUtilities.RGBAChannel.A:
                        return col.a;

                }

                return col.r;

            }

            bool hasUV = uvs != null && uvs.Length > 0;

            VertexClone[] uvClones = null;

            if (hasUV && UVgrouping) uvClones = MeshHelper.CalculateUVClones(uvs, 0.000001f, vertices);

            bool hasColors = colors != null && colors.Length > 0;


            VertexGroupExtractor extractor = new VertexGroupExtractor();

            mesh = extractor.ExtractVertexGroups(mesh, true, true, false, clusterTag);

            foreach(VertexGroup vg in extractor.vertexGroups)
            {

                MeshSurfaceData.Cluster cluster = CreateNewCluster();

                for(int a = 0; a < vg.indices.Count; a++)
                {

                    int index = vg.indices[a];
                    float weight = vg.weights[a];

                    MeshSurfaceData.Vertex vertex = surfaceData.vertices[index];

                    if (vertex.clusterId >= 0) continue;

                    cluster.AddIndex(index, weight);

                    vertex.clusterId = cluster.id;

                    surfaceData.vertices[index] = vertex;

                }

            }

            extractor = new VertexGroupExtractor();

            mesh = extractor.ExtractVertexGroups(mesh, true, true, false, preservationTag);

            foreach (VertexGroup vg in extractor.vertexGroups)
            {

                for (int a = 0; a < vg.indices.Count; a++)
                {

                    int index = vg.indices[a];
                    float weight = vg.weights[a];

                    MeshSurfaceData.Vertex vertex = surfaceData.vertices[index];

                    vertex.originalMix = weight;

                    surfaceData.vertices[index] = vertex;

                }

            }

            bool[] evaluated = new bool[mesh.vertexCount];

            void EvaluateVertex(int index)
            {

                if (evaluated[index]) return;

                MeshSurfaceData.Vertex vertex = surfaceData.vertices[index];

                vertex.index = index;

                evaluated[index] = true;

                if (uvClones != null) // Handles meshes that have a thickness but identical uvs on each side
                {

                    VertexClone uvClone = uvClones[index];

                    //vertex.parent = uvClone.first;

                    int par = index;

                    foreach (int i in uvClone.indices) if (i < par) par = i;

                    vertex.parent = par;

                }

                VertexClone clones = vertexClones[index];

                if (clones.indices != null) foreach (int clone in clones.indices) evaluated[clone] = true;

                if (hasColors)
                {

                    Color color = colors[index];

                    vertex.smoothingWeight = ExtractSmoothingValue(color);

                    vertex.neighborMix = ExtractNeighborMixValue(color);

                    vertex.clusterMix = ExtractClusterMixValue(color);

                }

                surfaceData.vertices[index] = vertex;

                if (clones.indices != null)
                {

                    foreach (int clone in clones.indices)
                    {

                        MeshSurfaceData.Vertex vertex_ = vertex;

                        vertex_.index = clone;

                        surfaceData.vertices[clone] = vertex_;

                    }

                }

            }

            for (int a = 0; a < mesh.vertexCount; a++) EvaluateVertex(a);


            surfaceData.clusters = clusters.ToArray();

            surfaceData.mesh = mesh;

            return surfaceData;

        }

    }

}
