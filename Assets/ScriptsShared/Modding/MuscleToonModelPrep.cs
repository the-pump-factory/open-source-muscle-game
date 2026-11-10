using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

using Swole.DataStructures;
using static Swole.API.Unity.MuscularCharacterSharedData;
using static VeinsToonModelPrep;
using static Swole.API.Unity.MuscularRenderedCharacter;

using Swole;
using Swole.API.Unity;

[ExecuteInEditMode]
public class MuscleToonModelPrep : MonoBehaviour
{

    [Serializable]
    public enum UVSide
    {

        Both, Left, Right

    }

    [Serializable]
    public class MuscleGroup
    {

        public string name;

        public bool hasMirror;
        public int mirroredIndex;

        public Texture2D maskMap;
        public string maskShapeName;

        public UVSide uvSide;

        public RGBAChannel channel;

    }

    [Serializable]
    public struct MuscleShapesIdentifier
    {

        public int shapeMuscleFit;
        public int shapeMuscleMax;
        public int shapeMuscleFlex;
        public int shapeBreastFull;
        public int shapeBreastFullMuscleMax;

    }

    [Serializable]
    public struct MuscleMaskInfo
    {
        public int index;
        public float minShapeDistance, maxShapeDistance;
    }

    [Header("Control")]
    public bool apply;

    public bool applyOnAwake = true;

    public bool flipUvSide = true;

    public string muscularCharacterDataPath = "Assets/Baked/";
    public string muscularCharacterSharedDataFileName = "sharedMuscleMeshData";

    [Header("Veins")]
    public bool optimizeVeinInputMesh = true;
    public bool optimizePreMerge = true;
    public int optVeins_maxEdgeLoopLength = 8;
    public int optVeins_minEdgeLoopLength = 4;
    public float optVeins_maxEdgeToBoneDirDot = 0.6f;
    public float optVeins_minInclusionWeight = 0.15f;
    public float optVeins_junctionBoneDistanceThreshold = 0.001f;
    public float optVeins_minFaceNormalToBoneDirDot = 0.68f;
    public Vector3 optVeins_boneRollAxis = Vector3.up;
    public List<string> optVeins_edgeAffectingShapes;

    public string veinTexturesDataPath = "Assets/Baked/Veins";
    public string veinMuscleShapeTextureName = "muscleShapes";
    public bool veinsCopyNormalsFromSurface = true;
    public float veinsCopyNormalsFromSurfaceFactor = 0.9f;
    public float veinsShapeDeltaNormalNullificationFactor = 1;

    public bool noVeins;
    public bool useRiggedVeins;
    public bool useCustomSkinningForVeins;
    public bool mergeVeinMeshes = true;
    public bool mergeVeinMeshBones = true;
    public float veinMeshBoneMergeMaxDistance = 0.005f;
    public SkinnedMeshRenderer[] veinRenderers;
    public int veins_maxBonesPerVertex = 8;
    public Material[] veinMaterials;
    public bool addVeinOutlineMaterial = false;
    public Material veinOutlineMaterial;

    public MuscleMapPacker[] veinMuscleMapPacking;

    [Header("Options")]
    public GameObject rootGameobject;

    [Serializable]
    public struct DeltaBlendShape
    {
        public string baseShapeName;
        public string deltaShapeName;
    }

    public SkinnedMeshRenderer muscleMaskShapesBaseModel;
    [Serializable]
    public struct MuscleMaskShape
    {
        public string name;
        public int index;
    }
    public MuscleMaskShape[] muscleMaskShapes;

    public DeltaBlendShape[] globalDeltaBlendShapes;
    public SkinnedMeshRenderer[] editableSkinnedMeshRenderers;

    public InputMuscleDependentBlendShape[] muscleDependentBlendShapes;

    public VeinsRiggedSharedData.BlendShapeWeight[] surfaceBlendShapeStates;

    public Material defaultOutlineMaterial;
    public Material muscleOutlineMaterial;
    public Material muscleOutlineMaterialCustomSkinning;

    public const string muscleMapMaterialProperty = "_MuscleMap";
    public const string muscleMapSizeMaterialProperty = "_MuscleMapSize";
    public const string maxOffsetDistanceMaterialProperty = "_MaxOffsetDistance";
    public const string vertexCountMaterialProperty = "_VertexCount";

    public const string muscleMapMaskArms = "_MaskArms";
    public const string muscleMapMaskUpperTorso = "_MaskUpperTorso";
    public const string muscleMapMaskBack = "_MaskBack";
    public const string muscleMapMaskAbs = "_MaskAbs";
    public const string muscleMapMaskLowerTorso = "_MaskLowerTorso";
    public const string muscleMapMaskUpperLegs = "_MaskUpperLegs";
    public const string muscleMapMaskOther = "_MaskOther";

    public const string muscleMaskMapMaterialProperty = "_MuscleMasksMap";
    public const string muscleMaskMapSizeMaterialProperty = "_MuscleMasksMapSize";

    [Serializable]
    public class TargetRenderer
    {

        public bool addOutlineMaterials = true;
        public Material outlineMaterialOverride;

        public bool useCustomSkinning;
        public Material[] customSkinningMaterials;

        public bool solidify;
        public float solidifyThickness;
        [Range(-1, 2)]
        public float solidifyOffset = 0.5f;
        public bool solidifyRecalculateNormals;
        [Tooltip("How far from open edges an inner vertex will retain its line thickness vertex color. Zero means all vertes will retain line thickness.")]
        public int solidifyMaxInnerVertexDepthWithLineThickness = 0;

        public bool createMuscleShapeTexture = true;
        public bool consolidateUVs = true;
        public bool storeRecalculatedNormals = true;

        public SkinnedMeshRenderer renderer;

        public UVChannelURP defaultUVChannel = UVChannelURP.UV1;

        public UVChannelURP storeNormalsUVChannel = UVChannelURP.UV3;

        public bool mergeIndexUVs = true;
        public bool packIndexUVMergeDataIntoSingleFloat = false;
        public int uvMergeDataIntoSingleFloatChannelComponentIndex = 3;

        public UVChannelURP indexUVReferenceChannel = UVChannelURP.UV0;
        public UVChannelURP indexUVMergedTargetChannel = UVChannelURP.UV3;

        public bool storeUVMergeInZW = false;

        public float maxShapeVertexTranslationDistance = 2;

        public bool createMasksFromShapes;

        public string muscleShapeTextureName = "muscleShapes";
        public string maskShapeTextureName = "muscleMasks";

