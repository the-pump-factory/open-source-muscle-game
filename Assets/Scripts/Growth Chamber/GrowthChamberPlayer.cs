using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using PurrNet;

using BulkOut;

using Swole;
using Swole.API.Unity.Animation;
using Swole.Animation;

[RequireComponent(typeof(SimplePlayer))]
public class GrowthChamberPlayer : NetworkBehaviour, IBulkOutObjectCarrier
{

    protected IBulkOutCarryable carriedObject;
    public IBulkOutCarryable CarriedObject => carriedObject;
    public bool IsCarryingObject => carriedObject != null;

    public bool Pickup(IBulkOutCarryable carryable)
    {
        if (!carryable.Pickup(this, simplePlayer.LeftHandeItemBone, simplePlayer.RightHandeItemBone)) return false;

        animLayerAdd_carry.mix = 1f;
        animLayerOverride_carry.mix = 1f; 

        this.carriedObject = carryable;
        return true;
    }

    private IBulkOutCarryable toPickUp = null;
    private float toPickUpMinDistance;
    public void RegisterForPickup(IBulkOutCarryable carryable, float distance)
    {
        if (IsCarryingObject) return;

        if (toPickUp == null || distance < toPickUpMinDistance)
        {
            toPickUp = carryable;
            toPickUpMinDistance = distance;
        }
    }

    public bool DropCarriedObject()
    {
        if (!IsCarryingObject) return false;
        if (carriedObject.Drop())
        {
            animLayerAdd_carry.mix = 0f;
            animLayerOverride_carry.mix = 0f; 

            carriedObject = null;

            return true;
        }

        return false;
    }

    protected readonly Dictionary<CustomAnimationLayer, float> tempLayerMixes = new Dictionary<CustomAnimationLayer, float>();
    private IEnumerator AlignWithCanisterPort(GrowthChamberSerumTank.CanisterPort port, CustomAnimationLayer mixLayer, float time)
    {
        tempLayerMixes.Clear();
        if (mixLayer != null)
        {
            for (int i = mixLayer.indexInAnimator - 1; i >= 0; i--)
            {
                var lowerLayer = player.Animator.GetTypedLayer(i); 
                if (lowerLayer == null) continue;

                tempLayerMixes[lowerLayer] = lowerLayer.mix;
            }
        }

        Vector3 playerFwd = transform.forward;
        playerFwd.y = 0f;
        playerFwd = playerFwd.normalized;

        Vector3 targetDir = -port.InteractionDirectionWorld; 
        targetDir.y = 0f;
        targetDir = targetDir.normalized;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 targetPos = port.InteractionPointWorld;
        Quaternion targetRot = Quaternion.AngleAxis(Vector3.SignedAngle(playerFwd, targetDir, Vector3.up), Vector3.up) * transform.rotation;

        float t = 0f;

        if (time > 0f)
        {
            while (t < time)
            {
                t += Time.deltaTime;
                float t_ = t / time;

                if (mixLayer != null) 
                {
                    mixLayer.mix = t_;
                    foreach (var entry in tempLayerMixes) entry.Key.mix = 1f - t_;
                }
                transform.SetPositionAndRotation(Vector3.LerpUnclamped(startPos, targetPos, t), Quaternion.SlerpUnclamped(startRot, targetRot, t_));

                yield return null;
            }
        }

        if (mixLayer != null) mixLayer.mix = 1f;
        transform.SetPositionAndRotation(targetPos, targetRot); 
    }

