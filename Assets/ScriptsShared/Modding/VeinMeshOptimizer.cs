using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;

using UnityEngine;

using Swole;
using Swole.DataStructures;

[ExecuteInEditMode]
public class VeinMeshOptimizer : MonoBehaviour
{

    [Header("VEIN MESH ARMATURE SHOULD BE EXPORTED WITH LEAF BONES AND IMPORTED WITH 'STRIP BONES' UNCHECKED!")]
    public bool apply;

    public bool debug;

    public SkinnedMeshRenderer targetRenderer;

    public List<string> edgeAffectingShapes;

    public int maxEdgeLoopLength = 8;
    public int minEdgeLoopLength = 4;
    public float maxEdgeToBoneDirDot = 0.6f;
    public float minInclusionWeight = 0.15f;
    public float junctionBoneDistanceThreshold = 0.001f;
    public float minFaceNormalToBoneDirDot = 0.68f;
    public Vector3 boneRollAxis = Vector3.up;

    public void Update()
    {
        
        if (apply)
        {

            apply = false;

            var mesh = Optimize(targetRenderer, edgeAffectingShapes, maxEdgeLoopLength, minEdgeLoopLength, maxEdgeToBoneDirDot, minInclusionWeight, junctionBoneDistanceThreshold, minFaceNormalToBoneDirDot, boneRollAxis, debug);

            if (Application.isPlaying) targetRenderer.sharedMesh = mesh;

        }

    }

    private struct EdgeLoop
    {

        public int[] indices;

        public EdgeLoop(params int[] indices)
        {

            this.indices = indices;

        }

        public bool Contains(int index)
        {

            if (indices != null) for (int a = 0; a < indices.Length; a++) if (indices[a] == index) return true;

            return false;

        }

        public bool Contains(int index, VertexClone[] clones)
        {

            index = clones[index].first;

            if (indices != null) for (int a = 0; a < indices.Length; a++) if (clones[indices[a]].first == index) return true;

            return false;

        }

    }

    public static readonly Vector3 defaultBoneRollAxis = Vector3.up;

