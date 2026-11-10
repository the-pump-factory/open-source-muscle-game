using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Unity.Collections;

using Swole.API.Unity.Animation;
using Swole.DataStructures;
using Unity.Mathematics;
using Swole.API.Unity;

using static Swole.Morphing.MorphUtils;

namespace Swole.Morphing
{

    [ExecuteAlways]
    public class CustomizableCharacterMeshSetup : MonoBehaviour
    {

        [Header("Mesh References")]
        public GameObject meshReferencesObject;
        public bool updateMeshReferences;

        public void UpdateMeshReferences() => UpdateMeshReferences(meshReferencesObject);
        public void UpdateMeshReferences(GameObject meshReferencesObject)
        {
            if (targetMeshes != null)
            {
                SkinnedMeshRenderer[] smrs = meshReferencesObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                MeshFilter[] filters = meshReferencesObject.GetComponentsInChildren<MeshFilter>(true);

                Mesh GetMeshReference(string meshName)
                {
                    foreach (var smr in smrs)
                    {
                        if (smr.name == meshName) return smr.sharedMesh;
                    }
                    foreach (var filter in filters)
                    {
                        if (filter.name == meshName) return filter.sharedMesh; 
                    }

                    return null;
                }

                for (int a = 0; a < targetMeshes.Length; a++)
                {
                    var target = targetMeshes[a];

                    if (target.mainMesh != null)
                    {
                        var newMesh = GetMeshReference(target.mainMesh.name);
                        if (newMesh != null) target.mainMesh = newMesh;
                    }

                    if (target.lods != null)
                    {
                        for (int b = 0; b < target.lods.Length; b++)
                        {
                            var lod = target.lods[b];
                            if (lod.mesh != null)
                            {
                                var newMesh = GetMeshReference(lod.mesh.name);
                                if (newMesh != null) lod.mesh = newMesh;
                            }
                            target.lods[b] = lod;
                        }
                    }

                    if (target.meshesToMerge != null)
                    {
                        for (int b = 0; b < target.meshesToMerge.Length; b++)
                        {
                            var merge = target.meshesToMerge[b];
                            if (merge.mesh != null)
                            {
                                var newMesh = GetMeshReference(merge.mesh.name);
                                if (newMesh != null) merge.mesh = newMesh;
                            }
                            target.meshesToMerge[b] = merge; 
                        }
                    }

                    targetMeshes[a] = target;
                }
            }
        }

        [Serializable]
        public struct MeshGroup
        {
            public string name;
            public Mesh mainMesh;
            public float screenRelativeHeightBias;
            public MeshLOD[] lods;
            public MeshMergeSlot[] meshesToMerge;
            public int submesh;
            public Material material;
            public CustomAvatar avatar;

            public Vector3 boundsCenter;
            public Vector3 boundsExtents;

            public bool isBreastMesh;

            public string rigRootName;
            public string rigBufferId;
            public string shapeBufferId;
            public string morphBufferId;

            public string seamShape;
            public string baseMeshGroup;
            public SeamMergeMethod seamMergeMethod;
            public bool mergeSeamTangents;
            public bool mergeSeamUVs;

            public BlendShapeTarget[] localStandaloneShapes;

            public string shapesInstanceMeshGroup;
            public string rigInstanceMeshGroup;
            public string characterInstanceMeshGroup;

            public bool addOrLookForDynamicAnimationPropertyController;

            public bool applyInitialOffset;
            public Vector3 initialOffset;
            public Vector3 initialRotationOffset;

            public float solidifyThickness;
        }
        [Serializable]
        public struct SecondaryMeshGroup
        {
            public string name;
            public string[] mainGroupDependencies;

            public bool treatAsMeshIslands;
            public bool transferBoneWeights;

            public Mesh mainMesh;
            public MeshLOD[] lods;
            public MeshMergeSlot[] meshesToMerge;
            
            public Material[] materials;

            [Tooltip("Used to locate and provide the correct bones to a skinned mesh renderer. If not provided, will use the first available avatar instead.")]
            public SkinnedMeshRenderer skinningReference;

            public string setupMaskShape;
            public bool removeSetupMaskShape;

            public Vector3 boundsCenter;
            public Vector3 boundsExtents;

            public Vector3 alignmentOffset;
            public Vector3 alignmentRotationOffset;

            public Vector3 gameObjectOffset;
            public Vector3 gameObjectRotationOffset;

            public float emptyShapeVertexThreshold;
            public float emptyShapeNormalThreshold;
            public float emptyShapeTangentThreshold;

            public float solidifyThickness;
        }

        [Serializable]
        public struct FatGroupModifier
        {
            public string name;
            public Vector2 muscleModifiers;
        }

        [Serializable]
        public enum VertexGroupType
        {
            StandaloneVertexGroup, MuscleVertexGroup, FatVertexGroup, VariationVertexGroup
        }
        [Serializable]
        public struct CompositeVertexGroup
        {

            public string name;

            public VertexGroupType type;

            public string[] addGroups;
            public string[] subtractGroups;

            public float weight;

            public bool normalize;

        }

        [Header("Asset Creation")]
        public string assetSavePath = "Baked/Meshes";
        public string dataSavePath = "Baked/Customization";
        public string prefabSavePath = "Prefabs";

        public string prefabName;
        [Tooltip("The game object that will be instantiated and have the mesh objects parented to it.")]
        public GameObject prefabRootReference;
        public bool deleteInactiveChildrenInPrefab = true;

        public MeshGroup[] targetMeshes;
        public SecondaryMeshGroup[] secondaryMeshes;

        public int mainMeshIndex;
        public int penisMeshIndex;
        public int vulvaMeshIndex;

        public int IndexOfTargetMesh(string targetMeshName)
        {
            if (targetMeshes == null || string.IsNullOrWhiteSpace(targetMeshName)) return -1;

            for (int a = 0; a < targetMeshes.Length; a++) if (targetMeshes[a].name == targetMeshName) return a;
            return -1;
        }

        public int IndexOfSecondaryMesh(string secondaryMeshName)
        {
            if (secondaryMeshes == null || string.IsNullOrWhiteSpace(secondaryMeshName)) return -1;

            for (int a = 0; a < secondaryMeshes.Length; a++) if (secondaryMeshes[a].name == secondaryMeshName) return a;
            return -1;
        }

        public bool storeNearestVertexInUV = true;
        public bool useUVsToFindClosestVertex = true;
        [Tooltip("The uv channel to use for determining the nearest vertex.")]
        public UVChannelURP nearestVertexUVChannel = UVChannelURP.UV0;
        [Tooltip("The uv channel to store the nearest vertex index in.")]
        public UVChannelURP nearestVertexIndexUVChannel = UVChannelURP.UV3;
        [Tooltip("The uv element to store the nearest vertex index in.")]
        public RGBAChannel nearestVertexIndexElement = RGBAChannel.R;
        protected Vector4 StoreIndexInUV(Vector4 uv, int index)
        {
            switch(nearestVertexIndexElement)
            {
                case RGBAChannel.R:
                    uv.x = index;
                    break;
                case RGBAChannel.G:
                    uv.y = index;
                    break;
                case RGBAChannel.B:
                    uv.z = index;
                    break;
                case RGBAChannel.A:
                    uv.w = index;
                    break;
            }

            return uv;
        }

        [Header("Vertex Groups")]
        public bool normalizeVertexGroupsFirstCreation = true;
        public bool normalizeVertexGroupsWithSetWeight = false;
        public float vertexGroupsNormalizationSetWeight = 1;
        public BlendShapeTarget[] vertexGroups;
        public string midlineVertexGroup;
        public string bustVertexGroup;
        public string bustNerfVertexGroup;
        public string nippleMaskVertexGroup;
        public string genitalMaskVertexGroup;
        public bool normalizeMuscleGroupsAsPool = true;
        public bool averageMuscleGroupsAsPool = true;
        public BlendShapeTarget[] muscleGroups;
        public bool normalizeFatGroupsAsPool = true;
        public bool averageFatGroupsAsPool = true;
        public BlendShapeTarget[] fatGroups;
        public FatGroupModifier[] fatGroupModifiers;
        public bool normalizeVariationGroupsAsPool = true;
        public bool averageVariationGroupsAsPool = true;
        public BlendShapeTarget[] variationGroups;
        public CompositeVertexGroup[] compositeVertexGroups;
        [Tooltip("Names of vertex groups that are used in other parts of the system, which can double as variation groups.")]
        public string[] additionalVariationGroups; 

        [Header("Morph Shapes")]
        public BlendShapeTarget[] standaloneShapes;
        public string bustSizeShape;
        public BlendShapeTarget[] muscleShapes;
        public float defaultShapeMuscleWeight;
        public BlendShapeTarget[] flexShapes;
        public BlendShapeTarget[] fatShapes;
        public BlendShapeTarget fatMuscleBlendShape;
        public Vector2 fatMuscleBlendWeightRange;
        public BlendShapeTarget[] variationShapes;

        [Header("Other")]
        public List<string> nonSeamMergableBlendShapes;

        [Tooltip("Defaults to the prefab root")]
        public Transform ikBonesParent;
        [Tooltip("IK Bones that are to be reparented to the prefab root")] 
        public List<Transform> ikBonesToFloat;

        [Serializable]
        public struct BoneReplacement
        {
            public Transform boneToReplace;
            public Transform replacementBone;
            public bool destroyReplacedBone;
        }

        public BoneReplacement[] boneReplacements;

        public SkinnedMeshRenderer[] skinnedRendererRigsToEdit;

        [Header("Output")]
        public List<CustomizableCharacterMeshData> outputDatas;
        public List<Mesh> outputMeshes = new List<Mesh>();

        public GameObject outputPrefab;

        public bool execute;

        private void Update()
        {
            if (updateMeshReferences)
            {
                updateMeshReferences = false;
                UpdateMeshReferences();
            }

            if (execute)
            {
                execute = false;
                Execute();
            }
        }

        public void Execute()
        {
            if (outputDatas == null) outputDatas = new List<CustomizableCharacterMeshData>();
            outputDatas.Clear();
            if (outputMeshes == null) outputMeshes = new List<Mesh>();
            outputMeshes.Clear();

#if UNITY_EDITOR
            if (!dataSavePath.ToLower().StartsWith("assets/")) dataSavePath = "Assets/" + dataSavePath;
            if (!assetSavePath.ToLower().StartsWith("assets/")) assetSavePath = "Assets/" + assetSavePath;
            if (!prefabSavePath.ToLower().StartsWith("assets/")) prefabSavePath = "Assets/" + prefabSavePath;
#endif

            outputPrefab = Instantiate(prefabRootReference);
            outputPrefab.name = string.IsNullOrWhiteSpace(prefabName) ? prefabRootReference.name : prefabName;
            if (deleteInactiveChildrenInPrefab)
            {
                var children = outputPrefab.GetComponentsInChildren<Transform>(true);
                foreach (var child in children)
                {
                    if (child == null || child.gameObject == outputPrefab) continue; 

                    if (!child.gameObject.activeSelf) DestroyImmediate(child.gameObject);
                }
            }

            var animator = outputPrefab.GetComponent<CustomAnimator>();

            var genitalController = outputPrefab.GetComponent<GenitalController>();

            List<BlendShapeTarget> tempBlendShapeTargets = new List<BlendShapeTarget>();
            List<VertexGroup> tempVertexGroups = new List<VertexGroup>();
            List<VertexGroup> finalVertexGroups = new List<VertexGroup>();
            List<MorphShape> tempMorphShapes = new List<MorphShape>();
            List<MorphShape> finalMorphShapes = new List<MorphShape>();
            Vector2Int CreateVertexGroups(string queryId, Mesh mesh, IEnumerable<BlendShapeTarget> shapeTargets, bool normalizeAsPool, bool averageAsPool)
            {
                Vector2Int indices = new Vector2Int();

                tempBlendShapeTargets.Clear();
                tempBlendShapeTargets.AddRange(shapeTargets);
                tempBlendShapeTargets.Sort((BlendShapeTarget shapeA, BlendShapeTarget shapeB) => (int)Mathf.Sign(shapeA.weight - shapeB.weight));
                indices.x = finalVertexGroups.Count;
                for (int b = 0; b < tempBlendShapeTargets.Count; b++)
                {
                    var shapeTarget = tempBlendShapeTargets[b];

                    BlendShape shape;
                    if (!shapeTarget.TryGetBlendShape(queryId, mesh, out shape, finalMorphShapes))
                    {

                        shape = new BlendShape(shapeTarget.targetName); // force create an empty shape if it wasnt in the mesh
                    }
                    if (shape.frames == null || shape.frames.Length <= 0)
                    {
                        var frame = new BlendShape.Frame(shape, 0, 0, new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount]);
                        shape.frames = new BlendShape.Frame[] { frame };
                    }

                    var vg = VertexGroup.ConvertToVertexGroup(shape, normalizeVertexGroupsFirstCreation, string.Empty, 0.00001f, normalizeVertexGroupsWithSetWeight ? vertexGroupsNormalizationSetWeight : 0, true);
                    vg.name = string.IsNullOrWhiteSpace(shapeTarget.newName) ? shapeTarget.targetName : shapeTarget.newName;
                    vg.flag = shapeTarget.animatable;
                    finalVertexGroups.Add(vg);
                }
                indices.y = finalVertexGroups.Count - 1;

                if (averageAsPool)
                {
                    tempVertexGroups.Clear();
                    for (int a = indices.x; a <= indices.y; a++) tempVertexGroups.Add(finalVertexGroups[a]);

                    MeshDataTools.AverageVertexGroupWeightsPerVertex(tempVertexGroups, normalizeAsPool);
                    tempVertexGroups.Clear();
                }
                else if (normalizeAsPool)
                {
                    tempVertexGroups.Clear();
                    for (int a = indices.x; a <= indices.y; a++) tempVertexGroups.Add(finalVertexGroups[a]);

                    MeshDataTools.NormalizeVertexGroupsAsPool(tempVertexGroups);
                    tempVertexGroups.Clear();
                }

                return indices;
            }
            Vector2Int CreateMorphShapes(string queryId, Mesh mesh, IEnumerable<BlendShapeTarget> shapeTargets, List<float> frameWeights)
            {
                Vector2Int indices = new Vector2Int();

                tempBlendShapeTargets.Clear();
                tempBlendShapeTargets.AddRange(shapeTargets);
                tempBlendShapeTargets.Sort((BlendShapeTarget shapeA, BlendShapeTarget shapeB) => (int)Mathf.Sign(shapeA.weight - shapeB.weight));
                indices.x = finalMorphShapes.Count;
                for (int b = 0; b < tempBlendShapeTargets.Count; b++)
                {
                    var shapeTarget = tempBlendShapeTargets[b];
                    if (frameWeights != null) frameWeights.Add(shapeTarget.weight);

                    BlendShape shape;
                    if (!shapeTarget.TryGetBlendShape(queryId, mesh, out shape, finalMorphShapes))
                    {
                        shape = new BlendShape(shapeTarget.targetName); // force create an empty shape if it wasnt in the mesh
                    }
                    if (shape.frames == null || shape.frames.Length <= 0)
                    {
                        var frame = new BlendShape.Frame(shape, 0, 0, new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount]);
                        shape.frames = new BlendShape.Frame[] { frame };
                    }

                    var morph = MorphShape.CreateFromBlendShape(shape);
                    morph.name = string.IsNullOrWhiteSpace(shapeTarget.newName) ? shapeTarget.targetName : shapeTarget.newName;
                    morph.animatable = shapeTarget.animatable;
                    finalMorphShapes.Add(morph);
                }
                indices.y = finalMorphShapes.Count - 1;

