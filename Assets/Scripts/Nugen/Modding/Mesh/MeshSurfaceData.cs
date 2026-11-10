using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

using nugen.mesh;
using nugen.mesh.data;
using nugen.collections;

namespace nugen.modding
{

    public class MeshSurfaceData
    {

        public struct Vertex
        {

            public int index;

            public int parent;

            public int[] siblings;

            public int clusterId;

            public float smoothingWeight;

            public float neighborMix;

            public float clusterMix;

            public float originalMix;

            public bool HasSiblings()
            {

                return siblings != null && siblings.Length > 0;

            }

            public bool IsClustered()
            {

                return clusterId > 0;

            }

            public Cluster GetCluster(Cluster[] clusters)
            {

                return IsClustered() ? clusters[clusterId - 1] : new Cluster();

            }

        }

        /// <summary>
        /// A group of vertices within a mesh to be treated as one entity
        /// </summary>
        public class Cluster
        {

            public int id;

            public WeightIndexPair[] indices;

            public int AddIndex(int index, float weight)
            {

                return AddIndex(new WeightIndexPair(index, weight));

            }

            public int AddIndex(WeightIndexPair pair)
            {

                if (indices == null) indices = new WeightIndexPair[0];

                int index = indices.Length;

                indices = (WeightIndexPair[])indices.Add(pair);

                return index;

            }

            public void Normalize()
            {

                float total = 0;

                for (int a = 0; a < indices.Length; a++) total += indices[a].weight;

                if (total != 0) for (int a = 0; a < indices.Length; a++) indices[a] = new WeightIndexPair(indices[a].index, indices[a].weight / total);

            }

            public bool Contains(int id)
            {

                foreach (WeightIndexPair i in indices) if (i.index == id) return true;

                return false;

            }

            public Vector3 CalculateCenter(Vector3[] vertexPositions)
            {

                Vector3 center = Vector3.zero;

                if (indices == null || indices.Length == 0) return center;

                foreach (WeightIndexPair i in indices) center += vertexPositions[i.index];

                center = center / indices.Length;

                return center;

            }

            public Vector3 CalculateWeightedCenter(Vector3[] vertexPositions)
            {

                Vector3 center = Vector3.zero;

                if (indices == null || indices.Length == 0) return center;

                foreach (WeightIndexPair i in indices) center += vertexPositions[i.index] * i.weight;

                center = center / indices.Length;

                return center;

            }

        }

        public Mesh mesh;

        public Vertex[] vertices;

        public Cluster[] clusters;

        public delegate void CompletionDelegate(int index);

        public delegate bool MixNeighborsBlock(Vertex vertex, bool[] completionArray, CompletionDelegate completion);

        public void MixNeighbors(Mesh mesh, VertexClone[] clones, MixNeighborsBlock execute)
        {

            bool[] complete = new bool[mesh.vertexCount];

            void Complete(int index)
            {

                complete[index] = true;

                VertexClone clone = clones[index];

                foreach (int sibling in clone.indices) complete[sibling] = true;

            }

            bool Evaluate(int index)
            {

                if (complete[index]) return true;

                Vertex vertex = vertices[index];

                if (vertex.neighborMix <= 0)
                {

                    Complete(index);

                    return true;

                }

                return execute(vertex, complete, Complete);

            }

            List<int> retry = new List<int>();

            for (int a = 0; a < mesh.vertexCount; a++) if (!Evaluate(a)) retry.Add(a);

            while (retry.Count > 0)
            {

                int count = retry.Count;

                for (int a = 0; a < retry.Count; a++)
                {

                    int retryIndex = retry[a];

                    if (!Evaluate(retryIndex)) retry.Add(retryIndex);

                }

                retry.RemoveRange(0, count);

                if (retry.Count == count) break;

            }

        }

