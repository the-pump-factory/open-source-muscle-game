using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using Swole;
using Swole.Animation;
using Swole.API.Unity.Animation;

public abstract class WorkoutBase : ScriptableObject
{

    #region Constants

    public const string statePrefix_enterWorkout = "enter_workout_";
    public const string statePrefix_exitWorkout = "exit_workout_";

    public static string GetEnterWorkoutStateName(string id) => $"{statePrefix_enterWorkout}{id}";
    public static string GetExitWorkoutStateName(string id) => $"{statePrefix_exitWorkout}{id}";

    public const string state_idleWorkout = "idle_workout";


    public const string directory_workoutViews = "views/workouts";
    public static string WorkoutViewsDirectory => Path.Combine(swole.AppDataDirectory.FullName, directory_workoutViews); 

    public const string viewGroup_enterWorkout = "enter";
    public const string viewGroup_exitWorkout = "exit";
    public const string viewGroup_rep = "rep";
    public const string viewGroup_rest = "rest";

    public const string viewGroup_enterWorkoutSpotter = "enter_spotter";
    public const string viewGroup_exitWorkoutSpotter = "exit_spotter";
    public const string viewGroup_repSpotter = "rep_spotter";
    public const string viewGroup_restSpotter = "rest_spotter"; 

    public static string GetViewGroupId(string workoutName, string viewGroupName) => $"{workoutName}_{viewGroupName}";
    public static string GetEnterWorkoutViewGroupId(string workoutName) => GetViewGroupId(workoutName, viewGroup_enterWorkout);
    public static string GetExitWorkoutViewGroupId(string workoutName) => GetViewGroupId(workoutName, viewGroup_exitWorkout);
    public static string GetRepViewGroupId(string workoutName) => GetViewGroupId(workoutName, viewGroup_rep);
    public static string GetRestViewGroupId(string workoutName) => GetViewGroupId(workoutName, viewGroup_rest);
    public static string GetSpotterEnterWorkoutViewGroupId(string workoutName) => GetViewGroupId(workoutName, viewGroup_enterWorkoutSpotter);
    public static string GetSpotterExitWorkoutViewGroupId(string workoutName) => GetViewGroupId(workoutName, viewGroup_exitWorkoutSpotter);
    public static string GetSpotterRepViewGroupId(string workoutName) => GetViewGroupId(workoutName, viewGroup_repSpotter);
    public static string GetSpotterRestViewGroupId(string workoutName) => GetViewGroupId(workoutName, viewGroup_restSpotter);


    public const string viewId_A = "view_1";
    public const string viewId_B = "view_2";
    public const string viewId_C = "view_3";
    public const string viewId_D = "view_4";

    #region Workout Parameters

    public const string workoutParam_enterWorkout = "enterWorkout"; 
    public const string workoutParam_exitWorkout = "exitWorkout"; 
    public const string workoutParam_hasSpotter = "hasSpotter"; 
    public const string workoutParam_isSpotter = "isSpotter";
    public const string workoutParam_repProgress = "repProgress"; 
    public const string workoutParam_normalizedLoad = "load"; 
    public const string workoutParam_repStruggle = "repStruggle"; 
    public const string workoutParam_tired = "tiredWorkout"; 
    public const string workoutParam_assist = "spotterAssist";
    public const string workoutParam_enterRest = "enterWorkoutRest"; 
    public const string workoutParam_exitRest = "exitWorkoutRest";
    public const string workoutParam_enterFlex = "enterWorkoutFlex"; 
    public const string workoutParam_exitFlex = "exitWorkoutFlex"; 
    public const string workoutParam_flexProgress = "flexProgress";

    #endregion

    #endregion

    public Sprite displayIcon;
    public Sprite DisplayIcon => displayIcon;

    public string displayName;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

    public float enterWorkoutTime = 0f;
    public float exitWorkoutTime = 0f;

    public float enterRestTime = 0f;
    public float exitRestTime = 0f;

    public float enterRepTime = 0f;
    public float exitRepTime = 0f;

    public bool canExitWorkoutFromRest;

    public PointCurve repCurve;

    public AnimationCurve speedByAlignmentCurve;
    [NonSerialized]
    private ValueCurve speedByAlignmentValueCurve;
    public ValueCurve SpeedByAlignmentCurve
    {
        get
        {
            if (speedByAlignmentValueCurve == null)
            {
                if (speedByAlignmentCurve != null && speedByAlignmentCurve.length > 0)
                {
                    speedByAlignmentValueCurve = ValueCurve.FromAnimationCurve(speedByAlignmentCurve); 
                }
                else
                {
                    speedByAlignmentValueCurve = new ValueCurve(new float[] { 0f, 1f }); 
                }
            }

            return speedByAlignmentValueCurve;
        }
    }

    public AnimationCurve speedNerfMaxByLoadCurve;
    [NonSerialized]
    private ValueCurve speedNerfMaxByLoadValueCurve;
    public ValueCurve SpeedNerfMaxByLoadCurve
    {
        get
        {
            if (speedNerfMaxByLoadValueCurve == null)
            {
                speedNerfMaxByLoadValueCurve = ValueCurve.FromAnimationCurve(speedNerfMaxByLoadCurve);
            }

            return speedNerfMaxByLoadValueCurve;
        }
    }

    public AnimationCurve speedNerfMaxByBurnCurve;
    [NonSerialized]
    private ValueCurve speedNerfMaxByBurnValueCurve;
    public ValueCurve SpeedNerfMaxByBurnCurve
    {
        get
        {
            if (speedNerfMaxByBurnValueCurve == null)
            {
                speedNerfMaxByBurnValueCurve = ValueCurve.FromAnimationCurve(speedNerfMaxByBurnCurve);
            }

            return speedNerfMaxByBurnValueCurve;
        }
    }