    /// <summary>
    /// VEIN MESH ARMATURE SHOULD BE EXPORTED WITH LEAF BONES AND IMPORTED WITH 'STRIP BONES' UNCHECKED!
    /// </summary>
    public static Mesh Optimize(SkinnedMeshRenderer renderer, List<string> edgeAffectingShapes, int maxEdgeLoopLength = 8, int minEdgeLoopLength = 4, float maxEdgeToBoneDirDot = 0.6f, float minInclusionWeight = 0.15f, float junctionBoneDistanceThreshold = 0.001f, float minFaceNormalToBoneDirDot = 0.68f, Vector3 boneRollAxis = default, bool debug = false)
    {

        if (boneRollAxis.x == 0 && boneRollAxis.y == 0 && boneRollAxis.z == 0) boneRollAxis = defaultBoneRollAxis;

        Mesh mesh = renderer.sharedMesh;

        Transform[] bones = renderer.bones;
        /*int GetBoneIndex(Transform transform)
        {

            for (int a = 0; a < bones.Length; a++) if (bones[a] == transform) return a;

            return -1;

        } */
        Vector3[] worldVertices = RealtimeMesh.GetTransformedVertices(renderer);
        Vector3[] worldNormals = RealtimeMesh.GetTransformedNormals(renderer);

        HashSet<int> junctionBones = new HashSet<int>();
        HashSet<Transform> junctionBonesByRef = new HashSet<Transform>();
        for (int a = 0; a < bones.Length; a++)
        {

            var boneA = bones[a];

            Vector3 pos = boneA.position;

            for (int b = 0; b < bones.Length; b++)
            {

                if (a == b) continue;

                var boneB = bones[b];

                if ((pos - boneB.position).magnitude <= junctionBoneDistanceThreshold)
                {

                    junctionBones.Add(a);
                    junctionBones.Add(b);

                    junctionBonesByRef.Add(boneA);
                    junctionBonesByRef.Add(boneB);

                    break;

                }

            }

        }
        Debug.Log($"Found {junctionBones.Count} junction bones");
        bool IsJunctionBone(int boneIndex)
        {

            if (boneIndex < 0) return false;

            if (junctionBones.Contains(boneIndex)) return true;

            Transform bone = bones[boneIndex]; 

            //if (bone.parent != null && junctionBonesByRef.Contains(bone.parent)) return true;

            for (int a = 0; a < bone.childCount; a++)
            {
                var child = bone.GetChild(a);
                if (child != null && junctionBonesByRef.Contains(child)) return true;

            }

            return false;

        }
        if (debug)
        {
            for (int a = 0; a < bones.Length; a++)
            {

                var bone = bones[a];
                if (bone == null) continue;

                Color col = IsJunctionBone(a) ? Color.magenta : Color.green;

                for (int b = 0; b < bone.childCount; b++)
                {

                    Debug.DrawLine(bone.position, bone.GetChild(b).position, col, 30);

                }

            }
        }

        int[] orig_triangles = mesh.triangles;
        Vector3[] orig_vertices = mesh.vertices;
        Vector3[] orig_normals = mesh.normals;
        Vector4[] orig_tangents = mesh.tangents;
        Color[] orig_colors = mesh.colors;

        var clones = MeshUtils.CalculateClones(orig_vertices);

        Vector4[] orig_uv0 = mesh.GetUVsByChannelV4(0);
        Vector4[] orig_uv1 = mesh.GetUVsByChannelV4(1);
        Vector4[] orig_uv2 = mesh.GetUVsByChannelV4(2);
        Vector4[] orig_uv3 = mesh.GetUVsByChannelV4(3);

        NativeArray<BoneWeight1> orig_boneWeights = new NativeArray<BoneWeight1>(mesh.GetAllBoneWeights(), Allocator.Persistent);
        NativeArray<byte> orig_boneCounts = new NativeArray<byte>(mesh.GetBonesPerVertex(), Allocator.Persistent);
        int[] boneWeightArrayIndexOffsets = new int[mesh.vertexCount];

        int offset = 0;
        for(int a = 0; a < mesh.vertexCount; a++)
        {

            boneWeightArrayIndexOffsets[a] = offset;
            offset += (int)orig_boneCounts[a];

        }

        List<int>[] containingTris = new List<int>[mesh.vertexCount];
        for(int a = 0; a < orig_triangles.Length; a += 3)
        {

            int i0 = orig_triangles[a];
            int i1 = orig_triangles[a + 1];
            int i2 = orig_triangles[a + 2];

            void AddTri(int index)
            {

                var clone = clones[index];

                for (int b = 0; b < clone.indices.Length; b++)
                {

                    int ind = clone.indices[b];

                    if (containingTris[ind] == null) containingTris[ind] = new List<int>();
                    containingTris[ind].Add(a);

                }

            }

            AddTri(i0);
            AddTri(i1);
            AddTri(i2);

        }

        List<BlendShape> orig_blendShapes = mesh.GetBlendShapes();

        mesh = MeshUtils.DuplicateMesh(mesh);


        bool[] immunities = new bool[mesh.vertexCount];
        if (edgeAffectingShapes != null)
        {

            foreach (BlendShape shape in orig_blendShapes)
            {

                bool isEdgeAffecting = false;

                string shapeName = shape.name.ToLower().Trim();

                foreach (string edgeAffectingName in edgeAffectingShapes) if (!string.IsNullOrEmpty(edgeAffectingName) && edgeAffectingName.ToLower().Trim() == shapeName)
                    {
                        isEdgeAffecting = true;
                        break;
                    }

                if (!isEdgeAffecting) continue;

                foreach (var frame in shape.frames)
                {

                    for (int a = 0; a < frame.deltaVertices.Length; a++) if (frame.deltaVertices[a].magnitude > Mathf.Epsilon) immunities[a] = true;

                }

            }

        }

        List<int> candidateIndices = new List<int>();
        int[] candidateIndexTranslator = new int[mesh.vertexCount];
        for(int a = 0; a < mesh.vertexCount; a++)
        {

            int index = clones[a].first;

            if (immunities[index]) continue; 

            if (candidateIndices.Contains(index)) continue;

            int boneCount = orig_boneCounts[index];
            int boneWeightOffset = boneWeightArrayIndexOffsets[index];

            int inclusionCount = 0;

            for(int b = 0; b < boneCount; b++)
            {

                var bw = orig_boneWeights[b + boneWeightOffset];

                if (bw.weight >= minInclusionWeight) 
                {

                    bool isJunc = IsJunctionBone(bw.boneIndex);

                    inclusionCount += isJunc ? 2 : 1;

                    if (isJunc || inclusionCount > 1) break;
                
                }

            }

            if (inclusionCount <= 1)
            {

                candidateIndices.Add(index);
                candidateIndexTranslator[index] = candidateIndices.Count;

            }

        } 

        int GetControlBone(int localIndex)
        {

            int boneCount = orig_boneCounts[localIndex];
            int boneIndexOffset = boneWeightArrayIndexOffsets[localIndex];
            float minDist = float.MaxValue;
            int controlBone = -1;

            for (int b = 0; b < boneCount; b++)
            {

                var bw = orig_boneWeights[b + boneIndexOffset];
                if (bw.boneIndex >= 0 && bw.weight > 0)
                {
                    float dist = (bones[bw.boneIndex].position - worldVertices[localIndex]).sqrMagnitude;
                    dist = dist - (dist * bw.weight);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        controlBone = bw.boneIndex;
                    }
                }

            }

            return controlBone;

        }

