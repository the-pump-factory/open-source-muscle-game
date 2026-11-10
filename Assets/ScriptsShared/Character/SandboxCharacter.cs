using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Jobs;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

using Swole;
using Swole.Morphing;
using Swole.API.Unity;
using Swole.API.Unity.Animation;


public class SandboxCharacter : MonoBehaviour, IUserCustomizable, IRaycastTarget
{

#if UNITY_EDITOR
    protected void OnValidate()
    {
        if (cacheFeatureTransforms)
        {
            cacheFeatureTransforms = false;
            CacheFeatureTransforms(); 
        }

        if (renameFeatureGroupReference || renameFeatureShapeReference)
        {
            try
            {
                if (customizableFeatures != null)
                {
                    for (int a = 0; a < customizableFeatures.Count; a++)
                    {
                        var feature = customizableFeatures[a];
                        if (string.IsNullOrWhiteSpace(feature.optionId)) continue;

                        int splitterIndex = feature.optionId.IndexOf(':');
                        if (splitterIndex < 0) continue;

                        string groupName = feature.optionId.Substring(0, splitterIndex);
                        string shapeName = feature.optionId.Substring(splitterIndex + 1);

                        if (renameFeatureGroupReference)
                        {
                            if (groupName == inputStringA) groupName = inputStringB;
                        }
                        else if (renameFeatureShapeReference)
                        {
                            if (shapeName == inputStringA) shapeName = inputStringB;
                        }

                        feature.optionId = $"{groupName}:{shapeName}";
                        customizableFeatures[a] = feature;
                    }
                }
            }
            finally
            {
                renameFeatureGroupReference = false;
                renameFeatureShapeReference = false;
            }
        }
    }
#endif

    /*[SerializeField]
    protected BulkOutCharacter boCharacter;
    [SerializeField]
    protected BasicBulkOutCharacter basicBoCharacter;

    [SerializeField]
    protected IBulkOutCharacter character;
    public void SetCharacter(IBulkOutCharacter character)
    {
        this.character = character;
    }*/

    public CustomizableCharacterMeshCollection characterMeshes;

    [NonSerialized]
    protected ICustomizableCharacter mainCharacterMesh;
    [SerializeField]
    protected string mainCharacterMeshID;
    public void SetMainCharacterMesh(string id)
    {
        mainCharacterMeshID = id;
        if (characterMeshes != null)
        {
            characterMeshes.TryGetMesh(id, out mainCharacterMesh);
        }
    }

    [NonSerialized]
    protected List<ICustomizableCharacter> characterSkinMeshes = new List<ICustomizableCharacter>();
    public void AddCharacterSkinMesh(ICustomizableCharacter mesh)
    {
        if (characterSkinMeshes == null) characterSkinMeshes = new List<ICustomizableCharacter>();
        if (!characterSkinMeshes.Contains(mesh)) characterSkinMeshes.Add(mesh);
    }
    public void RemoveCharacterSkinMesh(ICustomizableCharacter mesh)
    {
        if (characterSkinMeshes == null) return;
        characterSkinMeshes.Remove(mesh);
    }

    [SerializeField]
    protected BipedalCharacterHeight characterHeight;
    public BipedalCharacterHeight CharacterHeight => characterHeight;
    public void SetCharacterHeightController(BipedalCharacterHeight controller)
    {
        characterHeight = controller;
    }

    [SerializeField]
    protected CustomAnimator animator;
    public void SetAnimator(CustomAnimator animator)
    {
        if (this.animator != null) 
        { 
            this.animator.RemoveListener(CustomAnimator.BehaviourEvent.OnPostLateUpdate, PostAnimate);
            this.animator.RemoveListener(CustomAnimator.BehaviourEvent.OnEnable, OnEnableAnimator);
            this.animator.RemoveListener(CustomAnimator.BehaviourEvent.OnDisable, OnDisableAnimator);
        }
        this.animator = animator;

        if (animator != null) 
        { 
            animator.AddListener(CustomAnimator.BehaviourEvent.OnPostLateUpdate, PostAnimate);
            animator.AddListener(CustomAnimator.BehaviourEvent.OnEnable, OnEnableAnimator);
            animator.AddListener(CustomAnimator.BehaviourEvent.OnDisable, OnDisableAnimator);
        }
    }

    protected virtual void OnDestroy()
    {
        if (animator != null)
        {
            animator.RemoveListener(CustomAnimator.BehaviourEvent.OnPostLateUpdate, PostAnimate);
            animator.RemoveListener(CustomAnimator.BehaviourEvent.OnEnable, OnEnableAnimator);
            animator.RemoveListener(CustomAnimator.BehaviourEvent.OnDisable, OnDisableAnimator);
        }

        if (featureControllers != null)
        {
            foreach(var entry in featureControllers)
            {
                if (entry.Value != null)
                {
                    entry.Value.Dispose();
                }
            }

            featureControllers.Clear();
        }

        if (cachedTransformChanges != null)
        {
            foreach(var entry in cachedTransformChanges)
            {
                if (entry.Value != null)
                {
                    entry.Value.Dispose();
                }
            }

            cachedTransformChanges.Clear();
        }

        if (eyeballLeftMaterial != null)
        {
            if (ReferenceEquals(eyeballLeftMaterial, eyeballRightMaterial)) eyeballRightMaterial = null;
            Destroy(eyeballLeftMaterial);
            eyeballLeftMaterial = null;
        }
        if (eyeballRightMaterial != null)
        {
            Destroy(eyeballRightMaterial);
            eyeballRightMaterial = null;
        }

        if (hairMaterials != null)
        {
            foreach (var mat in hairMaterials) if (mat != null) Destroy(mat);
            hairMaterials = null;
        }
    }

    [SerializeField]
    protected GenitalController genitalController;
    public void SetGenitalController(GenitalController controller)
    {
        genitalController = controller;
    }

    public void SetHideNipples(bool hide)
    {
        if (mainCharacterMesh != null) mainCharacterMesh.SetHideNipples(hide);
    }

    [SerializeField]
    protected bool useMaleGenitalia;
    public bool UseMaleGenitalia
    {
        get => useMaleGenitalia;
        set
        {
            useMaleGenitalia = value;
            SetHideGenitals(mainCharacterMesh == null ? true : mainCharacterMesh.HideGenitals);
        }
    }

    public void SetHideGenitals(bool hide)
    {
        if (mainCharacterMesh != null) mainCharacterMesh.SetHideGenitals(hide);
        if (genitalController != null)
        {
            if (hide)
            {
                if (genitalController.vulvaMesh != null) genitalController.vulvaMesh.gameObject.SetActive(false);
                if (genitalController.penisMesh != null) genitalController.penisMesh.gameObject.SetActive(false);
            } 
            else
            {
                if (genitalController.vulvaMesh != null) 
                { 
                    genitalController.vulvaMesh.gameObject.SetActive(!useMaleGenitalia);
                    genitalController.vulvaMesh.enabled = !useMaleGenitalia;
                }
                if (genitalController.penisMesh != null) 
                {
                    genitalController.penisMesh.gameObject.SetActive(useMaleGenitalia);
                    genitalController.penisMesh.enabled = useMaleGenitalia; 
                }
            }
        }
    }

    public void SyncGenitalTypeAndSize()
    {
        if (genitalController == null) return;

        float bustSize = math.lerp(characterBustSizeRange.x, characterBustSizeRange.y, characterBustSize);

        genitalController.SetBustSize(bustSize);
    }

    [UserCustomizable]
    protected float characterBustSize;
    public float CharacterBustSize => characterBustSize;
    public Vector2 characterBustSizeRange = new Vector2(0f, 1.5f);

    public void SetBustSize(float val)
    {
        characterBustSize = val;
        SyncGenitalTypeAndSize();
    }
        
    [UserCustomizable]
    protected float characterBaseHeight;
    public float CharacterBaseHeight => characterBaseHeight;
    [SerializeField]
    protected Vector2 characterBaseHeightRange = new Vector2(0.9f, 1.08f);

    [UserCustomizable]
    protected float characterNeckLength;
    public float CharacterNeckLength => characterNeckLength;
    [SerializeField]
    protected Vector2 characterNeckLengthRange = new Vector2(0.88f, 1.05f);

    [UserCustomizable]
    protected float characterSpineLength;
    public float CharacterSpineLength => characterSpineLength;
    [SerializeField]
    protected Vector2 characterSpineLengthRange = new Vector2(0.88f, 1.05f);

    [UserCustomizable]
    protected float characterShoulderWidth;
    public float CharacterShoulderWidth => characterShoulderWidth;
    [SerializeField]
    protected Vector2 characterShoulderWidthRange = new Vector2(0.7f, 1.5f);

    [UserCustomizable]
    protected float characterUpperArmLength;
    public float CharacterUpperArmLength => characterUpperArmLength;
    [SerializeField]
    protected Vector2 characterUpperArmLengthRange = new Vector2(0.88f, 1.05f);

    [UserCustomizable]
    protected float characterForearmLength;
    public float CharacterForearmLength => characterForearmLength;
    [SerializeField]
    protected Vector2 characterForearmLengthRange = new Vector2(0.88f, 1.05f);

    [UserCustomizable]
    protected float characterHipWidth;
    public float CharacterHipWidth => characterHipWidth;
    [SerializeField]
    protected Vector2 characterHipWidthRange = new Vector2(0.65f, 1.2f);

    [UserCustomizable]
    protected float characterThighLength;
    public float CharacterThighLength => characterThighLength;
    [SerializeField]
    protected Vector2 characterThighLengthRange = new Vector2(0.88f, 1.05f);

    [UserCustomizable]
    protected float characterCalfLength;
    public float CharacterCalfLength => characterCalfLength;
    [SerializeField]
    protected Vector2 characterCalfLengthRange = new Vector2(0.88f, 1.05f);

    public void SyncCharacterHeight()
    {
        if (characterHeight == null) return;

        float baseHeight = Maths.BlendRange(1f, characterBaseHeightRange.x, characterBaseHeightRange.y, characterBaseHeight);

        float neckLength = baseHeight;
        float spineLength = baseHeight;
        float upperArmLength = baseHeight;
        float forearmLength = baseHeight;
        float thighLength = baseHeight;
        float calfLength = baseHeight;

        neckLength += Maths.BlendRange(1f, characterNeckLengthRange.x, characterNeckLengthRange.y, characterNeckLength) - 1f;
        spineLength += Maths.BlendRange(1f, characterSpineLengthRange.x, characterSpineLengthRange.y, characterSpineLength) - 1f;
        upperArmLength += Maths.BlendRange(1f, characterUpperArmLengthRange.x, characterUpperArmLengthRange.y, characterUpperArmLength) - 1f;
        forearmLength += Maths.BlendRange(1f, characterForearmLengthRange.x, characterForearmLengthRange.y, characterForearmLength) - 1f;
        thighLength += Maths.BlendRange(1f, characterThighLengthRange.x, characterThighLengthRange.y, characterThighLength) - 1f;
        calfLength += Maths.BlendRange(1f, characterCalfLengthRange.x, characterCalfLengthRange.y, characterCalfLength) - 1f;

        characterHeight.SetNeckLength(neckLength);
        characterHeight.SetSpineLength(spineLength);
        characterHeight.SetShoulderWidth(Maths.BlendRange(1f, characterShoulderWidthRange.x, characterShoulderWidthRange.y, characterShoulderWidth));
        characterHeight.SetArmLength(upperArmLength);
        characterHeight.SetForearmLength(forearmLength);
        characterHeight.SetHipWidth(Maths.BlendRange(1f, characterHipWidthRange.x, characterHipWidthRange.y, characterHipWidth));
        characterHeight.SetLegLength(thighLength);
        characterHeight.SetCalfLength(calfLength);
    }

    public void SetBaseHeight(float val)
    {
        characterBaseHeight = val;
        SyncCharacterHeight();
    }
    public void SetNeckLength(float val)
    {
        characterNeckLength = val;
        SyncCharacterHeight();
    }
    public void SetSpineLength(float val)
    {
        characterSpineLength = val;
        SyncCharacterHeight();
    }
    public void SetShoulderWidth(float val)
    {
        characterShoulderWidth = val;
        SyncCharacterHeight();
    }
    public void SetUpperArmLength(float val)
    {
        characterUpperArmLength = val;
        SyncCharacterHeight();
    }
    public void SetForearmLength(float val)
    {
        characterForearmLength = val;
        SyncCharacterHeight();
    }
    public void SetHipWidth(float val)
    {
        characterHipWidth = val;
        SyncCharacterHeight();
    }
    public void SetThighLength(float val)
    {
        characterThighLength = val;
        SyncCharacterHeight();
    }
    public void SetCalfLength(float val)
    {
        characterCalfLength = val;
        SyncCharacterHeight();
    }

    public Transform headBone;
    public bool resetHeadBoneTransform;
    public Vector2 headScaleOffsetRange;

    public Transform leftHandBone;
    public bool resetHandBoneTransforms;
    public Vector2 handScaleOffsetRange;

    public Transform leftFootBone;
    public bool resetFootBoneTransforms;
    public Vector2 footScaleOffsetRange;

    public const string _headScaleId = "head_size";
    public const string _handScaleId = "hand_size";
    public const string _footScaleId = "foot_size";

    [Serializable]
    public enum CustomizableOptionType
    {
        None, Shape, VariationLeft, VariationRight, VariationBoth
    }
    [Serializable]
    public struct TransformOffsets
    {
        public string targetTransformName;
        public Transform targetTransform;
        public bool skipTransformReset;
        public Vector3 minOffset;
        public Vector3 maxOffset;
        public Vector3 minRotationOffset;
        public Vector3 maxRotationOffset;
        public Vector3 minScaleOffset;
        public Vector3 maxScaleOffset;

        public Vector3 mirrorAxis;
        public Vector3 mirrorEuler;
    }
    [Serializable]
    public struct CustomizableFeature : IComparable<CustomizableFeature> 
    {
        // !!!WARNING!!! Changing variable names in this struct will break existing serialized character states!

        [Header("Identity")]
        public string id;
        public string displayName;
        public string category;
        public string subCategory;

        public float defaultValue;

        public string valueGroup;

        [Header("Mirroring")]
        public bool mirror;
        public Vector3 mirrorAxis;
        public Vector3 mirrorEuler;

        [Header("Transform Offset")]
        public string targetTransformName;
        public Transform targetTransform;
        public bool skipTransformReset;
        [Tooltip("Should the min and max for transform changes be treated as one range that is linearly interpolated between?")]
        public bool asLerpRange;
        public Vector3 minOffset;
        public Vector3 maxOffset;
        public Vector3 minRotationOffset;
        public Vector3 maxRotationOffset;
        public Vector3 minScaleOffset;
        public Vector3 maxScaleOffset;

        public TransformOffsets[] additionalTransformChanges;

        [Header("Customization Option")]
        public CustomizableOptionType optionType;
        public string optionId;
        public float minOptionValue;
        public float maxOptionValue;

        [Header("Ordering")]
        public int sortPriority;

        public int CompareTo(CustomizableFeature other)
        {
            return Math.Sign(sortPriority - other.sortPriority);
        }
    }

    [Serializable]
    public class CustomizableFeatureController : IComparable<CustomizableFeatureController>, IDisposable
    {
        private SandboxCharacter character;
        public bool additive;

        public void Initialize(ICustomizableCharacter mainCharacterMesh, bool additive, bool autoOptimizeTransformChanges)
        {
            this.additive = additive;
            this.autoOptimizeTransformChanges = autoOptimizeTransformChanges;

            value = feature.defaultValue;

            switch (feature.optionType)
            {
                case CustomizableOptionType.Shape:
                    cachedOptionIndex = mainCharacterMesh.IndexOfStandaloneShape(feature.optionId);
#if UNITY_EDITOR
                    if (cachedOptionIndex < 0)
                    {
                        Debug.LogError($"Feature shape '{feature.optionId}' not found!"); 
                    }
#endif
                    break;

                case CustomizableOptionType.VariationLeft:
                case CustomizableOptionType.VariationRight:
                case CustomizableOptionType.VariationBoth:
                    int splitterIndex = feature.optionId.IndexOf(':');
                    var groupIndex = mainCharacterMesh.IndexOfVariationGroup(feature.optionId.Substring(0, splitterIndex));
                    var shapeIndex = mainCharacterMesh.IndexOfVariationShape(feature.optionId.Substring(splitterIndex + 1));
                    cachedOptionIndex = mainCharacterMesh.GetPartialVariationShapeIndex(groupIndex, shapeIndex);
#if UNITY_EDITOR
                    if (cachedOptionIndex < 0)
                    {
                        Debug.LogError($"Feature variation '{feature.optionId}' not found! {groupIndex}:{shapeIndex}");  
                    }
#endif
                    break;
            }

            if (feature.targetTransform != null)
            {
                feature.targetTransform.GetLocalPositionAndRotation(out var localPos, out var localRot);
                Vector3 localScale = feature.targetTransform.localScale;

                cachedTransformState = new TransformState(localPos, localRot, localScale);   
            }

            if (feature.additionalTransformChanges != null && feature.additionalTransformChanges.Length > 0)
            {
                additionalCachedTransformStates = new TransformState[feature.additionalTransformChanges.Length];
                additionalCachedTransformOffsets = new SandboxCharacterOptimizationManager.TransformLocalDataState[feature.additionalTransformChanges.Length];
                for (int a = 0; a < additionalCachedTransformStates.Length; a++)
                {
                    var changes = feature.additionalTransformChanges[a];
                    if (changes.targetTransform == null) continue;

                    changes.targetTransform.GetLocalPositionAndRotation(out var localPos, out var localRot);
                    Vector3 localScale = changes.targetTransform.localScale;

                    additionalCachedTransformStates[a] = new TransformState(localPos, localRot, localScale);
                    additionalCachedTransformOffsets[a] = new SandboxCharacterOptimizationManager.TransformLocalDataState() { rotationOffset = quaternion.identity };
                }
            }
        }

        public CustomizableFeatureController(SandboxCharacter character, CustomizableFeature feature)
        {
            this.character = character;
            this.feature = feature;
        }

        public CustomizableFeature feature;

        public int CompareTo(CustomizableFeatureController other)
        {
            return Math.Sign(feature.sortPriority - other.feature.sortPriority);
        }

        private float value;
        public float Value
        {
            get => value;
            set => SetValueNoUpdate(value);
        }

        public UnityEvent<float> OnValueChange = new UnityEvent<float>();

        [SerializeField]
        protected bool autoOptimizeTransformChanges = true;
        public bool AutoOptimizeTransformChanges => autoOptimizeTransformChanges;
        public void SetAutoOptimizeTransformChanges(bool autoOptimizeTransformChanges)
        {
            if (!autoOptimizeTransformChanges)
            {
                RemoveTransformChangeOptimization();
            }
            else if (!TransformChangesAreOptimized)
            {
                TrySetupTransformChangeOptimization();
            }

            this.autoOptimizeTransformChanges = autoOptimizeTransformChanges;
        }