    public AnimationCurve loadRatioByRepPositionCurve;
    [NonSerialized]
    private ValueCurve loadRatioByRepPositionValueCurve;
    public ValueCurve LoadRatioByRepPositionCurve
    {
        get
        {
            if (loadRatioByRepPositionValueCurve == null)
            {
                loadRatioByRepPositionValueCurve = ValueCurve.FromAnimationCurve(loadRatioByRepPositionCurve);
            }

            return loadRatioByRepPositionValueCurve;
        }
    }

    public AnimationCurve strengthCurve;
    [NonSerialized]
    private ValueCurve strengthValueCurve;
    public ValueCurve StrengthCurve
    {
        get
        {
            if (strengthValueCurve == null)
            {
                strengthValueCurve = ValueCurve.FromAnimationCurve(strengthCurve);
            }

            return strengthValueCurve;
        }
    }

    public AnimationCurve staminaDrainByLoadCurve;
    [NonSerialized]
    private ValueCurve staminaDrainByLoadValueCurve;
    public ValueCurve StaminaDrainByLoadCurve
    {
        get
        {
            if (staminaDrainByLoadValueCurve == null)
            {
                staminaDrainByLoadValueCurve = ValueCurve.FromAnimationCurve(staminaDrainByLoadCurve);
            }

            return staminaDrainByLoadValueCurve;
        }
    }

    public AnimationCurve staminaDrainByRepPositionCurve;
    [NonSerialized]
    private ValueCurve staminaDrainByRepPositionValueCurve;
    public ValueCurve StaminaDrainByRepPositionCurve
    {
        get
        {
            if (staminaDrainByRepPositionValueCurve == null)
            {
                staminaDrainByRepPositionValueCurve = ValueCurve.FromAnimationCurve(staminaDrainByRepPositionCurve);
            }

            return staminaDrainByRepPositionValueCurve; 
        }
    }

    public float loadRangeMin;
    public float loadRangeMax;
    public float loadLimit;

    public float maxGravity;

    public float minRepSpeed;

    public float defaultRepSpeed;

    public float repAcceleration;
    public float repDeceleration;

    public float baseStaminaDrain;

    public bool allowSpotter = false;

    public WorkoutBaseAnimations animations;

    public virtual HashSet<CustomAnimation> GetAnimationAssets(HashSet<CustomAnimation> assets = null)
    {
        if (assets == null) assets = new HashSet<CustomAnimation>();

        var fields = GetType().GetFields(); 
        foreach (var field in fields)
        {
            var value = field.GetValue(this);
            if (value is LinearAnimationSequence seq)
            {
                seq.GetAssets(assets);
            }
            else if (value is LinearAnimationSequence[] seqs)
            {
                foreach (var seq2 in seqs)
                {
                    seq2.GetAssets(assets);
                }
            }
            else if (value is WorkoutBaseAnimations anims) 
            {
                anims.GetAssets(assets);
            }
        }

        return assets;
    }

    protected static readonly HashSet<CustomAnimation> temp_animationCache = new HashSet<CustomAnimation>();
    protected static CustomAnimation FindAnimationAsset(string name)
    {
        foreach (var anim in temp_animationCache)
        {
            if (anim == null) continue;

            if (anim.Name == name) return anim;
        }

        return null;
    }

    public OrbitCameraViewGroup enterWorkoutView;
    public OrbitCameraViewGroup exitWorkoutView;
    public OrbitCameraViewGroup repView;
    public OrbitCameraViewGroup restView;

    public OrbitCameraViewGroup spotterEnterWorkoutView;
    public OrbitCameraViewGroup spotterExitWorkoutView;
    public OrbitCameraViewGroup spotterRepView;
    public OrbitCameraViewGroup spotterRestView;

    protected static readonly List<AnimationParameterIdentifier> temp_parameters = new List<AnimationParameterIdentifier>();

    protected static readonly List<CustomAnimationParameter.Float> temp_floatParameters = new List<CustomAnimationParameter.Float>();
    protected static readonly List<CustomAnimationParameter.Boolean> temp_boolParameters = new List<CustomAnimationParameter.Boolean>();
    protected static readonly List<CustomAnimationParameter.Trigger> temp_triggerParameters = new List<CustomAnimationParameter.Trigger>();

    protected static readonly List<CustomAnimationLayer> temp_animationLayers = new List<CustomAnimationLayer>();
    protected static readonly List<CustomAnimationLayerState> temp_layerStates = new List<CustomAnimationLayerState>();
    protected static readonly List<MotionControllerIdentifier> temp_motionControllerIdentifiers = new List<MotionControllerIdentifier>();
    protected static readonly List<AnimationReference> temp_animationReferences = new List<AnimationReference>();
    protected static readonly List<BlendTree1D> temp_blendTrees1D = new List<BlendTree1D>();
    protected static readonly List<BlendTree2D> temp_blendTrees2D = new List<BlendTree2D>();
    protected static readonly List<MotionComposite> temp_motionComposites = new List<MotionComposite>();

    protected static void ClearTempData()
    {
        temp_animationCache.Clear();

        temp_parameters.Clear();

        temp_floatParameters.Clear();
        temp_boolParameters.Clear();
        temp_triggerParameters.Clear();

        temp_animationLayers.Clear();
        temp_layerStates.Clear();
        temp_motionControllerIdentifiers.Clear();
        temp_animationReferences.Clear();
        temp_blendTrees1D.Clear();
        temp_blendTrees2D.Clear();
        temp_motionComposites.Clear();
    }

