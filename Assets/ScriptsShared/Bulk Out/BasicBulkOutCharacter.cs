using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;

using UnityEngine;
using UnityEngine.Events;

using PurrNet;

using Swole;
using Swole.UI;
using Swole.Animation;
using Swole.API.Unity;
using Swole.API.Unity.Animation;

using static BulkOutCharacter;

public class BasicBulkOutCharacter : NetworkBehaviour, IBulkOutCharacter
{

#if UNITY_EDITOR
    public void OnDrawGizmosSelected()
    {
        if (footL_transform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(footL_transform.position + Vector3.down * footHeightOffset, 0.03f);
            Gizmos.DrawRay(footL_transform.position + Vector3.down * footHeightOffset, Vector3.down * 0.1f);
            Gizmos.DrawWireSphere(footL_transform.position + Vector3.down * footSoleHeightOffset, 0.02f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(footR_transform.position + Vector3.down * footHeightOffset, 0.03f);
            Gizmos.DrawRay(footR_transform.position + Vector3.down * footHeightOffset, Vector3.down * 0.1f);
            Gizmos.DrawWireSphere(footR_transform.position + Vector3.down * footSoleHeightOffset, 0.02f);
        }

        if (heightContributors != null)
        {
            Gizmos.color = Color.magenta;

            foreach (var heightContr in heightContributors)
            {
                if (heightContr.transform == null) continue;

                Gizmos.DrawSphere(heightContr.Position, 0.01f);
            }
        }
    }
#endif

    private IBulkOutCharacterInputProvider inputProvider = new BulkOutCharacterEmptyInput();
    public IBulkOutCharacterInputProvider InputProvider => inputProvider;
    public void SetInputProvider(IBulkOutCharacterInputProvider inputProvider)
    {
        this.inputProvider = inputProvider;
    }

    [SerializeField]
    protected BulkOutCamera gameCamera;
    public void SetBulkOutCamera(BulkOutCamera camera)
    {
        gameCamera = camera;
    }

    [SerializeField]
    protected SandboxCharacter sandboxCharacter;
    public void SetSandboxCharacter(SandboxCharacter sandboxCharacter)
    {
        this.sandboxCharacter = sandboxCharacter;
    }

    #region World Interaction

    public int InteractionManagerID => GetInstanceID();

    /// <summary>
    /// Represents an interactable presence that is currently in proximity to the character.
    /// </summary>
    protected class ValidInteractable
    {
        public int activeTrigger;
        public HashSet<int> activeTriggers = null;

        public IInteractable interactable;
        public int interactionIndex = -1;
        public int performableInteractionIndex = -1; // TODO: rework this to allow for multiple choices?

        /// <summary>
        /// Returns true if the interactable presence is no longer valid
        /// </summary>
        public bool RevokeTrigger(int triggerId)
        {
            if (activeTrigger == triggerId) 
            { 
                activeTrigger = 0; 
                if (activeTriggers == null || activeTriggers.Count <= 0) return true;
            }

            if (activeTriggers != null)
            {
                activeTriggers.Remove(triggerId);
                if (activeTriggers.Count <= 0 && activeTrigger == 0) return true;
            }

            return false;
        }

        public bool CanInteract(Vector3 worldPosition, Quaternion worldRotation)
        {
            return performableInteractionIndex >= 0 && interactionIndex >= 0 && interactable != null && interactable.IsPositionInBounds(worldPosition) && interactable.IsAligned(worldRotation);
        }
        public bool CanAutoInteract(Vector3 worldPosition, Quaternion worldRotation)
        {
            return performableInteractionIndex >= 0 && interactionIndex >= 0 && interactable != null && interactable.AllowAutoInteract && interactable.IsPositionInAutoInteractBounds(worldPosition) && interactable.IsAutoInteractAligned(worldRotation);
        }
    }

    protected Dictionary<int, ValidInteractable> validInteractables = new Dictionary<int, ValidInteractable>();

    public void AllowInteractability(int triggerId, IInteractable interactable)
    {
        if (validInteractables == null || performableInteractions == null) return;

        if (validInteractables.TryGetValue(interactable.InteractableID, out var validInteractable))
        {
            if (triggerId != validInteractable.activeTrigger)
            {
                if (validInteractable.activeTriggers == null) validInteractable.activeTriggers = new HashSet<int>();
                validInteractable.activeTriggers.Add(triggerId);
            }
        } 
        else
        {
            int interactionIndex = -1;
            int performableInteractionIndex = -1;
            for (int i = 0; i < performableInteractions.Length; i++) 
            {
                var performable = performableInteractions[i];

                if (interactable.TryGetInteractionIndex(performable.interactionId, out var interactionIndex_))
                {
                    interactionIndex = interactionIndex_;
                    performableInteractionIndex = i;
                    break;
                }
            }
            if (interactionIndex < 0) return;

            validInteractable = new ValidInteractable();
            validInteractable.activeTrigger = triggerId;
            validInteractable.interactable = interactable;
            validInteractable.interactionIndex = interactionIndex;
            validInteractable.performableInteractionIndex = performableInteractionIndex;

            validInteractables[interactable.InteractableID] = validInteractable;

            Debug.Log($"ALLOWING INTERACTION WITH {interactable}");
        }
    }
    public void RevokeInteractability(int triggerId, IInteractable interactable)
    {
        if (interactable == null || validInteractables == null || !validInteractables.TryGetValue(interactable.InteractableID, out var validInteractable)) return;
        if (validInteractable.RevokeTrigger(triggerId)) 
        { 
            validInteractables.Remove(interactable.InteractableID); 
        }
    }

    protected ValidInteractable GetClosestInteractable() => GetClosestInteractable(transform.position, transform.rotation);
    protected ValidInteractable GetClosestInteractable(Vector3 worldPosition, Quaternion worldRotation)
    {
        ValidInteractable closest = null;
        float minDist = float.MaxValue;

        foreach (var entry in validInteractables)
        {
            var vi = entry.Value;
            if (!vi.CanInteract(worldPosition, worldRotation)) continue;

            var performableInteraction = performableInteractions[vi.performableInteractionIndex];

            float dist = (worldPosition - vi.interactable.WorldPosition).sqrMagnitude;
            if (dist < minDist)
            {
                if (animator != null && interactionAnimationLayerIndices != null)
                {
                    bool foundTransition = false;

                    for (int i = 0; i < interactionAnimationLayerIndices.Length; i++) // look for animation transitions that rely on the interaction's animation flag parameter being armed. This will filter out some interactions that cannot currently be transitioned to.
                    {
                        var animationLayerIndex = interactionAnimationLayerIndices[i];

                        if (animationLayerIndex < 0) continue;

                        var animationLayer = animator.GetTypedLayer(animationLayerIndex);
                        if (!animationLayer.IsActive) continue; // ignore inactive layers

                        var animationState = animationLayer.ActiveState;
                        if (animationState == null) continue;

                        if (animationState.IsTransitioning) animationState = animationLayer.GetStateUnsafe(animationState.TransitionTarget);

                        if (animationState.TransitionCount <= 0) continue;

                        for(int j = 0; j < animationState.TransitionCount; j++)
                        {
                            var transition = animationState.GetTransitionUnsafe(j);
                            if (transition == null || !transition.HasRequirementParameter(performableInteraction.flagParameter, TransitionComparisonType.True)) continue;

                            foundTransition = true;
                            break;
                        }
                    }

                    if (!foundTransition) continue;
                }

                minDist = dist;
                closest = vi;
            }
        }

        return closest; 
    }

    public bool IsInteracting => activeInteraction != null && activeInteractable != null;

    protected IInteractable activeInteractable;
    public IInteractable ActiveInteractable => activeInteractable;

    protected IInteraction activeInteraction;
    public IInteraction ActiveInteraction => activeInteraction;

    protected InteractionPoint interactionPointHandL;
    protected InteractionPoint interactionPointHandR;
    protected InteractionPoint interactionPointFootL;
    protected InteractionPoint interactionPointFootR;

    public void EndActiveInteraction()
    {
        activeInteractable = null;
        activeInteraction = null;

        interactionPointHandL = null;
        interactionPointHandR = null;
        interactionPointFootL = null;
        interactionPointFootR = null;
    }

    [Serializable]
    public struct InteractionRequirement
    {
        public string flagParameter;
        public string interactionId;
        public float timeout;
        public float duration;

        public string snapPointHandL;
        public float snapRotationBlendHandL;
        public string snapPointHandL2;
        public float snapRotationBlendHandL2;

        public string snapPointHandR;
        public float snapRotationBlendHandR;
        public string snapPointHandR2;
        public float snapRotationBlendHandR2;

        public string snapPointFootL;
        public float snapRotationBlendFootL;
        public string snapPointFootL2;
        public float snapRotationBlendFootL2;

        public string snapPointFootR;
        public float snapRotationBlendFootR;
        public string snapPointFootR2;
        public float snapRotationBlendFootR2;
    }

    [Header("Interaction")]
    public string interactionPointTeleportID = "teleport";
    public string[] interactionAnimationLayers;
    [NonSerialized]
    protected int[] interactionAnimationLayerIndices;
    public InteractionRequirement[] performableInteractions; 

    protected void InitializeInteractions()
    {
        interactionAnimationLayerIndices = interactionAnimationLayers == null ? null : new int[interactionAnimationLayers.Length];

        if (animator != null && interactionAnimationLayerIndices != null)
        {
            for(int i = 0; i < interactionAnimationLayerIndices.Length; i++)
            {
                interactionAnimationLayerIndices[i] = animator.FindLayerIndex(controller.ConvertLayerName(interactionAnimationLayers[i]));
            }
        }
    }

    protected void OnPressInteractButton(ValidInteractable closestInteractable)
    {
        if (IsInteracting) return;

        if (closestInteractable != null)
        {
            Debug.Log($"DOING INTERACTION {closestInteractable.interactable.InteractableID}");
            StartCoroutine(BeginInteraction(closestInteractable));
        }
    }

    protected IEnumerator BeginInteraction(ValidInteractable interactable)
    {
        if (interactable == null || interactable.performableInteractionIndex < 0 || animator == null) yield break;

        var performable = performableInteractions[interactable.performableInteractionIndex];
        var flagParameter = animator.FindParameter(performable.flagParameter);
        if (flagParameter == null || !typeof(IAnimationParameterTrigger).IsAssignableFrom(flagParameter.GetType())) yield break;

        var trigger = (IAnimationParameterTrigger)flagParameter;
        if (trigger.IsTrue) yield break;

        float waitTime = 0f;
        trigger.Arm();
        while (trigger.IsTrue)
        {
            waitTime += Time.deltaTime;
            if (performable.timeout > 0f && waitTime > performable.timeout) 
            {
                trigger.TryConsume();
                yield break;
            }

            yield return null;
        }

        var interaction = interactable.interactable[interactable.interactionIndex];
        if (interaction.TryGetInteractionPoint(interactionPointTeleportID, out var tpIp))
        {
            StartCoroutine(BeginInteractionTeleport(tpIp)); 
        }

        interaction.Start(this);

        interaction.TryGetInteractionPoint(performable.snapPointHandL, out snapTarget_handL);
        snapRotationBlend_handL = performable.snapRotationBlendHandL;
        interaction.TryGetInteractionPoint(performable.snapPointHandL2, out snapTarget_handL2);
        snapRotationBlend_handL2 = performable.snapRotationBlendHandL2;
        interaction.TryGetInteractionPoint(performable.snapPointHandR, out snapTarget_handR);
        snapRotationBlend_handR = performable.snapRotationBlendHandR;
        interaction.TryGetInteractionPoint(performable.snapPointHandR2, out snapTarget_handR2);
        snapRotationBlend_handR2 = performable.snapRotationBlendHandR2; 

        interaction.TryGetInteractionPoint(performable.snapPointFootL, out snapTarget_footL);
        snapRotationBlend_footL = performable.snapRotationBlendFootL;
        interaction.TryGetInteractionPoint(performable.snapPointFootL2, out snapTarget_footL2);
        snapRotationBlend_footL2 = performable.snapRotationBlendFootL2;
        interaction.TryGetInteractionPoint(performable.snapPointFootR, out snapTarget_footR);
        snapRotationBlend_footR = performable.snapRotationBlendFootR;
        interaction.TryGetInteractionPoint(performable.snapPointFootR2, out snapTarget_footR2);
        snapRotationBlend_footR2 = performable.snapRotationBlendFootR2;

        float time = 0f;
        while(time < performable.duration)
        {
            time += Time.deltaTime; 
            if (dynamicProperty_interaction_time_pos >= 0)
            {
                interaction.SetTime(dynamicAnimationProperties.GetValueUnsafe(dynamicProperty_interaction_time_pos));
            } 
            else
            {
                interaction.SetTime(time / performable.duration);
            }

            yield return null;
        }

        interaction.End(this);

        ClearLimbSnapPoints(); 
    }

    protected IEnumerator BeginInteractionTeleport(InteractionPoint ip)
    {
        float teleportTime = ip == null ? 0f : ip.transitionTime;
        if (teleportTime <= 0) yield break;

        footLeftIsLocked = false;
        footRightIsLocked = false;
        footLockBlendLeft = 0.5f;
        footLockBlendRight = 0.5f;

        transform.GetPositionAndRotation(out var startPos, out var startRot);
        startRot = Quaternion.Inverse(startRot);

        float t = 0f;
        float prevT_ = 0f;
        while(t < teleportTime) 
        {
            t = Mathf.Min(teleportTime, t + Time.deltaTime); 
            float t_ = t / teleportTime;
            float tDelta_ = t_ - prevT_;

            ip.Refresh();

            Vector3 offsetPos = ip.CurrentWorldPosition - startPos;
            if (ip.ApplyRotation)
            {
                Quaternion offsetRot = ip.CurrentWorldRotation * startRot; 

                transform.GetPositionAndRotation(out var wpos, out var wrot);

                wpos = wpos + offsetPos * tDelta_;
                wrot = Quaternion.SlerpUnclamped(Quaternion.identity, offsetRot, tDelta_) * wrot;

                transform.SetPositionAndRotation(wpos, wrot);
            } 
            else
            {
                var wpos = transform.position;
                wpos = wpos + offsetPos * tDelta_;
                transform.position = wpos;
            }

            prevT_ = t_;

            yield return null;
        }
    }

    #region Foot Height Adjustment

    protected float footHeightAddLeft = 0f;
    protected float footHeightAddRight = 0f;

    protected float footLiftedThreshold = 0.06f;

    protected Vector3 footNormalLeft = Vector3.up;
    protected Vector3 footNormalRight = Vector3.up;

    protected float footLockBlendLeft = 0f;
    protected float footLockBlendRight = 0f;

    protected bool footLeftIsLocked;
    protected bool footLeftIsUnlocking;
    protected bool footRightIsLocked;
    protected bool footRightIsUnlocking;

    protected Vector3 footLockPositionLeft;
    protected Vector3 footLockPositionRight;

    protected void AdjustFoot(CustomIKManager ikManager, Vector3 floorPosition, int leg_ikIndex, Transform foot_transform, int dynamicProperty_lock_weight, int dynamicProperty_height_adjustment_weight, ref float footLockBlend, ref bool footIsLocked, ref bool footIsUnlocking, ref Vector3 footLockPosition, ref float footHeightAdd, ref Vector3 footNormal)
    {
        if (leg_ikIndex >= 0 && foot_transform != null)
        {
            var worldPos = foot_transform.position;
            var controller = ikManager.GetController(leg_ikIndex);

            if (controller.IsActive)
            {
                worldPos = Vector3.LerpUnclamped(worldPos, controller.Target.position, controller.GetPositionWeight() * controller.GetWeight());
            }

            //float simT = 0.3f;
            float footAnimHeight = animator.GetBoneHeight(foot_transform.name, 0f/*, simT*/);
            bool footIsLifted = footAnimHeight > footLiftedThreshold;

            bool activateController = false;
            float finalBlend = 1f;

            if (footLockBlend > 0.001f)
            {
                activateController = true;
                float lockBlend = dynamicProperty_lock_weight >= 0 ? dynamicAnimationProperties.GetValueUnsafe(dynamicProperty_lock_weight) : 1f;
                finalBlend = lockBlend;

                if (footIsLifted)
                {
                    if (footIsLocked && !footIsUnlocking)
                    {
                        footIsUnlocking = true;
                    }

                    footIsLocked = false;
                    footLockBlend = Mathf.MoveTowards(footLockBlend, 0f, Time.deltaTime * 12f);
                }
                else
                {
                    if (footIsUnlocking)
                    {
                        footLockBlend = Mathf.MoveTowards(footLockBlend, 0f, Time.deltaTime * 12f);
                    }
                    else
                    {
                        footLockBlend = Mathf.MoveTowards(footLockBlend, 1f, Time.deltaTime * 12f); 

                        if (footLockBlend * lockBlend >= 0.95f && !footIsLocked)
                        {
                            footIsLocked = true;
                            footLockPosition = worldPos;
                            footLockBlend = 1f; 
                        }
                    } 
                }

                if (footIsLocked || footIsUnlocking) worldPos = Vector3.LerpUnclamped(worldPos, footLockPosition, footLockBlend * lockBlend);  
            }
            else
            {
                footIsLocked = false;
                footIsUnlocking = false;
            }

            float adjustmentBlend = dynamicProperty_height_adjustment_weight >= 0 ? dynamicAnimationProperties.GetValueUnsafe(dynamicProperty_height_adjustment_weight) : 1f;

            float targetFootHeightAdd = 0f;
            Vector3 targetFootNormal = Vector3.up;

            if (adjustmentBlend > 0f)
            {
                var castOrigin = worldPos + new Vector3(0f, (foot_transform.parent.position - foot_transform.position).y, 0f);
                if (Physics.Raycast(castOrigin, Vector3.down, out RaycastHit hit, groundHeightCastDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
                {
                    targetFootHeightAdd = (hit.point - floorPosition).y;
                    targetFootNormal = hit.normal;

                    /*if (footIsLifted)
                    {
                        castOrigin = castOrigin + (rootVelocityOccum * simT) + (Vector3.up * Mathf.Max(0f, targetFootHeightAdd));
                        if (Physics.Raycast(castOrigin, Vector3.down, out hit, groundHeightCastDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
                        {
                            targetFootHeightAdd = (hit.point - floorPosition).y; 
                            targetFootNormal = hit.normal;
                            Debug.DrawRay(castOrigin, Vector3.down * hit.distance, Color.cyan, 3f);
                        } 
                        else
                        {
                            Debug.DrawRay(castOrigin, Vector3.down, Color.magenta, 3f);
                        }
                    }*/
                }

                footHeightAdd = Mathf.Lerp(footHeightAdd, targetFootHeightAdd, Time.deltaTime * 12f);
                footNormal = Vector3.Lerp(footNormal, targetFootNormal, Time.deltaTime * 12f);  

                if (footHeightAdd != 0f)
                {
                    activateController = true;

                    worldPos = worldPos + (Vector3.up * footHeightAdd) * adjustmentBlend;
                    controller.Target.rotation = Quaternion.SlerpUnclamped(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, footNormal), adjustmentBlend) * controller.Target.rotation;
                    finalBlend = finalBlend * adjustmentBlend;
                }
            }

            if (activateController)
            {
                if (controller.BendGoal != null && controller.ChainLength >= 2)
                {
                    var bendT = controller.GetBone(1);

                    var bendPos = controller.BendGoal.position;
                    var bendOffset = bendPos - bendT.position;
                    bendOffset = bendOffset.normalized;

                    controller.BendGoal.position = bendPos + bendOffset * 10f * finalBlend; 
                }

                controller.SetActive(true);
                controller.SetWeight(1f);
                controller.SetPositionWeight(1f);

                controller.Target.position = worldPos;
            }
        }
    }

    protected void ApplyFootAdjustments(CustomIKManager ikManager)
    {
        Vector3 floorPosition = transform.position; 

        AdjustFoot(ikManager, floorPosition, legL_ikIndex, footL_transform, dynamicProperty_foot_left_lock_weight, dynamicProperty_foot_left_height_adjustment, ref footLockBlendLeft, ref footLeftIsLocked, ref footLeftIsUnlocking, ref footLockPositionLeft, ref footHeightAddLeft, ref footNormalLeft);
        AdjustFoot(ikManager, floorPosition, legR_ikIndex, footR_transform, dynamicProperty_foot_right_lock_weight, dynamicProperty_foot_right_height_adjustment, ref footLockBlendRight, ref footRightIsLocked, ref footRightIsUnlocking, ref footLockPositionRight, ref footHeightAddRight, ref footNormalRight);
    }

    #endregion

    #region IK Snapping

    protected void SnapIkLimb(CustomIKManager ikManager, InteractionPoint snapTarget, int ikIndex, Transform fk_transform, int dynamicProperty_snap_weight, float rotationBlend)
    {
        if (snapTarget == null) return;

        snapTarget.Refresh();
        SnapIkLimb(ikManager, snapTarget.CurrentWorldPosition, snapTarget.GetRotationOffsetWorld(), ikIndex, fk_transform, dynamicProperty_snap_weight, rotationBlend);
    }
    protected void SnapIkLimb(CustomIKManager ikManager, Vector3 targetPosition, Quaternion targetRotationOffset, int ikIndex, Transform fk_transform, int dynamicProperty_snap_weight, float rotationBlend)
    {
        if (ikIndex >= 0 && fk_transform != null)
        {
            var worldPos = fk_transform.position;
            var worldRot = fk_transform.rotation;
            var controller = ikManager.GetController(ikIndex);

            if (controller.IsActive)
            {
                worldPos = Vector3.LerpUnclamped(worldPos, controller.Target.position, controller.GetPositionWeight() * controller.GetWeight());
                worldRot = Quaternion.SlerpUnclamped(worldRot, controller.Target.rotation, controller.GetRotationWeight() * controller.GetWeight());
            }

            float snapBlend = dynamicProperty_snap_weight >= 0 ? dynamicAnimationProperties.GetValueUnsafe(dynamicProperty_snap_weight) : 0f;
            if (snapBlend <= 0f) return;

            if (controller.BendGoal != null && controller.ChainLength >= 2)
            {
                var bendT = controller.GetBone(1);

                var bendPos = controller.BendGoal.position;
                var bendOffset = bendPos - bendT.position;
                bendOffset = bendOffset.normalized;

                controller.BendGoal.position = bendPos + bendOffset * 10f;
            }

            controller.SetActive(true);
            controller.SetWeight(1f);
            controller.SetPositionWeight(1f);
            controller.SetRotationWeight(1f); 

            controller.Target.position = Vector3.LerpUnclamped(worldPos, targetPosition, snapBlend);
            Debug.Log($"{controller.Target.name} -> {snapBlend}");
            Debug.DrawRay(worldPos, Vector3.up * 0.1f, Color.red, 0.05f);
            controller.Target.rotation = Quaternion.LerpUnclamped(Quaternion.identity, targetRotationOffset, snapBlend * rotationBlend) * worldRot; 

        }
    }

    protected InteractionPoint snapTarget_handL;
    protected float snapRotationBlend_handL;
    protected InteractionPoint snapTarget_handL2;
    protected float snapRotationBlend_handL2;

    protected InteractionPoint snapTarget_handR;
    protected float snapRotationBlend_handR;
    protected InteractionPoint snapTarget_handR2;
    protected float snapRotationBlend_handR2;

    protected InteractionPoint snapTarget_footL;
    protected float snapRotationBlend_footL;
    protected InteractionPoint snapTarget_footL2;
    protected float snapRotationBlend_footL2;

    protected InteractionPoint snapTarget_footR;
    protected float snapRotationBlend_footR;
    protected InteractionPoint snapTarget_footR2;
    protected float snapRotationBlend_footR2; 

    protected void ApplyLimbSnapping(CustomIKManager ikManager)
    {
        SnapIkLimb(ikManager, snapTarget_handL, armL_ikIndex, handL_transform, dynamicProperty_snap_point_hand_L, snapRotationBlend_handL);
        SnapIkLimb(ikManager, snapTarget_handL2, armL_ikIndex, handL_transform, dynamicProperty_snap_point_hand_L2, snapRotationBlend_handL2);

        SnapIkLimb(ikManager, snapTarget_handR, armR_ikIndex, handR_transform, dynamicProperty_snap_point_hand_R, snapRotationBlend_handR);
        SnapIkLimb(ikManager, snapTarget_handR2, armR_ikIndex, handR_transform, dynamicProperty_snap_point_hand_R2, snapRotationBlend_handR2);

        SnapIkLimb(ikManager, snapTarget_footL, legL_ikIndex, footL_transform, dynamicProperty_snap_point_foot_L, snapRotationBlend_footL);
        SnapIkLimb(ikManager, snapTarget_footL2, legL_ikIndex, footL_transform, dynamicProperty_snap_point_foot_L2, snapRotationBlend_footL2);

        SnapIkLimb(ikManager, snapTarget_footR, legR_ikIndex, footR_transform, dynamicProperty_snap_point_foot_R, snapRotationBlend_footR);
        SnapIkLimb(ikManager, snapTarget_footR2, legR_ikIndex, footR_transform, dynamicProperty_snap_point_foot_R2, snapRotationBlend_footR2);   
    }

    public void ClearLimbSnapPoints()
    { 
        snapTarget_handL = null;
        snapTarget_handL2 = null;
        snapTarget_handR = null;
        snapTarget_handR2 = null;
        snapRotationBlend_handL = 0f;
        snapRotationBlend_handL2 = 0f;
        snapRotationBlend_handR = 0f;
        snapRotationBlend_handR2 = 0f;

        snapTarget_footL = null;
        snapTarget_footL2 = null;
        snapTarget_footR = null;
        snapTarget_footR2 = null;
        snapRotationBlend_footL = 0f;
        snapRotationBlend_footL2 = 0f;
        snapRotationBlend_footR = 0f;
        snapRotationBlend_footR2 = 0f;
    }

    #endregion

    #endregion

    #region Workouts

    protected int localWorkoutMenuPage;

    [SerializeField]
    protected UIRadialMenu localWorkoutMenu;

    [SerializeField]
    protected WorkoutSystem localWorkoutSystem;

    [SerializeField]
    protected List<WorkoutBase> localWorkouts = new List<WorkoutBase>(); 

    protected IAnimationLayer workoutLayer;
    protected WorkoutSystem activeWorkout;
    protected float prevRepT;
    public bool IsPerformingWorkout => workoutLayer != null && activeWorkout != null;
    protected CustomizableActionCamera.OrbitState preWorkoutCameraState;

    private Dictionary<string, OrbitCameraViewGroup> workoutViewGroups;

    IEnumerator EnterWorkout(WorkoutSystem system, float transitionTime, IAnimationLayer layer, string enterState = "default")
    {
        workoutLayer = layer;
        activeWorkout = system;
        if (workoutLayer == null || activeWorkout == null) yield break;

        if (workoutViewGroups == null) workoutViewGroups = new Dictionary<string, OrbitCameraViewGroup>();
        workoutViewGroups.Clear();

        workoutViewGroups[WorkoutBase.viewGroup_enterWorkout] = activeWorkout.workout.enterWorkoutView;
        workoutViewGroups[WorkoutBase.viewGroup_exitWorkout] = activeWorkout.workout.exitWorkoutView;
        workoutViewGroups[WorkoutBase.viewGroup_rep] = activeWorkout.workout.repView;
        workoutViewGroups[WorkoutBase.viewGroup_rest] = activeWorkout.workout.restView;

        workoutViewGroups[WorkoutBase.viewGroup_enterWorkoutSpotter] = activeWorkout.workout.spotterEnterWorkoutView;
        workoutViewGroups[WorkoutBase.viewGroup_exitWorkoutSpotter] = activeWorkout.workout.spotterExitWorkoutView;
        workoutViewGroups[WorkoutBase.viewGroup_repSpotter] = activeWorkout.workout.spotterRepView;
        workoutViewGroups[WorkoutBase.viewGroup_restSpotter] = activeWorkout.workout.spotterRestView;

        var viewDir = WorkoutBase.WorkoutViewsDirectory;
        if (Directory.Exists(viewDir))
        {
            OrbitCameraViewGroup viewGroup;

            if (OrbitCameraViewGroup.TryLoad(viewDir, WorkoutBase.GetEnterWorkoutViewGroupId(activeWorkout.workout.name), out viewGroup))
            {
                workoutViewGroups[WorkoutBase.viewGroup_enterWorkout] = viewGroup;
            }
            if (OrbitCameraViewGroup.TryLoad(viewDir, WorkoutBase.GetExitWorkoutViewGroupId(activeWorkout.workout.name), out viewGroup))
            {
                workoutViewGroups[WorkoutBase.viewGroup_exitWorkout] = viewGroup;
            }
            if (OrbitCameraViewGroup.TryLoad(viewDir, WorkoutBase.GetRepViewGroupId(activeWorkout.workout.name), out viewGroup))
            {
                workoutViewGroups[WorkoutBase.viewGroup_rep] = viewGroup;
            }
            if (OrbitCameraViewGroup.TryLoad(viewDir, WorkoutBase.GetRestViewGroupId(activeWorkout.workout.name), out viewGroup))
            {
                workoutViewGroups[WorkoutBase.viewGroup_rest] = viewGroup;
            }

            if (OrbitCameraViewGroup.TryLoad(viewDir, WorkoutBase.GetSpotterEnterWorkoutViewGroupId(activeWorkout.workout.name), out viewGroup))
            {
                workoutViewGroups[WorkoutBase.viewGroup_enterWorkoutSpotter] = viewGroup;
            }
            if (OrbitCameraViewGroup.TryLoad(viewDir, WorkoutBase.GetSpotterExitWorkoutViewGroupId(activeWorkout.workout.name), out viewGroup))
            {
                workoutViewGroups[WorkoutBase.viewGroup_exitWorkoutSpotter] = viewGroup;
            }
            if (OrbitCameraViewGroup.TryLoad(viewDir, WorkoutBase.GetSpotterRepViewGroupId(activeWorkout.workout.name), out viewGroup))
            {
                workoutViewGroups[WorkoutBase.viewGroup_repSpotter] = viewGroup;
            }
            if (OrbitCameraViewGroup.TryLoad(viewDir, WorkoutBase.GetSpotterRestViewGroupId(activeWorkout.workout.name), out viewGroup))
            {
                workoutViewGroups[WorkoutBase.viewGroup_restSpotter] = viewGroup;
            }
        }

        int targetStateIndex = layer.FindStateIndex(WorkoutBase.GetEnterWorkoutStateName(enterState));
        if (targetStateIndex < 0) yield break;

        while (!activeWorkout.TryToEnter()) yield return null;

        preWorkoutCameraState = gameCamera.State;
        gameCamera.SetViewGroup(workoutViewGroups[WorkoutBase.viewGroup_enterWorkout], 1.5f);
        gameCamera.RecalculateOriginOnModeSwitch = true;

        layer.SetActive(true);
        layer.SetActiveStateUnsafe(targetStateIndex);
        var targetState = layer.ActiveState;
        if (targetState != null) 
        { 
            targetState.SetNormalizedTime(0f);
            targetState.SetWeight(1f);
        }

        if (transitionTime > 0f)
        {
            float t = 0f;
            while (t < transitionTime)
            {
                t += Time.deltaTime;
                layer.Mix = t / transitionTime;
                yield return null;
            }
        }

        layer.Mix = 1f;
        prevRepT = 0f;
    }
    IEnumerator ExitWorkout(WorkoutSystem workoutSystem, float transitionTime, string exit_state_prefix)
    {
        if (workoutLayer == null) yield break;

        var parameter = workoutLayer.FindParameter(WorkoutBase.workoutParam_exitWorkout);
        if (parameter is not CustomAnimationParameter.Trigger trigger) yield break;

        trigger.Arm();

        var activeState = workoutLayer.ActiveState;
        while(activeState == null || !activeState.Name.StartsWith(exit_state_prefix)) 
        {
            yield return null;
            activeState = workoutLayer.ActiveState;
        }

        gameCamera.RecalculateOriginOnModeSwitch = false;
        gameCamera.TransitionToState(preWorkoutCameraState, 2f); 

        float minTime = activeState.GetEstimatedDuration() - transitionTime;
        while(activeState.GetTime() < minTime) yield return null; 

        if (transitionTime > 0f)
        {
            float t = 0f;
            while (t < transitionTime)
            {
                t += Time.deltaTime;
                workoutLayer.Mix = 1f - (t / transitionTime); 
                yield return null;
            }
        }

        workoutLayer.Mix = 0f;
        workoutLayer.SetActive(false);

        workoutLayer = null;

        if (workoutSystem.workout != null) workoutSystem.workout.RemoveAnimationsFrom(animator); 
        workoutSystem.ClearWorkout();
    }

    public void OpenLocalWorkoutMenu()
    {
        if (localWorkoutMenu == null) return;

        localWorkoutMenu.gameObject.SetActive(true);

        localWorkoutMenu.Clear(false);

        if (localWorkouts != null)
        {
            foreach(var workout in localWorkouts)
            {
                localWorkoutMenu.AddItem(workout.name, workout.DisplayName, workout.DisplayIcon, null, () =>
                {
                    localWorkoutSystem.SetWorkout(workout);
                    var layerName = workout.SetupAnimations(animator);
                    var layer = animator.FindLayer(layerName);
                    StartCoroutine(EnterWorkout(localWorkoutSystem, 0.35f, layer));  

                    CloseLocalWorkoutMenu();

                }, null, false);
            }
        }

        localWorkoutMenu.SetPage(localWorkoutMenuPage, false);
        localWorkoutMenuPage = localWorkoutMenu.Page; 

        localWorkoutMenu.Rebuild();

        localWorkoutMenu.selectOnDeactivate = true;
    }

    public void CloseLocalWorkoutMenu()
    {
        if (localWorkoutMenu == null) return;

        localWorkoutMenu.gameObject.SetActive(false);
    }

    #endregion

    private float prevPelvisWorldY;
    protected void PreIK()
    {
        var ikManager = animator.IkManager;
        if (ikManager == null) return;

        animator.SyncIKFK(false, true, true);

        bool adjustPelvis = maxPelvisHeightAdjustmentDownward > 0f || maxPelvisHeightAdjustmentUpward > 0f;
        Vector3 footPosL = Vector3.zero;
        Vector3 footPosR = Vector3.zero;
        if (adjustPelvis)
        {
            footPosL = ikManager[legL_ikIndex].Target.position;
            footPosR = ikManager[legR_ikIndex].Target.position;
        }

        ApplyFootAdjustments(ikManager);
        ApplyLimbSnapping(ikManager);

        if (adjustPelvis)
        {

            Vector3 newFootPosL = ikManager[legL_ikIndex].Target.position;
            Vector3 newFootPosR = ikManager[legR_ikIndex].Target.position; 

            float offsetL = Vector3.Dot(Vector3.up, newFootPosL - footPosL);
            float offsetR = Vector3.Dot(Vector3.up, newFootPosR - footPosR);
            
            var pelvisPos = pelvis_transform.position;
            float offsetWorld = prevPelvisWorldY - pelvisPos.y;
            prevPelvisWorldY = pelvisPos.y;
            pelvisHeightAdd = pelvisHeightAdd + offsetWorld;

            //float offsetMin = Mathf.Min(offsetL, offsetR);
            float offset = /*Mathf.LerpUnclamped((offsetL + offsetR) * 0.5f, offsetMin, Mathf.Clamp01(Mathf.Pow(-Mathf.Min(-0.0001f, offsetMin), 0.15f)));*/Mathf.Min(offsetL, offsetR);
            float targetHeightAdd = Mathf.Clamp(offset, -maxPelvisHeightAdjustmentDownward, maxPelvisHeightAdjustmentUpward);
            pelvisHeightAdd = Mathf.MoveTowards(pelvisHeightAdd, targetHeightAdd, Time.deltaTime * Mathf.Max(Mathf.Abs(maxPelvisHeightAdjustmentDownward), Mathf.Abs(maxPelvisHeightAdjustmentUpward)) * (targetHeightAdd - pelvisHeightAdd) > 0f ? 0.65f : 0.35f);    
            pelvis_transform.position = pelvisPos + Vector3.up * Mathf.Clamp(pelvisHeightAdd, -maxPelvisHeightAdjustmentDownward, maxPelvisHeightAdjustmentUpward); 
        }
    }

    [Header("Physics"), SerializeField]
    new protected Rigidbody rigidbody;
    public Rigidbody Rigidbody
    {
        get
        {
            if (rigidbody == null)
            {
                rigidbody = Extensions.AddOrGetComponent<Rigidbody>(gameObject);
            }

            return rigidbody;
        }
    }

    public LayerMask groundLayerMask = ~0;

    public void ApplyVelocityToRoot(Vector3 velocity)
    {
        if (Rigidbody == null) return;

        rigidbody.velocity = velocity;
    }
    public void ApplyAngularVelocityToRoot(Vector3 velocity)
    {
        if (Rigidbody == null) return;

        rigidbody.angularVelocity = velocity;
    }

    public Transform handL_transform;
    public string armL_ikName;
    protected int armL_ikIndex = -1;
    public Transform handR_transform;
    public string armR_ikName;
    protected int armR_ikIndex = -1;

    public Transform pelvis_transform;
    public Transform footL_transform;
    public string legL_ikName;
    protected int legL_ikIndex = -1;
    public Transform footR_transform;
    public string legR_ikName;
    protected int legR_ikIndex = -1;

    public float maxPelvisHeightAdjustmentUpward = 0.1f;
    public float maxPelvisHeightAdjustmentDownward = 0.5f;
    private float pelvisHeightAdd;
    
    public float footHeightOffset = 0.05f;
    private Vector3 footHeightOffsetVec;
    public float footSoleHeightOffset = 0.02f;
    private Vector3 footSoleHeightOffsetVec;
    public float groundHeightCastDistance = 100f;
    private float distanceFromGround;
    public float DistanceFromGround => distanceFromGround;
    private float footDistanceFromGround;
    public float FootDistanceFromGround => footDistanceFromGround;
    public float CalculateDistanceFromGround()
    {
        distanceFromGround = groundHeightCastDistance;

        var centerT = pelvis_transform;
        if (centerT == null) return distanceFromGround;

        if (Physics.Raycast(centerT.position, Vector3.down, out RaycastHit hit, groundHeightCastDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            distanceFromGround = hit.distance;
#if UNITY_EDITOR
            //Debug.DrawRay(centerT.position, Vector3.down * hit.distance, Color.red, 1f);
#endif
        }
#if UNITY_EDITOR
        else
        {
            //Debug.DrawRay(centerT.position, Vector3.down * groundHeightCastDistance, Color.green, 1f);
        }
#endif

        return distanceFromGround;
    }
    public float CalculateFootDistanceFromGround() => CalculateFootDistanceFromGround(out _, out _);
    public float CalculateFootDistanceFromGround(out Vector3 footL_position, out Vector3 footR_position)
    {
        footL_position = footR_position = Vector3.zero;

        float soleOffset = footSoleHeightOffset - footHeightOffset;
        footDistanceFromGround = groundHeightCastDistance;
        if (footL_transform != null)
        {
            footL_position = footL_transform.position + footHeightOffsetVec;
            if (Physics.Raycast(footL_position, Vector3.down, out RaycastHit hit, groundHeightCastDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
            {
                footDistanceFromGround = hit.distance - soleOffset;
#if UNITY_EDITOR
                //Debug.DrawRay(footL_position, Vector3.down * (hit.distance - soleOffset), Color.red, 1f);
#endif
            }
#if UNITY_EDITOR
            else
            {
                //Debug.DrawRay(footL_position, Vector3.down * groundHeightCastDistance, Color.green, 1f);
            }
#endif
        }
        if (footR_transform != null)
        {
            footR_position = footR_transform.position + footHeightOffsetVec;
            if (Physics.Raycast(footR_position, Vector3.down, out RaycastHit hit, groundHeightCastDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
            {
                footDistanceFromGround = math.min(footDistanceFromGround, hit.distance - soleOffset);
#if UNITY_EDITOR
                //Debug.DrawRay(footR_position, Vector3.down * (hit.distance - soleOffset), Color.red, 1f);
#endif
            }
#if UNITY_EDITOR
            else
            {
                //Debug.DrawRay(footR_position, Vector3.down * groundHeightCastDistance, Color.green, 1f); 
            }
#endif
        }

        return footDistanceFromGround;
    }

    [Header("Animations")]
    public CustomAnimator animator;
    public CustomAnimator Animator => animator;

    public DynamicAnimationProperties dynamicAnimationProperties;

    public AnimationCurve moveAngleMultiplierCurve = null;
    public float CalculateMoveAngle(Vector3 char_forward, Vector3 camMapped_moveInput)
    {
        var angle = Vector3.SignedAngle(char_forward, camMapped_moveInput, Vector3.up);

        if (moveAngleMultiplierCurve != null && moveAngleMultiplierCurve.length > 0)
        {
            float normalizedAngle = Mathf.InverseLerp(0f, 180f, Mathf.Abs(angle));
            float multiplier = moveAngleMultiplierCurve.Evaluate(normalizedAngle) + 1f;
            angle *= multiplier;
        }

        return angle;
    }

    public float walk_anim_speed = 1f;
    public float jog_anim_speed = 1.1f;
    public float run_anim_speed = 1.45f;
    public float sprint_anim_speed = 1.7f;
    public float sprint_step_spacing = 0.5f;
    public float sprint_step_offset = 0.07f;
    public float sprint_step_window = 0.4f;
    public string sprint_step_reference_controller;
    private CustomMotionController sprintStepReferenceController;
    private float time_since_last_sprint_step = 0f;

    public string autoCreateAnimReferencePrefix = "ar_";

    public string animParameter_taunt = "taunt";

    [Tooltip("The gradually changing normalized angle (-1 to 1) between the character's movement direction and the input movement direction. (On the XZ plane)")]
    public string animParameter_turn = "turn";
    [Tooltip("The normalized angle (-1 to 1) between the character's movement direction and the input movement direction. (On the XZ plane)")]
    public string animParameter_turn_want = "turn_want";
    [Tooltip("The magnitude of the character's last movement vector.")]
    public string animParameter_move = "move";
    [Tooltip("The magnitude of the movement input.")]
    public string animParameter_move_want = "move_want";
    [Tooltip("A parameter that gradually syncs to the magnitude of the movement input.")]
    public string animParameter_move_want_grad = "move_want_grad";
    [Tooltip("The X value of the movement input.")]
    public string animParameter_move_wantX = "move_wantX";
    [Tooltip("The Y value of the movement input.")]
    public string animParameter_move_wantY = "move_wantY";
    [Tooltip("The sign of the X value of the movement input.")]
    public string animParameter_move_wantX_sign = "move_wantX_sign";
    [Tooltip("The signed angle between the character's forward axis and the move input vector mapped to the camera transform.")]
    public string animParameter_move_want_angle = "move_want_angle";
    [Tooltip("If true prevents main & bumper colliders from moving and changing size.")]
    public string animParameter_lock_colliders = "lock_colliders";
    [Tooltip("Locks the character direction from being recalculated when true. Useful for preserving character direction for certain calculations that happen over time.")]
    public string animParameter_lock_characterDir = "lock_characterDir";
    [Tooltip("Used to blend between walking/jogging/running etc. If using controller, varies based on position of analog stick.")]
    public string animParameter_move_speed = "move_speed";
    [Tooltip("Used to control the speed of walking/running animations.")]
    public string animParameter_move_anim_speed = "move_anim_speed";
    [Tooltip("Used to control the speed of redirection animations based on the degree of the move angle.")]
    public string animParameter_redirect_speed = "redirect_speed";
    [Tooltip("A multiplier for the amount this script can contribute to facing the input direction.")]
    public string animParameter_script_redirect_weight = "script_turn_weight";



    [Tooltip("Ratio of current occumulated root velocity that will then get applied to active rigidbodies. Resets to zero after being applied.")]
    public string animParameter_apply_root_velocity = "apply_root_velocity";
    [Tooltip("Ratio of current occumulated root angular velocity that will then get applied to active rigidbodies. Resets to zero after being applied.")]
    public string animParameter_apply_root_angularVelocity = "apply_root_angularVelocity";


    [Tooltip("Ratio of current root velocity to save.")]
    public string animParameter_save_root_velocity = "save_root_velocity";
    [Tooltip("Ratio of current root angular velocity to save.")]
    public string animParameter_save_root_angularVelocity = "save_root_angularVelocity";
    private Vector3 savedRootVelocity;
    private Vector3 savedRootAngularVelocity;
    public void SaveRootVelocity(float ratio = 1f)
    {
        if (Rigidbody == null) return;
        savedRootVelocity = rigidbody.velocity * ratio;
    }
    public void SaveRootAngularVelocity(float ratio = 1f)
    {
        if (Rigidbody == null) return;
        savedRootAngularVelocity = rigidbody.angularVelocity * ratio;
    }
    [Tooltip("Ratio of saved root velocity that will then get applied to the root rigidbody. Resets to zero after being applied.")]
    public string animParameter_apply_saved_root_velocity_to_root = "apply_saved_root_velocity_to_root";
    [Tooltip("Ratio of saved root angular velocity that will then get applied to the root rigidbody. Resets to zero after being applied.")]
    public string animParameter_apply_saved_root_angularVelocity_to_root = "apply_saved_root_angularVelocity_to_root";
    public void ApplySavedRootVelocityToRoot(float ratio = 1f) => ApplyVelocityToRoot(savedRootVelocity * ratio);
    public void ApplySavedRootAngularVelocityToRoot(float ratio = 1f) => ApplyAngularVelocityToRoot(savedRootAngularVelocity * ratio);

    [Tooltip("Apply a velocity impulse along the local X axis.")]
    public string animParameter_velocity_impulse_x = "velocity_impulse_x";
    [Tooltip("Apply a velocity impulse along the local Y axis.")]
    public string animParameter_velocity_impulse_y = "velocity_impulse_y";
    [Tooltip("Apply a velocity impulse along the local Z axis.")]
    public string animParameter_velocity_impulse_z = "velocity_impulse_z";
    [Tooltip("Apply an angular velocity impulse around the local X axis.")]
    public string animParameter_angularVelocity_impulse_x = "angularVelocity_impulse_x";
    [Tooltip("Apply an angular velocity impulse around the local Y axis.")]
    public string animParameter_angularVelocity_impulse_y = "angularVelocity_impulse_y";
    [Tooltip("Apply an angular velocity impulse around the local Z axis.")]
    public string animParameter_angularVelocity_impulse_z = "angularVelocity_impulse_z";

    [Tooltip("True if the root body or root muscle is colliding with the ground.")]
    public string animParameter_is_grounded = "is_grounded";
    [Tooltip("Amount of time the character has been in the air.")]
    public string animParameter_air_time = "air_time";
    [Tooltip("Used to indicate to animation transitions that the character is in range to land on the ground.")]
    public string animParameter_can_land = "can_land";
    [Tooltip("Length of the ray used to determine if the character can land on the ground.")]
    public string animParameter_ray_length_landing = "ray_length_landing";
    [Tooltip("Distance from the character's center (usually pelvis) to the ground.")]
    public string animParameter_ground_distance = "ground_distance";
    [Tooltip("Distance from the character's feet to the ground.")]
    public string animParameter_foot_ground_distance = "foot_ground_distance";
    [Tooltip("Gradually calculated distance from the character's center (usually pelvis) to the ground.")]
    public string animParameter_ground_distance_grad = "ground_distance_grad";
    [Tooltip("Gradually calculated distance from the character's feet to the ground.")]
    public string animParameter_foot_ground_distance_grad = "foot_ground_distance_grad";
    [Tooltip("Trigger to forcibly snap the character's feet to the ground.")]
    public string animParameter_ground_feet = "ground_feet";

    [Tooltip("How much of the current vertical root velocity to nullify.")]
    public string animParameter_root_force_kill_vertical = "root_force_kill_vertical";
    [Tooltip("How much of the current planar root velocity to nullify.")]
    public string animParameter_root_force_kill_planar = "root_force_kill_planar";
    [Tooltip("How much of the current angular root velocity to nullify.")]
    public string animParameter_root_force_kill_angular = "root_force_kill_angular";

    [Tooltip("The current speed of the character along the local z axis. Positive is forward, negative is backward.")]
    public string animParameter_speed_forward = "speed_forward";
    [Tooltip("The current speed of the character along the local x axis. Positive is right, negative is left.")]
    public string animParameter_speed_sideways = "speed_sideways";
    [Tooltip("The current speed of the character along the local y axis. Positive is up, negative is down.")]
    public string animParameter_speed_vertical = "speed_vertical";


    [Tooltip("The gradual speed of the character along the local z axis. Positive is forward, negative is backward.")]
    public string animParameter_speed_forward_grad = "speed_forward_grad";
    [Tooltip("The gradual speed of the character along the local x axis. Positive is right, negative is left.")]
    public string animParameter_speed_sideways_grad = "speed_sideways_grad";
    [Tooltip("The gradual speed of the character along the local y axis. Positive is up, negative is down.")]
    public string animParameter_speed_vertical_grad = "speed_vertical_grad";


    public string animParameter_sprint = "sprint";

    public string animParameter_jump = "jump";
    protected float jump_reset_timer;
    [Tooltip("How long before the armed jump trigger is reset after pressing jump.")]
    public float jump_reset_time = 0.2f;

    public string animParameter_roll = "roll";
    public string animParameter_turtle = "turtle";

    public string animParameter_disable_gravity = "disable_gravity";

    private int dynamicProperty_disable_gravity = -1;

    public string animParameter_foot_left_height_adjustment = "foot_left_height_adjustment_weight";
    public string animParameter_foot_right_height_adjustment = "foot_right_height_adjustment_weight"; 

    private int dynamicProperty_foot_left_height_adjustment = -1;
    private int dynamicProperty_foot_right_height_adjustment = -1;

    public string animParameter_foot_left_lock_weight = "foot_left_lock_weight";
    public string animParameter_foot_right_lock_weight = "foot_right_lock_weight";

    private int dynamicProperty_foot_left_lock_weight = -1;
    private int dynamicProperty_foot_right_lock_weight = -1;

    public string animParameter_interaction_time_pos = "interactionTimePos";
    private int dynamicProperty_interaction_time_pos = -1;


    public string animParameter_snap_point_hand_L = "snapPointHandL";
    private int dynamicProperty_snap_point_hand_L = -1;
    public string animParameter_snap_point_hand_L2 = "snapPointHandL2";
    private int dynamicProperty_snap_point_hand_L2 = -1;

    public string animParameter_snap_point_hand_R = "snapPointHandR";
    private int dynamicProperty_snap_point_hand_R = -1;
    public string animParameter_snap_point_hand_R2 = "snapPointHandR2";
    private int dynamicProperty_snap_point_hand_R2 = -1;


    public string animParameter_snap_point_foot_L = "snapPointFootL";
    private int dynamicProperty_snap_point_foot_L = -1;
    public string animParameter_snap_point_foot_L2 = "snapPointFootL2";
    private int dynamicProperty_snap_point_foot_L2 = -1;

    public string animParameter_snap_point_foot_R = "snapPointFootR";
    private int dynamicProperty_snap_point_foot_R = -1;
    public string animParameter_snap_point_foot_R2 = "snapPointFootR2";
    private int dynamicProperty_snap_point_foot_R2 = -1;

    private void InitializeDynamicAnimationProperties()
    {
        if (dynamicAnimationProperties == null) dynamicAnimationProperties = gameObject.GetComponent<DynamicAnimationProperties>();
        if (dynamicAnimationProperties == null) return;

        if (!dynamicAnimationProperties.IsInitialized) dynamicAnimationProperties.Initialize();

        dynamicProperty_disable_gravity = dynamicAnimationProperties.IndexOf(animParameter_disable_gravity);

        dynamicProperty_foot_left_height_adjustment = dynamicAnimationProperties.IndexOf(animParameter_foot_left_height_adjustment);
        dynamicProperty_foot_right_height_adjustment = dynamicAnimationProperties.IndexOf(animParameter_foot_right_height_adjustment);

        dynamicProperty_foot_left_lock_weight = dynamicAnimationProperties.IndexOf(animParameter_foot_left_lock_weight);
        dynamicProperty_foot_right_lock_weight = dynamicAnimationProperties.IndexOf(animParameter_foot_right_lock_weight);

        dynamicProperty_interaction_time_pos = dynamicAnimationProperties.IndexOf(animParameter_interaction_time_pos);


        dynamicProperty_snap_point_hand_L = dynamicAnimationProperties.IndexOf(animParameter_snap_point_hand_L);
        dynamicProperty_snap_point_hand_L2 = dynamicAnimationProperties.IndexOf(animParameter_snap_point_hand_L2);

        dynamicProperty_snap_point_hand_R = dynamicAnimationProperties.IndexOf(animParameter_snap_point_hand_R);
        dynamicProperty_snap_point_hand_R2 = dynamicAnimationProperties.IndexOf(animParameter_snap_point_hand_R2);

        dynamicProperty_snap_point_foot_L = dynamicAnimationProperties.IndexOf(animParameter_snap_point_foot_L);
        dynamicProperty_snap_point_foot_L2 = dynamicAnimationProperties.IndexOf(animParameter_snap_point_foot_L2);

        dynamicProperty_snap_point_foot_R = dynamicAnimationProperties.IndexOf(animParameter_snap_point_foot_R);
        dynamicProperty_snap_point_foot_R2 = dynamicAnimationProperties.IndexOf(animParameter_snap_point_foot_R2); 
    }

    public List<AnimationAsset> animations;
    public AnimationAsset FindAnimationAsset(string name)
    {
        if (animations == null) return null;

        foreach (var anim in animations)
        {
            if (anim == null) continue;

            if (anim.name == name) return anim;
        }

        return null;
    }

    public List<Layer> layers = new List<Layer>();
    public List<LayerStateTransitionCreator> stateTransitions;
    public LayerStateTransitionCreator FindTransitionCreator(string name)
    {
        if (stateTransitions == null) return null;

        foreach (var creator in stateTransitions)
        {
            if (creator == null || creator.name != name) continue;
            return creator;
        }

        return null;
    }

    [NonSerialized]
    private CustomAnimationController controller;
    public CustomAnimationController AnimationController => controller;

    public List<CustomAnimationParameter.Float> customFloatParameters;
    public List<CustomAnimationParameter.Boolean> customBoolParameters;
    public List<CustomAnimationParameter.Trigger> customTriggerParameters;

    [Header("Physics")]
    public CapsuleCollider mainCollider;
    public CapsuleCollider bumperCollider;

    [Range(0, 1)]
    public float minColliderScalingHeightRatio = 0.025f;
    [Range(0, 1)]
    public float colliderRadiusScaling = 0.08f;
    public float colliderScalingSpeed = 2f;
    public float mainColliderScallingSpeedMul = 1.5f;

    [Serializable]
    public struct HeightContributor
    {
        public Transform transform;
        public Vector3 localOffset;

        public Vector3 Position => transform.TransformPoint(localOffset);
    }
    public List<HeightContributor> heightContributors;


    private float startHeightTop;
    private float startHeightBottom;
    private float startHeightTotal;
    private TransformState startTransformMainCollider;
    private TransformState startTransformBumperCollider;
    private float startHeightMainCollider;
    private float startHeightBumperCollider;
    private float startRadiusMainCollider;
    private float startRadiusBumperCollider;
    private float startTotalHeightMainCollider;
    private float startTotalHeightBumperCollider;

    protected Vector2 GetHeightExtentsFromContributors(List<HeightContributor> contributors)
    {
        Vector2 heights = Vector2.zero;

        if (contributors != null && contributors.Count > 0)
        {
            var pos = transform.InverseTransformPoint(contributors[0].Position);
            heights = new Vector2(pos.y, pos.y);
            for (int a = 1; a < contributors.Count; a++)
            {
                pos = transform.InverseTransformPoint(contributors[a].Position);
                heights.x = Mathf.Min(heights.x, pos.y);
                heights.y = Mathf.Max(heights.y, pos.y);
            }
        }

        heights.x = 0f; // TODO: rework this

        return heights;
    }
    public Vector2 GetHeightExtents() => GetHeightExtentsFromContributors(heightContributors);

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            InitializeColliders();
        }
    }
#endif

    public void InitializeColliders()
    {
        if (mainCollider == null)
        {
            mainCollider = new GameObject("colliderMain").AddComponent<CapsuleCollider>();
            mainCollider.height = 1.8f;
            mainCollider.radius = 0.3f;

            mainCollider.transform.SetParent(transform, false);
            mainCollider.transform.position = transform.TransformPoint(new Vector3(0, mainCollider.height / 2f, 0));
        }
        if (bumperCollider == null)
        {
            bumperCollider = new GameObject("colliderBumper").AddComponent<CapsuleCollider>();
            bumperCollider.height = 1f;
            bumperCollider.radius = 0.45f;

            bumperCollider.transform.SetParent(transform, false);
            bumperCollider.transform.position = transform.TransformPoint(new Vector3(0, bumperCollider.height / 2f, 0));
        }

#if UNITY_EDITOR
        if (Application.isPlaying)
        {
#endif
            mainCollider.transform.position = mainCollider.transform.TransformPoint(mainCollider.center);
            mainCollider.center = Vector3.zero;

            bumperCollider.transform.position = bumperCollider.transform.TransformPoint(bumperCollider.center);
            bumperCollider.center = Vector3.zero;

            var heightExtents = GetHeightExtents();
            startHeightBottom = heightExtents.x;
            startHeightTop = heightExtents.y;
            startHeightTotal = startHeightTop - startHeightBottom;

            startHeightMainCollider = mainCollider.height;
            startHeightBumperCollider = bumperCollider.height;

            startRadiusMainCollider = mainCollider.radius;
            startRadiusBumperCollider = bumperCollider.radius;

            startTotalHeightMainCollider = startHeightMainCollider + startRadiusMainCollider;
            startTotalHeightBumperCollider = startHeightBumperCollider + startRadiusBumperCollider;

            startTransformMainCollider = new TransformState()
            {
                localScale = mainCollider.transform.localScale,
                position = transform.InverseTransformPoint(mainCollider.transform.position),
                rotation = mainCollider.transform.localRotation
            };
            startTransformBumperCollider = new TransformState()
            {
                localScale = bumperCollider.transform.localScale,
                position = transform.InverseTransformPoint(bumperCollider.transform.position),
                rotation = bumperCollider.transform.localRotation
            };
#if UNITY_EDITOR
        }
#endif
    }

    private Vector2 currentHeightExtents;
    private readonly List<Vector3> heightContributorPositions = new List<Vector3>();
    public Vector3 CalculateBottomPosition()
    {
        var t = bumperCollider.transform;
        Vector3 heightOffset = (bumperCollider.direction == 0 ? Vector3.right : bumperCollider.direction == 1 ? Vector3.up : Vector3.forward) * ((bumperCollider.height * 0.5f) + bumperCollider.radius);
        return t.TransformPoint(bumperCollider.center - heightOffset);
    }
    public void RefreshColliders(bool adjustTransformHeight, float deltaTime)
    {
        var p = animator.FindParameter(animParameter_lock_colliders);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_lock_colliders}");
        }
        else if (p is CustomAnimationParameter.Boolean bp)
        {
            if (bp.IsTrue) return;
        }

        adjustTransformHeight = adjustTransformHeight && (rootIsColliding > 0 || rigidbody.SweepTestExtended(Vector3.down, out _, 0.02f, groundLayerMask, QueryTriggerInteraction.Ignore));
        Vector3 origBotPos = adjustTransformHeight ? CalculateBottomPosition() : Vector3.zero;

        currentHeightExtents = GetHeightExtents();

        float totalHeightCurrent = currentHeightExtents.y - currentHeightExtents.x;
        float heightDiff = totalHeightCurrent - startHeightTotal;
        float heightDiffRatio = totalHeightCurrent / startHeightTotal;
        //float scaling = (Mathf.Max(0, Mathf.Abs(heightDiff - 1f) - minColliderScalingHeightRatio) / (1f - minColliderScalingHeightRatio));
        float signedScaling = heightDiffRatio - 1f;//scaling * Mathf.Sign(heightDiff - 1f);
        float adjustedHeightDiffRatio = signedScaling + 1f;

        var mainColliderTargetHeight = Mathf.Clamp(Mathf.Abs(startTotalHeightMainCollider * adjustedHeightDiffRatio), 0.1f, totalHeightCurrent);
        var bumperColliderTargetHeight = Mathf.Clamp(Mathf.Abs(startTotalHeightBumperCollider * adjustedHeightDiffRatio), 0.1f, totalHeightCurrent);

        //float radiusDelta = heightDiff * colliderRadiusScaling * (adjustedHeightDiffRatio / heightDiffRatio);
        var mainColliderTargetRadius = startRadiusMainCollider;//Mathf.Clamp(startRadiusMainCollider + radiusDelta, 0.01f, mainColliderTargetHeight * 0.5f);
        var bumperColliderTargetRadius = startRadiusBumperCollider;//Mathf.Clamp(startRadiusBumperCollider + radiusDelta, 0.01f, bumperColliderTargetHeight * 0.5f);

        mainColliderTargetHeight = Mathf.Max(0f, mainColliderTargetHeight - mainColliderTargetRadius * 2f);
        bumperColliderTargetHeight = Mathf.Max(0f, bumperColliderTargetHeight - bumperColliderTargetRadius * 2f);

        var mainColliderLocalPos = new Vector3(
            startTransformMainCollider.position.x,
            (currentHeightExtents.y - mainColliderTargetHeight * 0.5f),// + ((startTransformMainCollider.position.y + startTotalHeightMainCollider * 0.5f) - startHeightTop),
            startTransformMainCollider.position.z);
        var bumperColliderLocalPos = new Vector3(
            startTransformBumperCollider.position.x,
            (currentHeightExtents.x + bumperColliderTargetHeight * 0.5f),// + ((startTransformMainCollider.position.y - startTotalHeightBumperCollider * 0.5f) - startHeightBottom), 
            startTransformBumperCollider.position.z);

        if (colliderScalingSpeed > 0f)
        {
            var mainColliderLocalPosCurrent = transform.InverseTransformPoint(mainCollider.transform.position);
            var bumperColliderLocalPosCurrent = transform.InverseTransformPoint(bumperCollider.transform.position);

            float scalingSpeed = deltaTime * colliderScalingSpeed;
            float scalingSpeedMain = scalingSpeed * mainColliderScallingSpeedMul;
            mainColliderLocalPos = Vector3.MoveTowards(mainColliderLocalPosCurrent, mainColliderLocalPos, scalingSpeedMain);
            bumperColliderLocalPos = Vector3.MoveTowards(bumperColliderLocalPosCurrent, bumperColliderLocalPos, scalingSpeed);

            mainColliderTargetHeight = Mathf.MoveTowards(mainCollider.height, mainColliderTargetHeight, scalingSpeedMain);
            bumperColliderTargetHeight = Mathf.MoveTowards(bumperCollider.height, bumperColliderTargetHeight, scalingSpeed);

            //mainColliderTargetRadius = Mathf.MoveTowards(mainCollider.radius, mainColliderTargetRadius, scalingSpeedMain);
            //bumperColliderTargetRadius = Mathf.MoveTowards(bumperCollider.radius, bumperColliderTargetRadius, scalingSpeed);
        }

        /*mainCollider.transform.position = transform.TransformPoint(mainColliderLocalPos);
        bumperCollider.transform.position = transform.TransformPoint(bumperColliderLocalPos);

        mainCollider.height = mainColliderTargetHeight;
        bumperCollider.height = bumperColliderTargetHeight;*/

        //mainCollider.radius = mainColliderTargetRadius;
        //bumperCollider.radius = bumperColliderTargetRadius;

        /*if (adjustTransformHeight)
        {
            Vector3 newBotPos = CalculateBottomPosition();
            Vector3 offset = origBotPos - newBotPos;

            if (Vector3.Dot(offset, Vector3.up) > 0f)
            {
                rigidbody.MovePosition(rigidbody.position + (offset + Vector3.up * 0.0001f));
            }

            var velocity = rigidbody.velocity;
            if (velocity.y < 0f) velocity.y = 0f;
            rigidbody.velocity = velocity;
        }*/
    }

    [Header("Control")]
    public float turnAdjustmentSpeedDegrees = 35f;
    public float turnAdjustmentMaxAngle = 50f;
    public AnimationCurve turnAdjustmentInfluenceCurve;

    [Tooltip("How much rotation applied by animations can be nullified based on character's alignment with the current move input."), Range(0f, 1f)]
    public float animTurnAlignmentNerf = 0.5f;
    protected float moveAlignment;
    protected float animTurnNerf;

    public float redirectSpeed180 = 0.9f;

    public float testTurnSpeed = 2f;
    [Range(-1, 1)]
    public float testTurn = 0;
    [Range(-1, 1)]
    public float testSpeed = 0;

    protected virtual void Awake()
    {
        footHeightOffsetVec = Vector3.down * footHeightOffset;
        footSoleHeightOffsetVec = Vector3.down * footSoleHeightOffset;

        if (animator == null) animator = gameObject.GetComponent<CustomAnimator>();
        if (animator != null) 
        { 
            animator.AddRootMotionListener(OverrideRootMotion);

            var ikManager = animator.IkManager;
            if (ikManager != null)
            {
                ikManager.PreLateUpdate += PreIK;

                armL_ikIndex = ikManager.FindIKControllerIndex(armL_ikName);
                armR_ikIndex = ikManager.FindIKControllerIndex(armR_ikName); 

                legL_ikIndex = ikManager.FindIKControllerIndex(legL_ikName);
                legR_ikIndex = ikManager.FindIKControllerIndex(legR_ikName); 
            }
        }

        if (sandboxCharacter == null) sandboxCharacter = gameObject.GetComponent<SandboxCharacter>();
        if (sandboxCharacter != null)
        {
            SetSandboxCharacter(sandboxCharacter);
        }

        if (gameCamera == null) gameCamera = GameObject.FindFirstObjectByType<BulkOutCamera>();
        SetBulkOutCamera(gameCamera); 

        if (localWorkoutSystem == null) localWorkoutSystem = gameObject.GetComponent<WorkoutSystem>(); 

        InitializeColliders();
    }

    private static readonly List<AnimationParameterIdentifier> temp_parameters = new List<AnimationParameterIdentifier>();

    private static readonly List<CustomAnimationParameter.Float> temp_floatParameters = new List<CustomAnimationParameter.Float>();
    private static readonly List<CustomAnimationParameter.Boolean> temp_boolParameters = new List<CustomAnimationParameter.Boolean>();
    private static readonly List<CustomAnimationParameter.Trigger> temp_triggerParameters = new List<CustomAnimationParameter.Trigger>();

    private static readonly List<CustomAnimationLayer> temp_animationLayers = new List<CustomAnimationLayer>();
    private static readonly List<CustomAnimationLayerState> temp_layerStates = new List<CustomAnimationLayerState>();
    private static readonly List<MotionControllerIdentifier> temp_motionControllerIdentifiers = new List<MotionControllerIdentifier>();
    private static readonly List<AnimationReference> temp_animationReferences = new List<AnimationReference>();
    private static readonly List<BlendTree1D> temp_blendTrees1D = new List<BlendTree1D>();
    private static readonly List<BlendTree2D> temp_blendTrees2D = new List<BlendTree2D>();
    private static readonly List<MotionComposite> temp_motionComposites = new List<MotionComposite>();
    private static void ClearTempData()
    {
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
    protected virtual void Start()
    {
        if (animations != null)
        {
            foreach (var animAsset in animations)
            {
                if (animAsset == null || !animAsset.calculateFootHeights) continue;

                var asset_ = animAsset.Asset;
                if (asset_ == null) continue;

                float animLength = asset_.LengthInSeconds;

                int samplesPerSecond = animAsset.footHeightSamplesPerSecond <= 0 ? 16 : animAsset.footHeightSamplesPerSecond;
                animAsset.footHeightDebugCurveL = asset_.GetBoneHeightCurve(footL_transform, samplesPerSecond, animAsset.footHeightReferenceTime * animLength); 
                animAsset.footHeightDebugCurveR = asset_.GetBoneHeightCurve(footR_transform, animAsset.useSeperateRightFootHeightSamplesPerSecond ? animAsset.rightFootHeightSamplesPerSecond : samplesPerSecond, animAsset.useSeperateRightFootHeightReferenceTime ? (animAsset.rightFootHeightReferenceTime * animLength) : (animAsset.footHeightReferenceTime * animLength));
            }
        }

        InitializeDynamicAnimationProperties();

        if (animator != null)
        {
            if (layers != null)
            {
                ClearTempData();

                void AddFloatParameter(CustomAnimationParameter.Float p)
                {
                    temp_parameters.Add(new AnimationParameterIdentifier(AnimationParameterValueType.Float, temp_floatParameters.Count));
                    temp_floatParameters.Add(p);
                }
                void AddBoolParameter(CustomAnimationParameter.Boolean p)
                {
                    temp_parameters.Add(new AnimationParameterIdentifier(AnimationParameterValueType.Boolean, temp_boolParameters.Count));
                    temp_boolParameters.Add(p);
                }
                void AddTriggerParameter(CustomAnimationParameter.Trigger p)
                {
                    temp_parameters.Add(new AnimationParameterIdentifier(AnimationParameterValueType.Trigger, temp_triggerParameters.Count));
                    temp_triggerParameters.Add(p);
                }

                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_turn, 0));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_turn_want, 0));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_move, 0));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_turtle, 0));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_move_want, 0));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_move_want_grad, 0));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_move_wantX, 0));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_move_wantY, 0));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_move_wantX_sign, 1f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_move_want_angle, 0));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_move_speed, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_move_anim_speed, 1f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_redirect_speed, 1f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_script_redirect_weight, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_ray_length_landing, 1f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_ground_distance, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_foot_ground_distance, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_ground_distance_grad, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_foot_ground_distance_grad, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_air_time, 0f));


                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_root_force_kill_planar, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_root_force_kill_vertical, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_root_force_kill_angular, 0f));


                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_apply_root_velocity, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_apply_root_angularVelocity, 0f));




                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_save_root_velocity, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_save_root_angularVelocity, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_apply_saved_root_velocity_to_root, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_apply_saved_root_angularVelocity_to_root, 0f));




                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_velocity_impulse_x, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_velocity_impulse_y, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_velocity_impulse_z, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_angularVelocity_impulse_x, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_angularVelocity_impulse_y, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_angularVelocity_impulse_z, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_speed_forward, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_speed_sideways, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_speed_vertical, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_speed_forward_grad, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_speed_sideways_grad, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_speed_vertical_grad, 0f));

                AddBoolParameter(new CustomAnimationParameter.Boolean(animParameter_lock_colliders, false));
                AddBoolParameter(new CustomAnimationParameter.Boolean(animParameter_lock_characterDir, false));
                AddBoolParameter(new CustomAnimationParameter.Boolean(animParameter_disable_gravity, false));
                AddBoolParameter(new CustomAnimationParameter.Boolean(animParameter_is_grounded, false));
                AddBoolParameter(new CustomAnimationParameter.Boolean(animParameter_can_land, true));


                AddTriggerParameter(new CustomAnimationParameter.Trigger(animParameter_roll));
                AddTriggerParameter(new CustomAnimationParameter.Trigger(animParameter_jump));
                AddTriggerParameter(new CustomAnimationParameter.Trigger(animParameter_ground_feet));

                AddTriggerParameter(new CustomAnimationParameter.Trigger(animParameter_taunt));

                if (customFloatParameters != null)
                {
                    foreach (var p in customFloatParameters) AddFloatParameter(p);
                }
                if (customBoolParameters != null)
                {
                    foreach (var p in customBoolParameters) AddBoolParameter(p);
                }
                if (customTriggerParameters != null)
                {
                    foreach (var p in customTriggerParameters) AddTriggerParameter(p);
                }

                controller = ScriptableObject.CreateInstance<CustomAnimationController>();
                controller.name = "default_bulk_out_character";

                controller.parameters = temp_parameters.ToArray();
                controller.floatParameters = temp_floatParameters.ToArray();
                controller.boolParameters = temp_boolParameters.ToArray();
                controller.triggerParameters = temp_triggerParameters.ToArray();

                int FindParamterIndex(string parameterName)
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

                for (int l = 0; l < layers.Count; l++)
                {
                    var layer = layers[l];

                    temp_layerStates.Clear();
                    temp_motionControllerIdentifiers.Clear();

                    int GetAnyMotionControllerIndex(string name, CustomMotionController selfRef)
                    {
                        for (int a = 0; a < temp_motionControllerIdentifiers.Count; a++)
                        {
                            var id = temp_motionControllerIdentifiers[a];
                            var type = id.type;

                            CustomMotionController controller = null;
                            switch (type)
                            {
                                case MotionControllerType.AnimationReference:
                                    controller = temp_animationReferences[id.index];
                                    break;
                                case MotionControllerType.BlendTree1D:
                                    controller = temp_blendTrees1D[id.index];
                                    break;
                                case MotionControllerType.BlendTree2D:
                                    controller = temp_blendTrees2D[id.index];
                                    break;
                                case MotionControllerType.MotionComposite:
                                    controller = temp_motionComposites[id.index];
                                    break;
                            }

                            if (controller != null && controller.name == name)
                            {
                                if (ReferenceEquals(controller, selfRef))
                                {
                                    Debug.LogError($"Controller '{selfRef.name}' is self referencing!");
                                }
                                else
                                {
                                    return a;
                                }
                            }
                        }

                        return -1;
                    }
                    int GetAnyMotionControllerIndexOrCreateAnimRef(string controllerName, CustomMotionController selfRef)
                    {
                        int index = GetAnyMotionControllerIndex(controllerName, selfRef);
                        if (index < 0)
                        {
                            int prefixIndex = controllerName.IndexOf(autoCreateAnimReferencePrefix);
                            if (prefixIndex >= 0)
                            {
                                string animName = controllerName.Substring(prefixIndex + autoCreateAnimReferencePrefix.Length);
                                var animAsset = FindAnimationAsset(animName);
                                if (animAsset != null)
                                {
                                    index = temp_motionControllerIdentifiers.Count;
                                    temp_motionControllerIdentifiers.Add(new MotionControllerIdentifier() { index = temp_animationReferences.Count, type = MotionControllerType.AnimationReference });

                                    var animRef = new AnimationReference(controllerName, animAsset.Asset, animAsset.loopMode);
                                    animRef.baseSpeed = animAsset.speed;
                                    animRef.speedMultiplierParameter = -1;

                                    temp_animationReferences.Add(animRef);
                                }
                            }
                        }

                        return index;
                    }
                    int GetLayerStateIndex(string name)
                    {
                        for (int a = 0; a < temp_layerStates.Count; a++)
                        {
                            var state = temp_layerStates[a];
                            if (state == null || state.name != name) continue;

                            return a;
                        }

                        return -1;
                    }

                    CustomAnimationLayer layerInst = new CustomAnimationLayer();
                    temp_animationLayers.Add(layerInst);

                    //layerInst.SetActive(false);
                    layerInst.SetActive(true);
                    layerInst.name = layer.name;
                    layerInst.blendParameterIndex = -1;
                    layerInst.entryStateIndex = 0;
                    layerInst.mix = layer.mix;
                    layerInst.IsAdditive = layer.additive;

                    layer.instance = layerInst;

                    if (layer.animationReferences != null)
                    {
                        for (int a = 0; a < layer.animationReferences.Count; a++)
                        {
                            var anim = layer.animationReferences[a];
                            if (anim == null) continue;

                            var asset = FindAnimationAsset(anim.animationName);

                            temp_motionControllerIdentifiers.Add(new MotionControllerIdentifier() { index = temp_animationReferences.Count, type = MotionControllerType.AnimationReference });

                            var animRef = new AnimationReference(anim.name, asset.Asset, asset.loopMode);
                            animRef.baseSpeed = asset.speed * anim.baseSpeed;
                            animRef.speedMultiplierParameter = FindParamterIndex(anim.speedParameter);
                            animRef.timeOverrideParameter = FindParamterIndex(anim.timeOverrideParameter);

                            animRef.SetAvatarMask(asset.mask, asset.invertMask);
                            anim.instance = animRef;

                            temp_animationReferences.Add(animRef);
                        }
                    }
                    if (layer.blendTree1Ds != null)
                    {
                        for (int a = 0; a < layer.blendTree1Ds.Count; a++)
                        {
                            var creator = layer.blendTree1Ds[a];
                            if (creator == null) continue;

                            temp_motionControllerIdentifiers.Add(new MotionControllerIdentifier() { index = temp_blendTrees1D.Count, type = MotionControllerType.BlendTree1D });

                            var controller = new BlendTree1D(creator.name, FindParamterIndex(creator.parameterX), null);
                            controller.baseSpeed = creator.baseSpeed;
                            controller.speedMultiplierParameter = FindParamterIndex(creator.speedParameter);
                            controller.normalizeDurations = creator.normalizeDurations;
                            controller.normalizedDuration = creator.normalizedDuration;
                            controller.waitForLoops = creator.waitForLoops;
                            controller.forceSyncOnProgress = creator.forceSyncOnProgress;
                            controller.useDedicatedChildForTime = creator.useDedicatedChildForTime;
                            controller.timeDedicatedChildIndex = creator.timeDedicatedChildIndex;

                            controller.parameter_UpdateLimit = creator.parameter_UpdateLimit;
                            controller.parameter_UpdateNormalizedTimeRange = creator.parameter_UpdateNormalizedTimeRange;

                            creator.instance = controller;

                            temp_blendTrees1D.Add(controller);
                        }
                    }
                    if (layer.blendTree2Ds != null)
                    {
                        for (int a = 0; a < layer.blendTree2Ds.Count; a++)
                        {
                            var creator = layer.blendTree2Ds[a];
                            if (creator == null) continue;

                            temp_motionControllerIdentifiers.Add(new MotionControllerIdentifier() { index = temp_blendTrees2D.Count, type = MotionControllerType.BlendTree2D });

                            var controller = new BlendTree2D(creator.name, FindParamterIndex(creator.parameterX), FindParamterIndex(creator.parameterY), null);
                            controller.baseSpeed = creator.baseSpeed;
                            controller.speedMultiplierParameter = FindParamterIndex(creator.speedParameter);
                            controller.normalizeDurations = creator.normalizeDurations;
                            controller.normalizedDuration = creator.normalizedDuration;
                            controller.waitForLoops = creator.waitForLoops;
                            controller.forceSyncOnProgress = creator.forceSyncOnProgress;
                            controller.useDedicatedChildForTime = creator.useDedicatedChildForTime;
                            controller.timeDedicatedChildIndex = creator.timeDedicatedChildIndex;

                            controller.parameterX_UpdateLimit = creator.parameterX_UpdateLimit;
                            controller.parameterX_UpdateNormalizedTimeRange = creator.parameterX_UpdateNormalizedTimeRange;
                            controller.parameterY_UpdateLimit = creator.parameterY_UpdateLimit;
                            controller.parameterY_UpdateNormalizedTimeRange = creator.parameterY_UpdateNormalizedTimeRange;

                            creator.instance = controller;

                            temp_blendTrees2D.Add(controller);
                        }
                    }
                    if (layer.motionComposites != null)
                    {
                        for (int a = 0; a < layer.motionComposites.Count; a++)
                        {
                            var creator = layer.motionComposites[a];
                            if (creator == null) continue;

                            temp_motionControllerIdentifiers.Add(new MotionControllerIdentifier() { index = temp_motionComposites.Count, type = MotionControllerType.MotionComposite });

                            var controller = new MotionComposite(creator.name, null);
                            controller.baseSpeed = creator.baseSpeed;
                            controller.speedMultiplierParameter = FindParamterIndex(creator.speedParameter);
                            controller.normalizeDurations = creator.normalizeDurations;
                            controller.normalizedDuration = creator.normalizedDuration;
                            controller.waitForLoops = creator.waitForLoops;
                            controller.forceSyncOnProgress = creator.forceSyncOnProgress;
                            controller.useDedicatedChildForTime = creator.useDedicatedChildForTime;
                            controller.timeDedicatedChildIndex = creator.timeDedicatedChildIndex;

                            creator.instance = controller;

                            temp_motionComposites.Add(controller);
                        }
                    }

                    // assign motion controller indices to controllers that rely on them
                    if (layer.blendTree1Ds != null)
                    {
                        for (int a = 0; a < layer.blendTree1Ds.Count; a++)
                        {
                            var creator = layer.blendTree1Ds[a];
                            if (creator == null || creator.instance == null) continue;

                            if (creator.motionFields != null)
                            {
                                var motionFields = new MotionField1D[creator.motionFields.Length];
                                for (int b = 0; b < motionFields.Length; b++)
                                {
                                    var creator_ = creator.motionFields[b];
                                    creator_.data.controllerIndex = GetAnyMotionControllerIndexOrCreateAnimRef(creator_.controllerName, creator.instance);
#if UNITY_EDITOR
                                    if (creator_.data.controllerIndex < 0)
                                    {
                                        Debug.LogError($"Controller '{creator_.controllerName}' not found for Blend Tree 1D '{creator.name}'");
                                    }
#endif

                                    motionFields[b] = creator_.data;
                                }

                                creator.instance.motionFields = motionFields;
                            }
                        }
                    }
                    if (layer.blendTree2Ds != null)
                    {
                        for (int a = 0; a < layer.blendTree2Ds.Count; a++)
                        {
                            var creator = layer.blendTree2Ds[a];
                            if (creator == null || creator.instance == null) continue;

                            if (creator.motionFields != null)
                            {
                                var motionFields = new MotionField2D[creator.motionFields.Length];
                                for (int b = 0; b < motionFields.Length; b++)
                                {
                                    var creator_ = creator.motionFields[b];
                                    creator_.data.controllerIndex = GetAnyMotionControllerIndexOrCreateAnimRef(creator_.controllerName, creator.instance);
#if UNITY_EDITOR
                                    if (creator_.data.controllerIndex < 0)
                                    {
                                        Debug.LogError($"Controller '{creator_.controllerName}' not found for Blend Tree 2D '{creator.name}'");
                                    }
#endif

                                    motionFields[b] = creator_.data;
                                }

                                creator.instance.motionFields = motionFields;
                            }
                        }
                    }
                    if (layer.motionComposites != null)
                    {
                        for (int a = 0; a < layer.motionComposites.Count; a++)
                        {
                            var creator = layer.motionComposites[a];
                            if (creator == null || creator.instance == null) continue;

                            if (creator.motionParts != null)
                            {
                                var motionParts = new MotionPart[creator.motionParts.Length];
                                for (int b = 0; b < motionParts.Length; b++)
                                {
                                    var creator_ = creator.motionParts[b];
                                    creator_.data.controllerIndex = GetAnyMotionControllerIndexOrCreateAnimRef(creator_.controllerName, creator.instance);
#if UNITY_EDITOR
                                    if (creator_.data.controllerIndex < 0)
                                    {
                                        Debug.LogError($"Controller '{creator_.controllerName}' not found for Motion Composite '{creator.name}'");
                                    }
#endif
                                    creator_.data.mixParameterIndex = FindParamterIndex(creator_.mixParameter);
                                    motionParts[b] = creator_.data;
                                }

                                creator.instance.motionParts = motionParts;
                            }
                        }
                    }

                    if (layer.layerStates != null)
                    {
                        for (int a = 0; a < layer.layerStates.Count; a++)
                        {
                            var creator = layer.layerStates[a];
                            if (creator == null) continue;

                            creator.instance = new CustomAnimationLayerState();
                            creator.instance.name = creator.name;
                            creator.instance.index = a;
                            creator.instance.motionControllerIndex = GetAnyMotionControllerIndexOrCreateAnimRef(creator.motionControllerName, null);

#if UNITY_EDITOR
                            if (creator.instance.motionControllerIndex < 0)
                            {
                                Debug.LogError($"Motion controller '{creator.motionControllerName}' not found for layer state '{creator.name}'");
                            }
#endif

                            if (creator.transitions != null)
                            {
                                creator.instance.transitions = new Transition[creator.transitions.Length];
                                for (int b = 0; b < creator.transitions.Length; b++)
                                {
                                    var transitionName = creator.transitions[b];
                                    var creator_ = FindTransitionCreator(transitionName);
                                    if (creator_ != null)
                                    {
                                        var transitionInstance = creator_.data.Duplicate();
                                        creator.instance.transitions[b] = transitionInstance;
                                    }
#if UNITY_EDITOR
                                    else
                                    {
                                        Debug.LogError($"State transition '{transitionName}' not found for layer state '{creator.name}'");
                                    }
#endif
                                }
                            }

                            if (layer.initStateName == creator.instance.name) layerInst.entryStateIndex = temp_layerStates.Count;
                            temp_layerStates.Add(creator.instance);
                        }
                        for (int a = 0; a < layer.layerStates.Count; a++)
                        {
                            var creator = layer.layerStates[a];
                            if (creator == null || creator.instance == null) continue;

                            if (creator.transitions != null && creator.instance.transitions != null)
                            {
                                for (int b = 0; b < creator.transitions.Length; b++)
                                {
                                    var transitionName = creator.transitions[b];
                                    var creator_ = FindTransitionCreator(transitionName);
                                    if (creator_ != null)
                                    {
                                        var transitionInstance = creator.instance.transitions[b];
                                        transitionInstance.targetStateIndex = GetLayerStateIndex(creator_.targetStateName);
#if UNITY_EDITOR
                                        Debug.Log($"Create transition {transitionName} with target state index {transitionInstance.targetStateIndex} for state {creator.name}");
#endif
                                    }
                                }
                            }
                        }
                    }

                    layerInst.motionControllerIdentifiers = temp_motionControllerIdentifiers.ToArray();
                    layerInst.states = temp_layerStates.ToArray();

                    if (layer.mask != null) layerInst.SetAvatarMask(layer.mask, layer.invertMask);
                }

                controller.animationReferences = temp_animationReferences.ToArray();
                controller.blendTrees1D = temp_blendTrees1D.ToArray();
                controller.blendTrees2D = temp_blendTrees2D.ToArray();
                controller.motionComposites = temp_motionComposites.ToArray();
                controller.layers = temp_animationLayers.ToArray();

                ClearTempData();

                animator.ApplyController(controller);
                for (int l = 0; l < layers.Count; l++)
                {
                    var layer = layers[l];

                    var layerInst = animator.FindTypedLayer(controller.ConvertLayerName(layer.name));
                    if (layerInst != null)
                    {
                        layer.instance = layerInst;

                        foreach (var anim in layer.animationReferences)
                        {
                            for (int a = 0; a < layerInst.ControllerCount; a++)
                            {
                                var mc = layerInst.GetMotionController(a);
                                if (mc is AnimationReference ar)
                                {
                                    if (ar.name == anim.name)
                                    {
                                        anim.instance = ar;
                                        break;
                                    }
                                }
                            }
                        }
                        foreach (var controller in layer.blendTree1Ds)
                        {
                            for (int a = 0; a < layerInst.ControllerCount; a++)
                            {
                                var mc = layerInst.GetMotionController(a);
                                if (mc is BlendTree1D inst)
                                {
                                    if (inst.name == controller.name)
                                    {
                                        controller.instance = inst;
                                        break;
                                    }
                                }
                            }
                        }
                        foreach (var controller in layer.blendTree2Ds)
                        {
                            for (int a = 0; a < layerInst.ControllerCount; a++)
                            {
                                var mc = layerInst.GetMotionController(a);
                                if (mc is BlendTree2D inst)
                                {
                                    if (inst.name == controller.name)
                                    {
                                        controller.instance = inst;
                                        break;
                                    }
                                }
                            }
                        }
                        foreach (var controller in layer.motionComposites)
                        {
                            for (int a = 0; a < layerInst.ControllerCount; a++)
                            {
                                var mc = layerInst.GetMotionController(a);
                                if (mc is MotionComposite inst)
                                {
                                    if (inst.name == controller.name)
                                    {
                                        controller.instance = inst;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (animator.OnPostLateUpdate == null) animator.OnPostLateUpdate = new UnityEngine.Events.UnityEvent();
            animator.OnPostLateUpdate.RemoveListener(MainUpdate);
            animator.OnPostLateUpdate.AddListener(MainUpdate);

            if (!string.IsNullOrWhiteSpace(sprint_step_reference_controller))
            {
                for (int a = 0; a < animator.LayerCount; a++)
                {
                    var layer = animator.GetLayer(a);
                    var mc = layer.FindMotionController(sprint_step_reference_controller);
                    if (mc is CustomMotionController cmc)
                    {
                        sprintStepReferenceController = cmc;
                        break;
                    }
                }
            }
        }

        //if (owner == null && isClient) GiveOwnership(localPlayer);

        IEnumerator InitCharacter()
        {
            yield return null;

            if (isController)
            {
                if (animator != null)
                {
                    for (int a = 0; a < animator.LayerCount; a++)
                    {
                        byte layerIndex = (byte)a;
                        var layer = animator.GetLayer(a);
                        if (layer is CustomAnimationLayer cal)
                        {
                            cal.AddActiveStateChangeListener((int stateIndex) =>
                            {
                                var state = cal.GetState(stateIndex);
                                if (state != null)
                                {
                                    temp_parameterIndices.Clear();
                                    state.GetActiveParameters(layer, temp_parameterIndices);

                                    SyncLayerState(layerIndex, stateIndex, state.GetNormalizedTime());

                                    SetActiveParameters(temp_parameterIndices);
                                    SyncParameters(temp_parameterIndices);
                                }
                            });
                            
                            cal.AddTransitionStartListener((int transitionIndex) =>
                            {
                                var state = layer.ActiveState;
                                if (state != null)
                                {
                                    temp_parameterIndices.Clear();
                                    state.GetActiveParameters(layer, temp_parameterIndices);

                                    var targetState = layer.GetState(state.TransitionTarget);
                                    SyncStateTransition(layerIndex, layer.ActiveStateIndex, (short)transitionIndex, state.TransitionTarget, state.TransitionTimeLeft, state.GetWeight(), state.GetNormalizedTime(), targetState == null ? 0f : targetState.GetWeight(), targetState == null ? 0f : targetState.GetNormalizedTime());

                                    SetActiveParameters(temp_parameterIndices);
                                    SyncParameters(temp_parameterIndices);
                                }
                            });

                            cal.AddTransitionCancelListener((int transitionIndex) =>
                            {
                                var state = layer.ActiveState;
                                if (state != null)
                                {
                                    temp_parameterIndices.Clear();
                                    state.GetActiveParameters(layer, temp_parameterIndices);

                                    var targetState = layer.GetState(state.TransitionTarget);
                                    SyncStateTransitionCancel(layerIndex, layer.ActiveStateIndex, (short)transitionIndex, state.TransitionTarget, state.TransitionTime, state.TransitionTimeLeft, state.GetWeight(), state.GetNormalizedTime(), targetState == null ? 0f : targetState.GetWeight(), targetState == null ? 0f : targetState.GetNormalizedTime());
                                    
                                    SetActiveParameters(temp_parameterIndices);
                                    SyncParameters(temp_parameterIndices);
                                }
                            });

                            cal.AddTransitionUncancelListener((int transitionIndex) =>
                            {
                                var state = layer.ActiveState;
                                if (state != null)
                                {
                                    temp_parameterIndices.Clear();
                                    state.GetActiveParameters(layer, temp_parameterIndices);

                                    var targetState = layer.GetState(state.TransitionTarget);
                                    SyncStateTransitionUncancel(layerIndex, layer.ActiveStateIndex, (short)transitionIndex, state.TransitionTarget, state.TransitionTime, state.TransitionTimeLeft, state.GetWeight(), state.GetNormalizedTime(), targetState == null ? 0f : targetState.GetWeight(), targetState == null ? 0f : targetState.GetNormalizedTime()); 

                                    SetActiveParameters(temp_parameterIndices);
                                    SyncParameters(temp_parameterIndices);
                                }
                            });
                        }
                    }
                }
            }
            else
            {
                var rigidbody = Rigidbody;
                if (rigidbody != null) rigidbody.isKinematic = true;

                //animator.applyRootMotion = false;
            }

            InitializeInteractions();
        }

        StartCoroutine(InitCharacter());
    }
    protected HashSet<int> temp_parameterIndices = new HashSet<int>();
    protected struct ActiveParameter
    {
        public int index;
        public CustomAnimationParameter parameter;
        public float prevValue;
    }
    private List<ActiveParameter> activeParameters = new List<ActiveParameter>();
    protected void ResetActiveParameters()
    {
        activeParameters.Clear();
    }
    protected void SetActiveParameters(ICollection<int> parameterIndices)
    {
        ResetActiveParameters();
        foreach(var index in parameterIndices)
        {
            var parameter = animator.GetParameter(index);
            if (parameter is CustomAnimationParameter cap)
            {
                activeParameters.Add(new ActiveParameter() { index = index, parameter = cap, prevValue = cap.Value });
            }
        }
    }
    protected void SyncParameters(ICollection<int> parameterIndices)
    {
        foreach(var index in parameterIndices)
        {
            var parameter = animator.GetParameter(index);
            if (parameter is CustomAnimationParameter cap)
            {
                SyncParameter((short)index, cap.Value);
            }
        }
    }
    protected void SyncActiveParameters()
    {
        if (!isController) return;

        if (activeParameters != null)
        {
            for(int a = 0; a < activeParameters.Count; a++)
            {
                var ap = activeParameters[a];
                var currentValue = ap.parameter.Value;
                if (Mathf.Abs(currentValue - ap.prevValue) > 0.0001f)
                {
                    SyncParameter((short)ap.index, currentValue);
                    ap.prevValue = currentValue;
                    activeParameters[a] = ap;
                }
            }
        }
    }
    protected float lastActiveParamSyncTime;
    protected ValidInteractable closestInteractable;
    protected virtual void LateUpdate()
    {
        if (Time.time - lastActiveParamSyncTime >= 0.2f)
        {
            lastActiveParamSyncTime = Time.time;
            SyncActiveParameters();
        }

        closestInteractable = GetClosestInteractable();
        if (InputProxy.InventoryKey)
        {
            OnPressInteractButton(closestInteractable);
        }
        else if (closestInteractable != null && closestInteractable.CanAutoInteract(transform.position, transform.rotation))
        {
            OnPressInteractButton(closestInteractable);
        }

        if (InputProxy.InventoryKeyDown) 
        {
            var p = animator.FindParameter(animParameter_taunt);
            if (p is CustomAnimationParameter.Trigger bp)
            {
                bp.Arm();
            }
        }

        if (InputProxy.MOBA_AbilityQDown)
        {
            OpenLocalWorkoutMenu();
        }
        else if (InputProxy.MOBA_AbilityQUp)
        {
            CloseLocalWorkoutMenu();
        }

        if (gameCamera != null)
        {
            if (IsPerformingWorkout)
            {
                activeWorkout.IgnoreInput = gameCamera.ActionMode == CustomizableActionCamera.Mode.Free;  

                var p = animator.FindParameter(WorkoutBase.workoutParam_repProgress);
                if (p == null)
                {
                }
                else if (p is CustomAnimationParameter.Float fp)
                {
                    float t = activeWorkout.workoutProvider.RepHasReachedApex ? (0.5f + (0.5f * (1f - activeWorkout.RepPosition))) : (activeWorkout.RepPosition * 0.5f);
                    if (activeWorkout.workout.animations.performerRepTimeRemap != null && activeWorkout.workout.animations.performerRepTimeRemap.length > 0) t = activeWorkout.workout.animations.performerRepTimeRemap.Evaluate(t);

                    float currentVal = fp.Value;
                    float diff = (t - prevRepT);
                    if (math.abs(diff) > 0.5f) currentVal += Mathf.Sign(diff);
                    fp.SetValue(Mathf.Clamp01(Mathf.MoveTowards(currentVal, t, Time.deltaTime * 3f)));
                    prevRepT = t;
                }

                if (InputProxy.MOBA_AbilityRDown && activeWorkout.TryToExit())
                {
                    StartCoroutine(ExitWorkout(activeWorkout, 1f, WorkoutBase.statePrefix_exitWorkout));
                }
                else
                {
                    if (InputProxy.RollButtonDown)
                    {
                        gameCamera.SetActionMode(gameCamera.ActionMode == CustomizableActionCamera.Mode.Free ? CustomizableActionCamera.Mode.Orbit : CustomizableActionCamera.Mode.Free);
                    }
                }
            }
            else
            {
                if (gameCamera.ActionMode != CustomizableActionCamera.Mode.Orbit) 
                {
                    gameCamera.RecalculateOriginOnModeSwitch = false;
                    gameCamera.SetActionMode(CustomizableActionCamera.Mode.Orbit); 
                }
            }
        }

    }

    [ServerRpc(PurrNet.Transports.Channel.Unreliable)]
    private void SyncParameter(short parameterIndex, float value)
    {
        SendParameter(parameterIndex, value);
    }

    [ObserversRpc(PurrNet.Transports.Channel.Unreliable)]
    private void SendParameter(short parameterIndex, float value)
    {
        if (isController) return;

        var para = animator.GetParameter(parameterIndex);
        if (para != null)
        {
            para.SetValue(value);
        }
    }

    [Serializable]
    public struct ParameterUpdate
    {
        public short parameterIndex;
        public float value;
    }

    [ServerRpc(PurrNet.Transports.Channel.Unreliable)]
    private void SyncParameters(List<ParameterUpdate> updates)
    {
        SendParameters(updates);
    }

    [ObserversRpc(PurrNet.Transports.Channel.Unreliable)]
    private void SendParameters(List<ParameterUpdate> updates)
    {
        if (isController) return;

        foreach (var update in updates)
        {
            var para = animator.GetParameter(update.parameterIndex);
            if (para != null)
            {
                para.SetValue(update.value);
            }
        }
    }

    [ServerRpc(PurrNet.Transports.Channel.ReliableOrdered)]
    private void SyncLayerState(byte layerIndex, int stateIndex, float normalizedTime)
    {
        SendLayerState(layerIndex, stateIndex, normalizedTime);
    }

    [ObserversRpc(PurrNet.Transports.Channel.ReliableOrdered)]
    private void SendLayerState(byte layerIndex, int stateIndex, float normalizedTime)
    {
        if (isController) return;

        var layer = animator.GetLayer(layerIndex);
        if (layer != null)
        {
            layer.SetActiveState(stateIndex);
            var state = layer.ActiveState;
            if (state != null) 
            { 
                state.ResetTransition();
                state.SetWeight(1f);
                state.SetNormalizedTime(normalizedTime);
            }
        }
    }

    [ServerRpc(PurrNet.Transports.Channel.ReliableOrdered)]
    private void SyncStateTransition(byte layerIndex, int stateIndex, short transitionIndex, int targetStateIndex, float transitionTimeLeft, float stateWeight, float stateTime, float targetWeight, float targetTime)
    {
        SendStateTransition(layerIndex, stateIndex, transitionIndex, targetStateIndex, transitionTimeLeft, stateWeight, stateTime, targetWeight, targetTime);
    }

    [ObserversRpc(PurrNet.Transports.Channel.ReliableOrdered)]
    private void SendStateTransition(byte layerIndex, int stateIndex, short transitionIndex, int targetStateIndex, float transitionTimeLeft, float stateWeight, float stateTime, float targetWeight, float targetTime)
    {
        if (isController) return;

        var layer = animator.GetLayer(layerIndex);
        if (layer != null)
        {
            if (layer.ActiveStateIndex != stateIndex) layer.SetActiveStateUnsafe(stateIndex);

            var activeState = layer.ActiveState;
            if (activeState is CustomAnimationLayerState cals)
            {
                var targetTransition = cals.GetTransition(transitionIndex);
                if (cals.ActiveTransition != targetTransition)
                {
                    cals.ForceStartTransition(transitionIndex, targetStateIndex, transitionTimeLeft, true);
                }

                activeState.SetNormalizedTime(stateTime);
                activeState.SetWeight(stateWeight);

                var targetState = layer.GetState(targetStateIndex);
                if (targetState != null)
                {
                    targetState.SetNormalizedTime(targetTime);
                    targetState.SetWeight(targetWeight);
                }
            }
        }
    }

    [ServerRpc(PurrNet.Transports.Channel.ReliableOrdered)]
    private void SyncStateTransitionCancel(byte layerIndex, int stateIndex, short transitionIndex, int targetStateIndex, float transitionTime, float transitionTimeLeft, float stateWeight, float stateTime, float targetWeight, float targetTime)
    {
        SendStateTransitionCancel(layerIndex, stateIndex, transitionIndex, targetStateIndex, transitionTime, transitionTimeLeft, stateWeight, stateTime, targetWeight, targetTime);
    }

    [ObserversRpc(PurrNet.Transports.Channel.ReliableOrdered)]
    private void SendStateTransitionCancel(byte layerIndex, int stateIndex, short transitionIndex, int targetStateIndex, float transitionTime, float transitionTimeLeft, float stateWeight, float stateTime, float targetWeight, float targetTime)
    {
        if (isController) return;

        var layer = animator.GetLayer(layerIndex);
        if (layer != null)
        {
            if (layer.ActiveStateIndex != stateIndex) layer.SetActiveStateUnsafe(stateIndex);

            var activeState = layer.ActiveState;
            if (activeState is CustomAnimationLayerState cals)
            {
                var targetTransition = cals.GetTransition(transitionIndex);
                if (cals.ActiveTransition == targetTransition)
                {
                    cals.CancelTransition(transitionTime, transitionTimeLeft);
                }

                activeState.SetNormalizedTime(stateTime);
                activeState.SetWeight(stateWeight);

                var targetState = layer.GetState(targetStateIndex);
                if (targetState != null)
                {
                    targetState.SetNormalizedTime(targetTime);
                    targetState.SetWeight(targetWeight);
                }
            }
        }
    }

    [ServerRpc(PurrNet.Transports.Channel.ReliableOrdered)]
    private void SyncStateTransitionUncancel(byte layerIndex, int stateIndex, short transitionIndex, int targetStateIndex, float transitionTime, float transitionTimeLeft, float stateWeight, float stateTime, float targetWeight, float targetTime)
    {
        SendStateTransitionUncancel(layerIndex, stateIndex, transitionIndex, targetStateIndex, transitionTime, transitionTimeLeft, stateWeight, stateTime, targetWeight, targetTime);
    }

    [ObserversRpc(PurrNet.Transports.Channel.ReliableOrdered)]
    private void SendStateTransitionUncancel(byte layerIndex, int stateIndex, short transitionIndex, int targetStateIndex, float transitionTime, float transitionTimeLeft, float stateWeight, float stateTime, float targetWeight, float targetTime)
    {
        if (isController) return;

        var layer = animator.GetLayer(layerIndex);
        if (layer != null)
        {
            if (layer.ActiveStateIndex != stateIndex) layer.SetActiveStateUnsafe(stateIndex);

            var activeState = layer.ActiveState;
            if (activeState is CustomAnimationLayerState cals)
            {
                var targetTransition = cals.GetTransition(transitionIndex);
                if (cals.ActiveTransition == targetTransition)
                {
                    cals.UncancelTransition(transitionTime, transitionTimeLeft);
                }

                activeState.SetNormalizedTime(stateTime);
                activeState.SetWeight(stateWeight);

                var targetState = layer.GetState(targetStateIndex);
                if (targetState != null)
                {
                    targetState.SetNormalizedTime(targetTime);
                    targetState.SetWeight(targetWeight);
                }
            }
        }
    }

    private int rootIsColliding;
    private void RootCollision(Collision collision)
    {
        rootIsColliding = math.min(rootIsColliding + 1, 3);
    }

    private void OnCollisionEnter(Collision collision)
    {
        RootCollision(collision);
    }
    private void OnCollisionStay(Collision collision)
    {
        RootCollision(collision);
    }

    public bool RaycastAgainst(int lod, float3 origin, float3 offset, out Maths.RaycastHitResult hit, float errorMargin = 0.01f)
    {
        if (sandboxCharacter == null)
        {
            hit = default;
            return false;
        }

        return sandboxCharacter.RaycastAgainst(lod, origin, offset, out hit, errorMargin);
    }

    public int DefaultRaycastLOD
    {
        get => sandboxCharacter == null ? 0 : sandboxCharacter.DefaultRaycastLOD;
        set
        {
            if (sandboxCharacter == null) return;
            sandboxCharacter.DefaultRaycastLOD = value;
        }
    }

    protected virtual void OnEnable()
    {
        if (animator != null) animator.AddRootMotionListener(OverrideRootMotion);
    }
    protected virtual void OnDisable()
    {
        if (animator != null) animator.RemoveRootMotionListener(OverrideRootMotion);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (animator != null) 
        { 
            animator.RemoveRootMotionListener(OverrideRootMotion); 
            if (animator.IkManager != null)
            {
                animator.IkManager.PreLateUpdate -= PreIK;
            }
        }

        if (controller != null)
        {
            Destroy(controller);
            controller = null;
        }
    }

    private Vector3 char_moveDir = Vector3.forward;
    private Vector3 char_forward = Vector3.forward;
    private Vector3 char_right = Vector3.right;

    private Vector3 char_moveDir_current = Vector3.forward;
    private Vector3 char_forward_current = Vector3.forward;
    private Vector3 char_right_current = Vector3.right;

    public bool updateCharacterDir = true;
    public float moveAngle;
    public float moveWantX_sign;
    public float locked_moveWantX_sign;
    private float sprintBoost = 1f;
    protected virtual void MainUpdate()
    {
        if (!isController)
        {
            if (rigidbody != null) rigidbody.isKinematic = true;
            return;
        }

        if (animator != null)
        {
            IAnimationParameter p;

            //Vector3 cam_forward = Vector3.ProjectOnPlane(Camera.main == null ? Vector3.forward : Camera.main.transform.forward, Vector3.up).normalized;
            //Vector3 cam_right = Vector3.ProjectOnPlane(Camera.main == null ? Vector3.right : Camera.main.transform.right, Vector3.up).normalized;
            Vector3 cam_forward = Vector3.ProjectOnPlane(inputProvider.FacingForwardAxis, Vector3.up).normalized;
            Vector3 cam_right = Vector3.ProjectOnPlane(inputProvider.FacingRightAxis, Vector3.up).normalized;

            bool setMoveAng = false;
            //updateCharacterDir = true;
            p = animator.FindParameter(animParameter_lock_characterDir);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_lock_characterDir}"); 
                updateCharacterDir = true;
            }
            else if (p is CustomAnimationParameter.Boolean bp)
            {
                if (bp.IsTrue)
                {
                    if (updateCharacterDir) setMoveAng = true;
                    updateCharacterDir = false;
                }
                else
                {
                    updateCharacterDir = true;
                }
            }

            char_moveDir_current = transform.forward;
            char_forward_current = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            char_right_current = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            if (updateCharacterDir || setMoveAng)
            {
                char_moveDir = char_moveDir_current;
                char_forward = char_forward_current;
                char_right = char_right_current;
            }

            Vector2 moveDir = new Vector2(Vector3.Dot(char_moveDir_current, cam_right), Vector3.Dot(char_moveDir_current, cam_forward)).normalized;

            Vector2 moveInput = new Vector2(Mathf.Clamp(InputProvider.MoveAxisX, -1, 1), Mathf.Clamp(InputProvider.MoveAxisY, -1, 1));
            float moveMag = moveInput.magnitude;

            time_since_last_sprint_step += Time.deltaTime;
            bool slowSprint = true;
            if (InputProvider.SprintWasPressed)
            {
                if (sprintStepReferenceController != null)
                {
                    /*var nT = Maths.wrap(sprintStepReferenceController.GetNormalizedTime(sprintStepReferenceController.Layer) + sprint_step_offset, 1f);
                    float nextStep = sprint_step_spacing * math.floor(nT / sprint_step_spacing);
                    float prevStep = Maths.wrap(nextStep - sprint_step_offset, 1f);

                    float offsetA = math.abs(nT - prevStep);
                    float offsetB = math.abs(nT - nextStep);

                    float offset = math.min(offsetA, offsetB);
                    if (offset < sprint_step_window) 
                    {
                        Debug.Log($"BOOST:{(1f - (offset / sprint_step_window))} {nT - prevStep} {nT - nextStep}"); 
                        sprintBoost = math.min(10f, sprintBoost + 3f * (1f - (offset / sprint_step_window)));  
                        slowSprint = false;
                    } 
                    else
                    {
                        sprintBoost = sprintBoost * 0.5f; 
                    }*/

                    float normalizedTimeSInceLast = time_since_last_sprint_step / sprintStepReferenceController.GetScaledDuration(sprintStepReferenceController.Layer);
                    float offset = math.abs(time_since_last_sprint_step - sprint_step_spacing);
                    if (offset < sprint_step_window)
                    {
                        //Debug.Log($"BOOST:{(1f - (offset / sprint_step_window))} {time_since_last_sprint_step - sprint_step_spacing}");  
                        sprintBoost = math.min(10f, sprintBoost + 4f * (1f - (offset / sprint_step_window)));
                        slowSprint = false;
                    }
                    else
                    {
                        sprintBoost = sprintBoost * 0.7f;
                    }

                    time_since_last_sprint_step = 0f;
                }
            }
            if (slowSprint)
            {
                sprintBoost = Mathf.MoveTowards(sprintBoost, 0f, Time.deltaTime * (InputProvider.SprintIsHeld ? 0.8f : 5f));
            }

            float sprint = math.max(InputProvider.SprintIsHeld ? 1.5f : 1f, 1f + (sprintBoost / 10f));
            float moveAnimSpeed = sprint > 1.55f ? sprint_anim_speed : (sprint > 1.1f ? jog_anim_speed : walk_anim_speed);
            //Debug.Log(sprint); 

            Vector3 camMapped_moveInput = (cam_right * moveInput.x) + (cam_forward * moveInput.y);
            Vector2 charMapped_moveInput = new Vector2(Vector3.Dot(camMapped_moveInput, char_right_current), Vector3.Dot(camMapped_moveInput, char_forward_current)).normalized;

            float realMoveAngle = CalculateMoveAngle(char_forward_current, camMapped_moveInput);
            float realMoveAngleABS = math.abs(realMoveAngle);
            moveAlignment = 1f - math.saturate(realMoveAngleABS / 180f);

            float moveAngleTarget = CalculateMoveAngle(char_forward, camMapped_moveInput);
            if (updateCharacterDir || setMoveAng)
            {
                moveAngle = moveAngleTarget;
                locked_moveWantX_sign = moveAngle > 0f ? 1 : -1f;
            }
            else
            {
                moveAngle = Mathf.MoveTowards(moveAngle, moveAngleTarget, Time.deltaTime * 25f);
                //moveAngle = targetAng;//Mathf.Lerp(moveAngle, targetAng, Time.deltaTime * 300f);  
            }

            float scriptTurnAdjustmentNerf = (1f - math.saturate(realMoveAngleABS / turnAdjustmentMaxAngle));
            scriptTurnAdjustmentNerf = turnAdjustmentInfluenceCurve == null || turnAdjustmentInfluenceCurve.length <= 1 ? scriptTurnAdjustmentNerf : turnAdjustmentInfluenceCurve.Evaluate(scriptTurnAdjustmentNerf);
            p = animator.FindParameter(animParameter_script_redirect_weight);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_script_redirect_weight}");
            }
            else if (p is CustomAnimationParameter.Float fp)
            {
                scriptTurnAdjustmentNerf = scriptTurnAdjustmentNerf * fp.Value;
            }

            float scriptTurnAdjustment = Mathf.Clamp(realMoveAngle, -turnAdjustmentSpeedDegrees, turnAdjustmentSpeedDegrees) * moveMag;
            animTurnNerf = math.max(math.saturate(math.abs(CalculateMoveAngle(rootRotationOccum * char_forward_current, camMapped_moveInput) / realMoveAngle)), math.abs(scriptTurnAdjustment / turnAdjustmentSpeedDegrees)) * scriptTurnAdjustmentNerf;
            scriptTurnAdjustment = scriptTurnAdjustment * scriptTurnAdjustmentNerf;
            rootRotationOccumScript = Quaternion.Euler(0f, scriptTurnAdjustment * Time.deltaTime, 0f) * rootRotationOccumScript;

            Debug.DrawRay(Vector3.zero, Vector3.back, Color.green);
            Debug.DrawRay(charMapped_moveInput, Vector3.back, Color.red);

            bool stopFlag = moveMag < 0.11f;
            Vector2 moveDir_want = stopFlag ? moveDir : (moveInput / moveMag);
            p = animator.FindParameter(animParameter_move);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_move}");
            }
            else if (p is CustomAnimationParameter.Float fp)
            {
                fp.SetValue(moveInput.magnitude);
            }

            p = animator.FindParameter(animParameter_move_want);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_move_want}");
            }
            else if (p is CustomAnimationParameter.Float fp)
            {
                fp.SetValue(moveDir_want.magnitude);
            }

            p = animator.FindParameter(animParameter_move_want_grad);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_move_want_grad}");
            }
            else if (p is CustomAnimationParameter.Float fp)
            {
                fp.SetValue(Mathf.MoveTowards(fp.Value, moveInput.magnitude, Time.deltaTime));
            }

            p = animator.FindParameter(animParameter_move_wantX);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_move_wantX}");
            }
            else if (p is CustomAnimationParameter.Float fp)
            {
                fp.SetValue(charMapped_moveInput.x);
            }

            p = animator.FindParameter(animParameter_move_wantY);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_move_wantY}");
            }
            else if (p is CustomAnimationParameter.Float fp)
            {
                fp.SetValue(charMapped_moveInput.y);
            }


            p = animator.FindParameter(animParameter_move_want_angle);
            if (p == null)
            {
                Debug.LogError("NO PARAMETER");
            }
            else
            {
                if (p is CustomAnimationParameter.Float fp)
                {
                    fp.SetValue(moveAngle);
                }
            }
            p = animator.FindParameter(animParameter_move_wantX_sign);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_move_wantX_sign}");
            }
            else if (p is CustomAnimationParameter.Float fp)
            {
                moveWantX_sign = moveAngle > 0f ? 1 : -1f;
                fp.SetValue(moveWantX_sign);
            }

            float turn_want = stopFlag ? 0f : (-Vector2.SignedAngle(moveDir, moveDir_want) / 180f);
            p = animator.FindParameter(animParameter_turn_want);
            if (p == null)
            {
                Debug.LogError("NO PARAMETER");
            }
            else
            {
                if (p is CustomAnimationParameter.Float fp) fp.SetValue(turn_want);
            }
            p = animator.FindParameter(animParameter_turn);
            if (p == null)
            {
                Debug.LogError("NO PARAMETER");
            }
            else
            {
                if (p is CustomAnimationParameter.Float fp)
                {
                    fp.SetValue(Mathf.Clamp(Mathf.MoveTowards(fp.Value, stopFlag ? 0f : (-Vector2.SignedAngle(moveDir, moveDir_want) / 90f), Time.deltaTime * testTurnSpeed), !updateCharacterDir ? -0.35f : -1f, !updateCharacterDir ? 0.35f : 1f));
                }
            }
            p = animator.FindParameter(animParameter_move_speed);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_move_speed}");
            }
            else
            {
                if (p is CustomAnimationParameter.Float fp)
                {
                    fp.SetValue(Mathf.Clamp(Mathf.MoveTowards(fp.Value, math.remap(0f, 1f, 0.8f, 1f, moveInput.magnitude) * sprint, Time.deltaTime * 0.65f), 0.8f, 2f));
                }
            }
            p = animator.FindParameter(animParameter_move_anim_speed);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_move_anim_speed}");
            }
            else
            {
                if (p is CustomAnimationParameter.Float fp)
                {
                    fp.SetValue(Mathf.MoveTowards(fp.Value, moveAnimSpeed, Time.deltaTime * 0.5f));
                }
            }
            p = animator.FindParameter(animParameter_redirect_speed);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_redirect_speed}");
            }
            else
            {
                if (p is CustomAnimationParameter.Float fp)
                {
                    fp.SetValue(Mathf.MoveTowards(fp.Value, math.lerp(1f, redirectSpeed180, math.saturate(math.abs(moveAngle) / 180f)), Time.deltaTime));
                }
            }

            p = animator.FindParameter(animParameter_ground_distance);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_ground_distance}");
            }
            else
            {
                if (p is CustomAnimationParameter.Float fp)
                {
                    fp.SetValue(CalculateDistanceFromGround());

                    p = animator.FindParameter(animParameter_ground_distance_grad);
                    if (p == null)
                    {
                        Debug.LogError($"NO PARAMETER {animParameter_ground_distance_grad}");
                    }
                    else
                    {
                        if (p is CustomAnimationParameter.Float fp2)
                        {
                            fp2.SetValue(Mathf.MoveTowards(fp2.Value, fp.Value, Time.deltaTime * 3f));
                        }
                    }
                }
            }
            p = animator.FindParameter(animParameter_foot_ground_distance);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_foot_ground_distance}");
            }
            else
            {
                if (p is CustomAnimationParameter.Float fp)
                {
                    fp.SetValue(CalculateFootDistanceFromGround());

                    p = animator.FindParameter(animParameter_foot_ground_distance_grad);
                    if (p == null)
                    {
                        Debug.LogError($"NO PARAMETER {animParameter_foot_ground_distance_grad}");
                    }
                    else
                    {
                        if (p is CustomAnimationParameter.Float fp2)
                        {
                            fp2.SetValue(Mathf.MoveTowards(fp2.Value, fp.Value, Time.deltaTime * 3f));
                        }
                    }
                }
            }


            if (InputProvider.JumpWasPressed)
            {
                p = animator.FindParameter(animParameter_jump);
                if (p == null) Debug.LogError("NO JUMP PARAMETER");
                if (p is CustomAnimationParameter.Trigger tp)
                {
                    tp.Arm();
                    jump_reset_timer = jump_reset_time;
                }
            }
            else
            {
                if (jump_reset_timer > 0f)
                {
                    jump_reset_timer -= Time.deltaTime;
                }

                if (jump_reset_timer <= 0f)
                {
                    p = animator.FindParameter(animParameter_jump);
                    if (p == null) Debug.LogError("NO JUMP PARAMETER");
                    if (p is CustomAnimationParameter.Trigger tp) tp.Reset(); // reset jump trigger
                }
            }

            bool disableGravity = dynamicProperty_disable_gravity >= 0 ? dynamicAnimationProperties.GetValueUnsafe(dynamicProperty_disable_gravity) > 0.01f : false;
            p = animator.FindParameter(animParameter_disable_gravity);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_disable_gravity}");
            }
            else
            {
                if (p is CustomAnimationParameter.Boolean bp)
                {
                    if (rigidbody != null)
                    {
                        disableGravity = disableGravity || bp.IsTrue;
                    }
                }
            }
            if (Rigidbody.useGravity != !disableGravity)
            {
                rigidbody.useGravity = !disableGravity;
            }

            p = animator.FindParameter(animParameter_can_land);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_can_land}");
            }
            else
            {
                if (p is CustomAnimationParameter.Boolean bp)
                {
                    float rayLength = 1f;
                    p = animator.FindParameter(animParameter_ray_length_landing);
                    if (p == null)
                    {
                        Debug.LogError($"NO PARAMETER {animParameter_ray_length_landing}");
                    }
                    else
                    {
                        if (p is CustomAnimationParameter.Float fp) rayLength = fp.Value;
                    }

                    bp.SetValue(rayLength > 0f ? (rigidbody.SweepTestExtended(Vector3.down, out RaycastHit hit, rayLength, groundLayerMask, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.4f) : false); // check if we can land
                    Debug.DrawRay(transform.TransformPoint(new Vector3(0f, currentHeightExtents.x, 0f)), Vector3.down * rayLength, bp.IsTrue ? Color.green : Color.red);
                }
            }

        }
    }

    protected void CheckAlterVelocities()
    {
        IAnimationParameter p;

        // Save velocities
        p = animator.FindParameter(animParameter_save_root_velocity);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_save_root_velocity}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val != 0f)
                {
                    SaveRootVelocity(val);

                    fp.SetValue(0f);
                }
            }
        }
        p = animator.FindParameter(animParameter_save_root_angularVelocity);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_save_root_angularVelocity}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val != 0f)
                {
                    SaveRootAngularVelocity(val);

                    fp.SetValue(0f);
                }
            }
        }


        // Kill Velocities
        p = animator.FindParameter(animParameter_root_force_kill_vertical);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_root_force_kill_vertical}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val > 0f)
                {
                    val = math.clamp(1f - val, -1f, 1f);
                    if (Rigidbody != null && !rigidbody.isKinematic)
                    {

                        var velocity = rigidbody.velocity;
                        velocity.y *= val; // kill vertical velocity
                        rigidbody.velocity = velocity;

                    }

                    fp.SetValue(0f);
                }
            }
        }
        p = animator.FindParameter(animParameter_root_force_kill_planar);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_root_force_kill_planar}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val > 0f)
                {
                    val = math.clamp(1f - val, -1f, 1f);
                    if (Rigidbody != null && !rigidbody.isKinematic)
                    {

                        var velocity = rigidbody.velocity;
                        velocity.x *= val;
                        velocity.z *= val;
                        rigidbody.velocity = velocity;

                    }

                    fp.SetValue(0f);
                }
            }
        }
        p = animator.FindParameter(animParameter_root_force_kill_angular);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_root_force_kill_angular}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val > 0f)
                {
                    val = math.clamp(1f - val, -1f, 1f);
                    if (Rigidbody != null && !rigidbody.isKinematic)
                    {

                        rigidbody.angularVelocity = rigidbody.angularVelocity * val;

                    }

                    fp.SetValue(0f);
                }
            }
        }


        // Apply saved root velocity
        p = animator.FindParameter(animParameter_apply_saved_root_velocity_to_root);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_apply_saved_root_velocity_to_root}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val != 0f)
                {
                    ApplySavedRootVelocityToRoot(val);

                    fp.SetValue(0f);
                }
            }
        }
        p = animator.FindParameter(animParameter_apply_saved_root_angularVelocity_to_root);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_apply_saved_root_angularVelocity_to_root}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val != 0f)
                {
                    ApplySavedRootAngularVelocityToRoot(val);

                    fp.SetValue(0f);
                }
            }
        }
    }

    protected Vector3 lastMoveVelocity;
    protected float lastMoveSpeed;
    protected float lastMoveSpeedGrad;
    protected Vector3 lastMoveAngularVelocity;
    protected Vector3 lastMoveAngularAxis;
    protected float lastMoveAngularSpeedDegrees;
    protected float lastMoveAngularSpeedDegreesGrad;

    protected Vector3 rootTranslationOccum;
    protected Quaternion rootRotationOccum = Quaternion.identity;
    protected Quaternion rootRotationOccumScript = Quaternion.identity;
    protected virtual void OverrideRootMotion(Transform rootMotionTransform, Vector3 rootTranslation, Quaternion rootRotation, out Vector3 outgoingRootTranslation, out Quaternion outgoingRootRotation)
    {
        outgoingRootTranslation = rootTranslation;
        outgoingRootRotation = rootRotation;

        if (Rigidbody != null)
        {
            outgoingRootTranslation = Vector3.zero;
            outgoingRootRotation = Quaternion.identity;

            rootTranslationOccum += rootTranslation;
            rootRotationOccum *= rootRotation;

            if (animator.rootMotionMode != CustomAnimator.RootMotionMode.Physics) ApplyRootMotion(Time.deltaTime);
        }
    }

    protected Vector3 rootVelocityOccum;
    protected Vector3 rootAngularVelocityOccum; // radians per second
    protected float rootVelocityOccumAcceleration = 1.5f;
    protected float rootAngularVelocityOccumAcceleration = Mathf.PI; // radians
    protected virtual void ApplyRootMotion(float deltaTime)
    {
        if (!isController)
        {
            return;
        }

        lastMoveVelocity = deltaTime <= 0f ? Vector3.zero : rootTranslationOccum / deltaTime;
        lastMoveSpeed = lastMoveVelocity.magnitude;

        rootRotationOccum.ToAngleAxis(out lastMoveAngularSpeedDegrees, out lastMoveAngularAxis);
        lastMoveAngularSpeedDegrees = deltaTime <= 0f ? 0f : (lastMoveAngularSpeedDegrees / deltaTime);
        lastMoveAngularVelocity = lastMoveAngularAxis * lastMoveAngularSpeedDegrees * Mathf.Deg2Rad;

        lastMoveSpeedGrad = Mathf.MoveTowards(lastMoveSpeedGrad, lastMoveSpeed, deltaTime * 0.5f);
        lastMoveAngularSpeedDegreesGrad = Mathf.MoveTowards(lastMoveAngularSpeedDegreesGrad, lastMoveAngularSpeedDegrees, deltaTime * 15f);

        rootVelocityOccum = Vector3.MoveTowards(rootVelocityOccum, lastMoveVelocity, deltaTime * rootVelocityOccumAcceleration);
        rootAngularVelocityOccum = Vector3.MoveTowards(rootAngularVelocityOccum, lastMoveAngularVelocity, deltaTime * rootAngularVelocityOccumAcceleration);

        rootRotationOccum = rootRotationOccumScript * Quaternion.SlerpUnclamped(Quaternion.identity, rootRotationOccum, math.lerp(1f, math.saturate(1f - animTurnNerf), animTurnAlignmentNerf));

        if (Rigidbody != null)
        {
            rigidbody.Move(rigidbody.position + rootTranslationOccum, rootRotationOccum * rigidbody.rotation);
        }

        rootTranslationOccum = Vector3.zero;
        rootRotationOccum = rootRotationOccumScript = Quaternion.identity;
    }
    protected readonly List<TransformState> tempTransformStates = new List<TransformState>();
    protected void GroundFeetFromOffset(Vector3 offset)
    {
        var rb = Rigidbody;
        if (rb.SweepTestExtended(Vector3.down, out var hit, footDistanceFromGround, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            offset = (Vector3.down * (hit.distance - 0.00001f));
            rb.MovePosition(rb.position + offset);
        }
        else
        {
            rb.MovePosition(rb.position + offset);
        }

    }
    protected void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;
        if (animator.rootMotionMode == CustomAnimator.RootMotionMode.Physics) ApplyRootMotion(Time.fixedDeltaTime);

        RefreshColliders(isController && !Rigidbody.isKinematic, deltaTime);

        if (!isController) return;

        CheckAlterVelocities();


        IAnimationParameter p;

        p = animator.FindParameter(animParameter_ground_feet);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_ground_feet}");
        }
        else
        {
            if (p is CustomAnimationParameter.Trigger tp && tp.TryConsume())
            {
                footDistanceFromGround = CalculateFootDistanceFromGround();
                if (footDistanceFromGround < 1f)
                {
                    Vector3 offset = Vector3.down * footDistanceFromGround;
                    GroundFeetFromOffset(offset);
                }
            }
        }

        p = animator.FindParameter(animParameter_apply_root_velocity);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_apply_root_velocity}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.UpdateAndGetValue();
                if (val != 0f)
                {
                    Vector3 change = rootVelocityOccum * val;
                    if (rigidbody != null && !rigidbody.isKinematic) rigidbody.AddForce(change, ForceMode.VelocityChange);

                    fp.SetValue(0f);
                }
            }
        }
        p = animator.FindParameter(animParameter_apply_root_angularVelocity);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_apply_root_angularVelocity}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.UpdateAndGetValue();
                if (val != 0f)
                {
                    Vector3 change = rootAngularVelocityOccum * val;
                    if (rigidbody != null && !rigidbody.isKinematic) rigidbody.angularVelocity = rigidbody.angularVelocity + change;

                    fp.SetValue(0f);
                }
            }
        }


        p = animator.FindParameter(animParameter_is_grounded);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_is_grounded}");
        }
        else
        {
            if (p is CustomAnimationParameter.Boolean bp)
            {
                bool isGrounded = false;

                var rootBody = Rigidbody;
                //Debug.DrawRay(CalculateBottomPosition() + Vector3.up * 0.05f, Vector3.down * 0.1f, Color.magenta, 1f);
                isGrounded = rootIsColliding > 0 || Physics.Raycast(CalculateBottomPosition() + Vector3.up * 0.05f, Vector3.down, out _, 0.1f, groundLayerMask, QueryTriggerInteraction.Ignore) || rootBody.SweepTestExtended(Vector3.down, out _, 0.05f, groundLayerMask, QueryTriggerInteraction.Ignore);

                bp.SetValue(isGrounded);

                p = animator.FindParameter(animParameter_air_time);
                if (p == null)
                {
                    Debug.LogError($"NO PARAMETER {animParameter_air_time}");
                }
                else
                {
                    if (p is CustomAnimationParameter.Float fp)
                    {
                        fp.SetValue(isGrounded ? 0f : (fp.Value + deltaTime));
                        //Debug.Log($"AIR TIME: {fp.Value}");
                    }
                }
            }
        }


        IAnimationParameter pX, pY, pZ, pX2, pY2, pZ2;
        pX = animator.FindParameter(animParameter_velocity_impulse_x);
        pY = animator.FindParameter(animParameter_velocity_impulse_y);
        pZ = animator.FindParameter(animParameter_velocity_impulse_z);
        if (pX != null && pY != null && pZ != null)
        {
            float valX = pX.UpdateAndGetValue();
            float valY = pY.UpdateAndGetValue();
            float valZ = pZ.UpdateAndGetValue();
            if (valX != 0f || valY != 0f || valZ != 0f)
            {
                Vector3 change = (transform.forward * valZ) + (transform.right * valX) + (transform.up * valY);
                if (rigidbody != null && !rigidbody.isKinematic) rigidbody.AddForce(change, ForceMode.VelocityChange);

                pX.SetValue(0f);
                pY.SetValue(0f);
                pZ.SetValue(0f);
            }
        }

        pX = animator.FindParameter(animParameter_angularVelocity_impulse_x);
        pY = animator.FindParameter(animParameter_angularVelocity_impulse_y);
        pZ = animator.FindParameter(animParameter_angularVelocity_impulse_z);
        if (pX != null && pY != null && pZ != null)
        {
            float valX = pX.UpdateAndGetValue();
            float valY = pY.UpdateAndGetValue();
            float valZ = pZ.UpdateAndGetValue();
            if (valX != 0f || valY != 0f || valZ != 0f)
            {
                Vector3 change = (transform.forward * valZ) + (transform.right * valX) + (transform.up * valY);
                if (rigidbody != null && !rigidbody.isKinematic) rigidbody.angularVelocity = rigidbody.angularVelocity + change;

                pX.SetValue(0f);
                pY.SetValue(0f);
                pZ.SetValue(0f);
            }
        }

        pX = animator.FindParameter(animParameter_speed_sideways);
        pY = animator.FindParameter(animParameter_speed_vertical);
        pZ = animator.FindParameter(animParameter_speed_forward);
        if (pX != null && pY != null && pZ != null)
        {

            pX2 = animator.FindParameter(animParameter_speed_sideways_grad);
            pY2 = animator.FindParameter(animParameter_speed_vertical_grad);
            pZ2 = animator.FindParameter(animParameter_speed_forward_grad);
            if (pX2 != null && pY2 != null && pZ2 != null)
            {
                pX2.SetValue(Mathf.MoveTowards(pX2.Value, pX.Value, Time.fixedDeltaTime * 3f));
                pY2.SetValue(Mathf.MoveTowards(pY2.Value, pY.Value, Time.fixedDeltaTime * 3f));
                pZ2.SetValue(Mathf.MoveTowards(pZ2.Value, pZ.Value, Time.fixedDeltaTime * 3f));
            }
        }

        rootIsColliding = math.max(0, rootIsColliding - 1);
    }

    public MuscleDataLR GetMuscleDataUnsafe(int groupIndex)
    {
        return default;
    }

    public MuscleDataLR GetMuscleData(int groupIndex)
    {
        throw new NotImplementedException();
    }

    public void SetMuscleDataUnsafe(int groupIndex, MuscleDataLR data)
    {
        throw new NotImplementedException();
    }

    public void SetMuscleData(int groupIndex, MuscleDataLR data)
    {
        throw new NotImplementedException();
    }

    public int IndexOfMuscleGroup(string groupName, bool caseSensitive = false)
    {
        throw new NotImplementedException();
    }
}