    private int unscrewTurns;
    private GrowthChamberSerumTank.CanisterPort targetCanisterPort;
    private Vector3 canisterVelocity;
    private Vector3 canisterPrevPos;
    private IEnumerator WaitAndRecalculateCanisterVelocity()
    {
        yield return new WaitForEndOfFrame();

        if (targetCanisterPort == null || targetCanisterPort.insertedCanister == null) yield break; 

        var canisterPos = targetCanisterPort.insertedCanister.transform.position; 
        canisterVelocity = (canisterVelocity == Vector3.zero ? (canisterPos - canisterPrevPos) : Vector3.MoveTowards(canisterVelocity, canisterPos - canisterPrevPos, Time.deltaTime * 10f)); 
    }
    private void RecalculateCanisterVelocity()
    {
        StartCoroutine(WaitAndRecalculateCanisterVelocity());
    }
    public bool StartLoadingCanister(GrowthChamberSerumTank.CanisterPort port)
    {
        if (!(carriedObject is GrowthChamberRefillCanister canister)) return false;

        actionState = ActionState.CanisterUnscrew;

        targetCanisterPort = port;

        canister.StopSyncingWithHolder();
        canister.transform.SetParent(transform, false); 
        canister.transform.SetPositionAndRotation(transform.position + Vector3.up * canister.length, Quaternion.FromToRotation((canister.transform.rotation * canister.unscrewDir).normalized, Vector3.up) * canister.transform.rotation);

        unscrewTurns = 0;
        animParam_unscrewProgress.SetValue(0f);
        animParam_unscrewHeight.SetValue(0f);
        unscrewSpeed.value = 0f; 

        simplePlayer.PauseMovement();

        int stateIndex = animLayer_canisterHandling.FindStateIndex(layerState_unscrew);
        animLayer_canisterHandling.SetActiveStateUnsafe(stateIndex);
        animLayer_canisterHandling.ActiveState.Reset();

        StartCoroutine(AlignWithCanisterPort(port, animLayer_canisterHandling, 0.5f));   

        return true;
    }
    public bool StartUnloadingCanister(GrowthChamberSerumTank.CanisterPort port)
    {
        if (IsCarryingObject || !port.HasCanister) return false; 

        actionState = ActionState.CanisterUnload;

        targetCanisterPort = port;

        simplePlayer.PauseMovement();

        canisterVelocity = Vector3.zero;
        canisterPrevPos = port.insertedCanister.transform.position;
        carriedObject = port.insertedCanister;

        bool unloadRight = Vector3.Dot(transform.right, port.tank.DisposalTargetPositionWorld - transform.position) >= 0f;

        int stateIndex = animLayer_canisterHandling.FindStateIndex(unloadRight ? layerState_unloadCanisterR : layerState_unloadCanisterL);
        animLayer_canisterHandling.SetActiveStateUnsafe(stateIndex);
        animLayer_canisterHandling.ActiveState.Reset();

        StartCoroutine(AlignWithCanisterPort(port, animLayer_canisterHandling, 0.5f)); 

        return true;
    }

    public GameObject GameObject => gameObject;
    public Transform Transform => transform;

    [Serializable]
    public enum ActionState
    {
        Default, InGrowthStation, CanisterUnscrew, CanisterLoad, CanisterUnload
    }

    protected ActionState actionState;
    public ActionState State => actionState;

    public void ResetToDefaultState()
    {
        actionState = ActionState.Default;
        player.UnpauseMovement();

        if (!IsCarryingObject)
        {
            animLayerAdd_carry.mix = 0f;
            animLayerOverride_carry.mix = 0f; 
        }

        tempLayerMixes.Clear(); 
    }

    [SerializeField]
    protected SimplePlayer player;
    public SimplePlayer simplePlayer => player;

    public const string prefix = "GC";

    public const string layerName_carry = "carry";
    public const string layerName_canisterHandling = "canisterHandling";

    public const string layerState_unscrew = "unscrew";
    public const string layerState_loadCanister = "canisterLoad";
    public const string layerState_unloadCanisterL = "canisterUnloadL";
    public const string layerState_unloadCanisterR = "canisterUnloadR";

    public const string paramName_unscrewProgress = "unscrewProgress";
    public const string paramName_unscrewHeight = "unscrewHeight";

    protected string additiveLayerName = "Add";
    protected string overrideLayerName = "Override";

    public string GetAdditiveLayerName(string baseLayerName) => $"{baseLayerName}_{additiveLayerName}";
    public string GetOverrideLayerName(string baseLayerName) => $"{baseLayerName}_{overrideLayerName}"; 

    [SerializeField]
    protected SimplePlayerAssets.AnimGroup[] carryDefaultAnims;
    public float carryLayerMix = 0.95f;

    public float unscrewEndNormalizedTime = 1f;
    public float unscrewDegrees = 35f;
    [SerializeField]
    protected SimplePlayerAssets.AnimGroup canisterUnscrewAnim;
    [SerializeField]
    protected SimplePlayerAssets.AnimGroup canisterUnscrewHeightAnim;
    [SerializeField]
    protected SimplePlayerAssets.AnimGroup canisterUnscrewIdleAnim;