        public BlendshapesToTextures.DeltaShape[] deltaShapes;

        public string[] muscleShapes;

        public MuscleShapesIdentifier muscleShapesIdentifier;

        public MuscleGroup midpointMuscleGroup;

        public MuscleGroup[] muscleGroups;

    }

    public List<TargetRenderer> targetRenderers;

    public List<SkinnedMeshRenderer> nonMuscularSurfaces;

    public bool addOutlineMaterialsToQuickStoreRenderers = true;
    public UVChannelURP quickStoreNormalsUVChannel = UVChannelURP.UV3;

    public List<SkinnedMeshRenderer> renderersToQuickStoreRecalculatedNormals;

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

        if (targetRenderers == null || targetRenderers.Count <= 0) return;

        MuscularCharacterSharedData sharedData = null;
        VeinsRigged veins = null;
        SkinnedMeshRenderer mergedVeinsRenderer = null;

#if UNITY_EDITOR

        sharedData = MuscularCharacterSharedData.Create(muscularCharacterDataPath, muscularCharacterSharedDataFileName);

        if (!noVeins && veinRenderers != null)
        {

            if (mergeVeinMeshes && veinRenderers.Length > 0)
            {

                if (optimizeVeinInputMesh && optimizePreMerge)
                {

                    for (int a = 0; a < veinRenderers.Length; a++) veinRenderers[a].sharedMesh = VeinMeshOptimizer.Optimize(veinRenderers[a], optVeins_edgeAffectingShapes, optVeins_maxEdgeLoopLength, optVeins_minEdgeLoopLength, optVeins_maxEdgeToBoneDirDot, optVeins_minInclusionWeight, optVeins_junctionBoneDistanceThreshold, optVeins_minFaceNormalToBoneDirDot, optVeins_boneRollAxis);

                }  

                var mergedVeinMesh = SkinnedMeshMerge.Merge(out Transform[] mergedVeinRig, out Transform mergedVeinRoot, veinRenderers, mergeVeinMeshBones, veinMeshBoneMergeMaxDistance, false, true);

                mergedVeinMesh.name = gameObject.name + "_mergedVeins";

                mergedVeinsRenderer = Instantiate(veinRenderers[0]);
                mergedVeinsRenderer.transform.position = veinRenderers[0].transform.position;
                mergedVeinsRenderer.transform.rotation = veinRenderers[0].transform.rotation;
                mergedVeinsRenderer.transform.SetParent(mergedVeinRoot, true);
                mergedVeinsRenderer.transform.localScale = veinRenderers[0].transform.localScale;
                mergedVeinsRenderer.sharedMesh = null;
                mergedVeinsRenderer.bones = null; 
                mergedVeinsRenderer.sharedMesh = mergedVeinMesh;
                mergedVeinsRenderer.name = "mergedVeins_renderer";
                mergedVeinsRenderer.bones = mergedVeinRig;
                if (mergedVeinsRenderer.rootBone != null) mergedVeinsRenderer.rootBone = mergedVeinRig.Length == 0 ? null : mergedVeinRig[0];

                if (veinRenderers[0].transform.parent != null && veinRenderers[0].transform.parent.parent != null) mergedVeinRoot.transform.SetParent(veinRenderers[0].transform.parent.parent, true); else mergedVeinRoot.transform.SetParent(transform, true);
                mergedVeinRoot.name = mergedVeinMesh.name;

            }

            if (useRiggedVeins)
            {

                if (mergeVeinMeshes)
                {

                    if (mergedVeinsRenderer != null)
                    {

                        List<SkinnedMeshRenderer> surfaces = new List<SkinnedMeshRenderer>();

                        if (targetRenderers != null) for (int a = 0; a < targetRenderers.Count; a++) surfaces.Add(targetRenderers[a].renderer);
                        if (nonMuscularSurfaces != null) for (int a = 0; a < nonMuscularSurfaces.Count; a++) surfaces.Add(nonMuscularSurfaces[a]);

                        VeinsRiggedSharedData veinData = VeinsRiggedSharedData.Create(veinTexturesDataPath, muscularCharacterSharedDataFileName + $"_veins", mergedVeinsRenderer.bones, surfaces.ToArray(), surfaceBlendShapeStates);

                        veins = mergedVeinsRenderer.gameObject.AddComponent<VeinsRigged>();

                        veins.sharedData = veinData;

                    }

                }
                else
                {

                    for (int r = 0; r < veinRenderers.Length; r++)
                    {

                        var veinsRenderer = veinRenderers[r];

                        if (veinsRenderer != null)
                        {

                            List<SkinnedMeshRenderer> surfaces = new List<SkinnedMeshRenderer>();

                            if (targetRenderers != null) for (int a = 0; a < targetRenderers.Count; a++) surfaces.Add(targetRenderers[a].renderer);
                            if (nonMuscularSurfaces != null) for (int a = 0; a < nonMuscularSurfaces.Count; a++) surfaces.Add(nonMuscularSurfaces[a]);

                            VeinsRiggedSharedData veinData = VeinsRiggedSharedData.Create(veinTexturesDataPath, muscularCharacterSharedDataFileName + $"_veins_{r}", veinsRenderer.bones, surfaces.ToArray(), surfaceBlendShapeStates);

                            veins = veinsRenderer.gameObject.AddComponent<VeinsRigged>();

                            veins.sharedData = veinData;

                        }

                    }

                }

            }

        } 

#endif

        List<SkinnedMeshRenderer> surfaceRenderers = new List<SkinnedMeshRenderer>();
        List<MuscularSurfaceState> muscularSurfaceStates = new List<MuscularSurfaceState>();

        List<CustomSkinnedMeshRenderer> customRenderers = new List<CustomSkinnedMeshRenderer>();
        List<MuscularSurfaceState> customRendererStates = new List<MuscularSurfaceState>();

        if (!noVeins)
        {
            if (mergeVeinMeshes)
            {

                if (mergedVeinsRenderer != null && !useCustomSkinningForVeins)
                {

                    surfaceRenderers.Add(mergedVeinsRenderer);
                    muscularSurfaceStates.Add(MuscularSurfaceState.Veins);

                }

            }
            else if (veinRenderers != null)
            {

                for (int r = 0; r < veinRenderers.Length; r++)
                {

                    var veinsRenderer = veinRenderers[r];

                    if (veinsRenderer == null) continue;

                    surfaceRenderers.Add(veinsRenderer);
                    muscularSurfaceStates.Add(MuscularSurfaceState.Veins);

                }

            }
        }

