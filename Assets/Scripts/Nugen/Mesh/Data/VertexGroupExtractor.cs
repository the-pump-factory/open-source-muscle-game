using System;
using System.Collections.Generic;
using UnityEngine;

namespace nugen.mesh.data
{

    /// <summary>
    /// Converts blendshape vertex offset data into scalar weight values. The smallest vertex offset magnitude (usually 0) is used as the minimum, and the largest magnitude as the maximum. The data can then be normalized by dividing by the maximum.
    /// </summary>
    public class VertexGroupExtractor
    {

        public const string tag_Split = "_SPLIT";

        #region Vertex Groups

        public List<VertexGroup> vertexGroups = new List<VertexGroup>();

        public int AddVertexGroup(VertexGroup vertexGroup)
        {

            if (vertexGroups == null) vertexGroups = new List<VertexGroup>();

            int i = vertexGroups.Count;

            vertexGroups.Add(vertexGroup);

            return i;

        }

        /// <summary>
        /// Average all Vertex Groups weights using the combined maximum at each index.
        /// </summary>
        public void PoolVertexGroupsWithCombinedAverage(bool normalize = true)
        {

            if (vertexGroups == null) return;

            Dictionary<int, float> weights = new Dictionary<int, float>();

            foreach (VertexGroup vg in vertexGroups)
            {

                for (int b = 0; b < vg.indices.Count; b++)
                {

                    int i = vg.indices[b];

                    weights.TryGetValue(i, out float w);

                    weights[i] = w + vg.weights[b];

                }

            }

            List<VertexGroup> vgroups = new List<VertexGroup>();

            foreach (VertexGroup vg in vertexGroups)
            {

                List<int> indexes = new List<int>();
                List<float> weightValues = new List<float>();

                for (int b = 0; b < vg.indices.Count; b++)
                {

                    int i = vg.indices[b];

                    float total = weights[i];

                    if (total <= 0) continue;

                    indexes.Add(i);

                    weightValues.Add(vg.weights[b] / total);

                }

                if (indexes.Count == 0) continue;

                vg.indices = indexes;
                vg.weights = weightValues;

                vgroups.Add(vg);

            }

            vertexGroups = vgroups;

            if (normalize) NormalizeVertexGroupsAsPool();

        }

        /// <summary>
        /// Normalize all Vertex Groups weights using the maximum weight gleaned from checking every index in every group.
        /// </summary>
        public void NormalizeVertexGroupsAsPool()
        {

            float maxWeight = 0;

            foreach(VertexGroup vg in vertexGroups)
            {

                foreach (float weight in vg.weights) if (Mathf.Abs(weight) > maxWeight) maxWeight = weight;

            }

            foreach(VertexGroup vg in vertexGroups)
            {

                for (int a = 0; a < vg.weights.Count; a++) vg.weights[a] = vg.weights[a] / maxWeight;

            }

        }

        public static BlendShape SplitShape(Vector3[] vertices, BlendShape shape, float axisDistance = 0.005f, float maxDistance = 0.05f, float distanceExp = 1.65f, float threshold = 0.0001f)
        {

            float sqrThreshold = threshold * threshold;

            bool inProximity = false;

            BlendShape mirror = shape;

            if (axisDistance >= 0)
            {

                for (int a = 0; a < mirror.frames.Length; a++)
                {

                    BlendShape.Frame frame = mirror.frames[a];

                    for (int b = 0; b < frame.deltaVertices.Length; b++)
                    {

                        if (frame.deltaVertices[b].sqrMagnitude < sqrThreshold) continue;

                        if (Mathf.Abs(vertices[b].x) < axisDistance)
                        {

                            inProximity = true;

                            break;

                        }

                    }

                    if (inProximity) break;

                }

            }

            mirror = shape.Clone();

            shape.name = shape.name.Replace(tag_Split, "_R");

            mirror.name = mirror.name.Replace(tag_Split, "_L");

            if (inProximity)
            {

                for (int a = 0; a < mirror.frames.Length; a++)
                {

                    BlendShape.Frame frame = shape.frames[a];

                    BlendShape.Frame frame_mirror = mirror.frames[a];

                    for (int b = 0; b < frame.deltaVertices.Length; b++)
                    {

                        Vector3 v = vertices[b];

                        if (v.x > 0)
                        {

                            frame_mirror.deltaVertices[b] = frame_mirror.deltaVertices[b] * Mathf.Pow(1 - Mathf.Clamp01(v.x / maxDistance), distanceExp);

                        }
                        else if (v.x < 0)
                        {

                            frame.deltaVertices[b] = frame.deltaVertices[b] * Mathf.Pow(1 - Mathf.Clamp01((-v.x) / maxDistance), distanceExp);

                        }

                    }

                }

            }
            else
            {

                for (int a = 0; a < mirror.frames.Length; a++)
                {

                    BlendShape.Frame frame = shape.frames[a];

                    BlendShape.Frame frame_mirror = mirror.frames[a];

                    for (int b = 0; b < frame.deltaVertices.Length; b++)
                    {

                        Vector3 v = vertices[b];

                        if (v.x > 0)
                        {

                            frame_mirror.deltaVertices[b] = Vector3.zero;

                        }
                        else if (v.x < 0)
                        {

                            frame.deltaVertices[b] = Vector3.zero;

                        }

                    }

                }

            }

            return mirror;

        }