    [SerializeField]
    protected SimplePlayerAssets.AnimGroup canisterLoadAnim;
    [SerializeField]
    protected SimplePlayerAssets.AnimGroup canisterUnloadAnimL;
    [SerializeField]
    protected SimplePlayerAssets.AnimGroup canisterUnloadAnimR;

    private CustomAnimationController animationController;


    #region Animation Instances

    [NonSerialized]
    protected CustomAnimationLayer animLayerAdd_carry = null;
    [NonSerialized]
    protected CustomAnimationLayer animLayerOverride_carry = null;
    [NonSerialized]
    protected CustomAnimationLayer animLayer_canisterHandling = null;

    [NonSerialized]
    protected CustomAnimationParameter.Float animParam_unscrewHeight = null;
    [NonSerialized]
    protected CustomAnimationParameter.Float animParam_unscrewProgress = null;

    #endregion


    protected void Awake()
    {
        if (player == null) player = gameObject.GetComponent<SimplePlayer>();

        player.OnUpdate.AddListener(UpdateStep);
        player.OnLateUpdate.AddListener(LateUpdateStep);
        player.OnFixedUpdate.AddListener(FixedUpdateStep);
    }
    protected void Start()
    {
        animationController = ScriptableObject.CreateInstance<CustomAnimationController>();

        List<CustomAnimationLayer> layers = new List<CustomAnimationLayer>();
        CustomAnimationParametersBuilder parametersBuilder = new CustomAnimationParametersBuilder();

        SetupCarryAnims(animationController, layers);
        SetupCanisterHandlingAnims(animationController, parametersBuilder, layers);

        parametersBuilder.Finalize(animationController); 
        animationController.layers = layers.ToArray();
        animationController.Prefix = prefix;
        animationController.ApplyTo(player.Animator, true);

        animLayerAdd_carry = player.Animator.FindTypedLayer(animationController.ConvertLayerName(GetAdditiveLayerName(layerName_carry)));
        animLayerOverride_carry = player.Animator.FindTypedLayer(animationController.ConvertLayerName(GetOverrideLayerName(layerName_carry)));

        animLayer_canisterHandling = player.Animator.FindTypedLayer(animationController.ConvertLayerName(layerName_canisterHandling));

        animParam_unscrewHeight = (CustomAnimationParameter.Float)player.Animator.FindParameter(paramName_unscrewHeight);
        animParam_unscrewProgress = (CustomAnimationParameter.Float)player.Animator.FindParameter(paramName_unscrewProgress);

        if (player.Animator != null)
        {
            player.Animator.SubscribePostAnimationEvent(ProcessAnimationEvent);
        }
    }

    protected readonly List<Vector2> tempIndices = new List<Vector2>();
    protected readonly List<CustomAnimationLayerState> tempLayerStates = new List<CustomAnimationLayerState>();
    protected readonly List<MotionPart> tempMotionParts = new List<MotionPart>();

