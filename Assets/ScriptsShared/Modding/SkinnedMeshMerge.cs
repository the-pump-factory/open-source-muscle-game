using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

using Swole;

[ExecuteInEditMode]
public class SkinnedMeshMerge : MonoBehaviour
{

    [Header("MESHES WITH ARMATURES THAT HAVE BONES WITH IDENTICAL NAMES IN OTHER ARMATURES MUST BE EXPORTED SEPERATELY, OTHERWISE UNITY USES THE WRONG BONES")] 
    public bool apply;

    public SkinnedMeshRenderer[] renderers;
    public bool mergeNearbyBones = true;
    public float boneMergeMaxDist = 0.001f;
    public bool createMeshFromScratch;
    public bool forceSingleBoneWeightForJunctionBones = true;
    public float forceBoneWeightThreshold = 0.5f;
    public bool verbose = false;

    public void Update()
    {
        
        if (apply)
        {

            apply = false;

            var mergedMesh = Merge(/*out Vector3[] debugVerts, */out Transform[] finalRig, out Transform newRootObject, renderers, mergeNearbyBones, boneMergeMaxDist, createMeshFromScratch, forceSingleBoneWeightForJunctionBones, forceBoneWeightThreshold, verbose);

            if (mergedMesh != null)
            {

                mergedMesh.name = gameObject.name + "_meshMerge";
                var newRenderer = Instantiate(renderers[0]);
                newRenderer.transform.position = renderers[0].transform.position;
                newRenderer.transform.rotation = renderers[0].transform.rotation;
                newRenderer.transform.SetParent(newRootObject, true);
                newRenderer.transform.localScale = renderers[0].transform.localScale;
                newRenderer.sharedMesh = null;
                newRenderer.bones = null; 
                newRenderer.sharedMesh = mergedMesh;
                newRenderer.name = "renderer";
                newRenderer.bones = finalRig;
                newRootObject.name = mergedMesh.name;
                if (newRenderer.rootBone != null) newRenderer.rootBone = finalRig.Length == 0 ? null : finalRig[0];
                /*
                Vector3[] debugFinalVerts = RealtimeMesh.GetTransformedVertices(newRenderer);
                BoneWeight[] debugBoneWeights = mergedMesh.boneWeights;

                for(int a = 0; a < debugFinalVerts.Length; a++)
                {

                    if ((debugFinalVerts[a] - debugVerts[a]).magnitude > 0.01f)
                    {

                        Debug.DrawLine(debugFinalVerts[a], debugVerts[a], Color.red, 40);
                        var bw = debugBoneWeights[a];

                        string nl = "null";
                        Debug.Log($"Incorrectly bound vertex {a} => {(bw.boneIndex0 >= 0 ? finalRig[bw.boneIndex0].name : nl)}[{bw.weight0}] : {(bw.boneIndex1 >= 0 ? finalRig[bw.boneIndex1].name : nl)}[{bw.weight1}] : {(bw.boneIndex2 >= 0 ? finalRig[bw.boneIndex2].name : nl)}[{bw.weight2}] : {(bw.boneIndex3 >= 0 ? finalRig[bw.boneIndex3].name : nl)}[{bw.weight3}]");

                        if (bw.boneIndex0 >= 0 && bw.weight0 > 0) Debug.DrawLine(debugFinalVerts[a], finalRig[bw.boneIndex0].position, Color.magenta, 40);
                        if (bw.boneIndex1 >= 0 && bw.weight1 > 0) Debug.DrawLine(debugFinalVerts[a], finalRig[bw.boneIndex1].position, Color.magenta, 40);
                        if (bw.boneIndex2 >= 0 && bw.weight2 > 0) Debug.DrawLine(debugFinalVerts[a], finalRig[bw.boneIndex2].position, Color.magenta, 40);
                        if (bw.boneIndex3 >= 0 && bw.weight3 > 0) Debug.DrawLine(debugFinalVerts[a], finalRig[bw.boneIndex3].position, Color.magenta, 40);

                    }

                }*/

            }

        }

    }