    protected static void AddFloatParameter(CustomAnimationParameter.Float p)
    {
        temp_parameters.Add(new AnimationParameterIdentifier(AnimationParameterValueType.Float, temp_floatParameters.Count));
        temp_floatParameters.Add(p);
    }
    protected static void AddBoolParameter(CustomAnimationParameter.Boolean p)
    {
        temp_parameters.Add(new AnimationParameterIdentifier(AnimationParameterValueType.Boolean, temp_boolParameters.Count));
        temp_boolParameters.Add(p);
    }
    protected static void AddTriggerParameter(CustomAnimationParameter.Trigger p)
    {
        temp_parameters.Add(new AnimationParameterIdentifier(AnimationParameterValueType.Trigger, temp_triggerParameters.Count));
        temp_triggerParameters.Add(p);
    }

    protected static int FindParamterIndex(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName)) return -1;

        for (int a = 0; a < temp_parameters.Count; a++)
        {
            var id = temp_parameters[a];

            CustomAnimationParameter parameter = null;
            switch (id.type)
            {
                case AnimationParameterValueType.Float:
                    parameter = temp_floatParameters[id.index];
                    if (parameter != null && parameter.name == parameterName) return a;
                    break;
                case AnimationParameterValueType.Boolean:
                    parameter = temp_boolParameters[id.index];
                    if (parameter != null && parameter.name == parameterName) return a;
                    break;
                case AnimationParameterValueType.Trigger:
                    parameter = temp_triggerParameters[id.index];
                    if (parameter != null && parameter.name == parameterName) return a;
                    break;
            }
        }

        return -1;
    }

    [NonSerialized]
    protected CustomAnimationController animationController;
    public CustomAnimationController AnimationController => animationController;

    protected virtual void EvaluateAnimationSequence(string debugName, string baseStateName, LinearAnimationSequence seq, int sequenceIndex, string defaultBaseTimeOverrideParameter = null, bool useNormalizedTimeForBaseOverride = false)
    {
        if (string.IsNullOrWhiteSpace(seq.id))
        {
            Debug.LogWarning($"Workout '{name}' has an {debugName} animation sequence [{sequenceIndex}] with no ID!");
            return;
        }
        if (seq.sequence.Length == 0)
        {
            Debug.LogWarning($"Workout '{name}' has an {debugName} animation sequence [{sequenceIndex}] with no animations!");
            return;
        }

        var lastStateIndex = -1;
        var lastLengthInSeconds = 0f;
        for (int b = 0; b < seq.sequence.Length; b++)
        {
            var anim = seq.sequence[b];
            if (anim == null || !anim.IsValid)
            {
                Debug.LogWarning($"Workout '{name}' has an invalid {debugName} animation [{b}] in sequence [{sequenceIndex}]!");
                continue;
            }

            bool isFirst = b == 0;
            bool isLast = b >= seq.sequence.Length - 1;

            var layerState = new CustomAnimationLayerState();
            layerState.name = isFirst ? baseStateName : $"{baseStateName}:{b}"; 
            layerState.index = temp_layerStates.Count;
            layerState.motionControllerIndex = temp_motionControllerIdentifiers.Count;

            temp_layerStates.Add(layerState);
            if (b == 0) 
            { 
                seq.startLayerState = layerState;
            }
            if (b == seq.sequence.Length - 1) 
            { 
                seq.endLayerState = layerState; 
            }

            var baseAsset = anim.BaseAsset;
            if (anim.NeedsBlending)
            {
                temp_motionControllerIdentifiers.Add(new MotionControllerIdentifier() { index = temp_motionComposites.Count, type = MotionControllerType.MotionComposite }); // the root motion controller for the layer state

                int partOffset = (baseAsset == null ? 0 : 1);
                var motionParts = new MotionPart[anim.blendedAnimations.Length + partOffset];
                if (baseAsset != null)
                {
                    temp_motionControllerIdentifiers.Add(new MotionControllerIdentifier() { index = temp_animationReferences.Count, type = MotionControllerType.AnimationReference });

                    var animRef = new AnimationReference(anim.name, baseAsset, isLast ? anim.loopMode : AnimationLoopMode.PlayOnce);
                    animRef.baseSpeed = 1f;
                    animRef.speedMultiplierParameter = FindParamterIndex(anim.speedMultiplierParameter);
                    animRef.timeOverrideParameter = FindParamterIndex(anim.timeOverrideParameter);
                    animRef.useNormalizedTimeForOverriding = anim.useNormalizedTimeForOverriding;
                    if (animRef.timeOverrideParameter < 0) 
                    { 
                        animRef.timeOverrideParameter = FindParamterIndex(defaultBaseTimeOverrideParameter);
                        animRef.useNormalizedTimeForOverriding = useNormalizedTimeForBaseOverride;
                    }

                    temp_animationReferences.Add(animRef);

                    var part = new MotionPart();
                    part.controllerIndex = temp_motionControllerIdentifiers.Count - 1;
                    part.speed = anim.speed;
                    part.mix = 1f;
                    part.mixParameterIndex = -1;
                    part.normalizedStartTime = 0f;
                    part.syncMode = TimeSyncMode.None;
                    part.localSyncReferenceIndex = -1;

                    motionParts[0] = part;
                }
                for (int c = 0; c < anim.blendedAnimations.Length; c++)
                {
                    var blendAnim = anim.blendedAnimations[c];

                    temp_motionControllerIdentifiers.Add(new MotionControllerIdentifier() { index = temp_animationReferences.Count, type = MotionControllerType.AnimationReference });

                    var animRef = new AnimationReference(blendAnim.name, baseAsset, blendAnim.loopMode, blendAnim.startNormalizedTime);
                    animRef.baseSpeed = 1f;
                    animRef.speedMultiplierParameter = FindParamterIndex(blendAnim.speedMultiplierParameter);
                    animRef.timeOverrideParameter = FindParamterIndex(blendAnim.timeOverrideParameter);
                    animRef.useNormalizedTimeForOverriding = blendAnim.useNormalizedTimeForOverriding;

                    temp_animationReferences.Add(animRef);

                    var part = new MotionPart();
                    part.controllerIndex = temp_motionControllerIdentifiers.Count - 1;
                    part.speed = anim.speed;
                    part.mix = blendAnim.baseWeight;
                    part.mixParameterIndex = FindParamterIndex(blendAnim.blendParameter);
                    part.clampMixParameter = blendAnim.clampWeight;
                    part.mixParameterMappingMin = blendAnim.parameterMapping.x;
                    part.mixParameterMappingMax = blendAnim.parameterMapping.y; 
                    part.normalizedStartTime = blendAnim.startNormalizedTime;
                    part.syncMode = blendAnim.syncMode;
                    part.localSyncReferenceIndex = blendAnim.localSyncReferenceIndex;

                    motionParts[c + partOffset] = part; 
                }

                var controller = new MotionComposite(anim.name, motionParts);
                controller.baseSpeed = anim.speed;
                controller.speedMultiplierParameter = -1;
                controller.normalizeDurations = anim.blendNormalizeDurations;
                controller.normalizedDuration = anim.blendNormalizedDuration;
                controller.waitForLoops = anim.blendWaitForLoops;
                controller.forceSyncOnProgress = anim.blendForceSyncOnProgress;
                controller.useDedicatedChildForTime = anim.blendUseDedicatedChildForTime;
                controller.timeDedicatedChildIndex = anim.blendTimeDedicatedChildIndex;

                temp_motionComposites.Add(controller);
            }
            else
            {
                temp_motionControllerIdentifiers.Add(new MotionControllerIdentifier() { index = temp_animationReferences.Count, type = MotionControllerType.AnimationReference }); // the root motion controller for the layer state

                var animRef = new AnimationReference(anim.name, baseAsset, isLast ? anim.loopMode : AnimationLoopMode.PlayOnce, anim.startNormalizedTime);
                animRef.baseSpeed = anim.speed;
                animRef.speedMultiplierParameter = FindParamterIndex(anim.speedMultiplierParameter);
                animRef.timeOverrideParameter = FindParamterIndex(anim.timeOverrideParameter);
                animRef.useNormalizedTimeForOverriding = anim.useNormalizedTimeForOverriding;
                if (animRef.timeOverrideParameter < 0) 
                { 
                    animRef.timeOverrideParameter = FindParamterIndex(defaultBaseTimeOverrideParameter);
                    animRef.useNormalizedTimeForOverriding = useNormalizedTimeForBaseOverride; 
                }

                temp_animationReferences.Add(animRef);
            }

            if (lastStateIndex >= 0)
            {
                var prevState = temp_layerStates[lastStateIndex];
                var transition = anim.GetTransition(lastLengthInSeconds);
                if (transition == null)
                {
                    Debug.LogWarning($"Workout '{name}' has an invalid {debugName} transition [{b}] in sequence [{sequenceIndex}]!");
                    continue;
                }

                transition = anim.useDefaultTransition ? transition : transition.Duplicate();
                transition.targetStateIndex = layerState.index;
                prevState.transitions[prevState.transitions.Length - 1] = transition; 
                prevState.AddTransition(transition);
            }

            lastStateIndex = temp_layerStates.Count - 1;
            if (baseAsset != null)
            {
                lastLengthInSeconds = baseAsset.LengthInSeconds;
            }
            else if (anim.blendedAnimations != null)
            {
                lastLengthInSeconds = 0f;
                foreach (var blendAnim in anim.blendedAnimations)
                {
                    if (blendAnim == null || blendAnim.Asset == null) continue;
                    lastLengthInSeconds = Mathf.Max(lastLengthInSeconds, blendAnim.Asset.LengthInSeconds);
                }
            }
            else
            {
                lastLengthInSeconds = 0f;
            }

            if (b == 0)
            {
                seq.cachedStartStateLengthInSeconds = lastLengthInSeconds;
            }
            if (b == seq.sequence.Length - 1)
            {
                seq.cachedEndStateLengthInSeconds = lastLengthInSeconds;
            }
        }
    }

    public delegate bool ParameterCheckDelegate();
    public virtual string SetupAnimations(CustomAnimator animator, ParameterCheckDelegate checkAdditionalParameters = null, Action<CustomAnimationLayer> setupAux = null,
        string enterWorkoutParameter = workoutParam_enterWorkout, string exitWorkoutParameter = workoutParam_exitWorkout, string spotterParameter = workoutParam_hasSpotter, string isSpotterParameter = workoutParam_isSpotter,
        string repProgressParameter = workoutParam_repProgress, string normalizedLoadParameter = workoutParam_normalizedLoad, string repStruggleParameter = workoutParam_repStruggle, string tiredParameter = workoutParam_tired, string assistParameter = workoutParam_assist,
        string enterRestParemeter = workoutParam_enterRest, string exitRestParameter = workoutParam_exitRest, 
        string enterFlexParameter = workoutParam_enterFlex, string exitFlexParameter = workoutParam_exitFlex, string flexProgressParemeter = workoutParam_flexProgress)
    {

        #region Check Existing Controller

        if (animationController != null) 
        {
            bool flag = false;

            if (!animationController.HasTriggerParameter(enterWorkoutParameter))
            {
                flag = true;
            } 
            else if (!animationController.HasTriggerParameter(exitWorkoutParameter))
            {
                flag = true;
            }
            else if (!animationController.HasTriggerParameter(spotterParameter))
            {
                flag = true;
            }
            else if (!animationController.HasFloatParameter(isSpotterParameter))
            {
                flag = true;
            }
            else if (!animationController.HasFloatParameter(repProgressParameter))
            {
                flag = true;
            }
            else if (!animationController.HasFloatParameter(normalizedLoadParameter))
            {
                flag = true;
            }
            else if (!animationController.HasFloatParameter(repStruggleParameter))
            {
                flag = true;
            }
            else if (!animationController.HasFloatParameter(tiredParameter))
            {
                flag = true;
            }
            else if (!animationController.HasFloatParameter(assistParameter))
            {
                flag = true;
            }
            else if (!animationController.HasFloatParameter(enterRestParemeter))
            {
                flag = true;
            }
            else if (!animationController.HasFloatParameter(exitRestParameter))
            {
                flag = true;
            }
            else if (!animationController.HasFloatParameter(enterFlexParameter))
            {
                flag = true;
            }
            else if (!animationController.HasFloatParameter(exitFlexParameter))
            {
                flag = true;
            }
            else if (!animationController.HasFloatParameter(flexProgressParemeter))
            {
                flag = true;
            } 
            else if (checkAdditionalParameters != null && !checkAdditionalParameters())
            {
                flag = true;
            }

            if (flag)
            {
                DestroyImmediate(animationController);
                animationController = null;
            }
        }

        #endregion

        if (animationController == null)
        {

            ClearTempData();

            GetAnimationAssets(temp_animationCache);

            try
            {

                #region Create Parameters

                var enterWorkoutParam = new CustomAnimationParameter.Trigger(enterWorkoutParameter);
                var exitWorkoutParam = new CustomAnimationParameter.Trigger(exitWorkoutParameter);
                var spotterParam = new CustomAnimationParameter.Boolean(spotterParameter, false);
                var isSpotterParam = new CustomAnimationParameter.Boolean(isSpotterParameter, false);
                var repProgressParam = new CustomAnimationParameter.Float(repProgressParameter, 0f);
                var normalizedLoadParam = new CustomAnimationParameter.Float(normalizedLoadParameter, 0f);
                var repStruggleParam = new CustomAnimationParameter.Float(repStruggleParameter, 0f);
                var tiredParam = new CustomAnimationParameter.Float(tiredParameter, 0f);
                var assistParam = new CustomAnimationParameter.Float(assistParameter, 0f);
                var enterRestParam = new CustomAnimationParameter.Trigger(enterRestParemeter);
                var exitRestParam = new CustomAnimationParameter.Trigger(exitRestParameter);
                var enterFlexParam = new CustomAnimationParameter.Trigger(enterFlexParameter);
                var exitFlexParam = new CustomAnimationParameter.Trigger(exitFlexParameter);
                var flexProgressParam = new CustomAnimationParameter.Float(flexProgressParemeter, 0f);

                AddTriggerParameter(enterWorkoutParam);
                AddTriggerParameter(exitWorkoutParam);
                AddBoolParameter(spotterParam);
                AddBoolParameter(isSpotterParam);
                AddFloatParameter(repProgressParam);
                AddFloatParameter(normalizedLoadParam);
                AddFloatParameter(repStruggleParam);
                AddFloatParameter(tiredParam);
                AddFloatParameter(assistParam);
                AddTriggerParameter(enterRestParam);
                AddTriggerParameter(exitRestParam);
                AddTriggerParameter(enterFlexParam);
                AddTriggerParameter(exitFlexParam);
                AddFloatParameter(flexProgressParam);

                #endregion

                animationController = ScriptableObject.CreateInstance<CustomAnimationController>();
                animationController.name = "workout";

                CustomAnimationLayer workoutLayer = new CustomAnimationLayer();
                temp_animationLayers.Add(workoutLayer); 

                //layerInst.SetActive(false);
                workoutLayer.SetActive(true);
                workoutLayer.name = name;
                workoutLayer.blendParameterIndex = -1;
                workoutLayer.entryStateIndex = 0;
                workoutLayer.mix = 0f;
                workoutLayer.IsAdditive = false;

                if (animations.enterWorkoutAnimations != null && animations.enterWorkoutAnimations.Length > 0)
                {
                    string debugName = "enter workout";
                    for (int a = 0; a < animations.enterWorkoutAnimations.Length; a++)
                    {
                        var seq = animations.enterWorkoutAnimations[a];
                        if (seq == null || !seq.IsValid)
                        {
                            Debug.LogError($"Enter workout sequence {a} for workout {name} is null or invalid");
                            continue;
                        }

                        var baseStateName = GetEnterWorkoutStateName(seq.id);
                        EvaluateAnimationSequence(debugName, baseStateName, seq, a);
                    }
                } 
                else
                {
                    throw new Exception($"Workout {name} enter animations is null or invalid");
                }

                if (animations.exitWorkoutAnimations != null && animations.exitWorkoutAnimations.Length > 0)
                {
                    string debugName = "exit workout";
                    for (int a = 0; a < animations.exitWorkoutAnimations.Length; a++)
                    {
                        var seq = animations.exitWorkoutAnimations[a];
                        if (seq == null || !seq.IsValid)
                        {
                            Debug.LogError($"Exit workout sequence {a} for workout {name} is null or invalid");
                            continue;
                        }

                        var baseStateName = GetExitWorkoutStateName(seq.id);
                        EvaluateAnimationSequence(debugName, baseStateName, seq, a);
                    }
                }
                else
                {
                    throw new Exception($"Workout {name} exit animations is null or invalid");
                }

                if (animations.idleAnimation != null && animations.idleAnimation.IsValid)
                {
                    EvaluateAnimationSequence("idle workout", state_idleWorkout, animations.idleAnimation, 0, workoutParam_repProgress, true);  
                } 
                else
                {
                    throw new Exception($"Workout {name} idle animation is null or invalid");
                }
                
                setupAux?.Invoke(workoutLayer);

                foreach(var enterWorkout in animations.enterWorkoutAnimations) 
                {
                    bool isNewInstance = false;
                    var transitionToIdle = animations.idleAnimation.GetEnterTransition(enterWorkout.id);
                    if (transitionToIdle == null)
                    {
                        transitionToIdle = new Transition();
                        isNewInstance = true;
                    }

                    if (!isNewInstance) transitionToIdle = transitionToIdle.Duplicate();

                    if (!transitionToIdle.HasRequirements)
                    {
                        transitionToIdle.validRequirementPaths = new TransitionRequirement[]
                        {
                            new TransitionRequirement()
                            {
                                 validNormalizedTimeRanges = new TransitionTimeRange[]
                                 {
                                     new TransitionTimeRange(enterWorkout.cachedEndStateLengthInSeconds <= 0 ? 0f : Mathf.Max(0f, 1f - (0.1f / enterWorkout.cachedEndStateLengthInSeconds)), 1f)
                                 }
                            }
                        };
                    }

                    transitionToIdle.targetStateIndex = animations.idleAnimation.startLayerState.index;
                    enterWorkout.endLayerState.AddTransition(transitionToIdle);   
                }

                foreach (var exitWorkout in animations.exitWorkoutAnimations)
                {
                    bool isNewInstance = false;
                    var transitionFromIdle = animations.idleAnimation.GetExitTransition(exitWorkout.id);
                    if (transitionFromIdle == null)
                    {
                        transitionFromIdle = new Transition();
                        isNewInstance = true; 
                    }

                    if (!isNewInstance) transitionFromIdle = transitionFromIdle.Duplicate();

                    if (transitionFromIdle.HasRequirements)
                    {
                        for (int i = 0; i < transitionFromIdle.validRequirementPaths.Length; i++)
                        {
                            var requirement = transitionFromIdle.validRequirementPaths[i];

                            bool flag = true;
                            if (requirement.parameters != null)
                            {
                                for (int j = 0; j < requirement.parameters.Length; j++)
                                {
                                    var parameter = requirement.parameters[j];
                                    if (parameter.parameterName == workoutParam_exitWorkout)
                                    {
                                        flag = false;
                                        break;
                                    }
                                }
                            }

                            if (flag)
                            {
                                if (requirement.parameters == null)
                                {
                                    requirement.parameters = new TransitionParameter[1];
                                }
                                else
                                {
                                    Array.Resize(ref requirement.parameters, requirement.parameters.Length + 1);
                                }

                                requirement.parameters[requirement.parameters.Length - 1] = new TransitionParameter()
                                {
                                    comparisonType = TransitionComparisonType.True,
                                    parameterName = workoutParam_exitWorkout
                                };
                            }

                            transitionFromIdle.validRequirementPaths[i] = requirement;
                        }
                    }
                    else
                    {
                        transitionFromIdle.validRequirementPaths = new TransitionRequirement[]
                        {
                            new TransitionRequirement()
                            {
                                 validNormalizedTimeRanges = new TransitionTimeRange[]
                                 {
                                     new TransitionTimeRange(0f, 1f)
                                 },

                                 parameters = new TransitionParameter[]
                                 {
                                     new TransitionParameter()
                                     {
                                          comparisonType = TransitionComparisonType.True,
                                         parameterName = workoutParam_exitWorkout
                                     }
                                 }
                            }
                        };
                    }

                    transitionFromIdle.targetStateIndex = exitWorkout.startLayerState.index;
                    animations.idleAnimation.endLayerState.AddTransition(transitionFromIdle); 
                }

                workoutLayer.motionControllerIdentifiers = temp_motionControllerIdentifiers.ToArray();
                workoutLayer.states = temp_layerStates.ToArray();

                animationController.animationReferences = temp_animationReferences.ToArray();
                animationController.blendTrees1D = temp_blendTrees1D.ToArray();
                animationController.blendTrees2D = temp_blendTrees2D.ToArray();
                animationController.motionComposites = temp_motionComposites.ToArray();
                animationController.layers = new CustomAnimationLayer[] { workoutLayer };

                animationController.parameters = temp_parameters.ToArray();
                animationController.floatParameters = temp_floatParameters.ToArray();
                animationController.boolParameters = temp_boolParameters.ToArray();
                animationController.triggerParameters = temp_triggerParameters.ToArray();

            } 
            catch(Exception e)
            {
                Debug.LogException(e);

                if (animationController != null) DestroyImmediate(animationController);
                animationController = null;
            }
            finally
            {
                ClearTempData();
            }

        }

        if (animationController != null)
        {
            animator.RemoveControllerData(animationController.Prefix, true);
            animator.ApplyController(animationController, true, false, false);
            return animationController.ConvertLayerName(name);
        }

        return string.Empty;
    }

    public void RemoveAnimationsFrom(CustomAnimator animator)
    {
        if (animationController == null || animator == null) return;
         
        animator.RemoveControllerData(animationController, true);
    }

}