    protected virtual void SetupCarryAnims(CustomAnimationController animationController, List<CustomAnimationLayer> layers)
    {
        CustomMotionControllersBuilder motionControllersBuilderAdd = new CustomMotionControllersBuilder();
        CustomMotionControllersBuilder motionControllersBuilderOverride = new CustomMotionControllersBuilder();

        var carryStateControllerAdd = new CustomAnimationLayerState("default");
        var carryStateControllerOverride = new CustomAnimationLayerState("default");
        if (carryDefaultAnims != null && carryDefaultAnims.Length > 0)
        {
            var topCompositeAdd = new MotionComposite($"mc_{layerName_carry}", null);
            motionControllersBuilderAdd.AddMotionComposite(topCompositeAdd, out int topIndexAdd, out _);

            var topCompositeOverride = new MotionComposite($"mc_{layerName_carry}", null);
            motionControllersBuilderOverride.AddMotionComposite(topCompositeOverride, out int topIndexOverride, out _);

            carryStateControllerAdd.motionControllerIndex = topIndexAdd;
            carryStateControllerOverride.motionControllerIndex = topIndexOverride;

            tempMotionParts.Clear();
            foreach (var group in carryDefaultAnims)
            {
                if (!group.isAdditive || group.anims == null) continue;

                foreach (var anim in group.anims)
                {
                    if (!ContentManager.TryFindContent<CustomAnimation>(anim.animId.name, anim.animId.package, out var animAsset))
                    {
                        swole.LogError($"Animation asset {anim.animId.name} not found!");
                        continue;
                    }

                    motionControllersBuilderAdd.AddAnimationReference(new AnimationReference(anim.animId.name, animAsset, Swole.Animation.AnimationLoopMode.Loop, 0f), out int mcIndex, out _);
                    tempMotionParts.Add(new MotionPart()
                    {
                        controllerIndex = mcIndex,
                        mix = anim.mix,
                        speed = 1f
                    });
                }
            }
            topCompositeAdd.motionParts = tempMotionParts.ToArray();


            tempMotionParts.Clear();
            foreach (var group in carryDefaultAnims)
            {
                if (group.isAdditive || group.anims == null) continue;

                foreach (var anim in group.anims)
                {
                    if (!ContentManager.TryFindContent<CustomAnimation>(anim.animId.name, anim.animId.package, out var animAsset))
                    {
                        swole.LogError($"Animation asset {anim.animId.name} not found!");
                        continue;
                    }

                    motionControllersBuilderOverride.AddAnimationReference(new AnimationReference(anim.animId.name, animAsset, Swole.Animation.AnimationLoopMode.Loop, 0f), out int mcIndex, out _);
                    tempMotionParts.Add(new MotionPart()
                    {
                        controllerIndex = mcIndex,
                        mix = anim.mix,
                        speed = 1f
                    });
                }
            }
            topCompositeOverride.motionParts = tempMotionParts.ToArray();
        }


        tempLayerStates.Clear();
        tempLayerStates.Add(carryStateControllerAdd);

        var addLayer = new CustomAnimationLayer(GetAdditiveLayerName(layerName_carry), 0f);
        addLayer.IsAdditive = true;
        addLayer.states = tempLayerStates.ToArray(); 


        tempLayerStates.Clear();
        tempLayerStates.Add(carryStateControllerOverride);

        var overrideLayer = new CustomAnimationLayer(GetOverrideLayerName(layerName_carry), 0f);
        overrideLayer.states = tempLayerStates.ToArray();


        motionControllersBuilderAdd.Finalize(animationController, true, out _, out _, out _, out _, true);
        motionControllersBuilderOverride.Finalize(animationController, true, out _, out _, out _, out _, true);

        motionControllersBuilderAdd.Finalize(addLayer);
        motionControllersBuilderOverride.Finalize(overrideLayer);

        layers.Add(addLayer);
        layers.Add(overrideLayer);
    }