        Dictionary<string, MuscleMaskInfo> muscleMaskShapesInfo = new Dictionary<string, MuscleMaskInfo>();
        if (muscleMaskShapes != null) 
        {
            if (muscleMaskShapesBaseModel == null)
            {
                foreach(var muscleMaskShape in muscleMaskShapes) muscleMaskShapesInfo[muscleMaskShape.name] = new MuscleMaskInfo() { index = muscleMaskShape.index, minShapeDistance = 0, maxShapeDistance = 1 };
            }
            else // determine weight range for each muscle mask shape using the base model
            {
                var tempShapes = muscleMaskShapesBaseModel.sharedMesh.GetBlendShapes();
                BlendShape FindMuscleMaskShape(string name)
                {
                    foreach(var shape in tempShapes) if (shape.name == name) return shape;

                    return null;
                }
                foreach (var muscleMaskShape in muscleMaskShapes) 
                {
                    float minDist = float.MaxValue;
                    float maxDist = float.MinValue;

                    var shapeInst = FindMuscleMaskShape(muscleMaskShape.name);
                    if (shapeInst == null || shapeInst.frames == null || shapeInst.frames.Length <= 0) 
                    {
                        minDist = 0;
                        maxDist = 1;
                        Debug.LogWarning($"Muscle mask shape {muscleMaskShape.name} had null data for base model!");
                    } 
                    else
                    {
                        var frame = shapeInst.frames[0];
                        for(int a = 0; a < frame.deltaVertices.Length; a++)
                        {
                            var weight = frame.deltaVertices[a].magnitude;
                            minDist = Mathf.Min(minDist, weight);
                            maxDist = Mathf.Max(maxDist, weight);
                        }
                    }

                    //Debug.Log(muscleMaskShape.name + $" : min {minDist}  max {maxDist}");
                    muscleMaskShapesInfo[muscleMaskShape.name] = new MuscleMaskInfo() { index = muscleMaskShape.index, minShapeDistance = minDist, maxShapeDistance = maxDist }; 
                }
            }
        }
        for (int a = 0; a < targetRenderers.Count; a++)
        {

            TargetRenderer tr = targetRenderers[a]; 

            var output = Apply(tr.useCustomSkinning, tr.customSkinningMaterials, sharedData, tr.renderer, 
                tr.solidify, tr.solidifyThickness, tr.solidifyOffset, tr.solidifyRecalculateNormals, tr.solidifyMaxInnerVertexDepthWithLineThickness,
                muscularCharacterDataPath, tr.defaultUVChannel, tr.storeNormalsUVChannel, tr.mergeIndexUVs, tr.packIndexUVMergeDataIntoSingleFloat, tr.uvMergeDataIntoSingleFloatChannelComponentIndex, tr.indexUVReferenceChannel, tr.indexUVMergedTargetChannel, tr.storeUVMergeInZW, tr.maxShapeVertexTranslationDistance, tr.muscleShapeTextureName, tr.deltaShapes, tr.muscleShapes, tr.muscleShapesIdentifier, tr.midpointMuscleGroup, tr.muscleGroups, tr.createMuscleShapeTexture, tr.consolidateUVs, tr.storeRecalculatedNormals, tr.addOutlineMaterials, tr.outlineMaterialOverride == null ? muscleOutlineMaterial : tr.outlineMaterialOverride, flipUvSide, tr.createMasksFromShapes, muscleMaskShapesInfo, tr.maskShapeTextureName);

            if (tr.useCustomSkinning)
            {
                customRenderers.Add(output.customSkinnedMeshRenderer);
                customRendererStates.Add(MuscularSurfaceState.Muscular);
            }
            else
            {
                surfaceRenderers.Add(tr.renderer);
                muscularSurfaceStates.Add(MuscularSurfaceState.Muscular); 
            }

        }

        if (nonMuscularSurfaces != null) for (int a = 0; a < nonMuscularSurfaces.Count; a++)
            {

                surfaceRenderers.Add(nonMuscularSurfaces[a]);
                muscularSurfaceStates.Add(MuscularSurfaceState.NonMuscular);

            }

        if (renderersToQuickStoreRecalculatedNormals != null)
        {

            foreach (SkinnedMeshRenderer renderer in renderersToQuickStoreRecalculatedNormals)
            {

                if (renderer == null) continue;

                Apply(false, null, null, renderer, false, 0, 0, false, 0, null, UVChannelURP.UV1, quickStoreNormalsUVChannel, false, false, 0, UVChannelURP.UV0, UVChannelURP.UV0, false, 0, null, null, null, default, null, null, false, true, true, addOutlineMaterialsToQuickStoreRenderers, defaultOutlineMaterial, flipUvSide, false, null, null);

            }

        }

        GameObject root = rootGameobject == null ? gameObject : rootGameobject;

        if (editableSkinnedMeshRenderers != null)
        {

            Mesh[] editedMeshes = new Mesh[editableSkinnedMeshRenderers.Length];
            Mesh GetEditedMesh(int index)
            {

                var mesh = editedMeshes[index];

                if (mesh != null) return mesh;

                if (editableSkinnedMeshRenderers[index] == null || editableSkinnedMeshRenderers[index].sharedMesh == null) return null;

                mesh = MeshUtils.DuplicateMesh(editableSkinnedMeshRenderers[index].sharedMesh);
                editableSkinnedMeshRenderers[index].sharedMesh = mesh;

                editedMeshes[index] = mesh;

                return mesh;

            }
            List<BlendShape>[] editedShapes = new List<BlendShape>[editedMeshes.Length];
            BlendShape GetEditedShape(int index, string shapeName)
            {

                List<BlendShape> shapes = editedShapes[index];

                if (shapes == null)
                {

                    var mesh = GetEditedMesh(index);
                    if (mesh == null) return null;

                    shapes = mesh.GetBlendShapes();
                    editedShapes[index] = shapes;

                }

                for (int a = 0; a < shapes.Count; a++) if (shapes[a] != null && shapes[a].name == shapeName) return shapes[a];
                shapeName = shapeName.ToLower().Trim();
                for (int a = 0; a < shapes.Count; a++) if (shapes[a] != null && shapes[a].name.ToLower().Trim() == shapeName) return shapes[a];

                return null;

            }

            if (globalDeltaBlendShapes != null)
            {

                foreach(DeltaBlendShape deltaShapeInfo in globalDeltaBlendShapes)
                {

                    if (string.IsNullOrEmpty(deltaShapeInfo.baseShapeName) || string.IsNullOrEmpty(deltaShapeInfo.deltaShapeName)) continue;

                    for(int a = 0; a < editedMeshes.Length; a++)
                    {

                        var baseShape = GetEditedShape(a, deltaShapeInfo.baseShapeName);
                        if (baseShape == null) continue;
                        var deltaShape = GetEditedShape(a, deltaShapeInfo.deltaShapeName);
                        if (deltaShape == null) continue;

                        deltaShape.MakeRelative(baseShape);

                    }

                }

            } 

            for(int a = 0; a < editedMeshes.Length; a++)
            {

                var mesh = editedMeshes[a];

                if (mesh == null) continue;

                var shapes = editedShapes[a];

                if (shapes != null)
                {

                    mesh.ClearBlendShapes();
                    for (int b = 0; b < shapes.Count; b++) if (shapes[b] != null) shapes[b].AddToMesh(mesh);

                }

                if (editableSkinnedMeshRenderers[a] != null) editableSkinnedMeshRenderers[a].sharedMesh = mesh;

            }

        }
         
