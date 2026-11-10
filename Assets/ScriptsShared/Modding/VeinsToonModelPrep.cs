using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

using Swole;
using Swole.API.Unity;

using Swole.DataStructures;
using static VeinsRiggedSharedData;
using static Swole.API.Unity.MuscularCharacterSharedData;
using static MuscleToonModelPrep;

using Unity.Collections;
using static VeinsRigged;
using static Swole.API.Unity.CharacterSurface;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class VeinsToonModelPrep : MonoBehaviour
{

    [Header("Control")]
    public bool apply;

    public bool applyOnAwake = true;

    [Header("Optimization")]
    public bool optimizeInputMesh = true;
    public int opt_maxEdgeLoopLength = 8;
    public int opt_minEdgeLoopLength = 4;
    public float opt_maxEdgeToBoneDirDot = 0.6f;
    public float opt_minInclusionWeight = 0.15f;
    public float opt_junctionBoneDistanceThreshold = 0.001f;
    public float opt_minFaceNormalToBoneDirDot = 0.68f;
    public Vector3 opt_boneRollAxis = Vector3.up;
    public List<string> opt_edgeAffectingShapes;

    [Header("Options")]
    public bool useCustomSkinning = false;
    public bool copyNormalsFromSurface = true;
    public float copyNormalsFromSurfaceFactor = 0.9f;
    public float shapeDeltaNormalNullificationFactor = 1;

    public bool flipIndexUV_V = false; 

    public bool flipUvSide = true;

    public TextureFormat outputFormat = TextureFormat.RGBA32;

    public string varicoseShapeName = "varicose";
    public string shrinkShapeName = "shrink";

    public string muscularCharacterSharedDataPath = "Assets/Baked/";
    public string muscularCharacterSharedDataFileName = "sharedMuscleMeshData";

    public string veinMuscleShapeTextureName = "muscleShapes";

    public GameObject rootGameobject;

    public MuscularRenderedCharacter character;

    public BlendShapeWeight[] surfaceBlendShapeStates;

    public Material[] muscleMaterials;

    public Material muscleOutlineMaterial;

    public int muscleMapMidpointSpacingPixels = 10;
    public MuscleMapPacker[] muscleMapPacking;

    [Serializable]
    public class TargetRenderer
    {

        public bool addOutlineMaterial = true;
        public Material outlineMaterialOverride;

        public int maxBonesPerVertex = 8;
        public SkinnedMeshRenderer renderer;

        public string textureOutputPath = "";

        public UVChannelURP defaultUVChannel = UVChannelURP.UV1;

        public bool storeRecalculatedNormals = true;

        public UVChannelURP storeNormalsUVChannel = UVChannelURP.UV3;

        public bool mergeIndexUVs = true;
        public bool packIndexUVMergeDataIntoSingleFloat = false;
        public int uvMergeDataIntoSingleFloatChannelComponentIndex = 3;

        public UVChannelURP indexUVReferenceChannel = UVChannelURP.UV0;
        public UVChannelURP indexUVMergedTargetChannel = UVChannelURP.UV3;

        public bool storeUVMergeInZW = false;

        public float maxShapeVertexTranslationDistance = 2;

        //public BlendshapesToTextures.DeltaShape[] deltaShapes;

        //public string[] muscleShapes;

        //public MuscleToonModelPrep.MuscleShapesIdentifier muscleShapesIdentifier;

        public MuscleToonModelPrep.MuscleGroup[] muscleGroups;

    }

    public List<TargetRenderer> targetRenderers;

    [HideInInspector]
    public List<CustomSkinnedMeshRenderer> outputCustomRenderers;

    protected void Awake()
    {

        if (Application.isPlaying && applyOnAwake)
        {

            Apply();

        }

    }

    protected void Update()
    {

        if (apply)
        {

            apply = false;

            Apply();

        }

    }

    public void Apply()
    {

        if (targetRenderers == null) return;

        outputCustomRenderers = new List<CustomSkinnedMeshRenderer>();

        for (int a = 0; a < targetRenderers.Count; a++)
        {

            TargetRenderer tr = targetRenderers[a];

            Apply(out CustomSkinnedMeshRenderer customRenderer, tr.renderer, varicoseShapeName, shrinkShapeName, tr.maxBonesPerVertex, character,
                copyNormalsFromSurface, copyNormalsFromSurfaceFactor, shapeDeltaNormalNullificationFactor,
                muscleMapMidpointSpacingPixels,
                flipUvSide, muscleMapPacking, veinMuscleShapeTextureName, surfaceBlendShapeStates, tr.textureOutputPath, tr.defaultUVChannel, tr.storeNormalsUVChannel, tr.mergeIndexUVs, tr.packIndexUVMergeDataIntoSingleFloat, tr.uvMergeDataIntoSingleFloatChannelComponentIndex, tr.indexUVReferenceChannel, tr.indexUVMergedTargetChannel, tr.storeUVMergeInZW,
                tr.maxShapeVertexTranslationDistance, tr.addOutlineMaterial, tr.outlineMaterialOverride == null ? muscleOutlineMaterial : tr.outlineMaterialOverride, RootTransform.Renderer, null, tr.storeRecalculatedNormals, true, flipIndexUV_V, useCustomSkinning, outputFormat,
                optimizeInputMesh, opt_maxEdgeLoopLength, opt_minEdgeLoopLength, opt_maxEdgeToBoneDirDot, opt_minInclusionWeight, opt_junctionBoneDistanceThreshold, opt_minFaceNormalToBoneDirDot, opt_boneRollAxis, opt_edgeAffectingShapes);

            if (customRenderer != null) outputCustomRenderers.Add(customRenderer);

        }

    }

    protected class TempSurfaceData : IDisposable
    {

        public string name;

        public NativeArray<BoneWeight1> boneWeights;
        public NativeArray<byte> boneCounts;
        public int[] boneWeightIndexLookup;

        public NativeList<CharacterSurface.SamplePoint> samplePoints;
        public NativeList<CharacterSurface.SamplePoint> dynamicSamplePoints;

        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector3[] recalculatedNormals; 

        public BlendShape[] blendShapes;
        public BlendShape[] recalculatedBlendShapes;

        public TempSurfaceData(Mesh mesh, bool mergeNormals = true)
        {

            name = mesh.name;

            vertices = mesh.vertices;
            normals = mesh.normals;

            Mesh temp = MeshUtils.DuplicateMesh(mesh);
            temp.RecalculateNormals();

            recalculatedNormals = temp.normals;

            var clones = MeshUtils.CalculateClones(vertices);

            if (mergeNormals)
            {

                for(int a = 0; a < temp.vertexCount; a++)
                {

                    var clone = clones[a];

                    if (clone.first != a) continue;

                    Vector3 normal = Vector3.zero;

                    for (int b = 0; b < clone.indices.Length; b++) normal = normal + recalculatedNormals[clone.indices[b]];

                    float mag = normal.magnitude;
                    if (mag > 0) normal = normal / mag;

                    for (int b = 0; b < clone.indices.Length; b++) recalculatedNormals[clone.indices[b]] = normal;

                }

            }

            boneWeights = new NativeArray<BoneWeight1>(mesh.GetAllBoneWeights(), Allocator.Persistent);
            boneCounts = new NativeArray<byte>(mesh.GetBonesPerVertex(), Allocator.Persistent);

            samplePoints = new NativeList<CharacterSurface.SamplePoint>(mesh.vertexCount, Allocator.Persistent);
            dynamicSamplePoints = new NativeList<CharacterSurface.SamplePoint>(mesh.vertexCount, Allocator.Persistent);

            for(int a = 0; a < mesh.vertexCount; a++)
            {

                CharacterSurface.SamplePoint samplePoint = new CharacterSurface.SamplePoint();

                samplePoint.index = a;
                samplePoint.localVertex = vertices[a];
                samplePoint.localNormal = normals[a];
                samplePoint.localSurfaceNormal = recalculatedNormals[a];

                samplePoint.localReshapedVertex = samplePoint.localVertex;
                samplePoint.localReshapedNormal = samplePoint.localNormal;
                samplePoint.localReshapedSurfaceNormal = samplePoint.localSurfaceNormal;

                samplePoints.Add(samplePoint);
                dynamicSamplePoints.Add(samplePoint);

            }

            if (boneWeights.IsCreated && boneCounts.IsCreated)
            {

                boneWeightIndexLookup = new int[boneCounts.Length];

                int i = 0;
                for (int a = 0; a < boneCounts.Length; a++)
                {

                    boneWeightIndexLookup[a] = i;
                    i += boneCounts[a];

                }

            }

            blendShapes = mesh.GetBlendShapes().ToArray();

            recalculatedBlendShapes = MeshUtils.GetRecalculatedBlendShapeNormals(mesh, blendShapes, null, recalculatedNormals, false, true, clones, true);

        }

        public bool isDefaultData = true;

        public void ResetDynamicSamples()
        {

            isDefaultData = true;

            for (int a = 0; a < samplePoints.Length; a++) dynamicSamplePoints[a] = samplePoints[a];

        }

        public void UpdateDynamicSamples(BlendShapeWeight[] blendShapeWeights, float multiplier = 1)
        {

            isDefaultData = false;

            for (int a = 0; a < samplePoints.Length; a++)
            {

                CharacterSurface.SamplePoint basePoint = samplePoints[a];
                CharacterSurface.SamplePoint dynamicPoint = basePoint;

                for(int b = 0; b < blendShapeWeights.Length; b++)
                {

                    var weights = blendShapeWeights[b];

                    for(int c = 0; c < blendShapes.Length; c++)
                    {

                        var blendShape = blendShapes[c];
                        if (blendShape.name != weights.blendShapeName) continue;

                        var recalculatedShape = recalculatedBlendShapes[c];

                        dynamicPoint.localReshapedVertex = blendShape.GetTransformedVertex(dynamicPoint.localReshapedVertex, a, weights.blendShapeWeight, multiplier);
                        dynamicPoint.localReshapedNormal = blendShape.GetTransformedNormal(dynamicPoint.localReshapedNormal, a, weights.blendShapeWeight, multiplier);
                        dynamicPoint.localReshapedSurfaceNormal = recalculatedShape.GetTransformedNormal(dynamicPoint.localReshapedSurfaceNormal, a, weights.blendShapeWeight, multiplier);

                        break;

                    }

                }

                dynamicSamplePoints[a] = dynamicPoint;

            }

        }

        public void UpdateDynamicSamples(int shapeIndex, float weight, float multiplier = 1)
        {

            isDefaultData = false;

            var blendShape = blendShapes[shapeIndex];
            var recalculatedShape = recalculatedBlendShapes[shapeIndex];

            for (int a = 0; a < samplePoints.Length; a++)
            {

                CharacterSurface.SamplePoint basePoint = samplePoints[a];
                CharacterSurface.SamplePoint dynamicPoint = basePoint;

                dynamicPoint.localReshapedVertex = blendShape.GetTransformedVertex(basePoint.localVertex, a, weight, multiplier);
                dynamicPoint.localReshapedNormal = blendShape.GetTransformedNormal(basePoint.localNormal, a, weight, multiplier);
                dynamicPoint.localReshapedSurfaceNormal = recalculatedShape.GetTransformedNormal(basePoint.localSurfaceNormal, a, weight, multiplier);

                dynamicSamplePoints[a] = dynamicPoint;

            }

        }

        public void UpdateDynamicSamples(ShapeData shapeData, float multiplier = 1)
        {

            isDefaultData = false;

            for (int a = 0; a < samplePoints.Length; a++)
            {

                CharacterSurface.SamplePoint basePoint = samplePoints[a];
                CharacterSurface.SamplePoint dynamicPoint = basePoint;

                dynamicPoint.localReshapedVertex = shapeData.GetTransformedVertex(basePoint.localVertex, a, multiplier);
                dynamicPoint.localReshapedNormal = shapeData.GetTransformedNormal(basePoint.localNormal, a, multiplier);
                dynamicPoint.localReshapedSurfaceNormal = shapeData.GetTransformedSurfaceNormal(basePoint.localSurfaceNormal, a, multiplier);

                dynamicSamplePoints[a] = dynamicPoint;

            }

        }

        public void Dispose()
        {

            if (boneWeights.IsCreated) boneWeights.Dispose();
            if (boneCounts.IsCreated) boneCounts.Dispose();

            boneWeights = default; 
            boneCounts = default;

            if (samplePoints.IsCreated) samplePoints.Dispose();
            if (dynamicSamplePoints.IsCreated) dynamicSamplePoints.Dispose();

            samplePoints = default;
            dynamicSamplePoints = default;

        }

    }

    [Serializable]
    public struct MuscleMapPacker
    {

        public string mapName;

        public int rChannelMuscleGroupIndex;
        public int gChannelMuscleGroupIndex;
        public int bChannelMuscleGroupIndex;
        public int aChannelMuscleGroupIndex;

    }

    protected struct StringPair
    {

        public string l;
        public string r;

        public StringPair(string l, string r)
        {

            this.l = l;
            this.r = r;

        }

        public static implicit operator StringPair(string str) => new StringPair() { l = str, r = str };

    }

    protected struct DefaultSurfacePoint
    {

        public StringPair surfaceNameLR;
        public int2 vertexIndexLR;

    }

    protected struct DefaultSurfacePoint4
    {

        public DefaultSurfacePoint pointA, pointB, pointC, pointD;

    }

    public static void Apply(out CustomSkinnedMeshRenderer customRenderer,
        SkinnedMeshRenderer renderer, string varicoseShapeName, string shrinkShapeName, int maxBonesPerVertex, MuscularRenderedCharacter character, bool copyNormalsFromSurface, float copyNormalsFromSurfaceFactor, float shapeDeltaNormalNullificationFactor, int muscleMapMidpointSpacingPixels, bool flipUVSide, 
        MuscleMapPacker[] muscleMapPacking, string muscleShapeTextureName, BlendShapeWeight[] blendShapeWeights, string textureOutputPath, UVChannelURP defaultUVChannel, UVChannelURP storeNormalsUVChannel, bool mergeIndexUVs, bool packIndexUVMergeDataIntoSingleFloat, 
        int uvMergeDataIntoSingleFloatChannelComponentIndex, UVChannelURP indexUVReferenceChannel, UVChannelURP indexUVMergedTargetChannel, bool storeUVMergeInZW, float maxShapeVertexTranslationDistance, bool addOutlineMaterial = false, Material outlineMaterial = null, 
        RootTransform rootTransform = RootTransform.Renderer, Transform rootTransformUser = null, bool mergeNormals = true, bool storeRecalculatedNormals = true, bool flipIndexUV_V = false, bool useCustomSkinning = false, TextureFormat outputFormat = TextureFormat.RGBA32,
        bool optimizeInputMesh = true, int opt_maxEdgeLoopLength = 8, int opt_minEdgeLoopLength = 4, float opt_maxEdgeToBoneDirDot = 0.6f, float opt_minInclusionWeight = 0.15f, float opt_junctionBoneDistanceThreshold = 0.001f, float opt_minFaceNormalToBoneDirDot = 0.68f, Vector3 opt_boneRollAxis = default, List<string> opt_edgeAffectingShapes = null)
    {
        customRenderer = null;
        if (string.IsNullOrEmpty(textureOutputPath)) textureOutputPath = "Assets/";

        Transform localRoot = rootTransformUser;

        switch (rootTransform)
        {

            default:
                break;

            case RootTransform.Renderer:

                localRoot = renderer.transform;

                break;

            case RootTransform.RootBone:

                localRoot = renderer.rootBone == null ? renderer.transform : renderer.rootBone;

                break;

            case RootTransform.RootBoneParent:

                localRoot = renderer.rootBone == null ? renderer.transform : (renderer.rootBone.parent == null ? renderer.rootBone : renderer.rootBone.parent);

                break;

        }

        SkinnedMeshRenderer[] surfaces = new SkinnedMeshRenderer[character.SurfaceCount];
        for (int a = 0; a < character.SurfaceCount; a++) surfaces[a] = character.GetSurfaceUnsafe(a).Renderer;

        Transform rootBone = null;
        List<Transform> newBones = new List<Transform>();
        List<Matrix4x4> newBindpose = new List<Matrix4x4>();

        int FindBone(string boneName)
        {

            for(int a = 0; a < newBones.Count; a++)
            {

                if (newBones[a].name == boneName) return a;

            }

            boneName = boneName.ToLower().Trim();

            for (int a = 0; a < newBones.Count; a++)
            {

                if (newBones[a].name.ToLower().Trim() == boneName) return a;

            }

            return -1;

        }

        Dictionary<int, int>[] boneIndexConverters = new Dictionary<int, int>[surfaces.Length];

        List<int> muscularSurfaces = new List<int>();
        for (int b = 0; b < surfaces.Length; b++)
        {

            if (character.GetSurfaceUnsafe(b) is MuscularCharacterSurface) muscularSurfaces.Add(b);

        }
        TempSurfaceData[] surfaceData = new TempSurfaceData[surfaces.Length];
        for (int a = 0; a < surfaces.Length; a++)
        {

            var surface = surfaces[a];
            if (surface == null) continue;

            if (rootBone == null) rootBone = surface.rootBone;

            var bones = surface.bones;
            var bindpose = surface.sharedMesh.bindposes;
            boneIndexConverters[a] = new Dictionary<int, int>();

            for (int b = 0; b < bones.Length; b++)
            {

                var bone = bones[b];

                int i = newBones.IndexOf(bone);

                if (i >= 0)
                {

                    boneIndexConverters[a][b] = i;

                    continue;

                }

                boneIndexConverters[a][b] = newBones.Count;

                newBones.Add(bone);
                newBindpose.Add(bindpose[b]);

            }

            surfaceData[a] = new TempSurfaceData(surface.sharedMesh);

        }
        int FindSurface(string surfaceName)
        {

            for (int a = 0; a < surfaceData.Length; a++) if (surfaceData[a].name == surfaceName) return a;

            return -1;

        }

        if (renderer != null && renderer.sharedMesh != null && surfaces.Length > 0)
        {

            Material[] materials = renderer.sharedMaterials;

            if (addOutlineMaterial)
            {

                Material[] temp_ = new Material[materials.Length * 2];

                materials.CopyTo(temp_, 0);

                for (int a = materials.Length; a < temp_.Length; a++) temp_[a] = outlineMaterial;

                renderer.sharedMaterials = temp_;
                materials = temp_;

            }

            string name = renderer.sharedMesh.name;
            Mesh mesh = optimizeInputMesh ? 
                VeinMeshOptimizer.Optimize(renderer, opt_edgeAffectingShapes, opt_maxEdgeLoopLength, opt_minEdgeLoopLength, opt_maxEdgeToBoneDirDot, opt_minInclusionWeight, opt_junctionBoneDistanceThreshold, opt_minFaceNormalToBoneDirDot, opt_boneRollAxis) 
                : MeshUtils.DuplicateMesh(renderer.sharedMesh);

            Vector3[] vertices = mesh.vertices;
            Vector3[] originalNormals = mesh.normals;
            Vector3[] normals = mesh.normals;

            Transform[] bones = renderer.bones;
            int GetBoneIndex(string boneName)
            {

                for (int a = 0; a < bones.Length; a++) if (bones[a].name == boneName) return a;

                return -1;

            }

            if (bones == null) return;

            VeinsRiggedSharedData tempData = ScriptableObject.CreateInstance<VeinsRiggedSharedData>();
            VeinsRiggedSharedData.FillData(tempData, bones, surfaces, blendShapeWeights, rootTransform, rootTransformUser, mergeNormals);

            SurfaceBindings tempBindings = new SurfaceBindings();
            BoneToSurfaceBindingJobData[] bindingData = new BoneToSurfaceBindingJobData[bones.Length];
            for (int a = 0; a < tempData.bindings.Length; a++)
            {

                var binding = tempData.bindings[a];

                int boneIndex = GetBoneIndex(binding.boneName);
                if (boneIndex < 0) continue;

                var jobData = binding.jobData;
                jobData.samplePointIndices = jobData.vertexIndices;

                bindingData[a] = jobData;

            }
            tempBindings.boundData = new NativeArray<BoneToSurfaceBindingJobData>(bindingData, Allocator.Persistent);

            BoneWeight[] initialBoneWeights = mesh.boneWeights;

            List<BoneWeight1> boneWeights = new List<BoneWeight1>();
            byte[] boneCounts = new byte[mesh.vertexCount];

            for (int a = 0; a < mesh.vertexCount; a++)
            {

                BoneWeight weights = initialBoneWeights[a];

                BoneWeightGroup bwGroup = new BoneWeightGroup();


                var bindingA = weights.boneIndex0 < 0 || weights.weight0 <= 0 ? default : tempData.bindings[weights.boneIndex0];
                var bindingB = weights.boneIndex1 < 0 || weights.weight1 <= 0 ? default : tempData.bindings[weights.boneIndex1];
                var bindingC = weights.boneIndex2 < 0 || weights.weight2 <= 0 ? default : tempData.bindings[weights.boneIndex2];
                var bindingD = weights.boneIndex3 < 0 || weights.weight3 <= 0 ? default : tempData.bindings[weights.boneIndex3];

                int surfaceIndexA = character.FindSurfaceIndex(bindingA.surfaceName);
                int surfaceIndexB = character.FindSurfaceIndex(bindingB.surfaceName);
                int surfaceIndexC = character.FindSurfaceIndex(bindingC.surfaceName);
                int surfaceIndexD = character.FindSurfaceIndex(bindingD.surfaceName);

                var surfaceDataA = surfaceIndexA < 0 ? null : surfaceData[surfaceIndexA];
                var surfaceDataB = surfaceIndexB < 0 ? null : surfaceData[surfaceIndexB];
                var surfaceDataC = surfaceIndexC < 0 ? null : surfaceData[surfaceIndexC];
                var surfaceDataD = surfaceIndexD < 0 ? null : surfaceData[surfaceIndexD];

                void AddWeights(int surfaceIndex, int vertexIndex, TempSurfaceData data, float multiplier)
                {

                    if (data == null) return;

                    int boneCount = data.boneCounts[vertexIndex];

                    for (int b = 0; b < boneCount; b++)
                    {

                        var bw = data.boneWeights[b + data.boneWeightIndexLookup[vertexIndex]];

                        bw.boneIndex = boneIndexConverters[surfaceIndex][bw.boneIndex];

                        bwGroup.Add(bw, multiplier);

                    }

                }

                if (surfaceIndexA >= 0)
                {

                    AddWeights(surfaceIndexA, bindingA.jobData.vertexIndices.x, surfaceDataA, weights.weight0 * bindingA.jobData.coordinates.x);
                    AddWeights(surfaceIndexA, bindingA.jobData.vertexIndices.y, surfaceDataA, weights.weight0 * bindingA.jobData.coordinates.y);
                    AddWeights(surfaceIndexA, bindingA.jobData.vertexIndices.z, surfaceDataA, weights.weight0 * bindingA.jobData.coordinates.z);

                }

                if (surfaceIndexB >= 0)
                {

                    AddWeights(surfaceIndexB, bindingB.jobData.vertexIndices.x, surfaceDataB, weights.weight1 * bindingB.jobData.coordinates.x);
                    AddWeights(surfaceIndexB, bindingB.jobData.vertexIndices.y, surfaceDataB, weights.weight1 * bindingB.jobData.coordinates.y);
                    AddWeights(surfaceIndexB, bindingB.jobData.vertexIndices.z, surfaceDataB, weights.weight1 * bindingB.jobData.coordinates.z);

                }

                if (surfaceIndexC >= 0)
                {

                    AddWeights(surfaceIndexC, bindingC.jobData.vertexIndices.x, surfaceDataC, weights.weight2 * bindingC.jobData.coordinates.x);
                    AddWeights(surfaceIndexC, bindingC.jobData.vertexIndices.y, surfaceDataC, weights.weight2 * bindingC.jobData.coordinates.y);
                    AddWeights(surfaceIndexC, bindingC.jobData.vertexIndices.z, surfaceDataC, weights.weight2 * bindingC.jobData.coordinates.z);

                }

                if (surfaceIndexD >= 0)
                {

                    AddWeights(surfaceIndexD, bindingD.jobData.vertexIndices.x, surfaceDataD, weights.weight3 * bindingD.jobData.coordinates.x);
                    AddWeights(surfaceIndexD, bindingD.jobData.vertexIndices.y, surfaceDataD, weights.weight3 * bindingD.jobData.coordinates.y);
                    AddWeights(surfaceIndexD, bindingD.jobData.vertexIndices.z, surfaceDataD, weights.weight3 * bindingD.jobData.coordinates.z);

                }


                var newWeights = bwGroup.Extract(maxBonesPerVertex);

                boneCounts[a] = (byte)newWeights.Count;

                for (int b = 0; b < newWeights.Count; b++) boneWeights.Add(newWeights[b]);

            }

            List<BlendShape> blendShapes = mesh.GetBlendShapes();
            BlendShape FindBlendShape(string name)
            {

                foreach (BlendShape shape in blendShapes) if (shape.name == name) return shape;
                name = name.ToLower().Trim();
                foreach (BlendShape shape in blendShapes) if (shape.name.ToLower().Trim() == name) return shape;

                return null;

            }

            // Set vertices to base position

            for (int a = 0; a < surfaceData.Length; a++)
            {

                var data = surfaceData[a];
                if (data == null) continue; 

                data.UpdateDynamicSamples(blendShapeWeights, -1);

            }

            for (int a = 0; a < mesh.vertexCount; a++)
            {

                BoneWeight weights = initialBoneWeights[a];

                var bindingA = weights.boneIndex0 < 0 || weights.weight0 <= 0 ? default : tempData.bindings[weights.boneIndex0];
                var bindingB = weights.boneIndex1 < 0 || weights.weight1 <= 0 ? default : tempData.bindings[weights.boneIndex1];
                var bindingC = weights.boneIndex2 < 0 || weights.weight2 <= 0 ? default : tempData.bindings[weights.boneIndex2];
                var bindingD = weights.boneIndex3 < 0 || weights.weight3 <= 0 ? default : tempData.bindings[weights.boneIndex3];

                int surfaceIndexA = character.FindSurfaceIndex(bindingA.surfaceName);
                int surfaceIndexB = character.FindSurfaceIndex(bindingB.surfaceName);
                int surfaceIndexC = character.FindSurfaceIndex(bindingC.surfaceName);
                int surfaceIndexD = character.FindSurfaceIndex(bindingD.surfaceName);

                void AddShapeData(int initialBoneIndex, int vertexIndex, int surfaceIndex, BoneToSurfaceBinding binding, float multiplier)
                {

                    if (initialBoneIndex < 0 || vertexIndex < 0 || surfaceIndex < 0) return;

                    TempSurfaceData tempData = surfaceData[surfaceIndex];

                    var jobData = tempBindings.boundData[initialBoneIndex];

                    SamplePoint samplePointA = tempData.dynamicSamplePoints[jobData.samplePointIndices.x];
                    SamplePoint samplePointB = tempData.dynamicSamplePoints[jobData.samplePointIndices.y];
                    SamplePoint samplePointC = tempData.dynamicSamplePoints[jobData.samplePointIndices.z];

                    float3 originalPosition = ((samplePointA.localVertex * jobData.coordinates.x) + (samplePointB.localVertex * jobData.coordinates.y) + (samplePointC.localVertex * jobData.coordinates.z)) + jobData.defaultLocalPosition;
                    VeinsRigged.UpdateVeinRigJob.GetLocalTransform(initialBoneIndex, tempBindings.boundData, tempData.dynamicSamplePoints, out float3 reshapedPosition, out _, out _, out _, out quaternion surfaceRotation, out quaternion directionOffset);

                    vertices[vertexIndex] += (Vector3)(reshapedPosition - originalPosition) * multiplier;
                    normals[vertexIndex] += (Vector3)(math.rotate(math.mul(directionOffset, surfaceRotation), originalNormals[vertexIndex]) - (float3)originalNormals[vertexIndex]) * multiplier;

                }

                AddShapeData(weights.boneIndex0, a, surfaceIndexA, bindingA, weights.weight0);
                AddShapeData(weights.boneIndex1, a, surfaceIndexB, bindingB, weights.weight1);
                AddShapeData(weights.boneIndex2, a, surfaceIndexC, bindingC, weights.weight2);
                AddShapeData(weights.boneIndex3, a, surfaceIndexD, bindingD, weights.weight3);

            }

            for (int a = 0; a < mesh.vertexCount; a++) normals[a] = normals[a].normalized;

            //

            Dictionary<string, List<int>> surfaceGroupedVerts = new Dictionary<string, List<int>>();
            void AddSurfaceGroupedVert(string surfaceName, int vert)
            {

                if (string.IsNullOrEmpty(surfaceName)) return;

                if (!surfaceGroupedVerts.TryGetValue(surfaceName, out List<int> verts))
                {

                    verts = new List<int>();
                    surfaceGroupedVerts[surfaceName] = verts; 

                }

                verts.Add(vert);

            }

            for (int b = 0; b < mesh.vertexCount; b++)
            {

                BoneWeight weights = initialBoneWeights[b];

                var bindingA = weights.boneIndex0 < 0 || weights.weight0 <= 0 ? default : tempData.bindings[weights.boneIndex0];
                var bindingB = weights.boneIndex1 < 0 || weights.weight1 <= 0 ? default : tempData.bindings[weights.boneIndex1];
                var bindingC = weights.boneIndex2 < 0 || weights.weight2 <= 0 ? default : tempData.bindings[weights.boneIndex2];
                var bindingD = weights.boneIndex3 < 0 || weights.weight3 <= 0 ? default : tempData.bindings[weights.boneIndex3];

                AddSurfaceGroupedVert(bindingA.surfaceName, b);
                AddSurfaceGroupedVert(bindingB.surfaceName, b);
                AddSurfaceGroupedVert(bindingC.surfaceName, b);
                AddSurfaceGroupedVert(bindingD.surfaceName, b);

            }

            SurfaceBoundWeights[] muscleGroups = new SurfaceBoundWeights[character.MuscleGroupCount];

            // Copies the values of a muscular surface's muscle groups to the vein model, which is then converted into texture maps to be used as muscle group masks in shader
            SurfaceBoundWeights CreateMuscleGroupWeights(MuscularCharacterSharedData.MuscleGroup charMuscleGroup, MuscularCharacterSharedData.MuscleGroup charMuscleGroupMirror = null)
            {

                Dictionary<string, DefaultSurfacePoint> nearestSurfacePointToGroups = new Dictionary<string, DefaultSurfacePoint>();
                foreach(var group in surfaceGroupedVerts)
                {

                    Vector3 averagePositionL = Vector3.zero;
                    Vector3 averagePositionR = Vector3.zero;

                    float weightTotalL = 0;
                    float weightTotalR = 0;
                    foreach (int vert in group.Value)
                    {

                        BoneWeight weights = initialBoneWeights[vert];

                        var bindingA = weights.boneIndex0 < 0 || weights.weight0 <= 0 ? default : tempData.bindings[weights.boneIndex0];
                        var bindingB = weights.boneIndex1 < 0 || weights.weight1 <= 0 ? default : tempData.bindings[weights.boneIndex1];
                        var bindingC = weights.boneIndex2 < 0 || weights.weight2 <= 0 ? default : tempData.bindings[weights.boneIndex2];
                        var bindingD = weights.boneIndex3 < 0 || weights.weight3 <= 0 ? default : tempData.bindings[weights.boneIndex3];

                        Vector3 position = (Vector3)(
                            (bindingA.jobData.defaultLocalPositionClosestPoint * weights.weight0) + 
                            (bindingB.jobData.defaultLocalPositionClosestPoint * weights.weight1) +
                            (bindingC.jobData.defaultLocalPositionClosestPoint * weights.weight2) +
                            (bindingD.jobData.defaultLocalPositionClosestPoint * weights.weight3)
                            );

                        if (position.x < 0)
                        {

                            weightTotalL += weights.weight0;
                            weightTotalL += weights.weight1;
                            weightTotalL += weights.weight2;
                            weightTotalL += weights.weight3;

                            averagePositionL += position;

                        } 
                        else
                        {

                            weightTotalR += weights.weight0;
                            weightTotalR += weights.weight1;
                            weightTotalR += weights.weight2;
                            weightTotalR += weights.weight3;

                            averagePositionR += position;

                        }

                    }

                    averagePositionL = weightTotalL <= 0 ? Vector3.zero : averagePositionL / weightTotalL;
                    averagePositionR = weightTotalR <= 0 ? Vector3.zero : averagePositionR / weightTotalR;

                    DefaultSurfacePoint nearestSurfacePoint = default;
                    nearestSurfacePoint.vertexIndexLR = -1;
                    float minDistL = float.MaxValue;
                    float minDistR = float.MaxValue;
                    for (int b = 0; b < muscularSurfaces.Count; b++)
                    {
                         
                        var tempData = surfaceData[muscularSurfaces[b]];

                        if (charMuscleGroup.GetSurfaceBoundWeightsIndex(tempData.name) < 0) continue;

                        for(int c = 0; c < tempData.vertices.Length; c++) 
                        {

                            float distL = (tempData.vertices[c] - averagePositionL).sqrMagnitude;

                            if (weightTotalL > 0 && distL < minDistL)
                            {

                                nearestSurfacePoint = new DefaultSurfacePoint() { surfaceNameLR = new StringPair(tempData.name, nearestSurfacePoint.surfaceNameLR.r), vertexIndexLR = new int2(c, nearestSurfacePoint.vertexIndexLR.y) };
                                minDistL = distL;
                                 
                            }

                            float distR = (tempData.vertices[c] - averagePositionR).sqrMagnitude;

                            if (weightTotalR > 0 && distR < minDistR)
                            {

                                nearestSurfacePoint = new DefaultSurfacePoint() { surfaceNameLR = new StringPair(nearestSurfacePoint.surfaceNameLR.l, tempData.name), vertexIndexLR = new int2(nearestSurfacePoint.vertexIndexLR.x, c) };
                                minDistR = distR;

                            }

                        }

                    }

                    if (math.any(nearestSurfacePoint.vertexIndexLR >= 0)) nearestSurfacePointToGroups[group.Key] = nearestSurfacePoint;

                }

                DefaultSurfacePoint4[] defaultSurfacePoints = new DefaultSurfacePoint4[mesh.vertexCount]; // Used for parts of a vein mesh that may rest on a surface that has no muscle groups, such as the hands or forehead. These are the closest known muscular surface points that have muscle group data.

                DefaultSurfacePoint GetDefaultVertexIndex(BoneToSurfaceBinding binding)
                {

                    var jobData = binding.jobData;

                    return (jobData.coordinates.x > jobData.coordinates.y && jobData.coordinates.x > jobData.coordinates.z) ? new DefaultSurfacePoint() { surfaceNameLR = binding.surfaceName, vertexIndexLR = jobData.vertexIndices.x } :
                            ((jobData.coordinates.y > jobData.coordinates.x && jobData.coordinates.y > jobData.coordinates.z) ? new DefaultSurfacePoint() { surfaceNameLR = binding.surfaceName, vertexIndexLR = jobData.vertexIndices.y } : new DefaultSurfacePoint() { surfaceNameLR = binding.surfaceName, vertexIndexLR = jobData.vertexIndices.z });

                }

                DefaultSurfacePoint GetDefaultSurfacePoint(BoneToSurfaceBinding binding)
                {

                    if (string.IsNullOrEmpty(binding.surfaceName)) return new DefaultSurfacePoint() { surfaceNameLR = "", vertexIndexLR = -1 };

                    int surfaceIndex = charMuscleGroup.GetSurfaceBoundWeightsIndex(binding.surfaceName);

                    if (surfaceIndex >= 0) return GetDefaultVertexIndex(binding);

                    nearestSurfacePointToGroups.TryGetValue(binding.surfaceName, out DefaultSurfacePoint point);

                    return point;

                } 

                for (int b = 0; b < mesh.vertexCount; b++)
                {

                    BoneWeight weights = initialBoneWeights[b];

                    var bindingA = weights.boneIndex0 < 0 || weights.weight0 <= 0 ? default : tempData.bindings[weights.boneIndex0];
                    var bindingB = weights.boneIndex1 < 0 || weights.weight1 <= 0 ? default : tempData.bindings[weights.boneIndex1];
                    var bindingC = weights.boneIndex2 < 0 || weights.weight2 <= 0 ? default : tempData.bindings[weights.boneIndex2];
                    var bindingD = weights.boneIndex3 < 0 || weights.weight3 <= 0 ? default : tempData.bindings[weights.boneIndex3];

                    DefaultSurfacePoint4 defaults = new DefaultSurfacePoint4();

                    defaults.pointA = GetDefaultSurfacePoint(bindingA);
                    defaults.pointB = GetDefaultSurfacePoint(bindingB);
                    defaults.pointC = GetDefaultSurfacePoint(bindingC);
                    defaults.pointD = GetDefaultSurfacePoint(bindingD);

                    defaultSurfacePoints[b] = defaults;

                }

                SurfaceBoundWeights muscleGroup = new SurfaceBoundWeights();

                muscleGroup.surfaceName = charMuscleGroup.name;

                muscleGroup.vertexCount = mesh.vertexCount;
                muscleGroup.weights = new WeightIndexPair[muscleGroup.vertexCount];

                for (int b = 0; b < mesh.vertexCount; b++)
                {

                    Vector3 pos = vertices[b];
                    
                    BoneWeight weights = initialBoneWeights[b];
                    DefaultSurfacePoint4 defaults = defaultSurfacePoints[b];

                    var bindingA = weights.boneIndex0 < 0 || weights.weight0 <= 0 ? default : tempData.bindings[weights.boneIndex0];
                    var bindingB = weights.boneIndex1 < 0 || weights.weight1 <= 0 ? default : tempData.bindings[weights.boneIndex1];
                    var bindingC = weights.boneIndex2 < 0 || weights.weight2 <= 0 ? default : tempData.bindings[weights.boneIndex2];
                    var bindingD = weights.boneIndex3 < 0 || weights.weight3 <= 0 ? default : tempData.bindings[weights.boneIndex3];

                    int surfaceIndexA = charMuscleGroup.GetSurfaceBoundWeightsIndex(bindingA.surfaceName);
                    int surfaceIndexB = charMuscleGroup.GetSurfaceBoundWeightsIndex(bindingB.surfaceName);
                    int surfaceIndexC = charMuscleGroup.GetSurfaceBoundWeightsIndex(bindingC.surfaceName);
                    int surfaceIndexD = charMuscleGroup.GetSurfaceBoundWeightsIndex(bindingD.surfaceName);

                    int surfaceIndexAMirrored = charMuscleGroupMirror == null ? -1 : charMuscleGroupMirror.GetSurfaceBoundWeightsIndex(bindingA.surfaceName);
                    int surfaceIndexBMirrored = charMuscleGroupMirror == null ? -1 : charMuscleGroupMirror.GetSurfaceBoundWeightsIndex(bindingB.surfaceName);
                    int surfaceIndexCMirrored = charMuscleGroupMirror == null ? -1 : charMuscleGroupMirror.GetSurfaceBoundWeightsIndex(bindingC.surfaceName);
                    int surfaceIndexDMirrored = charMuscleGroupMirror == null ? -1 : charMuscleGroupMirror.GetSurfaceBoundWeightsIndex(bindingD.surfaceName);

                    var surfaceMuscleGroupA = surfaceIndexA < 0 ? null : charMuscleGroup.surfaceWeights[surfaceIndexA];
                    var surfaceMuscleGroupB = surfaceIndexB < 0 ? null : charMuscleGroup.surfaceWeights[surfaceIndexB];
                    var surfaceMuscleGroupC = surfaceIndexC < 0 ? null : charMuscleGroup.surfaceWeights[surfaceIndexC];
                    var surfaceMuscleGroupD = surfaceIndexD < 0 ? null : charMuscleGroup.surfaceWeights[surfaceIndexD];

                    var surfaceMuscleGroupAMirrored = surfaceIndexAMirrored < 0 ? null : charMuscleGroupMirror.surfaceWeights[surfaceIndexA];
                    var surfaceMuscleGroupBMirrored = surfaceIndexBMirrored < 0 ? null : charMuscleGroupMirror.surfaceWeights[surfaceIndexB];
                    var surfaceMuscleGroupCMirrored = surfaceIndexCMirrored < 0 ? null : charMuscleGroupMirror.surfaceWeights[surfaceIndexC];
                    var surfaceMuscleGroupDMirrored = surfaceIndexDMirrored < 0 ? null : charMuscleGroupMirror.surfaceWeights[surfaceIndexD];

                    float GetCachedWeight(SurfaceBoundWeights surfaceWeights, int3 vertexIndices, float3 coordinates, StringPair defaultSurfaceName = default, int2 defaultVertexIndex = default)
                    {

                        if (surfaceWeights != null)
                        {

                            return (surfaceWeights.GetCachedWeight(vertexIndices.x) * coordinates.x) +
                                   (surfaceWeights.GetCachedWeight(vertexIndices.y) * coordinates.y) +
                                   (surfaceWeights.GetCachedWeight(vertexIndices.z) * coordinates.z);
                             
                        }

                        float weight = 0;

                        int defaultSurfaceIndex = charMuscleGroup.GetSurfaceBoundWeightsIndex(pos.x < 0 ? defaultSurfaceName.l : defaultSurfaceName.r);
                        int defaultSurfaceIndexMirrored = charMuscleGroupMirror == null ? -1 : charMuscleGroupMirror.GetSurfaceBoundWeightsIndex(pos.x < 0 ? defaultSurfaceName.l : defaultSurfaceName.r);

                        surfaceWeights = defaultSurfaceIndex < 0 ? null : charMuscleGroup.surfaceWeights[defaultSurfaceIndex];
                        SurfaceBoundWeights surfaceWeightsMirrored = defaultSurfaceIndexMirrored < 0 ? null : charMuscleGroupMirror.surfaceWeights[defaultSurfaceIndexMirrored];

                        if (surfaceWeights != null) weight += surfaceWeights.GetCachedWeight(pos.x < 0 ? defaultVertexIndex.x : defaultVertexIndex.y);
                        if (surfaceWeightsMirrored != null) weight += surfaceWeightsMirrored.GetCachedWeight(pos.x < 0 ? defaultVertexIndex.x : defaultVertexIndex.y);
                         
                        return weight;

                    } 

                    float weightA = (GetCachedWeight(surfaceMuscleGroupA, bindingA.jobData.vertexIndices, bindingA.jobData.coordinates, defaults.pointA.surfaceNameLR, defaults.pointA.vertexIndexLR) + GetCachedWeight(surfaceMuscleGroupAMirrored, bindingA.jobData.vertexIndices, bindingA.jobData.coordinates)) * weights.weight0;
                    float weightB = (GetCachedWeight(surfaceMuscleGroupB, bindingB.jobData.vertexIndices, bindingB.jobData.coordinates, defaults.pointB.surfaceNameLR, defaults.pointB.vertexIndexLR) + GetCachedWeight(surfaceMuscleGroupBMirrored, bindingB.jobData.vertexIndices, bindingB.jobData.coordinates)) * weights.weight1;
                    float weightC = (GetCachedWeight(surfaceMuscleGroupC, bindingC.jobData.vertexIndices, bindingC.jobData.coordinates, defaults.pointC.surfaceNameLR, defaults.pointC.vertexIndexLR) + GetCachedWeight(surfaceMuscleGroupCMirrored, bindingC.jobData.vertexIndices, bindingC.jobData.coordinates)) * weights.weight2;
                    float weightD = (GetCachedWeight(surfaceMuscleGroupD, bindingD.jobData.vertexIndices, bindingD.jobData.coordinates, defaults.pointD.surfaceNameLR, defaults.pointD.vertexIndexLR) + GetCachedWeight(surfaceMuscleGroupDMirrored, bindingD.jobData.vertexIndices, bindingD.jobData.coordinates)) * weights.weight3;

                    muscleGroup.weights[b] = new WeightIndexPair() { index = b, weight = weightA + weightB + weightC + weightD };

                }

                return muscleGroup;

            }

            SurfaceBoundWeights midpointGroup = CreateMuscleGroupWeights(character.inputData.midpointMuscleGroup);

            for (int a = 0; a < character.MuscleGroupCount; a++)
            {

                var charMuscleGroup = character.inputData.muscleGroups[a];

                muscleGroups[a] = CreateMuscleGroupWeights(charMuscleGroup, charMuscleGroup.mirroredIndex >= 0 ? character.inputData.muscleGroups[charMuscleGroup.mirroredIndex] : null);

            }

            SurfaceShapeData muscleMapData = new SurfaceShapeData() { vertexCount = mesh.vertexCount };
            muscleMapData.shapeMuscleFit = new ShapeData() { deltaVertices = new float3[mesh.vertexCount], deltaNormals = new float3[mesh.vertexCount] };
            muscleMapData.shapeMuscleMax = new ShapeData() { deltaVertices = new float3[mesh.vertexCount], deltaNormals = new float3[mesh.vertexCount] };
            muscleMapData.shapeMuscleFlex = new ShapeData() { deltaVertices = new float3[mesh.vertexCount], deltaNormals = new float3[mesh.vertexCount] };
            muscleMapData.shapeBreastFull = new ShapeData() { deltaVertices = new float3[mesh.vertexCount], deltaNormals = new float3[mesh.vertexCount] };
            muscleMapData.shapeBreastFullMuscleMax = new ShapeData() { deltaVertices = new float3[mesh.vertexCount], deltaNormals = new float3[mesh.vertexCount] };

            for (int a = 0; a < mesh.vertexCount; a++)
            {

                BoneWeight weights = initialBoneWeights[a];

                var bindingA = weights.boneIndex0 < 0 || weights.weight0 <= 0 ? default : tempData.bindings[weights.boneIndex0];
                var bindingB = weights.boneIndex1 < 0 || weights.weight1 <= 0 ? default : tempData.bindings[weights.boneIndex1];
                var bindingC = weights.boneIndex2 < 0 || weights.weight2 <= 0 ? default : tempData.bindings[weights.boneIndex2];
                var bindingD = weights.boneIndex3 < 0 || weights.weight3 <= 0 ? default : tempData.bindings[weights.boneIndex3];

                int surfaceIndexA = character.inputData.GetSurfaceShapeDataIndex(bindingA.surfaceName);
                int surfaceIndexB = character.inputData.GetSurfaceShapeDataIndex(bindingB.surfaceName);
                int surfaceIndexC = character.inputData.GetSurfaceShapeDataIndex(bindingC.surfaceName);
                int surfaceIndexD = character.inputData.GetSurfaceShapeDataIndex(bindingD.surfaceName);

                var shapeDataA = surfaceIndexA < 0 ? null : character.inputData.surfaceShapeData[surfaceIndexA];
                var shapeDataB = surfaceIndexB < 0 ? null : character.inputData.surfaceShapeData[surfaceIndexB];
                var shapeDataC = surfaceIndexC < 0 ? null : character.inputData.surfaceShapeData[surfaceIndexC];
                var shapeDataD = surfaceIndexD < 0 ? null : character.inputData.surfaceShapeData[surfaceIndexD];

                void AddShapeData(int vertexIndex, SurfaceShapeData data, BoneToSurfaceBinding binding, float multiplier)
                {

                    if (data == null) return;

                    muscleMapData.shapeMuscleFit.deltaVertices[vertexIndex] += ((data.shapeMuscleFit.deltaVertices[binding.jobData.vertexIndices.x] * binding.jobData.coordinates.x) + (data.shapeMuscleFit.deltaVertices[binding.jobData.vertexIndices.y] * binding.jobData.coordinates.y) + (data.shapeMuscleFit.deltaVertices[binding.jobData.vertexIndices.z] * binding.jobData.coordinates.z)) * multiplier;
                    muscleMapData.shapeMuscleMax.deltaVertices[vertexIndex] += ((data.shapeMuscleMax.deltaVertices[binding.jobData.vertexIndices.x] * binding.jobData.coordinates.x) + (data.shapeMuscleMax.deltaVertices[binding.jobData.vertexIndices.y] * binding.jobData.coordinates.y) + (data.shapeMuscleMax.deltaVertices[binding.jobData.vertexIndices.z] * binding.jobData.coordinates.z)) * multiplier;
                    muscleMapData.shapeMuscleFlex.deltaVertices[vertexIndex] += ((data.shapeMuscleFlex.deltaVertices[binding.jobData.vertexIndices.x] * binding.jobData.coordinates.x) + (data.shapeMuscleFlex.deltaVertices[binding.jobData.vertexIndices.y] * binding.jobData.coordinates.y) + (data.shapeMuscleFlex.deltaVertices[binding.jobData.vertexIndices.z] * binding.jobData.coordinates.z)) * multiplier;
                    muscleMapData.shapeBreastFull.deltaVertices[vertexIndex] += ((data.shapeBreastFull.deltaVertices[binding.jobData.vertexIndices.x] * binding.jobData.coordinates.x) + (data.shapeBreastFull.deltaVertices[binding.jobData.vertexIndices.y] * binding.jobData.coordinates.y) + (data.shapeBreastFull.deltaVertices[binding.jobData.vertexIndices.z] * binding.jobData.coordinates.z)) * multiplier;
                    muscleMapData.shapeBreastFullMuscleMax.deltaVertices[vertexIndex] += ((data.shapeBreastFullMuscleMax.deltaVertices[binding.jobData.vertexIndices.x] * binding.jobData.coordinates.x) + (data.shapeBreastFullMuscleMax.deltaVertices[binding.jobData.vertexIndices.y] * binding.jobData.coordinates.y) + (data.shapeBreastFullMuscleMax.deltaVertices[binding.jobData.vertexIndices.z] * binding.jobData.coordinates.z)) * multiplier;

                    muscleMapData.shapeMuscleFit.deltaNormals[vertexIndex] += ((data.shapeMuscleFit.deltaNormals[binding.jobData.vertexIndices.x] * binding.jobData.coordinates.x) + (data.shapeMuscleFit.deltaNormals[binding.jobData.vertexIndices.y] * binding.jobData.coordinates.y) + (data.shapeMuscleFit.deltaNormals[binding.jobData.vertexIndices.z] * binding.jobData.coordinates.z)) * multiplier * (1 - shapeDeltaNormalNullificationFactor);
                    muscleMapData.shapeMuscleMax.deltaNormals[vertexIndex] += ((data.shapeMuscleMax.deltaNormals[binding.jobData.vertexIndices.x] * binding.jobData.coordinates.x) + (data.shapeMuscleMax.deltaNormals[binding.jobData.vertexIndices.y] * binding.jobData.coordinates.y) + (data.shapeMuscleMax.deltaNormals[binding.jobData.vertexIndices.z] * binding.jobData.coordinates.z)) * multiplier * (1 - shapeDeltaNormalNullificationFactor);
                    muscleMapData.shapeMuscleFlex.deltaNormals[vertexIndex] += ((data.shapeMuscleFlex.deltaNormals[binding.jobData.vertexIndices.x] * binding.jobData.coordinates.x) + (data.shapeMuscleFlex.deltaNormals[binding.jobData.vertexIndices.y] * binding.jobData.coordinates.y) + (data.shapeMuscleFlex.deltaNormals[binding.jobData.vertexIndices.z] * binding.jobData.coordinates.z)) * multiplier * (1 - shapeDeltaNormalNullificationFactor);
                    muscleMapData.shapeBreastFull.deltaNormals[vertexIndex] += ((data.shapeBreastFull.deltaNormals[binding.jobData.vertexIndices.x] * binding.jobData.coordinates.x) + (data.shapeBreastFull.deltaNormals[binding.jobData.vertexIndices.y] * binding.jobData.coordinates.y) + (data.shapeBreastFull.deltaNormals[binding.jobData.vertexIndices.z] * binding.jobData.coordinates.z)) * multiplier * (1 - shapeDeltaNormalNullificationFactor);
                    muscleMapData.shapeBreastFullMuscleMax.deltaNormals[vertexIndex] += ((data.shapeBreastFullMuscleMax.deltaNormals[binding.jobData.vertexIndices.x] * binding.jobData.coordinates.x) + (data.shapeBreastFullMuscleMax.deltaNormals[binding.jobData.vertexIndices.y] * binding.jobData.coordinates.y) + (data.shapeBreastFullMuscleMax.deltaNormals[binding.jobData.vertexIndices.z] * binding.jobData.coordinates.z)) * multiplier * (1 - shapeDeltaNormalNullificationFactor);

                }

                AddShapeData(a, shapeDataA, bindingA, weights.weight0);
                AddShapeData(a, shapeDataB, bindingB, weights.weight1);
                AddShapeData(a, shapeDataC, bindingC, weights.weight2);
                AddShapeData(a, shapeDataD, bindingD, weights.weight3);

            }

            // Convert muscle shapes to muscle map

            NativeArray<byte> tempBoneCounts = new NativeArray<byte>(boneCounts, Allocator.Persistent);
            NativeArray<BoneWeight1> tempBoneWeights = new NativeArray<BoneWeight1>(boneWeights.ToArray(), Allocator.Persistent);

            Mesh temp = new Mesh();
            temp.indexFormat = mesh.indexFormat;

            temp.vertices = vertices;
            temp.normals = normals;
            temp.tangents = mesh.tangents;
            temp.colors = mesh.colors;

            temp.SetUVs(0, Swole.MeshUtils.GetUVsByChannelAsListV4(mesh, 0));
            temp.SetUVs(1, Swole.MeshUtils.GetUVsByChannelAsListV4(mesh, 1));
            temp.SetUVs(2, Swole.MeshUtils.GetUVsByChannelAsListV4(mesh, 2));
            temp.SetUVs(3, Swole.MeshUtils.GetUVsByChannelAsListV4(mesh, 3));

            temp.triangles = mesh.triangles;
            temp.subMeshCount = mesh.subMeshCount;
            for (int s = 0; s < mesh.subMeshCount; s++) temp.SetSubMesh(s, mesh.GetSubMesh(s));

            mesh = temp;

            mesh.SetBoneWeights(tempBoneCounts, tempBoneWeights);

            tempBoneCounts.Dispose();
            tempBoneWeights.Dispose();

            int indexTextureSize = Mathf.CeilToInt(math.sqrt(mesh.vertexCount));
            int maskMapTextureWidth = Mathf.FloorToInt(indexTextureSize * 0.5f) - (Mathf.FloorToInt((muscleMapMidpointSpacingPixels * 0.5f)) + 1);
            int maskMapTextureHeight = indexTextureSize + (indexTextureSize - maskMapTextureWidth);
            indexTextureSize = Maths.AsPowerOf2(indexTextureSize);
            int maskMapTextureSize = Maths.AsPowerOf2(maskMapTextureHeight);
            Vector4[] indexUV = new Vector4[mesh.vertexCount];
            List<int> sideA = new List<int>();
            List<int> sideB = new List<int>();
            for (int a = 0; a < indexUV.Length; a++)
            {

                if (vertices[a].x < 0) sideA.Add(a); else sideB.Add(a);

            }
            for (int a = 0; a < sideA.Count; a++)
            {

                /*float x = (a % maskMapTextureWidth) / (float)maskMapTextureSize;
                float y = (a / maskMapTextureWidth) / (float)maskMapTextureSize;

                if (flipUVSide) x = 1 - (x + (1 / (float)maskMapTextureSize));*/

                int x = a % maskMapTextureWidth;
                int y = a / maskMapTextureWidth;

                if (flipUVSide) x = (maskMapTextureSize - 1) - x;

                indexUV[sideA[a]] = new Vector4(x / (float)maskMapTextureSize, y / (float)maskMapTextureSize, 0, 0);

            }
            for (int a = 0; a < sideB.Count; a++)
            {

                /*float x = (a % maskMapTextureWidth) / (float)maskMapTextureSize;
                float y = (a / maskMapTextureWidth) / (float)maskMapTextureSize;

                if (!flipUVSide) x = 1 - (x + (1 / (float)maskMapTextureSize));*/

                int x = a % maskMapTextureWidth;
                int y = a / maskMapTextureWidth;

                if (!flipUVSide) x = (maskMapTextureSize - 1) - x;

                indexUV[sideB[a]] = new Vector4(x / (float)maskMapTextureSize, y / (float)maskMapTextureSize, 0, 0);

            }

            mesh.SetUVs((int)indexUVReferenceChannel, indexUV);

            mesh.name = name;
            mesh.bindposes = newBindpose.ToArray();

            renderer.sharedMesh = null;
            renderer.rootBone = null;
            renderer.bones = newBones.ToArray();
            renderer.rootBone = rootBone;
            mesh.RecalculateTangents();

            normals = mesh.normals;

            if (copyNormalsFromSurface)
            {

                for (int a = 0; a < mesh.vertexCount; a++)
                {

                    BoneWeight weights = initialBoneWeights[a];

                    var bindingA = weights.boneIndex0 < 0 || weights.weight0 <= 0 ? default : tempData.bindings[weights.boneIndex0];
                    var bindingB = weights.boneIndex1 < 0 || weights.weight1 <= 0 ? default : tempData.bindings[weights.boneIndex1];
                    var bindingC = weights.boneIndex2 < 0 || weights.weight2 <= 0 ? default : tempData.bindings[weights.boneIndex2];
                    var bindingD = weights.boneIndex3 < 0 || weights.weight3 <= 0 ? default : tempData.bindings[weights.boneIndex3];

                    int surfaceIndexA = character.FindSurfaceIndex(bindingA.surfaceName);
                    int surfaceIndexB = character.FindSurfaceIndex(bindingB.surfaceName);
                    int surfaceIndexC = character.FindSurfaceIndex(bindingC.surfaceName);
                    int surfaceIndexD = character.FindSurfaceIndex(bindingD.surfaceName);

                    var surfaceDataA = surfaceIndexA < 0 ? null : surfaceData[surfaceIndexA];
                    var surfaceDataB = surfaceIndexB < 0 ? null : surfaceData[surfaceIndexB];
                    var surfaceDataC = surfaceIndexC < 0 ? null : surfaceData[surfaceIndexC];
                    var surfaceDataD = surfaceIndexD < 0 ? null : surfaceData[surfaceIndexD];

                    Vector3 normal = Vector3.zero;

                    void AddNormal(int surfaceIndex, int vertexIndex, TempSurfaceData data, float multiplier)
                    {

                        if (data == null) return;

                        normal = normal + data.normals[vertexIndex] * multiplier;

                    }

                    if (surfaceIndexA >= 0)
                    {

                        AddNormal(surfaceIndexA, bindingA.jobData.vertexIndices.x, surfaceDataA, weights.weight0 * bindingA.jobData.coordinates.x);
                        AddNormal(surfaceIndexA, bindingA.jobData.vertexIndices.y, surfaceDataA, weights.weight0 * bindingA.jobData.coordinates.y);
                        AddNormal(surfaceIndexA, bindingA.jobData.vertexIndices.z, surfaceDataA, weights.weight0 * bindingA.jobData.coordinates.z);
                    }
                    if (surfaceIndexB >= 0)
                    {

                        AddNormal(surfaceIndexB, bindingB.jobData.vertexIndices.x, surfaceDataB, weights.weight1 * bindingB.jobData.coordinates.x);
                        AddNormal(surfaceIndexB, bindingB.jobData.vertexIndices.y, surfaceDataB, weights.weight1 * bindingB.jobData.coordinates.y);
                        AddNormal(surfaceIndexB, bindingB.jobData.vertexIndices.z, surfaceDataB, weights.weight1 * bindingB.jobData.coordinates.z);

                    }

                    if (surfaceIndexC >= 0)
                    {

                        AddNormal(surfaceIndexC, bindingC.jobData.vertexIndices.x, surfaceDataC, weights.weight2 * bindingC.jobData.coordinates.x);
                        AddNormal(surfaceIndexC, bindingC.jobData.vertexIndices.y, surfaceDataC, weights.weight2 * bindingC.jobData.coordinates.y);
                        AddNormal(surfaceIndexC, bindingC.jobData.vertexIndices.z, surfaceDataC, weights.weight2 * bindingC.jobData.coordinates.z);

                    }

                    if (surfaceIndexD >= 0)
                    {

                        AddNormal(surfaceIndexD, bindingD.jobData.vertexIndices.x, surfaceDataD, weights.weight3 * bindingD.jobData.coordinates.x);
                        AddNormal(surfaceIndexD, bindingD.jobData.vertexIndices.y, surfaceDataD, weights.weight3 * bindingD.jobData.coordinates.y);
                        AddNormal(surfaceIndexD, bindingD.jobData.vertexIndices.z, surfaceDataD, weights.weight3 * bindingD.jobData.coordinates.z);

                    }

                    normals[a] = Vector3.Lerp(normals[a], normal, copyNormalsFromSurfaceFactor).normalized;

                }

            }

            Vector4[] tangents = mesh.tangents;
            for (int a = 0; a < mesh.vertexCount; a++)
            {

                Vector3 normal = normals[a].normalized;
                Vector4 tangent = tangents[a];

                Vector3 tangentXYZ = new Vector3(tangent.x, tangent.y, tangent.z).normalized;
                tangent = new Vector4(tangentXYZ.x, tangentXYZ.y, tangentXYZ.z, tangent.w);

                normals[a] = normal;
                tangents[a] = tangent;

            }
            mesh.normals = normals;
            mesh.tangents = tangents;

            renderer.sharedMesh = mesh;

            List<string> tempShapeNames = new List<string>();

            void AddTempShape(string shapeName, ShapeData shapeData)
            {

                BlendShape tempShape = new BlendShape(shapeName);

                BlendShape.Frame tempFrame = new BlendShape.Frame(tempShape, 0, 1, shapeData.deltaVertices.ToVector3(), shapeData.deltaNormals.ToVector3(), new Vector3[mesh.vertexCount]);
                tempShape.frames = new BlendShape.Frame[] { tempFrame };

                tempShapeNames.Add(shapeName);

                tempShape.AddToMesh(mesh);

            }

            AddTempShape("temp_shapeMuscleFit", muscleMapData.shapeMuscleFit);
            AddTempShape("temp_shapeMuscleMax", muscleMapData.shapeMuscleMax);
            AddTempShape("temp_shapeMuscleFlex", muscleMapData.shapeMuscleFlex);
            AddTempShape("temp_shapeBreastFull", muscleMapData.shapeBreastFull);
            AddTempShape("temp_shapeBreastFullMuscleMax", muscleMapData.shapeBreastFullMuscleMax);

            if (!string.IsNullOrEmpty(shrinkShapeName)) 
            {
                var shrinkShape = FindBlendShape(shrinkShapeName);
                if (shrinkShape != null)
                {
                    tempShapeNames.Add(shrinkShapeName);
                    shrinkShape.AddToMesh(mesh);
                }
            }
            if (!string.IsNullOrEmpty(varicoseShapeName)) 
            {
                var varicoseShape = FindBlendShape(varicoseShapeName);
                if (varicoseShape != null)
                {
                    tempShapeNames.Add(varicoseShapeName);
                    varicoseShape.AddToMesh(mesh);
                }
            }
             
            BlendshapesToTextures muscleShapeBake = new GameObject("temp_shapeBake").AddComponent<BlendshapesToTextures>();
            muscleShapeBake.useObjectSpace = useCustomSkinning;
            muscleShapeBake.renderer = renderer;
            muscleShapeBake.recalculateBlendShapeNormals = true;
            muscleShapeBake.recalculateMeshNormalsForShapeBaseNormals = true;
            muscleShapeBake.targetShapes = tempShapeNames.ToArray();
            muscleShapeBake.storeTangentRebuildDataInUV = false;
            muscleShapeBake.maxOffsetDistance = maxShapeVertexTranslationDistance;
            muscleShapeBake.savePath = textureOutputPath;
            muscleShapeBake.textureName = name + "_" + muscleShapeTextureName;

            muscleShapeBake.mergeIndexUVs = mergeIndexUVs;
            muscleShapeBake.packIndexUVMergeDataIntoSingleFloat = packIndexUVMergeDataIntoSingleFloat;
            muscleShapeBake.uvMergeDataIntoSingleFloatChannelComponentIndex = uvMergeDataIntoSingleFloatChannelComponentIndex;
            muscleShapeBake.indexUVReferenceChannel = indexUVReferenceChannel;
            muscleShapeBake.indexUVMergedTargetChannel = indexUVMergedTargetChannel;
            muscleShapeBake.storeUVMergeInZW = storeUVMergeInZW;

            //muscleShapeBake.mergeNormals = false;
            //muscleShapeBake.mergeTangents = false;

            Debug.Log($"Creating Muscle Shape Texture For {name}...");
            muscleShapeBake.Prepare();
            Texture2D muscleMap = muscleShapeBake.Bake();
            GameObject.DestroyImmediate(muscleShapeBake.gameObject);

            mesh = renderer.sharedMesh;

            // Convert muscle groups to texture masks

            Texture2D[] maskMaps = new Texture2D[muscleGroups.Length];

            Texture2D FindMaskMap(string propertyName)
            {

                for (int a = 0; a < muscleMapPacking.Length; a++) if (propertyName.Contains(muscleMapPacking[a].mapName)) return maskMaps[a];

                return null;

            }

#if UNITY_EDITOR

            for (int a = 0; a < muscleMapPacking.Length; a++)
            {

                var packer = muscleMapPacking[a];

                var groupR = packer.rChannelMuscleGroupIndex < 0 ? null : muscleGroups[packer.rChannelMuscleGroupIndex];
                var groupG = packer.gChannelMuscleGroupIndex < 0 ? null : muscleGroups[packer.gChannelMuscleGroupIndex];
                var groupB = packer.bChannelMuscleGroupIndex < 0 ? null : muscleGroups[packer.bChannelMuscleGroupIndex];
                var groupA = a == muscleMapPacking.Length - 1 ? midpointGroup : (packer.aChannelMuscleGroupIndex < 0 ? null : muscleGroups[packer.aChannelMuscleGroupIndex]);

                Color[] pixels = new Color[maskMapTextureSize * maskMapTextureSize];

                for (int b = 0; b < mesh.vertexCount; b++)
                {

                    Vector4 uv = indexUV[b];

                    int x = Mathf.FloorToInt(uv.x * maskMapTextureSize);
                    int y = Mathf.FloorToInt(uv.y * maskMapTextureSize);

                    int index = (y * maskMapTextureSize) + x;

                    pixels[index] = new Color(groupR == null ? 0 : groupR.weights[b].weight, groupG == null ? 0 : groupG.weights[b].weight, groupB == null ? 0 : groupB.weights[b].weight, groupA == null ? 0 : groupA.weights[b].weight);

                    //Color tempCololr = pixels[index];
                    //tempCololr.a = 1; 
                    //if (a == 0) Debug.DrawRay(renderer.transform.TransformPoint(tempV[b]), renderer.transform.TransformDirection(tempN[b]) * 0.01f, tempCololr, 25);   

                }

                Texture2D texture = new Texture2D(maskMapTextureSize, maskMapTextureSize, outputFormat, false, true);

                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                texture.SetPixels(pixels);

                texture.Apply();

                string textureName = name + "_" + packer.mapName;

                texture.name = textureName;

                maskMaps[a] = texture;

                string fullPath = $"{(textureOutputPath + (textureOutputPath.EndsWith('/') ? "" : "/"))}{textureName}.asset";
                //string fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
                AssetDatabase.CreateAsset(texture, fullPath);
                EditorUtility.SetDirty(texture);

            }

            AssetDatabase.SaveAssets();

#endif

            //

            for (int a = 0; a < materials.Length; a++)
            {

                Material mat = materials[a];

                if (mat == null) continue;

                if (mat.HasProperty(muscleMapMaterialProperty))
                {

                    mat = Instantiate(mat);
                    mat.SetTexture(muscleMapMaterialProperty, muscleMap);
                    mat.SetInt(muscleMapSizeMaterialProperty, muscleMap.width);
                    mat.SetFloat(maxOffsetDistanceMaterialProperty, maxShapeVertexTranslationDistance);
                    mat.SetInt(vertexCountMaterialProperty, mesh.vertexCount);

                    mat.SetTexture(muscleMapMaskArms, FindMaskMap(muscleMapMaskArms));
                    mat.SetTexture(muscleMapMaskUpperTorso, FindMaskMap(muscleMapMaskUpperTorso));
                    mat.SetTexture(muscleMapMaskBack, FindMaskMap(muscleMapMaskBack));
                    mat.SetTexture(muscleMapMaskAbs, FindMaskMap(muscleMapMaskAbs));
                    mat.SetTexture(muscleMapMaskLowerTorso, FindMaskMap(muscleMapMaskLowerTorso));
                    mat.SetTexture(muscleMapMaskUpperLegs, FindMaskMap(muscleMapMaskUpperLegs));
                    mat.SetTexture(muscleMapMaskOther, FindMaskMap(muscleMapMaskOther));

                    materials[a] = mat;

                }

            }

            renderer.sharedMaterials = materials;

            for (int a = 0; a < materials.Length; a++) character.AddMuscleMaterial(materials[a]);

            //

            // Transfer blend shapes

            List<BlendShape> externalBlendShapes = new List<BlendShape>();

            BlendShape AddOrGetBlendShape(string shapeName)
            {

                for (int a = 0; a < externalBlendShapes.Count; a++) if (externalBlendShapes[a].name == shapeName) return externalBlendShapes[a];

                BlendShape newShape = new BlendShape(shapeName);
                externalBlendShapes.Add(newShape);

                return newShape;

            }

            for (int a = 0; a < surfaces.Length; a++)
            {

                var data = surfaceData[a];
                if (data == null) continue;

                for (int b = 0; b < data.blendShapes.Length; b++)
                {

                    var surfaceBlendShape = data.blendShapes[b];
                    BlendShape veinBlendShape = AddOrGetBlendShape(surfaceBlendShape.name);

                    BlendShape.Frame[] frames = new BlendShape.Frame[surfaceBlendShape.frames.Length];
                    for (int c = 0; c < frames.Length; c++) frames[c] = new BlendShape.Frame(veinBlendShape, c, surfaceBlendShape.frames[c].weight, new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount]);

                    veinBlendShape.frames = frames;

                }

            }

            for (int a = 0; a < externalBlendShapes.Count; a++)
            {

                var localShape = externalBlendShapes[a];

                for (int f = 0; f < localShape.frames.Length; f++)
                {

                    var localFrame = localShape.frames[f];

                    for (int b = 0; b < surfaceData.Length; b++)
                    {

                        var data = surfaceData[b];
                        if (data == null) continue;

                        int surfaceShapeIndex = -1;

                        for (int c = 0; c < data.blendShapes.Length; c++)
                        {

                            if (data.blendShapes[c].name != localShape.name) continue;

                            surfaceShapeIndex = c;

                            break;

                        }

                        if (surfaceShapeIndex < 0)
                        {

                            data.ResetDynamicSamples();

                        }
                        else
                        {

                            data.UpdateDynamicSamples(surfaceShapeIndex, localFrame.weight);

                        }

                    }

                    for (int b = 0; b < mesh.vertexCount; b++)
                    {

                        BoneWeight weights = initialBoneWeights[b];

                        var bindingA = weights.boneIndex0 < 0 || weights.weight0 <= 0 ? default : tempData.bindings[weights.boneIndex0];
                        var bindingB = weights.boneIndex1 < 0 || weights.weight1 <= 0 ? default : tempData.bindings[weights.boneIndex1];
                        var bindingC = weights.boneIndex2 < 0 || weights.weight2 <= 0 ? default : tempData.bindings[weights.boneIndex2];
                        var bindingD = weights.boneIndex3 < 0 || weights.weight3 <= 0 ? default : tempData.bindings[weights.boneIndex3];

                        int surfaceIndexA = character.FindSurfaceIndex(bindingA.surfaceName);
                        int surfaceIndexB = character.FindSurfaceIndex(bindingB.surfaceName);
                        int surfaceIndexC = character.FindSurfaceIndex(bindingC.surfaceName);
                        int surfaceIndexD = character.FindSurfaceIndex(bindingD.surfaceName);

                        void AddShapeData(int initialBoneIndex, int vertexIndex, int surfaceIndex, BoneToSurfaceBinding binding, float multiplier)
                        {

                            if (initialBoneIndex < 0 || vertexIndex < 0 || surfaceIndex < 0) return;

                            TempSurfaceData tempData = surfaceData[surfaceIndex];

                            if (tempData.isDefaultData) return;

                            var jobData = tempBindings.boundData[initialBoneIndex];

                            SamplePoint samplePointA = tempData.dynamicSamplePoints[jobData.samplePointIndices.x];
                            SamplePoint samplePointB = tempData.dynamicSamplePoints[jobData.samplePointIndices.y];
                            SamplePoint samplePointC = tempData.dynamicSamplePoints[jobData.samplePointIndices.z];

                            if (!samplePointA.IsReshaped && !samplePointB.IsReshaped && !samplePointC.IsReshaped) return;

                            float3 originalPosition = ((samplePointA.localVertex * jobData.coordinates.x) + (samplePointB.localVertex * jobData.coordinates.y) + (samplePointC.localVertex * jobData.coordinates.z)) + jobData.defaultLocalPosition;
                            VeinsRigged.UpdateVeinRigJob.GetLocalTransform(initialBoneIndex, tempBindings.boundData, tempData.dynamicSamplePoints, out float3 reshapedPosition, out _, out _, out _, out quaternion surfaceRotation, out quaternion directionOffset);

                            localFrame.deltaVertices[vertexIndex] += (Vector3)(reshapedPosition - originalPosition) * multiplier;
                            localFrame.deltaNormals[vertexIndex] += (Vector3)(math.rotate(math.mul(directionOffset, surfaceRotation), normals[vertexIndex]) - (float3)normals[vertexIndex]) * multiplier * (1 - shapeDeltaNormalNullificationFactor);

                        }

                        AddShapeData(weights.boneIndex0, b, surfaceIndexA, bindingA, weights.weight0);
                        AddShapeData(weights.boneIndex1, b, surfaceIndexB, bindingB, weights.weight1);
                        AddShapeData(weights.boneIndex2, b, surfaceIndexC, bindingC, weights.weight2);
                        AddShapeData(weights.boneIndex3, b, surfaceIndexD, bindingD, weights.weight3);

                    }

                }

            }

            //

            bool HasLocalShape(string name)
            {

                for (int a = 0; a < blendShapes.Count; a++) if (blendShapes[a].name == name) return true;

                return false;

            }

            externalBlendShapes.RemoveAll(i => HasLocalShape(i.name));
            blendShapes.AddRange(externalBlendShapes);
            mesh.ClearBlendShapes();
            for (int a = 0; a < blendShapes.Count; a++) blendShapes[a].AddToMesh(mesh);

            if (storeRecalculatedNormals) mesh = useCustomSkinning ? StoreNormalsInUV.Store(mesh, storeNormalsUVChannel, true, true, true, true) : StoreNormalsInUV.StoreRecalculatedNormalsInTangentSpace(mesh, storeNormalsUVChannel, true, true, true);

            mesh.name = name;

            renderer.sharedMesh = null;
            renderer.sharedMesh = mesh;

            tempBindings.Dispose();
            if (useCustomSkinning)
            {

                mesh.ClearBlendShapes();

                CustomSkinnedMeshData meshData = CustomSkinnedMeshData.Create(textureOutputPath, name + "_customSkinning");
                meshData.SetBlendShapes(mesh.vertexCount, blendShapes); 

                meshData.rigId = name;
                meshData.bindpose = mesh.bindposes;

                BoneWeight8Float[] customBoneWeights = new BoneWeight8Float[mesh.vertexCount];

                int i = 0;
                for(int a = 0; a < boneCounts.Length; a++)
                {

                    int count = boneCounts[a];

                    BoneWeight8Float customBW = new BoneWeight8Float();

                    for (int b = 0; b < math.min(8, count); b++)
                    {

                        BoneWeight1 bw = boneWeights[b + i];

                        customBW = customBW.Modify(b, bw.boneIndex, bw.weight);
                        
                    }

                    //customBW = customBW.Modify(0, tempBone, 1);

                    customBoneWeights[a] = customBW;

                    i += count;
                     
                }

                meshData.boneWeights = customBoneWeights;

#if UNITY_EDITOR
                EditorUtility.SetDirty(meshData);
                AssetDatabase.SaveAssetIfDirty(meshData);
#endif

                GameObject newVeinObject = new GameObject(renderer.gameObject.name + "_customSkinning");
                newVeinObject.transform.SetParent(surfaces[0].transform.parent);
                newVeinObject.transform.localPosition = surfaces[0].transform.localPosition;
                newVeinObject.transform.localRotation = surfaces[0].transform.localRotation;
                newVeinObject.transform.localScale = surfaces[0].transform.localScale;

                customRenderer = newVeinObject.AddComponent<CustomSkinnedMeshRenderer>();
                customRenderer.instantiateMaterials = true;
                customRenderer.data = meshData;
                var filter = newVeinObject.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                customRenderer.meshRenderer = newVeinObject.AddOrGetComponent<MeshRenderer>();
                customRenderer.meshRenderer.sharedMaterials = materials;
                customRenderer.SetBones(newBones.ToArray());

                renderer.gameObject.SetActive(false);

            }

        }

        for (int a = 0; a < surfaceData.Length; a++)
        {

            TempSurfaceData data = surfaceData[a];
            if (data == null) continue;

            data.Dispose();

        }

    }

}