    protected virtual void SetupCanisterHandlingAnims(CustomAnimationController animationController, CustomAnimationParametersBuilder parameters, List<CustomAnimationLayer> layers)
    {
        CustomMotionControllersBuilder motionControllers = new CustomMotionControllersBuilder();

        var unscrewStateController = new CustomAnimationLayerState(layerState_unscrew);
        if (canisterUnscrewAnim.anims != null && canisterUnscrewAnim.anims.Length > 0)
        {
            var topComposite = new MotionComposite($"mc_{layerState_unscrew}", null);
            topComposite.forceSyncOnProgress = false;
            topComposite.waitForLoops = false;
            motionControllers.AddMotionComposite(topComposite, out int topIndex, out _);

            unscrewStateController.motionControllerIndex = topIndex;

            tempMotionParts.Clear();

            int param_unscrewProgress = parameters.FindParameterIndex(paramName_unscrewProgress);
            if (param_unscrewProgress < 0) parameters.AddFloatParameter(new CustomAnimationParameter.Float(paramName_unscrewProgress, 0f), out param_unscrewProgress, out _);
            foreach (var anim in canisterUnscrewAnim.anims)
            {
                if (!ContentManager.TryFindContent<CustomAnimation>(anim.animId.name, anim.animId.package, out var animAsset))
                {
                    swole.LogError($"Animation asset {anim.animId.name} not found!");
                    continue;
                }

                var animRef = new AnimationReference(anim.animId.name, animAsset, Swole.Animation.AnimationLoopMode.Loop, 0f);
                animRef.timeOverrideParameter = param_unscrewProgress;
                motionControllers.AddAnimationReference(animRef, out int mcIndex, out _);
                tempMotionParts.Add(new MotionPart()
                {
                    controllerIndex = mcIndex,
                    mix = anim.mix,
                    speed = 1f
                });
            }

            foreach (var anim in canisterUnscrewIdleAnim.anims)
            {
                if (!ContentManager.TryFindContent<CustomAnimation>(anim.animId.name, anim.animId.package, out var animAsset))
                {
                    swole.LogError($"Animation asset {anim.animId.name} not found!"); 
                    continue;
                }

                motionControllers.AddAnimationReference(new AnimationReference(anim.animId.name, animAsset, Swole.Animation.AnimationLoopMode.Loop, 0f), out int mcIndex, out _);
                tempMotionParts.Add(new MotionPart()
                {
                    controllerIndex = mcIndex, 
                    mix = anim.mix,
                    speed = 1f 
                });
            }

            int param_unscrewHeight = parameters.FindParameterIndex(paramName_unscrewHeight);
            if (param_unscrewHeight < 0) parameters.AddFloatParameter(new CustomAnimationParameter.Float(paramName_unscrewHeight, 0f), out param_unscrewHeight, out _);
            foreach (var anim in canisterUnscrewHeightAnim.anims)
            {
                if (!ContentManager.TryFindContent<CustomAnimation>(anim.animId.name, anim.animId.package, out var animAsset))
                {
                    swole.LogError($"Animation asset {anim.animId.name} not found!");
                    continue;
                }

                motionControllers.AddAnimationReference(new AnimationReference(anim.animId.name, animAsset, Swole.Animation.AnimationLoopMode.Loop, 0f), out int mcIndex, out _);
                tempMotionParts.Add(new MotionPart()
                {
                    controllerIndex = mcIndex,
                    mix = anim.mix,
                    mixParameterIndex = param_unscrewHeight,
                    speed = 0f
                });
            }

            topComposite.motionParts = tempMotionParts.ToArray();
        }


        var loadStateController = new CustomAnimationLayerState(layerState_loadCanister);
        if (canisterLoadAnim.anims != null && canisterLoadAnim.anims.Length > 0)
        {
            var topComposite = new MotionComposite($"mc_{layerState_loadCanister}", null); 
            motionControllers.AddMotionComposite(topComposite, out int topIndex, out _);

            loadStateController.motionControllerIndex = topIndex;

            tempMotionParts.Clear();

            foreach (var anim in canisterLoadAnim.anims)
            {
                if (!ContentManager.TryFindContent<CustomAnimation>(anim.animId.name, anim.animId.package, out var animAsset))
                {
                    swole.LogError($"Animation asset {anim.animId.name} not found!");
                    continue;
                }

                var animRef = new AnimationReference(anim.animId.name, animAsset, anim.playOnce ? Swole.Animation.AnimationLoopMode.PlayOnce : Swole.Animation.AnimationLoopMode.Loop, 0f);
                motionControllers.AddAnimationReference(animRef, out int mcIndex, out _);
                tempMotionParts.Add(new MotionPart()
                {
                    controllerIndex = mcIndex,
                    mix = anim.mix,
                    speed = 1f
                });
            }

            topComposite.motionParts = tempMotionParts.ToArray();
        }

        var unloadStateControllerL = new CustomAnimationLayerState(layerState_unloadCanisterL);
        if (canisterUnloadAnimL.anims != null && canisterUnloadAnimL.anims.Length > 0)
        {
            var topComposite = new MotionComposite($"mc_{layerState_unloadCanisterL}", null);
            motionControllers.AddMotionComposite(topComposite, out int topIndex, out _);

            unloadStateControllerL.motionControllerIndex = topIndex;

            tempMotionParts.Clear();

            foreach (var anim in canisterUnloadAnimL.anims)
            {
                if (!ContentManager.TryFindContent<CustomAnimation>(anim.animId.name, anim.animId.package, out var animAsset))
                {
                    swole.LogError($"Animation asset {anim.animId.name} not found!");
                    continue;
                }

                var animRef = new AnimationReference(anim.animId.name, animAsset, anim.playOnce ? Swole.Animation.AnimationLoopMode.PlayOnce : Swole.Animation.AnimationLoopMode.Loop, 0f);
                motionControllers.AddAnimationReference(animRef, out int mcIndex, out _);
                tempMotionParts.Add(new MotionPart()
                {
                    controllerIndex = mcIndex,
                    mix = anim.mix,
                    speed = 1f
                });
            }

            topComposite.motionParts = tempMotionParts.ToArray();
        }
        var unloadStateControllerR = new CustomAnimationLayerState(layerState_unloadCanisterR);
        if (canisterUnloadAnimR.anims != null && canisterUnloadAnimR.anims.Length > 0)
        {
            var topComposite = new MotionComposite($"mc_{layerState_unloadCanisterR}", null);
            motionControllers.AddMotionComposite(topComposite, out int topIndex, out _);

            unloadStateControllerR.motionControllerIndex = topIndex;

            tempMotionParts.Clear();

            foreach (var anim in canisterUnloadAnimR.anims)
            {
                if (!ContentManager.TryFindContent<CustomAnimation>(anim.animId.name, anim.animId.package, out var animAsset))
                {
                    swole.LogError($"Animation asset {anim.animId.name} not found!");
                    continue;
                }

                var animRef = new AnimationReference(anim.animId.name, animAsset, anim.playOnce ? Swole.Animation.AnimationLoopMode.PlayOnce : Swole.Animation.AnimationLoopMode.Loop, 0f);
                motionControllers.AddAnimationReference(animRef, out int mcIndex, out _);
                tempMotionParts.Add(new MotionPart()
                {
                    controllerIndex = mcIndex,
                    mix = anim.mix,
                    speed = 1f 
                });
            }

            topComposite.motionParts = tempMotionParts.ToArray();  
        }

        tempLayerStates.Clear();
        tempLayerStates.Add(unscrewStateController);
        tempLayerStates.Add(loadStateController);
        tempLayerStates.Add(unloadStateControllerL);
        tempLayerStates.Add(unloadStateControllerR); 

        var layer = new CustomAnimationLayer(layerName_canisterHandling, 0f);
        layer.IsAdditive = true;
        layer.states = tempLayerStates.ToArray();

        motionControllers.Finalize(animationController, true, out _, out _, out _, out _, true);

        motionControllers.Finalize(layer);

        layers.Add(layer);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GameObject.Destroy(animationController);
    }