        List<EdgeLoop> wastefulEdgeLoops = new List<EdgeLoop>();
        HashSet<int> evaluatedIndices = new HashSet<int>();
        bool FindEdgeLoop(int currentIndex, int targetBone, Vector3 boneWorldDir, Quaternion toBoneLocalAxis, List<int> indices, int depth, out EdgeLoop edgeLoop)
        {

            edgeLoop = new EdgeLoop();

            if (depth > maxEdgeLoopLength) return false;

            currentIndex = clones[currentIndex].first;

            if (depth < indices.Count) indices.RemoveRange(depth, indices.Count - depth);

            var tris = containingTris[currentIndex];

            Vector3 worldPos = worldVertices[currentIndex];

            if (tris != null)
            {

                for (int a = 0; a < tris.Count; a++)
                {

                    int triIndex = tris[a];

                    int i0 = clones[orig_triangles[triIndex]].first;
                    int i1 = clones[orig_triangles[triIndex + 1]].first;
                    int i2 = clones[orig_triangles[triIndex + 2]].first;

                    Vector3 norm = (Maths.CalcNormal(worldVertices[i0], worldVertices[i1], worldVertices[i2]) * 100).normalized;
                    if (Mathf.Abs(Vector3.Dot(norm, boneWorldDir)) > minFaceNormalToBoneDirDot) return false;  

                } 

                for(int a = 0; a < tris.Count; a++)
                {

                    int triIndex = tris[a];

                    int i0 = clones[orig_triangles[triIndex]].first;
                    int i1 = clones[orig_triangles[triIndex + 1]].first;
                    int i2 = clones[orig_triangles[triIndex + 2]].first;

                    float d0 = (worldVertices[i0] - worldPos).sqrMagnitude;
                    float d1 = (worldVertices[i1] - worldPos).sqrMagnitude;
                    float d2 = (worldVertices[i2] - worldPos).sqrMagnitude;

                    float dMax = Mathf.Max(d0, d1, d2);

                    bool EvaluateIndex(int index, out EdgeLoop edgeLoop)
                    {

                        edgeLoop = new EdgeLoop();

                        if (index == currentIndex) return false;

                        Vector3 edgeDir = ((worldVertices[index] - worldPos) * 100).normalized;

                        if (Mathf.Abs(Vector3.Dot(edgeDir, boneWorldDir)) > maxEdgeToBoneDirDot) return false; 

                        if (index == indices[0] && indices.Count > 2)
                        {

                            edgeLoop = new EdgeLoop(indices.ToArray());

                            return true;

                        }
                        else if (candidateIndexTranslator[index] > 0 && !indices.Contains(index))
                        {

                            int controlBone = GetControlBone(index);
                            if (controlBone != targetBone) return false;

                            /*int boneCount = orig_boneCounts[index];
                            int boneIndexOffset = boneWeightArrayIndexOffsets[index];
                            bool isBound = false;

                            for(int b = 0; b < boneCount; b++)
                            {

                                var bw = orig_boneWeights[b + boneIndexOffset];
                                if (bw.boneIndex >= 0 && bw.weight > 0 && bw.boneIndex == targetBone)
                                {

                                    isBound = true;
                                    break;

                                }

                            }

                            if (isBound)
                            {*/

                                indices.Add(index);

                                if (FindEdgeLoop(index, targetBone, boneWorldDir, toBoneLocalAxis, indices, depth + 1, out EdgeLoop newLoop))
                                {

                                    if (edgeLoop.indices == null || edgeLoop.indices.Length <= 0 || (edgeLoop.indices.Length > newLoop.indices.Length)) edgeLoop = newLoop;

                                }

                            //}

                        }

                        return edgeLoop.indices != null && edgeLoop.indices.Length > 0;

                    }

                    EdgeLoop newLoop;
                    if (dMax != d0 && EvaluateIndex(i0, out newLoop)) 
                    {

                        if (edgeLoop.indices == null || edgeLoop.indices.Length <= 0 || (edgeLoop.indices.Length > newLoop.indices.Length)) edgeLoop = newLoop;

                    }
                    if (dMax != d1 && EvaluateIndex(i1, out newLoop))
                    {

                        if (edgeLoop.indices == null || edgeLoop.indices.Length <= 0 || (edgeLoop.indices.Length > newLoop.indices.Length)) edgeLoop = newLoop;

                    }
                    if (dMax != d2 && EvaluateIndex(i2, out newLoop))
                    {

                        if (edgeLoop.indices == null || edgeLoop.indices.Length <= 0 || (edgeLoop.indices.Length > newLoop.indices.Length)) edgeLoop = newLoop;

                    }

                }

            }

            return edgeLoop.indices != null && edgeLoop.indices.Length > 0;

        }
        Debug.Log($"Evaluating {candidateIndices.Count} candidates");
        for (int a = 0; a < candidateIndices.Count; a++)
        {

            int localIndex = clones[candidateIndices[a]].first;

            if (evaluatedIndices.Contains(localIndex)) continue;

            int controlBone = GetControlBone(localIndex);

            if (controlBone >= 0)
            {

                if (debug) Debug.DrawRay(worldVertices[localIndex], worldNormals[localIndex] * 0.01f, Color.blue, 30);

                Transform bone = bones[controlBone];

                //Vector3 boneDir = bone.childCount > 0 ? (bone.GetChild(0).position - bone.position).normalized : (bone.parent != null ? (bone.position - bone.parent.position).normalized : bone.TransformDirection(Vector3.up));
                Vector3 boneDir = bone.childCount > 0 ? ((bone.GetChild(0).position - bone.position) * 100).normalized : bone.TransformDirection(boneRollAxis);
                if (bone.childCount == 0) Debug.DrawRay(bone.position, boneDir * 0.1f, Color.cyan, 30);

                List<int> loopIndices = new List<int>() { localIndex };

                if (FindEdgeLoop(localIndex, controlBone, boneDir, Quaternion.FromToRotation(boneDir, Vector3.up), loopIndices, 1, out EdgeLoop loop))
                {

                    if (loop.indices == null || loop.indices.Length < minEdgeLoopLength) continue;

                    bool isValid = true;

                    foreach (int i in loop.indices) if (!candidateIndices.Contains(clones[i].first))
                        {

                            isValid = false;
                            break;

                        } 

                    if (!isValid) continue;

                    foreach (var index in loop.indices) evaluatedIndices.Add(clones[index].first);

                    wastefulEdgeLoops.Add(loop);

                }

            }

        }