[Serializable]
public class WorkoutBaseAnimations
{
    [Header("Performer")]
    public LinearAnimationSequence[] enterWorkoutAnimations;
    public LinearAnimationSequence[] exitWorkoutAnimations;

    public LinearAnimationSequence[] enterWorkoutTiredAnimations;
    public LinearAnimationSequence[] exitWorkoutTiredAnimations;

    public LinearAnimationSequence idleAnimation;
    public LinearAnimationSequence idleTiredAnimation;

    public LinearAnimationSequence idleToRepAnimation;
    public LinearAnimationSequence idleTiredToRepAnimation;

    public AnimationCurve performerRepTimeRemap;

    public LinearAnimationSequence repAnimation;
    public LinearAnimationSequence repEasyAnimation;
    public LinearAnimationSequence repStruggleAnimation;
    public LinearAnimationSequence repExtremeAnimation;

    public LinearAnimationSequence repFailAnimation;

    public LinearAnimationSequence repCompleteAnimation;
    public LinearAnimationSequence repEasyCompleteAnimation;
    public LinearAnimationSequence repStruggleCompleteAnimation;
    public LinearAnimationSequence repExtremeCompleteAnimation;

    public LinearAnimationSequence repResetAnimation;
    public LinearAnimationSequence repEasyResetAnimation;
    public LinearAnimationSequence repStruggleResetAnimation;
    public LinearAnimationSequence repExtremeResetAnimation;

