using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;

using UnityEngine;

using Swole;
using Swole.Animation;
using Swole.API.Unity;
using Swole.API.Unity.Animation;

public class BulkOutCharacter : MonoBehaviour, IBulkOutCharacter
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

    public bool skipUpdates;

    [Header("Physics"), SerializeField]
    new protected Rigidbody rigidbody;
    public Rigidbody Rigidbody
    {
        get
        {
            if (rigidbody == null) 
            {
                rigidbody = puppetBehaviour == null ? null : puppetBehaviour.RootBody;

                if (rigidbody == null)
                {
                    rigidbody = Extensions.AddOrGetComponent<Rigidbody>(gameObject);
                }
            }

            return rigidbody;
        }
    }

    public LayerMask groundLayerMask = ~0;

    [SerializeField]
    protected TransformLockManager transformLockManager;
    public TransformLockManager TransformLockManager
    {
        get
        {
            if (transformLockManager == null) 
            { 
                transformLockManager = gameObject.GetComponent<TransformLockManager>();
                UpdateLockIndices();
            }
            return transformLockManager;
        }
    }

    public void UpdateLockIndices()
    {
        if (transformLockManager == null) return;

        lockFootL_Index = transformLockManager.IndexOf(lockFootL);
        lockFootR_Index = transformLockManager.IndexOf(lockFootR);
    }

    public void ApplyVelocityToRoot(Vector3 velocity)
    {
        if (Rigidbody == null) return;

        rigidbody.velocity = velocity;
    }
    public void ApplyVelocityToPuppet(Vector3 velocity)
    {
#if SWOLE_ROOTMOTION
        if (puppet == null || puppet.muscles == null) return;

        foreach (var muscle in puppet.muscles)
        {
            if (muscle == null || muscle.rigidbody == null) continue;
            muscle.rigidbody.velocity = velocity;
        }
#endif
    }
    public void ApplyAngularVelocityToRoot(Vector3 velocity)
    {
        if (Rigidbody == null) return;

        rigidbody.angularVelocity = velocity;
    }
    public void ApplyAngularVelocityToPuppet(Vector3 velocity)
    {
#if SWOLE_ROOTMOTION
        if (puppet == null || puppet.muscles == null) return;

        foreach (var muscle in puppet.muscles)
        {
            if (muscle == null || muscle.rigidbody == null) continue;
            muscle.rigidbody.angularVelocity = velocity;
        }
#endif
    }

    [Header("Puppet"), SerializeField]
    protected SwolePuppetMaster puppet; 
    public SwolePuppetMaster Puppet
    {
        get
        {
            if (puppet == null) 
            { 
                puppet = gameObject.GetComponentInChildren<SwolePuppetMaster>(true);
                if (puppet.PostLateUpdate == null) puppet.PostLateUpdate = new UnityEngine.Events.UnityEvent();
                puppet.PostLateUpdate.AddListener(PostPuppetUpdate);
                if (puppet.PostFixedUpdate == null) puppet.PostFixedUpdate = new UnityEngine.Events.UnityEvent();
                puppet.PostFixedUpdate.AddListener(OnFixedUpdate); 
            }

            return puppet;
        }
    }

    [SerializeField]
    protected SwolePuppetBehaviour puppetBehaviour;
    public SwolePuppetBehaviour PuppetBehaviour
    {
        get
        {
            if (puppetBehaviour == null) puppetBehaviour = gameObject.GetComponentInChildren<SwolePuppetBehaviour>(true);
            return puppetBehaviour;
        }
    }

    public Transform footL_transform;
    public string lockFootL = "footLeft";
    private int lockFootL_Index = -1;
    public Transform footR_transform;
    public string lockFootR = "footRight";
    private int lockFootR_Index = -1;

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
        if (puppetBehaviour == null) return distanceFromGround;
        var centerT = puppetBehaviour.MainRigBone;
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

    public SwolePuppetMuscleConfigurationAsset testConfiguration;
    public SwolePuppetMuscleConfigurationAsset testProtectFetalConfiguration;
    public SwolePuppetMuscleConfigurationAsset testRollConfiguration;

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

    [Tooltip("Used to set the puppet pose back to the animation pose. Resets to zero after being applied.")]
    public string animParameter_reset_puppet = "reset_puppet";
    [Tooltip("The current balance of the character.")]
    public string animParameter_balance = "balance";
    [Tooltip("Used to forcibly set the balance of the character. Resets after being applied.")]
    public string animParameter_set_balance = "set_balance";
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

    [Tooltip("Ratio of current velocity of root muscle that will then get applied to the root rigidbody. Resets to zero after being applied.")]
    public string animParameter_apply_velocity_to_root = "apply_velocity_to_root";
    [Tooltip("Ratio of current angular velocity of root muscle that will then get applied to the root rigidbody. Resets to zero after being applied.")]
    public string animParameter_apply_angular_velocity_to_root = "apply_angular_velocity_to_root";


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
    [Tooltip("Ratio of saved root velocity that will then get applied to muscle rigidbodies. Resets to zero after being applied.")]
    public string animParameter_apply_saved_root_velocity_to_puppet = "apply_saved_root_velocity_to_puppet";
    [Tooltip("Ratio of saved root angular velocity that will then get applied to muscle rigidbodies. Resets to zero after being applied.")]
    public string animParameter_apply_saved_root_angularVelocity_to_puppet = "apply_saved_root_angularVelocity_to_puppet";
    public void ApplySavedRootVelocityToRoot(float ratio = 1f) => ApplyVelocityToRoot(savedRootVelocity * ratio);
    public void ApplySavedRootVelocityToPuppet(float ratio = 1f) => ApplyVelocityToPuppet(savedRootVelocity * ratio);
    public void ApplySavedRootAngularVelocityToRoot(float ratio = 1f) => ApplyAngularVelocityToRoot(savedRootAngularVelocity * ratio);
    public void ApplySavedRootAngularVelocityToPuppet(float ratio = 1f) => ApplyAngularVelocityToPuppet(savedRootAngularVelocity * ratio);

    [Tooltip("Ratio of current root muscle velocity to save.")]
    public string animParameter_save_puppet_velocity = "save_puppet_velocity";
    [Tooltip("Ratio of current root muscle angular velocity to save.")]
    public string animParameter_save_puppet_angularVelocity = "save_puppet_angularVelocity";
    private Vector3 savedPuppetVelocity;
    private Vector3 savedPuppetAngularVelocity;
    public void SavePuppetVelocity(float ratio = 1f)
    {
        if (puppetBehaviour == null) return;
        var rb = puppetBehaviour.RootMuscleBody;
        if (rb == null) return;

        savedPuppetVelocity = rb.velocity * ratio;
    }
    public void SavePuppetAngularVelocity(float ratio = 1f)
    {
        if (puppetBehaviour == null) return;
        var rb = puppetBehaviour.RootMuscleBody;
        if (rb == null) return;

        savedPuppetAngularVelocity = rb.angularVelocity * ratio;
    }
    [Tooltip("Ratio of saved puppet velocity that will then get applied to the root rigidbody. Resets to zero after being applied.")]
    public string animParameter_apply_saved_puppet_velocity_to_root = "apply_saved_puppet_velocity_to_root";
    [Tooltip("Ratio of saved puppet angular velocity that will then get applied to the root rigidbody. Resets to zero after being applied.")]
    public string animParameter_apply_saved_puppet_angularVelocity_to_root = "apply_saved_puppet_angularVelocity_to_root";
    [Tooltip("Ratio of saved puppet velocity that will then get applied to muscle rigidbodies. Resets to zero after being applied.")]
    public string animParameter_apply_saved_puppet_velocity_to_puppet = "apply_saved_puppet_velocity_to_puppet";
    [Tooltip("Ratio of saved puppet angular velocity that will then get applied to muscle rigidbodies. Resets to zero after being applied.")]
    public string animParameter_apply_saved_puppet_angularVelocity_to_puppet = "apply_saved_puppet_angularVelocity_to_puppet";
    public void ApplySavedPuppetVelocityToRoot(float ratio = 1f) => ApplyVelocityToRoot(savedPuppetVelocity * ratio);
    public void ApplySavedPuppetVelocityToPuppet(float ratio = 1f) => ApplyVelocityToPuppet(savedPuppetVelocity * ratio);
    public void ApplySavedPuppetAngularVelocityToRoot(float ratio = 1f) => ApplyAngularVelocityToRoot(savedPuppetAngularVelocity * ratio);
    public void ApplySavedPuppetAngularVelocityToPuppet(float ratio = 1f) => ApplyAngularVelocityToPuppet(savedPuppetAngularVelocity * ratio); 


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
    [Tooltip("Target puppet state index.")]
    public string animParameter_target_state = "target_state";
    [Tooltip("Puppet behaviour mapping override sync speed.")]
    public string animParameter_mapping_sync_speed = "mapping_sync_speed";
    [Tooltip("Puppet behaviour unpinning override sync speed.")]
    public string animParameter_unpinning_sync_speed = "unpinning_sync_speed";


    [Tooltip("How much of the current vertical velocity of puppet muscles to nullify.")]
    public string animParameter_puppet_force_kill_vertical = "puppet_force_kill_vertical";
    [Tooltip("How much of the current planar velocity of puppet muscles to nullify.")]
    public string animParameter_puppet_force_kill_planar = "puppet_force_kill_planar";
    [Tooltip("How much of the current angular velocity of puppet muscles to nullify.")]
    public string animParameter_puppet_force_kill_angular = "puppet_force_kill_angular";

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
    private void InitializeDynamicAnimationProperties()
    {
        if (dynamicAnimationProperties == null) dynamicAnimationProperties = gameObject.GetComponent<DynamicAnimationProperties>();
        if (dynamicAnimationProperties == null) return;

        dynamicProperty_disable_gravity = dynamicAnimationProperties.IndexOf(animParameter_disable_gravity);
    }

    [Serializable]
    public class AnimationAsset
    {
        public string name;
        public float speed = 1f;
        public AnimationLoopMode loopMode;
        public AssetIdentifier fileIdentifier;
        public CustomAvatarMaskAsset mask;
        public bool invertMask;
        public bool calculateFootHeights;
        public float footHeightReferenceTime;
        public int footHeightSamplesPerSecond = 16;
        public bool useSeperateRightFootHeightReferenceTime;
        public float rightFootHeightReferenceTime;
        public bool useSeperateRightFootHeightSamplesPerSecond;
        public int rightFootHeightSamplesPerSecond;

        [NonSerialized]
        private CustomAnimation asset;
        public CustomAnimation Asset
        {
            get
            {
                if (asset == null)
                {
                    if (!ContentManager.TryFindContent<CustomAnimation>(fileIdentifier.name, fileIdentifier.package, out asset)) Debug.LogError($"Animation '{fileIdentifier}' not found!");
                }

                return asset;
            }
        }

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public AnimationCurve footHeightDebugCurveL;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public AnimationCurve footHeightDebugCurveR;
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

    [Serializable]
    public class Layer
    {
        public string name;
        public string initStateName;
        public bool additive;
        public CustomAvatarMaskAsset mask;
        public bool invertMask;
        public float mix = 1f;

        [NonSerialized]
        public CustomAnimationLayer instance;

        public void SetActiveAnimation(int index)
        {
            if (instance == null) return;

            instance.ActiveStateIndex = index;
        }

        public List<AnimationReferenceCreator> animationReferences;
        public List<BlendTree1DCreator> blendTree1Ds;
        public List<BlendTree2DCreator> blendTree2Ds;
        public List<MotionCompositeCreator> motionComposites;

        public List<LayerStateCreator> layerStates;
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

    [Serializable]
    public class LayerStateCreator
    {
        public string name;
        public string motionControllerName;

        public SwolePuppetMuscleConfigurationAsset puppetConfig;

        public string[] transitions;

        [NonSerialized]
        public CustomAnimationLayerState instance;
    }
    [Serializable]
    public class LayerStateTransitionCreator
    {
        public string name;
        public string targetStateName;
        public Transition data;
    }

    [Serializable]
    public class BlendTree1DCreator
    {
        public string name;
        public string speedParameter;
        public float baseSpeed;
        public bool normalizeDurations;
        public float normalizedDuration;
        public bool waitForLoops;
        public bool forceSyncOnProgress;
        public bool useDedicatedChildForTime;
        public int timeDedicatedChildIndex;

        public string parameterX;
        public MotionField1DCreator[] motionFields;

        public BlendValuesUpdateLimit parameter_UpdateLimit;
        public float2 parameter_UpdateNormalizedTimeRange = new float2(0f, 1f);

        [NonSerialized]
        public BlendTree1D instance;
    }
    [Serializable]
    public class MotionField1DCreator
    {
        public string controllerName;
        public MotionField1D data;
    }

    [Serializable]
    public class BlendTree2DCreator
    {
        public string name;
        public string speedParameter;
        public float baseSpeed;
        public bool normalizeDurations;
        public float normalizedDuration;
        public bool waitForLoops;
        public bool forceSyncOnProgress;
        public bool useDedicatedChildForTime;
        public int timeDedicatedChildIndex;

        public string parameterX;
        public string parameterY;
        public MotionField2DCreator[] motionFields;

        public BlendValuesUpdateLimit parameterX_UpdateLimit;
        public float2 parameterX_UpdateNormalizedTimeRange = new float2(0f, 1f);

        public BlendValuesUpdateLimit parameterY_UpdateLimit;
        public float2 parameterY_UpdateNormalizedTimeRange = new float2(0f, 1f); 

        [NonSerialized]
        public BlendTree2D instance;
    }
    [Serializable]
    public class MotionField2DCreator
    {
        public string controllerName;
        public MotionField2D data;
    }

    [Serializable]
    public class AnimationReferenceCreator
    {
        public string name;
        public string speedParameter;
        public float baseSpeed;
        public string timeOverrideParameter;

        public string animationName;

        [NonSerialized]
        public AnimationReference instance;
    }

    [Serializable]
    public class MotionCompositeCreator
    {
        public string name;
        public string speedParameter;
        public float baseSpeed;
        public bool normalizeDurations;
        public float normalizedDuration;
        public bool waitForLoops;
        public bool forceSyncOnProgress;
        public bool useDedicatedChildForTime;
        public int timeDedicatedChildIndex;

        public MotionPartCreator[] motionParts;

        [NonSerialized]
        public MotionComposite instance;
    }
    [Serializable]
    public class MotionPartCreator
    {
        public string controllerName;
        public string mixParameter;
        public MotionPart data;
    }

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
        Vector3 heightOffset = (bumperCollider.direction == 0 ? Vector3.right : bumperCollider.direction == 1 ? Vector3.up :  Vector3.forward) * ((bumperCollider.height * 0.5f) + bumperCollider.radius); 
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
        
        float radiusDelta = heightDiff * colliderRadiusScaling * (adjustedHeightDiffRatio / heightDiffRatio); 
        var mainColliderTargetRadius = Mathf.Clamp(startRadiusMainCollider + radiusDelta, 0.01f, mainColliderTargetHeight * 0.5f);
        var bumperColliderTargetRadius = Mathf.Clamp(startRadiusBumperCollider + radiusDelta, 0.01f, bumperColliderTargetHeight * 0.5f); 
        
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

            mainColliderTargetRadius = Mathf.MoveTowards(mainCollider.radius, mainColliderTargetRadius, scalingSpeedMain);
            bumperColliderTargetRadius = Mathf.MoveTowards(bumperCollider.radius, bumperColliderTargetRadius, scalingSpeed);
        }

        mainCollider.transform.position = transform.TransformPoint(mainColliderLocalPos);
        bumperCollider.transform.position = transform.TransformPoint(bumperColliderLocalPos); 

        mainCollider.height = mainColliderTargetHeight;
        bumperCollider.height = bumperColliderTargetHeight;

        mainCollider.radius = mainColliderTargetRadius;
        bumperCollider.radius = bumperColliderTargetRadius;

        if (adjustTransformHeight)
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
        }
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

    public int tempWalkLayer;
    public int tempWalkFwdAnim;
    public int tempWalkTurnLAnim;
    public int tempWalkTurnRAnim;

    public int tempRollAnim;

    public int tempTumbleRecoveryLayer;
    public int tempProtectDefaultAnim;
    public int tempProtectFetalAnim;
    public int tempTumblePushAnim;

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
        if (animator != null) animator.AddRootMotionListener(OverrideRootMotion);

        InitializeColliders();

        if (puppet != null)
        {
            if (puppet.PostLateUpdate == null) puppet.PostLateUpdate = new UnityEngine.Events.UnityEvent();
            puppet.PostLateUpdate.AddListener(PostPuppetUpdate);
        } 
        else
        {
            puppet = Puppet;
        }

        if (TransformLockManager != null) UpdateLockIndices();
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
    protected virtual void Start()
    {
        InitializeDynamicAnimationProperties();

        if (animator != null)
        {
            if (layers != null)
            {
                temp_animationLayers.Clear();
                temp_animationReferences.Clear();
                temp_blendTrees1D.Clear();
                temp_blendTrees2D.Clear();
                temp_motionComposites.Clear();

                temp_parameters.Clear();
                temp_floatParameters.Clear();
                temp_boolParameters.Clear();
                temp_triggerParameters.Clear();

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

                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_reset_puppet, 0));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_balance, 0));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_set_balance, -1));
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
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_target_state, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_mapping_sync_speed, 5f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_unpinning_sync_speed, 3f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_air_time, 0f));


                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_puppet_force_kill_planar, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_puppet_force_kill_vertical, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_puppet_force_kill_angular, 0f));

                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_root_force_kill_planar, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_root_force_kill_vertical, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_root_force_kill_angular, 0f));


                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_apply_root_velocity, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_apply_root_angularVelocity, 0f));

                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_apply_velocity_to_root, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_apply_angular_velocity_to_root, 0f));




                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_save_root_velocity, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_save_root_angularVelocity, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_apply_saved_root_velocity_to_root, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_apply_saved_root_angularVelocity_to_root, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_apply_saved_root_velocity_to_puppet, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_apply_saved_root_angularVelocity_to_puppet, 0f));

                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_save_puppet_velocity, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_save_puppet_angularVelocity, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_apply_saved_puppet_velocity_to_root, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_apply_saved_puppet_angularVelocity_to_root, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_apply_saved_puppet_velocity_to_puppet, 0f));
                AddFloatParameter(new CustomAnimationParameter.Float(animParameter_apply_saved_puppet_angularVelocity_to_puppet, 0f));




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
                    /*int GetMotionControllerIndex(string name, MotionControllerType type)
                    {
                        switch (type)
                        {
                            case MotionControllerType.AnimationReference:
                                for (int a = 0; a < temp_motionControllerIdentifiers.Count; a++)
                                {
                                    if (temp_motionControllerIdentifiers[a].type == type && temp_animationReferences[temp_motionControllerIdentifiers[a].index].name == name)
                                        return a;
                                }
                                break;
                            case MotionControllerType.BlendTree1D:
                                for (int a = 0; a < temp_motionControllerIdentifiers.Count; a++)
                                {
                                    if (temp_motionControllerIdentifiers[a].type == type && temp_blendTrees1D[temp_motionControllerIdentifiers[a].index].name == name)
                                        return a;
                                }
                                break;
                            case MotionControllerType.BlendTree2D:
                                for (int a = 0; a < temp_motionControllerIdentifiers.Count; a++)
                                {
                                    if (temp_motionControllerIdentifiers[a].type == type && temp_blendTrees2D[temp_motionControllerIdentifiers[a].index].name == name)
                                        return a;
                                }
                                break;
                            case MotionControllerType.MotionComposite:
                                for (int a = 0; a < temp_motionControllerIdentifiers.Count; a++)
                                {
                                    if (temp_motionControllerIdentifiers[a].type == type && temp_motionComposites[temp_motionControllerIdentifiers[a].index].name == name)
                                        return a;
                                }
                                break;
                        }

                        return -1;
                    }*/
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
                            creator.instance.PuppetConfiguration = creator.puppetConfig;