        public void EvaluateBoneWeights(Mesh mesh, int smoothingIterations, float smoothingFactor, out BoneWeight1[] boneWeights, out byte[] boneWeightCounts, VertexClone[] clones = null, VertexConnection[][] connections = null, BoneWeight1[] startingBoneWeights = null, byte[] startingBoneCounts = null)
        {

            if (clones == null) clones = MeshHelper.CalculateClones(mesh.vertices);

            if (connections == null) connections = mesh.GetVertexConnections(clones);

            NativeArray<BoneWeight1> originalBoneWeights = new NativeArray<BoneWeight1>(mesh.GetAllBoneWeights(), Allocator.Persistent);
            NativeArray<byte> originalBoneWeightCounts = new NativeArray<byte>(mesh.GetBonesPerVertex(), Allocator.Persistent);

            BoneWeight1[][] boneWeightArray = new BoneWeight1[mesh.vertexCount][];

            void SetWeightArray(int index, BoneWeight1[] array)
            {

                boneWeightArray[index] = array;

                VertexClone clone = clones[index];

                foreach (int sibling in clone.indices) boneWeightArray[sibling] = array;

            }

            int boneIndex = 0;

            for(int a = 0; a < mesh.vertexCount; a++)
            {

                byte count = originalBoneWeightCounts[a];

                BoneWeight1[] array = new BoneWeight1[count];

                for (int b = 0; b < count; b++) array[b] = originalBoneWeights[boneIndex + b];

                boneWeightArray[a] = array;

                boneIndex += count;

            }

            originalBoneWeights.Dispose();
            originalBoneWeightCounts.Dispose();

            // Handle Clusters

            foreach (Cluster cluster in clusters)
            {

                if (cluster.indices == null || cluster.indices.Length == 0) continue;

                MeshHelper.BoneWeightDelta bwd = new MeshHelper.BoneWeightDelta();

                foreach (var pair in cluster.indices)
                {

                    BoneWeight1[] array = boneWeightArray[pair.index];

                    for (int a = 0; a < array.Length; a++)
                    {

                        bwd.AddWeightDelta(array[a], pair.weight);

                    }

                }

                var weights = bwd.GetOrderedWeights();

                BoneWeight1[] newArray = new BoneWeight1[weights.Count];

                for (int a = 0; a < weights.Count; a++) newArray[a] = new BoneWeight1()
                {

                    boneIndex = weights[a].index,
                    weight = weights[a].weight

                };

                //foreach (var pair in cluster.indices) boneWeightArray[pair.index] = newArray;

                foreach (var pair in cluster.indices)
                {

                    Vertex vertex = vertices[pair.index];

                    if (vertex.clusterMix >= 1)
                    {

                        boneWeightArray[pair.index] = newArray;

                    } 
                    else
                    {

                        MeshHelper.BoneWeightDelta interpolated = MeshHelper.BoneWeightDelta.Lerp(new MeshHelper.BoneWeightDelta(boneWeightArray[pair.index]), bwd, vertex.clusterMix);

                        weights = interpolated.GetOrderedWeights();

                        BoneWeight1[] newArray_ = new BoneWeight1[weights.Count];

                        for (int a = 0; a < weights.Count; a++) newArray_[a] = new BoneWeight1()
                        {

                            boneIndex = weights[a].index,
                            weight = weights[a].weight

                        };

                        boneWeightArray[pair.index] = newArray_;

                    }

                }

            }

            // Handle Neighbor Mixing
            
            bool Mix(Vertex vertex, bool[] completed, CompletionDelegate complete)
            {

                MeshHelper.BoneWeightDelta bwd = new MeshHelper.BoneWeightDelta();

                VertexConnection[] connections_ = connections[vertex.index];

                bool valid = false;

                foreach (VertexConnection connection in connections_)
                {

                    Vertex vertex_ = vertices[connection.index];

                    if (!completed[vertex_.index]) continue;

                    valid = true;

                    BoneWeight1[] array = boneWeightArray[vertex_.index];

                    for (int a = 0; a < array.Length; a++) bwd.AddWeightDelta(array[a]);

                }

                if (!valid) return false;

                complete(vertex.index);

                bwd.Normalize();

                BoneWeight1[] weightArray = boneWeightArray[vertex.index];

                if (vertex.neighborMix < 1) bwd = MeshHelper.BoneWeightDelta.Lerp(new MeshHelper.BoneWeightDelta(weightArray), bwd, vertex.neighborMix);

                var weights = bwd.GetOrderedWeights();

                BoneWeight1[] newWeightArray = new BoneWeight1[Mathf.Min(Mathf.Max(4, weightArray.Length), weights.Count)];

                for (int a = 0; a < newWeightArray.Length; a++) newWeightArray[a] = new BoneWeight1() { boneIndex = weights[a].index, weight = weights[a].weight };

                newWeightArray = MeshHelper.Normalize(newWeightArray);

                SetWeightArray(vertex.index, newWeightArray);

                return true;

            }

            MixNeighbors(mesh, clones, Mix);

            // Handle Smoothing

            for(int s = 0; s < smoothingIterations; s++)
            {

                bool[] completed = new bool[mesh.vertexCount];

                for (int a = 0; a < mesh.vertexCount; a++)
                {

                    if (completed[a]) continue;

                    Vertex vertex = vertices[a];

                    if (vertex.smoothingWeight <= 0) continue;

                    MeshHelper.BoneWeightDelta bwd = new MeshHelper.BoneWeightDelta(boneWeightArray[a]);

                    VertexConnection[] connections_ = connections[a];

                    foreach (VertexConnection connection in connections_) {
                        
                        BoneWeight1[] array = boneWeightArray[connection.index];

                        foreach (BoneWeight1 bw in array) bwd.AddWeightDelta(bw);
                    
                    }

                    bwd = MeshHelper.BoneWeightDelta.Lerp(new MeshHelper.BoneWeightDelta(boneWeightArray[a]), bwd, smoothingFactor * vertex.smoothingWeight);

                    bwd.Normalize();

                    var weights = bwd.GetOrderedWeights();

                    BoneWeight1[] newArray = new BoneWeight1[weights.Count];

                    for (int b = 0; b < weights.Count; b++) newArray[b] = new BoneWeight1() { boneIndex = weights[b].index, weight = weights[b].weight };

                    VertexClone clone = clones[a];

                    foreach (int sibling in clone.indices) {

                        completed[sibling] = true;
                        
                        boneWeightArray[sibling] = newArray; 
                    
                    }

                }

            }

            // Handle Original Mixing

            if (startingBoneWeights != null && startingBoneCounts != null)
            {

                int currentIndex = 0;

                bool[] completed = new bool[mesh.vertexCount];

                for (int a = 0; a < mesh.vertexCount; a++)
                {

                    int startCount = startingBoneCounts[a];

                    if (!completed[a])
                    {

                        Vertex vertex = vertices[a];

                        if (vertex.originalMix > 0)
                        {

                            MeshHelper.BoneWeightDelta startWeights = new MeshHelper.BoneWeightDelta();

                            for (int b = 0; b < startCount; b++) startWeights.AddWeightDelta(startingBoneWeights[currentIndex + b]);

                            MeshHelper.BoneWeightDelta interpolated = new MeshHelper.BoneWeightDelta(boneWeightArray[a]).Lerp(startWeights, vertex.originalMix);

                            interpolated.Normalize();

                            var weights = interpolated.GetOrderedWeights();

                            BoneWeight1[] newArray = new BoneWeight1[weights.Count];

                            for (int b = 0; b < weights.Count; b++) newArray[b] = new BoneWeight1() { boneIndex = weights[b].index, weight = weights[b].weight };

                            VertexClone clone = clones[a];

                            foreach (int sibling in clone.indices)
                            {

                                completed[sibling] = true;

                                boneWeightArray[sibling] = newArray;

                            }

                        }

                    }

                    currentIndex += startCount;

                }

            }

            // Finish Up

            List<BoneWeight1> boneWeightList = new List<BoneWeight1>();

            boneWeightCounts = new byte[mesh.vertexCount];

            for(int a = 0; a < mesh.vertexCount; a++)
            {

                Vertex vertex = vertices[a];

                BoneWeight1[] array = vertex.parent < 0 || vertex.parent == a ? boneWeightArray[a] : boneWeightArray[vertex.parent];

                boneWeightCounts[a] = (byte)array.Length;

                for (int b = 0; b < array.Length; b++) boneWeightList.Add(array[b]);

            }

            boneWeights = boneWeightList.ToArray();

        }