        private TransformState? cachedTransformState;
        private TransformState[] additionalCachedTransformStates;
        private SandboxCharacterOptimizationManager.TransformLocalDataState cachedTransformOffsets;
        private SandboxCharacterOptimizationManager.TransformLocalDataState[] additionalCachedTransformOffsets;
        private bool transformChangesAreCached;
        public bool TransformChangesAreCached => transformChangesAreCached;
        private bool transformChangesAreOptimized;
        public bool TransformChangesAreOptimized => transformChangesAreOptimized;
        public void RemoveTransformChangeOptimization()
        {
            ResetTransformChangeOptimization();

            /*if (character != null)
            {
                if (feature.targetTransform != null) character.RemoveOptimizationForTransform(feature.targetTransform);
                if (feature.additionalTransformChanges != null && feature.additionalTransformChanges.Length > 0)
                {
                    foreach(var change in feature.additionalTransformChanges)
                    {
                        if (change.targetTransform != null) character.RemoveOptimizationForTransform(change.targetTransform);
                    }
                }
            }*/
        }
        public void ResetTransformChangeOptimization()
        {
            transformChangesAreOptimized = false;

            if (character != null) // ended up needing to do this here because transform optimizations that weren't removed caused a snowball effect. We now ensure all features that affect a transform that was reset will also reset the any other transforms they affect.
            {
                if (feature.targetTransform != null) character.RemoveOptimizationForTransform(feature.targetTransform); 
                if (feature.additionalTransformChanges != null && feature.additionalTransformChanges.Length > 0)
                {
                    foreach (var change in feature.additionalTransformChanges)
                    {
                        if (change.targetTransform != null) character.RemoveOptimizationForTransform(change.targetTransform);
                    }
                }
            }
        }
        public bool TrySetupTransformChangeOptimization()
        {
            if (!TransformChangesAreCached || TransformChangesAreOptimized) return TransformChangesAreOptimized;

            if (feature.targetTransform != null) 
            {
                if (character != null)
                {
                    var startState = cachedTransformState.HasValue ? cachedTransformState.Value : new TransformState(feature.targetTransform, false);
                    var indexRef = character.GetOptimizationReferenceForTransform(feature.targetTransform, startState, !feature.skipTransformReset, out bool isNewRef);
                    if (indexRef != null)
                    {
                        if (isNewRef && !feature.skipTransformReset)
                        {
                            indexRef.CachedDataChanges = indexRef.CachedDataChanges + new SandboxCharacterOptimizationManager.TransformLocalDataState()
                            {
                                positionOffset = startState.position,
                                rotationOffset = startState.rotation,
                                scaleOffset = startState.localScale
                            };
                        }

                        indexRef.CachedDataChanges = indexRef.CachedDataChanges + cachedTransformOffsets; 
                        indexRef.OnDispose.RemoveListener(ResetTransformChangeOptimization);
                        indexRef.OnDispose.AddListener(ResetTransformChangeOptimization);  
                    }
                }
            }
            if (feature.additionalTransformChanges != null && feature.additionalTransformChanges.Length > 0 && additionalCachedTransformOffsets != null)
            {
                if (character != null)
                {
                    for (int a = 0; a < feature.additionalTransformChanges.Length; a++)
                    {
                        var changes = feature.additionalTransformChanges[a];
                        if (changes.targetTransform == null) continue;

                        var startState = additionalCachedTransformStates != null && additionalCachedTransformStates.Length > 0 ? additionalCachedTransformStates[a] : new TransformState(changes.targetTransform, false);
                        var indexRef = character.GetOptimizationReferenceForTransform(changes.targetTransform, startState, !changes.skipTransformReset, out bool isNewRef);
                        if (indexRef != null)
                        {
                            if (isNewRef && !changes.skipTransformReset)
                            {
                                indexRef.CachedDataChanges = indexRef.CachedDataChanges + new SandboxCharacterOptimizationManager.TransformLocalDataState()
                                {
                                    positionOffset = startState.position,
                                    rotationOffset = startState.rotation,
                                    scaleOffset = startState.localScale
                                };
                            }

                            indexRef.CachedDataChanges = indexRef.CachedDataChanges + additionalCachedTransformOffsets[a];
                            indexRef.OnDispose.RemoveListener(ResetTransformChangeOptimization);
                            indexRef.OnDispose.AddListener(ResetTransformChangeOptimization);
                        }
                    }
                }
            }

            transformChangesAreOptimized = true;
            return true;
        }
        private int cachedOptionIndex;
        public void SetValue(float newValue, ICustomizableCharacter mainCharacterMesh, bool resetTransform = false, bool forceUpdate = false, bool notifyListeners = true)
        {
            transformChangesAreCached = false;
            if (character != null) character.hasAppliedFeatures = false;
            RemoveTransformChangeOptimization();

            this.value = newValue;
            if (notifyListeners) OnValueChange?.Invoke(newValue);

            Apply(mainCharacterMesh, resetTransform, forceUpdate, true);
        }
        public void SetValueNoUpdate(float newValue, bool notifyListeners = true)
        {
            transformChangesAreCached = false;
            if (character != null) character.hasAppliedFeatures = false;
            RemoveTransformChangeOptimization();
                
            this.value = newValue;
            if (notifyListeners) OnValueChange?.Invoke(newValue);
        }