        public VertexGroup ConvertToVertexGroup(BlendShape shape, bool normalize = true, string keyword = "", float threshold = 0.0001f)
        {

            VertexGroup vg = VertexGroup.ConvertToVertexGroup(shape, normalize, keyword, threshold);

            if (vg.indices.Count > 0) AddVertexGroup(vg);

            return vg;

        }

        public void ExtractVertexGroups(SkinnedMeshRenderer renderer, bool removeFromMesh = true, bool normalize = true, bool normalizeToMesh = false, string keyword = "_VGROUP", float threshold = 0.0001f)
        {

            if (renderer == null) return;

            Mesh mesh = renderer.sharedMesh;

            if (mesh == null) return;

            renderer.sharedMesh = ExtractVertexGroups(mesh, removeFromMesh, normalize, normalizeToMesh, keyword, threshold);

        }

        /// <summary>
        /// Converts vertex group data stored in blendshapes into useable vertex groups.
        /// </summary>
        public Mesh ExtractVertexGroups(Mesh mesh, bool removeFromMesh = true, bool normalize = true, bool averageAsPool = false, string keyword = "_VGROUP", float threshold = 0.0001f)
        {

            if (mesh == null) return null;

            Vector3[] vertices = mesh.vertices;

            bool IsVertexGroup(string shapeName)
            {

                return shapeName.IndexOf(keyword) >= 0;

            }

            List<BlendShape> blendShapes = new List<BlendShape>();

            for (int a = 0; a < mesh.blendShapeCount; a++)
            {

                string id = mesh.GetBlendShapeName(a);

                BlendShape shape = new BlendShape(mesh, id);

                if (IsVertexGroup(id))
                {

                    if (shape.name.IndexOf(tag_Split) >= 0)
                    {

                        ConvertToVertexGroup(SplitShape(vertices, shape));

                    }

                    ConvertToVertexGroup(shape);

                }
                else
                {

                    blendShapes.Add(shape);

                }

            }

            if (removeFromMesh)
            {

                string meshName = mesh.name;

                mesh = MeshHelper.DuplicateMesh(mesh);

                mesh.name = meshName;

                mesh.ClearBlendShapes();

                for (int a = 0; a < blendShapes.Count; a++) blendShapes[a].AddToMesh(mesh);

            }

            if (averageAsPool) PoolVertexGroupsWithCombinedAverage(normalize);

            return mesh;

        }

        #endregion

        #region Main

        [NonSerialized]
        public Behaviour behaviour;

        public class Behaviour : MonoBehaviour
        {

            new public SkinnedMeshRenderer renderer;

            [NonSerialized]
            private VertexGroupExtractor extractor;

            public VertexGroupExtractor Extractor
            {

                get
                {

                    if (extractor == null)
                    {

                        extractor = new VertexGroupExtractor();

                        extractor.behaviour = this;

                    }

                    return extractor;

                }

            }

            private void Start()
            {

                if (extractor == null) extractor = new VertexGroupExtractor();

                extractor.behaviour = this;

                if (renderer == null) renderer = gameObject.GetComponent<SkinnedMeshRenderer>();

                if (renderer == null)
                {

                    GameObject.Destroy(this);

                    return;

                }

                if (extractor.vertexGroups == null || extractor.vertexGroups.Count == 0) extractor.ExtractVertexGroups(renderer);

            }

        }

        #endregion

    }

}