        MuscularRenderedCharacter character = root.AddComponent<MuscularRenderedCharacter>();

        character.inputData = sharedData;
        character.inputSurfaceRenderers = surfaceRenderers.ToArray();
        character.inputMuscularSurfaceStates = muscularSurfaceStates.ToArray();

        character.inputMuscleDependentBlendShapes = muscleDependentBlendShapes;

        character.Initialize(); 

        if (veins != null)
        {

            veins.character = character;
            veins.Initialize();

        }

#if UNITY_EDITOR

        if (!noVeins && !useRiggedVeins)
        {

            VeinsToonModelPrep veinsPrep = gameObject.AddComponent<VeinsToonModelPrep>();

            TargetRenderer defaultTarget = targetRenderers[0];

            List<VeinsToonModelPrep.TargetRenderer> veinTargets = new List<VeinsToonModelPrep.TargetRenderer>();

            void AddVeinTarget(SkinnedMeshRenderer veinsRenderer) 
            {

                VeinsToonModelPrep.TargetRenderer veinsTarget = new VeinsToonModelPrep.TargetRenderer();
                veinsTarget.maxShapeVertexTranslationDistance = defaultTarget.maxShapeVertexTranslationDistance;
                veinsTarget.muscleGroups = defaultTarget.muscleGroups;
                veinsTarget.renderer = veinsRenderer;
                veinsTarget.maxBonesPerVertex = veins_maxBonesPerVertex;
                veinsTarget.textureOutputPath = veinTexturesDataPath;
                veinsTarget.addOutlineMaterial = addVeinOutlineMaterial;

                veinsTarget.defaultUVChannel = defaultTarget.defaultUVChannel;
                veinsTarget.storeRecalculatedNormals = defaultTarget.storeRecalculatedNormals;
                veinsTarget.storeNormalsUVChannel = defaultTarget.storeNormalsUVChannel;
                veinsTarget.mergeIndexUVs = defaultTarget.mergeIndexUVs;
                veinsTarget.packIndexUVMergeDataIntoSingleFloat = defaultTarget.packIndexUVMergeDataIntoSingleFloat;
                veinsTarget.uvMergeDataIntoSingleFloatChannelComponentIndex = defaultTarget.uvMergeDataIntoSingleFloatChannelComponentIndex;
                veinsTarget.indexUVReferenceChannel = defaultTarget.indexUVReferenceChannel;
                veinsTarget.indexUVMergedTargetChannel = defaultTarget.indexUVMergedTargetChannel;
                veinsTarget.storeUVMergeInZW = defaultTarget.storeUVMergeInZW;

                veinTargets.Add(veinsTarget);

            }

            if (mergeVeinMeshes)
            {

                if (mergedVeinsRenderer != null) AddVeinTarget(mergedVeinsRenderer);

            } 
            else
            {

                for(int r = 0; r < veinRenderers.Length; r++) if (veinRenderers[r] != null) AddVeinTarget(veinRenderers[r]);

            }

            veinsPrep.useCustomSkinning = useCustomSkinningForVeins;
            veinsPrep.character = character;
            veinsPrep.rootGameobject = rootGameobject;
            veinsPrep.surfaceBlendShapeStates = surfaceBlendShapeStates;
            veinsPrep.targetRenderers = veinTargets;
            veinsPrep.muscleMapPacking = veinMuscleMapPacking;
            veinsPrep.flipUvSide = flipUvSide;
            veinsPrep.muscleMaterials = veinMaterials;
            veinsPrep.muscleOutlineMaterial = veinOutlineMaterial;

            veinsPrep.copyNormalsFromSurface = veinsCopyNormalsFromSurface;
            veinsPrep.copyNormalsFromSurfaceFactor = veinsCopyNormalsFromSurfaceFactor;
            veinsPrep.shapeDeltaNormalNullificationFactor = veinsShapeDeltaNormalNullificationFactor;

            veinsPrep.optimizeInputMesh = optimizeVeinInputMesh && (!mergeVeinMeshes || !optimizePreMerge);
            veinsPrep.opt_maxEdgeLoopLength = optVeins_maxEdgeLoopLength;
            veinsPrep.opt_minEdgeLoopLength = optVeins_minEdgeLoopLength;
            veinsPrep.opt_maxEdgeToBoneDirDot = optVeins_maxEdgeToBoneDirDot;
            veinsPrep.opt_minInclusionWeight = optVeins_minInclusionWeight;
            veinsPrep.opt_junctionBoneDistanceThreshold = optVeins_junctionBoneDistanceThreshold;
            veinsPrep.opt_minFaceNormalToBoneDirDot = optVeins_minFaceNormalToBoneDirDot;
            veinsPrep.opt_boneRollAxis = optVeins_boneRollAxis;
            veinsPrep.opt_edgeAffectingShapes = optVeins_edgeAffectingShapes;

            veinsPrep.Apply();
            
            if (useCustomSkinningForVeins)
            {

                //character.inputCustomSkinnedRenderers = veinsPrep.outputCustomRenderers.ToArray();
                //character.inputMuscularCustomSkinnedRendererStates = new MuscularSurfaceState[character.inputCustomSkinnedRenderers.Length];
                //for (int a = 0; a < character.inputMuscularCustomSkinnedRendererStates.Length; a++) character.inputMuscularCustomSkinnedRendererStates[a] = MuscularSurfaceState.Veins;

                for (int a = 0; a < veinsPrep.outputCustomRenderers.Count; a++)
                {
                    customRenderers.Add(veinsPrep.outputCustomRenderers[a]);
                    customRendererStates.Add(MuscularSurfaceState.Veins);
                }

            }

            //character.Initialize();

        }