    private SyncVar<float> unscrewSpeed = new(0f);
    protected void UpdateFromState()
    {
        switch (State)
        {
            case ActionState.Default:

                if (isOwner)
                {
                    if (IsCarryingObject)
                    {
                        if (InputProxy.ObjectInteraction_DropPressed)
                        {
                            DropCarriedObject();
                        }
                    } 
                    else
                    {
                        if (toPickUp != null)
                        {
                            Pickup(toPickUp);
                            toPickUp = null;  
                        }
                    }
                }

                break;

            case ActionState.CanisterUnscrew:

                if (carriedObject is GrowthChamberRefillCanister canister) 
                {
                    if (isOwner)
                    {
                        if (InputProxy.JumpButtonDown)
                        {
                            unscrewSpeed.value = Mathf.Max(0.4f, unscrewSpeed.value + ((15f * Time.deltaTime) / Mathf.Pow(unscrewSpeed.value + 1f, 1.3f)));
                        }
                        else
                        {
                            unscrewSpeed.value = Mathf.Max(0.4f, unscrewSpeed.value - Time.deltaTime * 0.1f);  
                        }
                    }

                    animParam_unscrewProgress.SetValue(Mathf.Clamp01(animParam_unscrewProgress.Value + unscrewSpeed * Time.deltaTime)); 
                    if (animParam_unscrewProgress.Value >= 1f)
                    {
                        animParam_unscrewProgress.SetValue(0f); 
                        unscrewTurns += 1;
                    }
                    canister.unscrew.value = ((unscrewTurns * unscrewDegrees) + (Mathf.Clamp01(animParam_unscrewProgress.Value / unscrewEndNormalizedTime) * unscrewDegrees)) / Mathf.Abs(canister.unscrewMaxRotation); 
                    animParam_unscrewHeight.SetValue(canister.unscrew); 
                    if (canister.unscrew >= 1f || !canister.HasCap) 
                    {
                        //animParam_unscrewProgress.SetValue(0f);
                        canister.PopCap();

                        actionState = ActionState.CanisterLoad; 
                        int stateIndex = animLayer_canisterHandling.FindStateIndex(layerState_loadCanister);
                        animLayer_canisterHandling.SetActiveStateUnsafe(stateIndex); 
                        animLayer_canisterHandling.ActiveState.Reset();   
                    }
                }

                break;

            case ActionState.CanisterLoad:

                if (targetCanisterPort != null) 
                {
                    if (targetCanisterPort.insertedCanister != null && targetCanisterPort.insertedCanister.transform.parent != targetCanisterPort.tank.transform) // canister has been inserted by character animation and is ready to be nudged into place
                    {
                        targetCanisterPort.insertedCanister.Rigidbody.isKinematic = true; 
                        targetCanisterPort.insertedCanister.transform.SetParent(targetCanisterPort.tank.transform, true); 
                        StartCoroutine(targetCanisterPort.MoveCanisterIntoSlot(0.5f));

                        animLayerAdd_carry.mix = 0f;
                        animLayerOverride_carry.mix = 0f;

                        if (carriedObject != null)
                        {
                            carriedObject.Carrier = null;
                            carriedObject = null;
                        }
                    }

                    var activeState = animLayer_canisterHandling.ActiveState;
                    var activeMC = animLayer_canisterHandling.GetMotionControllerUnsafe(activeState.MotionControllerIndex); 
                    if (activeMC is MotionComposite comp)
                    {
                        var baseAnim = animLayer_canisterHandling.GetMotionControllerUnsafe(comp.motionParts[0].controllerIndex);
                        float nT = baseAnim.GetNormalizedTime(animLayer_canisterHandling);

                        if (!tempLayerMixes.TryGetValue(animLayer_canisterHandling, out float targetMix)) targetMix = 1f; // check if we're already blending
                        if (nT > 0.9f && targetMix > 0f) 
                        {
                            tempLayerMixes.Remove(animLayerAdd_carry);
                            tempLayerMixes.Remove(animLayerOverride_carry); 

                            tempLayerMixes[animLayer_canisterHandling] = 0f;  
                            StartCoroutine(player.Animator.BlendLayers(tempLayerMixes, baseAnim.GetScaledDuration(animLayer_canisterHandling) * ((1f - nT) * 0.95f), () => ResetToDefaultState()));  
                        }
                    }
                }
                else
                {
                    actionState = ActionState.Default;
                    foreach (var entry in tempLayerMixes) entry.Key.mix = entry.Value;
                    animLayer_canisterHandling.mix = 0f;
                }
                break;

            case ActionState.CanisterUnload:

                if (targetCanisterPort != null)
                {
                    if (targetCanisterPort.insertedCanister != null)
                    {
                        if (targetCanisterPort.insertedCanister.transform.parent == targetCanisterPort.tank.transform)
                        {
                            targetCanisterPort.insertedCanister.Rigidbody.isKinematic = true;
                            targetCanisterPort.insertedCanister.transform.SetParent(null, true);
                        }

                        if (targetCanisterPort.insertedCanister.transform.parent != null)
                        {
                            RecalculateCanisterVelocity(); 
                        }
                    }
                    
                    var activeState = animLayer_canisterHandling.ActiveState;
                    var activeMC = animLayer_canisterHandling.GetMotionControllerUnsafe(activeState.MotionControllerIndex);
                    if (activeMC is MotionComposite comp)
                    {
                        var baseAnim = animLayer_canisterHandling.GetMotionControllerUnsafe(comp.motionParts[0].controllerIndex);
                        float nT = baseAnim.GetNormalizedTime(animLayer_canisterHandling);

                        if (!tempLayerMixes.TryGetValue(animLayer_canisterHandling, out float targetMix)) targetMix = 1f; // check if we're already blending
                        if (nT > 0.9f && targetMix > 0f)
                        {
                            if (carriedObject != null)
                            {
                                carriedObject.Carrier = null; 
                                carriedObject = null;
                            }

                            tempLayerMixes.Remove(animLayerAdd_carry);
                            tempLayerMixes.Remove(animLayerOverride_carry);

                            tempLayerMixes[animLayer_canisterHandling] = 0f;
                            StartCoroutine(player.Animator.BlendLayers(tempLayerMixes, baseAnim.GetScaledDuration(animLayer_canisterHandling) * ((1f - nT) * 0.95f), () => ResetToDefaultState())); 
                        }
                    }
                }
                else
                {
                    actionState = ActionState.Default;
                    foreach (var entry in tempLayerMixes) entry.Key.mix = entry.Value;
                    animLayer_canisterHandling.mix = 0f;
                }
                break;
        }

        toPickUp = null;
    }