                return indices;
            }

            List<BlendShapeTarget> tempBlendShapeTargets2 = new List<BlendShapeTarget>();
            List<float> tempWeights = new List<float>();
            List<int> variationGroupIndices = new List<int>();
            List<MeshLOD> meshLODs = new List<MeshLOD>();
            List<BlendShape> seamShapes = new List<BlendShape>();
            List<CustomizableCharacterMesh> characterMeshes = new List<CustomizableCharacterMesh>();
            for (int a = 0; a < targetMeshes.Length; a++)
            {

                finalVertexGroups.Clear();
                finalMorphShapes.Clear();

                var target = targetMeshes[a];

                int baseMeshIndex = IndexOfTargetMesh(target.baseMeshGroup);

                CustomizableCharacterMeshData outputData = null;
#if UNITY_EDITOR
                string savePath = Path.Combine(dataSavePath, target.name);
                outputData = AssetDatabase.LoadAssetAtPath<CustomizableCharacterMeshData>(savePath);
#endif

                if (outputData == null) outputData = ScriptableObject.CreateInstance<CustomizableCharacterMeshData>();
                outputData.name = target.name;

                Mesh mainMesh = target.mainMesh;
                Mesh[] lodMeshes = target.lods == null ? null : new Mesh[target.lods.Length];
                if (lodMeshes != null) for (int b = 0; b < target.lods.Length; b++) lodMeshes[b] = target.lods[b].mesh;

                // Combine specified meshes
                if (target.meshesToMerge != null)
                {

                    int[] baseMeshTriangles = null;

                    Vector3[] baseMeshVertices = null;
                    Vector3[] baseMeshNormals = null;

                    NativeArray<BoneWeight1> baseMeshBoneWeights = default;
                    NativeArray<byte> baseMeshBonesPerVertex = default;
                    int[] baseMeshBoneWeightStartIndices = null;

                    List<BlendShape> baseMeshBlendShapes = new List<BlendShape>();

                    void UpdateBaseMeshData(Mesh baseMesh)
                    {
                        baseMeshTriangles = mainMesh.triangles;

                        baseMeshVertices = mainMesh.vertices;
                        baseMeshNormals = mainMesh.normals;

                        if (baseMeshBoneWeights.IsCreated) baseMeshBoneWeights.Dispose();
                        if (baseMeshBonesPerVertex.IsCreated) baseMeshBonesPerVertex.Dispose();  

                        baseMeshBoneWeights = new NativeArray<BoneWeight1>(mainMesh.GetAllBoneWeights(), Allocator.Persistent);
                        baseMeshBonesPerVertex = new NativeArray<byte>(mainMesh.GetBonesPerVertex(), Allocator.Persistent);
                        baseMeshBoneWeightStartIndices = new int[mainMesh.vertexCount];

                        baseMeshBlendShapes.Clear();
                        baseMeshBlendShapes = mainMesh.GetBlendShapes(baseMeshBlendShapes);

                        if (baseMeshBonesPerVertex.IsCreated)
                        {
                            int i = 0;
                            for (int c = 0; c < baseMeshBonesPerVertex.Length; c++)
                            {
                                int count = baseMeshBonesPerVertex[c];
                                baseMeshBoneWeightStartIndices[c] = i;
                                i += count;
                            }
                        }
                    }

                    UpdateBaseMeshData(mainMesh);

                    foreach (var merger in target.meshesToMerge)
                    {
                        var meshToMerge = merger.mesh;
                        if (merger.recalculateNormals || merger.recalculateTangents)
                        {
                            var vertices = meshToMerge.vertices;

                            Mesh tempMesh = GameObject.Instantiate(meshToMerge);

                            if (merger.recalculateNormals) tempMesh.RecalculateNormals();
                            if (merger.recalculateTangents) tempMesh.RecalculateTangents();

                            var normals = tempMesh.normals;
                            var tangents = tempMesh.tangents;

                            MeshDataTools.WeldedVertex[] mergedVertices = null;

                            var shapes = tempMesh.GetBlendShapes();
                            foreach (var shape in shapes)
                            {
                                for (int b = 0; b < shape.frames.Length; b++)
                                {
                                    var frame = shape.frames[b];

                                    var tempVertices = meshToMerge.vertices;
                                    for (int c = 0; c < tempVertices.Length; c++)
                                    {
                                        tempVertices[c] = vertices[c] + frame.deltaVertices[c];
                                    }

                                    tempMesh.vertices = tempVertices;

                                    if (merger.recalculateNormals)
                                    {
                                        tempMesh.RecalculateNormals();
                                        var tempNormals = tempMesh.normals;

                                        if (mergedVertices == null) mergedVertices = MeshDataTools.WeldVertices(vertices);
                                        for (int d = 0; d < mergedVertices.Length; d++)
                                        {
                                            var mv = mergedVertices[d];
                                            if (d != mv.firstIndex) continue;

                                            var normal = Vector3.zero;
                                            for (int e = 0; e < mv.indices.Count; e++) normal = normal + tempNormals[mv.indices[e]];

                                            normal = normal.normalized;
                                            for (int e = 0; e < mv.indices.Count; e++) tempNormals[mv.indices[e]] = normal;
                                        }

                                        for (int c = 0; c < tempVertices.Length; c++) frame.deltaNormals[c] = tempNormals[c] - normals[c];
                                    }

                                    if (merger.recalculateTangents)
                                    {
                                        tempMesh.RecalculateTangents();
                                        var tempTangents = tempMesh.tangents;

                                        for (int c = 0; c < tempVertices.Length; c++) frame.deltaTangents[c] = tempTangents[c] - tangents[c]; 
                                    }
                                }
                            }

                            meshToMerge = MeshUtils.DuplicateMesh(meshToMerge);
                            meshToMerge.name = merger.mesh.name;

                            if (merger.recalculateNormals) meshToMerge.normals = normals;
                            if (merger.recalculateTangents) meshToMerge.tangents = tangents;

                            meshToMerge.ClearBlendShapes();
                            foreach (var shape in shapes) shape.AddToMesh(meshToMerge);  
                        }

                        #region Transfer Surface Data

                        if (merger.allowSurfaceDataTransfer) 
                        {

                            var mesh = meshToMerge;

                            Quaternion alignmentRotationOffset = Quaternion.Euler(merger.alignmentRotationOffset);

                            List<string> originalBlendShapes = new List<string>();
                            List<BlendShape> blendShapes = meshToMerge.GetBlendShapes();
                            foreach (var shape in blendShapes) originalBlendShapes.Add(shape.name);

                            bool IsOriginalBlendShape(string name)
                            {
                                foreach(var shapeName in originalBlendShapes) if (shapeName == name) return true;

                                return false;
                            }
                            BlendShape AddOrGetBlendShape(string name, BlendShape.Frame[] referenceFrames)
                            {
                                foreach (var shape in blendShapes)
                                {
                                    if (shape.name == name)
                                    {
                                        if (shape.frames == null || shape.frames.Length <= 0) shape.AddFrame(referenceFrames == null || referenceFrames.Length <= 0 ? 1 : referenceFrames[0].weight, new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount]);

                                        return shape;
                                    }
                                }

                                BlendShape newShape = new BlendShape(name, referenceFrames, mesh.vertexCount, false);
                                blendShapes.Add(newShape);

                                return newShape;
                            }

                            List<MeshDataTools.MeshIsland> meshIslands = null;
                            int[] meshIslandBindings = null;
                            ContainingTri[] meshIslandTriBindings = null;
                            if (merger.treatAsMeshIslands) meshIslands = MeshDataTools.CalculateMeshIslands(mesh);

                            Vector3[] localVertices = mesh.vertices;
                            for (int c = 0; c < localVertices.Length; c++) localVertices[c] = (alignmentRotationOffset * localVertices[c]) + merger.alignmentOffset;

                            Vector3[] localNormals = mesh.normals;

                            Dictionary<int2, float> newBoneWeights = new Dictionary<int2, float>();

                            if (meshIslands != null)
                            {
                                meshIslandBindings = new int[localVertices.Length];
                                for (int c = 0; c < meshIslandBindings.Length; c++) meshIslandBindings[c] = -1;

                                meshIslandTriBindings = new ContainingTri[meshIslands.Count];

                                for (int c = 0; c < meshIslands.Count; c++)
                                {

                                    var island = meshIslands[c];

                                    if (island.vertices == null || island.vertices.Length <= 0) continue;

                                    Vector3 localVertex;

                                    float closestDistance = float.MaxValue;
                                    int closestLocalIndex = -1;

                                    for (int d = 0; d < island.vertices.Length; d++)
                                    {
                                        float distanceWeight = 1; // TODO: Add possible distance weighting based on position in the mesh island or vertex group or vertex colors

                                        int localIndex = island.vertices[d];
                                        localVertex = localVertices[localIndex];

                                        meshIslandBindings[localIndex] = c;

                                        foreach (var v in baseMeshVertices)
                                        {
                                            float dista = (v - localVertex).sqrMagnitude * distanceWeight;
                                            if (dista < closestDistance)
                                            {
                                                closestLocalIndex = d;
                                                closestDistance = dista;
                                            }
                                        }
                                    }

                                    island.originIndex = closestLocalIndex;

                                    closestDistance = float.MaxValue;
                                    closestLocalIndex = -1;
                                    int closestIndex0 = -1;
                                    int closestIndex1 = -1;
                                    int closestIndex2 = -1;
                                    float closestWeight0 = 0;
                                    float closestWeight1 = 0;
                                    float closestWeight2 = 0;

                                    localVertex = localVertices[island.vertices[island.originIndex]]; 

                                    if (MeshDataTools.GetClosestContainingTriangle(baseMeshVertices, baseMeshTriangles, localVertex, out var index0, out var index1, out var index2, out var weight0, out var weight1, out var weight2, out var dist, 0.5f, 0.2f))
                                    {
                                        if (dist < closestDistance)
                                        {
                                            closestDistance = dist;
                                            closestIndex0 = index0;
                                            closestIndex1 = index1;
                                            closestIndex2 = index2;
                                            closestWeight0 = weight0;
                                            closestWeight1 = weight1;
                                            closestWeight2 = weight2;
                                        }
                                    }

                                    meshIslandTriBindings[c] = new ContainingTri(0, closestIndex0, closestIndex1, closestIndex2, closestWeight0, closestWeight1, closestWeight2);
                                }
                            }

                            for (int vIndex = 0; vIndex < localVertices.Length; vIndex++)
                            {
                                var localVertex = localVertices[vIndex];
                                var centerPoint = localVertex;

                                int meshIslandIndex = meshIslandBindings == null ? -1 : meshIslandBindings[vIndex];
                                var meshIsland = meshIslandIndex >= 0 ? meshIslands[meshIslandIndex] : default;
                                var meshIslandTri = meshIslandIndex >= 0 ? meshIslandTriBindings[meshIslandIndex] : default;
                                if (meshIslandIndex >= 0) centerPoint = localVertices[meshIsland.OriginVertex];

                                Vector3 originOffset = localVertex - centerPoint;
                                float originOffsetDist = originOffset.magnitude;

                                bool hasOrigin = false;
                                if (originOffsetDist > 0.0001f)
                                {
                                    originOffset = originOffset / originOffsetDist;
                                    hasOrigin = true;
                                }

                                float closestDistance = float.MaxValue;
                                int closestIndex0 = -1;
                                int closestIndex1 = -1;
                                int closestIndex2 = -1;
                                float closestWeight0 = 0;
                                float closestWeight1 = 0;
                                float closestWeight2 = 0;

                                if (meshIslandIndex < 0)
                                {

                                    if (MeshDataTools.GetClosestContainingTriangle(baseMeshVertices, baseMeshTriangles, localVertex, out var index0, out var index1, out var index2, out var weight0, out var weight1, out var weight2, out var dist, 0.5f, 0.2f))
                                    {
                                        if (dist < closestDistance)
                                        {
                                            closestDistance = dist;
                                            closestIndex0 = index0;
                                            closestIndex1 = index1;
                                            closestIndex2 = index2;
                                            closestWeight0 = weight0;
                                            closestWeight1 = weight1;
                                            closestWeight2 = weight2;
                                        }
                                    }

                                }
                                else
                                {
                                    closestIndex0 = meshIslandTri.indexA;
                                    closestIndex1 = meshIslandTri.indexB;
                                    closestIndex2 = meshIslandTri.indexC;
                                    closestWeight0 = meshIslandTri.weightA;
                                    closestWeight1 = meshIslandTri.weightB;
                                    closestWeight2 = meshIslandTri.weightC;
                                }

                                if (closestIndex0 >= 0 && closestIndex1 >= 0 && closestIndex2 >= 0)
                                {
                                    if (merger.transferBlendShapes)
                                    {
                                        var dependencyNormal = ((baseMeshNormals[closestIndex0] * closestWeight0) + (baseMeshNormals[closestIndex1] * closestWeight1) + (baseMeshNormals[closestIndex2] * closestWeight2)).normalized;

                                        Vector3 AddNormalBasedRotationToDelta(Vector3 deltaVertex, Vector3 deltaNormal)
                                        {
                                            if (hasOrigin)
                                            {
                                                Quaternion rotOffset = Quaternion.FromToRotation(dependencyNormal, (dependencyNormal + deltaNormal).normalized);
                                                deltaVertex = deltaVertex + ((centerPoint + (rotOffset * originOffset) * originOffsetDist) - localVertex);
                                            }

                                            return deltaVertex;
                                        }

                                        for (int d = 0; d < baseMeshBlendShapes.Count; d++)
                                        {
                                            var shape = baseMeshBlendShapes[d];
                                            if (shape != null && (!merger.preserveExistingBlendShapeData || !IsOriginalBlendShape(shape.name)))
                                            {
                                                var blendShape = AddOrGetBlendShape(shape.name, shape.frames);
                                                if (blendShape != null)
                                                {
                                                    for (int e = 0; e < blendShape.frames.Length; e++)
                                                    {
                                                        var frame = blendShape.frames[0];
                                                        var baseFrame = shape.frames[e];

                                                        var deltaVertex = (baseFrame.deltaVertices[closestIndex0] * closestWeight0) + (baseFrame.deltaVertices[closestIndex1] * closestWeight1) + (baseFrame.deltaVertices[closestIndex2] * closestWeight2);
                                                        var deltaNormal = (baseFrame.deltaNormals[closestIndex0] * closestWeight0) + (baseFrame.deltaNormals[closestIndex1] * closestWeight1) + (baseFrame.deltaNormals[closestIndex2] * closestWeight2);
                                                        var deltaTangent = (baseFrame.deltaTangents[closestIndex0] * closestWeight0) + (baseFrame.deltaTangents[closestIndex1] * closestWeight1) + (baseFrame.deltaTangents[closestIndex2] * closestWeight2);

                                                        deltaVertex = AddNormalBasedRotationToDelta(deltaVertex, deltaNormal);

                                                        frame.deltaVertices[vIndex] = deltaVertex;
                                                        frame.deltaNormals[vIndex] = deltaNormal;
                                                        frame.deltaTangents[vIndex] = deltaTangent;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (merger.transferNormals)
                                    {
                                        localNormals[vIndex] = Vector3.LerpUnclamped(localNormals[vIndex], (baseMeshNormals[closestIndex0] * closestWeight0) + (baseMeshNormals[closestIndex1] * closestWeight1) + (baseMeshNormals[closestIndex2] * closestWeight2), merger.transferNormalsWeight).normalized;

                                        for (int d = 0; d < baseMeshBlendShapes.Count; d++)
                                        {
                                            var shape = baseMeshBlendShapes[d];
                                            if (shape != null)
                                            {
                                                var blendShape = AddOrGetBlendShape(shape.name, shape.frames);
                                                if (blendShape != null)
                                                {
                                                    for (int e = 0; e < blendShape.frames.Length; e++)
                                                    {
                                                        var frame = blendShape.frames[0];
                                                        var baseFrame = shape.frames[e];

                                                        var deltaNormal = (baseFrame.deltaNormals[closestIndex0] * closestWeight0) + (baseFrame.deltaNormals[closestIndex1] * closestWeight1) + (baseFrame.deltaNormals[closestIndex2] * closestWeight2);

                                                        Vector3.LerpUnclamped(frame.deltaNormals[vIndex], deltaNormal, merger.transferNormalsWeight);  

                                                        frame.deltaNormals[vIndex] = deltaNormal;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (merger.transferBoneWeights)
                                    {
                                        var boneWeights = baseMeshBoneWeights;
                                        var bonesPerVertex = baseMeshBonesPerVertex;
                                        var boneWeightStartIndices = baseMeshBoneWeightStartIndices;

                                        if (boneWeights.IsCreated && bonesPerVertex.IsCreated && boneWeightStartIndices != null)
                                        {
                                            int startIndex = boneWeightStartIndices[closestIndex0];
                                            int count = bonesPerVertex[closestIndex0];
                                            for (int d = 0; d < count; d++)
                                            {
                                                int index = startIndex + d;
                                                var bw = boneWeights[index];

                                                int2 i = new int2(vIndex, bw.boneIndex);

                                                newBoneWeights.TryGetValue(i, out float currentWeight);
                                                newBoneWeights[i] = currentWeight + (bw.weight * closestWeight0);
                                            }

                                            startIndex = boneWeightStartIndices[closestIndex1];
                                            count = bonesPerVertex[closestIndex1];
                                            for (int d = 0; d < count; d++)
                                            {
                                                int index = startIndex + d;
                                                var bw = boneWeights[index];

                                                int2 i = new int2(vIndex, bw.boneIndex);

                                                newBoneWeights.TryGetValue(i, out float currentWeight);
                                                newBoneWeights[i] = currentWeight + (bw.weight * closestWeight1);
                                            }

                                            startIndex = boneWeightStartIndices[closestIndex2];
                                            count = bonesPerVertex[closestIndex2];
                                            for (int d = 0; d < count; d++)
                                            {
                                                int index = startIndex + d;
                                                var bw = boneWeights[index];

                                                int2 i = new int2(vIndex, bw.boneIndex);

                                                newBoneWeights.TryGetValue(i, out float currentWeight);
                                                newBoneWeights[i] = currentWeight + (bw.weight * closestWeight2);
                                            }
                                        }
                                    }
                                }
                            }

                            if (merger.transferBlendShapes || merger.transferNormals)
                            {
                                meshToMerge.ClearBlendShapes();
                                foreach (var shape in blendShapes) shape.AddToMesh(meshToMerge); 
                            }
                            
                            if (merger.transferNormals)
                            {
                                mesh.normals = localNormals;
                            }

                            if (merger.transferBoneWeights && newBoneWeights.Count > 0)
                            {
                                var boneWeights = new NativeList<BoneWeight1>(meshToMerge.vertexCount, Allocator.Persistent);
                                var bonesPerVertex = new NativeArray<byte>(meshToMerge.vertexCount, Allocator.Persistent);

                                List<BoneWeight1> vertexBoneWeights = new List<BoneWeight1>(); 
                                for (int c = 0; c < meshToMerge.vertexCount; c++)
                                {
                                    vertexBoneWeights.Clear();
                                    foreach (var pair in newBoneWeights)
                                    {
                                        if (pair.Key.x == c)
                                        {
                                            var bw = new BoneWeight1();
                                            bw.boneIndex = pair.Key.y;
                                            bw.weight = pair.Value;

                                            vertexBoneWeights.Add(bw);
                                        }
                                    }
                                    vertexBoneWeights.Sort((BoneWeight1 a, BoneWeight1 b) => (int)Mathf.Sign(b.weight - a.weight));
                                    if (vertexBoneWeights.Count > 8) vertexBoneWeights.RemoveRange(8, vertexBoneWeights.Count - 8);

                                    float totalWeight = 0;
                                    foreach (var bw in vertexBoneWeights) totalWeight += bw.weight;
                                    if (totalWeight > 0)
                                    {
                                        for (int d = 0; d < vertexBoneWeights.Count; d++)
                                        {
                                            var bw = vertexBoneWeights[d];
                                            bw.weight /= totalWeight;
                                            vertexBoneWeights[d] = bw;
                                        }
                                    }

                                    for (int d = 0; d < vertexBoneWeights.Count; d++)
                                    {
                                        var bw = vertexBoneWeights[d];
                                        boneWeights.Add(bw);
                                    }
                                    bonesPerVertex[c] = (byte)vertexBoneWeights.Count;
                                }

                                meshToMerge.SetBoneWeights(bonesPerVertex, boneWeights.AsArray());

                                boneWeights.Dispose();
                                bonesPerVertex.Dispose();
                            }

                        }

                        #endregion

                        if (merger.lodSlot == 0)
                        {
                            string meshName = mainMesh.name;
                            var mergeMesh = MergeMeshSeam(meshToMerge, mainMesh, target.seamShape, target.seamMergeMethod, out _, true, true, target.mergeSeamTangents, target.mergeSeamUVs, nonSeamMergableBlendShapes);
                            mainMesh = MeshDataTools.CombineMeshes(mainMesh, new Mesh[] { mergeMesh });
                            mainMesh.name = meshName;

                            if (merger.updateBaseMeshData)
                            {
                                UpdateBaseMeshData(mainMesh);
                            }
                        }
                        else if (lodMeshes != null && merger.lodSlot - 1 >= 0 && merger.lodSlot - 1 < lodMeshes.Length)
                        {
                            int slot = merger.lodSlot - 1;

                            string meshName = lodMeshes[slot].name;
                            var mergeMesh = MergeMeshSeam(meshToMerge, lodMeshes[slot], target.seamShape, target.seamMergeMethod, out _, true, true, target.mergeSeamTangents, target.mergeSeamUVs, nonSeamMergableBlendShapes); 
                            lodMeshes[slot] = MeshDataTools.CombineMeshes(lodMeshes[slot], new Mesh[] { mergeMesh });
                            lodMeshes[slot].name = meshName;
                        }
                    }

                    if (baseMeshBoneWeights.IsCreated) baseMeshBoneWeights.Dispose();
                    if (baseMeshBonesPerVertex.IsCreated) baseMeshBonesPerVertex.Dispose();
                }

                var rendererBoundsCenter = target.boundsCenter;
                var rendererBoundsExtents = target.boundsExtents;
                var boundsCenter = rendererBoundsCenter;
                var boundsExtents = rendererBoundsExtents; 
                if (target.applyInitialOffset)
                {
                    Quaternion offsetRot = Quaternion.Euler(target.initialRotationOffset);
                    Mesh ApplyInitialOffsetToMesh(Mesh mesh)
                    {
                        if (mesh == null) return null;

                        var meshOut = MeshUtils.DuplicateMesh(mesh); 
                        meshOut.name = mesh.name;

                        var verts = meshOut.vertices;
                        var norms = meshOut.normals;
                        var tans = meshOut.tangents;
                        var shaps = meshOut.GetBlendShapes();

                        for(int i = 0; i < meshOut.vertexCount; i++)
                        {
                            verts[i] = (offsetRot * verts[i]) + target.initialOffset;
                            if (i < norms.Length) norms[i] = offsetRot * norms[i];
                            if (i < tans.Length) tans[i] = new float4(math.rotate(offsetRot, new float3(tans[i].x, tans[i].y, tans[i].z)), tans[i].w);

                            for(int j = 0; j < shaps.Count; j++)
                            {
                                var shape = shaps[j];
                                if (shape == null || shape.frames == null) continue;

                                for(int k = 0; k < shape.frames.Length; k++)
                                {
                                    var frame = shape.frames[k];

                                    frame.deltaVertices[i] = offsetRot * frame.deltaVertices[i];
                                    frame.deltaNormals[i] = offsetRot * frame.deltaNormals[i];
                                    frame.deltaTangents[i] = offsetRot * frame.deltaTangents[i];
                                }
                            }
                        }

                        meshOut.vertices = verts;
                        meshOut.normals = norms;
                        meshOut.tangents = tans;

                        meshOut.ClearBlendShapes();
                        foreach (var shape in shaps) shape.AddToMesh(meshOut);

                        meshOut.RecalculateBounds();
                        return meshOut;
                    }

                    mainMesh = ApplyInitialOffsetToMesh(mainMesh);
                    if (lodMeshes != null)
                    {
                        for (int l = 0; l < lodMeshes.Length; l++) lodMeshes[l] = ApplyInitialOffsetToMesh(lodMeshes[l]);
                    }
                    
                    boundsCenter = (offsetRot * boundsCenter) + target.initialOffset;
                    boundsExtents = offsetRot * boundsExtents;
                }

                // Merge Seam
                seamShapes.Clear();
                if (!string.IsNullOrWhiteSpace(target.seamShape) && baseMeshIndex >= 0)
                {
                    mainMesh = MergeMeshSeam(mainMesh, targetMeshes[baseMeshIndex].mainMesh, target.seamShape, target.seamMergeMethod, out var seamShape, true, true, target.mergeSeamTangents, target.mergeSeamUVs, nonSeamMergableBlendShapes); 
                    seamShapes.Add(seamShape);

                    for (int b = 0; b < lodMeshes.Length; b++) 
                    {
                        if (b >= targetMeshes[baseMeshIndex].lods.Length) continue; // base mesh doesn't have the same amount of lods

                        lodMeshes[b] = MergeMeshSeam(lodMeshes[b], targetMeshes[baseMeshIndex].lods[b].mesh, target.seamShape, target.seamMergeMethod, out seamShape, true, true, target.mergeSeamTangents, target.mergeSeamUVs, nonSeamMergableBlendShapes); 
                        seamShapes.Add(seamShape);
                    }
                }

                // Solidify
                if (target.solidifyThickness > 0.00001f)
                {
                    var solidifiedMesh = MeshDataTools.Solidify(mainMesh, target.solidifyThickness, 0f, true, true);
                    solidifiedMesh.name = mainMesh.name;
                    mainMesh = solidifiedMesh;
                    Debug.Log($"SOLIDIFIED: {target.name} : {target.solidifyThickness} : {solidifiedMesh.name} :: {solidifiedMesh.GetType().FullName}");
                }

                // Create vertex groups
                outputData.vertexGroupsBufferRange = CreateVertexGroups(target.name, mainMesh, vertexGroups, false, false);
                outputData.muscleVertexGroupsBufferRange = CreateVertexGroups(target.name, mainMesh, muscleGroups, normalizeMuscleGroupsAsPool, averageMuscleGroupsAsPool);
                outputData.fatVertexGroupsBufferRange = CreateVertexGroups(target.name, mainMesh, fatGroups, normalizeFatGroupsAsPool, averageFatGroupsAsPool);
                float2[] fatModifiers = new float2[fatGroups.Length];
                for (int b = 0; b < fatModifiers.Length; b++) fatModifiers[b] = ICustomizableCharacter.Defaults._defaultFatGroupModifier;
                if (fatGroupModifiers != null)
                {
                    for(int b = 0; b < fatGroupModifiers.Length; b++)
                    {
                        var modifier = fatGroupModifiers[b];
                        for(int c = outputData.fatVertexGroupsBufferRange.x; c <= outputData.fatVertexGroupsBufferRange.y; c++)
                        {
                            var group = finalVertexGroups[c];
                            if (group.name == modifier.name) fatModifiers[c - outputData.fatVertexGroupsBufferRange.x] = modifier.muscleModifiers; 
                        }
                    }
                }
                outputData.fatGroupModifiers = fatModifiers;

                variationGroupIndices.Clear();
                var mainVariationRange = CreateVertexGroups(target.name, mainMesh, variationGroups, normalizeVariationGroupsAsPool, averageVariationGroupsAsPool);
                for (int b = mainVariationRange.x; b <= mainVariationRange.y; b++) variationGroupIndices.Add(b);

                // Create Composite VertexGroups
                if (compositeVertexGroups != null)
                {
                    List<CompositeVertexGroup> compGroups = new List<CompositeVertexGroup>(compositeVertexGroups);  
                    compGroups.Sort((CompositeVertexGroup groupA, CompositeVertexGroup groupB) => (int)Mathf.Sign(groupA.weight - groupB.weight));

                    foreach(var group in compGroups)
                    {
                        var vg = new VertexGroup(group.name); 

                        if (group.addGroups != null)
                        {
                            foreach (var groupName in group.addGroups)
                            {
                                foreach (var vg2 in finalVertexGroups)
                                {
                                    if (vg2.name == groupName)
                                    {
                                        vg.Add(vg2);
                                        break;
                                    }
                                }
                            }
                        }
                        if (group.subtractGroups != null)
                        {
                            foreach (var groupName in group.subtractGroups)
                            {
                                foreach (var vg2 in finalVertexGroups)
                                {
                                    if (vg2.name == groupName)
                                    {
                                        vg.Subtract(vg2, true, 0); 
                                        break;
                                    }
                                }
                            }
                        }

                        if (group.normalize) vg.Normalize();  

                        switch(group.type)
                        {
                            case VertexGroupType.StandaloneVertexGroup:
                                if (outputData.muscleVertexGroupsBufferRange.x >= outputData.vertexGroupsBufferRange.y) outputData.muscleVertexGroupsBufferRange = new Vector2Int(outputData.muscleVertexGroupsBufferRange.x + 1, outputData.muscleVertexGroupsBufferRange.y + 1);
                                if (outputData.fatVertexGroupsBufferRange.x >= outputData.vertexGroupsBufferRange.y) outputData.fatVertexGroupsBufferRange = new Vector2Int(outputData.fatVertexGroupsBufferRange.x + 1, outputData.fatVertexGroupsBufferRange.y + 1);
                                if (outputData.variationGroupIndices != null)
                                {
                                    for(int b = 0; b < outputData.variationGroupIndices.Length; b++)
                                    {
                                        var ind = outputData.variationGroupIndices[b];
                                        if (ind >= outputData.vertexGroupsBufferRange.y) ind = ind + 1;
                                        outputData.variationGroupIndices[b] = ind;
                                    }
                                }

                                if (outputData.vertexGroupsBufferRange.y + 1 >= finalVertexGroups.Count) finalVertexGroups.Add(vg); else finalVertexGroups.Insert(outputData.vertexGroupsBufferRange.y + 1, vg);
                                outputData.vertexGroupsBufferRange = new Vector2Int(outputData.vertexGroupsBufferRange.x, outputData.vertexGroupsBufferRange.y + 1);
                                break;

                            case VertexGroupType.MuscleVertexGroup:
                                if (outputData.vertexGroupsBufferRange.x >= outputData.muscleVertexGroupsBufferRange.y) outputData.vertexGroupsBufferRange = new Vector2Int(outputData.vertexGroupsBufferRange.x + 1, outputData.vertexGroupsBufferRange.y + 1);
                                if (outputData.fatVertexGroupsBufferRange.x >= outputData.muscleVertexGroupsBufferRange.y) outputData.fatVertexGroupsBufferRange = new Vector2Int(outputData.fatVertexGroupsBufferRange.x + 1, outputData.fatVertexGroupsBufferRange.y + 1);
                                if (outputData.variationGroupIndices != null)
                                {
                                    for (int b = 0; b < outputData.variationGroupIndices.Length; b++)
                                    {
                                        var ind = outputData.variationGroupIndices[b];
                                        if (ind >= outputData.muscleVertexGroupsBufferRange.y) ind = ind + 1;
                                        outputData.variationGroupIndices[b] = ind;
                                    }
                                }

                                if (outputData.muscleVertexGroupsBufferRange.y + 1 >= finalVertexGroups.Count) finalVertexGroups.Add(vg); else finalVertexGroups.Insert(outputData.muscleVertexGroupsBufferRange.y + 1, vg);
                                outputData.muscleVertexGroupsBufferRange = new Vector2Int(outputData.muscleVertexGroupsBufferRange.x, outputData.muscleVertexGroupsBufferRange.y + 1);
                                break;

                            case VertexGroupType.FatVertexGroup:
                                if (outputData.vertexGroupsBufferRange.x >= outputData.fatVertexGroupsBufferRange.y) outputData.vertexGroupsBufferRange = new Vector2Int(outputData.vertexGroupsBufferRange.x + 1, outputData.vertexGroupsBufferRange.y + 1);
                                if (outputData.muscleVertexGroupsBufferRange.x >= outputData.fatVertexGroupsBufferRange.y) outputData.muscleVertexGroupsBufferRange = new Vector2Int(outputData.muscleVertexGroupsBufferRange.x + 1, outputData.muscleVertexGroupsBufferRange.y + 1);
                                if (outputData.variationGroupIndices != null)
                                {
                                    for (int b = 0; b < outputData.variationGroupIndices.Length; b++)
                                    {
                                        var ind = outputData.variationGroupIndices[b];
                                        if (ind >= outputData.fatVertexGroupsBufferRange.y) ind = ind + 1;
                                        outputData.variationGroupIndices[b] = ind;
                                    }
                                }

                                if (outputData.fatVertexGroupsBufferRange.y + 1 >= finalVertexGroups.Count) finalVertexGroups.Add(vg); else finalVertexGroups.Insert(outputData.fatVertexGroupsBufferRange.y + 1, vg);
                                outputData.fatVertexGroupsBufferRange = new Vector2Int(outputData.fatVertexGroupsBufferRange.x, outputData.fatVertexGroupsBufferRange.y + 1);
                                break;

                            case VertexGroupType.VariationVertexGroup:

                                variationGroupIndices.Add(finalVertexGroups.Count);
                                finalVertexGroups.Add(vg);

                                break;
                        }
                    }
                }

                if (additionalVariationGroups != null)
                {
                    foreach (var groupName in additionalVariationGroups)
                    {
                        for (int b = 0; b < finalVertexGroups.Count; b++)
                        {
                            var group = finalVertexGroups[b];
                            if (group.name == groupName)
                            {
                                variationGroupIndices.Add(b);
                                break;
                            }
                        }
                    }
                }

                // Merge Vertex Groups at Seam
                if (baseMeshIndex >= 0 && baseMeshIndex < a && seamShapes.Count > 0)
                {
                    tempVertexGroups.Clear();
                    outputDatas[baseMeshIndex].GetVertexGroups(tempVertexGroups);
                    MergeVertexGroupsAtSeam(mainMesh, targetMeshes[baseMeshIndex].mainMesh, seamShapes[0], finalVertexGroups, tempVertexGroups, nonSeamMergableBlendShapes);
                }

                outputData.variationGroupIndices = variationGroupIndices.ToArray();
                outputData.SetVertexGroups(finalVertexGroups.ToArray()); 

                // Create morph shapes
                tempBlendShapeTargets2.Clear();
                tempBlendShapeTargets2.AddRange(standaloneShapes);
                tempBlendShapeTargets2.AddRange(target.localStandaloneShapes);         
                outputData.standaloneShapesBufferRange = CreateMorphShapes(target.name, mainMesh, tempBlendShapeTargets2, null);

                tempWeights.Clear();
                outputData.muscleShapesBufferRange = CreateMorphShapes(target.name, mainMesh, muscleShapes, tempWeights);
                outputData.frameWeightsMuscleShapes = tempWeights.ToArray();
                outputData.defaultShapeMuscleWeight = defaultShapeMuscleWeight;

                tempWeights.Clear();
                outputData.flexShapesBufferRange = CreateMorphShapes(target.name, mainMesh, flexShapes, tempWeights);
                outputData.frameWeightsFlexShapes = tempWeights.ToArray();

                tempWeights.Clear();
                outputData.fatShapesBufferRange = CreateMorphShapes(target.name, mainMesh, fatShapes, tempWeights);
                outputData.frameWeightsFatShapes = tempWeights.ToArray();

                outputData.fatMuscleBlendShapeIndex = CreateMorphShapes(target.name, mainMesh, new BlendShapeTarget[] { fatMuscleBlendShape }, null).x;
                outputData.fatMuscleBlendWeightRange = fatMuscleBlendWeightRange;

                outputData.variationShapesBufferRange = CreateMorphShapes(target.name, mainMesh, variationShapes, null);

                outputData.midlineVertexGroupIndex = -1;
                if (!string.IsNullOrWhiteSpace(midlineVertexGroup))
                {
                    for (int b = 0; b < finalVertexGroups.Count; b++) if (b >= outputData.vertexGroupsBufferRange.x && b <= outputData.vertexGroupsBufferRange.y && finalVertexGroups[b].name == midlineVertexGroup)
                        {
                            outputData.midlineVertexGroupIndex = b - outputData.vertexGroupsBufferRange.x;
                            break;
                        }
                }

                outputData.bustSizeShapeIndex = -1;
                outputData.bustVertexGroupIndex = -1;
                outputData.bustNerfVertexGroupIndex = -1;
                outputData.nippleMaskVertexGroupIndex = -1;
                outputData.genitalMaskVertexGroupIndex = -1;
                if (!string.IsNullOrWhiteSpace(bustVertexGroup) && !string.IsNullOrWhiteSpace(bustSizeShape))
                {
                    for (int b = 0; b < finalVertexGroups.Count; b++) if (b >= outputData.vertexGroupsBufferRange.x && b <= outputData.vertexGroupsBufferRange.y && finalVertexGroups[b].name == bustVertexGroup)
                        {
                            outputData.bustVertexGroupIndex = b - outputData.vertexGroupsBufferRange.x;
                            break;
                        }
                    for (int b = 0; b < finalMorphShapes.Count; b++) if (b >= outputData.standaloneShapesBufferRange.x && b <= outputData.standaloneShapesBufferRange.y && finalMorphShapes[b].name == bustSizeShape)
                        {
                            outputData.bustSizeShapeIndex = b - outputData.standaloneShapesBufferRange.x;
                            break;
                        }

                    //var bustVG = finalVertexGroups[outputData.vertexGroupsBufferRange.x + outputData.bustVertexGroupIndex];
                    //var bustShape = finalMorphShapes[outputData.standaloneShapesBufferRange.x + outputData.bustSizeShapeIndex];
                    //bustShape.ApplyVertexGroupAsCookie(bustVG);
                }
                if (!string.IsNullOrWhiteSpace(bustNerfVertexGroup))
                {
                    for (int b = 0; b < finalVertexGroups.Count; b++) if (b >= outputData.vertexGroupsBufferRange.x && b <= outputData.vertexGroupsBufferRange.y && finalVertexGroups[b].name == bustNerfVertexGroup)
                        {
                            outputData.bustNerfVertexGroupIndex = b - outputData.vertexGroupsBufferRange.x;
                            break;
                        }
                }
                if (!string.IsNullOrWhiteSpace(nippleMaskVertexGroup))
                {
                    for (int b = 0; b < finalVertexGroups.Count; b++) if (b >= outputData.vertexGroupsBufferRange.x && b <= outputData.vertexGroupsBufferRange.y && finalVertexGroups[b].name == nippleMaskVertexGroup)
                        {
                            outputData.nippleMaskVertexGroupIndex = b - outputData.vertexGroupsBufferRange.x;
                            break;
                        }
                }
                if (!string.IsNullOrWhiteSpace(genitalMaskVertexGroup))
                {
                    for (int b = 0; b < finalVertexGroups.Count; b++) if (b >= outputData.vertexGroupsBufferRange.x && b <= outputData.vertexGroupsBufferRange.y && finalVertexGroups[b].name == genitalMaskVertexGroup)
                        {
                            outputData.genitalMaskVertexGroupIndex = b - outputData.vertexGroupsBufferRange.x;
                            break;
                        }
                }
                 
                // Merge Morph Shapes at Seam
                if (baseMeshIndex >= 0 && baseMeshIndex < a && seamShapes.Count > 0)
                {
                    tempMorphShapes.Clear();
                    outputDatas[baseMeshIndex].GetMorphs(tempMorphShapes);
                    MergeMorphsAtSeam(mainMesh, targetMeshes[baseMeshIndex].mainMesh, seamShapes[0], finalMorphShapes, tempMorphShapes, nonSeamMergableBlendShapes);
                }

                outputData.SetMorphShapes(finalMorphShapes.ToArray());

                // Create LODs
                Mesh outputMeshMain = null;
                var baseVertices = mainMesh.vertices;
                var baseUV = useUVsToFindClosestVertex ? mainMesh.GetUVsByChannelAsList((int)nearestVertexUVChannel) : null;
                if (storeNearestVertexInUV)
                {
                    List<Vector4> uv3 = mainMesh.GetUVsByChannelAsListV4(3);
                    if (uv3 == null || uv3.Count < mainMesh.vertexCount)
                    {
                        uv3 = new List<Vector4>();
                        for (int b = 0; b < mainMesh.vertexCount; b++) uv3.Add(Vector4.zero);
                    }

                    for (int b = 0; b < uv3.Count; b++) uv3[b] = StoreIndexInUV(uv3[b], b);

                    if (outputMeshMain == null) outputMeshMain = MeshUtils.DuplicateMesh(mainMesh);
                    outputMeshMain.SetUVs(3, uv3);
                }

                if (outputMeshMain == null && !ReferenceEquals(mainMesh, target.mainMesh)) outputMeshMain = mainMesh;
                if (outputMeshMain != null)
                {
                    outputMeshMain.ClearBlendShapes();

                    outputMeshMain.name = mainMesh.name + "_" + target.name;
                    outputMeshes.Add(outputMeshMain);
                }
                else outputMeshMain = mainMesh;

                meshLODs.Clear();
                if (target.lods != null)
                {
                    for (int b = 0; b < target.lods.Length; b++)
                    {
                        var lod = target.lods[b];

                        Mesh lodMesh = lodMeshes[b];
                        Mesh outputMesh = null;
                        if (storeNearestVertexInUV)
                        {
                            var lodVertices = lodMesh.vertices;
                            var mergedVertices = MeshDataTools.WeldVertices(lodVertices);

                            List<Vector4> uv3 = lodMesh.GetUVsByChannelAsListV4(3); 
                            if (uv3 == null || uv3.Count < lodMesh.vertexCount)
                            {
                                uv3 = new List<Vector4>();
                                for (int c = 0; c < lodMesh.vertexCount; c++) uv3.Add(Vector4.zero);
                            }

                            int[] nearestVertices = new int[lodMesh.vertexCount];
                            if (useUVsToFindClosestVertex)
                            {
                                var lodUV = lodMesh.GetUVsByChannelAsList((int)nearestVertexUVChannel);
                                MeshDataTools.FindClosestVerticesUV(lodUV, baseUV, nearestVertices);
                            }
                            else
                            {
                                MeshDataTools.FindClosestVertices(lodVertices, baseVertices, nearestVertices);
                            }

                            for (int c = 0; c < uv3.Count; c++) uv3[c] = StoreIndexInUV(uv3[c], nearestVertices[c]);  
                            if (mergedVertices != null)
                            {
                                for(int c = 0; c < mergedVertices.Length; c++) 
                                {
                                    var merge = mergedVertices[c];
                                    uv3[c] = uv3[merge.firstIndex]; 
                                }
                            }

                            if (outputMesh == null) outputMesh = MeshUtils.DuplicateMesh(lodMesh);
                            outputMesh.SetUVs(3, uv3);
                        }

                        if (outputMesh != null)
                        {
                            outputMesh.ClearBlendShapes();

                            if (target.solidifyThickness > 0f)
                            {
                                var solidifiedMesh = MeshDataTools.Solidify(outputMesh, target.solidifyThickness, 0f, true, true);
                                solidifiedMesh.name = outputMesh.name;
                                outputMesh = solidifiedMesh;
                            }

                            outputMesh.name = lodMesh.name + "_" + target.name;
                            outputMesh.bounds = new Bounds() { center = boundsCenter, extents = boundsExtents };
                            outputMeshes.Add(outputMesh);
                        }
                        else outputMesh = lodMesh;

                        meshLODs.Add(new MeshLOD() { mesh = outputMesh, screenRelativeTransitionHeight = lod.screenRelativeTransitionHeight });
                    }

                    meshLODs.Sort((MeshLOD lodA, MeshLOD lodB) => Math.Sign(lodB.screenRelativeTransitionHeight - lodA.screenRelativeTransitionHeight)); // descending order
                }
                meshLODs.Insert(0, new MeshLOD() { mesh = outputMeshMain, screenRelativeTransitionHeight = meshLODs.Count == 0 ? 1f : (meshLODs[0].screenRelativeTransitionHeight < 1f ? 1f : (meshLODs[0].screenRelativeTransitionHeight + 0.1f)) }); 

                outputData.SetMeshLODs(meshLODs.ToArray());
                outputDatas.Add(outputData);
                
                meshLODs.Clear();

                var meshGroup = new InstancedMeshGroup(target.name, new Material[] { target.material });

                outputData.meshGroups = new InstancedMeshGroup[] { meshGroup };

                outputData.boundsCenter = rendererBoundsCenter;
                outputData.boundsExtents = rendererBoundsExtents; 

                GameObject prefabChild = new GameObject(target.name);
                prefabChild.transform.SetParent(outputPrefab.transform, false);

                var characterMesh = prefabChild.AddComponent<CustomizableCharacterMesh>();
                characterMesh.SetScreenRelativeHeightBias(target.screenRelativeHeightBias); 
                characterMesh.SetMeshData(outputData);
                characterMesh.SetSubMeshindex(target.submesh);
                characterMesh.SetRigRoot(outputPrefab.transform.FindDeepChildLiberal(target.rigRootName));
                characterMesh.SetAvatar(target.avatar);
                characterMesh.SetRigBufferID(target.rigBufferId);
                characterMesh.SetShapeBufferID(target.shapeBufferId);
                characterMesh.SetMorphBufferID(target.morphBufferId);

                characterMeshes.Add(characterMesh);  

                if (genitalController != null)
                {
                    if (penisMeshIndex == a) genitalController.penisMesh = characterMesh;
                    if (vulvaMeshIndex == a) genitalController.vulvaMesh = characterMesh;
                }

                if (mainMeshIndex == a)
                {
                    if (animator == null) 
                    {
                        animator = outputPrefab.AddComponent<CustomAnimator>();
                    }

                    if (animator.avatar == null)
                    {
                        animator.avatar = target.avatar;
                    }
                }

                if (target.addOrLookForDynamicAnimationPropertyController)
                {
                    var dap = outputPrefab.transform.gameObject.AddOrGetComponent<DynamicAnimationProperties>();
                    characterMesh.SetAnimatablePropertiesController(dap);  
                }
            }

            var gc = outputPrefab.GetComponent<GenitalController>();
            for (int a = 0; a < targetMeshes.Length; a++)
            {
                var target = targetMeshes[a];
                var characterMesh = characterMeshes[a];

                int shapeInstanceIndex = IndexOfTargetMesh(target.shapesInstanceMeshGroup);
                if (shapeInstanceIndex >= 0) characterMesh.shapesInstanceReference = characterMeshes[shapeInstanceIndex];

                int rigInstanceIndex = IndexOfTargetMesh(target.rigInstanceMeshGroup);
                if (rigInstanceIndex >= 0) characterMesh.rigInstanceReference = characterMeshes[rigInstanceIndex];

                int characterInstanceIndex = IndexOfTargetMesh(target.characterInstanceMeshGroup);
                if (characterInstanceIndex >= 0) characterMesh.characterInstanceReference = characterMeshes[characterInstanceIndex]; 

                if (gc != null && target.isBreastMesh)
                {
                    if (gc.breastMeshes == null) gc.breastMeshes = new List<CustomizableCharacterMesh>();
                    gc.breastMeshes.Add(characterMesh);
                }
            }

            for (int a = 0; a < secondaryMeshes.Length; a++)
            {
                var target = secondaryMeshes[a];
                if (target.mainGroupDependencies == null || target.mainGroupDependencies.Length <= 0) continue;

                CustomizableCharacterMesh[] characterMeshDependencies = new CustomizableCharacterMesh[target.mainGroupDependencies.Length];

                int[][] characterMeshTriangles = new int[target.mainGroupDependencies.Length][];

                Vector3[][] characterMeshVertices = new Vector3[target.mainGroupDependencies.Length][];
                Vector3[][] characterMeshNormals = new Vector3[target.mainGroupDependencies.Length][];

                Vector2[][] characterMeshUVs = new Vector2[target.mainGroupDependencies.Length][];

                NativeArray<BoneWeight1>[] characterMeshBoneWeights = new NativeArray<BoneWeight1>[target.mainGroupDependencies.Length];
                NativeArray<byte>[] characterMeshBonesPerVertex = new NativeArray<byte>[target.mainGroupDependencies.Length];
                int[][] characterMeshBoneWeightStartIndices = new int[target.mainGroupDependencies.Length][];

                for (int b = 0; b < target.mainGroupDependencies.Length; b++)
                {
                    int index = IndexOfTargetMesh(target.mainGroupDependencies[b]);
                    if (index >= 0)
                    {
                        var dep = characterMeshes[index];

                        characterMeshDependencies[b] = dep;

                        characterMeshTriangles[b] = dep.MeshData.Mesh.triangles;

                        characterMeshVertices[b] = dep.MeshData.Mesh.vertices;
                        characterMeshNormals[b] = dep.MeshData.Mesh.normals;

                        characterMeshUVs[b] = dep.MeshData.Mesh.uv;

                        var boneWeights = new NativeArray<BoneWeight1>(dep.MeshData.Mesh.GetAllBoneWeights(), Allocator.Persistent);
                        var bonesPerVertex = new NativeArray<byte>(dep.MeshData.Mesh.GetBonesPerVertex(), Allocator.Persistent);
                        var boneWeightStartIndices = new int[dep.MeshData.Mesh.vertexCount];

                        if (bonesPerVertex.IsCreated)
                        {
                            int i = 0;
                            for (int c = 0; c < bonesPerVertex.Length; c++)
                            {
                                int count = bonesPerVertex[c];
                                boneWeightStartIndices[c] = i;
                                i += count;
                            }
                        }

                        characterMeshBoneWeights[b] = boneWeights;
                        characterMeshBonesPerVertex[b] = bonesPerVertex;
                        characterMeshBoneWeightStartIndices[b] = boneWeightStartIndices;

                    }
                }

                Quaternion alignmentRotationOffset = Quaternion.Euler(target.alignmentRotationOffset);
                Quaternion inverseAlignmentRotationOffset = Quaternion.Inverse(alignmentRotationOffset);

                List<Mesh> meshesToEvaluate = new List<Mesh>();
                meshesToEvaluate.Add(target.mainMesh);
                if (target.lods != null)
                {
                    for (int b = 0; b < target.lods.Length; b++)
                    {
                        var lod = target.lods[b];
                        meshesToEvaluate.Add(lod.mesh);
                    }
                }

                for (int b = 0; b < meshesToEvaluate.Count; b++)
                {
                    var mesh = meshesToEvaluate[b];

                    List<BlendShape> blendShapes = new List<BlendShape>();
                    if (target.mainMesh != null) target.mainMesh.GetBlendShapes(blendShapes);
                    BlendShape AddOrGetBlendShape(string name, BlendShape.Frame[] referenceFrames)
                    {
                        foreach (var shape in blendShapes)
                        {
                            if (shape.name == name)
                            {
                                if (shape.frames == null || shape.frames.Length <= 0) shape.AddFrame(referenceFrames == null || referenceFrames.Length <= 0 ? 1 : referenceFrames[0].weight, new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount]);

                                return shape;
                            }
                        }

                        BlendShape newShape = new BlendShape(name, referenceFrames, mesh.vertexCount, false);
                        blendShapes.Add(newShape);

                        return newShape;
                    }

                    VertexGroup setupMask = null;
                    if (!string.IsNullOrWhiteSpace(target.setupMaskShape))
                    {
                        BlendShape setupMaskShape = null;
                        foreach(var shape in blendShapes)
                        {
                            if (shape != null && shape.name == target.setupMaskShape)
                            {
                                setupMaskShape = shape;
                                break;
                            }
                        }

                        if (setupMaskShape != null)
                        {
                            setupMask = VertexGroup.ConvertToVertexGroup(setupMaskShape, true, null, 0.00001f, 1f, true);
                            
                            if (target.removeSetupMaskShape)
                            {
                                blendShapes.Remove(setupMaskShape);
                            }
                        }
                    }

                    List<MeshDataTools.MeshIsland> meshIslands = null;
                    int[] meshIslandBindings = null;
                    ContainingTri[] meshIslandTriBindings = null;
                    if (target.treatAsMeshIslands) meshIslands = MeshDataTools.CalculateMeshIslands(mesh);

                    Vector3[] localVertices = mesh.vertices;
                    for (int c = 0; c < localVertices.Length; c++) localVertices[c] = (alignmentRotationOffset * localVertices[c]) + target.alignmentOffset;

                    Dictionary<int2, float> newBoneWeights = new Dictionary<int2, float>();

                    if (meshIslands != null)
                    {
                        meshIslandBindings = new int[localVertices.Length];
                        for (int c = 0; c < meshIslandBindings.Length; c++) meshIslandBindings[c] = -1;

                        meshIslandTriBindings = new ContainingTri[meshIslands.Count];

                        for (int c = 0; c < meshIslands.Count; c++)
                        {

                            var island = meshIslands[c];

                            if (island.vertices == null || island.vertices.Length <= 0) continue;

                            Vector3 localVertex;

                            float closestDistance = float.MaxValue;
                            int closestLocalIndex = -1;

                            for (int d = 0; d < island.vertices.Length; d++)
                            {
                                float distanceWeight = 1; // TODO: Add possible distance weighting based on position in the mesh island or vertex group or vertex colors

                                int localIndex = island.vertices[d];
                                localVertex = localVertices[localIndex];

                                meshIslandBindings[localIndex] = c;

                                for (int e = 0; e < characterMeshDependencies.Length; e++)
                                {
                                    var verts = characterMeshVertices[e];

                                    if (verts == null) continue;

                                    foreach(var v in verts)
                                    {
                                        float dist = (v - localVertex).sqrMagnitude * distanceWeight;
                                        if (dist < closestDistance)
                                        {
                                            closestLocalIndex = d;
                                            closestDistance = dist;
                                        }
                                    }
                                }
                            }

                            island.originIndex = closestLocalIndex;

                            closestDistance = float.MaxValue;
                            closestLocalIndex = -1;
                            int closestDependency = -1;
                            int closestIndex0 = -1;
                            int closestIndex1 = -1;
                            int closestIndex2 = -1;
                            float closestWeight0 = 0;
                            float closestWeight1 = 0;
                            float closestWeight2 = 0;

                            localVertex = localVertices[island.vertices[island.originIndex]];

                            for (int d = 0; d < characterMeshDependencies.Length; d++)
                            {
                                var tris = characterMeshTriangles[d];
                                var verts = characterMeshVertices[d];

                                if (tris == null || verts == null) continue; 

                                if (MeshDataTools.GetClosestContainingTriangle(verts, tris, localVertex, out var index0, out var index1, out var index2, out var weight0, out var weight1, out var weight2, out var dist, 0.5f, 0.2f))
                                {
                                    if (dist < closestDistance)
                                    {
                                        closestDistance = dist;
                                        closestDependency = d;
                                        closestIndex0 = index0;
                                        closestIndex1 = index1;
                                        closestIndex2 = index2;
                                        closestWeight0 = weight0;
                                        closestWeight1 = weight1;
                                        closestWeight2 = weight2;
                                    }
                                }
                            }

                            meshIslandTriBindings[c] = new ContainingTri(closestDependency, closestIndex0, closestIndex1, closestIndex2, closestWeight0, closestWeight1, closestWeight2);
                        }
                    }

                    for (int c = 0; c < localVertices.Length; c++)
                    {
                        var localVertex = localVertices[c];
                        var centerPoint = localVertex;

                        Vector3 originOffset = localVertex - centerPoint;
                        float originOffsetDist = originOffset.magnitude;

                        bool hasOrigin = false;
                        if (originOffsetDist > 0.0001f) 
                        { 
                            originOffset = originOffset / originOffsetDist;
                            hasOrigin = true;
                        }

                        int meshIslandIndex = meshIslandBindings == null ? -1 : meshIslandBindings[c];
                        var meshIsland = meshIslandIndex >= 0 ? meshIslands[meshIslandIndex] : default;
                        var meshIslandTri = meshIslandIndex >= 0 ? meshIslandTriBindings[meshIslandIndex] : default;
                        if (meshIslandIndex >= 0) centerPoint = localVertices[meshIsland.OriginVertex];
                         
                        float closestDistance = float.MaxValue;
                        int closestDependency = -1;
                        int closestIndex0 = -1;
                        int closestIndex1 = -1;
                        int closestIndex2 = -1;
                        float closestWeight0 = 0;
                        float closestWeight1 = 0;
                        float closestWeight2 = 0;

                        if (meshIslandIndex < 0)
                        {
                            for (int d = 0; d < characterMeshDependencies.Length; d++)
                            {
                                var tris = characterMeshTriangles[d]; 
                                var verts = characterMeshVertices[d];

                                if (tris == null || verts == null) continue;

                                if (MeshDataTools.GetClosestContainingTriangle(verts, tris, localVertex, out var index0, out var index1, out var index2, out var weight0, out var weight1, out var weight2, out var dist, 0.5f, 0.2f))
                                {
                                    if (dist < closestDistance)
                                    {
                                        closestDistance = dist;
                                        closestDependency = d;
                                        closestIndex0 = index0;
                                        closestIndex1 = index1;
                                        closestIndex2 = index2;
                                        closestWeight0 = weight0;
                                        closestWeight1 = weight1;
                                        closestWeight2 = weight2;

                                        /*Debug.DrawLine(verts[closestIndex0], verts[closestIndex1], Color.green, 120);
                                        Debug.DrawLine(verts[closestIndex1], verts[closestIndex2], Color.green, 120);
                                        Debug.DrawLine(verts[closestIndex2], verts[closestIndex0], Color.green, 120);

                                        Debug.DrawLine(localVertex, (verts[closestIndex0] + verts[closestIndex1] + verts[closestIndex2]) / 3.0f, Color.red, 400);*/
                                    }
                                }
                            }
                        } 
                        else
                        {
                            closestDependency = meshIslandTri.closestDependency;
                            closestIndex0 = meshIslandTri.indexA;
                            closestIndex1 = meshIslandTri.indexB;
                            closestIndex2 = meshIslandTri.indexC;
                            closestWeight0 = meshIslandTri.weightA;
                            closestWeight1 = meshIslandTri.weightB;
                            closestWeight2 = meshIslandTri.weightC;
                        }

                        if (setupMask != null)
                        {
                            float maskWeight = setupMask[c];

                            closestWeight0 = closestWeight0 * maskWeight;
                            closestWeight1 = closestWeight1 * maskWeight;
                            closestWeight2 = closestWeight2 * maskWeight;
                        }

                        if (closestDependency >= 0)
                        {
                            var uvs = characterMeshUVs[closestDependency];
                            var normals = characterMeshNormals[closestDependency];
                            var dep = characterMeshDependencies[closestDependency];
                            var charData = dep.CharacterMeshData;

                            var dependencyNormal = ((normals[closestIndex0] * closestWeight0) + (normals[closestIndex1] * closestWeight1) + (normals[closestIndex2] * closestWeight2)).normalized;

                            Vector3 AddNormalBasedRotationToDelta(Vector3 deltaVertex, Vector3 deltaNormal)
                            {
                                if (hasOrigin)
                                {
                                    Quaternion rotOffset = Quaternion.FromToRotation(dependencyNormal, (dependencyNormal + deltaNormal).normalized);
                                    deltaVertex = deltaVertex + ((centerPoint + (rotOffset * originOffset) * originOffsetDist) - localVertex);
                                }

                                return deltaVertex;
                            }

                            float midlineWeight = 1;
                            float maskLeft = 1;
                            float maskRight = 1;
                            if (charData.midlineVertexGroupIndex >= 0)
                            {
                                var midlineGroup = charData.GetVertexGroup(charData.midlineVertexGroupIndex);
                                if (midlineGroup != null)
                                {
                                    midlineWeight = 0;
                                    midlineWeight += midlineGroup.GetWeight(closestIndex0) * closestWeight0;
                                    midlineWeight += midlineGroup.GetWeight(closestIndex1) * closestWeight1;
                                    midlineWeight += midlineGroup.GetWeight(closestIndex2) * closestWeight2;
                                }
                            }

                            var mainUV = uvs[closestIndex0] * closestWeight0 + uvs[closestIndex1] * closestWeight1 + uvs[closestIndex2] * closestWeight2;
                            if (mainUV.x > 0.5)
                            {
                                maskLeft = 1;
                                maskRight = midlineWeight;
                            }
                            else if (mainUV.x < 0.5)
                            {
                                maskLeft = midlineWeight;
                                maskRight = 1;
                            }
                            float maskTotal = maskLeft + maskRight;
                            maskLeft = maskLeft / maskTotal;
                            maskRight = maskRight / maskTotal;

                            for (int d = 0; d < charData.StandaloneShapesCount; d++)
                            {
                                var shape = charData.GetStandaloneShape(d);
                                if (shape != null)
                                {
                                    var blendShape = AddOrGetBlendShape(shape.name, null);
                                    if (blendShape != null)
                                    {
                                        var frame = blendShape.frames[0];

                                        var deltaVertex = (shape.DeltaVertices[closestIndex0] * closestWeight0) + (shape.DeltaVertices[closestIndex1] * closestWeight1) + (shape.DeltaVertices[closestIndex2] * closestWeight2);
                                        var deltaNormal = (shape.DeltaNormals[closestIndex0] * closestWeight0) + (shape.DeltaNormals[closestIndex1] * closestWeight1) + (shape.DeltaNormals[closestIndex2] * closestWeight2);
                                        var deltaTangent = (shape.DeltaTangents[closestIndex0] * closestWeight0) + (shape.DeltaTangents[closestIndex1] * closestWeight1) + (shape.DeltaTangents[closestIndex2] * closestWeight2);

                                        deltaVertex = AddNormalBasedRotationToDelta(deltaVertex, deltaNormal);

                                        frame.deltaVertices[c] = deltaVertex;
                                        frame.deltaNormals[c] = deltaNormal;
                                        frame.deltaTangents[c] = deltaTangent;
                                    }
                                }
                            }

                            for (int d = 0; d < charData.MuscleVertexGroupCount; d++)
                            {
                                var group = charData.GetMuscleVertexGroup(d);
                                if (group != null)
                                {
                                    float groupWeight = 0;
                                    groupWeight += group.GetWeight(closestIndex0) * closestWeight0;
                                    groupWeight += group.GetWeight(closestIndex1) * closestWeight1;
                                    groupWeight += group.GetWeight(closestIndex2) * closestWeight2;

                                    for (int e = 0; e < charData.MuscleShapesCount; e++)
                                    {
                                        var shape = charData.GetMuscleShape(e);
                                        if (shape != null)
                                        {
                                            var shapeNameL = ICustomizableCharacter.Defaults.GetMuscleMassShapeSyncNameLeft(d, e);
                                            var shapeNameR = ICustomizableCharacter.Defaults.GetMuscleMassShapeSyncNameRight(d, e);

                                            var blendShapeL = AddOrGetBlendShape(shapeNameL, null);
                                            if (blendShapeL != null)
                                            {
                                                var frame = blendShapeL.frames[0];

                                                float weightLeft = groupWeight * maskLeft;
                                                var deltaVertex = ((shape.DeltaVertices[closestIndex0] * closestWeight0) + (shape.DeltaVertices[closestIndex1] * closestWeight1) + (shape.DeltaVertices[closestIndex2] * closestWeight2)) * weightLeft;
                                                var deltaNormal = ((shape.DeltaNormals[closestIndex0] * closestWeight0) + (shape.DeltaNormals[closestIndex1] * closestWeight1) + (shape.DeltaNormals[closestIndex2] * closestWeight2)) * weightLeft;
                                                var deltaTangent = ((shape.DeltaTangents[closestIndex0] * closestWeight0) + (shape.DeltaTangents[closestIndex1] * closestWeight1) + (shape.DeltaTangents[closestIndex2] * closestWeight2)) * weightLeft;

                                                deltaVertex = AddNormalBasedRotationToDelta(deltaVertex, deltaNormal);

                                                frame.deltaVertices[c] = deltaVertex;
                                                frame.deltaNormals[c] = deltaNormal;
                                                frame.deltaTangents[c] = deltaTangent;
                                            }

                                            var blendShapeR = AddOrGetBlendShape(shapeNameR, null);
                                            if (blendShapeR != null)
                                            {
                                                var frame = blendShapeR.frames[0];

                                                float weightRight = groupWeight * maskRight;
                                                var deltaVertex = ((shape.DeltaVertices[closestIndex0] * closestWeight0) + (shape.DeltaVertices[closestIndex1] * closestWeight1) + (shape.DeltaVertices[closestIndex2] * closestWeight2)) * weightRight;
                                                var deltaNormal = ((shape.DeltaNormals[closestIndex0] * closestWeight0) + (shape.DeltaNormals[closestIndex1] * closestWeight1) + (shape.DeltaNormals[closestIndex2] * closestWeight2)) * weightRight;
                                                var deltaTangent = ((shape.DeltaTangents[closestIndex0] * closestWeight0) + (shape.DeltaTangents[closestIndex1] * closestWeight1) + (shape.DeltaTangents[closestIndex2] * closestWeight2)) * weightRight;

                                                deltaVertex = AddNormalBasedRotationToDelta(deltaVertex, deltaNormal);

                                                frame.deltaVertices[c] = deltaVertex;
                                                frame.deltaNormals[c] = deltaNormal;
                                                frame.deltaTangents[c] = deltaTangent;
                                            }
                                        }
                                    }
                                }
                            }

                            for (int d = 0; d < charData.FatVertexGroupCount; d++)
                            {
                                var group = charData.GetFatVertexGroup(d);
                                if (group != null)
                                {
                                    float groupWeight = 0;
                                    groupWeight += group.GetWeight(closestIndex0) * closestWeight0;
                                    groupWeight += group.GetWeight(closestIndex1) * closestWeight1;
                                    groupWeight += group.GetWeight(closestIndex2) * closestWeight2;

                                    for (int e = 0; e < charData.FatShapesCount; e++)
                                    {
                                        var shape = charData.GetFatShape(e);
                                        if (shape != null)
                                        {
                                            var shapeName = ICustomizableCharacter.Defaults.GetFatShapeSyncName(d, e);

                                            var blendShape = AddOrGetBlendShape(shapeName, null);
                                            if (blendShape != null)
                                            {
                                                var frame = blendShape.frames[0];

                                                var deltaVertex = ((shape.DeltaVertices[closestIndex0] * closestWeight0) + (shape.DeltaVertices[closestIndex1] * closestWeight1) + (shape.DeltaVertices[closestIndex2] * closestWeight2)) * groupWeight;
                                                var deltaNormal = ((shape.DeltaNormals[closestIndex0] * closestWeight0) + (shape.DeltaNormals[closestIndex1] * closestWeight1) + (shape.DeltaNormals[closestIndex2] * closestWeight2)) * groupWeight;
                                                var deltaTangent = ((shape.DeltaTangents[closestIndex0] * closestWeight0) + (shape.DeltaTangents[closestIndex1] * closestWeight1) + (shape.DeltaTangents[closestIndex2] * closestWeight2)) * groupWeight;

                                                deltaVertex = AddNormalBasedRotationToDelta(deltaVertex, deltaNormal);

                                                frame.deltaVertices[c] = deltaVertex;
                                                frame.deltaNormals[c] = deltaNormal;
                                                frame.deltaTangents[c] = deltaTangent;
                                            }
                                        }
                                    }
                                }
                            }

                            for (int d = 0; d < charData.VariationVertexGroupCount; d++)
                            {
                                var group = charData.GetVariationVertexGroup(d);
                                if (group != null)
                                {
                                    float groupWeight = 0;
                                    groupWeight += group.GetWeight(closestIndex0) * closestWeight0;
                                    groupWeight += group.GetWeight(closestIndex1) * closestWeight1;
                                    groupWeight += group.GetWeight(closestIndex2) * closestWeight2;

                                    for (int e = 0; e < charData.VariationShapesCount; e++)
                                    {
                                        var shape = charData.GetVariationShape(e);
                                        if (shape != null)
                                        {
                                            var shapeNameL = ICustomizableCharacter.Defaults.GetVariationShapeSyncNameLeft(d, e);
                                            var shapeNameR = ICustomizableCharacter.Defaults.GetVariationShapeSyncNameRight(d, e);

                                            var blendShapeL = AddOrGetBlendShape(shapeNameL, null);
                                            if (blendShapeL != null)
                                            {
                                                var frame = blendShapeL.frames[0];

                                                float weightLeft = groupWeight * maskLeft;
                                                var deltaVertex = ((shape.DeltaVertices[closestIndex0] * closestWeight0) + (shape.DeltaVertices[closestIndex1] * closestWeight1) + (shape.DeltaVertices[closestIndex2] * closestWeight2)) * weightLeft;
                                                var deltaNormal = ((shape.DeltaNormals[closestIndex0] * closestWeight0) + (shape.DeltaNormals[closestIndex1] * closestWeight1) + (shape.DeltaNormals[closestIndex2] * closestWeight2)) * weightLeft;
                                                var deltaTangent = ((shape.DeltaTangents[closestIndex0] * closestWeight0) + (shape.DeltaTangents[closestIndex1] * closestWeight1) + (shape.DeltaTangents[closestIndex2] * closestWeight2)) * weightLeft;

                                                deltaVertex = AddNormalBasedRotationToDelta(deltaVertex, deltaNormal);

                                                frame.deltaVertices[c] = deltaVertex;
                                                frame.deltaNormals[c] = deltaNormal;
                                                frame.deltaTangents[c] = deltaTangent;
                                            }

                                            var blendShapeR = AddOrGetBlendShape(shapeNameR, null);
                                            if (blendShapeR != null)
                                            {
                                                var frame = blendShapeR.frames[0];

                                                float weightRight = groupWeight * maskRight;
                                                var deltaVertex = ((shape.DeltaVertices[closestIndex0] * closestWeight0) + (shape.DeltaVertices[closestIndex1] * closestWeight1) + (shape.DeltaVertices[closestIndex2] * closestWeight2)) * weightRight;
                                                var deltaNormal = ((shape.DeltaNormals[closestIndex0] * closestWeight0) + (shape.DeltaNormals[closestIndex1] * closestWeight1) + (shape.DeltaNormals[closestIndex2] * closestWeight2)) * weightRight;
                                                var deltaTangent = ((shape.DeltaTangents[closestIndex0] * closestWeight0) + (shape.DeltaTangents[closestIndex1] * closestWeight1) + (shape.DeltaTangents[closestIndex2] * closestWeight2)) * weightRight;

                                                deltaVertex = AddNormalBasedRotationToDelta(deltaVertex, deltaNormal);

                                                frame.deltaVertices[c] = deltaVertex;
                                                frame.deltaNormals[c] = deltaNormal;
                                                frame.deltaTangents[c] = deltaTangent; 
                                            }
                                        }
                                    }
                                }
                            }

                            if (target.transferBoneWeights)
                            {
                                var boneWeights = characterMeshBoneWeights[closestDependency];
                                var bonesPerVertex = characterMeshBonesPerVertex[closestDependency];
                                var boneWeightStartIndices = characterMeshBoneWeightStartIndices[closestDependency];

                                if (boneWeights.IsCreated && bonesPerVertex.IsCreated && boneWeightStartIndices != null)
                                {
                                    int startIndex = boneWeightStartIndices[closestIndex0];
                                    int count = bonesPerVertex[closestIndex0];
                                    for (int d = 0; d < count; d++)
                                    {
                                        int index = startIndex + d;
                                        var bw = boneWeights[index];

                                        int2 i = new int2(c, bw.boneIndex);

                                        newBoneWeights.TryGetValue(i, out float currentWeight);
                                        newBoneWeights[i] = currentWeight + (bw.weight * closestWeight0);
                                    }

                                    startIndex = boneWeightStartIndices[closestIndex1];
                                    count = bonesPerVertex[closestIndex1];
                                    for (int d = 0; d < count; d++)
                                    {
                                        int index = startIndex + d;
                                        var bw = boneWeights[index];

                                        int2 i = new int2(c, bw.boneIndex);

                                        newBoneWeights.TryGetValue(i, out float currentWeight);
                                        newBoneWeights[i] = currentWeight + (bw.weight * closestWeight1);
                                    }

                                    startIndex = boneWeightStartIndices[closestIndex2];
                                    count = bonesPerVertex[closestIndex2];
                                    for (int d = 0; d < count; d++)
                                    {
                                        int index = startIndex + d;
                                        var bw = boneWeights[index];

                                        int2 i = new int2(c, bw.boneIndex);

                                        newBoneWeights.TryGetValue(i, out float currentWeight);
                                        newBoneWeights[i] = currentWeight + (bw.weight * closestWeight2);
                                    }
                                }
                            }
                        }
                    }

                    float emptyVertexThreshold = target.emptyShapeVertexThreshold;
                    float emptyNormalThreshold = target.emptyShapeNormalThreshold;
                    float emptyTangentThreshold = target.emptyShapeTangentThreshold;

                    if (emptyVertexThreshold <= 0) emptyVertexThreshold = 0.001f;
                    if (emptyNormalThreshold <= 0) emptyNormalThreshold = 0.005f;
                    if (emptyTangentThreshold <= 0) emptyTangentThreshold = 0.005f; 

                    var outputMesh = MeshUtils.DuplicateMesh(mesh);
                    outputMesh.name = mesh.name + "_" + target.name;

                    outputMesh.ClearBlendShapes();
                    foreach (var shape in blendShapes)
                    {
                        if (shape.frames == null || shape.frames.Length <= 0) continue;

                        bool isEmpty = true;
                        foreach (var frame in shape.frames)
                        {
                            for (int v = 0; v < mesh.vertexCount; v++)
                            {
                                if (frame.deltaVertices[v].magnitude > emptyVertexThreshold || frame.deltaNormals[v].magnitude > emptyNormalThreshold || frame.deltaTangents[v].magnitude > emptyTangentThreshold)
                                {
                                    isEmpty = false;
                                    break;
                                }
                            }

                            if (!isEmpty) break;
                            
                        }

                        if (isEmpty) 
                        {
                            Debug.LogWarning($"Skipped adding empty shape '{shape.name}' to mesh '{outputMesh.name}'"); 
                            continue;
                        }

                        shape.AddToMesh(outputMesh);
                    }
                    
                    if (target.transferBoneWeights && newBoneWeights.Count > 0)
                    {
                        var boneWeights = new NativeList<BoneWeight1>(outputMesh.vertexCount, Allocator.Persistent);
                        var bonesPerVertex = new NativeArray<byte>(outputMesh.vertexCount, Allocator.Persistent);

                        List<BoneWeight1> vertexBoneWeights = new List<BoneWeight1>();
                        for (int c = 0; c < outputMesh.vertexCount; c++)
                        {
                            vertexBoneWeights.Clear();
                            foreach (var pair in newBoneWeights)
                            {
                                if (pair.Key.x == c)
                                {
                                    var bw = new BoneWeight1();
                                    bw.boneIndex = pair.Key.y;
                                    bw.weight = pair.Value;

                                    vertexBoneWeights.Add(bw);
                                }
                            }
                            vertexBoneWeights.Sort((BoneWeight1 a, BoneWeight1 b) => (int)Mathf.Sign(b.weight - a.weight));
                            if (vertexBoneWeights.Count > 8) vertexBoneWeights.RemoveRange(8, vertexBoneWeights.Count - 8);
                             
                            float totalWeight = 0;
                            foreach(var bw in vertexBoneWeights) totalWeight += bw.weight;
                            if (totalWeight > 0)
                            {
                                for (int d = 0; d < vertexBoneWeights.Count; d++)
                                {
                                    var bw = vertexBoneWeights[d];
                                    bw.weight /= totalWeight;
                                    vertexBoneWeights[d] = bw;
                                }
                            }

                            for (int d = 0; d < vertexBoneWeights.Count; d++)
                            {
                                var bw = vertexBoneWeights[d];
                                boneWeights.Add(bw); 
                            }
                            bonesPerVertex[c] = (byte)vertexBoneWeights.Count;
                        }

                        outputMesh.SetBoneWeights(bonesPerVertex, boneWeights.AsArray());

                        boneWeights.Dispose();
                        bonesPerVertex.Dispose();
                    }

                    if (target.solidifyThickness > 0.00001f)
                    {
                        var solidifiedMesh = MeshDataTools.Solidify(outputMesh, target.solidifyThickness, 0f, true, true);
                        solidifiedMesh.name = outputMesh.name;
                        outputMesh = solidifiedMesh;
                        Debug.Log($"{target.name} : {target.solidifyThickness} : {solidifiedMesh.name} :: {solidifiedMesh.GetType().FullName}");
                    }
                    meshesToEvaluate[b] = outputMesh;
                    outputMeshes.Add(outputMesh);
                }

                GameObject secondaryObj = new GameObject(target.name);
                secondaryObj.transform.SetParent(outputPrefab.transform, false);
                secondaryObj.transform.SetLocalPositionAndRotation(target.gameObjectOffset, Quaternion.Euler(target.gameObjectRotationOffset));

                Transform rootBone = null;
                Transform[] bones = null;
                if (target.skinningReference != null)
                {
                    rootBone = target.skinningReference.rootBone;
                    bones = target.skinningReference.bones;

                    var root = rootBone == null ? outputPrefab.transform : outputPrefab.transform.FindDeepChild(rootBone.name);
                    if (root == null) root = outputPrefab.transform;

                    if (bones != null)
                    {
                        for (int b = 0; b < bones.Length; b++)
                        {
                            if (bones[b] != null)
                            {
                                bones[b] = root.FindDeepChild(bones[b].name);
                            }
                        }
                    }
                }
                else
                {
                    CustomAvatar avatar = null;

                    foreach (var dep in characterMeshDependencies)
                    {
                        if (dep != null && dep.Avatar != null)
                        {
                            avatar = dep.Avatar;
                            break;
                        }
                    }

                    var root = string.IsNullOrWhiteSpace(avatar.rigContainer) ? outputPrefab.transform : outputPrefab.transform.FindDeepChildLiberal(avatar.rigContainer);
                    if (root == null) root = outputPrefab.transform;

                    bones = new Transform[avatar.bones.Length];
                    for (int b = 0; b < bones.Length; b++)
                    {                      
                        bones[b] = root.FindDeepChildLiberal(avatar.bones[b]);
                    }
                }

                if (meshesToEvaluate.Count == 1)
                {
                    var renderer = secondaryObj.AddComponent<SkinnedMeshRenderer>();

                    Matrix4x4[] bindpose = meshesToEvaluate[0].bindposes;
                    if (bindpose == null || bindpose.Length <= 0 || target.transferBoneWeights)
                    {
                        foreach (var dep in characterMeshDependencies)
                        {
                            if (dep != null && dep.MeshData.Mesh != null)
                            {
                                bindpose = dep.MeshData.Mesh.bindposes;
                                if (bindpose != null && bindpose.Length > 0) break;
                            }
                        }
                    }

                    if (bindpose == null || bindpose.Length != bones.Length)
                    {
                        var newBP = new Matrix4x4[bones.Length];
                        for (int b = 0; b < newBP.Length; b++)
                        {
                            newBP[b] = (bindpose != null && b < bindpose.Length) ? bindpose[b] : bones[b].localToWorldMatrix.inverse;
                        }

                        meshesToEvaluate[0].bindposes = newBP;
                    }

                    renderer.rootBone = null;
                    renderer.bones = null;

                    renderer.sharedMesh = meshesToEvaluate[0];
                    renderer.sharedMaterials = target.materials;

                    renderer.bones = bones; 
                    renderer.rootBone = rootBone;

                    var boundsCenter = target.boundsCenter;
                    var boundsExtents = target.boundsExtents;
                    renderer.localBounds = new Bounds() { center = boundsCenter, extents = boundsExtents };

                    foreach (var dep in characterMeshDependencies)
                    {
                        dep.SyncSkinnedMesh(renderer, false);
                    }
                }
                else if (meshesToEvaluate.Count > 1)
                {
                    List<SkinnedMeshRenderer> renderers = new List<SkinnedMeshRenderer>();

                    for (int b = 0; b < meshesToEvaluate.Count; b++)
                    {
                        var mesh = meshesToEvaluate[b];

                        GameObject childObj = new GameObject($"lod_{b}");
                        childObj.transform.SetParent(secondaryObj.transform, false);

                        var renderer = childObj.AddComponent<SkinnedMeshRenderer>();

                        Matrix4x4[] bindpose = mesh.bindposes;
                        if (bindpose == null || bindpose.Length <= 0 || target.transferBoneWeights)
                        {
                            foreach (var dep in characterMeshDependencies)
                            {
                                if (dep != null && dep.MeshData.Mesh != null)
                                {
                                    bindpose = dep.MeshData.Mesh.bindposes;
                                    if (bindpose != null && bindpose.Length > 0) break;
                                }
                            }
                        }

                        if (bindpose == null || bindpose.Length != bones.Length)
                        {
                            var newBP = new Matrix4x4[bones.Length];
                            for (int c = 0; c < newBP.Length; c++)
                            {
                                newBP[c] = (bindpose != null && c < bindpose.Length) ? bindpose[c] : bones[c].localToWorldMatrix.inverse;
                            }

                            mesh.bindposes = newBP; 
                        }

                        renderer.rootBone = null;
                        renderer.bones = null; 

                        renderer.sharedMesh = mesh;
                        renderer.sharedMaterials = target.materials;

                        renderer.bones = bones;
                        renderer.rootBone = rootBone;

                        var boundsCenter =target.boundsCenter;
                        var boundsExtents = target.boundsExtents;
                        renderer.localBounds = new Bounds() { center = boundsCenter, extents = boundsExtents };

                        renderers.Add(renderer);

                        foreach (var dep in characterMeshDependencies)
                        {
                            dep.SyncSkinnedMesh(renderer, false);
                        }
                    }

                    var lodGroup = secondaryObj.AddComponent<LODGroup>();
                    var lods = new LOD[renderers.Count];
                    for (int b = 0; b < renderers.Count; b++)
                    {
                        float transitionHeight = 0;
                        if (b < target.lods.Length)
                        {
                            var lodInfo = target.lods[b];
                            transitionHeight = lodInfo.screenRelativeTransitionHeight;
                        }
                        else
                        {
                            transitionHeight = Mathf.Lerp(target.lods[target.lods.Length - 1].screenRelativeTransitionHeight, 0, ((b - target.lods.Length) + 1) / ((float)renderers.Count - target.lods.Length));
                        }

                        var lod = new LOD(transitionHeight, new Renderer[] { renderers[b] });
                        lods[b] = lod;
                    }

                    lodGroup.SetLODs(lods);
                }

                foreach (var array in characterMeshBoneWeights)
                {
                    if (array.IsCreated) array.Dispose();
                }
                foreach (var array in characterMeshBonesPerVertex)
                {
                    if (array.IsCreated) array.Dispose();
                }
            }
            
            var copyTransforms = outputPrefab.GetComponentsInChildren<CopyTransformState>(true);
            foreach (var copyTransform in copyTransforms) 
            {
                if (copyTransform.destroyOnAwake)
                {
                    copyTransform.Apply();
                    DestroyImmediate(copyTransform); 
                }
            }

            Transform prefabRoot = outputPrefab.transform;

            BoneReplacement[] instantiatedBoneReplacements = new BoneReplacement[boneReplacements == null ? 0 : boneReplacements.Length];
            if (boneReplacements != null)
            {
                for (int b = 0; b < boneReplacements.Length; b++)
                {
                    var replacement = boneReplacements[b];

                    if (replacement.boneToReplace == null || replacement.replacementBone == null) continue;

                    string boneToReplacePath = replacement.boneToReplace.GetPathString(true);
                    Transform instantiatedBoneToReplace = prefabRoot.GetTransformByPath(boneToReplacePath);

                    string replacementBonePath = replacement.replacementBone.GetPathString(true);
                    Transform instantiatedReplacementBone = prefabRoot.GetTransformByPath(replacementBonePath); 

                    replacement.boneToReplace = instantiatedBoneToReplace;
                    replacement.replacementBone = instantiatedReplacementBone;

                    instantiatedBoneReplacements[b] = replacement;
                }
            }
            Transform GetReplacementBone(Transform originalBone)
            {
                foreach (var bone in instantiatedBoneReplacements) if (bone.boneToReplace == originalBone) return bone.replacementBone;
                return originalBone;
            }

            if (skinnedRendererRigsToEdit != null)
            {
                while(prefabRoot.parent != null) prefabRoot = prefabRoot.parent;

                foreach (var renderer in skinnedRendererRigsToEdit)
                {
                    if (renderer == null) continue;

                    string path = renderer.transform.GetPathString(true);  
                    Transform instantiatedTransform = prefabRoot.GetTransformByPath(path);

                    if (instantiatedTransform != null)
                    {
                        var instantiatedRenderer = instantiatedTransform.GetComponent<SkinnedMeshRenderer>();

                        if (instantiatedRenderer != null)
                        {
                            var bones = instantiatedRenderer.bones;
                            var rootBone = instantiatedRenderer.rootBone;

                            if (instantiatedBoneReplacements.Length > 0)
                            {
                                if (rootBone != null) rootBone = GetReplacementBone(rootBone);
                                if (bones != null)
                                {
                                    for (int b = 0; b < bones.Length; b++)
                                    {
                                        var bone = bones[b];
                                        if (bone != null)
                                        {
                                            bones[b] = GetReplacementBone(bone); 
                                        }
                                    }
                                }

                                instantiatedRenderer.bones = bones;
                                instantiatedRenderer.rootBone = rootBone;
                            }
                        }
                    }
                }
            }

            if (ikBonesToFloat != null)
            {
                Transform ikParent = prefabRoot;
                if (ikBonesParent != null)
                {
                    string path = ikBonesParent.GetPathString(true);
                    ikParent = prefabRoot.GetTransformByPath(path);
                }

                foreach (var ikBone in ikBonesToFloat)
                {
                    if (ikBone == null) continue;

                    string path = ikBone.GetPathString(true);
                    Transform instantiatedTransform = prefabRoot.GetTransformByPath(path);

                    if (instantiatedTransform != null)
                    {
                        instantiatedTransform.SetParent(ikParent, true); 
                    }
                }
            }

            if (instantiatedBoneReplacements.Length > 0)
            {
                List<Transform> tempChildren = new List<Transform>();
                foreach (var replacement in instantiatedBoneReplacements)
                {
                    if (replacement.boneToReplace == null || replacement.replacementBone == null) continue;

                    tempChildren.Clear();
                    for(int b = 0; b < replacement.boneToReplace.childCount; b++)
                    {
                        var child = replacement.boneToReplace.GetChild(b);
                        if (child != null) tempChildren.Add(child); 
                    }

                    foreach (var child in tempChildren)
                    {
                        if (child == null) continue;
                        child.SetParent(replacement.replacementBone, true); 
                    }
                    
                    if (replacement.destroyReplacedBone) GameObject.DestroyImmediate(replacement.boneToReplace.gameObject);
                }
            }

            if (animator != null && !animator.HasPreInitializedBindPose) animator.ReinitializeBindPose(); 

#if UNITY_EDITOR
            foreach (var mesh in outputMeshes)
            {
                string meshSavePath = Path.Combine(assetSavePath, mesh.name + ".asset");

                if (!AssetDatabase.IsNativeAsset(mesh))
                {
                    AssetDatabase.DeleteAsset(meshSavePath);
                    AssetDatabase.CreateAsset(mesh, meshSavePath);

                    Debug.Log("Saved mesh " + mesh.name);
                }
                else
                {
                    AssetDatabase.SaveAssetIfDirty(mesh);

                    Debug.Log("Saved native mesh " + mesh.name);
                }
            }

            foreach (var outputData in outputDatas)
            {
                string savePath = Path.Combine(dataSavePath, outputData.name + ".asset");

                if (!AssetDatabase.IsNativeAsset(outputData))
                {
                    AssetDatabase.DeleteAsset(savePath);
                    AssetDatabase.CreateAsset(outputData, savePath);
                }
                else
                {
                    AssetDatabase.SaveAssetIfDirty(outputData);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(outputPrefab, Path.Combine(prefabSavePath, outputPrefab.name + ".prefab"), out bool success);
            if (!success) Debug.LogError($"Failed to save prefab '{outputPrefab.name}'!");
#endif
        }
    }

}