        if (customRenderers.Count > 0)
        {
            character.inputCustomSkinnedRenderers = customRenderers.ToArray();
            character.inputMuscularCustomSkinnedRendererStates = customRendererStates.ToArray();

            character.Initialize();
        }

#endif

    }

    public struct PrepOutput
    {
        public CustomSkinnedMeshRenderer customSkinnedMeshRenderer;
    }
    public static PrepOutput Apply(bool useCustomSkinning, Material[] customSkinningMaterials, MuscularCharacterSharedData sharedData, SkinnedMeshRenderer renderer, bool solidify, float solidifyThickness, float solidifyOffset, bool solidifyRecalNormals, int solidifyMaxInnerVertexDepthWithLineThickness, string textureOutputPath, UVChannelURP defaultUVChannel, UVChannelURP storeNormalsUVChannel, bool mergeIndexUVs, bool packIndexUVMergeDataIntoSingleFloat, int uvMergeDataIntoSingleFloatChannelComponentIndex, UVChannelURP indexUVReferenceChannel, UVChannelURP indexUVMergedTargetChannel, bool storeUVMergeInZW, float maxShapeVertexTranslationDistance, string muscleShapeTextureName, BlendshapesToTextures.DeltaShape[] deltaShapes, string[] muscleShapes, MuscleShapesIdentifier muscleShapesIdentifier, MuscleGroup midpointMuscleGroup, MuscleGroup[] muscleGroups, bool createMuscleShapeTexture = true, bool consolidateUVs = true, bool storeRecalculatedNormals = true, bool addOutlineMaterials = false, Material outlineMaterial = null, bool flipUvSide = true, bool createMuscleMasksFromShapes = false, Dictionary<string, MuscleMaskInfo> muscleMaskShapesInfo = null, string muscleMaskShapeTextureName = null)
    {

        PrepOutput output = default;

        Material[] materials = useCustomSkinning ? customSkinningMaterials : renderer.sharedMaterials;
        
        if (addOutlineMaterials)
        {

            Material[] temp = new Material[materials.Length * 2];

            materials.CopyTo(temp, 0);

            for (int a = materials.Length; a < temp.Length; a++) temp[a] = outlineMaterial;

            if (!useCustomSkinning) renderer.sharedMaterials = temp;
            materials = temp;

        }

        Mesh mesh = renderer.sharedMesh;
        string name = mesh.name;

        List<Vector4> defaultUV = mesh.GetUVsByChannelAsListV4((int)defaultUVChannel);

        Vector2[] tempUV;

        tempUV = mesh.uv2;
        if (tempUV == null || tempUV.Length == 0) mesh.SetUVs(1, defaultUV);

        tempUV = mesh.uv3;
        if (tempUV == null || tempUV.Length == 0) mesh.SetUVs(2, defaultUV);

        tempUV = mesh.uv4;
        if (tempUV == null || tempUV.Length == 0) mesh.SetUVs(3, defaultUV);

        tempUV = mesh.uv5;
        if (tempUV == null || tempUV.Length == 0) mesh.SetUVs(4, defaultUV);

        #region Solidify

        if (solidify)
        {
            mesh = MeshDataTools.Solidify(mesh, solidifyThickness, solidifyOffset, false, solidifyRecalNormals, true, out var outputVertexInfo);
            if (solidifyMaxInnerVertexDepthWithLineThickness > 0)
            {
                var colors = mesh.colors;
                for(int a = 0; a < outputVertexInfo.Length; a++)
                {
                    var info = outputVertexInfo[a];
                    if (!info.isInnerLayer) continue;

                    var col = colors[a];
                    col.r = col.r * Mathf.Max(0, 1 - (info.distanceFromOpenEdge / (float)solidifyMaxInnerVertexDepthWithLineThickness));  
                    colors[a] = col;
                }
                mesh.colors = colors;
            }
        }
         
        #endregion

        BlendShape[] muscleBlendShapes = null;
        BlendShape[] recalculatedMuscleBlendShape = null;

        if (muscleShapes != null && sharedData != null)
        {

            muscleBlendShapes = new BlendShape[muscleShapes.Length];

            for (int a = 0; a < muscleBlendShapes.Length; a++) muscleBlendShapes[a] = new BlendShape(mesh, muscleShapes[a]);

            recalculatedMuscleBlendShape = MeshUtils.GetRecalculatedBlendShapeNormals(mesh, muscleBlendShapes, null, null, true, true);

            if (deltaShapes != null)
            {

                for(int a = 0; a < deltaShapes.Length; a++)
                {

                    muscleBlendShapes[deltaShapes[a].deltaIndex] = muscleBlendShapes[deltaShapes[a].deltaIndex].AsRelative(muscleBlendShapes[deltaShapes[a].baseIndex]);
                    recalculatedMuscleBlendShape[deltaShapes[a].deltaIndex] = recalculatedMuscleBlendShape[deltaShapes[a].deltaIndex].AsRelative(recalculatedMuscleBlendShape[deltaShapes[a].baseIndex]);

                }

            }

        }

        if (consolidateUVs) mesh = UVConsolidator.Consolidate(mesh, UVChannelURP.UV0, UVChannelURP.UV2); else mesh = MeshUtils.DuplicateMesh(renderer.sharedMesh);

        var normals = mesh.normals;
        for (int a = 0; a < normals.Length; a++) normals[a] = normals[a].normalized;
        mesh.normals = normals;

        var tangents = mesh.tangents;
        for (int a = 0; a < tangents.Length; a++)
        {
            var tan = tangents[a];
            var tanXYZ = new Vector3(tan.x, tan.y, tan.z).normalized;
            tan.x = tanXYZ.x;
            tan.y = tanXYZ.y;
            tan.z = tanXYZ.z;

            tangents[a] = tan; 
        }
        mesh.tangents = tangents;

        renderer.sharedMesh = mesh;  

        if (createMuscleShapeTexture)
        {

            BlendshapesToTextures muscleShapeBake = new GameObject("temp_shapeBake").AddComponent<BlendshapesToTextures>();
            muscleShapeBake.renderer = renderer;
            muscleShapeBake.recalculateBlendShapeNormals = true;
            muscleShapeBake.recalculateMeshNormalsForShapeBaseNormals = true;
            muscleShapeBake.deltaShapes = deltaShapes;
            muscleShapeBake.targetShapes = muscleShapes;
            muscleShapeBake.storeTangentRebuildDataInUV = false;
            muscleShapeBake.maxOffsetDistance = maxShapeVertexTranslationDistance;
            muscleShapeBake.savePath = textureOutputPath; 
            muscleShapeBake.textureName = muscleShapeTextureName;

            muscleShapeBake.mergeNormals = false;
            muscleShapeBake.mergeTangents = false;
            
            muscleShapeBake.mergeIndexUVs = mergeIndexUVs;
            muscleShapeBake.packIndexUVMergeDataIntoSingleFloat = packIndexUVMergeDataIntoSingleFloat;
            muscleShapeBake.uvMergeDataIntoSingleFloatChannelComponentIndex = uvMergeDataIntoSingleFloatChannelComponentIndex;
            muscleShapeBake.indexUVReferenceChannel = indexUVReferenceChannel;
            muscleShapeBake.indexUVMergedTargetChannel = indexUVMergedTargetChannel;
            muscleShapeBake.storeUVMergeInZW = storeUVMergeInZW;

            Debug.Log($"Creating Muscle Shape Texture For {name}...");
            muscleShapeBake.Prepare();
            Texture2D muscleMap = muscleShapeBake.Bake();
            GameObject.DestroyImmediate(muscleShapeBake.gameObject);

            mesh = renderer.sharedMesh;

            for(int a = 0; a < materials.Length; a++)
            {

                Material mat = materials[a];

                if (mat.HasProperty(muscleMapMaterialProperty))
                {

                    mat = Instantiate(mat);
                    mat.SetTexture(muscleMapMaterialProperty, muscleMap);
                    mat.SetInt(muscleMapSizeMaterialProperty, muscleMap.width); 
                    mat.SetFloat(maxOffsetDistanceMaterialProperty, maxShapeVertexTranslationDistance);
                    mat.SetInt(vertexCountMaterialProperty, mesh.vertexCount);

                    materials[a] = mat;

                }

            }

            if (!useCustomSkinning) renderer.sharedMaterials = materials;

        }

        if (storeRecalculatedNormals) mesh = StoreNormalsInUV.StoreRecalculatedNormalsInTangentSpace(mesh, storeNormalsUVChannel, true, true, true);
        
        if (sharedData != null)
        {
            
            SurfaceShapeData shapeData = new SurfaceShapeData() { surfaceName = name, vertexCount = mesh.vertexCount };

            if (muscleBlendShapes != null && recalculatedMuscleBlendShape != null) 
            {

                if (muscleShapesIdentifier.shapeMuscleFit >= 0) shapeData.shapeMuscleFit = ShapeData.FromBlendShape(muscleBlendShapes[muscleShapesIdentifier.shapeMuscleFit], recalculatedMuscleBlendShape[muscleShapesIdentifier.shapeMuscleFit]);
                if (muscleShapesIdentifier.shapeMuscleMax >= 0) shapeData.shapeMuscleMax = ShapeData.FromBlendShape(muscleBlendShapes[muscleShapesIdentifier.shapeMuscleMax], recalculatedMuscleBlendShape[muscleShapesIdentifier.shapeMuscleMax]);
                if (muscleShapesIdentifier.shapeMuscleFlex >= 0) shapeData.shapeMuscleFlex = ShapeData.FromBlendShape(muscleBlendShapes[muscleShapesIdentifier.shapeMuscleFlex], recalculatedMuscleBlendShape[muscleShapesIdentifier.shapeMuscleFlex]);
                if (muscleShapesIdentifier.shapeBreastFull >= 0) shapeData.shapeBreastFull = ShapeData.FromBlendShape(muscleBlendShapes[muscleShapesIdentifier.shapeBreastFull], recalculatedMuscleBlendShape[muscleShapesIdentifier.shapeBreastFull]);
                if (muscleShapesIdentifier.shapeBreastFullMuscleMax >= 0) shapeData.shapeBreastFullMuscleMax = ShapeData.FromBlendShape(muscleBlendShapes[muscleShapesIdentifier.shapeBreastFullMuscleMax], recalculatedMuscleBlendShape[muscleShapesIdentifier.shapeBreastFullMuscleMax]);

            }

            sharedData.AddSurfaceShapeData(shapeData);

            Vector4[] uvs = mesh.GetUVsByChannelV4((int)indexUVMergedTargetChannel);

            if (muscleGroups != null)
            {

                List<BlendShape> muscleMaskShapes = null;
                if (createMuscleMasksFromShapes)
                {
                    var shapes = mesh.GetBlendShapes();

                    var emptyBlendShapeData = new Vector3[mesh.vertexCount];
                    muscleMaskShapes = new List<BlendShape>();
                    foreach(var pair in muscleMaskShapesInfo)
                    {
                        var maskInfo = pair.Value;
                        BlendShape muscleMaskShape = null;
                        foreach(var shape in shapes)
                        {
                            if (shape.name.AsID() == pair.Key.AsID())
                            {
                                muscleMaskShape = shape;
                                break;
                            }
                        }

                        if (muscleMaskShape == null)
                        {
                            muscleMaskShape = new BlendShape();
                            muscleMaskShape.frames = new BlendShape.Frame[1];

                            var frame = new BlendShape.Frame(muscleMaskShape, 0, 0, emptyBlendShapeData, emptyBlendShapeData, emptyBlendShapeData);
                            muscleMaskShape.frames[0] = frame;
                        } 
                        else
                        {
                            var frame = muscleMaskShape.frames[0];
                            var verts = mesh.vertices;
                            for (int b = 0; b < mesh.vertexCount; b++)
                            {
                                float distanceWeight = Mathf.Clamp01((frame.deltaVertices[b].magnitude - maskInfo.minShapeDistance) / (maskInfo.maxShapeDistance - maskInfo.minShapeDistance));
                                /*float distanceWeight = Mathf.Max(0, (frame.deltaVertices[b].magnitude - maskInfo.minShapeDistance) / (maskInfo.maxShapeDistance - maskInfo.minShapeDistance));
                                if (distanceWeight > 1) 
                                { 
                                    Debug.LogWarning($"Greater than ONE {muscleMaskShape.name}, {b}, {distanceWeight}");
                                    Debug.DrawRay(renderer.transform.TransformPoint(verts[b]), Vector3.up, Color.red, 10000);
                                }*/
                                frame.deltaVertices[b] = new Vector3(distanceWeight, distanceWeight, distanceWeight); // store the weights in the shape instance for later

                            }

                        }

                        muscleMaskShape.name = pair.Key;
                        muscleMaskShapes.Add(muscleMaskShape);
                    }

                    #region Average muscle mask weights
                    /*int[] weightContributorCounts = new int[mesh.vertexCount];
                    foreach(var muscleMaskShape in muscleMaskShapes)
                    {
                        var frame = muscleMaskShape.frames[0];
                        for(int a = 0; a < mesh.vertexCount; a++)
                        {
                            var weight = frame.deltaVertices[a].x;
                            if (weight > 0) weightContributorCounts[a] = weightContributorCounts[a] + 1;
                        }
                    }
                    foreach (var muscleMaskShape in muscleMaskShapes)
                    {
                        var frame = muscleMaskShape.frames[0];
                        for (int a = 0; a < mesh.vertexCount; a++)
                        {
                            if (weightContributorCounts[a] > 1) frame.deltaVertices[a] = frame.deltaVertices[a] / weightContributorCounts[a];
                        }
                    }*/
                    #endregion
                    #region Normalize muscle mask weights
                    float[] weightTotals = new float[mesh.vertexCount];
                    foreach(var muscleMaskShape in muscleMaskShapes)
                    {
                        if (muscleMaskShape.name == midpointMuscleGroup.maskShapeName) continue;

                        var frame = muscleMaskShape.frames[0];
                        for(int a = 0; a < mesh.vertexCount; a++)
                        {
                            var weight = frame.deltaVertices[a].x;
                            weightTotals[a] = weightTotals[a] + weight;
                        }
                    }
                    foreach (var muscleMaskShape in muscleMaskShapes)
                    {
                        if (muscleMaskShape.name == midpointMuscleGroup.maskShapeName) continue;

                        var frame = muscleMaskShape.frames[0];
                        for (int a = 0; a < mesh.vertexCount; a++)
                        {
                            if (weightTotals[a] > 1) frame.deltaVertices[a] = frame.deltaVertices[a] / weightTotals[a]; 
                        }
                    }
                    #endregion

                    foreach (var maskShape in muscleMaskShapes) shapes.Remove(maskShape); // remove mask shapes from mesh

                    muscleMaskShapes.Sort((BlendShape x, BlendShape y) => (int)Mathf.Sign(muscleMaskShapesInfo[x.name].index - muscleMaskShapesInfo[y.name].index));

                    mesh.ClearBlendShapes();
                    foreach (var shape in shapes) shape.AddToMesh(mesh);
                }
                SurfaceBoundWeights BindSurfaceWeights(MuscleToonModelPrep.MuscleGroup dataGroup)
                {

                    SurfaceBoundWeights surfaceWeights = new SurfaceBoundWeights();

                    surfaceWeights.surfaceName = name;
                    surfaceWeights.vertexCount = mesh.vertexCount;

                    List<WeightIndexPair> weights = new List<WeightIndexPair>();

                    if (createMuscleMasksFromShapes)
                    {
                        BlendShape muscleMaskShape = null;
                        if (muscleMaskShapes != null && !string.IsNullOrWhiteSpace(dataGroup.maskShapeName))
                        {
                            foreach (var shape in muscleMaskShapes) if (shape.name == dataGroup.maskShapeName)
                                {
                                    muscleMaskShape = shape;
                                    break;
                                }
                        }

                        if (muscleMaskShape != null && muscleMaskShape.frames != null && muscleMaskShape.frames.Length > 0)
                        {
                            var frame = muscleMaskShape.frames[0];
                            for (int b = 0; b < surfaceWeights.vertexCount; b++)
                            {

                                Vector2 uv = storeUVMergeInZW ? new Vector2(uvs[b].z, uvs[b].w) : new Vector2(uvs[b].x, uvs[b].y);

                                float weight = frame.deltaVertices[b].magnitude;
                                weight = dataGroup == null ? 0 : (dataGroup.uvSide == UVSide.Both || ((dataGroup.uvSide == (flipUvSide ? UVSide.Right : UVSide.Left) && uv.x < 0.5f) || (dataGroup.uvSide == (flipUvSide ? UVSide.Left : UVSide.Right) && uv.x >= 0.5f)) ? weight : 0);
                                if (weight > 0) weights.Add(new WeightIndexPair() { index = b, weight = weight }); 

                            }
                        }
                    }
                    else
                    {
                        Color[] pixels = dataGroup == null ? null : dataGroup.maskMap == null ? null : dataGroup.maskMap.GetPixels();
                        int width = dataGroup == null || dataGroup.maskMap == null ? 0 : dataGroup.maskMap.width;
                        int height = dataGroup == null || dataGroup.maskMap == null ? 0 : dataGroup.maskMap.height;

                        for (int b = 0; b < surfaceWeights.vertexCount; b++)
                        {
                             
                            Vector2 uv = storeUVMergeInZW ? new Vector2(uvs[b].z, uvs[b].w) : new Vector2(uvs[b].x, uvs[b].y);

                            int x = Mathf.RoundToInt((width - 1) * uv.x);
                            int y = Mathf.RoundToInt((height - 1) * uv.y);

                            int index = (y * width) + x;

                            Color pixel = pixels == null ? Color.clear : pixels[index];

                            float weight = dataGroup == null ? 0 : (dataGroup.uvSide == UVSide.Both || ((dataGroup.uvSide == (flipUvSide ? UVSide.Right : UVSide.Left) && uv.x < 0.5f) || (dataGroup.uvSide == (flipUvSide ? UVSide.Left : UVSide.Right) && uv.x >= 0.5f)) ? pixel.GetChannel(dataGroup.channel) : 0);

                            if (weight > 0) weights.Add(new WeightIndexPair() { index = b, weight = weight });

                        }
                    }

                    surfaceWeights.weights = weights.ToArray();

                    return surfaceWeights;

                }

                if (sharedData.midpointMuscleGroup == null) sharedData.midpointMuscleGroup = new MuscularCharacterSharedData.MuscleGroup() { name = "midpoint" }; 
                var midpointSurfaceBoundWeights = BindSurfaceWeights(midpointMuscleGroup);
                sharedData.midpointMuscleGroup.AddSurfaceWeights(midpointSurfaceBoundWeights);
                sharedData.midpointMuscleGroup.mirroredIndex = -1;

                Dictionary<int, int> indexTranslator = new Dictionary<int, int>();
                for (int a = 0; a < muscleGroups.Length; a++)
                {

                    var dataGroup = muscleGroups[a];

                    var muscleGroup = sharedData.AddOrGetMuscleGroup(dataGroup.name, out int groupIndex);
                    indexTranslator[a] = groupIndex;

                    muscleGroup.AddSurfaceWeights(BindSurfaceWeights(dataGroup));

                }

                for (int a = 0; a < muscleGroups.Length; a++) if (muscleGroups[a].hasMirror && muscleGroups[a].mirroredIndex >= 0) sharedData.muscleGroups[indexTranslator[a]].mirroredIndex = indexTranslator[muscleGroups[a].mirroredIndex];

                if (createMuscleShapeTexture)
                {

                    Debug.Log($"Creating Muscle Mask Shape Texture For {name}..."); 

                    int totalVertexCount = mesh.vertexCount * Mathf.CeilToInt(muscleMaskShapes.Count / 4f); 
                    int textureSize = Maths.AsPowerOf2(Mathf.CeilToInt(Mathf.Sqrt(totalVertexCount)));

                    int pixelCount = textureSize * textureSize;
                    Color[] pixels = new Color[pixelCount];

                    for(int maskIndex = 0; maskIndex < muscleMaskShapes.Count; maskIndex++)
                    {
                        int packIndex = (maskIndex / 4) * mesh.vertexCount;
                        int channelIndex = maskIndex % 4; 

                        var muscleMaskShape = muscleMaskShapes[maskIndex];
                        var frame = muscleMaskShape.frames[0];
                        Debug.Log(muscleMaskShape.name + $": packIndex {packIndex}  channelIndex {channelIndex}"); 
                        for (int vIndex = 0; vIndex < mesh.vertexCount; vIndex++)
                        {
                            int pixelIndex = packIndex + vIndex;
                            var weight = frame.deltaVertices[vIndex].x; // data has previously been converted into weight floats
                            var pixel = pixels[pixelIndex];
                            pixels[pixelIndex] = new Color(channelIndex == 0 ? weight : pixel.r, channelIndex == 1 ? weight : pixel.g, channelIndex == 2 ? weight : pixel.b, channelIndex == 3 ? weight : pixel.a);
                        }
                    }

                    var muscleMaskMap = new Texture2D(textureSize, textureSize, TextureFormat.RGBA64, false, true);
                    muscleMaskMap.name = muscleMaskShapeTextureName;

                    muscleMaskMap.filterMode = FilterMode.Point;
                    muscleMaskMap.wrapMode = TextureWrapMode.Clamp; 

                    muscleMaskMap.SetPixels(pixels);
                    muscleMaskMap.Apply();

#if UNITY_EDITOR
                    UnityEditor.AssetDatabase.CreateAsset(muscleMaskMap, (textureOutputPath.StartsWith("Assets") ? "" : "Assets/") + textureOutputPath + (textureOutputPath.EndsWith("/") ? "" : "/") + $"{muscleMaskShapeTextureName}.asset");
#endif

                    for (int a = 0; a < materials.Length; a++)
                    {

                        Material mat = materials[a];

                        if (mat.HasProperty(muscleMaskMapMaterialProperty))
                        {

                            //mat = Instantiate(mat);
                            mat.SetTexture(muscleMaskMapMaterialProperty, muscleMaskMap);
                            mat.SetInt(muscleMaskMapSizeMaterialProperty, muscleMaskMap.width);
                            mat.SetInt(vertexCountMaterialProperty, mesh.vertexCount);

                            materials[a] = mat;

                        }

                    }

                    if (!useCustomSkinning) renderer.sharedMaterials = materials;
                }

            }

#if UNITY_EDITOR

            if (sharedData != null) UnityEditor.EditorUtility.SetDirty(sharedData);

#endif

        }

        mesh.name = name;
        if (!useCustomSkinning) renderer.sharedMesh = mesh;

        if (useCustomSkinning)
        {
            var blendShapes = mesh.GetBlendShapes();
            mesh.ClearBlendShapes();

            CustomSkinnedMeshData meshData = CustomSkinnedMeshData.Create(textureOutputPath, name + "_customSkinning");
            meshData.SetBlendShapes(mesh.vertexCount, blendShapes);

            meshData.rigId = name;
            meshData.bindpose = mesh.bindposes;

            BoneWeight8Float[] customBoneWeights = new BoneWeight8Float[mesh.vertexCount];

            var boneCounts = mesh.GetBonesPerVertex();
            var boneWeights = mesh.GetAllBoneWeights();
            int i = 0;
            for (int a = 0; a < boneCounts.Length; a++)
            {
                int count = boneCounts[a];

                BoneWeight8Float customBW = new BoneWeight8Float();

                for (int b = 0; b < math.min(8, count); b++)
                {

                    BoneWeight1 bw = boneWeights[b + i];

                    customBW = customBW.Modify(b, bw.boneIndex, bw.weight);

                }

                customBoneWeights[a] = customBW;

                i += count;
            }
            boneCounts.Dispose();
            boneWeights.Dispose();

            meshData.boneWeights = customBoneWeights;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(meshData);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(meshData);
#endif

            GameObject newRendererObject = new GameObject(renderer.gameObject.name + "_customSkinning");
            newRendererObject.transform.SetParent(renderer.transform.parent);
            newRendererObject.transform.localPosition = renderer.transform.localPosition;
            newRendererObject.transform.localRotation = renderer.transform.localRotation;
            newRendererObject.transform.localScale = renderer.transform.localScale;

            var customRenderer = newRendererObject.AddComponent<CustomSkinnedMeshRenderer>();
            customRenderer.instantiateMaterials = true;
            customRenderer.data = meshData;
            var filter = newRendererObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            customRenderer.meshRenderer = newRendererObject.AddOrGetComponent<MeshRenderer>();
            customRenderer.meshRenderer.sharedMaterials = materials;
            customRenderer.SetBones(renderer.bones);

            renderer.gameObject.SetActive(false);
            output.customSkinnedMeshRenderer = customRenderer;
        }

#if UNITY_EDITOR

        if (sharedData != null) UnityEditor.AssetDatabase.SaveAssetIfDirty(sharedData);

#endif

        return output;

    }

}