    public LinearAnimationSequence startRestAnimation;
    public LinearAnimationSequence idleRestAnimation;
    public LinearAnimationSequence[] startFlexRestAnimations;
    public LinearAnimationSequence[] flexRestAnimations;
    public LinearAnimationSequence[] stopFlexRestAnimations;
    public LinearAnimationSequence stopRestAnimation;

    public LinearAnimationSequence startRestTiredAnimation;
    public LinearAnimationSequence idleRestTiredAnimation;
    public LinearAnimationSequence[] startFlexRestTiredAnimations;
    public LinearAnimationSequence[] flexRestTiredAnimations;
    public LinearAnimationSequence[] stopFlexRestTiredAnimations;
    public LinearAnimationSequence stopRestTiredAnimation;

    public LinearAnimationSequence idleRestToTiredAnimation;

    [Header("Spotter")]
    public LinearAnimationSequence[] enterIdleSpotterAnimations;
    public LinearAnimationSequence[] exitIdleSpotterAnimations;

    public LinearAnimationSequence[] enterRestSpotterAnimations;
    public LinearAnimationSequence[] exitRestSpotterAnimations;

    public LinearAnimationSequence idleSpotterAnimation;
    public LinearAnimationSequence idleTiredSpotterAnimation;
    public LinearAnimationSequence idleToTiredSpotterAnimation;