#if UNITY_EDITOR
                            if (creator.instance.motionControllerIndex < 0)
                            {
                                Debug.LogError($"Motion controller '{creator.motionControllerName}' not found for layer state '{creator.name}'"); 
                            }
#endif

                            if (creator.transitions != null)
                            {
                                creator.instance.transitions = new Transition[creator.transitions.Length];
                                for(int b = 0; b < creator.transitions.Length; b++)
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

                    /*if (l == tempWalkLayer)
                    {
                        MotionField2D[] motionFields = new MotionField2D[3];
                        motionFields[0] = new MotionField2D()
                        {
                            controllerIndex = animIndices[tempWalkFwdAnim],
                            speed = 1,
                            x = 0,
                            y = 1
                        };
                        motionFields[1] = new MotionField2D()
                        {
                            controllerIndex = animIndices[tempWalkTurnLAnim],
                            speed = 1,
                            x = -1,
                            y = 1
                        };
                        motionFields[2] = new MotionField2D()
                        {
                            controllerIndex = animIndices[tempWalkTurnRAnim],
                            speed = 1,
                            x = 1,
                            y = 1
                        };

                        var btree = new BlendTree2D("walk", 0, 1, motionFields);

                        layerInst.entryStateIndex = temp_layerStates.Count;
                        var state = new CustomAnimationLayerState() { name = "walk_state", motionControllerIndex = temp_motionControllerIdentifiers.Count };
                        state.PuppetConfiguration = testConfiguration;
                        temp_layerStates.Add(state);

                        temp_motionControllerIdentifiers.Add(new MotionControllerIdentifier() { index = temp_blendTrees2D.Count, type = MotionControllerType.BlendTree2D });
                        temp_blendTrees2D.Add(btree);

                        var rollState = new CustomAnimationLayerState() { name = "roll_state", motionControllerIndex = animIndices[tempRollAnim] };
                        rollState.PuppetConfiguration = testRollConfiguration;
                        temp_layerStates.Add(rollState);

                        var transition = new Transition();
                        transition.targetStateIndex = temp_layerStates.Count - 1; // roll_state
                        transition.transitionTime = 0.3f;
                        transition.validRequirementPaths = new TransitionRequirement[]
                        {
                            new TransitionRequirement()
                            {
                                 parameters = new TransitionParameter[]
                                 {
                                     new TransitionParameter()
                                     {
                                         comparisonType = TransitionComparisonType.True,
                                         parameterName = "roll",
                                         parameterValue = 1f
                                     }
                                 }
                            }
                        };

                        state.transitions = new Transition[] { transition };

                        transition = new Transition();
                        transition.targetStateIndex = 0; // walk_state
                        transition.transitionTime = 0.2f;
                        transition.validRequirementPaths = new TransitionRequirement[]
                        {
                            new TransitionRequirement()
                            {
                                validNormalizedTimeRanges = new TransitionTimeRange[]
                                {
                                    new TransitionTimeRange()
                                    {
                                        minNormalizedTime = 0.85f,
                                        maxNormalizedTime = 999f
                                    }
                                }
                            }
                        };

                        rollState.transitions = new Transition[] { transition };  
                    } 
                    else if (l == tempTumbleRecoveryLayer)
                    {
                        MotionField1D[] motionFields = new MotionField1D[2];
                        motionFields[0] = new MotionField1D()
                        {
                            controllerIndex = animIndices[tempProtectDefaultAnim], 
                            speed = 1,
                            threshold = 0f
                        };
                        motionFields[1] = new MotionField1D()
                        {
                            controllerIndex = animIndices[tempProtectFetalAnim],
                            speed = 1,
                            threshold = 1f
                        };

                        var btree = new BlendTree1D("tumble_protect", 2, motionFields);

                        layerInst.entryStateIndex = temp_layerStates.Count;
                        var state = new CustomAnimationLayerState() { name = "tumble_protect_state", motionControllerIndex = temp_motionControllerIdentifiers.Count };
                        state.PuppetConfiguration = testProtectFetalConfiguration;
                        temp_layerStates.Add(state);

                        temp_motionControllerIdentifiers.Add(new MotionControllerIdentifier() { index = temp_blendTrees1D.Count, type = MotionControllerType.BlendTree1D });
                        temp_blendTrees1D.Add(btree);

                        var pushState = new CustomAnimationLayerState() { name = "tumble_push_state", motionControllerIndex = animIndices[tempTumblePushAnim] };
                        pushState.PuppetConfiguration = testProtectFetalConfiguration;
                        temp_layerStates.Add(pushState);

                        var transition = new Transition();
                        transition.targetStateIndex = temp_layerStates.Count - 1; // tumble_push_state
                        transition.transitionTime = 0.6f;
                        transition.validRequirementPaths = new TransitionRequirement[]
                        {
                            new TransitionRequirement()
                            {
                                 parameters = new TransitionParameter[]
                                 {
                                     new TransitionParameter()
                                     {
                                         comparisonType = TransitionComparisonType.True,
                                         parameterName = "jump", 
                                         parameterValue = 1f
                                     }
                                 }
                            }
                        };

                        state.transitions = new Transition[] { transition };

                        transition = new Transition();
                        transition.targetStateIndex = 0; // tumble_protect_state
                        transition.transitionTime = 0.6f;
                        transition.validRequirementPaths = new TransitionRequirement[]
                        {
                            new TransitionRequirement()
                            {
                                validNormalizedTimeRanges = new TransitionTimeRange[]
                                {
                                    new TransitionTimeRange()
                                    {
                                        minNormalizedTime = 0.4f,
                                        maxNormalizedTime = 999f
                                    }
                                }
                            }
                        };

                        pushState.transitions = new Transition[] { transition }; 
                    }*/

                    layerInst.motionControllerIdentifiers = temp_motionControllerIdentifiers.ToArray(); 
                    layerInst.states = temp_layerStates.ToArray(); 

                    if (layer.mask != null) layerInst.SetAvatarMask(layer.mask, layer.invertMask);
                }

                controller.animationReferences = temp_animationReferences.ToArray();
                controller.blendTrees1D = temp_blendTrees1D.ToArray();
                controller.blendTrees2D = temp_blendTrees2D.ToArray();
                controller.motionComposites = temp_motionComposites.ToArray();
                controller.layers = temp_animationLayers.ToArray(); 
                
                temp_animationLayers.Clear();
                temp_layerStates.Clear();
                temp_motionControllerIdentifiers.Clear();
                temp_animationReferences.Clear(); 
                temp_blendTrees1D.Clear();
                temp_blendTrees2D.Clear();

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

            //if (animator.OnPostLateUpdate == null) animator.OnPostLateUpdate = new UnityEngine.Events.UnityEvent();
            //animator.OnPostLateUpdate.RemoveListener(MainUpdate);
            //animator.OnPostLateUpdate.AddListener(MainUpdate);

            if (!string.IsNullOrWhiteSpace(sprint_step_reference_controller))
            {
                for(int a = 0; a < animator.LayerCount; a++)
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
    }

    private int rootIsColliding;
    private int rootMuscleIsColliding;
    private void RootCollision(Collision collision)
    {
        rootIsColliding = math.min(rootIsColliding + 1, 3);
    }
    private void RootMuscleCollision(Collision collision)
    {
        rootMuscleIsColliding = math.min(rootMuscleIsColliding + 1, 3);
    }

    private void OnCollisionEnter(Collision collision)
    {
        RootCollision(collision);
    }
    private void OnCollisionStay(Collision collision)
    {
        RootCollision(collision);
    }

    protected virtual void OnEnable()
    {
        if (animator != null) animator.AddRootMotionListener(OverrideRootMotion);

        if (PuppetBehaviour != null)
        {
#if SWOLE_ROOTMOTION
            var rootMuscle = puppetBehaviour.RootMuscle;
            if (rootMuscle != null)
            {
                var broadcaster = rootMuscle.broadcaster;
                if (broadcaster is SwoleMuscleCollisionBroadcaster smcb)
                {
                    if (smcb.OnContinueColliding == null) smcb.OnContinueColliding = new UnityEngine.Events.UnityEvent<Collision>();
                    smcb.OnContinueColliding.RemoveListener(RootMuscleCollision);
                    smcb.OnContinueColliding.AddListener(RootMuscleCollision); 
                }
            }
#endif
        }
    }
    protected virtual void OnDisable()
    {
        if (animator != null) animator.RemoveRootMotionListener(OverrideRootMotion);

        if (PuppetBehaviour != null)
        {
#if SWOLE_ROOTMOTION
            var rootMuscle = puppetBehaviour.RootMuscle;
            if (rootMuscle != null)
            {
                var broadcaster = rootMuscle.broadcaster;
                if (broadcaster is SwoleMuscleCollisionBroadcaster smcb)
                {
                    if (smcb.OnContinueColliding != null) smcb.OnContinueColliding.RemoveListener(RootMuscleCollision);
                }
            }
#endif
        }
    }
    protected virtual void OnDestroy()
    {
        if (animator != null) animator.RemoveRootMotionListener(OverrideRootMotion); 

        if (controller != null)
        {
            Destroy(controller);
            controller = null;
        }

        if (puppet != null) 
        { 
            if (puppet.PostLateUpdate != null) puppet.PostLateUpdate.RemoveListener(PostPuppetUpdate); 
            if (puppet.PostFixedUpdate != null) puppet.PostFixedUpdate.RemoveListener(OnFixedUpdate); 
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
        if (skipUpdates || Puppet == null || PuppetBehaviour == null) return;

        if (animator != null)
        {
            IAnimationParameter p;

            p = animator.FindParameter(animParameter_reset_puppet);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_reset_puppet}");
            }
            else if (p is CustomAnimationParameter.Float fp)
            {
#if SWOLE_ROOTMOTION
                if (fp.Value > 0f && puppet.muscles != null)
                {
                    for(int a = 0; a < puppet.muscles.Length; a++)
                    {
                        var muscle = puppet.muscles[a];
                        if (muscle == null) continue;

                        //muscle.MoveToTarget(); 
                        muscle.target.GetPositionAndRotation(out var pos, out var rot); 
                        muscle.transform.SetPositionAndRotation(pos, rot);
                    }
                }
#endif

                fp.SetValue(0f);
            }

            p = animator.FindParameter(animParameter_set_balance);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_set_balance}");
            }
            else if (p is CustomAnimationParameter.Float fp)
            {
                if (fp.Value > -1f)
                {
                    puppetBehaviour.Balance = fp.Value; 
                    fp.SetValue(-1f);
                }
            }

            p = animator.FindParameter(animParameter_balance);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_balance}");
            }
            else if (p is CustomAnimationParameter.Float fp)
            {
                fp.SetValue(puppetBehaviour.Balance);
            }


            p = animator.FindParameter(animParameter_mapping_sync_speed);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_mapping_sync_speed}");
            }
            else if (p is CustomAnimationParameter.Float fp)
            {
                puppetBehaviour.mappingOverrideSyncSpeed = fp.Value <= 0f ? fp.GetDefaultValue() : fp.Value; 
            }

            p = animator.FindParameter(animParameter_unpinning_sync_speed);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_unpinning_sync_speed}"); 
            }
            else if (p is CustomAnimationParameter.Float fp)
            {
                puppetBehaviour.unpinningOverrideSyncSpeed = fp.Value <= 0f ? fp.GetDefaultValue() : fp.Value;
            }

            Vector3 cam_forward = Vector3.ProjectOnPlane(Camera.main == null ? Vector3.forward : Camera.main.transform.forward, Vector3.up).normalized;
            Vector3 cam_right = Vector3.ProjectOnPlane(Camera.main == null ? Vector3.right : Camera.main.transform.right, Vector3.up).normalized;  

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


            p = animator.FindParameter(animParameter_target_state);
            if (p == null)
            {
                Debug.LogError($"NO PARAMETER {animParameter_target_state}");  
            }
            else
            {
                if (p is CustomAnimationParameter.Float fp)
                {
                    int val = (int)fp.Value;
                    if (val > 0)
                    {
                        val = val - 1;
                        if ((int)puppetBehaviour.CurrentState != val) 
                        {
                            puppetBehaviour.SetState((SwolePuppetBehaviour.State)val);
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
#if SWOLE_ROOTMOTION
                if (puppet != null && puppet.muscles != null)
                {
                    foreach (var muscle in puppet.muscles)
                    {
                        if (muscle == null || muscle.rigidbody == null) continue;

                        muscle.rigidbody.useGravity = rigidbody.useGravity; 
                    }
                }
#endif
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

            /*switch (puppetBehaviour.CurrentState)
            {
                case SwolePuppetBehaviour.State.Normal:

                    if (InputProxy.RollButtonDown)
                    {
                        p = animator.FindParameter("roll");
                        if (p == null) Debug.LogError("NO ROLL PARAMETER");
                        if (p is CustomAnimationParameter.Trigger tp) tp.Arm();
                    }

                    break;

                case SwolePuppetBehaviour.State.Tumbling:

                    p = animator.FindParameter("turtle");
                    if (p == null) Debug.LogError("NO TURTLE PARAMETER");
                    if (p is CustomAnimationParameter.Float fp_) fp_.SetValue(Mathf.MoveTowards(fp_.Value, InputProxy.RollButton ? 1f : 0f, Time.deltaTime * 3f));

                    if (InputProxy.JumpButtonDown)
                    {
                        p = animator.FindParameter("jump");
                        if (p == null) Debug.LogError("NO JUMP PARAMETER");
                        if (p is CustomAnimationParameter.Trigger tp) tp.Arm();
                    }

                    break;
            }*/

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
        p = animator.FindParameter(animParameter_save_puppet_velocity);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_save_puppet_velocity}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val != 0f)
                {
                    SavePuppetVelocity(val);

                    fp.SetValue(0f);
                }
            }
        }
        p = animator.FindParameter(animParameter_save_puppet_angularVelocity);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_save_puppet_angularVelocity}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val != 0f)
                {
                    SavePuppetAngularVelocity(val);

                    fp.SetValue(0f);
                }
            }
        }


        // Kill Velocities
        p = animator.FindParameter(animParameter_puppet_force_kill_vertical);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_puppet_force_kill_vertical}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val > 0f)
                {
                    val = math.clamp(1f - val, -1f, 1f);
#if SWOLE_ROOTMOTION
                    if (puppet != null && puppet.muscles != null)
                    {
                        foreach (var muscle in puppet.muscles)
                        {
                            if (muscle == null || muscle.rigidbody == null || muscle.rigidbody.isKinematic) continue;

                            var velocity = muscle.rigidbody.velocity;
                            velocity.y *= val; // kill vertical velocity
                            muscle.rigidbody.velocity = velocity;
                        }
                    }
#endif

                    fp.SetValue(0f);
                }
            }
        }
        p = animator.FindParameter(animParameter_puppet_force_kill_planar);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_puppet_force_kill_planar}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val > 0f)
                {
                    val = math.clamp(1f - val, -1f, 1f);
#if SWOLE_ROOTMOTION
                    if (puppet != null && puppet.muscles != null)
                    {
                        foreach (var muscle in puppet.muscles)
                        {
                            if (muscle == null || muscle.rigidbody == null || muscle.rigidbody.isKinematic) continue;

                            var velocity = muscle.rigidbody.velocity;
                            velocity.x *= val;
                            velocity.z *= val;
                            muscle.rigidbody.velocity = velocity;
                        }
                    }
#endif

                    fp.SetValue(0f);
                }
            }
        }
        p = animator.FindParameter(animParameter_puppet_force_kill_angular);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_puppet_force_kill_angular}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val > 0f)
                {
                    val = math.clamp(1f - val, -1f, 1f);
#if SWOLE_ROOTMOTION
                    if (puppet != null && puppet.muscles != null)
                    {
                        foreach (var muscle in puppet.muscles)
                        {
                            if (muscle == null || muscle.rigidbody == null || muscle.rigidbody.isKinematic) continue;

                            muscle.rigidbody.angularVelocity = muscle.rigidbody.angularVelocity * val;
                        }
                    }
#endif

                    fp.SetValue(0f);
                }
            }
        }

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
        p = animator.FindParameter(animParameter_apply_saved_root_velocity_to_puppet);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_apply_saved_root_velocity_to_puppet}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val != 0f)
                {
                    ApplySavedRootVelocityToPuppet(val);

                    fp.SetValue(0f);
                }
            }
        }
        p = animator.FindParameter(animParameter_apply_saved_root_angularVelocity_to_puppet);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_apply_saved_root_angularVelocity_to_puppet}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val != 0f)
                {
                    ApplySavedRootAngularVelocityToPuppet(val);

                    fp.SetValue(0f);
                }
            }
        }

        // Apply saved puppet velocity
        p = animator.FindParameter(animParameter_apply_saved_puppet_velocity_to_root);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_apply_saved_puppet_velocity_to_root}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val != 0f)
                {
                    ApplySavedPuppetVelocityToRoot(val);

                    fp.SetValue(0f);
                }
            }
        }
        p = animator.FindParameter(animParameter_apply_saved_puppet_angularVelocity_to_root);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_apply_saved_puppet_angularVelocity_to_root}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val != 0f)
                {
                    ApplySavedPuppetAngularVelocityToRoot(val); 

                    fp.SetValue(0f);
                }
            }
        }
        p = animator.FindParameter(animParameter_apply_saved_puppet_velocity_to_puppet);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_apply_saved_puppet_velocity_to_puppet}"); 
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val != 0f)
                {
                    ApplySavedPuppetVelocityToPuppet(val);

                    fp.SetValue(0f);
                }
            }
        }
        p = animator.FindParameter(animParameter_apply_saved_puppet_angularVelocity_to_puppet);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_apply_saved_puppet_angularVelocity_to_puppet}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.Value;
                if (val != 0f)
                {
                    ApplySavedPuppetAngularVelocityToPuppet(val);

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
        float balance = 1f;
        if (puppetBehaviour != null) balance = rigidbody.isKinematic ? 1f : puppetBehaviour.Balance;

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
            if (balance > 0f) rigidbody.Move(rigidbody.position + rootTranslationOccum * balance, Quaternion.SlerpUnclamped(Quaternion.identity, rootRotationOccum, balance) * rigidbody.rotation); 
            if (balance < 1f)
            {
                Vector3 velocity = rigidbody.velocity; 
                float speed = velocity.magnitude;

                Vector3 angularVelocity = rigidbody.angularVelocity;
                float angularSpeed = angularVelocity.magnitude; 

                float translationMag = rootTranslationOccum.magnitude;
                float redirectionSpeed = ((1f - balance) * deltaTime) * 5f;
                if (translationMag > 0f) rigidbody.velocity = Vector3.MoveTowards(velocity, rootTranslationOccum, redirectionSpeed * (1f - Mathf.Min(1f, speed / translationMag)));
                
                rootRotationOccum.normalized.ToAngleAxis(out float rootRotDeg, out _); 
                if (rootRotDeg > 0f) rigidbody.angularVelocity = Vector3.MoveTowards(angularVelocity, rootTranslationOccum, redirectionSpeed * (1f - Mathf.Min(1f, angularSpeed / (rootRotDeg * Mathf.Deg2Rad))));   
            }
        } 

        rootTranslationOccum = Vector3.zero;
        rootRotationOccum = rootRotationOccumScript = Quaternion.identity; 
    }
    protected readonly List<TransformState> tempTransformStates = new List<TransformState>();
    protected void GroundFeetFromOffset(Vector3 offset)
    {
        var rb = Rigidbody;
        if (rb.isKinematic)
        {
            tempTransformStates.Clear();
#if SWOLE_ROOTMOTION
            if (puppet != null && puppet.muscles != null)
            {
                for (int a = 0; a < puppet.muscles.Length; a++)
                {
                    var muscle = puppet.muscles[a];
                    if (muscle == null || muscle.transform == null) 
                    {
                        tempTransformStates.Add(default);
                        continue;
                    }

                    tempTransformStates.Add(new TransformState(muscle.transform, true)); 
                }
            }
#endif

            rb.transform.position = rb.transform.position + offset;
#if SWOLE_ROOTMOTION
            if (puppet != null && puppet.muscles != null) 
            {
                for (int a = 0; a < puppet.muscles.Length; a++)
                {
                    var muscle = puppet.muscles[a];
                    if (muscle == null) continue;

                    if (muscle.transform != null)
                    {
                        muscle.transform.position = tempTransformStates[a].position + offset; 
                    }
                }
            }
#endif
        }
        else
        {
            if (rigidbody.SweepTestExtended(Vector3.down, out var hit, footDistanceFromGround, groundLayerMask, QueryTriggerInteraction.Ignore))
            {
                offset = (Vector3.down * (hit.distance - 0.00001f));
                rigidbody.MovePosition(rigidbody.position + offset);
            }
            else
            {
                rigidbody.MovePosition(rigidbody.position + offset);
            }
        }

        bool isLocked = false;
        if (TransformLockManager != null)
        {
            var lockableL = transformLockManager.GetLockable(lockFootL_Index);
            var lockableR = transformLockManager.GetLockable(lockFootR_Index);

            if (lockableL != null)
            {
                lockableL.lockedWorldPosition = lockableL.lockedWorldPosition + offset;
                isLocked = lockableL.locked;
            }
            if (lockableR != null)
            {
                lockableR.lockedWorldPosition = lockableR.lockedWorldPosition + offset;
                isLocked = isLocked || lockableR.locked;
            }
        }
    }
    protected void OnFixedUpdate()
    {
        if (skipUpdates) return;

        float deltaTime = Time.fixedDeltaTime;
        if (animator.rootMotionMode == CustomAnimator.RootMotionMode.Physics) ApplyRootMotion(Time.fixedDeltaTime);

        RefreshColliders(!Rigidbody.isKinematic, deltaTime);    

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
#if SWOLE_ROOTMOTION
                    if (puppet != null && puppet.muscles != null)
                    {
                        foreach (var muscle in puppet.muscles)
                        {
                            if (muscle == null || muscle.rigidbody == null || muscle.rigidbody.isKinematic) continue;
                            muscle.rigidbody.AddForce(change, ForceMode.VelocityChange);
                        }
                    }
#endif

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
#if SWOLE_ROOTMOTION
                    if (puppet != null && puppet.muscles != null)
                    {
                        foreach (var muscle in puppet.muscles)
                        {
                            if (muscle == null || muscle.rigidbody == null || muscle.rigidbody.isKinematic) continue;
                            rigidbody.angularVelocity = rigidbody.angularVelocity + change;
                        }
                    }
#endif

                    fp.SetValue(0f);
                }
            }
        }


        p = animator.FindParameter(animParameter_apply_velocity_to_root);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_apply_velocity_to_root}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.UpdateAndGetValue();
                if (val != 0f)
                {
                    Vector3 newVelocity = puppetBehaviour.RootMuscleBody.velocity * val;
                    if (rigidbody != null && !rigidbody.isKinematic) rigidbody.velocity = newVelocity;

                    fp.SetValue(0f);
                }
            }
        }
        p = animator.FindParameter(animParameter_apply_angular_velocity_to_root);
        if (p == null)
        {
            Debug.LogError($"NO PARAMETER {animParameter_apply_angular_velocity_to_root}");
        }
        else
        {
            if (p is CustomAnimationParameter.Float fp)
            {
                float val = fp.UpdateAndGetValue();
                if (val != 0f)
                {
                    Vector3 newVelocity = puppetBehaviour.RootMuscleBody.angularVelocity * val;
                    if (rigidbody != null && !rigidbody.isKinematic) rigidbody.angularVelocity = newVelocity; 

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

                if (PuppetBehaviour != null)
                {
                    var rootBody = puppetBehaviour.RootBody;
                    if (rootBody == null || rootBody.isKinematic)
                    {
                        var rootMuscleBody = puppetBehaviour.RootMuscleBody;
                        if (rootMuscleBody != null && !rootMuscleBody.isKinematic)
                        {
                            isGrounded = rootMuscleIsColliding > 0 || Physics.Raycast(rootMuscleBody.transform.TransformPoint(Vector3.up * 0.05f), Vector3.down, out _, 0.1f, groundLayerMask, QueryTriggerInteraction.Ignore) || rootMuscleBody.SweepTestExtended(Vector3.down, out _, 0.05f, groundLayerMask, QueryTriggerInteraction.Ignore);
                        }
                    }
                    else
                    {
                        //Debug.DrawRay(CalculateBottomPosition() + Vector3.up * 0.05f, Vector3.down * 0.1f, Color.magenta, 1f);
                        isGrounded = rootIsColliding > 0 || Physics.Raycast(CalculateBottomPosition() + Vector3.up * 0.05f, Vector3.down, out _, 0.1f, groundLayerMask, QueryTriggerInteraction.Ignore) || rootBody.SweepTestExtended(Vector3.down, out _, 0.05f, groundLayerMask, QueryTriggerInteraction.Ignore); 
                    }
                } 

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
#if SWOLE_ROOTMOTION
                if (puppet != null && puppet.muscles != null)
                {
                    foreach (var muscle in puppet.muscles)
                    {
                        if (muscle == null || muscle.rigidbody == null || muscle.rigidbody.isKinematic) continue;
                        muscle.rigidbody.AddForce(change, ForceMode.VelocityChange);
                    }
                }
#endif

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
#if SWOLE_ROOTMOTION
                if (puppet != null && puppet.muscles != null)
                {
                    foreach (var muscle in puppet.muscles)
                    {
                        if (muscle == null || muscle.rigidbody == null || muscle.rigidbody.isKinematic) continue;
                        rigidbody.angularVelocity = rigidbody.angularVelocity + change;
                    }
                }
#endif

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
            if (puppetBehaviour != null)
            {
                var body = Rigidbody;
                if ((body == null || body.isKinematic) && puppetBehaviour != null) body = puppetBehaviour.RootMuscleBody;

                if (body != null)
                {
                    var fwd = transform.forward;
                    var side = transform.right;
                    var up = transform.up;

                    var velocity = body.velocity;

                    pX.SetValue(Vector3.Dot(velocity, side));
                    pY.SetValue(Vector3.Dot(velocity, up));
                    pZ.SetValue(Vector3.Dot(velocity, fwd)); 
                }
            }

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
        rootMuscleIsColliding = math.max(0, rootMuscleIsColliding - 1); 
    }

    protected virtual void PostPuppetUpdate()
    {
        if (skipUpdates) return;

        MainUpdate();

        /*if (pelvisTransform != null && PelvisMuscleTransform != null)
        {
            var targetPos = pelvisTransform.position;
            var pelvisOffset = pelvisMuscleTransform.position - targetPos;
            if (pelvisOffset.magnitude > 0.1f)
            {
                pelvisMuscleTransform.position = targetPos;
                Rigidbody.MovePosition(Rigidbody.position + pelvisOffset);   
            }
        }*/

#if SWOLE_ROOTMOTION
        var puppetBehaviour = PuppetBehaviour;
        if (puppetBehaviour != null)
        {
            var kinematic = puppet.state != RootMotion.Dynamics.PuppetMaster.State.Alive || puppetBehaviour.CurrentState == SwolePuppetBehaviour.State.NormalNoCollider || puppetBehaviour.CurrentState == SwolePuppetBehaviour.State.Tumbling || puppetBehaviour.CurrentState == SwolePuppetBehaviour.State.Floating;
            if (Rigidbody.isKinematic != kinematic)
            {
                Rigidbody.isKinematic = kinematic;   
            }
            if (mainCollider.enabled != !kinematic)
            {
                mainCollider.enabled = !kinematic;
            }
            if (bumperCollider.enabled != !kinematic)
            {
                bumperCollider.enabled = !kinematic;  
            }

            /*if (puppetBehaviour.CurrentState == SwolePuppetBehaviour.State.Tumbling)
            {
                var walkLayer = animator.GetLayer(tempWalkLayer);
                var tumbleRecoveryLayer = animator.GetLayer(tempTumbleRecoveryLayer);
                tumbleRecoveryLayer.Mix = Mathf.MoveTowards(tumbleRecoveryLayer.Mix, 1f, Time.deltaTime);
                walkLayer.Mix = 1f - tumbleRecoveryLayer.Mix;
                Debug.Log($"Tumble In: {tumbleRecoveryLayer.Mix}");  
            } 
            else
            {
                var walkLayer = animator.GetLayer(tempWalkLayer);
                var tumbleRecoveryLayer = animator.GetLayer(tempTumbleRecoveryLayer);
                tumbleRecoveryLayer.Mix = Mathf.MoveTowards(tumbleRecoveryLayer.Mix, 0f, Time.deltaTime);
                walkLayer.Mix = 1f - tumbleRecoveryLayer.Mix;
                Debug.Log($"Tumble Out: {tumbleRecoveryLayer.Mix}");
            }*/

            puppetBehaviour.TeleportRootBodyToPuppetOffset(Time.deltaTime); 
        }
#endif
    }

    public int InteractionManagerID => throw new NotImplementedException();

    public bool IsInteracting => throw new NotImplementedException();

    public IInteractable ActiveInteractable => throw new NotImplementedException();

    public IInteraction ActiveInteraction => throw new NotImplementedException();

    public int DefaultRaycastLOD { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public void AllowInteractability(int triggerId, IInteractable interactable)
    {
        throw new NotImplementedException();
    }

    public void RevokeInteractability(int triggerId, IInteractable interactable)
    {
        throw new NotImplementedException();
    }

    public MuscleDataLR GetMuscleDataUnsafe(int groupIndex)
    {
        throw new NotImplementedException();
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

    public bool RaycastAgainst(int lod, float3 rayOrigin, float3 rayOffset, out Maths.RaycastHitResult hit, float errorMargin = 0.01F)
    {
        throw new NotImplementedException();
    }
}

public interface IBulkOutCharacter : IInteractionManager, IWorkoutPerformer, IRaycastTarget
{
    public CustomAnimator Animator { get; }
    public CustomAnimationController AnimationController { get; }

    public IBulkOutCharacterInputProvider InputProvider { get; }
    public void SetInputProvider(IBulkOutCharacterInputProvider inputProvider);
}

public interface IBulkOutCharacterInputProvider
{
    public float MoveAxisX { get; }
    public float MoveAxisY { get; }

    public float LookAxisX { get; }
    public float LookAxisY { get; }

    public Vector3 FacingForwardAxis { get; }
    public Vector3 FacingRightAxis { get; }

    public bool JumpWasPressed { get; }
    public bool JumpWasReleased { get; }
    public bool JumpIsHeld { get; }

    public bool SprintWasPressed { get; }
    public bool SprintWasReleased { get; }
    public bool SprintIsHeld { get; }
}

public class BulkOutCharacterEmptyInput : IBulkOutCharacterInputProvider
{
    public float MoveAxisX => 0f;
    public float MoveAxisY => 0f;
    public float LookAxisX => 0f;
    public float LookAxisY => 0f;
    public Vector3 FacingForwardAxis => Vector3.zero;
    public Vector3 FacingRightAxis => Vector3.zero;
    public bool JumpWasPressed => false;
    public bool JumpWasReleased => false;
    public bool JumpIsHeld => false;
    public bool SprintWasPressed => false;
    public bool SprintWasReleased => false;
    public bool SprintIsHeld => false;
}

public class BulkOutCharacterPlayerInput : IBulkOutCharacterInputProvider 
{
    public float MoveAxisX => InputProxy.MainLeftJoystickHorizontalRaw;

    public float MoveAxisY => InputProxy.MainLeftJoystickVerticalRaw; 

    public float LookAxisX => InputProxy.CursorAxisX;

    public float LookAxisY => InputProxy.CursorAxisY;

    public Vector3 FacingForwardAxis => Camera.main == null ? Vector3.forward : Camera.main.transform.forward;
    public Vector3 FacingRightAxis => Camera.main == null ? Vector3.right : Camera.main.transform.right;

    public bool JumpWasPressed => InputProxy.JumpButtonDown;

    public bool JumpWasReleased => InputProxy.JumpButtonUp;

    public bool JumpIsHeld => InputProxy.JumpButton;

    public bool SprintWasPressed => InputProxy.SprintButtonDown;

    public bool SprintWasReleased => InputProxy.SprintButtonUp;

    public bool SprintIsHeld => InputProxy.SprintButton;
}