        // Debug

        if (debug)
        {

            Debug.Log($"Found {wastefulEdgeLoops.Count} wasteful edge loops.");

            for (int a = 0; a < wastefulEdgeLoops.Count; a++)
            {

                var loop = wastefulEdgeLoops[a];

                if (loop.indices == null) continue;

                //Color col = Color.Lerp(Color.red, Color.magenta, a / (float)wastefulEdgeLoops.Count);
                //Color col = Random.ColorHSV();
                Color col = Color.red;

                for (int b = 0; b < loop.indices.Length; b++)
                {

                    //Debug.DrawRay(worldVertices[loop.indices[b]], worldNormals[loop.indices[b]] * 0.005f, col, 30);
                    Debug.DrawLine(worldVertices[loop.indices[b == 0 ? (loop.indices.Length - 1) : (b - 1)]], worldVertices[loop.indices[b]], col, 30);

                    /*var tris = containingTris[loop.indices[b]];
                    for (int c = 0; c < tris.Count; c++)
                    {

                        int triIndex = tris[c];

                        int i0 = clones[orig_triangles[triIndex]].first;
                        int i1 = clones[orig_triangles[triIndex + 1]].first;
                        int i2 = clones[orig_triangles[triIndex + 2]].first;

                        Vector3 norm = (nugen.Maths.CalcNormal(worldVertices[i0], worldVertices[i1], worldVertices[i2]) * 100).normalized;
                        Debug.DrawRay((worldVertices[i0] + worldVertices[i1] + worldVertices[i2]) / 3f, norm * 0.03f, Color.yellow, 60);

                    }*/

                }

            }

        }