    public LinearAnimationSequence idleToRepSpotterAnimation;
    public LinearAnimationSequence idleTiredToRepSpotterAnimation;

    public AnimationCurve spotterRepTimeRemap;

    public LinearAnimationSequence repSpotterAnimation;
    public LinearAnimationSequence repEasySpotterAnimation;
    public LinearAnimationSequence repStruggleSpotterAnimation;
    public LinearAnimationSequence repExtremeSpotterAnimation;

    public LinearAnimationSequence repFailSpotterAnimation;

    public LinearAnimationSequence repCompleteSpotterAnimation;
    public LinearAnimationSequence repEasyCompleteSpotterAnimation;
    public LinearAnimationSequence repStruggleCompleteSpotterAnimation;
    public LinearAnimationSequence repExtremeCompleteSpotterAnimation;

    public LinearAnimationSequence repResetSpotterAnimation;
    public LinearAnimationSequence repEasyResetSpotterAnimation;
    public LinearAnimationSequence repStruggleResetSpotterAnimation;
    public LinearAnimationSequence repExtremeResetSpotterAnimation;

    public LinearAnimationSequence repAssistSpotterAnimation;
    public LinearAnimationSequence repAssistEasySpotterAnimation;
    public LinearAnimationSequence repAssistStruggleSpotterAnimation;
    public LinearAnimationSequence repAssistExtremeSpotterAnimation;