        public void EvaluateBlendShape(BlendShape shape, int smoothingIterations, float smoothingFactor, VertexClone[] clones, VertexConnection[][] connections)
        {

            foreach (Cluster cluster in clusters)
            {

                if (cluster.indices == null || cluster.indices.Length == 0) continue;

                for (int a = 0; a < shape.frames.Length; a++)
                {

                    BlendShape.Frame frame = shape.frames[a];

                    Vector3 deltaVertex = Vector3.zero;
                    Vector3 deltaNormal = Vector3.zero;
                    Vector3 deltaTangent = Vector3.zero;

                    foreach (var pair in cluster.indices)
                    {

                        deltaVertex += frame.deltaVertices[pair.index] * pair.weight;
                        deltaNormal += frame.deltaNormals[pair.index] * pair.weight;
                        deltaTangent += frame.deltaTangents[pair.index] * pair.weight;

                    }

                    foreach (var pair in cluster.indices)
                    {

                        Vertex vertex = vertices[pair.index];

                        if (vertex.clusterMix >= 1)
                        {

                            frame.deltaVertices[pair.index] = deltaVertex;
                            frame.deltaNormals[pair.index] = deltaNormal;
                            frame.deltaTangents[pair.index] = deltaTangent;

                        } 
                        else
                        {

                            frame.deltaVertices[pair.index] = Vector3.Lerp(frame.deltaVertices[pair.index], deltaVertex, vertex.clusterMix);
                            frame.deltaNormals[pair.index] = Vector3.Lerp(frame.deltaNormals[pair.index], deltaNormal, vertex.clusterMix);
                            frame.deltaTangents[pair.index] = Vector3.Lerp(frame.deltaTangents[pair.index], deltaTangent, vertex.clusterMix);

                        }

                    }

                }

            }

            // Handle Neighbor Mixing

            bool Mix(Vertex vertex, bool[] completed, CompletionDelegate complete)
            {

                VertexConnection[] connections_ = connections[vertex.index];

                float combinationFactor = 0;

                foreach (VertexConnection connection in connections_)
                {

                    Vertex vertex_ = vertices[connection.index];

                    if (!completed[vertex_.index]) continue;

                    combinationFactor += 1;

                }

                if (combinationFactor <= 0) return false;

                complete(vertex.index);

                for (int a = 0; a < shape.frames.Length; a++)
                {

                    BlendShape.Frame frame = shape.frames[a];

                    Vector3 deltaVertex = Vector3.zero;
                    Vector3 deltaNormal = Vector3.zero;
                    Vector3 deltaTangent = Vector3.zero;

                    foreach (VertexConnection connection in connections_)
                    {

                        deltaVertex += frame.deltaVertices[connection.index] / combinationFactor;
                        deltaNormal += frame.deltaNormals[connection.index] / combinationFactor;
                        deltaTangent += frame.deltaTangents[connection.index] / combinationFactor;

                    }

                    if (vertex.neighborMix < 1) 
                    {

                        Vector3 originalDeltaVertex = frame.deltaVertices[vertex.index];
                        Vector3 originalDeltaNormal = frame.deltaNormals[vertex.index];
                        Vector3 originalDeltaTangent = frame.deltaTangents[vertex.index];

                        deltaVertex = Vector3.Lerp(originalDeltaVertex, deltaVertex, vertex.neighborMix);
                        deltaNormal = Vector3.Lerp(originalDeltaNormal, deltaNormal, vertex.neighborMix);
                        deltaTangent = Vector3.Lerp(originalDeltaTangent, deltaTangent, vertex.neighborMix);

                    }

                    VertexClone clone = clones[vertex.index];

                    foreach (int index in clone.indices)
                    {

                        frame.deltaVertices[index] = deltaVertex;
                        frame.deltaNormals[index] = deltaNormal;
                        frame.deltaTangents[index] = deltaTangent;

                    }

                }

                return true;

            }

            MixNeighbors(mesh, clones, Mix);

            // Handle Smoothing

            for (int s = 0; s < smoothingIterations; s++)
            {

                bool[] completed = new bool[mesh.vertexCount];

                for (int a = 0; a < mesh.vertexCount; a++)
                {

                    if (completed[a]) continue;

                    Vertex vertex = vertices[a];

                    if (vertex.smoothingWeight <= 0) continue;

                    VertexConnection[] connections_ = connections[a];

                    float combinationFactor = connections_.Length + 1;

                    for (int f = 0; f < shape.frames.Length; f++)
                    {

                        BlendShape.Frame frame = shape.frames[f];

                        Vector3 originalDeltaVertex = frame.deltaVertices[vertex.index];
                        Vector3 originalDeltaNormal = frame.deltaNormals[vertex.index];
                        Vector3 originalDeltaTangent = frame.deltaTangents[vertex.index];

                        Vector3 deltaVertex = originalDeltaVertex / combinationFactor;
                        Vector3 deltaNormal = originalDeltaNormal / combinationFactor;
                        Vector3 deltaTangent = originalDeltaTangent / combinationFactor;

                        foreach (VertexConnection connection in connections_)
                        {

                            deltaVertex += frame.deltaVertices[connection.index] / combinationFactor;
                            deltaNormal += frame.deltaNormals[connection.index] / combinationFactor;
                            deltaTangent += frame.deltaTangents[connection.index] / combinationFactor;

                        }

                        deltaVertex = Vector3.Lerp(originalDeltaVertex, deltaVertex, smoothingFactor * vertex.smoothingWeight);
                        deltaNormal = Vector3.Lerp(originalDeltaNormal, deltaNormal, smoothingFactor * vertex.smoothingWeight);
                        deltaTangent = Vector3.Lerp(originalDeltaTangent, deltaTangent, smoothingFactor * vertex.smoothingWeight);

                        VertexClone clone = clones[vertex.index];

                        foreach (int index in clone.indices)
                        {

                            completed[index] = true;

                            frame.deltaVertices[index] = deltaVertex;
                            frame.deltaNormals[index] = deltaNormal;
                            frame.deltaTangents[index] = deltaTangent;

                        }

                    }

                }

            }

        }

    }

}