        //

        int[] vertexRemapper = new int[mesh.vertexCount];
        for (int a = 0; a < vertexRemapper.Length; a++) vertexRemapper[a] = a;

        int GetRemappedIndex(int originalIndex)
        {

            if (vertexRemapper[originalIndex] != originalIndex) return GetRemappedIndex(vertexRemapper[originalIndex]);

            return originalIndex;

        }

        List<int>[] collapsedIndices = new List<int>[mesh.vertexCount];

        void RemapIndex(int origIndex, int newIndex)
        {

            if (origIndex == newIndex || vertexRemapper[origIndex] != origIndex) return;

            if (vertexRemapper[newIndex] == origIndex)
            {

                Debug.LogError("Tried to remap vertex to one that was mapped to itself!");

                return;

            }

            newIndex = GetRemappedIndex(newIndex);

            vertexRemapper[origIndex] = newIndex;

            if (collapsedIndices[newIndex] == null) collapsedIndices[newIndex] = new List<int>();

            collapsedIndices[newIndex].Add(origIndex);

            if (collapsedIndices[origIndex] != null)
            {

                collapsedIndices[newIndex].AddRange(collapsedIndices[origIndex]);

                for (int a = 0; a < collapsedIndices[origIndex].Count; a++) vertexRemapper[collapsedIndices[origIndex][a]] = newIndex;

                collapsedIndices[origIndex] = null;

            }

        }