    /// <summary>
    /// DOES NOT YET SUPPORT SUBMESHES! MESHES WITH ARMATURES THAT HAVE BONES WITH IDENTICAL NAMES IN OTHER ARMATURES MUST BE EXPORTED SEPERATELY, OTHERWISE UNITY USES THE WRONG BONES!
    /// </summary>
    public static Mesh Merge(/*out Vector3[] debugVertices, */out Transform[] finalRig, out Transform newRootObject, SkinnedMeshRenderer[] renderers, bool mergeNearbyBones = true, float boneMergeMaxDist = 0.001f, bool createMeshFromScratch = false, bool forceSingleBoneWeightForJunctionBones = true, float forceBoneWeightThreshold = 0.8f, bool verbose = false)
    {

        finalRig = null;
        newRootObject = null;
        //debugVertices = null;

        if (renderers == null || renderers.Length == 0) return null;

        Mesh mesh = createMeshFromScratch ? new Mesh() : MeshUtils.DuplicateMesh(renderers[0].sharedMesh);

        List<Transform> newRig = new List<Transform>();
        List<Matrix4x4> newBindpose = new List<Matrix4x4>();
        List<int2> newBoneIdentities = new List<int2>();
        List<bool> junctionStates = new List<bool>();

        void MergeBone(Transform localBone, Transform mergeBone)
        {

            if (verbose) Debug.Log($"Merged {localBone.name} with {mergeBone.name}");

            if (localBone != mergeBone && localBone.childCount > 0)
            {

                for (int d = 0; d < localBone.childCount; d++) localBone.GetChild(d).SetParent(mergeBone, true);

                if (Application.isPlaying) GameObject.Destroy(localBone.gameObject); else GameObject.DestroyImmediate(localBone.gameObject);

            }

        }
        int IndexOfNewBone(Transform bone)
        {

            for (int a = 0; a < newRig.Count; a++) if (newRig[a] == bone) return a;

            return -1;

        }

        newRootObject = null;
        Transform[] rigContainers = new Transform[renderers.Length];
        List<Transform>[] topLevelBones = new List<Transform>[renderers.Length];
        Transform FindEquivalentBone(Transform referenceBone, Transform root)
        {

            return FindEquivalentBoneByName(referenceBone.name, root);

        }
        Transform FindEquivalentBoneByName(string name, Transform root)
        {

            return root.FindDeepChild(name);

        }
        Transform[][] bones = new Transform[renderers.Length][];
        int2[][] boneConnections = new int2[renderers.Length][];
        bool ContainsTransform(Transform[] boneArray, Transform transform)
        {
            for (int a = 0; a < boneArray.Length; a++) if (boneArray[a] == transform) return true;

            return false; 
        }
        int2 GetBoneIdentity(Transform bone)
        {

            for(int a = 0; a < bones.Length; a++)
            {

                var bones_ = bones[a];

                for(int b = 0; b < bones_.Length; b++)
                {

                    var bone_ = bones_[b];

                    if (bone_ == bone) return new int2(a, b);

                }

            }

            return new int2(-1, -1);

        }
        Matrix4x4[][] bindposes = new Matrix4x4[renderers.Length][];
        int[][] boneIndexTranslators = new int[renderers.Length][];
        for (int a = 0; a < bones.Length; a++) 
        {

            var renderer = renderers[a];

            bones[a] = renderer.bones;
            boneConnections[a] = new int2[bones[a].Length];
            for (int b = 0; b < boneConnections[a].Length; b++) boneConnections[a][b] = -1;
            bindposes[a] = renderer.sharedMesh.bindposes;


        }
        for(int a = 0; a < bones.Length; a++)
        {

            var bones_ = bones[a];

            topLevelBones[a] = new List<Transform>();

            for (int b = 0; b < bones_.Length; b++) 
            {

                Transform root = bones_[b];
                while (root.parent != null) 
                {

                    if (!ContainsTransform(bones_, root.parent)) 
                    {

                        if (root.parent.parent != null)
                        {

                            bool isTopLevel = true;

                            Transform parent = root.parent;

                            while(parent.parent != null)
                            {

                                if (ContainsTransform(bones_, parent.parent))
                                {
                                    isTopLevel = false;
                                    break;
                                }

                                parent = parent.parent;

                            }

                            if (isTopLevel) break;

                        }
                        else break;

                    }

                    root = root.parent; 
                
                }

                if (!topLevelBones[a].Contains(root)) topLevelBones[a].Add(root);

            }

            Debug.Log(renderers[a].name + $" has {topLevelBones[a].Count} top level bones");

        }

        for(int a = 0; a < topLevelBones.Length; a++)
        {

            if (topLevelBones[a] == null || topLevelBones[a].Count <= 0) continue;

            Transform firstTop = topLevelBones[a][0];

            if (newRootObject == null && firstTop != null)
            {

                newRootObject = firstTop;

                while (newRootObject.parent != null) newRootObject = newRootObject.parent;

            }

            if (firstTop.parent != null) firstTop = firstTop.parent;

            rigContainers[a] = Instantiate(firstTop);
            rigContainers[a].parent = firstTop.parent;
            rigContainers[a].localPosition = firstTop.localPosition;
            rigContainers[a].localRotation = firstTop.localRotation;
            rigContainers[a].localScale = firstTop.localScale;
            rigContainers[a].name = firstTop.name;
            Transform[] allChildren = rigContainers[a].GetComponentsInChildren<Transform>();
            for (int b = 0; b < allChildren.Length; b++) allChildren[b].name = allChildren[b].name + "_" + a;

            var bones_ = bones[a];
            for (int b = 0; b < bones_.Length; b++) bones_[b] = FindEquivalentBoneByName(bones_[b].name + "_" + a, rigContainers[a]);
            for (int b = 0; b < topLevelBones[a].Count; b++) 
            {

                if (verbose) Debug.Log($"Finding equivalent for {(topLevelBones[a][b].name + "_" + a)} under {rigContainers[a].name}");

                topLevelBones[a][b] = FindEquivalentBoneByName(topLevelBones[a][b].name + "_" + a, rigContainers[a]);

                if (verbose) { if (topLevelBones[a][b] == null) Debug.Log("Failed..."); else Debug.Log("Success"); }

            }

        }
        if (newRootObject != null)
        {
            Transform temp = new GameObject("meshMerge").transform; 
            temp.position = newRootObject.position;
            temp.rotation = newRootObject.rotation;
            temp.localScale = newRootObject.localScale;
             
            newRootObject = temp;

        }
        if (newRootObject == null) newRootObject = new GameObject("meshMerge").transform;
        for (int a = 0; a < rigContainers.Length; a++) rigContainers[a].SetParent(newRootObject, true);
        for(int a = 1; a < rigContainers.Length; a++)
        {

            if (topLevelBones[a] == null || topLevelBones[a].Count <= 0) continue;

            for (int b = 0; b < topLevelBones[a].Count; b++) topLevelBones[a][b].SetParent(rigContainers[0], true);

        }
        for(int a = 1; a < rigContainers.Length; a++)
        {
            if (rigContainers[a] != null) GameObject.Destroy(rigContainers[a].gameObject); 
        }
        if (mergeNearbyBones)
        {

            for (int a = 0; a < bones.Length; a++)
            {
                 
                var bones_ = bones[a];
                var bindpose = bindposes[a];
                boneIndexTranslators[a] = new int[bones_.Length];

                for (int b = 0; b < bones_.Length; b++)
                {

                    boneIndexTranslators[a][b] = -1;

                    var localBone = bones_[b];

                    if (localBone == null) continue;

                    var localBind = bindpose[b];

                    bool localIsLeaf = localBind == Matrix4x4.identity;

                    int mergeIndex = -1;
                    float mergeDist = float.MaxValue;

                    for(int c = 0; c < newRig.Count; c++)
                    {

                        var newBone = newRig[c];
                        var newBind = newBindpose[c];

                        float dist = (newBone.position - localBone.position).magnitude;

                        if (dist <= boneMergeMaxDist && dist < mergeDist)
                        {

                            mergeIndex = c;
                            mergeDist = dist;

                        }

                    }

                    if (mergeIndex >= 0)
                    {

                        var mergeBone = newRig[mergeIndex];
                        var mergeBind = newBindpose[mergeIndex];

                        bool mergeIsLeaf = mergeBind == Matrix4x4.identity;

                        if (mergeIsLeaf) // Merge bone is leaf
                        {

                            if (localIsLeaf) // Local bone is also leaf
                            {

                                if (mergeBone != localBone)
                                {

                                    if (mergeBone.parent != null && localBone.parent != null)
                                    {

                                        int2 leafParentIndexA = GetBoneIdentity(localBone.parent);
                                        int2 leafParentIndexB = GetBoneIdentity(mergeBone.parent);

                                        if (math.all(leafParentIndexA >= 0) && math.all(leafParentIndexB >= 0))
                                        {

                                            boneConnections[leafParentIndexA.x][leafParentIndexA.y] = leafParentIndexB;
                                            boneConnections[leafParentIndexB.x][leafParentIndexB.y] = leafParentIndexA;

                                            if (verbose) Debug.Log($"Connected {bones[leafParentIndexA.x][leafParentIndexA.y].name} <=> {bones[leafParentIndexB.x][leafParentIndexB.y].name}");

                                        }

                                    }

                                }

                            } 
                            else
                            {

                                Transform temp = mergeBone;
                                mergeBone = localBone;
                                localBone = temp;

                                newRig[mergeIndex] = mergeBone;
                                newBindpose[mergeIndex] = localBind;
                                newBoneIdentities[mergeIndex] = new int2(a, b);

                                if (localBone.parent != null)
                                {

                                    int2 leafParentIndex = GetBoneIdentity(localBone.parent);

                                    if (math.all(leafParentIndex >= 0))
                                    {

                                        boneConnections[leafParentIndex.x][leafParentIndex.y] = new int2(a, b);

                                        if (verbose) Debug.Log($"Connected {bones[leafParentIndex.x][leafParentIndex.y].name} => {mergeBone.name}");

                                    }

                                }

                            }

                        } 
                        else if (localIsLeaf)
                        {

                            if (localBone.parent != null)
                            {

                                int2 leafParentIndex = GetBoneIdentity(localBone.parent);

                                if (math.all(leafParentIndex >= 0))
                                {

                                    boneConnections[leafParentIndex.x][leafParentIndex.y] = new int2(a, b);

                                    if (verbose) Debug.Log($"Connected {bones[leafParentIndex.x][leafParentIndex.y].name} => {mergeBone.name}");

                                }

                            } 

                        } 
                        
                        boneIndexTranslators[a][b] = mergeIndex;

                        MergeBone(localBone, mergeBone);

                        junctionStates[mergeIndex] = true;

                    } 
                    else
                    {

                        boneIndexTranslators[a][b] = newRig.Count;
                        newRig.Add(localBone);
                        newBindpose.Add(localBind);
                        newBoneIdentities.Add(new int2(a, b));
                        junctionStates.Add(false);

                    }

                }

            }
            for(int a = 0; a < bones.Length; a++)
            {

                var bones_ = bones[a];

                var connections = boneConnections[a];
                if (connections == null) continue;

                for(int b = 0; b < bones_.Length; b++)
                {

                    var connection = connections[b];
                    if (math.any(connection < 0)) continue;

                    int newIndex = boneIndexTranslators[a][b];
                    if (newIndex < 0) continue;

                    junctionStates[newIndex] = true;

                }

            }

        } 
        else
        {

            for(int a = 0; a < bones.Length; a++)
            {

                var bones_ = bones[a];
                var bindpose = bindposes[a];
                boneIndexTranslators[a] = new int[bones_.Length];
                for(int b = 0; b < bones_.Length; b++)
                {

                    boneIndexTranslators[a][b] = newRig.Count;
                    newRig.Add(bones_[b]);
                    newBindpose.Add(bindpose[b]);
                    newBoneIdentities.Add(new int2(a, b));
                    junctionStates.Add(false);

                }

            }

        }

        //List<Vector3> debug_vertices = new List<Vector3>();

        List<int> new_triangles = new List<int>();
        List<Vector3> new_vertices = new List<Vector3>();
        List<Vector3> new_normals = null;
        List<Vector4> new_tangents = null;
        List<Color> new_colors = null;

        List<Vector4> new_uv0 = null;
        List<Vector4> new_uv1 = null;
        List<Vector4> new_uv2 = null;
        List<Vector4> new_uv3 = null;

        List<Vector3> GetNormalsList()
        {

            if (new_normals == null)
            {
                new_normals = new List<Vector3>();
                for (int a = 0; a < new_vertices.Count; a++) new_normals.Add(Vector3.zero);
            }
            return new_normals;

        }
        List<Vector4> GetTangentsList()
        {

            if (new_tangents == null)
            {
                new_tangents = new List<Vector4>();
                for (int a = 0; a < new_vertices.Count; a++) new_tangents.Add(Vector4.zero);
            }
            return new_tangents;

        }
        List<Color> GetColorsList()
        {

            if (new_colors == null)
            {
                new_colors = new List<Color>();
                for (int a = 0; a < new_vertices.Count; a++) new_colors.Add(Color.white);
            }
            return new_colors;

        }
        List<Vector4> GetUV0List()
        {

            if (new_uv0 == null)
            {
                new_uv0 = new List<Vector4>();
                for (int a = 0; a < new_vertices.Count; a++) new_uv0.Add(Vector4.zero);
            }
            return new_uv0;

        }
        List<Vector4> GetUV1List()
        {

            if (new_uv1 == null)
            {
                new_uv1 = new List<Vector4>();
                for (int a = 0; a < new_vertices.Count; a++) new_uv1.Add(Vector4.zero);
            }
            return new_uv1;

        }
        List<Vector4> GetUV2List()
        {

            if (new_uv2 == null)
            {
                new_uv2 = new List<Vector4>();
                for (int a = 0; a < new_vertices.Count; a++) new_uv2.Add(Vector4.zero);
            }
            return new_uv2;

        }
        List<Vector4> GetUV3List()
        {

            if (new_uv3 == null)
            {
                new_uv3 = new List<Vector4>();
                for (int a = 0; a < new_vertices.Count; a++) new_uv3.Add(Vector4.zero);
            }
            return new_uv3;

        }

        List<BoneWeight1> new_boneWeights = new List<BoneWeight1>();
        List<byte> new_boneCounts = new List<byte>();

        List<BlendShape> new_blendShapes = new List<BlendShape>();
        BlendShape GetOrCreateShape(BlendShape referenceShape)
        {

            BlendShape shape = null;

            for (int a = 0; a < new_blendShapes.Count; a++) if (new_blendShapes[a].name == referenceShape.name) return new_blendShapes[a];

            shape = new BlendShape(referenceShape.name, referenceShape.frames, new_vertices.Count, true);

            new_blendShapes.Add(shape);

            return shape;

        }

        for (int a = 0; a < renderers.Length; a++)
        {

            //Vector3[] orig_debugVertices = RealtimeMesh.GetTransformedVertices(renderers[a]); // Remove

            int vertexIndexOffset = new_vertices.Count;

            var orig_mesh = renderers[a].sharedMesh;

            int[] orig_triangles = orig_mesh.triangles;
            Vector3[] orig_worldVertices = RealtimeMesh.GetTransformedVertices(renderers[a]);
            Vector3[] orig_vertices = orig_mesh.vertices;
            Vector3[] orig_normals = orig_mesh.normals;
            Vector4[] orig_tangents = orig_mesh.tangents;
            Color[] orig_colors = orig_mesh.colors;

            Vector4[] orig_uv0 = orig_mesh.GetUVsByChannelV4(0);
            Vector4[] orig_uv1 = orig_mesh.GetUVsByChannelV4(1);
            Vector4[] orig_uv2 = orig_mesh.GetUVsByChannelV4(2);
            Vector4[] orig_uv3 = orig_mesh.GetUVsByChannelV4(3);

            NativeArray<BoneWeight1> orig_boneWeights = new NativeArray<BoneWeight1>(orig_mesh.GetAllBoneWeights(), Allocator.Persistent);
            NativeArray<byte> orig_boneCounts = new NativeArray<byte>(orig_mesh.GetBonesPerVertex(), Allocator.Persistent);

            List<BlendShape> orig_blendShapes = orig_mesh.GetBlendShapes();


            for (int c = 0; c < orig_triangles.Length; c++) new_triangles.Add(orig_triangles[c] + vertexIndexOffset);

            int boneWeightIndexOffset = 0;
            for (int d = 0; d < orig_mesh.vertexCount; d++)
            {

                Vector3 worldPos = orig_worldVertices[d];

                if (orig_normals != null && orig_normals.Length > 0) GetNormalsList().Add(orig_normals[d]); else if (new_normals != null) new_normals.Add(Vector3.zero);
                if (orig_tangents != null && orig_tangents.Length > 0) GetTangentsList().Add(orig_tangents[d]); else if (new_tangents != null) new_tangents.Add(Vector4.zero);
                if (orig_colors != null && orig_colors.Length > 0) GetColorsList().Add(orig_colors[d]); else if (new_colors != null) new_colors.Add(Color.white);

                if (orig_uv0 != null && orig_uv0.Length > 0) GetUV0List().Add(orig_uv0[d]); else if (new_uv0 != null) new_uv0.Add(Vector4.zero);
                if (orig_uv1 != null && orig_uv1.Length > 0) GetUV1List().Add(orig_uv1[d]); else if (new_uv1 != null) new_uv1.Add(Vector4.zero);
                if (orig_uv2 != null && orig_uv2.Length > 0) GetUV2List().Add(orig_uv2[d]); else if (new_uv2 != null) new_uv2.Add(Vector4.zero);
                if (orig_uv3 != null && orig_uv3.Length > 0) GetUV3List().Add(orig_uv3[d]); else if (new_uv3 != null) new_uv3.Add(Vector4.zero);

                foreach (var new_blendShape in new_blendShapes)
                {

                    for (int e = 0; e < new_blendShape.frames.Length; e++)
                    {

                        var new_frame = new_blendShape.frames[e];
                        new_frame.expandable_deltaVertices.Add(Vector3.zero);
                        new_frame.expandable_deltaNormals.Add(Vector3.zero);
                        new_frame.expandable_deltaTangents.Add(Vector3.zero);

                    }

                }

                new_vertices.Add(orig_vertices[d]);
                //debug_vertices.Add(orig_debugVertices[d]);

                foreach (var orig_blendShape in orig_blendShapes)
                {

                    var new_blendShape = GetOrCreateShape(orig_blendShape);

                    for (int e = 0; e < orig_blendShape.frames.Length; e++)
                    {

                        var orig_frame = orig_blendShape.frames[e];
                        var new_frame = new_blendShape.frames[e];
                        new_frame.expandable_deltaVertices[new_vertices.Count - 1] = orig_frame.deltaVertices[d];
                        new_frame.expandable_deltaNormals[new_vertices.Count - 1] = orig_frame.deltaNormals[d];
                        new_frame.expandable_deltaTangents[new_vertices.Count - 1] = orig_frame.deltaTangents[d];

                    }

                }

                int orig_boneCount = orig_boneCounts[d];
                int new_boneCount = 0;

                void AddBoneWeight(BoneWeight1 new_bw)
                {

                    if (new_bw.weight <= 0) return;

                    for (int f = 0; f < new_boneCount; f++)
                    {

                        var fresh_bw = new_boneWeights[new_boneWeights.Count - 1 - f];

                        if (fresh_bw.boneIndex == new_bw.boneIndex)
                        {

                            fresh_bw.weight = fresh_bw.weight + new_bw.weight;

                            new_boneWeights[new_boneWeights.Count - 1 - f] = fresh_bw;

                            new_bw.boneIndex = -1;

                            break;

                        }

                    }

                    if (new_bw.boneIndex < 0) return;

                    new_boneWeights.Add(new_bw);
                    new_boneCount++;

                }

                for (int e = 0; e < orig_boneCount; e++)
                {

                    var orig_bw = orig_boneWeights[e + boneWeightIndexOffset];

                    if (orig_bw.boneIndex < 0 || orig_bw.weight <= 0) continue;

                    float mul = 1;

                    var connection = boneConnections[a][orig_bw.boneIndex];
                    if (math.all(connection >= 0))
                    {

                        var con_bw = orig_bw;
                        var con_index = boneIndexTranslators[connection.x][connection.y];

                        if (con_index >= 0)
                        {

                            con_bw.boneIndex = con_index;

                            float distA = (bones[a][orig_bw.boneIndex].position - worldPos).magnitude;
                            float distB = (bones[connection.x][connection.y].position - worldPos).magnitude;

                            mul = distA <= 0 ? 0 : distB <= 0 ? 1 : (distB / (distA + distB));

                            con_bw.weight = con_bw.weight * (1 - mul);

                            AddBoneWeight(con_bw);

                        }

                    }

                    var new_bw = orig_bw;
                    new_bw.boneIndex = boneIndexTranslators[a][orig_bw.boneIndex];

                    if (new_bw.boneIndex < 0) continue;

                    new_bw.weight = new_bw.weight * mul;

                    AddBoneWeight(new_bw);

                }


                if (new_boneCount == 0)
                {

                    new_boneCount = 1;
                    new_boneWeights.Add(new BoneWeight1() { weight = 1 });

                }
                else
                {

                    float total = 0;
                    for (int e = 0; e < new_boneCount; e++) total += new_boneWeights[new_boneWeights.Count - 1 - e].weight;

                    if (total > 0)
                    {
                        BoneWeight1[] tempArray = new BoneWeight1[new_boneCount];
                        for (int e = 0; e < new_boneCount; e++)
                        {

                            var bw = new_boneWeights[new_boneWeights.Count - 1 - e];

                            bw.weight = bw.weight / total;

                            //new_boneWeights[new_boneWeights.Count - 1 - e] = bw;
                            tempArray[e] = bw;

                        }
                        tempArray = tempArray.OrderBy(x => x.weight).ToArray();
                        for (int e = 0; e < new_boneCount; e++)
                        {

                            new_boneWeights[new_boneWeights.Count - 1 - e] = tempArray[e];

                        }

                    }

                }

                if (forceSingleBoneWeightForJunctionBones)
                {

                    BoneWeight1 juncBoneWeight = new BoneWeight1() { boneIndex = -1 };

                    for (int e = 0; e < new_boneCount; e++)
                    {

                        var new_bw = new_boneWeights[new_boneWeights.Count - 1 - e];

                        if (new_bw.boneIndex < 0 || new_bw.weight <= 0) continue;

                        if (!junctionStates[new_bw.boneIndex]) continue;

                        if (new_bw.weight >= forceBoneWeightThreshold && new_bw.weight > juncBoneWeight.weight)
                        {

                            juncBoneWeight = new_bw;

                        }

                    }

                    if (juncBoneWeight.boneIndex >= 0)
                    {

                        new_boneWeights.RemoveRange(new_boneWeights.Count - new_boneCount, new_boneCount);
                        new_boneCount = 1;
                        juncBoneWeight.weight = 1;
                        new_boneWeights.Add(juncBoneWeight);

                    }

                }

                boneWeightIndexOffset += orig_boneCount;
                new_boneCounts.Add((byte)new_boneCount);

            }

            orig_boneWeights.Dispose();
            orig_boneCounts.Dispose();

        } 

        mesh.Clear();
        mesh.ClearBlendShapes();
        mesh.bindposes = null;

        mesh.SetVertices(new_vertices);
        mesh.SetNormals(new_normals);
        mesh.SetTangents(new_tangents);
        mesh.SetColors(new_colors);

        mesh.SetUVs(0, new_uv0);
        mesh.SetUVs(1, new_uv1);
        mesh.SetUVs(2, new_uv2);
        mesh.SetUVs(3, new_uv3);

        NativeArray<BoneWeight1> boneWeights = new NativeArray<BoneWeight1>(new_boneWeights.ToArray(), Allocator.Persistent);
        NativeArray<byte> boneCounts = new NativeArray<byte>(new_boneCounts.ToArray(), Allocator.Persistent);

        mesh.SetBoneWeights(boneCounts, boneWeights);

        boneWeights.Dispose();
        boneCounts.Dispose();

        mesh.subMeshCount = 1; 
        mesh.SetTriangles(new_triangles, 0);

        foreach (var new_blendShape in new_blendShapes)
        {

            new_blendShape.Expand();
            new_blendShape.AddToMesh(mesh);

        }

        finalRig = newRig.ToArray();
        mesh.bindposes = newBindpose.ToArray();

        //debugVertices = debug_vertices.ToArray();

        //foreach (var bone in finalRig) Debug.DrawRay(bone.position, bone.TransformDirection(Vector3.up) * 0.03f, Color.green, 40);

        return mesh;

    }

}