    public LinearAnimationSequence repAssistFailSpotterAnimation;

    public LinearAnimationSequence repAssistCompleteSpotterAnimation;
    public LinearAnimationSequence repAssistEasyCompleteSpotterAnimation;
    public LinearAnimationSequence repAssistStruggleCompleteSpotterAnimation;
    public LinearAnimationSequence repAssistExtremeCompleteSpotterAnimation;

    public LinearAnimationSequence repAssistResetSpotterAnimation;
    public LinearAnimationSequence repAssistEasyResetSpotterAnimation;
    public LinearAnimationSequence repAssistStruggleResetSpotterAnimation;
    public LinearAnimationSequence repAssistExtremeResetSpotterAnimation;

    public LinearAnimationSequence startRestSpotterAnimation;
    public LinearAnimationSequence idleRestSpotterAnimation;
    public LinearAnimationSequence[] startFlexRestSpotterAnimations;
    public LinearAnimationSequence[] flexRestSpotterAnimations;
    public LinearAnimationSequence[] stopFlexRestSpotterAnimations;
    public LinearAnimationSequence stopRestSpotterAnimation;

    public LinearAnimationSequence startRestTiredSpotterAnimation;
    public LinearAnimationSequence idleRestTiredSpotterAnimation;
    public LinearAnimationSequence[] startFlexRestTiredSpotterAnimations;
    public LinearAnimationSequence[] flexRestTiredSpotterAnimations;
    public LinearAnimationSequence[] stopFlexRestTiredSpotterAnimations;
    public LinearAnimationSequence stopRestTiredSpotterAnimation;

    public LinearAnimationSequence idleRestToTiredSpotterAnimation;

    public HashSet<CustomAnimation> GetAssets(HashSet<CustomAnimation> assets = null)
    {
        if (assets == null) assets = new HashSet<CustomAnimation>();

        var fields = GetType().GetFields();
        foreach (var field in fields)
        {
            var value = field.GetValue(this);
            if (value is LinearAnimationSequence seq)
            {
                seq.GetAssets(assets);
            }
            else if (value is LinearAnimationSequence[] seqs)
            {
                foreach (var seq2 in seqs)
                {
                    seq2.GetAssets(assets);
                }
            }
        }

        return assets;
    }
}