        if (wastefulEdgeLoops.Count > 0)
        {

            int failed = 0;
            int count = 0;
            evaluatedIndices.Clear();
            foreach (var edgeLoop in wastefulEdgeLoops)
            {

                if (edgeLoop.indices == null || edgeLoop.indices.Length == 0) continue;

                for (int a = 0; a < edgeLoop.indices.Length; a++)
                {

                    int index = clones[edgeLoop.indices[a]].first;
                    if (evaluatedIndices.Contains(index)) continue;
                    count++;

                    evaluatedIndices.Add(index);

                    var tris = containingTris[index];

                    bool TryCollapseWithTris(int rootIndex, int owningIndex, List<int> tris)
                    {

                        bool collapsed = false;

                        foreach (int tri in tris)
                        {

                            int i0 = clones[orig_triangles[tri]].first;
                            int i1 = clones[orig_triangles[tri + 1]].first;
                            int i2 = clones[orig_triangles[tri + 2]].first;

                            if (i0 != owningIndex) 
                            {
                                int remap = GetRemappedIndex(i0);
                                if (remap == owningIndex || remap == rootIndex) continue;
                            }
                            if (i1 != owningIndex)
                            {
                                int remap = GetRemappedIndex(i1);
                                if (remap == owningIndex || remap == rootIndex) continue;
                            }
                            if (i2 != owningIndex)
                            {
                                int remap = GetRemappedIndex(i2);
                                if (remap == owningIndex || remap == rootIndex) continue;
                            }

                            bool TryCollapse(int indexToCollapse, int otherIndexA, int otherIndexB)
                            {

                                bool cA = edgeLoop.Contains(otherIndexA, clones);
                                bool cB = edgeLoop.Contains(otherIndexB, clones);

                                if (!(cA ^ cB)) return false;

                                if (cA)
                                {
                                    Vector3 collapsePos = orig_vertices[otherIndexB];
                                    float dL = (collapsePos - orig_vertices[indexToCollapse]).sqrMagnitude;
                                    float dO = (collapsePos - orig_vertices[otherIndexA]).sqrMagnitude;
                                    if (dL >= dO) return false;
                                    RemapIndex(indexToCollapse, otherIndexB);
                                }
                                else
                                {
                                    Vector3 collapsePos = orig_vertices[otherIndexA];
                                    float dL = (collapsePos - orig_vertices[indexToCollapse]).sqrMagnitude;
                                    float dO = (collapsePos - orig_vertices[otherIndexB]).sqrMagnitude;
                                    if (dL >= dO) return false;
                                    RemapIndex(indexToCollapse, otherIndexA);
                                }

                                return true;

                            }

                            if (i0 == owningIndex && TryCollapse(rootIndex, i1, i2))
                            {
                                collapsed = true;
                                break;
                            }
                            else if (i1 == owningIndex && TryCollapse(rootIndex, i0, i2))
                            {
                                collapsed = true;
                                break;
                            }
                            else if (i2 == owningIndex && TryCollapse(rootIndex, i0, i1))
                            {
                                collapsed = true;
                                break;
                            }

                        }

                        return collapsed;

                    }

                    if (TryCollapseWithTris(index, index, tris)) continue; 

                    if (collapsedIndices[index] != null)
                    {

                        bool collapsed = false;

                        foreach (int collapsedIndex in collapsedIndices[index]) 
                        { 

                            if (TryCollapseWithTris(index, collapsedIndex, containingTris[collapsedIndex]))
                            {

                                collapsed = true;

                                break;

                            }

                        }

                        if (collapsed) continue;

                    }
                    Debug.DrawRay(worldVertices[index], worldNormals[index] * 0.35f, Color.red, 60);
                    failed++;

                }

            } 

            if (failed > 0) Debug.LogWarning($"Failed to remove {failed}/{count} wasteful vertices!"); else Debug.Log($"Successfully removed {count} wasteful vertices!");

        }

        List<int> new_triangles = new List<int>();
        List<Vector3> new_vertices = new List<Vector3>();
        List<Vector3> new_normals = orig_normals == null || orig_normals.Length == 0 ? null : new List<Vector3>();
        List<Vector4> new_tangents = orig_tangents == null || orig_tangents.Length == 0 ? null : new List<Vector4>();
        List<Color> new_colors = orig_colors == null || orig_colors.Length == 0 ? null : new List<Color>();

        List<Vector4> new_uv0 = orig_uv0 == null || orig_uv0.Length == 0 ? null : new List<Vector4>();
        List<Vector4> new_uv1 = orig_uv1 == null || orig_uv1.Length == 0 ? null : new List<Vector4>();
        List<Vector4> new_uv2 = orig_uv2 == null || orig_uv2.Length == 0 ? null : new List<Vector4>();
        List<Vector4> new_uv3 = orig_uv3 == null || orig_uv3.Length == 0 ? null : new List<Vector4>();

        List<BoneWeight1> new_boneWeights = new List<BoneWeight1>();
        List<byte> new_boneCounts = new List<byte>();

        BlendShape[] new_blendShapes = new BlendShape[orig_blendShapes.Count];
        for (int a = 0; a < orig_blendShapes.Count; a++) 
        {

            var oldShape = orig_blendShapes[a];

            var newShape = new BlendShape(oldShape.name, oldShape.frames, 0, true);

            new_blendShapes[a] = newShape;

        }

        int[] newIndices = new int[mesh.vertexCount];