    protected IEnumerator ProcessAnimationEventAtEndOfFrame(string eventName, float val, object sender)
    {
        yield return new WaitForEndOfFrame();

        switch (eventName)
        {
            case SimplePlayer.eventName_parentHeldItemL:
                if (IsCarryingObject)
                {
                    CarriedObject.Transform.SetParent(player.LeftHandeItemBone, true);
                }
                break;
            case SimplePlayer.eventName_parentHeldItemR:
                if (IsCarryingObject)
                {
                    CarriedObject.Transform.SetParent(player.RightHandeItemBone, true);
                }
                break;

            case SimplePlayer.eventName_parentHeldItemResetL:
                if (IsCarryingObject)
                {
                    var t = CarriedObject.Transform;
                    t.SetParent(player.LeftHandeItemBone, false);
                    t.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    t.localScale = Vector3.one;
                }
                break;
            case SimplePlayer.eventName_parentHeldItemResetR:
                if (IsCarryingObject)
                {
                    var t = CarriedObject.Transform;
                    t.SetParent(player.RightHandeItemBone, false);
                    t.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    t.localScale = Vector3.one;
                }
                break;

            case SimplePlayer.eventName_unparentHeldItemL:
                if (IsCarryingObject && CarriedObject.Transform.parent != null)
                {
                    CarriedObject.Transform.SetParent(null, true);
                    if (State == ActionState.CanisterLoad)
                    {
                        if (CarriedObject is GrowthChamberRefillCanister canister && targetCanisterPort != null) targetCanisterPort.insertedCanister = canister; // allows this script to physically slot canister into port
                    } 
                    else if (State == ActionState.CanisterUnload)
                    {
                        if (CarriedObject is GrowthChamberRefillCanister canister)
                        {
                            canister.Rigidbody.isKinematic = false;
                            canister.Rigidbody.velocity = canisterVelocity.normalized * 2f;//canisterVelocity;
                        }

                        if (targetCanisterPort != null) targetCanisterPort.insertedCanister = null; 
                    }
                }
                break;
            case SimplePlayer.eventName_unparentHeldItemR:
                if (IsCarryingObject)
                {
                    CarriedObject.Transform.SetParent(null, true);
                    if (State == ActionState.CanisterLoad)
                    {
                        if (CarriedObject is GrowthChamberRefillCanister canister && targetCanisterPort != null) targetCanisterPort.insertedCanister = canister; // allows this script to physically slot canister into port
                    }
                    else if (State == ActionState.CanisterUnload)
                    {
                        if (CarriedObject is GrowthChamberRefillCanister canister) 
                        {
                            canister.Rigidbody.isKinematic = false;
                            canister.Rigidbody.velocity = canisterVelocity.normalized * 2f;// canisterVelocity;
                        }

                        if (targetCanisterPort != null) targetCanisterPort.insertedCanister = null;
                    }
                }
                break;
        }
    }
    protected void ProcessAnimationEvent(string eventName, float val, object sender)
    {
        StartCoroutine(ProcessAnimationEventAtEndOfFrame(eventName, val, sender));
    }

    protected void UpdateStep()
    {

    }
    protected void LateUpdateStep()
    {
        UpdateFromState();
    }
    protected void FixedUpdateStep()
    {

    }
}


public interface IBulkOutObjectCarrier
{

    public GameObject GameObject { get; }
    public Transform Transform { get; }

    public IBulkOutCarryable CarriedObject { get; }
    public bool IsCarryingObject { get; }

    public bool Pickup(IBulkOutCarryable carryable);

    public bool DropCarriedObject();

    public void RegisterForPickup(IBulkOutCarryable carryable, float distance);

}

public interface IBulkOutCarryable
{

    public GameObject GameObject { get; }
    public Transform Transform { get; }

    public bool Pickup(IBulkOutObjectCarrier carrier, Transform leftHolder, Transform rightHolder);
    public bool Drop();

    public void Toss(Vector3 velocity);

    public IBulkOutObjectCarrier Carrier { get; set; }

    public Transform LeftHolder { get; set; }
    public Transform RightHolder { get; set; }

    public bool IsBeingCarried { get; }

}