[Serializable]
public class LinearAnimationSequence
{

    [Serializable]
    public class BlendedAnim
    {
        public string name;
        public float speed = 1f;
        public float startNormalizedTime;
        public AnimationLoopMode loopMode;
        public string speedMultiplierParameter;
        public string timeOverrideParameter;
        public bool useNormalizedTimeForOverriding;

        public TimeSyncMode syncMode;
        [Tooltip("If there is a base animation, 0 refers to it, otherwise refers to the first blended animation.")]
        public int localSyncReferenceIndex = -1;

        [Tooltip("Defaults to 1 if left at zero.")]
        public float baseWeight;

        [Tooltip("Optional parameter to use for blending.")]
        public string blendParameter;
        [Tooltip("Value range of parameter to map to 0 to 1 blend weight.")]
        public Vector2 parameterMapping;
        public bool clampWeight;

        public AssetIdentifier animation;

        public bool IsValid => animation.IsValid;

        [NonSerialized]
        private CustomAnimation asset;
        public CustomAnimation Asset
        {
            get
            {
                if (asset == null)
                {
                    if (!ContentManager.TryFindContent<CustomAnimation>(animation.name, animation.package, out asset)) Debug.LogError($"Animation '{animation}' not found!");
                }

                return asset;
            }
        }
    }

    public string id;

    [NonSerialized]
    public CustomAnimationLayerState startLayerState;
    [NonSerialized]
    public float cachedStartStateLengthInSeconds;

    [NonSerialized]
    public CustomAnimationLayerState endLayerState;
    [NonSerialized]
    public float cachedEndStateLengthInSeconds;

    [Serializable]
    public class Anim
    {
        public string name;
        public float startNormalizedTime;
        public float speed = 1f;
        public AnimationLoopMode loopMode;
        public string speedMultiplierParameter;
        public string timeOverrideParameter;
        public bool useNormalizedTimeForOverriding; 

        public AssetIdentifier baseAnimation;

        public BlendedAnim[] blendedAnimations;
        public bool blendNormalizeDurations;
        public float blendNormalizedDuration;
        public bool blendWaitForLoops;
        public bool blendForceSyncOnProgress;
        public bool blendUseDedicatedChildForTime;
        public int blendTimeDedicatedChildIndex;

        public bool NeedsBlending => blendedAnimations != null && blendedAnimations.Length > 0;

        public bool useDefaultTransition;
        public float defaultTransitionDuration;
        [Tooltip("The transition to use when entering this animation.")]
        public Transition transition;

        public Transition GetTransition(float fromLengthInSeconds) 
        {
            if (useDefaultTransition)
            {
                var defaultTransition = new Transition();
                defaultTransition.transitionTime = defaultTransitionDuration;
                defaultTransition.validRequirementPaths = new TransitionRequirement[]
                {
                            new TransitionRequirement()
                            {
                                 validNormalizedTimeRanges = new TransitionTimeRange[]
                                 {
                                     new TransitionTimeRange(fromLengthInSeconds <= 0 ? 0f : Mathf.Max(0f, 1f - (defaultTransitionDuration / fromLengthInSeconds)), 1f) 
                                 }
                            }
                };

                return defaultTransition;
            }

            return transition;
        }

        public bool IsValid
        {
            get
            {
                if (baseAnimation.IsValid) return true;

                if (blendedAnimations != null)
                {
                    foreach (var item in blendedAnimations)
                    {
                        if (item.animation.IsValid) return true;
                    }
                }

                return false;
            }
        }

        [NonSerialized]
        private CustomAnimation baseAsset;
        public CustomAnimation BaseAsset
        {
            get
            {
                if (baseAsset == null && baseAnimation.IsValid)
                {
                    if (!ContentManager.TryFindContent<CustomAnimation>(baseAnimation.name, baseAnimation.package, out baseAsset)) Debug.LogError($"Animation '{baseAnimation}' not found!");
                }

                return baseAsset;
            }
        }

        public HashSet<CustomAnimation> GetAssets(HashSet<CustomAnimation> assets = null)
        {
            if (assets == null) assets = new HashSet<CustomAnimation>();

            var ba = BaseAsset;
            if (ba != null) assets.Add(ba);

            if (blendedAnimations != null)
            {
                foreach (var item in blendedAnimations)
                {
                    var ba2 = item.Asset;
                    if (ba2 != null) assets.Add(ba2);
                }
            }

            return assets;
        }
    }

    [Serializable]
    public struct TransitionWithID
    {
        public string id;
        public Transition transition;
    }

    public Transition defaultEnterTransition;
    public TransitionWithID[] enterTransitionsByID;

    public Transition GetEnterTransition(string id)
    {
        if (enterTransitionsByID != null)
        {
            foreach (var item in enterTransitionsByID)
            {
                if (item.id == id) return item.transition;
            }
        }

        return defaultEnterTransition;
    }

    public Transition defaultExitTransition;
    public TransitionWithID[] exitTransitionsByID;

    public Transition GetExitTransition(string id)
    {
        if (exitTransitionsByID != null)
        {
            foreach (var item in exitTransitionsByID)
            {
                if (item.id == id) return item.transition;
            }
        }

        return defaultExitTransition;
    }

    [Tooltip("The linear sequence of animations that will play out, looping the final animation if possible.")]
    public Anim[] sequence;

    public bool IsValid => sequence != null && sequence.Length > 0;

    public HashSet<CustomAnimation> GetAssets(HashSet<CustomAnimation> assets = null)
    {
        if (assets == null) assets = new HashSet<CustomAnimation>();

        if (sequence != null)
        {
            foreach (var anim in sequence)
            {
                anim.GetAssets(assets);
            }
        }

        return assets;
    }

}