        for (int a = 0; a < mesh.vertexCount; a++)
        {

            newIndices[a] = -1;

            int remap = GetRemappedIndex(clones[a].first);

            if (remap != clones[a].first) continue;

            newIndices[a] = new_vertices.Count;

            new_vertices.Add(orig_vertices[a]);
            if (new_normals != null) new_normals.Add(orig_normals[a]);
            if (new_tangents != null) new_tangents.Add(orig_tangents[a]);
            if (new_colors != null) new_colors.Add(orig_colors[a]);

            if (new_uv0 != null) new_uv0.Add(orig_uv0[a]);
            if (new_uv1 != null) new_uv0.Add(orig_uv1[a]);
            if (new_uv2 != null) new_uv0.Add(orig_uv2[a]);
            if (new_uv3 != null) new_uv0.Add(orig_uv3[a]);

            byte boneCount = orig_boneCounts[a];

            new_boneCounts.Add(boneCount);

            int indexOffset = boneWeightArrayIndexOffsets[a];
            for(int b = 0; b < boneCount; b++) new_boneWeights.Add(orig_boneWeights[indexOffset + b]);

            for (int b = 0; b < orig_blendShapes.Count; b++)
            {

                var oldShape = orig_blendShapes[b];

                var newShape = new_blendShapes[b];

                for(int c = 0; c < newShape.frames.Length; c++)
                {

                    var oldFrame = oldShape.frames[c];
                    var newFrame = newShape.frames[c];

                    newFrame.expandable_deltaVertices.Add(oldFrame.deltaVertices[a]);
                    newFrame.expandable_deltaNormals.Add(oldFrame.deltaNormals[a]);
                    newFrame.expandable_deltaTangents.Add(oldFrame.deltaTangents[a]);

                }

            }

        }

        for(int a = 0; a < new_blendShapes.Length; a++)
        {

            var newShape = new_blendShapes[a];

            for (int b = 0; b < newShape.frames.Length; b++) newShape.frames[b].Expand();

        }

        for(int a = 0; a < orig_triangles.Length; a += 3)
        {

            int i0 = orig_triangles[a];
            int i1 = orig_triangles[a + 1];
            int i2 = orig_triangles[a + 2];

            int remapI0 = GetRemappedIndex(clones[i0].first);
            int remapI1 = GetRemappedIndex(clones[i1].first);
            int remapI2 = GetRemappedIndex(clones[i2].first);

            bool newI0 = remapI0 != clones[i0].first;
            bool newI1 = remapI1 != clones[i1].first;
            bool newI2 = remapI2 != clones[i2].first;

            if (newI0 && newI1 && newI2) continue;

            if (newI0) i0 = remapI0;
            if (newI1) i1 = remapI1;
            if (newI2) i2 = remapI2;

            i0 = newIndices[i0];
            i1 = newIndices[i1];
            i2 = newIndices[i2];

            if (i0 < 0 || i1 < 0 || i2 < 0) continue;

            new_triangles.Add(i0);
            new_triangles.Add(i1);
            new_triangles.Add(i2);

        }

        orig_boneWeights.Dispose();
        orig_boneCounts.Dispose();

        Matrix4x4[] bindpose = mesh.bindposes;

        mesh.ClearBlendShapes();
        mesh.Clear();

        mesh.SetVertices(new_vertices);
        mesh.SetNormals(new_normals);
        mesh.SetTangents(new_tangents);
        mesh.SetColors(new_colors);

        mesh.SetUVs(0, new_uv0);
        mesh.SetUVs(1, new_uv1);
        mesh.SetUVs(2, new_uv2);
        mesh.SetUVs(3, new_uv3);

        orig_boneWeights = new NativeArray<BoneWeight1>(new_boneWeights.ToArray(), Allocator.Persistent);
        orig_boneCounts = new NativeArray<byte>(new_boneCounts.ToArray(), Allocator.Persistent);

        mesh.SetBoneWeights(orig_boneCounts, orig_boneWeights);

        orig_boneWeights.Dispose();
        orig_boneCounts.Dispose();

        mesh.subMeshCount = 1;
        mesh.SetTriangles(new_triangles, 0);

        mesh.bindposes = bindpose;

        for (int a = 0; a < new_blendShapes.Length; a++) new_blendShapes[a].AddToMesh(mesh);

        return mesh;

    }

}