        public void Apply(ICustomizableCharacter mainCharacterMesh, bool resetTransform = false, bool forceUpdate = false, bool allowOptimization = true)
        {
            float optionValue;
            float2 currentValueLR;
            switch (feature.optionType)
            {
                case CustomizableOptionType.Shape:
                    optionValue = math.lerp(feature.minOptionValue, feature.maxOptionValue, Value);
                    if (forceUpdate || mainCharacterMesh.GetStandaloneShapeWeightUnsafe(cachedOptionIndex) != optionValue)
                    {
                        mainCharacterMesh.SetStandaloneShapeWeightUnsafe(cachedOptionIndex, optionValue);
                    }
                    break;

                case CustomizableOptionType.VariationLeft:
                    optionValue = math.lerp(feature.minOptionValue, feature.maxOptionValue, Value);
                    currentValueLR = mainCharacterMesh.GetVariationWeightUnsafe(cachedOptionIndex);
                    if (forceUpdate || currentValueLR.x != optionValue)
                    {
                        currentValueLR.x = optionValue;
                        mainCharacterMesh.SetVariationWeightUnsafe(cachedOptionIndex, currentValueLR);
                    }
                    break;

                case CustomizableOptionType.VariationRight:
                    optionValue = math.lerp(feature.minOptionValue, feature.maxOptionValue, Value);
                    currentValueLR = mainCharacterMesh.GetVariationWeightUnsafe(cachedOptionIndex);
                    if (forceUpdate || currentValueLR.y != optionValue)
                    {
                        currentValueLR.y = optionValue;
                        mainCharacterMesh.SetVariationWeightUnsafe(cachedOptionIndex, currentValueLR);
                    }
                    break;

                case CustomizableOptionType.VariationBoth:
                    optionValue = math.lerp(feature.minOptionValue, feature.maxOptionValue, Value);
                    if (forceUpdate || math.any(mainCharacterMesh.GetVariationWeightUnsafe(cachedOptionIndex) != optionValue))
                    {
                        mainCharacterMesh.SetVariationWeightUnsafe(cachedOptionIndex, optionValue);
                    }
                    break;
            }

            if (!TransformChangesAreOptimized)
            {
                bool willOptimize = AutoOptimizeTransformChanges && allowOptimization;
                if (feature.targetTransform != null)
                {
                    feature.targetTransform.GetLocalPositionAndRotation(out var localPos, out var localRot);
                    Vector3 localScale = feature.targetTransform.localScale;

                    if (resetTransform && !feature.skipTransformReset && cachedTransformState.HasValue)
                    {
                        localPos = cachedTransformState.Value.position;
                        localRot = cachedTransformState.Value.rotation;
                        localScale = cachedTransformState.Value.localScale;
                    }


                    Vector3 offsetPos = Vector3.zero;
                    Quaternion offsetRot = Quaternion.identity;
                    Vector3 offsetScale = Vector3.zero;
                    if (feature.asLerpRange)
                    {
                        offsetPos = Vector3.LerpUnclamped(feature.minOffset, feature.maxOffset, value);
                        offsetRot = Quaternion.SlerpUnclamped(Quaternion.Euler(feature.minRotationOffset), Quaternion.Euler(feature.maxRotationOffset), value);
                        offsetScale = Vector3.LerpUnclamped(feature.minScaleOffset, feature.maxScaleOffset, value);
                    }
                    else
                    {
                        float signedValue = (value - 0.5f) * 2f;
                        if (signedValue < 0f)
                        {
                            float _value = Mathf.Abs(signedValue);
                            offsetPos = feature.minOffset * _value;
                            offsetRot = Quaternion.Euler(feature.minRotationOffset * _value);
                            offsetScale = feature.minScaleOffset * _value;
                        }
                        else
                        {
                            offsetPos = feature.maxOffset * signedValue;
                            offsetRot = Quaternion.Euler(feature.maxRotationOffset * signedValue);
                            offsetScale = feature.maxScaleOffset * signedValue;
                        }
                    }

                    cachedTransformOffsets = new SandboxCharacterOptimizationManager.TransformLocalDataState()
                    {
                        positionOffset = offsetPos,
                        rotationOffset = offsetRot,
                        scaleOffset = offsetScale
                    };
                    localPos = localPos + offsetPos;
                    localRot = offsetRot * localRot;
                    localScale = localScale + offsetScale;

                    if (!willOptimize)
                    {
                        feature.targetTransform.SetLocalPositionAndRotation(localPos, localRot);
                        feature.targetTransform.localScale = localScale;
                    }
                }

                if (feature.additionalTransformChanges != null && feature.additionalTransformChanges.Length > 0)
                {
                    if (feature.asLerpRange)
                    {
                        for (int a = 0; a < feature.additionalTransformChanges.Length; a++)
                        {
                            var changes = feature.additionalTransformChanges[a];
                            if (changes.targetTransform != null)
                            {
                                changes.targetTransform.GetLocalPositionAndRotation(out var localPos, out var localRot);
                                Vector3 localScale = changes.targetTransform.localScale;

                                if (resetTransform && !changes.skipTransformReset && cachedTransformState.HasValue)
                                {
                                    localPos = cachedTransformState.Value.position;
                                    localRot = cachedTransformState.Value.rotation;
                                    localScale = cachedTransformState.Value.localScale;
                                }

                                Vector3 offsetPos = Vector3.LerpUnclamped(changes.minOffset, changes.maxOffset, value);
                                Quaternion offsetRot = Quaternion.SlerpUnclamped(Quaternion.Euler(changes.minRotationOffset), Quaternion.Euler(changes.maxRotationOffset), value);
                                Vector3 offsetScale = Vector3.LerpUnclamped(changes.minScaleOffset, changes.maxScaleOffset, value);

                                additionalCachedTransformOffsets[a] = new SandboxCharacterOptimizationManager.TransformLocalDataState()
                                {
                                    positionOffset = offsetPos,
                                    rotationOffset = offsetRot,
                                    scaleOffset = offsetScale
                                };
                                localPos = localPos + offsetPos;
                                localRot = offsetRot * localRot;
                                localScale = localScale + offsetScale;

                                if (!willOptimize)
                                {
                                    changes.targetTransform.SetLocalPositionAndRotation(localPos, localRot);
                                    changes.targetTransform.localScale = localScale; 
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int a = 0; a < feature.additionalTransformChanges.Length; a++)
                        {
                            var changes = feature.additionalTransformChanges[a];
                            if (changes.targetTransform != null)
                            {
                                changes.targetTransform.GetLocalPositionAndRotation(out var localPos, out var localRot);
                                Vector3 localScale = changes.targetTransform.localScale;

                                if (resetTransform && !changes.skipTransformReset && additionalCachedTransformStates != null)
                                {
                                    var cachedState = additionalCachedTransformStates[a];
                                    localPos = cachedState.position;
                                    localRot = cachedState.rotation;
                                    localScale = cachedState.localScale;
                                }

                                Vector3 offsetPos = Vector3.zero;
                                Quaternion offsetRot = Quaternion.identity;
                                Vector3 offsetScale = Vector3.zero;

                                float signedValue = (value - 0.5f) * 2f;
                                if (signedValue < 0f)
                                {
                                    float _value = Mathf.Abs(signedValue);
                                    offsetPos = changes.minOffset * _value;
                                    offsetRot = Quaternion.Euler(changes.minRotationOffset * _value);
                                    offsetScale = changes.minScaleOffset * _value;
                                }
                                else
                                {
                                    offsetPos = changes.maxOffset * signedValue;
                                    offsetRot = Quaternion.Euler(changes.maxRotationOffset * signedValue);
                                    offsetScale = changes.maxScaleOffset * signedValue;
                                }

                                additionalCachedTransformOffsets[a] = new SandboxCharacterOptimizationManager.TransformLocalDataState()
                                {
                                    positionOffset = offsetPos,
                                    rotationOffset = offsetRot,
                                    scaleOffset = offsetScale
                                };
                                localPos = localPos + offsetPos;
                                localRot = offsetRot * localRot;
                                localScale = localScale + offsetScale;

                                if (!willOptimize)
                                {
                                    changes.targetTransform.SetLocalPositionAndRotation(localPos, localRot);
                                    changes.targetTransform.localScale = localScale;
                                }
                            }
                        }
                    }

                }

                transformChangesAreCached = true;

                if (willOptimize) TrySetupTransformChangeOptimization();
            }
        }

        public void Dispose()
        {
            RemoveTransformChangeOptimization();

            if (OnValueChange != null) OnValueChange.RemoveAllListeners();
        }
    }

    public class ValueGroup
    {
        public string name;
        public ValueGroup(string name)
        {
            this.name = name;
        }

        public readonly List<CustomizableFeatureController> features = new List<CustomizableFeatureController>();
        public void AddFeature(CustomizableFeatureController feature)
        {
            features.Add(feature);
            feature.OnValueChange.AddListener((float _) => UpdateValues(feature)); 
        }

        public UnityEvent OnValueChange = new UnityEvent();
        public void CallListeners() => OnValueChange?.Invoke();

        public void UpdateValues(CustomizableFeatureController featureToLock = null)
        {
            if (featureToLock == null)
            {
                float total = 0f;
                foreach (var feature in features)
                {
                    total += feature.Value;
                }

                if (total != 0f)
                {
                    foreach (var feature in features)
                    {
                        feature.SetValueNoUpdate(feature.Value / total, false); 
                    }
                }
            } 
            else
            {
                float total = 0f;
                foreach (var feature in features)
                {
                    if (ReferenceEquals(feature, featureToLock)) continue; 

                    total += feature.Value;
                }

                float mix = Mathf.Max(0f, 1f - featureToLock.Value); 
                if (total > mix)
                {
                    foreach (var feature in features)
                    {
                        if (ReferenceEquals(feature, featureToLock)) continue;

                        feature.SetValueNoUpdate((feature.Value / total) * mix, false);
                    }
                }
            }

            CallListeners();
        }
    }

    protected bool hasAppliedFeatures;
    protected readonly Dictionary<Transform, SandboxCharacterOptimizationManager.IndexReference> cachedTransformChanges = new Dictionary<Transform, SandboxCharacterOptimizationManager.IndexReference>();
    public void RemoveOptimizationForTransform(Transform transform)
    {
        if (cachedTransformChanges.TryGetValue(transform, out var indexRef))
        {
            hasAppliedFeatures = false;
            cachedTransformChanges.Remove(transform);
            if (indexRef != null) indexRef.Dispose();
        }
    }
    public SandboxCharacterOptimizationManager.IndexReference GetOptimizationReferenceForTransform(Transform transform, TransformState startState, bool resetTransformEveryUpdate, out bool isNewRef)
    {
        isNewRef = false;
        if (!cachedTransformChanges.TryGetValue(transform, out var indexRef))
        {
            isNewRef = true;
            indexRef = SandboxCharacterOptimizationManager.RegisterCachedChanges(transform, new SandboxCharacterOptimizationManager.TransformLocalDataState()
            {
                resetTransform = resetTransformEveryUpdate,
                //localRotation = quaternion.identity
                rotationOffset = quaternion.identity,
                localPosition = startState.position,
                localRotation = startState.rotation,
                localScale = startState.localScale
            });
            cachedTransformChanges[transform] = indexRef;
        }

        return indexRef;
    }
    [SerializeField]
    protected bool autoOptimizeTransformChanges = true;
    public bool AutoOptimizeTransformChanges => autoOptimizeTransformChanges;
    public void SetAutoOptimizeTransformChanges(bool autoOptimizeTransformChanges)
    {
        if (featureControllers != null)
        {
            foreach (var entry in featureControllers)
            {
                if (entry.Value != null)
                {
                    entry.Value.SetAutoOptimizeTransformChanges(autoOptimizeTransformChanges);
                }
            }
        }

        this.autoOptimizeTransformChanges = autoOptimizeTransformChanges;
        hasAppliedFeatures = false;
    }

#if UNITY_EDITOR
    [SerializeField]
#endif
    protected bool renameFeatureGroupReference;
#if UNITY_EDITOR
    [SerializeField]
#endif
    protected bool renameFeatureShapeReference; 
#if UNITY_EDITOR
    [SerializeField]
#endif
    protected string inputStringA;
#if UNITY_EDITOR
    [SerializeField]
#endif
    protected string inputStringB;

#if UNITY_EDITOR
    [SerializeField]
#endif
    protected bool cacheFeatureTransforms;
    public List<CustomizableFeature> customizableFeatures;
    public void CacheFeatureTransforms() 
    {
        if (customizableFeatures == null || customizableFeatures.Count <= 0) return;

        for (int a = 0; a < customizableFeatures.Count; a++)
        {
            var feature = customizableFeatures[a];
            if (feature.targetTransform != null)
            {
                feature.targetTransformName = feature.targetTransform.name;
            }
            else if (!string.IsNullOrWhiteSpace(feature.targetTransformName))
            {
                feature.targetTransform = transform.FindDeepChild(feature.targetTransformName);
            }

            if (feature.additionalTransformChanges != null)
            {
                for (int b = 0; b < feature.additionalTransformChanges.Length; b++)
                {
                    var change = feature.additionalTransformChanges[b];
                    if (change.targetTransform != null)
                    {
                        change.targetTransformName = change.targetTransform.name;
                    }
                    else if (!string.IsNullOrWhiteSpace(change.targetTransformName))
                    {
                        change.targetTransform = transform.FindDeepChild(change.targetTransformName);
                    }
                    feature.additionalTransformChanges[b] = change;
                }
            }

            customizableFeatures[a] = feature;
        }
    }

    private Dictionary<string, CustomizableFeatureController> featureControllers;
    private Dictionary<string, ValueGroup> valueGroups;
    public ValueGroup GetValueGroup(string valueGroupName)
    {
        if (valueGroups.TryGetValue(valueGroupName, out var group))
        {
            return group;
        }

        return null;
    }

    public List<Transform> GetAffectedTransforms(List<Transform> list = null)
    {
        if (list == null) list = new List<Transform>();

        if (characterHeight != null) characterHeight.GetAffectedTransforms(list);

        return list;
    }

    #region Eyes

    [Header("Eyes")]
    public MeshRenderer eyeballLeft;
    public MeshRenderer eyeballRight;
    public SelectableTextureGroup irisTextures;
    public SelectableTextureGroup scleraTextures;
    public ColorPaletteGroup eyeColorPresets;

    public static SelectableTextureGroup customIrisTextures;
    public static SelectableTextureGroup customScleraTextures;
    public static SelectableTextureGroup customEyeColorPresets;

    private Material eyeballLeftMaterial;
    public Material LeftEyeMaterial => eyeballLeftMaterial;
    private Material eyeballRightMaterial;
    public Material RightEyeMaterial => eyeballRightMaterial;
    private bool isUsingSingleEyeballMaterial;

    public const string matProp_irisMap = "_IrisMap";
    public const string matProp_irisNormalMap = "_IrisNormalMap";
    public const string matProp_scleraMap = "_BaseMap";
    public const string matProp_scleraNormalMap = "_ScleraNormalMap";

    public const string matProp_irisColorA = "_IrisColor1";
    public const string matProp_irisColorB = "_IrisColor2";
    public const string matProp_irisColorC = "_IrisColor3";
    public const string matProp_scleraTint = "_ScleraTint";
    public const string matProp_scleraMapIsMask = "_ScleraMapIsMask";
    public const string matProp_scleraColorA = "_ScleraColor1";
    public const string matProp_scleraColorB = "_ScleraColor2";
    public const string matProp_scleraColorC = "_ScleraColor3";

    public const string matProp_irisIOR = "_IOR";
    public const string matProp_irisRefractionOffset = "_PlaneOffset";
    public const string matProp_pupilShape = "_PupilShape";
    public const string matProp_irisShape = "_IrisShape";
    public const string matProp_irisSize = "_IrisRadius";
    public const string matProp_scleraWetness = "_ScleraSmoothness";
    public const string matProp_corneaWetness = "_CorneaSmoothness";

    public const string attr_irisScleraPair = "sclera:";
    public const string attr_isMask = "is_mask";
    public const string attr_normalMap = "normal";

    public const float eyeIOR_min = 2f;
    public const float eyeIOR_max = 0.91f;
    public const float eyeIOR_default = 1.333f;
    public const float eyeRefractionOffset_default = 0.02f;
    public const float eyeIrisSize_default = 0.225f;
    public const float eyeScleraWetness_default = 0.8f;
    public const float eyeCorneaWetness_default = 0.983f;

    private Texture default_irisLeftTexture;
    private Texture default_irisLeftNormalTexture;
    private Texture default_irisRightTexture;
    private Texture default_irisRightNormalTexture;

    private Texture default_scleraLeftTexture;
    private Texture default_scleraLeftNormalTexture;
    private Texture default_scleraRightTexture;
    private Texture default_scleraRightNormalTexture;

    [UserCustomizable]
    private Color irisLeftColorA; 
    public Color LeftIrisColorA
    {
        get => irisLeftColorA;
        set
        {
            irisLeftColorA = value;
            ReapplyEyeMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color irisLeftColorB;
    public Color LeftIrisColorB
    {
        get => irisLeftColorB;
        set
        {
            irisLeftColorB = value;
            ReapplyEyeMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color irisLeftColorC;
    public Color LeftIrisColorC
    {
        get => irisLeftColorC;
        set
        {
            irisLeftColorC = value;
            ReapplyEyeMaterialProperties();
        }
    }

    [UserCustomizable]
    private Color scleraLeftTint;
    public Color LeftScleraTint
    {
        get => scleraLeftTint;
        set
        {
            scleraLeftTint = value;
            ReapplyEyeMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color scleraLeftColorA;
    public Color LeftScleraColorA
    {
        get => scleraLeftColorA;
        set
        {
            scleraLeftColorA = value;
            ReapplyEyeMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color scleraLeftColorB;
    public Color LeftScleraColorB
    {
        get => scleraLeftColorB;
        set
        {
            scleraLeftColorB = value;
            ReapplyEyeMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color scleraLeftColorC;
    public Color LeftScleraColorC
    {
        get => scleraLeftColorC;
        set
        {
            scleraLeftColorC = value;
            ReapplyEyeMaterialProperties();
        }
    }

    [UserCustomizable]
    private Color irisRightColorA;
    public Color RightIrisColorA
    {
        get => irisRightColorA;
        set
        {
            irisRightColorA = value;
            ReapplyEyeMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color irisRightColorB;
    public Color RightIrisColorB
    {
        get => irisRightColorB;
        set
        {
            irisRightColorB = value;
            ReapplyEyeMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color irisRightColorC;
    public Color RightIrisColorC
    {
        get => irisRightColorC;
        set
        {
            irisRightColorC = value;
            ReapplyEyeMaterialProperties();
        }
    }

    [UserCustomizable]
    private Color scleraRightTint;
    public Color RightScleraTint
    {
        get => scleraRightTint;
        set
        {
            scleraRightTint = value;
            ReapplyEyeMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color scleraRightColorA;
    public Color RightScleraColorA
    {
        get => scleraRightColorA;
        set
        {
            scleraRightColorA = value;
            ReapplyEyeMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color scleraRightColorB;
    public Color RightScleraColorB
    {
        get => scleraRightColorB;
        set
        {
            scleraRightColorB = value;
            ReapplyEyeMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color scleraRightColorC;
    public Color RightScleraColorC
    {
        get => scleraRightColorC;
        set
        {
            scleraRightColorC = value;
            ReapplyEyeMaterialProperties();
        }
    }

    [UserCustomizable]
    private float irisLeftSize;
    public float IrisLeftSize
    {
        get => irisLeftSize;
        set
        {
            irisLeftSize = value;
            ReapplyEyeMaterialProperties();
        }
    }

    [UserCustomizable]
    private float pupilLeftShapeX;
    public float PupilLeftShapeX
    {
        get => pupilLeftShapeX;
        set
        {
            pupilLeftShapeX = value;
            ReapplyEyeMaterialProperties();
        }
    }

    [UserCustomizable]
    private float pupilLeftShapeY;
    public float PupilLeftShapeY
    {
        get => pupilLeftShapeY;
        set
        {
            pupilLeftShapeY = value;
            ReapplyEyeMaterialProperties();
        }
    }

    [UserCustomizable]
    private float irisLeftIOR;
    public float IrisLeftIOR
    {
        get => irisLeftIOR;
        set
        {
            irisLeftIOR = value;
            ReapplyEyeMaterialProperties();
        }
    }

    [UserCustomizable]
    private float irisLeftRefractionOffset;
    public float IrisLeftRefractionOffset
    {
        get => irisLeftRefractionOffset;
        set
        {
            irisLeftRefractionOffset = value;
            ReapplyEyeMaterialProperties();
        }
    }

    [UserCustomizable]
    private float scleraLeftWetness;
    public float ScleraLeftWetness
    {
        get => scleraLeftWetness;
        set
        {
            scleraLeftWetness = value;
            ReapplyEyeMaterialProperties();
        }
    }

    [UserCustomizable]
    private float corneaLeftWetness;
    public float CorneaLeftWetness
    {
        get => corneaLeftWetness;
        set
        {
            corneaLeftWetness = value;
            ReapplyEyeMaterialProperties();
        }
    }

    [UserCustomizable]
    private float irisRightSize;
    public float IrisRightSize
    {
        get => irisRightSize;
        set
        {
            irisRightSize = value;
            ReapplyEyeMaterialProperties();
        }
    }

    [UserCustomizable]
    private float pupilRightShapeX;
    public float PupilRightShapeX
    {
        get => pupilRightShapeX;
        set
        {
            pupilRightShapeX = value;
            ReapplyEyeMaterialProperties();
        }
    }

    [UserCustomizable]
    private float pupilRightShapeY;
    public float PupilRightShapeY
    {
        get => pupilRightShapeY;
        set
        {
            pupilRightShapeY = value;
            ReapplyEyeMaterialProperties();
        }
    }

    [UserCustomizable]
    private float irisRightIOR;
    public float IrisRightIOR
    {
        get => irisRightIOR;
        set
        {
            irisRightIOR = value;
            ReapplyEyeMaterialProperties();
        }
    }

    [UserCustomizable]
    private float irisRightRefractionOffset;
    public float IrisRightRefractionOffset
    {
        get => irisRightRefractionOffset;
        set
        {
            irisRightRefractionOffset = value;
            ReapplyEyeMaterialProperties();
        }
    }

    [UserCustomizable]
    private float scleraRightWetness;
    public float ScleraRightWetness
    {
        get => scleraRightWetness;
        set
        {
            scleraRightWetness = value;
            ReapplyEyeMaterialProperties();
        }
    }

    [UserCustomizable]
    private float corneaRightWetness;
    public float CorneaRightWetness
    {
        get => corneaRightWetness;
        set
        {
            corneaRightWetness = value;
            ReapplyEyeMaterialProperties();
        }
    }

    public bool IsHeterochromic => irisLeftColorA != irisRightColorA || irisLeftColorB != irisRightColorB || irisLeftColorC != irisRightColorC ||
        scleraLeftTint != scleraRightTint || scleraLeftColorA != scleraRightColorA || scleraLeftColorB != scleraRightColorB || scleraLeftColorC != scleraRightColorC ||
        irisLeftTextureID != irisRightTextureID || scleraLeftTextureID != scleraRightTextureID ||
        irisLeftSize != irisRightSize || pupilLeftShapeX != pupilRightShapeX || pupilLeftShapeY != pupilRightShapeY ||
        irisLeftIOR != irisRightIOR || irisLeftRefractionOffset != irisRightRefractionOffset || scleraLeftWetness != scleraRightWetness || corneaLeftWetness != corneaRightWetness; 

    public void ReapplyEyeMaterialProperties()
    {
        if (isUsingSingleEyeballMaterial && IsHeterochromic)
        {
            isUsingSingleEyeballMaterial = false;
            if (eyeballLeftMaterial != null && ReferenceEquals(eyeballLeftMaterial, eyeballRightMaterial))
            {
                eyeballRightMaterial = Instantiate(eyeballLeftMaterial);
                eyeballRight.sharedMaterial = eyeballRightMaterial; 
            }
        }

        if (eyeballLeftMaterial != null)
        {
            eyeballLeftMaterial.SetColor(matProp_irisColorA, irisLeftColorA);
            eyeballLeftMaterial.SetColor(matProp_irisColorB, irisLeftColorB);
            eyeballLeftMaterial.SetColor(matProp_irisColorC, irisLeftColorC);

            eyeballLeftMaterial.SetColor(matProp_scleraTint, scleraLeftTint);
            eyeballLeftMaterial.SetColor(matProp_scleraColorA, scleraLeftColorA);
            eyeballLeftMaterial.SetColor(matProp_scleraColorB, scleraLeftColorB);
            eyeballLeftMaterial.SetColor(matProp_scleraColorC, scleraLeftColorC); 

            eyeballLeftMaterial.SetFloat(matProp_irisSize, Mathf.Max(0f, irisLeftSize));
            eyeballLeftMaterial.SetVector(matProp_pupilShape, new Vector4(1f - (pupilLeftShapeX - 1f) * 0.5f, 1f - (pupilLeftShapeY - 1f) * 0.5f, 0f, 0f));
            eyeballLeftMaterial.SetVector(matProp_irisShape, new Vector4(1f + (pupilLeftShapeX - 1f) * 0.5f, 1f + (pupilLeftShapeY - 1f) * 0.5f, 0f, 0f));
            eyeballLeftMaterial.SetFloat(matProp_irisIOR, irisLeftIOR);
            eyeballLeftMaterial.SetFloat(matProp_irisRefractionOffset, irisLeftRefractionOffset);
            eyeballLeftMaterial.SetFloat(matProp_scleraWetness, scleraLeftWetness);
            eyeballLeftMaterial.SetFloat(matProp_corneaWetness, corneaLeftWetness);
        }

        if (eyeballRightMaterial != null)
        {
            eyeballRightMaterial.SetColor(matProp_irisColorA, irisRightColorA);
            eyeballRightMaterial.SetColor(matProp_irisColorB, irisRightColorB);
            eyeballRightMaterial.SetColor(matProp_irisColorC, irisRightColorC);

            eyeballRightMaterial.SetColor(matProp_scleraTint, scleraRightTint);
            eyeballRightMaterial.SetColor(matProp_scleraColorA, scleraRightColorA);
            eyeballRightMaterial.SetColor(matProp_scleraColorB, scleraRightColorB);
            eyeballRightMaterial.SetColor(matProp_scleraColorC, scleraRightColorC);

            eyeballRightMaterial.SetFloat(matProp_irisSize, Mathf.Max(0f, irisRightSize));
            eyeballRightMaterial.SetVector(matProp_pupilShape, new Vector4(1f - (pupilRightShapeX - 1f) * 0.5f, 1f - (pupilRightShapeY - 1f) * 0.5f, 0f, 0f));
            eyeballRightMaterial.SetVector(matProp_irisShape, new Vector4(1f + (pupilRightShapeX - 1f) * 0.5f, 1f + (pupilRightShapeY - 1f) * 0.5f, 0f, 0f));  
            eyeballRightMaterial.SetFloat(matProp_irisIOR, irisRightIOR);
            eyeballRightMaterial.SetFloat(matProp_irisRefractionOffset, irisRightRefractionOffset);
            eyeballRightMaterial.SetFloat(matProp_scleraWetness, scleraRightWetness);
            eyeballRightMaterial.SetFloat(matProp_corneaWetness, corneaRightWetness);
        }
    }

    [UserCustomizable]
    private string irisLeftTextureID;
    public string LeftIrisTextureID => irisLeftTextureID;
    //[UserCustomizable]
    private string scleraLeftTextureID;
    public string LeftScleraTextureID => scleraLeftTextureID;
    private void SetLeftScleraTexture(string id)
    {
        scleraLeftTextureID = null;

        SelectableTextureGroup.SelectableTexture scleraTexture = default;
        if (scleraTextures != null)
        {
            if (scleraTextures.TryGetTexture(id, out scleraTexture))
            {
                scleraLeftTextureID = id;
                ReapplyEyeMaterialProperties();

                if (eyeballLeftMaterial != null)
                {
                    eyeballLeftMaterial.SetTexture(matProp_scleraMap, scleraTexture.texture == null ? default_scleraLeftTexture : scleraTexture.texture);
                    eyeballLeftMaterial.SetFloat(matProp_scleraMapIsMask, scleraTexture.HasAttribute(attr_isMask) ? 1f : 0f);
                    if (scleraTexture.TryGetSecondaryTexture(attr_normalMap, out var normalTex))
                    {
                        eyeballLeftMaterial.SetTexture(matProp_scleraNormalMap, normalTex);
                    }
                    else
                    {
                        eyeballLeftMaterial.SetTexture(matProp_scleraNormalMap, null);
                    }
                }

                return;
            }
        }
        if (customScleraTextures != null)
        {
            if (customScleraTextures.TryGetTexture(id, out scleraTexture))
            {
                scleraLeftTextureID = id;
                ReapplyEyeMaterialProperties();

                if (eyeballLeftMaterial != null)
                {
                    eyeballLeftMaterial.SetTexture(matProp_scleraMap, scleraTexture.texture == null ? default_scleraLeftTexture : scleraTexture.texture);
                    eyeballLeftMaterial.SetFloat(matProp_scleraMapIsMask, scleraTexture.HasAttribute(attr_isMask) ? 1f : 0f);
                    if (scleraTexture.TryGetSecondaryTexture(attr_normalMap, out var normalTex))
                    {
                        eyeballLeftMaterial.SetTexture(matProp_scleraNormalMap, normalTex);
                    }
                    else
                    {
                        eyeballLeftMaterial.SetTexture(matProp_scleraNormalMap, null);
                    }
                }

                return;
            }
        }

        if (eyeballLeftMaterial != null)
        {
            ReapplyEyeMaterialProperties();
            eyeballLeftMaterial.SetTexture(matProp_scleraMap, default_scleraLeftTexture);
            eyeballLeftMaterial.SetTexture(matProp_scleraNormalMap, default_scleraLeftNormalTexture);
            eyeballLeftMaterial.SetFloat(matProp_scleraMapIsMask, 0f);
        }
    }
    public void SetLeftIrisTexture(string id)
    {
        irisLeftTextureID = null;

        SelectableTextureGroup.SelectableTexture irisTexture = default;
        if (irisTextures != null)
        {
            if (irisTextures.TryGetTexture(id, out irisTexture))
            {
                irisLeftTextureID = id;
                ReapplyEyeMaterialProperties();

                if (eyeballLeftMaterial != null)
                {
                    eyeballLeftMaterial.SetTexture(matProp_irisMap, irisTexture.texture == null ? default_irisLeftTexture : irisTexture.texture);
                    if (irisTexture.TryGetSecondaryTexture(attr_normalMap, out var normalTex))
                    {
                        eyeballLeftMaterial.SetTexture(matProp_irisNormalMap, normalTex);
                    }
                    else
                    {
                        eyeballLeftMaterial.SetTexture(matProp_irisNormalMap, null);
                    }
                }

                SetLeftScleraTexture(irisTexture.GetAttributeValue(attr_irisScleraPair));
                return;
            }
        }
        if (customIrisTextures != null)
        {
            if (customIrisTextures.TryGetTexture(id, out irisTexture))
            {
                irisLeftTextureID = id;
                ReapplyEyeMaterialProperties();

                if (eyeballLeftMaterial != null)
                {
                    eyeballLeftMaterial.SetTexture(matProp_irisMap, irisTexture.texture == null ? default_irisLeftTexture : irisTexture.texture);
                    if (irisTexture.TryGetSecondaryTexture(attr_normalMap, out var normalTex))
                    {
                        eyeballLeftMaterial.SetTexture(matProp_irisNormalMap, normalTex);
                    }
                    else
                    {
                        eyeballLeftMaterial.SetTexture(matProp_irisNormalMap, null);
                    }
                }

                SetLeftScleraTexture(irisTexture.GetAttributeValue(attr_irisScleraPair));
                return;
            }
        }

        if (eyeballLeftMaterial != null)
        {
            ReapplyEyeMaterialProperties();
            eyeballLeftMaterial.SetTexture(matProp_irisMap, default_irisLeftTexture);
            eyeballLeftMaterial.SetTexture(matProp_irisNormalMap, default_irisLeftNormalTexture);
        }
    }

    [UserCustomizable]
    private string irisRightTextureID;
    public string RightIrisTextureID => irisRightTextureID;
    //[UserCustomizable]
    private string scleraRightTextureID;
    public string RightScleraTextureID => scleraRightTextureID;
    private void SetRightScleraTexture(string id)
    {
        scleraRightTextureID = null;

        SelectableTextureGroup.SelectableTexture scleraTexture = default;
        if (scleraTextures != null)
        {
            if (scleraTextures.TryGetTexture(id, out scleraTexture))
            {
                scleraRightTextureID = id;
                ReapplyEyeMaterialProperties();

                if (eyeballRightMaterial != null)
                {
                    eyeballRightMaterial.SetTexture(matProp_scleraMap, scleraTexture.texture == null ? default_scleraRightTexture : scleraTexture.texture);
                    eyeballRightMaterial.SetFloat(matProp_scleraMapIsMask, scleraTexture.HasAttribute(attr_isMask) ? 1f : 0f);
                    if (scleraTexture.TryGetSecondaryTexture(attr_normalMap, out var normalTex))
                    {
                        eyeballRightMaterial.SetTexture(matProp_scleraNormalMap, normalTex);
                    }
                    else
                    {
                        eyeballRightMaterial.SetTexture(matProp_scleraNormalMap, null);
                    }
                }

                return;
            }
        }
        if (customScleraTextures != null)
        {
            if (customScleraTextures.TryGetTexture(id, out scleraTexture))
            {
                scleraRightTextureID = id;
                ReapplyEyeMaterialProperties();

                if (eyeballRightMaterial != null)
                {
                    eyeballRightMaterial.SetTexture(matProp_scleraMap, scleraTexture.texture == null ? default_scleraRightTexture : scleraTexture.texture);
                    eyeballRightMaterial.SetFloat(matProp_scleraMapIsMask, scleraTexture.HasAttribute(attr_isMask) ? 1f : 0f);
                    if (scleraTexture.TryGetSecondaryTexture(attr_normalMap, out var normalTex))
                    {
                        eyeballRightMaterial.SetTexture(matProp_scleraNormalMap, normalTex);
                    }
                    else
                    {
                        eyeballRightMaterial.SetTexture(matProp_scleraNormalMap, null);
                    }
                }

                return;
            }
        }

        if (eyeballRightMaterial != null)
        {
            ReapplyEyeMaterialProperties();
            eyeballRightMaterial.SetTexture(matProp_scleraMap, default_scleraRightTexture);
            eyeballRightMaterial.SetTexture(matProp_scleraNormalMap, default_scleraRightNormalTexture);
            eyeballRightMaterial.SetFloat(matProp_scleraMapIsMask, 0f);
        }
    }
    public void SetRightIrisTexture(string id)
    {
        irisRightTextureID = null;

        SelectableTextureGroup.SelectableTexture irisTexture = default;
        if (irisTextures != null)
        {
            if (irisTextures.TryGetTexture(id, out irisTexture))
            {
                irisRightTextureID = id;
                ReapplyEyeMaterialProperties();

                if (eyeballRightMaterial != null)
                {
                    eyeballRightMaterial.SetTexture(matProp_irisMap, irisTexture.texture == null ? default_irisRightTexture : irisTexture.texture);
                    if (irisTexture.TryGetSecondaryTexture(attr_normalMap, out var normalTex))
                    {
                        eyeballRightMaterial.SetTexture(matProp_irisNormalMap, normalTex);
                    }
                    else
                    {
                        eyeballRightMaterial.SetTexture(matProp_irisNormalMap, null);
                    }
                }

                SetRightScleraTexture(irisTexture.GetAttributeValue(attr_irisScleraPair));
                return;
            }
        }
        if (customIrisTextures != null)
        {
            if (customIrisTextures.TryGetTexture(id, out irisTexture))
            {
                irisRightTextureID = id;
                ReapplyEyeMaterialProperties();

                if (eyeballRightMaterial != null)
                {
                    eyeballRightMaterial.SetTexture(matProp_irisMap, irisTexture.texture == null ? default_irisRightTexture : irisTexture.texture);
                    if (irisTexture.TryGetSecondaryTexture(attr_normalMap, out var normalTex))
                    {
                        eyeballRightMaterial.SetTexture(matProp_irisNormalMap, normalTex);
                    }
                    else
                    {
                        eyeballRightMaterial.SetTexture(matProp_irisNormalMap, null);
                    }
                }

                SetRightScleraTexture(irisTexture.GetAttributeValue(attr_irisScleraPair));
                return;
            }
        }

        if (eyeballRightMaterial != null)
        {
            ReapplyEyeMaterialProperties();
            eyeballRightMaterial.SetTexture(matProp_irisMap, default_irisRightTexture);
            eyeballRightMaterial.SetTexture(matProp_irisNormalMap, default_irisRightNormalTexture);
        }
    }
    #endregion

    #region Skin

    public ColorPaletteGroup skinColorPresets;

    public const string matProp_melanin = "_Melanin";
    public const string matProp_skinTint = "_BaseColor";
    public const string matProp_skinScatterTint = "_SSSTint";
    public const string matProp_veinTint = "_VeinTint";
    public const string matProp_freckleColor = "_SkinDetailColorA";
    public const string matProp_moleColor = "_SkinDetailColorB";
    public const string matProp_pimpleColor = "_SkinDetailColorC";
    public const string matProp_scarringColor = "_SkinDetailColorD";

    public const string matProp_skinDetailScale = "_SkinDetailTiling";
    public const string matProp_skinDetailSeed = "_SkinDetailSeed";
    public const string matProp_skinDetailStrengths = "_SkinDetailWeightPowers";
    public const string matProp_skinDetailMaskScales = "_SkinDetailMaskScales";
    public const string matProp_skinDetailMaskSeeds = "_SkinDetailSeeds";
    public const string matProp_skinDetailMaskStrengths = "_SkinDetailMaskPowers";

    #region Masking

    public const string matProp_skinDetailMaskR_FaceLR = "_SkinDetailsR_FaceLR";
    public const string matProp_skinDetailMaskG_FaceLR = "_SkinDetailsG_FaceLR";
    public const string matProp_skinDetailMaskB_FaceLR = "_SkinDetailsB_FaceLR";
    public const string matProp_skinDetailMaskA_FaceLR = "_SkinDetailsA_FaceLR";

    public const string matProp_skinDetailMaskR_HeadLR = "_SkinDetailsR_HeadLR";
    public const string matProp_skinDetailMaskG_HeadLR = "_SkinDetailsG_HeadLR";
    public const string matProp_skinDetailMaskB_HeadLR = "_SkinDetailsB_HeadLR";
    public const string matProp_skinDetailMaskA_HeadLR = "_SkinDetailsA_HeadLR";

    public const string matProp_skinDetailMaskR_NeckLR = "_SkinDetailsR_NeckLR";
    public const string matProp_skinDetailMaskG_NeckLR = "_SkinDetailsG_NeckLR";
    public const string matProp_skinDetailMaskB_NeckLR = "_SkinDetailsB_NeckLR";
    public const string matProp_skinDetailMaskA_NeckLR = "_SkinDetailsA_NeckLR";

    public const string matProp_skinDetailMaskR_ChestLR = "_SkinDetailsR_ChestLR";
    public const string matProp_skinDetailMaskG_ChestLR = "_SkinDetailsG_ChestLR";
    public const string matProp_skinDetailMaskB_ChestLR = "_SkinDetailsB_ChestLR";
    public const string matProp_skinDetailMaskA_ChestLR = "_SkinDetailsA_ChestLR";

    public const string matProp_skinDetailMaskR_BackLR = "_SkinDetailsR_BackLR";
    public const string matProp_skinDetailMaskG_BackLR = "_SkinDetailsG_BackLR";
    public const string matProp_skinDetailMaskB_BackLR = "_SkinDetailsB_BackLR";
    public const string matProp_skinDetailMaskA_BackLR = "_SkinDetailsA_BackLR";

    public const string matProp_skinDetailMaskR_TorsoLR = "_SkinDetailsR_TorsoLR";
    public const string matProp_skinDetailMaskG_TorsoLR = "_SkinDetailsG_TorsoLR";
    public const string matProp_skinDetailMaskB_TorsoLR = "_SkinDetailsB_TorsoLR";
    public const string matProp_skinDetailMaskA_TorsoLR = "_SkinDetailsA_TorsoLR";

    public const string matProp_skinDetailMaskR_ArmsLR = "_SkinDetailsR_ArmsLR";
    public const string matProp_skinDetailMaskG_ArmsLR = "_SkinDetailsG_ArmsLR";
    public const string matProp_skinDetailMaskB_ArmsLR = "_SkinDetailsB_ArmsLR";
    public const string matProp_skinDetailMaskA_ArmsLR = "_SkinDetailsA_ArmsLR";

    public const string matProp_skinDetailMaskR_HandsLR = "_SkinDetailsR_HandsLR";
    public const string matProp_skinDetailMaskG_HandsLR = "_SkinDetailsG_HandsLR";
    public const string matProp_skinDetailMaskB_HandsLR = "_SkinDetailsB_HandsLR";
    public const string matProp_skinDetailMaskA_HandsLR = "_SkinDetailsA_HandsLR";

    public const string matProp_skinDetailMaskR_LegsLR = "_SkinDetailsR_LegsLR";
    public const string matProp_skinDetailMaskG_LegsLR = "_SkinDetailsG_LegsLR";
    public const string matProp_skinDetailMaskB_LegsLR = "_SkinDetailsB_LegsLR";
    public const string matProp_skinDetailMaskA_LegsLR = "_SkinDetailsA_LegsLR";

    public const string matProp_skinDetailMaskR_FeetLR = "_SkinDetailsR_FeetLR";
    public const string matProp_skinDetailMaskG_FeetLR = "_SkinDetailsG_FeetLR";
    public const string matProp_skinDetailMaskB_FeetLR = "_SkinDetailsB_FeetLR";
    public const string matProp_skinDetailMaskA_FeetLR = "_SkinDetailsA_FeetLR";

    #endregion

    [UserCustomizable]
    private float skinMelanin;
    public float SkinMelanin
    {
        get => skinMelanin;
        set
        {
            skinMelanin = value;
            ReapplySkinMaterialProperties();
        }
    }

    [UserCustomizable/*, SerializeField, ColorUsage(true, true)*/]
    private Color skinTint;
    public Color SkinTint
    {
        get => skinTint;
        set
        {
            skinTint = value;
            ReapplySkinMaterialProperties();
        }
    }

    [UserCustomizable]
    private Color skinScatterTint;
    public Color SkinScatterTint
    {
        get => skinScatterTint;
        set
        {
            skinScatterTint = value;
            ReapplySkinMaterialProperties();
        }
    }

    [UserCustomizable]
    private Color veinTint;
    public Color VeinTint
    {
        get => veinTint;
        set
        {
            veinTint = value;
            ReapplySkinMaterialProperties();
        }
    }

    #region Skin Details

    [UserCustomizable]
    private Color freckleColor;
    public Color FreckleColor
    {
        get => freckleColor;
        set
        {
            freckleColor = value;
            ReapplySkinMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color moleColor;
    public Color MoleColor
    {
        get => moleColor;
        set
        {
            moleColor = value;
            ReapplySkinMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color pimpleColor;
    public Color PimpleColor
    {
        get => pimpleColor;
        set
        {
            pimpleColor = value;
            ReapplySkinMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color scarringColor;
    public Color ScarringColor
    {
        get => scarringColor;
        set
        {
            scarringColor = value;
            ReapplySkinMaterialProperties();
        }
    }

    [UserCustomizable]
    private float skinDetailScale;
    public float SkinDetailScale
    {
        get => skinDetailScale;
        set
        {
            skinDetailScale = value;
            ReapplySkinMaterialProperties();
        }
    }
    [UserCustomizable]
    private float skinDetailSeed;
    public float SkinDetailSeed
    {
        get => skinDetailSeed;
        set
        {
            skinDetailSeed = value;
            ReapplySkinMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector4 skinDetailStrengths;
    public Vector4 SkinDetailStrengths
    {
        get => skinDetailStrengths;
        set
        {
            skinDetailStrengths = value;
            ReapplySkinMaterialProperties();
        }
    }

    [UserCustomizable]
    private Vector4 skinDetailMaskScales;
    public Vector4 SkinDetailMaskScales
    {
        get => skinDetailMaskScales;
        set
        {
            skinDetailMaskScales = value;
            ReapplySkinMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector4 skinDetailMaskSeeds;
    public Vector4 SkinDetailMaskSeeds
    {
        get => skinDetailMaskSeeds;
        set
        {
            skinDetailMaskSeeds = value;
            ReapplySkinMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector4 skinDetailMaskStrengths;
    public Vector4 SkinDetailMaskStrengths
    {
        get => skinDetailMaskStrengths;
        set
        {
            skinDetailMaskStrengths = value;
            ReapplySkinMaterialProperties();
        }
    }

    [UserCustomizable]
    private Vector4 skinDetailMaskFaceL;
    public Vector4 SkinDetailMaskFaceL
    {
        get => skinDetailMaskFaceL;
        set
        {
            skinDetailMaskFaceL = value;
            ReapplySkinMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector4 skinDetailMaskFaceR;
    public Vector4 SkinDetailMaskFaceR
    {
        get => skinDetailMaskFaceR;
        set
        {
            skinDetailMaskFaceR = value;
            ReapplySkinMaterialProperties();
        }
    }

    [UserCustomizable]
    private Vector4 skinDetailMaskHeadL;
    public Vector4 SkinDetailMaskHeadL
    {
        get => skinDetailMaskHeadL;
        set
        {
            skinDetailMaskHeadL = value;
            ReapplySkinMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector4 skinDetailMaskHeadR;
    public Vector4 SkinDetailMaskHeadR
    {
        get => skinDetailMaskHeadR;
        set
        {
            skinDetailMaskHeadR = value;
            ReapplySkinMaterialProperties();
        }
    }

    [UserCustomizable]
    private Vector4 skinDetailMaskNeckL;
    public Vector4 SkinDetailMaskNeckL
    {
        get => skinDetailMaskNeckL;
        set
        {
            skinDetailMaskNeckL = value;
            ReapplySkinMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector4 skinDetailMaskNeckR;
    public Vector4 SkinDetailMaskNeckR
    {
        get => skinDetailMaskNeckR;
        set
        {
            skinDetailMaskNeckR = value;
            ReapplySkinMaterialProperties();
        }
    }

    [UserCustomizable]
    private Vector4 skinDetailMaskChestL;
    public Vector4 SkinDetailMaskChestL
    {
        get => skinDetailMaskChestL;
        set
        {
            skinDetailMaskChestL = value;
            ReapplySkinMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector4 skinDetailMaskChestR;
    public Vector4 SkinDetailMaskChestR
    {
        get => skinDetailMaskChestR;
        set
        {
            skinDetailMaskChestR = value;
            ReapplySkinMaterialProperties();
        }
    }

    [UserCustomizable]
    private Vector4 skinDetailMaskBackL;
    public Vector4 SkinDetailMaskBackL
    {
        get => skinDetailMaskBackL;
        set
        {
            skinDetailMaskBackL = value;
            ReapplySkinMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector4 skinDetailMaskBackR;
    public Vector4 SkinDetailMaskBackR
    {
        get => skinDetailMaskBackR;
        set
        {
            skinDetailMaskBackR = value;
            ReapplySkinMaterialProperties();
        }
    }

    [UserCustomizable]
    private Vector4 skinDetailMaskTorsoL;
    public Vector4 SkinDetailMaskTorsoL
    {
        get => skinDetailMaskTorsoL;
        set
        {
            skinDetailMaskTorsoL = value;
            ReapplySkinMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector4 skinDetailMaskTorsoR;
    public Vector4 SkinDetailMaskTorsoR
    {
        get => skinDetailMaskTorsoR;
        set
        {
            skinDetailMaskTorsoR = value;
            ReapplySkinMaterialProperties();
        }
    }

    [UserCustomizable]
    private Vector4 skinDetailMaskArmsL;
    public Vector4 SkinDetailMaskArmsL
    {
        get => skinDetailMaskArmsL;
        set
        {
            skinDetailMaskArmsL = value;
            ReapplySkinMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector4 skinDetailMaskArmsR;
    public Vector4 SkinDetailMaskArmsR
    {
        get => skinDetailMaskArmsR;
        set
        {
            skinDetailMaskArmsR = value;
            ReapplySkinMaterialProperties();
        }
    }

    [UserCustomizable]
    private Vector4 skinDetailMaskHandsL;
    public Vector4 SkinDetailMaskHandsL
    {
        get => skinDetailMaskHandsL;
        set
        {
            skinDetailMaskHandsL = value;
            ReapplySkinMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector4 skinDetailMaskHandsR;
    public Vector4 SkinDetailMaskHandsR
    {
        get => skinDetailMaskHandsR;
        set
        {
            skinDetailMaskHandsR = value;
            ReapplySkinMaterialProperties();
        }
    }

    [UserCustomizable]
    private Vector4 skinDetailMaskLegsL;
    public Vector4 SkinDetailMaskLegsL
    {
        get => skinDetailMaskLegsL;
        set
        {
            skinDetailMaskLegsL = value;
            ReapplySkinMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector4 skinDetailMaskLegsR;
    public Vector4 SkinDetailMaskLegsR
    {
        get => skinDetailMaskLegsR;
        set
        {
            skinDetailMaskLegsR = value;
            ReapplySkinMaterialProperties();
        }
    }

    [UserCustomizable]
    private Vector4 skinDetailMaskFeetL;
    public Vector4 SkinDetailMaskFeetL
    {
        get => skinDetailMaskFeetL;
        set
        {
            skinDetailMaskFeetL = value;
            ReapplySkinMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector4 skinDetailMaskFeetR;
    public Vector4 SkinDetailMaskFeetR
    {
        get => skinDetailMaskFeetR;
        set
        {
            skinDetailMaskFeetR = value;
            ReapplySkinMaterialProperties();
        }
    }

    #endregion

    protected void ReapplySkinMaterialProperties(ICustomizableCharacter mesh)
    {
        if (mesh == null) return;

        mesh.SetFloatOverride(matProp_melanin, skinMelanin, true); // override properties are only updated per type when updating material
        mesh.SetColorOverride(matProp_skinTint, skinTint, false);
        mesh.SetColorOverride(matProp_skinScatterTint, skinScatterTint, false);
        mesh.SetColorOverride(matProp_veinTint, veinTint, false);

        mesh.SetColorOverride(matProp_freckleColor, freckleColor, false);
        mesh.SetColorOverride(matProp_moleColor, moleColor, false);
        mesh.SetColorOverride(matProp_pimpleColor, pimpleColor, false);
        mesh.SetColorOverride(matProp_scarringColor, scarringColor, true);

        mesh.SetFloatOverride(matProp_skinDetailScale, math.remap(0f, 1f, 23f, 0.9f, skinDetailScale), false);
        mesh.SetFloatOverride(matProp_skinDetailSeed, skinDetailSeed * 5f, true);
        mesh.SetVectorOverride(matProp_skinDetailStrengths, math.remap(0f, 1f, 2.5f, 0.16f, (float4)skinDetailStrengths), false);

        mesh.SetVectorOverride(matProp_skinDetailMaskScales, math.remap(0f, 1f, 100f, 0.01f, (float4)skinDetailMaskScales), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskSeeds, skinDetailMaskSeeds * 10f, false);
        mesh.SetVectorOverride(matProp_skinDetailMaskStrengths, math.remap(0f, 1f, 0.15f, 1.5f, (float4)skinDetailMaskStrengths), false);

        #region Masking

        mesh.SetVectorOverride(matProp_skinDetailMaskR_FaceLR, new Vector4(skinDetailMaskFaceL.x, skinDetailMaskFaceR.x, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskG_FaceLR, new Vector4(skinDetailMaskFaceL.y, skinDetailMaskFaceR.y, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskB_FaceLR, new Vector4(skinDetailMaskFaceL.z, skinDetailMaskFaceR.z, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskA_FaceLR, new Vector4(skinDetailMaskFaceL.w, skinDetailMaskFaceR.w, 0f, 0f), false);

        mesh.SetVectorOverride(matProp_skinDetailMaskR_HeadLR, new Vector4(skinDetailMaskHeadL.x, skinDetailMaskHeadR.x, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskG_HeadLR, new Vector4(skinDetailMaskHeadL.y, skinDetailMaskHeadR.y, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskB_HeadLR, new Vector4(skinDetailMaskHeadL.z, skinDetailMaskHeadR.z, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskA_HeadLR, new Vector4(skinDetailMaskHeadL.w, skinDetailMaskHeadR.w, 0f, 0f), false);

        mesh.SetVectorOverride(matProp_skinDetailMaskR_NeckLR, new Vector4(skinDetailMaskNeckL.x, skinDetailMaskNeckR.x, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskG_NeckLR, new Vector4(skinDetailMaskNeckL.y, skinDetailMaskNeckR.y, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskB_NeckLR, new Vector4(skinDetailMaskNeckL.z, skinDetailMaskNeckR.z, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskA_NeckLR, new Vector4(skinDetailMaskNeckL.w, skinDetailMaskNeckR.w, 0f, 0f), false);

        mesh.SetVectorOverride(matProp_skinDetailMaskR_ChestLR, new Vector4(skinDetailMaskChestL.x, skinDetailMaskChestR.x, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskG_ChestLR, new Vector4(skinDetailMaskChestL.y, skinDetailMaskChestR.y, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskB_ChestLR, new Vector4(skinDetailMaskChestL.z, skinDetailMaskChestR.z, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskA_ChestLR, new Vector4(skinDetailMaskChestL.w, skinDetailMaskChestR.w, 0f, 0f), false);

        mesh.SetVectorOverride(matProp_skinDetailMaskR_BackLR, new Vector4(skinDetailMaskBackL.x, skinDetailMaskBackR.x, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskG_BackLR, new Vector4(skinDetailMaskBackL.y, skinDetailMaskBackR.y, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskB_BackLR, new Vector4(skinDetailMaskBackL.z, skinDetailMaskBackR.z, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskA_BackLR, new Vector4(skinDetailMaskBackL.w, skinDetailMaskBackR.w, 0f, 0f), false);

        mesh.SetVectorOverride(matProp_skinDetailMaskR_TorsoLR, new Vector4(skinDetailMaskTorsoL.x, skinDetailMaskTorsoR.x, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskG_TorsoLR, new Vector4(skinDetailMaskTorsoL.y, skinDetailMaskTorsoR.y, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskB_TorsoLR, new Vector4(skinDetailMaskTorsoL.z, skinDetailMaskTorsoR.z, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskA_TorsoLR, new Vector4(skinDetailMaskTorsoL.w, skinDetailMaskTorsoR.w, 0f, 0f), false);

        mesh.SetVectorOverride(matProp_skinDetailMaskR_ArmsLR, new Vector4(skinDetailMaskArmsL.x, skinDetailMaskArmsR.x, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskG_ArmsLR, new Vector4(skinDetailMaskArmsL.y, skinDetailMaskArmsR.y, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskB_ArmsLR, new Vector4(skinDetailMaskArmsL.z, skinDetailMaskArmsR.z, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskA_ArmsLR, new Vector4(skinDetailMaskArmsL.w, skinDetailMaskArmsR.w, 0f, 0f), false);

        mesh.SetVectorOverride(matProp_skinDetailMaskR_HandsLR, new Vector4(skinDetailMaskHandsL.x, skinDetailMaskHandsR.x, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskG_HandsLR, new Vector4(skinDetailMaskHandsL.y, skinDetailMaskHandsR.y, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskB_HandsLR, new Vector4(skinDetailMaskHandsL.z, skinDetailMaskHandsR.z, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskA_HandsLR, new Vector4(skinDetailMaskHandsL.w, skinDetailMaskHandsR.w, 0f, 0f), false);

        mesh.SetVectorOverride(matProp_skinDetailMaskR_LegsLR, new Vector4(skinDetailMaskLegsL.x, skinDetailMaskLegsR.x, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskG_LegsLR, new Vector4(skinDetailMaskLegsL.y, skinDetailMaskLegsR.y, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskB_LegsLR, new Vector4(skinDetailMaskLegsL.z, skinDetailMaskLegsR.z, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskA_LegsLR, new Vector4(skinDetailMaskLegsL.w, skinDetailMaskLegsR.w, 0f, 0f), false);

        mesh.SetVectorOverride(matProp_skinDetailMaskR_FeetLR, new Vector4(skinDetailMaskFeetL.x, skinDetailMaskFeetR.x, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskG_FeetLR, new Vector4(skinDetailMaskFeetL.y, skinDetailMaskFeetR.y, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskB_FeetLR, new Vector4(skinDetailMaskFeetL.z, skinDetailMaskFeetR.z, 0f, 0f), false);
        mesh.SetVectorOverride(matProp_skinDetailMaskA_FeetLR, new Vector4(skinDetailMaskFeetL.w, skinDetailMaskFeetR.w, 0f, 0f), true);

        #endregion

    }
    public void ReapplySkinMaterialProperties()
    {
        ReapplySkinMaterialProperties(mainCharacterMesh);

        if (characterSkinMeshes != null)
        {
            foreach(var skinMesh in characterSkinMeshes)
            {
                ReapplySkinMaterialProperties(skinMesh);
            }
        }
    }

    #endregion

    #region Makeup

    public SelectableTextureGroup makeupStyles;

    public const string matProp_bodyPaintColor = "_BodyPaintColor";
    public const string matProp_bodyPaintSpecColor = "_BodyPaintSpecColor";
    public const string matProp_bodyPaintSmoothing = "_BodyPaintSmoothing";

    [UserCustomizable]
    private Color bodyPaintColor;
    public Color BodyPaintColor
    {
        get => bodyPaintColor;
        set
        {
            bodyPaintColor = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    [UserCustomizable]
    private Color bodyPaintSpecColor;
    public Color BodyPaintSpecColor
    {
        get => bodyPaintSpecColor;
        set
        {
            bodyPaintSpecColor = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    [UserCustomizable]
    private float bodyPaintSmoothing;
    public float BodyPaintSmoothing
    {
        get => bodyPaintSmoothing;
        set
        {
            bodyPaintSmoothing = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    #region Layer 1

    public const string matProp_makeupStyle_1 = "_MakeupStyle1";
    [UserCustomizable]
    private float makeupStyle_1;
    public float MakeupStyle_1
    {
        get => makeupStyle_1;
        set
        {
            makeupStyle_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupColorR_1 = "_MakeupColorR_1";
    public const string matProp_makeupColorG_1 = "_MakeupColorG_1";
    public const string matProp_makeupColorB_1 = "_MakeupColorB_1";
    public const string matProp_makeupColorA_1 = "_MakeupColorA_1";

    [UserCustomizable]
    private Color makeupColorR_1;
    public Color MakeupColorR_1
    {
        get => makeupColorR_1;
        set
        {
            makeupColorR_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupColorG_1;
    public Color MakeupColorG_1
    {
        get => makeupColorG_1;
        set
        {
            makeupColorG_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupColorB_1;
    public Color MakeupColorB_1
    {
        get => makeupColorB_1;
        set
        {
            makeupColorB_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupColorA_1;
    public Color MakeupColorA_1
    {
        get => makeupColorA_1;
        set
        {
            makeupColorA_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupSpecColorR_1 = "_MakeupSpecColorR_1";
    public const string matProp_makeupSpecColorG_1 = "_MakeupSpecColorG_1";
    public const string matProp_makeupSpecColorB_1 = "_MakeupSpecColorB_1";
    public const string matProp_makeupSpecColorA_1 = "_MakeupSpecColorA_1";

    [UserCustomizable]
    private Color makeupSpecColorR_1;
    public Color MakeupSpecColorR_1
    {
        get => makeupSpecColorR_1;
        set
        {
            makeupSpecColorR_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupSpecColorG_1;
    public Color MakeupSpecColorG_1
    {
        get => makeupSpecColorG_1;
        set
        {
            makeupSpecColorG_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupSpecColorB_1;
    public Color MakeupSpecColorB_1
    {
        get => makeupSpecColorB_1;
        set
        {
            makeupSpecColorB_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupSpecColorA_1;
    public Color MakeupSpecColorA_1
    {
        get => makeupSpecColorA_1;
        set
        {
            makeupSpecColorA_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupSmoothingR_1 = "_MakeupSmoothingR_1";
    public const string matProp_makeupSmoothingG_1 = "_MakeupSmoothingG_1";
    public const string matProp_makeupSmoothingB_1 = "_MakeupSmoothingB_1";
    public const string matProp_makeupSmoothingA_1 = "_MakeupSmoothingA_1";

    [UserCustomizable]
    private float makeupSmoothingR_1;
    public float MakeupSmoothingR_1
    {
        get => makeupSmoothingR_1;
        set
        {
            makeupSmoothingR_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupSmoothingG_1;
    public float MakeupSmoothingG_1
    {
        get => makeupSmoothingG_1;
        set
        {
            makeupSmoothingG_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupSmoothingB_1;
    public float MakeupSmoothingB_1
    {
        get => makeupSmoothingB_1;
        set
        {
            makeupSmoothingB_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupSmoothingA_1;
    public float MakeupSmoothingA_1
    {
        get => makeupSmoothingA_1;
        set
        {
            makeupSmoothingA_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupBlendTypeR_1 = "_MakeupBlendTypeR_1";
    public const string matProp_makeupBlendTypeG_1 = "_MakeupBlendTypeG_1";
    public const string matProp_makeupBlendTypeB_1 = "_MakeupBlendTypeB_1";
    public const string matProp_makeupBlendTypeA_1 = "_MakeupBlendTypeA_1";

    [UserCustomizable]
    private float makeupBlendTypeR_1;
    public float MakeupBlendTypeR_1
    {
        get => makeupBlendTypeR_1;
        set
        {
            makeupBlendTypeR_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupBlendTypeG_1;
    public float MakeupBlendTypeG_1
    {
        get => makeupBlendTypeG_1;
        set
        {
            makeupBlendTypeG_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupBlendTypeB_1;
    public float MakeupBlendTypeB_1
    {
        get => makeupBlendTypeB_1;
        set
        {
            makeupBlendTypeB_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupBlendTypeA_1;
    public float MakeupBlendTypeA_1
    {
        get => makeupBlendTypeA_1;
        set
        {
            makeupBlendTypeA_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupBlendR_1 = "_MakeupBlendR_LR_1";
    public const string matProp_makeupBlendG_1 = "_MakeupBlendG_LR_1";
    public const string matProp_makeupBlendB_1 = "_MakeupBlendB_LR_1";
    public const string matProp_makeupBlendA_1 = "_MakeupBlendA_LR_1";

    [UserCustomizable]
    private Vector2 makeupBlendR_1;
    public Vector2 MakeupBlendR_1
    {
        get => makeupBlendR_1;
        set
        {
            makeupBlendR_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector2 makeupBlendG_1;
    public Vector2 MakeupBlendG_1
    {
        get => makeupBlendG_1;
        set
        {
            makeupBlendG_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector2 makeupBlendB_1;
    public Vector2 MakeupBlendB_1
    {
        get => makeupBlendB_1;
        set
        {
            makeupBlendB_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector2 makeupBlendA_1;
    public Vector2 MakeupBlendA_1
    {
        get => makeupBlendA_1;
        set
        {
            makeupBlendA_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupStrengthR_1 = "_MakeupPowerR_1";
    public const string matProp_makeupStrengthG_1 = "_MakeupPowerG_1";
    public const string matProp_makeupStrengthB_1 = "_MakeupPowerB_1";
    public const string matProp_makeupStrengthA_1 = "_MakeupPowerA_1";

    [UserCustomizable]
    private float makeupStrengthR_1;
    public float MakeupStrengthR_1
    {
        get => makeupStrengthR_1;
        set
        {
            makeupStrengthR_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupStrengthG_1;
    public float MakeupStrengthG_1
    {
        get => makeupStrengthG_1;
        set
        {
            makeupStrengthG_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupStrengthB_1;
    public float MakeupStrengthB_1
    {
        get => makeupStrengthB_1;
        set
        {
            makeupStrengthB_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupStrengthA_1;
    public float MakeupStrengthA_1
    {
        get => makeupStrengthA_1;
        set
        {
            makeupStrengthA_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupFinalBlend_1 = "_MakeupFinalBlend_1";
    [UserCustomizable]
    private float makeupFinalBlend_1;
    public float MakeupFinalBlend_1
    {
        get => makeupFinalBlend_1;
        set
        {
            makeupFinalBlend_1 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    #endregion

    #region Layer 2

    public const string matProp_makeupStyle_2 = "_MakeupStyle2";
    [UserCustomizable]
    private float makeupStyle_2;
    public float MakeupStyle_2
    {
        get => makeupStyle_2;
        set
        {
            makeupStyle_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupColorR_2 = "_MakeupColorR_2";
    public const string matProp_makeupColorG_2 = "_MakeupColorG_2";
    public const string matProp_makeupColorB_2 = "_MakeupColorB_2";
    public const string matProp_makeupColorA_2 = "_MakeupColorA_2";

    [UserCustomizable]
    private Color makeupColorR_2;
    public Color MakeupColorR_2
    {
        get => makeupColorR_2;
        set
        {
            makeupColorR_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupColorG_2;
    public Color MakeupColorG_2
    {
        get => makeupColorG_2;
        set
        {
            makeupColorG_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupColorB_2;
    public Color MakeupColorB_2
    {
        get => makeupColorB_2;
        set
        {
            makeupColorB_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupColorA_2;
    public Color MakeupColorA_2
    {
        get => makeupColorA_2;
        set
        {
            makeupColorA_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupSpecColorR_2 = "_MakeupSpecColorR_2";
    public const string matProp_makeupSpecColorG_2 = "_MakeupSpecColorG_2";
    public const string matProp_makeupSpecColorB_2 = "_MakeupSpecColorB_2";
    public const string matProp_makeupSpecColorA_2 = "_MakeupSpecColorA_2";

    [UserCustomizable]
    private Color makeupSpecColorR_2;
    public Color MakeupSpecColorR_2
    {
        get => makeupSpecColorR_2;
        set
        {
            makeupSpecColorR_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupSpecColorG_2;
    public Color MakeupSpecColorG_2
    {
        get => makeupSpecColorG_2;
        set
        {
            makeupSpecColorG_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupSpecColorB_2;
    public Color MakeupSpecColorB_2
    {
        get => makeupSpecColorB_2;
        set
        {
            makeupSpecColorB_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupSpecColorA_2;
    public Color MakeupSpecColorA_2
    {
        get => makeupSpecColorA_2;
        set
        {
            makeupSpecColorA_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupSmoothingR_2 = "_MakeupSmoothingR_2";
    public const string matProp_makeupSmoothingG_2 = "_MakeupSmoothingG_2";
    public const string matProp_makeupSmoothingB_2 = "_MakeupSmoothingB_2";
    public const string matProp_makeupSmoothingA_2 = "_MakeupSmoothingA_2";

    [UserCustomizable]
    private float makeupSmoothingR_2;
    public float MakeupSmoothingR_2
    {
        get => makeupSmoothingR_2;
        set
        {
            makeupSmoothingR_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupSmoothingG_2;
    public float MakeupSmoothingG_2
    {
        get => makeupSmoothingG_2;
        set
        {
            makeupSmoothingG_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupSmoothingB_2;
    public float MakeupSmoothingB_2
    {
        get => makeupSmoothingB_2;
        set
        {
            makeupSmoothingB_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupSmoothingA_2;
    public float MakeupSmoothingA_2
    {
        get => makeupSmoothingA_2;
        set
        {
            makeupSmoothingA_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupBlendTypeR_2 = "_MakeupBlendTypeR_2";
    public const string matProp_makeupBlendTypeG_2 = "_MakeupBlendTypeG_2";
    public const string matProp_makeupBlendTypeB_2 = "_MakeupBlendTypeB_2";
    public const string matProp_makeupBlendTypeA_2 = "_MakeupBlendTypeA_2";

    [UserCustomizable]
    private float makeupBlendTypeR_2;
    public float MakeupBlendTypeR_2
    {
        get => makeupBlendTypeR_2;
        set
        {
            makeupBlendTypeR_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupBlendTypeG_2;
    public float MakeupBlendTypeG_2
    {
        get => makeupBlendTypeG_2;
        set
        {
            makeupBlendTypeG_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupBlendTypeB_2;
    public float MakeupBlendTypeB_2
    {
        get => makeupBlendTypeB_2;
        set
        {
            makeupBlendTypeB_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupBlendTypeA_2;
    public float MakeupBlendTypeA_2
    {
        get => makeupBlendTypeA_2;
        set
        {
            makeupBlendTypeA_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupBlendR_2 = "_MakeupBlendR_LR_2";
    public const string matProp_makeupBlendG_2 = "_MakeupBlendG_LR_2";
    public const string matProp_makeupBlendB_2 = "_MakeupBlendB_LR_2";
    public const string matProp_makeupBlendA_2 = "_MakeupBlendA_LR_2";

    [UserCustomizable]
    private Vector2 makeupBlendR_2;
    public Vector2 MakeupBlendR_2
    {
        get => makeupBlendR_2;
        set
        {
            makeupBlendR_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector2 makeupBlendG_2;
    public Vector2 MakeupBlendG_2
    {
        get => makeupBlendG_2;
        set
        {
            makeupBlendG_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector2 makeupBlendB_2;
    public Vector2 MakeupBlendB_2
    {
        get => makeupBlendB_2;
        set
        {
            makeupBlendB_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector2 makeupBlendA_2;
    public Vector2 MakeupBlendA_2
    {
        get => makeupBlendA_2;
        set
        {
            makeupBlendA_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupStrengthR_2 = "_MakeupPowerR_2";
    public const string matProp_makeupStrengthG_2 = "_MakeupPowerG_2";
    public const string matProp_makeupStrengthB_2 = "_MakeupPowerB_2";
    public const string matProp_makeupStrengthA_2 = "_MakeupPowerA_2";

    [UserCustomizable]
    private float makeupStrengthR_2;
    public float MakeupStrengthR_2
    {
        get => makeupStrengthR_2;
        set
        {
            makeupStrengthR_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupStrengthG_2;
    public float MakeupStrengthG_2
    {
        get => makeupStrengthG_2;
        set
        {
            makeupStrengthG_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupStrengthB_2;
    public float MakeupStrengthB_2
    {
        get => makeupStrengthB_2;
        set
        {
            makeupStrengthB_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupStrengthA_2;
    public float MakeupStrengthA_2
    {
        get => makeupStrengthA_2;
        set
        {
            makeupStrengthA_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupFinalBlend_2 = "_MakeupFinalBlend_2";
    [UserCustomizable]
    private float makeupFinalBlend_2;
    public float MakeupFinalBlend_2
    {
        get => makeupFinalBlend_2;
        set
        {
            makeupFinalBlend_2 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    #endregion

    #region Layer 3

    public const string matProp_makeupStyle_3 = "_MakeupStyle3";
    [UserCustomizable]
    private float makeupStyle_3;
    public float MakeupStyle_3
    {
        get => makeupStyle_3;
        set
        {
            makeupStyle_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupColorR_3 = "_MakeupColorR_3";
    public const string matProp_makeupColorG_3 = "_MakeupColorG_3";
    public const string matProp_makeupColorB_3 = "_MakeupColorB_3";
    public const string matProp_makeupColorA_3 = "_MakeupColorA_3";

    [UserCustomizable]
    private Color makeupColorR_3;
    public Color MakeupColorR_3
    {
        get => makeupColorR_3;
        set
        {
            makeupColorR_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupColorG_3;
    public Color MakeupColorG_3
    {
        get => makeupColorG_3;
        set
        {
            makeupColorG_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupColorB_3;
    public Color MakeupColorB_3
    {
        get => makeupColorB_3;
        set
        {
            makeupColorB_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupColorA_3;
    public Color MakeupColorA_3
    {
        get => makeupColorA_3;
        set
        {
            makeupColorA_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupSpecColorR_3 = "_MakeupSpecColorR_3";
    public const string matProp_makeupSpecColorG_3 = "_MakeupSpecColorG_3";
    public const string matProp_makeupSpecColorB_3 = "_MakeupSpecColorB_3";
    public const string matProp_makeupSpecColorA_3 = "_MakeupSpecColorA_3";

    [UserCustomizable]
    private Color makeupSpecColorR_3;
    public Color MakeupSpecColorR_3
    {
        get => makeupSpecColorR_3;
        set
        {
            makeupSpecColorR_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupSpecColorG_3;
    public Color MakeupSpecColorG_3
    {
        get => makeupSpecColorG_3;
        set
        {
            makeupSpecColorG_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupSpecColorB_3;
    public Color MakeupSpecColorB_3
    {
        get => makeupSpecColorB_3;
        set
        {
            makeupSpecColorB_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color makeupSpecColorA_3;
    public Color MakeupSpecColorA_3
    {
        get => makeupSpecColorA_3;
        set
        {
            makeupSpecColorA_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupSmoothingR_3 = "_MakeupSmoothingR_3";
    public const string matProp_makeupSmoothingG_3 = "_MakeupSmoothingG_3";
    public const string matProp_makeupSmoothingB_3 = "_MakeupSmoothingB_3";
    public const string matProp_makeupSmoothingA_3 = "_MakeupSmoothingA_3";

    [UserCustomizable]
    private float makeupSmoothingR_3;
    public float MakeupSmoothingR_3
    {
        get => makeupSmoothingR_3;
        set
        {
            makeupSmoothingR_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupSmoothingG_3;
    public float MakeupSmoothingG_3
    {
        get => makeupSmoothingG_3;
        set
        {
            makeupSmoothingG_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupSmoothingB_3;
    public float MakeupSmoothingB_3
    {
        get => makeupSmoothingB_3;
        set
        {
            makeupSmoothingB_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupSmoothingA_3;
    public float MakeupSmoothingA_3
    {
        get => makeupSmoothingA_3;
        set
        {
            makeupSmoothingA_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupBlendTypeR_3 = "_MakeupBlendTypeR_3";
    public const string matProp_makeupBlendTypeG_3 = "_MakeupBlendTypeG_3";
    public const string matProp_makeupBlendTypeB_3 = "_MakeupBlendTypeB_3";
    public const string matProp_makeupBlendTypeA_3 = "_MakeupBlendTypeA_3";

    [UserCustomizable]
    private float makeupBlendTypeR_3;
    public float MakeupBlendTypeR_3
    {
        get => makeupBlendTypeR_3;
        set
        {
            makeupBlendTypeR_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupBlendTypeG_3;
    public float MakeupBlendTypeG_3
    {
        get => makeupBlendTypeG_3;
        set
        {
            makeupBlendTypeG_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupBlendTypeB_3;
    public float MakeupBlendTypeB_3
    {
        get => makeupBlendTypeB_3;
        set
        {
            makeupBlendTypeB_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupBlendTypeA_3;
    public float MakeupBlendTypeA_3
    {
        get => makeupBlendTypeA_3;
        set
        {
            makeupBlendTypeA_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupBlendR_3 = "_MakeupBlendR_LR_3";
    public const string matProp_makeupBlendG_3 = "_MakeupBlendG_LR_3";
    public const string matProp_makeupBlendB_3 = "_MakeupBlendB_LR_3";
    public const string matProp_makeupBlendA_3 = "_MakeupBlendA_LR_3";

    [UserCustomizable]
    private Vector2 makeupBlendR_3;
    public Vector2 MakeupBlendR_3
    {
        get => makeupBlendR_3;
        set
        {
            makeupBlendR_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector2 makeupBlendG_3;
    public Vector2 MakeupBlendG_3
    {
        get => makeupBlendG_3;
        set
        {
            makeupBlendG_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector2 makeupBlendB_3;
    public Vector2 MakeupBlendB_3
    {
        get => makeupBlendB_3;
        set
        {
            makeupBlendB_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private Vector2 makeupBlendA_3;
    public Vector2 MakeupBlendA_3
    {
        get => makeupBlendA_3;
        set
        {
            makeupBlendA_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupStrengthR_3 = "_MakeupPowerR_3";
    public const string matProp_makeupStrengthG_3 = "_MakeupPowerG_3";
    public const string matProp_makeupStrengthB_3 = "_MakeupPowerB_3";
    public const string matProp_makeupStrengthA_3 = "_MakeupPowerA_3";

    [UserCustomizable]
    private float makeupStrengthR_3;
    public float MakeupStrengthR_3
    {
        get => makeupStrengthR_3;
        set
        {
            makeupStrengthR_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupStrengthG_3;
    public float MakeupStrengthG_3
    {
        get => makeupStrengthG_3;
        set
        {
            makeupStrengthG_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupStrengthB_3;
    public float MakeupStrengthB_3
    {
        get => makeupStrengthB_3;
        set
        {
            makeupStrengthB_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }
    [UserCustomizable]
    private float makeupStrengthA_3;
    public float MakeupStrengthA_3
    {
        get => makeupStrengthA_3;
        set
        {
            makeupStrengthA_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    public const string matProp_makeupFinalBlend_3 = "_MakeupFinalBlend_3";
    [UserCustomizable]
    private float makeupFinalBlend_3;
    public float MakeupFinalBlend_3
    {
        get => makeupFinalBlend_3;
        set
        {
            makeupFinalBlend_3 = value;
            ReapplyMakeupMaterialProperties();
        }
    }

    #endregion

    public const float _makeupStrengthRemapMin = 10f;
    public const float _makeupStrengthRemapMax = 0.15f;

    protected void ReapplyMakeupMaterialProperties(ICustomizableCharacter mesh)
    {
        if (mesh == null) return;

        mesh.SetColorOverride(matProp_bodyPaintColor, bodyPaintColor, false);
        mesh.SetColorOverride(matProp_bodyPaintSpecColor, bodyPaintSpecColor, false);
        mesh.SetFloatOverride(matProp_bodyPaintSmoothing, bodyPaintSmoothing, false);

        #region Layer 1

        mesh.SetFloatOverride(matProp_makeupStyle_1, makeupStyle_1, false);

        mesh.SetFloatOverride(matProp_makeupFinalBlend_1, makeupFinalBlend_1, false);

        mesh.SetColorOverride(matProp_makeupColorR_1, makeupColorR_1, false);
        mesh.SetColorOverride(matProp_makeupColorG_1, makeupColorG_1, false);
        mesh.SetColorOverride(matProp_makeupColorB_1, makeupColorB_1, false);
        mesh.SetColorOverride(matProp_makeupColorA_1, makeupColorA_1, false);

        mesh.SetColorOverride(matProp_makeupSpecColorR_1, makeupSpecColorR_1, false);
        mesh.SetColorOverride(matProp_makeupSpecColorG_1, makeupSpecColorG_1, false);
        mesh.SetColorOverride(matProp_makeupSpecColorB_1, makeupSpecColorB_1, false);
        mesh.SetColorOverride(matProp_makeupSpecColorA_1, makeupSpecColorA_1, false);

        mesh.SetFloatOverride(matProp_makeupSmoothingR_1, makeupSmoothingR_1, false);
        mesh.SetFloatOverride(matProp_makeupSmoothingG_1, makeupSmoothingG_1, false);
        mesh.SetFloatOverride(matProp_makeupSmoothingB_1, makeupSmoothingB_1, false);
        mesh.SetFloatOverride(matProp_makeupSmoothingA_1, makeupSmoothingA_1, false);

        mesh.SetFloatOverride(matProp_makeupBlendTypeR_1, makeupBlendTypeR_1, false);
        mesh.SetFloatOverride(matProp_makeupBlendTypeG_1, makeupBlendTypeG_1, false);
        mesh.SetFloatOverride(matProp_makeupBlendTypeB_1, makeupBlendTypeB_1, false);
        mesh.SetFloatOverride(matProp_makeupBlendTypeA_1, makeupBlendTypeA_1, false);

        mesh.SetVectorOverride(matProp_makeupBlendR_1, makeupBlendR_1, false);
        mesh.SetVectorOverride(matProp_makeupBlendG_1, makeupBlendG_1, false);
        mesh.SetVectorOverride(matProp_makeupBlendB_1, makeupBlendB_1, false);
        mesh.SetVectorOverride(matProp_makeupBlendA_1, makeupBlendA_1, false);

        mesh.SetFloatOverride(matProp_makeupStrengthR_1, math.remap(0f, 1f, _makeupStrengthRemapMin, _makeupStrengthRemapMax, makeupStrengthR_1), false);
        mesh.SetFloatOverride(matProp_makeupStrengthG_1, math.remap(0f, 1f, _makeupStrengthRemapMin, _makeupStrengthRemapMax, makeupStrengthG_1), false);
        mesh.SetFloatOverride(matProp_makeupStrengthB_1, math.remap(0f, 1f, _makeupStrengthRemapMin, _makeupStrengthRemapMax, makeupStrengthB_1), false);
        mesh.SetFloatOverride(matProp_makeupStrengthA_1, math.remap(0f, 1f, _makeupStrengthRemapMin, _makeupStrengthRemapMax, makeupStrengthA_1), false);

        #endregion

        #region Layer 2

        mesh.SetFloatOverride(matProp_makeupStyle_2, makeupStyle_2, false);

        mesh.SetFloatOverride(matProp_makeupFinalBlend_2, makeupFinalBlend_2, false);

        mesh.SetColorOverride(matProp_makeupColorR_2, makeupColorR_2, false);
        mesh.SetColorOverride(matProp_makeupColorG_2, makeupColorG_2, false);
        mesh.SetColorOverride(matProp_makeupColorB_2, makeupColorB_2, false);
        mesh.SetColorOverride(matProp_makeupColorA_2, makeupColorA_2, false);

        mesh.SetColorOverride(matProp_makeupSpecColorR_2, makeupSpecColorR_2, false);
        mesh.SetColorOverride(matProp_makeupSpecColorG_2, makeupSpecColorG_2, false);
        mesh.SetColorOverride(matProp_makeupSpecColorB_2, makeupSpecColorB_2, false);
        mesh.SetColorOverride(matProp_makeupSpecColorA_2, makeupSpecColorA_2, false);

        mesh.SetFloatOverride(matProp_makeupSmoothingR_2, makeupSmoothingR_2, false);
        mesh.SetFloatOverride(matProp_makeupSmoothingG_2, makeupSmoothingG_2, false);
        mesh.SetFloatOverride(matProp_makeupSmoothingB_2, makeupSmoothingB_2, false);
        mesh.SetFloatOverride(matProp_makeupSmoothingA_2, makeupSmoothingA_2, false);

        mesh.SetFloatOverride(matProp_makeupBlendTypeR_2, makeupBlendTypeR_2, false);
        mesh.SetFloatOverride(matProp_makeupBlendTypeG_2, makeupBlendTypeG_2, false);
        mesh.SetFloatOverride(matProp_makeupBlendTypeB_2, makeupBlendTypeB_2, false);
        mesh.SetFloatOverride(matProp_makeupBlendTypeA_2, makeupBlendTypeA_2, false);

        mesh.SetVectorOverride(matProp_makeupBlendR_2, makeupBlendR_2, false);
        mesh.SetVectorOverride(matProp_makeupBlendG_2, makeupBlendG_2, false);
        mesh.SetVectorOverride(matProp_makeupBlendB_2, makeupBlendB_2, false);
        mesh.SetVectorOverride(matProp_makeupBlendA_2, makeupBlendA_2, false);

        mesh.SetFloatOverride(matProp_makeupStrengthR_2, math.remap(0f, 1f, _makeupStrengthRemapMin, _makeupStrengthRemapMax, makeupStrengthR_2), false);
        mesh.SetFloatOverride(matProp_makeupStrengthG_2, math.remap(0f, 1f, _makeupStrengthRemapMin, _makeupStrengthRemapMax, makeupStrengthG_2), false);
        mesh.SetFloatOverride(matProp_makeupStrengthB_2, math.remap(0f, 1f, _makeupStrengthRemapMin, _makeupStrengthRemapMax, makeupStrengthB_2), false);
        mesh.SetFloatOverride(matProp_makeupStrengthA_2, math.remap(0f, 1f, _makeupStrengthRemapMin, _makeupStrengthRemapMax, makeupStrengthA_2), false);

        #endregion

        #region Layer 3

        mesh.SetFloatOverride(matProp_makeupStyle_3, makeupStyle_3, false);

        mesh.SetFloatOverride(matProp_makeupFinalBlend_3, makeupFinalBlend_3, false);

        mesh.SetColorOverride(matProp_makeupColorR_3, makeupColorR_3, false);
        mesh.SetColorOverride(matProp_makeupColorG_3, makeupColorG_3, false);
        mesh.SetColorOverride(matProp_makeupColorB_3, makeupColorB_3, false);
        mesh.SetColorOverride(matProp_makeupColorA_3, makeupColorA_3, false);

        mesh.SetColorOverride(matProp_makeupSpecColorR_3, makeupSpecColorR_3, false);
        mesh.SetColorOverride(matProp_makeupSpecColorG_3, makeupSpecColorG_3, false);
        mesh.SetColorOverride(matProp_makeupSpecColorB_3, makeupSpecColorB_3, false);
        mesh.SetColorOverride(matProp_makeupSpecColorA_3, makeupSpecColorA_3, true);

        mesh.SetFloatOverride(matProp_makeupSmoothingR_3, makeupSmoothingR_3, false);
        mesh.SetFloatOverride(matProp_makeupSmoothingG_3, makeupSmoothingG_3, false);
        mesh.SetFloatOverride(matProp_makeupSmoothingB_3, makeupSmoothingB_3, false);
        mesh.SetFloatOverride(matProp_makeupSmoothingA_3, makeupSmoothingA_3, false);

        mesh.SetFloatOverride(matProp_makeupBlendTypeR_3, makeupBlendTypeR_3, false);
        mesh.SetFloatOverride(matProp_makeupBlendTypeG_3, makeupBlendTypeG_3, false);
        mesh.SetFloatOverride(matProp_makeupBlendTypeB_3, makeupBlendTypeB_3, false);
        mesh.SetFloatOverride(matProp_makeupBlendTypeA_3, makeupBlendTypeA_3, false);

        mesh.SetVectorOverride(matProp_makeupBlendR_3, makeupBlendR_3, false);
        mesh.SetVectorOverride(matProp_makeupBlendG_3, makeupBlendG_3, false);
        mesh.SetVectorOverride(matProp_makeupBlendB_3, makeupBlendB_3, false);
        mesh.SetVectorOverride(matProp_makeupBlendA_3, makeupBlendA_3, true);

        mesh.SetFloatOverride(matProp_makeupStrengthR_3, math.remap(0f, 1f, _makeupStrengthRemapMin, _makeupStrengthRemapMax, makeupStrengthR_3), false);
        mesh.SetFloatOverride(matProp_makeupStrengthG_3, math.remap(0f, 1f, _makeupStrengthRemapMin, _makeupStrengthRemapMax, makeupStrengthG_3), false);
        mesh.SetFloatOverride(matProp_makeupStrengthB_3, math.remap(0f, 1f, _makeupStrengthRemapMin, _makeupStrengthRemapMax, makeupStrengthB_3), false);
        mesh.SetFloatOverride(matProp_makeupStrengthA_3, math.remap(0f, 1f, _makeupStrengthRemapMin, _makeupStrengthRemapMax, makeupStrengthA_3), true);

        #endregion

    }
    public void ReapplyMakeupMaterialProperties()
    {
        if (mainCharacterMesh != null) ReapplyMakeupMaterialProperties(mainCharacterMesh);

        if (characterSkinMeshes != null)
        {
            foreach (var skinMesh in characterSkinMeshes)
            {
                ReapplyMakeupMaterialProperties(skinMesh);
            }
        }
    }

    #endregion

    #region Body

    public const string matProp_faceVertexGroupIndex = "_FaceVertexGroupIndex";
    public const string matProp_headVertexGroupIndex = "_HeadVertexGroupIndex";
    public const string matProp_neckVertexGroupIndex = "_NeckVertexGroupIndex";
    public const string matProp_chestVertexGroupIndex = "_ChestVertexGroupIndex";
    public const string matProp_backVertexGroupIndex = "_BackVertexGroupIndex";
    public const string matProp_torsoVertexGroupIndex = "_TorsoVertexGroupIndex";
    public const string matProp_armsVertexGroupIndex = "_ArmsVertexGroupIndex";
    public const string matProp_handsVertexGroupIndex = "_HandsVertexGroupIndex";
    public const string matProp_legsVertexGroupIndex = "_LegsVertexGroupIndex";
    public const string matProp_feetVertexGroupIndex = "_FeetVertexGroupIndex";

    [Header("Body"), SerializeField]
    protected string faceVertexGroup;
    [SerializeField]
    protected string headVertexGroup;
    [SerializeField]
    protected string neckVertexGroup;
    [SerializeField]
    protected string chestVertexGroup;
    [SerializeField]
    protected string backVertexGroup;
    [SerializeField]
    protected string torsoVertexGroup;
    [SerializeField]
    protected string armsVertexGroup;
    [SerializeField]
    protected string handsVertexGroup;
    [SerializeField]
    protected string legsVertexGroup;
    [SerializeField]
    protected string feetVertexGroup;

    public void FetchBodyMaskVertexGroupIndicesFor(CustomizableCharacterMeshData data) 
    {
        if (data != null) 
        {
            var faceVertexGroupIndex = data.IndexOfVertexGroup(faceVertexGroup);
            var headVertexGroupIndex = data.IndexOfVertexGroup(headVertexGroup);
            var neckVertexGroupIndex = data.IndexOfVertexGroup(neckVertexGroup);
            var chestVertexGroupIndex = data.IndexOfVertexGroup(chestVertexGroup);
            var backVertexGroupIndex = data.IndexOfVertexGroup(backVertexGroup);
            var torsoVertexGroupIndex = data.IndexOfVertexGroup(torsoVertexGroup);
            var armsVertexGroupIndex = data.IndexOfVertexGroup(armsVertexGroup);
            var handsVertexGroupIndex = data.IndexOfVertexGroup(handsVertexGroup);
            var legsVertexGroupIndex = data.IndexOfVertexGroup(legsVertexGroup);
            var feetVertexGroupIndex = data.IndexOfVertexGroup(feetVertexGroup);

            if (data.meshGroups != null)
            {
                for (int a = 0; a < data.meshGroups.Length; a++)
                {
                    var group = data.meshGroups[a];
                    if (group == null) continue;

                    for(int b = 0; b < group.MaterialCount; b++)
                    {
                        var mat = group.GetMaterial(b);
                        if (mat == null) continue;

                        // properties defined in hlsl files are not recognized by HasProperty
                        /*if (mat.HasProperty(matProp_faceVertexGroupIndex))*/ mat.SetInteger(matProp_faceVertexGroupIndex, faceVertexGroupIndex);
                        /*if (mat.HasProperty(matProp_headVertexGroupIndex))*/ mat.SetInteger(matProp_headVertexGroupIndex, headVertexGroupIndex);
                        /*if (mat.HasProperty(matProp_neckVertexGroupIndex))*/ mat.SetInteger(matProp_neckVertexGroupIndex, neckVertexGroupIndex);
                        /*if (mat.HasProperty(matProp_chestVertexGroupIndex))*/ mat.SetInteger(matProp_chestVertexGroupIndex, chestVertexGroupIndex);
                        /*if (mat.HasProperty(matProp_backVertexGroupIndex))*/ mat.SetInteger(matProp_backVertexGroupIndex, backVertexGroupIndex);
                        /*if (mat.HasProperty(matProp_torsoVertexGroupIndex))*/ mat.SetInteger(matProp_torsoVertexGroupIndex, torsoVertexGroupIndex);
                        /*if (mat.HasProperty(matProp_armsVertexGroupIndex))*/ mat.SetInteger(matProp_armsVertexGroupIndex, armsVertexGroupIndex);
                        /*if (mat.HasProperty(matProp_handsVertexGroupIndex))*/ mat.SetInteger(matProp_handsVertexGroupIndex, handsVertexGroupIndex);
                        /*if (mat.HasProperty(matProp_legsVertexGroupIndex))*/ mat.SetInteger(matProp_legsVertexGroupIndex, legsVertexGroupIndex);
                        /*if (mat.HasProperty(matProp_feetVertexGroupIndex))*/ mat.SetInteger(matProp_feetVertexGroupIndex, feetVertexGroupIndex); 
                    }
                }
            }
        }
    }
    public void FetchBodyMaskVertexGroupIndicesFor(ICustomizableCharacter data)
    {
        if (data != null)
        {
            if (data is CustomizableCharacterMesh ccm)
            {
                FetchBodyMaskVertexGroupIndicesFor(ccm.CharacterMeshData);
                return;
            }

            var faceVertexGroupIndex = data.IndexOfVertexGroup(faceVertexGroup);
            var headVertexGroupIndex = data.IndexOfVertexGroup(headVertexGroup);
            var neckVertexGroupIndex = data.IndexOfVertexGroup(neckVertexGroup);
            var chestVertexGroupIndex = data.IndexOfVertexGroup(chestVertexGroup);
            var backVertexGroupIndex = data.IndexOfVertexGroup(backVertexGroup);
            var torsoVertexGroupIndex = data.IndexOfVertexGroup(torsoVertexGroup);
            var armsVertexGroupIndex = data.IndexOfVertexGroup(armsVertexGroup);
            var handsVertexGroupIndex = data.IndexOfVertexGroup(handsVertexGroup);
            var legsVertexGroupIndex = data.IndexOfVertexGroup(legsVertexGroup);
            var feetVertexGroupIndex = data.IndexOfVertexGroup(feetVertexGroup);

            data.SetIntegerOverride(matProp_faceVertexGroupIndex, faceVertexGroupIndex, false);
            data.SetIntegerOverride(matProp_headVertexGroupIndex, headVertexGroupIndex, false);
            data.SetIntegerOverride(matProp_neckVertexGroupIndex, neckVertexGroupIndex, false);
            data.SetIntegerOverride(matProp_chestVertexGroupIndex, chestVertexGroupIndex, false);
            data.SetIntegerOverride(matProp_backVertexGroupIndex, backVertexGroupIndex, false);
            data.SetIntegerOverride(matProp_torsoVertexGroupIndex, torsoVertexGroupIndex, false);
            data.SetIntegerOverride(matProp_armsVertexGroupIndex, armsVertexGroupIndex, false);
            data.SetIntegerOverride(matProp_handsVertexGroupIndex, handsVertexGroupIndex, false);
            data.SetIntegerOverride(matProp_legsVertexGroupIndex, legsVertexGroupIndex, false);
            data.SetIntegerOverride(matProp_feetVertexGroupIndex, feetVertexGroupIndex, true);
        }
    }

    #endregion

    #region Hair

    [Header("Hair")]
    public CharacterAdditionCollection hairstyles;
    public ColorPaletteGroup hairColorPresets;

    public const string matProp_hairColor = "_BaseColor";
    public const string matProp_hairSecondaryColor = "_SecondaryColor";
    public const string matProp_hairSmoothness = "_Smoothness";

    public const string matProp_hairSpecularColor = "_SpecularColor";

    public const string matProp_hairPrimarySpecTint = "_PrimarySpecularTint";
    public const string matProp_hairPrimaryLobeSmoothness = "_PrimarySmoothness";

    public const string matProp_hairSecondarySpecTint = "_SecondarySpecularTint";
    public const string matProp_hairSecondaryLobeSmoothness = "_SecondarySmoothness";

    [UserCustomizable]
    private Color hairColor;
    public Color HairColor
    {
        get => hairColor;
        set
        {
            hairColor = value;
            ReapplyHairMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color hairSecondaryColor;
    public Color HairSecondaryColor
    {
        get => hairSecondaryColor;
        set
        {
            hairSecondaryColor = value;
            ReapplyHairMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color hairSpecularColor;
    public Color HairSpecularColor
    {
        get => hairSpecularColor;
        set
        {
            hairSpecularColor = value;
            ReapplyHairMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color hairPrimarySpecularTint;
    public Color HairPrimarySpecularTint
    {
        get => hairPrimarySpecularTint;
        set
        {
            hairPrimarySpecularTint = value;
            ReapplyHairMaterialProperties();
        }
    }
    [UserCustomizable]
    private Color hairSecondarySpecularTint;
    public Color HairSecondarySpecularTint
    {
        get => hairSecondarySpecularTint;
        set
        {
            hairSecondarySpecularTint = value;
            ReapplyHairMaterialProperties();
        }
    }

    [UserCustomizable]
    private float hairSmoothness;
    public float HairSmoothness
    {
        get => hairSmoothness;
        set
        {
            hairSmoothness = value;
            ReapplyHairMaterialProperties();
        }
    }

    [UserCustomizable]
    private float hairPrimaryLobeSmoothness;
    public float HairPrimaryLobeSmoothness
    {
        get => hairPrimaryLobeSmoothness;
        set
        {
            hairPrimaryLobeSmoothness = value;
            ReapplyHairMaterialProperties();
        }
    }
    [UserCustomizable]
    private float hairSecondaryLobeSmoothness;
    public float HairSecondaryLobeSmoothness
    {
        get => hairSecondaryLobeSmoothness;
        set
        {
            hairSecondaryLobeSmoothness = value;
            ReapplyHairMaterialProperties();
        }
    }

    protected GameObject hairRoot;
    protected Material[] hairMaterials;

    [UserCustomizable]
    private int hairStyle;
    public int HairStyle
    {
        get => hairStyle;
        set => SetHairStyle(value);
    }

    public void SetHairStyle(int index)
    {
        index = index - 1; // -1 (index 0) means no hair

        if (hairRoot != null) Destroy(hairRoot);
        if (hairMaterials != null)
        {
            foreach (var mat in hairMaterials) if (mat != null) Destroy(mat);
        }

        hairRoot = null;
        hairMaterials = null;

        if (hairstyles != null && index >= 0 && index < hairstyles.Count)
        {
            var style = hairstyles.GetAddition(index);
            if (style != null)
            {
                hairRoot = style.Spawn(transform);
                if (hairRoot != null)
                {
                    switch(style.meshType)
                    {
                        case CharacterAddition.MeshType.UnitySkinnedMesh:
                            if (style.materials != null)
                            {
                                hairMaterials = new Material[style.materials.Length];
                                for(int a = 0; a < hairMaterials.Length; a++) hairMaterials[a] = style.materials[a] == null ? null : Instantiate(style.materials[a]);

                                var smrs = hairRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                                foreach (var smr in smrs) smr.sharedMaterials = hairMaterials;
                            }
                            break;
                    }
                }

                ReapplyHairMaterialProperties();
            }
        }
    }

    private static readonly Vector2 _hairSmoothnessRange = new Vector2(0.4f, 0.9f);
    private static readonly Vector2 _hairPrimaryLobeSmoothnessRange = new Vector2(0.4f, 0.9f);
    private static readonly Vector2 _hairSecondaryLobeSmoothnessRange = new Vector2(0.1f, 0.75f);

    public static readonly float _default_hairSmoothness = 0.5f;
    public static readonly float _default_hairPrimaryLobeSmoothness = 0.8f;
    public static readonly float _default_hairSecondaryLobeSmoothness = 0.538f;

    protected void ReapplyHairMaterialProperties(Material hairMaterial)
    {
        if (hairMaterial == null) return;

        hairMaterial.SetColor(matProp_hairColor, hairColor);
        hairMaterial.SetColor(matProp_hairSecondaryColor, hairSecondaryColor);

        hairMaterial.SetColor(matProp_hairSpecularColor, hairSpecularColor);

        hairMaterial.SetColor(matProp_hairPrimarySpecTint, hairPrimarySpecularTint);
        hairMaterial.SetColor(matProp_hairSecondarySpecTint, hairSecondarySpecularTint);

        hairMaterial.SetFloat(matProp_hairSmoothness, Mathf.Lerp(_hairSmoothnessRange.x, _hairSmoothnessRange.y, hairSmoothness));
        hairMaterial.SetFloat(matProp_hairPrimaryLobeSmoothness, Mathf.Lerp(_hairPrimaryLobeSmoothnessRange.x, _hairPrimaryLobeSmoothnessRange.y, hairPrimaryLobeSmoothness));
        hairMaterial.SetFloat(matProp_hairSecondaryLobeSmoothness, Mathf.Lerp(_hairSecondaryLobeSmoothnessRange.x, _hairSecondaryLobeSmoothnessRange.y, hairSecondaryLobeSmoothness));
    }
    public void ReapplyHairMaterialProperties()
    {
        if (hairMaterials != null)
        {
            foreach (var mat in hairMaterials) ReapplyHairMaterialProperties(mat);
        }
    }

    #endregion

    #region Movement

    [UserCustomizable]
    private float idleAura;
    public float IdleAura
    {
        get => idleAura;
        set
        {
            idleAura = value;
            ReapplyMovementParameters();
        }
    }
    public void SetIdleAura(float val) => IdleAura = val;

    [UserCustomizable]
    private float idleVibes;
    public float IdleVibes
    {
        get => idleVibes;
        set
        {
            idleVibes = value;
            ReapplyMovementParameters();
        }
    }
    public void SetIdleVibes(float val) => IdleVibes = val;

    [UserCustomizable]
    private float idleFlair;
    public float IdleFlair
    {
        get => idleFlair;
        set
        {
            idleFlair = value;
            ReapplyMovementParameters();
        }
    }
    public void SetIdleFlair(float val) => IdleFlair = val;

    [UserCustomizable]
    private float idleLean;
    public float IdleLean
    {
        get => idleLean;
        set
        {
            idleLean = value;
            ReapplyMovementParameters();
        }
    }

    public void SetIdleLean(float val) => IdleLean = val;

    public void ReapplyMovementParameters()
    {
        //if (character != null && character.AnimationController != null && character.Animator != null)
        if (animator != null)
        {

            float boring = idleAura + idleVibes + idleFlair + idleLean;
            float div = boring <= 0f ? 1f : (boring > 1f ? boring : 1f);
            idleAura = idleAura / div;
            idleVibes = idleVibes / div;
            idleFlair = idleFlair / div;
            idleLean = idleLean / div;
            boring = boring > 1f ? 0f : (1f - boring); 

            var auraParam = animator.FindParameter($"aura");  
            if (auraParam != null) auraParam.SetValue(idleAura);

            var vibesParam = animator.FindParameter($"vibes");
            if (vibesParam != null) vibesParam.SetValue(idleVibes);

            var flairParam = animator.FindParameter($"flair");
            if (flairParam != null) flairParam.SetValue(idleFlair);

            var leanParam = animator.FindParameter($"lean"); 
            if (leanParam != null) leanParam.SetValue(idleLean);

            var boringParam = animator.FindParameter($"boring");
            if (boringParam != null) boringParam.SetValue(boring); 
        }
    }

    #endregion

    public CustomizationState GenerateCustomizationState() => CustomizationState.GetState(this);
    public void SetCustomizationState(CustomizationState state)
    {
        SetLeftIrisTexture(irisLeftTextureID);
        SetRightIrisTexture(irisRightTextureID);
        ReapplyEyeMaterialProperties();

        SyncCharacterHeight();
    }

    public CustomizationState GenerateKeyValueState()
    {
        var state = new CustomizationState();

        if (featureControllers != null)
        {
            state.floatProps = new CustomizableFloat[featureControllers.Count];
            int index = 0;
            foreach(var entry in featureControllers)
            {
                state.floatProps[index] = new CustomizableFloat()
                {
                    id = entry.Key,
                    value = entry.Value == null ? 0f : entry.Value.Value
                };

                index++;
            }
        }

        return state;
    }
    public AdvancedCustomizationState GenerateAdvancedCustomizationState() => new AdvancedCustomizationState()
    {
        baseState = GenerateCustomizationState(),
        keyValueState = GenerateKeyValueState(),
    };

    public void SetCustomizationKeyValueState(CustomizationState state)
    {
        if (featureControllers != null && state.floatProps != null && state.floatProps.Length > 0)
        {
            foreach(var prop in state.floatProps)
            {
                if (featureControllers.TryGetValue(prop.id, out var controller)) controller.SetValue(prop.value, mainCharacterMesh);
            }
        }
    }
    public void SetAdvancedCustomizationState(AdvancedCustomizationState state) => state.Apply(this);

    protected virtual void Awake()
    {
        /*if (character == null) SetCharacter(gameObject.GetComponentInChildren<IBulkOutCharacter>(true));
        if (character == null) SetCharacter(boCharacter);
        if (character == null) SetCharacter(basicBoCharacter);*/

        if (characterMeshes == null) characterMeshes = gameObject.GetComponent<CustomizableCharacterMeshCollection>();

        featureControllers = new Dictionary<string, CustomizableFeatureController>();
        valueGroups = new Dictionary<string, ValueGroup>();

        if (characterHeight == null) SetCharacterHeightController(gameObject.GetComponentInChildren<BipedalCharacterHeight>(true));
        if (genitalController == null) SetGenitalController(gameObject.GetComponentInChildren<GenitalController>(true));
        if (mainCharacterMesh == null) 
        { 
            if (string.IsNullOrWhiteSpace(mainCharacterMeshID))
            {
                if (characterMeshes != null)
                {
                    var firstMesh = characterMeshes[0];
                    if (firstMesh != null) mainCharacterMeshID = firstMesh.Name;
                }
            }

            SetMainCharacterMesh(mainCharacterMeshID); 
        }
        if (characterMeshes != null)
        {
            for (int a = 0; a < characterMeshes.MeshCount; a++)
            {
                var mesh = characterMeshes[a];
                if (mesh != null && mesh != mainCharacterMesh)
                {
                    AddCharacterSkinMesh(mesh);
                }
            }
        }

        if (customizableFeatures == null) customizableFeatures = new List<CustomizableFeature>();
        if (headBone != null)
        {
            customizableFeatures.Add(new CustomizableFeature()
            {
                id = _headScaleId,
                displayName = "Size",
                category = SandboxCharacterCreator._proportionsCategoryName,
                subCategory = "Head",
                defaultValue = 0.5f,
                mirror = true,
                skipTransformReset = !resetHeadBoneTransform,
                targetTransform = headBone,
                minScaleOffset = Vector3.one * headScaleOffsetRange.x,
                maxScaleOffset = Vector3.one * headScaleOffsetRange.y
            });
        }
        if (leftHandBone != null)
        {
            customizableFeatures.Add(new CustomizableFeature()
            {
                id = _handScaleId + "_left", 
                displayName = "Size",
                category = SandboxCharacterCreator._proportionsCategoryName,
                subCategory = "Hands",
                defaultValue = 0.5f,
                mirror = true,
                skipTransformReset = !resetHandBoneTransforms,
                targetTransform = leftHandBone,
                minScaleOffset = Vector3.one * handScaleOffsetRange.x,
                maxScaleOffset = Vector3.one * handScaleOffsetRange.y 
            });
        }
        if (leftFootBone != null)
        {
            customizableFeatures.Add(new CustomizableFeature()
            {
                id = _footScaleId + "_left",
                displayName = "Size",
                category = SandboxCharacterCreator._proportionsCategoryName,
                subCategory = "Feet",
                defaultValue = 0.5f,
                mirror = true,
                skipTransformReset = !resetFootBoneTransforms,
                targetTransform = leftFootBone,
                minScaleOffset = Vector3.one * footScaleOffsetRange.x,
                maxScaleOffset = Vector3.one * footScaleOffsetRange.y 
            });
        }

        if (eyeballLeft != null)
        {
            eyeballLeftMaterial = eyeballLeft.material;

            default_irisLeftTexture = eyeballLeftMaterial.GetTexture(matProp_irisMap);
            default_irisLeftNormalTexture = eyeballLeftMaterial.GetTexture(matProp_irisNormalMap);
            default_scleraLeftTexture = eyeballLeftMaterial.GetTexture(matProp_scleraMap);
            default_scleraLeftNormalTexture = eyeballLeftMaterial.GetTexture(matProp_scleraNormalMap);

            irisLeftColorA = eyeballLeftMaterial.GetColor(matProp_irisColorA);
            irisLeftColorB = eyeballLeftMaterial.GetColor(matProp_irisColorB);
            irisLeftColorC = eyeballLeftMaterial.GetColor(matProp_irisColorC);

            scleraLeftTint = eyeballLeftMaterial.GetColor(matProp_scleraTint);
            scleraLeftColorA = eyeballLeftMaterial.GetColor(matProp_scleraColorA);
            scleraLeftColorB = eyeballLeftMaterial.GetColor(matProp_scleraColorB);
            scleraLeftColorC = eyeballLeftMaterial.GetColor(matProp_scleraColorC);

            irisLeftSize = eyeballLeftMaterial.GetFloat(matProp_irisSize);
            //var pupilShape = eyeballLeftMaterial.GetVector(matProp_pupilShape);
            pupilLeftShapeX = 1f;
            pupilLeftShapeY = 1f;
            irisLeftIOR = eyeballLeftMaterial.GetFloat(matProp_irisIOR);
            irisLeftRefractionOffset = eyeballLeftMaterial.GetFloat(matProp_irisRefractionOffset);
            scleraLeftWetness = eyeballLeftMaterial.GetFloat(matProp_scleraWetness);
            corneaLeftWetness = eyeballLeftMaterial.GetFloat(matProp_corneaWetness);
        }

        if (eyeballRight != null)
        {
            eyeballRightMaterial = eyeballLeftMaterial;
            eyeballRight.sharedMaterial = eyeballRightMaterial;
            isUsingSingleEyeballMaterial = true;

            default_irisRightTexture = eyeballRightMaterial.GetTexture(matProp_irisMap);
            default_irisRightNormalTexture = eyeballRightMaterial.GetTexture(matProp_irisNormalMap);
            default_scleraRightTexture = eyeballRightMaterial.GetTexture(matProp_scleraMap);
            default_scleraRightNormalTexture = eyeballRightMaterial.GetTexture(matProp_scleraNormalMap);

            irisRightColorA = eyeballRightMaterial.GetColor(matProp_irisColorA);
            irisRightColorB = eyeballRightMaterial.GetColor(matProp_irisColorB);
            irisRightColorC = eyeballRightMaterial.GetColor(matProp_irisColorC);

            scleraRightTint = eyeballRightMaterial.GetColor(matProp_scleraTint);
            scleraRightColorA = eyeballRightMaterial.GetColor(matProp_scleraColorA);
            scleraRightColorB = eyeballRightMaterial.GetColor(matProp_scleraColorB);
            scleraRightColorC = eyeballRightMaterial.GetColor(matProp_scleraColorC);

            irisRightSize = eyeballRightMaterial.GetFloat(matProp_irisSize);
            //var pupilShape = eyeballRightMaterial.GetVector(matProp_pupilShape);
            pupilRightShapeX = 1f;
            pupilRightShapeY = 1f;
            irisRightIOR = eyeballRightMaterial.GetFloat(matProp_irisIOR);
            irisRightRefractionOffset = eyeballRightMaterial.GetFloat(matProp_irisRefractionOffset);
            scleraRightWetness = eyeballRightMaterial.GetFloat(matProp_scleraWetness);
            corneaRightWetness = eyeballRightMaterial.GetFloat(matProp_corneaWetness);
        }

        if (mainCharacterMesh != null)
        {
            FetchBodyMaskVertexGroupIndicesFor(mainCharacterMesh);
        }
        if (characterSkinMeshes != null)
        {
            foreach(var mesh in characterSkinMeshes)
            {
                FetchBodyMaskVertexGroupIndicesFor(mesh);
            }
        }

        makeupFinalBlend_1 = 0f;
        makeupFinalBlend_2 = 0f;
        makeupFinalBlend_3 = 0f;

        makeupBlendR_1 = makeupBlendR_2 = makeupBlendR_3 = Vector2.one;
        makeupBlendG_1 = makeupBlendG_2 = makeupBlendG_3 = Vector2.one;
        makeupBlendB_1 = makeupBlendB_2 = makeupBlendB_3 = Vector2.one;
        makeupBlendA_1 = makeupBlendA_2 = makeupBlendA_3 = Vector2.one;

        makeupColorR_1 = makeupColorR_2 = makeupColorR_3 = new Color(0.75f, 0.02f, 0f, 1f);
        makeupColorG_1 = makeupColorG_2 = makeupColorG_3 = new Color(0f, 0f, 0f, 1f);
        makeupColorB_1 = makeupColorB_2 = makeupColorB_3 = new Color(0.34f, 0.1f, 0.32f, 1f);
        makeupColorA_1 = makeupColorA_2 = makeupColorA_3 = new Color(0.85f, 0.22f, 0.28f, 1f);

        makeupSpecColorR_1 = makeupSpecColorR_2 = makeupSpecColorR_3 = new Color(0.3f, 0.3f, 0.3f, 0.7f);
        makeupSpecColorG_1 = makeupSpecColorG_2 = makeupSpecColorG_3 = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        makeupSpecColorB_1 = makeupSpecColorB_2 = makeupSpecColorB_3 = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        makeupSpecColorA_1 = makeupSpecColorA_2 = makeupSpecColorA_3 = new Color(0.3f, 0.3f, 0.3f, 0.3f);

        float makeupStrengthCenter = Mathf.Abs((1 - _makeupStrengthRemapMin) / (_makeupStrengthRemapMax - _makeupStrengthRemapMin));
        makeupStrengthR_1 = makeupStrengthR_2 = makeupStrengthR_3 = makeupStrengthCenter;
        makeupStrengthG_1 = makeupStrengthG_2 = makeupStrengthG_3 = makeupStrengthCenter;
        makeupStrengthB_1 = makeupStrengthB_2 = makeupStrengthB_3 = makeupStrengthCenter;
        makeupStrengthA_1 = makeupStrengthA_2 = makeupStrengthA_3 = makeupStrengthCenter;

        ColorUtility.TryParseHtmlString("#958855", out hairColor);
        ColorUtility.TryParseHtmlString("#906C43", out hairSecondaryColor);

        ColorUtility.TryParseHtmlString("#E0B45A", out hairSpecularColor);

        ColorUtility.TryParseHtmlString("#FFFFFF00", out hairPrimarySpecularTint);
        ColorUtility.TryParseHtmlString("#FFFFFF00", out hairSecondarySpecularTint);

        hairSmoothness = _default_hairSmoothness;
        hairPrimaryLobeSmoothness = _default_hairPrimaryLobeSmoothness;
        hairSecondaryLobeSmoothness = _default_hairSecondaryLobeSmoothness;
    }

    protected virtual void Start()
    {
        SetHideNipples(true);
        SetHideGenitals(true);

        if (customizableFeatures != null)
        {
            CacheFeatureTransforms();

            int count = customizableFeatures.Count;
            for (int a = 0; a < count; a++)
            {
                var feature = customizableFeatures[a]; 
                if (!feature.mirror) continue;

                var mirroredFeature = feature;
                mirroredFeature.mirror = false;

                if (mirroredFeature.targetTransform != null)
                {
                    string mirrorTransformName = Utils.GetMirroredName(mirroredFeature.targetTransform.name);

                    if (mirrorTransformName != mirroredFeature.targetTransform.name)
                    {
                        var root = mirroredFeature.targetTransform.parent;
                        while(root != null)
                        {
                            var mirroredTransform = root.FindDeepChild(mirrorTransformName);
                            if (mirroredTransform != null)
                            {
                                mirroredFeature.targetTransform = mirroredTransform;
                                break;
                            }

                            root = root.parent;
                        }
                    }

                    if (feature.mirrorAxis != Vector3.zero)
                    {
                        mirroredFeature.minOffset = Maths.Mirror(mirroredFeature.minOffset, feature.mirrorAxis);
                        mirroredFeature.maxOffset = Maths.Mirror(mirroredFeature.maxOffset, feature.mirrorAxis);
                    }
                    if (feature.mirrorEuler != Vector3.zero)
                    {
                        mirroredFeature.minRotationOffset = Vector3.Scale(mirroredFeature.minRotationOffset, feature.mirrorEuler);
                        mirroredFeature.maxRotationOffset = Vector3.Scale(mirroredFeature.maxRotationOffset, feature.mirrorEuler);
                    }
                }

                if (mirroredFeature.additionalTransformChanges != null && mirroredFeature.additionalTransformChanges.Length > 0)
                {
                    mirroredFeature.additionalTransformChanges = (TransformOffsets[])mirroredFeature.additionalTransformChanges.Clone();
                    for(int b = 0; b <  mirroredFeature.additionalTransformChanges.Length; b++)
                    {
                        var changes = mirroredFeature.additionalTransformChanges[b];

                        string mirrorTransformName = Utils.GetMirroredName(changes.targetTransform.name);
                        if (mirrorTransformName != changes.targetTransform.name)
                        {
                            var root = changes.targetTransform.parent;
                            while (root != null)
                            {
                                var mirroredTransform = root.FindDeepChild(mirrorTransformName);
                                if (mirroredTransform != null)
                                {
                                    changes.targetTransform = mirroredTransform;
                                    break;
                                }

                                root = root.parent;
                            }
                        }

                        if (changes.mirrorAxis != Vector3.zero)
                        {
                            changes.minOffset = Maths.Mirror(changes.minOffset, changes.mirrorAxis);
                            changes.maxOffset = Maths.Mirror(changes.maxOffset, changes.mirrorAxis);
                        }
                        if (changes.mirrorEuler != Vector3.zero)
                        {
                            changes.minRotationOffset = Vector3.Scale(changes.minRotationOffset, changes.mirrorEuler);
                            changes.maxRotationOffset = Vector3.Scale(changes.maxRotationOffset, changes.mirrorEuler); 
                        }

                        mirroredFeature.additionalTransformChanges[b] = changes; 
                    }
                }

                switch (mirroredFeature.optionType)
                {
                    case CustomizableOptionType.VariationLeft:
                        mirroredFeature.optionType = CustomizableOptionType.VariationRight;
                        break;
                        
                    case CustomizableOptionType.VariationRight:
                        mirroredFeature.optionType = CustomizableOptionType.VariationLeft;
                        break;
                }

                mirroredFeature.id = Utils.GetMirroredName(feature.id);
                mirroredFeature.displayName = Utils.GetMirroredName(feature.displayName);
                mirroredFeature.category = Utils.GetMirroredName(mirroredFeature.category); 
                mirroredFeature.subCategory = Utils.GetMirroredName(mirroredFeature.subCategory);
                mirroredFeature.valueGroup = Utils.GetMirroredName(mirroredFeature.valueGroup); 

                customizableFeatures.Add(mirroredFeature);
            }

            List<Transform> referencedTransforms = GetAffectedTransforms();
            foreach (var feature in customizableFeatures)
            {
                bool isAdditive = referencedTransforms.Contains(feature.targetTransform);

                var controller = new CustomizableFeatureController(this, feature);
                controller.Initialize(mainCharacterMesh, isAdditive, AutoOptimizeTransformChanges);

                featureControllers[feature.id] = controller;
                if (!isAdditive) referencedTransforms.Add(feature.targetTransform);

                if (!string.IsNullOrWhiteSpace(feature.valueGroup))
                {
                    if (!valueGroups.TryGetValue(feature.valueGroup, out var valueGroup))
                    {
                        valueGroup = new ValueGroup(feature.valueGroup);
                        valueGroups[feature.valueGroup] = valueGroup; 
                    }

                    valueGroup.AddFeature(controller);  
                }
            }

            referencedTransforms.Clear(); 
        }

        if (animator == null) animator = gameObject.GetComponentInChildren<CustomAnimator>(true);
        SetAnimator(animator);

        freckleColor = new Color(0f, 0f, 0f, 1f);
        moleColor = new Color(0f, 0f, 0f, 1f);
        pimpleColor = new Color(0.2f, 0f, 0f, 1f);
        scarringColor = new Color(0.1f, 0f, 0f, 0.75f);

        skinDetailScale = 0.5f;
        skinDetailSeed = 0.1f;
        skinDetailStrengths = new Vector4(0f, 0f, 0f, 0f);
        skinDetailMaskScales = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);

        skinDetailMaskFaceL = skinDetailMaskFaceR = new Vector4(1f, 1f, 1f, 1f);
        skinDetailMaskHeadL = skinDetailMaskHeadR = new Vector4(1f, 1f, 1f, 1f);
        skinDetailMaskNeckL = skinDetailMaskNeckR = new Vector4(1f, 1f, 1f, 1f);
        skinDetailMaskChestL = skinDetailMaskChestR = new Vector4(1f, 1f, 1f, 1f);
        skinDetailMaskBackL = skinDetailMaskBackR = new Vector4(1f, 1f, 1f, 1f);
        skinDetailMaskTorsoL = skinDetailMaskTorsoR = new Vector4(1f, 1f, 1f, 1f);
        skinDetailMaskArmsL = skinDetailMaskArmsR = new Vector4(1f, 1f, 1f, 1f);
        skinDetailMaskHandsL = skinDetailMaskHandsR = new Vector4(1f, 1f, 1f, 1f);
        skinDetailMaskLegsL = skinDetailMaskLegsR = new Vector4(1f, 1f, 1f, 1f);
        skinDetailMaskFeetL = skinDetailMaskFeetR = new Vector4(1f, 1f, 1f, 1f); 

        ReapplySkinMaterialProperties();

        //SetHairStyle(1);
    }

    public virtual CustomizableFeatureController GetFeatureController(string featureId)
    {
        if (featureControllers.TryGetValue(featureId, out var controller)) return controller;
        return null;
    }
    public virtual void SetFeatureValue(string feature, float value)
    {
        value = Mathf.Clamp01(value);

        if (featureControllers.TryGetValue(feature, out var controller))
        {
            controller.SetValueNoUpdate(value, true);
        }
    }

    public virtual void ApplyFeatures()
    {
        if (featureControllers != null && featureControllers.Count > 0)
        {
            if (!autoOptimizeTransformChanges || !hasAppliedFeatures)
            {
                if (mainCharacterMesh != null && mainCharacterMesh.IsInitialized)
                {
                    foreach (var entry in featureControllers)
                    {
                        //entry.Value.SetValue((UnityEngine.Random.value - 0.5f) * 2f, mainCharacterMesh, true, false);
                        entry.Value.Apply(mainCharacterMesh, !entry.Value.additive, false, animator != null && animator.isActiveAndEnabled);
                    }

                    hasAppliedFeatures = true; 
                }
            }
        }
    }
    protected virtual void PostAnimate()
    {
        ApplyFeatures();
    }
    protected virtual void OnEnableAnimator()
    {
        /*if (animator != null)
        {
            animator.AddListener(CustomAnimator.BehaviourEvent.OnPostLateUpdate, PostAnimate);
        }*/
    }
    protected virtual void OnDisableAnimator()
    {
        /*if (animator != null)
        {
            animator.RemoveListener(CustomAnimator.BehaviourEvent.OnPostLateUpdate, PostAnimate);
        }*/

        if (featureControllers != null && featureControllers.Count > 0)
        {
            foreach (var entry in featureControllers)
            {
                if (entry.Value != null) entry.Value.RemoveTransformChangeOptimization();
            }
        }

        hasAppliedFeatures = false;
    }

    public int DefaultRaycastLOD
    {
        get => mainCharacterMesh == null ? 0 : mainCharacterMesh.DefaultRaycastLOD;

        set
        {
            if (mainCharacterMesh != null) mainCharacterMesh.DefaultRaycastLOD = value;
        }
    }

    public bool RaycastAgainst(int lod, float3 rayOrigin, float3 rayOffset, out Maths.RaycastHitResult hit, float errorMargin = 0.01F)
    {
        hit = default;
        if (mainCharacterMesh == null) return false;

        return mainCharacterMesh.RaycastAgainst(lod, rayOrigin, rayOffset, out hit, errorMargin);
    }
}

public class SandboxCharacterOptimizationManager : SingletonBehaviour<SandboxCharacterOptimizationManager>, IDisposable
{

    public static int ExecutionPriority => CustomAnimatorUpdater.ExecutionPriority + 1;
    public override int Priority => ExecutionPriority;

    public class IndexReference : IDisposable
    {
        private Transform transform;
        public Transform Transform => transform;
        private SandboxCharacterOptimizationManager manager;
        public IndexReference(Transform transform, SandboxCharacterOptimizationManager manager)
        {
            this.transform = transform;
            this.manager = manager;
        }

        public int index = -1;
        public bool IsValid => index >= 0 && manager != null && transform != null;

        public UnityEvent OnDispose = new UnityEvent();

        public void Dispose()
        {
            if (manager == null) return;

            if (index >= 0 && !manager.IsDisposed && manager.indexReferences != null && index < manager.indexReferences.Count)
            {
                int indexToRemove = manager.indexReferences.Count - 1;
                var toSwap = manager.indexReferences[indexToRemove];
                if (manager.cachedTransforms.isCreated)
                {
                    manager.cachedTransforms.RemoveAtSwapBack(index);
                }
                if (manager.cachedTransformChanges.IsCreated)
                {
                    manager.cachedTransformChanges.RemoveAtSwapBack(index);
                }

                toSwap.index = index;
                manager.indexReferences[index] = toSwap;
                manager.indexReferences.RemoveAt(indexToRemove);

                //manager.sortTransforms = true;
            }

            index = -1;
            manager = null;
            transform = null;

            OnDispose?.Invoke();
            OnDispose = null;
        }

        public TransformLocalDataState CachedDataChanges
        {
            get
            {
                if (manager == null || manager.IsDisposed || !manager.cachedTransformChanges.IsCreated || index < 0) return default;
                return manager.cachedTransformChanges[index];
            }
            set
            {
                if (manager == null || manager.IsDisposed || !manager.cachedTransformChanges.IsCreated || index < 0) return;
                manager.cachedTransformChanges[index] = value;
            }
        }
    }

    private TransformAccessArray cachedTransforms;
    private NativeList<TransformLocalDataState> cachedTransformChanges;
    private List<IndexReference> indexReferences;

    //private static readonly List<IndexReference> _tempIndexReferences = new List<IndexReference>();
    private static readonly List<Transform> _tempCachedTransforms = new List<Transform>();
    private static readonly List<TransformLocalDataState> _tempCachedTransformChanges = new List<TransformLocalDataState>();
    private static int CompareTransformDepth(Transform a, Transform b)
    {
        return Math.Sign(a.GetChildDepth() - b.GetChildDepth());
    }
    private static int CompareTransformDepthOfReference(IndexReference a, IndexReference b)
    {
        return a.Transform == null ? 1 : (b.Transform == null ? -1 : CompareTransformDepth(a.Transform, b.Transform));
    }
    private static readonly Comparison<IndexReference> _refTransformDepthComparison = CompareTransformDepthOfReference;
    private static readonly Transform[] _emptyTransformArray = new Transform[0];
    public void SortTransformsByHierarchyDepth()
    {
        if (IsDisposed) return;

        //_tempIndexReferences.Clear();
        _tempCachedTransforms.Clear();
        _tempCachedTransformChanges.Clear();

        for (int a = 0; a < cachedTransforms.length; a++) _tempCachedTransforms.Add(cachedTransforms[a]);
        for (int a = 0; a < cachedTransformChanges.Length; a++) _tempCachedTransformChanges.Add(cachedTransformChanges[a]);
        //_tempIndexReferences.AddRange(indexReferences);

        cachedTransforms.SetTransforms(_emptyTransformArray);
        cachedTransformChanges.Clear();

        indexReferences.Sort(_refTransformDepthComparison);
        for(int a = 0; a < indexReferences.Count; a++)
        {
            var indexRef = indexReferences[a];
            //int oldIndex = _tempIndexReferences.IndexOf(indexRef);
            cachedTransforms.Add(_tempCachedTransforms[indexRef.index]);
            cachedTransformChanges.Add(_tempCachedTransformChanges[indexRef.index]);
            indexRef.index = a;
        }

        //_tempIndexReferences.Clear();
        _tempCachedTransforms.Clear();
        _tempCachedTransformChanges.Clear();
    }
    //private bool sortTransforms;
    public IndexReference RegisterCachedChangesLocal(Transform transform, TransformLocalDataState changes)
    {
        if (IsDisposed) return null;

        var indexRef = new IndexReference(transform, this);
        indexRef.index = cachedTransforms.length;

        cachedTransforms.Add(transform);
        cachedTransformChanges.Add(changes);
#if UNITY_EDITOR
        foreach(var test in indexReferences)
        {
            if (test.Transform == transform) Debug.LogError($"Registered duplicate transform '{transform.name}' for cached changes");
        }
#endif
        indexReferences.Add(indexRef);

        //sortTransforms = true;

        return indexRef;
    }
    public static IndexReference RegisterCachedChanges(Transform transform, TransformLocalDataState changes)
    {
        var instance = Instance;
        if (instance == null) return null;

        return instance.RegisterCachedChangesLocal(transform, changes);
    }

    private JobHandle jobHandle;
    public JobHandle OutputDependencyLocal => jobHandle;
    public static JobHandle OutputDependency
    {
        get
        {
            var inst = InstanceOrNull;
            if (inst == null) return default;

            return inst.OutputDependencyLocal;
        }
    }

    protected override void OnAwake()
    {
        cachedTransforms = new TransformAccessArray(512, -1);
        cachedTransformChanges = new NativeList<TransformLocalDataState>(512, Allocator.Persistent);
        indexReferences = new List<IndexReference>(512);
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnUpdate()
    {
    }

    public override void OnLateUpdate()
    {
        /*if (sortTransforms)
        {
            sortTransforms = false;
            SortTransformsByHierarchyDepth();
        }*/

        jobHandle = new ApplyTransformChanges()
        {
            cachedTransformChanges = cachedTransformChanges
        }.Schedule(cachedTransforms, default);

        jobHandle.Complete();
    }

    private bool disposed;
    public bool IsDisposed => disposed;
    public void Dispose()
    {
        disposed = true;

        if (cachedTransforms.isCreated)
        {
            cachedTransforms.Dispose();
            cachedTransforms = default;
        }
        if (cachedTransformChanges.IsCreated)
        {
            cachedTransformChanges.Dispose();
            cachedTransformChanges = default;
        }

        if (indexReferences != null)
        {
            foreach(var indexRef in indexReferences)
            {
                if (indexRef != null) indexRef.index = -1;
            }
            indexReferences.Clear();
            indexReferences = null;
        }
    }
    protected void OnDestroy()
    {
        Dispose();
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct TransformLocalDataState
    {
        public bool resetTransform;

        public float3 localPosition;
        public float3 localScale;
        public quaternion localRotation;

        public float3 positionOffset;
        public float3 scaleOffset;
        public quaternion rotationOffset;

        public static TransformLocalDataState operator +(TransformLocalDataState a, TransformLocalDataState b)
        {
            var val = a;

            val.positionOffset = a.positionOffset + b.positionOffset;
            val.rotationOffset = math.mul(b.rotationOffset, a.rotationOffset);
            val.scaleOffset = a.scaleOffset + b.scaleOffset;

            return val;
        }
    }

    [BurstCompile]
    public struct ApplyTransformChanges : IJobParallelForTransform
    {
        [ReadOnly]
        public NativeList<TransformLocalDataState> cachedTransformChanges; 

        public void Execute(int index, TransformAccess transform)
        {
            var changes = cachedTransformChanges[index];
            if (changes.resetTransform)
            {
                transform.SetLocalPositionAndRotation(changes.positionOffset, changes.rotationOffset);
                transform.localScale = changes.scaleOffset;
            } 
            else
            {
                transform.GetLocalPositionAndRotation(out var localPos, out var localRot);
                var localScale = transform.localScale;

                localPos = (float3)localPos + changes.positionOffset;
                localRot = math.mul(changes.rotationOffset, (quaternion)localRot);
                localScale = (float3)localScale + changes.scaleOffset;

                transform.SetLocalPositionAndRotation(localPos, localRot); 
                transform.localScale = localScale;
            }
        }
    }

}