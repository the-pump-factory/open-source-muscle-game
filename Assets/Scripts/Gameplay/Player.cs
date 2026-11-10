using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using Swole;
using Swole.API.Unity;

public class Player : SubstepPhysicsBehaviour
{

    public enum State
    {

        Default, Transition, TransitionKinematic, Hanging, Climbing

    }

    public float totalSpeed;

    public UnityEvent OnUpdate;
    public UnityEvent OnFixedUpdate;
    public UnityEvent OnLateUpdate;

    public UnityEvent OnTick;

    protected State m_State;
    public State CurrentState => m_State;

    protected State m_PrevState;
    public State PreviousState => m_PrevState;

    protected State transitionTargetState;
    protected State transitionCancelState;
    protected bool transitionPosition;
    protected bool transitionRotaiton;
    protected bool transitionSetStateOnCancel;
    protected Vector3 transitionTargetFootPosition;
    protected Vector3 transitionTargetForwardDir;
    protected Vector3 transitionStartFootPosition;
    protected Vector3 transitionStartForwardDir;
    protected Transform transitionTargetAnchorTransform;
    protected float transitionProgress;
    protected float transitionTime;
    protected TransitionStepDelegate transitionStep;
    protected TransitionSpeedMultiplierDelegate transitionSpeed;
    protected TransitionValidationDelegate transitionValidator;
    protected TransitionCompletionDelegate transitionCompletion;
    protected TransitionValidationDelegate transitionCancel;
    protected TransitionCompletionDelegate transitionCancellation;

    public delegate void TransitionStepDelegate(float deltaTime);
    public delegate bool TransitionValidationDelegate(float deltaTime);
    public delegate void TransitionCompletionDelegate(float deltaTime);
    public delegate float TransitionSpeedMultiplierDelegate(float deltaTime);
     
    public void SetState(State state)
    {

        m_State = state;

        switch (state)
        {

            default:

                ResetJumpState();

                m_Rigidbody.isKinematic = false;
                m_Rigidbody.useGravity = true;

                jumpIgnoreToken = false;
                sprintIgnoreToken = false;

                return;

            case State.Transition:

                transitionProgress = 0;

                return;

            case State.TransitionKinematic:

                m_Rigidbody.isKinematic = true;
                m_Rigidbody.velocity = Vector3.zero;

                transitionProgress = 0;

                return;

            case State.Hanging:

                ResetJumpState();

                m_Rigidbody.isKinematic = false;
                m_Rigidbody.velocity = Vector3.zero;
                m_Rigidbody.useGravity = false;

                return;

            case State.Climbing:

                ResetJumpState();

                climbLedgeGrabIgnoranceTimer = climbLedgeGrabIgnoranceWindow;

                m_Rigidbody.isKinematic = false;
                m_Rigidbody.useGravity = true;

                Vector3 velocity = m_Rigidbody.velocity;
                velocity.y = Mathf.Max(0, velocity.y) * (1 - climbVelocityNerf);
                m_Rigidbody.velocity = velocity;

                Quaternion tilt = Quaternion.FromToRotation(m_Rigidbody.rotation * Vector3.forward, climbWallFacingDir);
                m_Rigidbody.AddForce(tilt * Vector3.up * climbPower, ForceMode.VelocityChange);

                return;

        }

    }

    public void TransitionTo(State targetState, Vector3 worldFootPosition, Vector3 worldForwardDir, float time, Transform anchorTransform = null, TransitionStepDelegate onStep = null, TransitionSpeedMultiplierDelegate speedMultiplier = null, TransitionValidationDelegate validationMethod = null, TransitionCompletionDelegate onComplete = null, bool isKinematic = true, TransitionValidationDelegate cancellationMethod = null, State cancellationState = State.Default, TransitionCompletionDelegate onCancel = null, bool setStateOnCancel = true)
    {

        TransitionTo(targetState, worldFootPosition, worldForwardDir, true, true, time, anchorTransform, onStep, speedMultiplier, validationMethod, onComplete, isKinematic, cancellationMethod, cancellationState, onCancel, setStateOnCancel);

    }

    public void TransitionToPosition(State targetState, Vector3 worldFootPosition, float time, Transform anchorTransform = null, TransitionStepDelegate onStep = null, TransitionSpeedMultiplierDelegate speedMultiplier = null, TransitionValidationDelegate validationMethod = null, TransitionCompletionDelegate onComplete = null, bool isKinematic = true, TransitionValidationDelegate cancellationMethod = null, State cancellationState = State.Default, TransitionCompletionDelegate onCancel = null, bool setStateOnCancel = true)
    {

        TransitionTo(targetState, worldFootPosition, Vector3.zero, true, false, time, anchorTransform, onStep, speedMultiplier, validationMethod, onComplete, isKinematic, cancellationMethod, cancellationState, onCancel, setStateOnCancel);

    }

    public void TransitionToRotation(State targetState, Vector3 worldForwardDir, float time, Transform anchorTransform = null, TransitionStepDelegate onStep = null, TransitionSpeedMultiplierDelegate speedMultiplier = null, TransitionValidationDelegate validationMethod = null, TransitionCompletionDelegate onComplete = null, bool isKinematic = true, TransitionValidationDelegate cancellationMethod = null, State cancellationState = State.Default, TransitionCompletionDelegate onCancel = null, bool setStateOnCancel = true)
    {

        TransitionTo(targetState, Vector3.zero, worldForwardDir, false, true, time, anchorTransform, onStep, speedMultiplier, validationMethod, onComplete, isKinematic, cancellationMethod, cancellationState, onCancel, setStateOnCancel);

    }

    public void TransitionTo(State targetState, Vector3 worldFootPosition, Vector3 worldForwardDir, bool includePosition, bool includeRotation, float time, Transform anchorTransform = null, TransitionStepDelegate onStep = null, TransitionSpeedMultiplierDelegate speedMultiplier = null, TransitionValidationDelegate validationMethod = null, TransitionCompletionDelegate onComplete = null, bool isKinematic = true, TransitionValidationDelegate cancellationMethod = null, State cancellationState = State.Default, TransitionCompletionDelegate onCancel = null, bool setStateOnCancel = true)
    {

        transitionPosition = includePosition;
        transitionRotaiton = includeRotation;

        worldForwardDir.y = 0;
        worldForwardDir = worldForwardDir.normalized;

        transitionTargetState = targetState;
        transitionTargetFootPosition = anchorTransform == null ? worldFootPosition : anchorTransform.InverseTransformPoint(worldFootPosition);
        transitionTargetForwardDir = anchorTransform == null ? worldForwardDir : anchorTransform.InverseTransformDirection(worldForwardDir);
        transitionTime = time;
        transitionTargetAnchorTransform = anchorTransform;

        transitionStep = onStep;
        transitionSpeed = speedMultiplier;
        transitionValidator = validationMethod;

        transitionStartForwardDir = m_Rigidbody.rotation * Vector3.forward;
        transitionStartForwardDir.y = 0;
        transitionStartForwardDir = transitionStartForwardDir.normalized;
        transitionStartForwardDir = anchorTransform == null ? transitionStartForwardDir : anchorTransform.InverseTransformDirection(transitionStartForwardDir);

        transitionStartFootPosition = m_Rigidbody.position + m_Rigidbody.rotation * Vector3.down * footHeight;
        transitionStartFootPosition = anchorTransform == null ? transitionStartFootPosition : anchorTransform.InverseTransformPoint(transitionStartFootPosition);

        transitionCompletion = onComplete;

        transitionCancel = cancellationMethod;
        transitionCancelState = cancellationState;
        transitionCancellation = onCancel;

        transitionSetStateOnCancel = setStateOnCancel;

        SetState(isKinematic ? State.TransitionKinematic : State.Transition);

    }

    protected void Transition(float deltaTime)
    {

        transitionProgress += deltaTime * (transitionSpeed == null ? 1 : transitionSpeed(deltaTime));

        transitionStep?.Invoke(deltaTime);

        if (transitionCancel != null && transitionCancel(deltaTime))
        {

            if (transitionSetStateOnCancel) SetState(transitionCancelState);

            transitionCancellation?.Invoke(deltaTime);

            return;

        }

        Vector3 targetPos = transitionTargetAnchorTransform == null ? transitionTargetFootPosition : transitionTargetAnchorTransform.TransformPoint(transitionTargetFootPosition);
        Vector3 targetForwardDir = transitionTargetAnchorTransform == null ? transitionTargetForwardDir : transitionTargetAnchorTransform.TransformDirection(transitionTargetForwardDir);
        targetForwardDir.y = 0;
        targetForwardDir = targetForwardDir.normalized;
        Quaternion targetRot = Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.forward, targetForwardDir, Vector3.up), Vector3.up);

        if (transitionProgress < transitionTime)
        {

            Vector3 startPos = transitionTargetAnchorTransform == null ? transitionStartFootPosition : transitionTargetAnchorTransform.TransformPoint(transitionStartFootPosition);
            Vector3 startForwardDir = transitionTargetAnchorTransform == null ? transitionStartForwardDir : transitionTargetAnchorTransform.TransformDirection(transitionStartForwardDir);
            startForwardDir.y = 0;
            startForwardDir = startForwardDir.normalized;
            Quaternion startRot = Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.forward, startForwardDir, Vector3.up), Vector3.up);

            float t = Mathf.Clamp01(transitionProgress / transitionTime);

            targetPos = Vector3.LerpUnclamped(startPos, targetPos, t);
            targetRot = Quaternion.SlerpUnclamped(startRot, targetRot, t);

        }

        if (transitionPosition) m_Rigidbody.MovePosition(GetPositionFromFootPosition(targetPos, targetRot, footHeightFloorlessOffset));
        if (transitionRotaiton) m_Rigidbody.MoveRotation(targetRot);
         
        if (transitionProgress >= transitionTime || (transitionValidator != null && !transitionValidator(deltaTime)))
        {

            transitionCompletion?.Invoke(deltaTime);

            SetState(transitionTargetState);

            Debug.Log(transitionTargetState.ToString()); 

        }

    }

    protected Rigidbody m_Rigidbody;
    public Rigidbody Rigidbody => m_Rigidbody;

    protected int playerLayerMask;

    protected override void Awake()
    {

        base.Awake();

        if (m_Rigidbody == null) m_Rigidbody = gameObject.AddOrGetComponent<Rigidbody>();

        m_Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        m_Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        m_Rigidbody.freezeRotation = true;
        m_Rigidbody.angularDrag = 0.05f;
        m_Rigidbody.drag = 0.025f;

        m_Rigidbody.mass = mass;

        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        playerLayerMask = LayerMask.GetMask("Player");

    }

    [Header("Physics")]

    public Collider feetCollider;

    public PhysicMaterial frictionlessMaterial;

    public PhysicMaterial defaultFeetMaterial;
    public PhysicMaterial slidingFeetMaterial;

    [Header("Input")]

    public Transform cameraTransform;

    public float inputX;
    public float inputY;

    protected Vector3 cameraAlignedInput;
    public Vector3 CameraAlignedInput => cameraAlignedInput;

    protected Vector3 cameraAlignedInputDirection;
    public Vector3 CameraAlignedInputDirection => cameraAlignedInputDirection;

    protected Vector3 cameraAlignedInputDirectionPlanar;
    public Vector3 CameraAlignedInputDirection2D => cameraAlignedInputDirectionPlanar;

    public float InputVerWorld => Vector3.Dot(CameraAlignedInputDirection, Vector3.up);
    public float InputHorWorld => 1 - math.abs(InputVerWorld);
    public float InputHorWorldSign => Mathf.Sign(Vector3.Dot(transform.forward, cameraTransform.forward));

    public bool inputJump;
    protected bool inputJumpPrev;
    protected bool jumpToken;
    protected bool jumpIgnoreToken;
    protected bool jumpReleaseToken;

    public bool QueryJumpInput()
    {

        if (inputJump != inputJumpPrev)
        {

            jumpReleaseToken = !inputJump;

            inputJumpPrev = jumpToken = inputJump;

        }

        return inputJump;

    }

    public bool IsHoldingJump => QueryJumpInput() && !jumpIgnoreToken;

    protected bool ConsumeJumpToken()
    {

        bool valid = jumpToken;

        jumpToken = false;

        return valid;

    }

    protected bool ConsumeJumpReleaseToken()
    {

        bool valid = jumpReleaseToken;

        jumpReleaseToken = false;

        return valid;

    }

    public bool inputSprint;
    protected bool inputSprintPrev;
    protected bool sprintToken;
    protected bool sprintIgnoreToken;
    protected bool sprintReleaseToken;

    public bool QuerySprintInput()
    {

        if (inputSprint != inputSprintPrev)
        {

            sprintIgnoreToken = !inputSprint;

            inputSprintPrev = sprintToken = inputSprint;

        }

        return inputSprint;

    }

    public bool IsHoldingSprint => QuerySprintInput() && !sprintIgnoreToken;

    protected bool ConsumeSprintToken()
    {

        bool valid = sprintToken;

        sprintToken = false;

        return valid;

    }

    protected bool ConsumeSprintReleaseToken()
    {

        bool valid = sprintReleaseToken;

        sprintReleaseToken = false;

        return valid;

    }

    public bool inputSlide;

    [Header("Params")]

    public float mass = 80;

    public float speedWalk = 6;
    public float speedSprint = 8;

    public float acceleration = 15;
    public float accelerationEase = 0.05f;

    public float powerJump = 5;

    public float airControlResponse = 1f;
    public float airControlSpeed = 6;

    public float rotationSpeed = 180;
    public float rotationEase = 0.05f;

    public float stepHeight = 0.4f;
    public float stepMaxDot = 0.35f;
    public float stepMinFacingDot = -0.25f;

    public float jumpMoveBoost = 1.5f;
    public float jumpThresholdVelocityY = 1.5f;

    public float slideRedirectionSrength = 0.8f;
    public float slideRedirectionSrengthAir = 0.95f;
    public float slideBoostThreshold = 4f;
    public float slideBoostPower = 0.1f;
    public float slideFloorTiltBoostMultiplier = 10f;

    public float floorMoveAsForce = 0.7f;
    public float floorMoveAsForceMaxVelocity = 6;
    public float collisionPadding = 0.05f;
    public float maxCollisionRedirectionDot = 0.8f;
    public float forceTransferOnFloorLoss = 0.85f;
    public float floorLossMinTime = 0.045f;
    [Tooltip("Exponent of velocity magnitude used to divide floor to air force transfer. i.e floor to air force is nerfed by the speed of current velocity")]
    public float floorAirForceTransferNerfMagnitudeExp = 0.15f;

    public float minFloorDot = 0.7071f;

    public float thickness = 0.6f;
    public float height = 2;
    public float minFingerGap = 0.03f;
    public float footHeight = 1;
    public Vector3 FootPosition => transform.position - Vector3.up * footHeight;
    public float footHeightFloorlessOffset = 0.025f;

    public float shimmySpeed = 1;
    public float shimmyMinDot = 0;
    public float shimmyCornerMaxDot = 0.7071f;
    public float shimmyCornerMinDot = -0.7071f;
    public float shimmyCornerTime = 0.4f;
    public float shimmyCornerCostRatio = 0.3f;
    public float shimmyCornerPrioritizationDistance = 0.21f;
    public float shimmyCornerDotLeeway = 0.35f;
    public float shimmySpeedFalloffFactor = 4;
    [Range(0, 1)]
    public float shimmyUpwardsNerfDistanceRatio = 0.44f;
    [Range(0, 1)]
    public float shimmyUpwardsNerfDivisorRatio = 0.714f;

    public float mantleTime = 0.75f;
    public float mantleTimeModifier = 1;
    public float MantleTimeModified => mantleTime * mantleTimeModifier;
    public float mantleCommitRatio = 0.6f;

    public int raycastPrecision = 7;

    public int grabPointPruneThreshold = 25;
    public float grabPointMergeThreshold = 0.2f;
    public float grabPointSearchRadius = 2.5f;
    [Range(1, 8)]
    public int grabPointSearchAxis = 1;
    [Range(4, 32)]
    public int grabPointSearchResolution = 4;

    [Range(0, 1), Tooltip("Minimum dot product allowed between surface normal of grab point and world up vector")]
    public float grabMinDot = 0.86f;
    [Range(-1, 1), Tooltip("Minimum dot product allowed between player facing direction and grab point wall facing normal")]
    public float grabMinFacingDot = 0.7f;

    public float grabReachTop = 1.1f;
    public float grabReachBot = 1.4f;
    public float grabReachClosestPointNerf = 0.95f;
    public Vector3 grabReachTopCenterOffset = new Vector3(0, 0.7f, 0.45f);
    public Vector3 grabReachBotCenterOffset = new Vector3(0, -0.7f, 0);
    public float grabEdgeMinDot = 0.15f;
    public float grabSideReachMultiplier = 0.85f;

    public Vector3 grabPointOrigin;
    public Vector2 grabPointOffset = new Vector2(1, 0.6f);

    public float grabWidth = 0.65f;
    public float grabRaycastStartHeight = 0.25f;

    public float hangChangeDirFrequencyNerfFactor = 5;
    public float hangChangeDirFrequencyNerfDecay = 1;
    protected float hangChangeDirFrequencyNerf;

    public float hangMaxDepth = 0f;
    public float hangVelocityDrag = 0.5f;
    public float hangForcePositionStrength = 3.5f;
    protected float hangForcePositionStrengthRuntime;

    protected float CalculateHangForcePositionStrength(float distance)
    {

        return hangForcePositionStrength / math.pow(math.max(1, distance / hangForcePositionMinDistance), hangForcePositionDistanceFactor);

    }

    public float hangForcePositionMinDistance = 0.01f;
    public float hangForcePositionDistanceFactor = 0.01f;

    public struct GrabPoint
    {

        public Collider collider;
        public Transform transform;
        public Vector3 localPoint;
        public Vector3 localNormal;
        public Vector3 localFacingNormal;

        public bool canMantle;

        public Vector3 PosWorld => transform == null ? localPoint : transform.TransformPoint(localPoint);
        public Vector3 PosLocal => localPoint;

        public Vector3 NormWorld => transform == null ? localNormal : transform.TransformDirection(localNormal);
        public Vector3 NormLocal => localNormal;

        public Vector3 NormFacingWorld => transform == null ? localFacingNormal : transform.TransformDirection(localFacingNormal);
        public Vector3 NormFacingLocal => localFacingNormal;

        public bool IsValid => collider != null;

        public GrabPoint(Collider collider, Transform transform, Vector3 worldPoint, Vector3 worldNormal, Vector3 worldFacingNormal, bool canMantle)
        {

            this.collider = collider;
            this.transform = transform;
            this.canMantle = canMantle;

            localPoint = transform.InverseTransformPoint(worldPoint);
            localNormal = transform.InverseTransformDirection(worldNormal);
            localFacingNormal = transform.InverseTransformDirection(worldFacingNormal);

        }

        public GrabPoint(Collider collider, Vector3 worldPoint, Vector3 worldNormal, Vector3 worldFacingNormal, bool canMantle)
        {

            this.collider = collider;
            this.transform = collider.transform;
            this.canMantle = canMantle;

            localPoint = transform.InverseTransformPoint(worldPoint);
            localNormal = transform.InverseTransformDirection(worldNormal);
            localFacingNormal = transform.InverseTransformDirection(worldFacingNormal);

        }

        public Vector3 TransformPoint(Vector3 point) => transform == null ? point : transform.TransformPoint(point);
        public Vector3 TransformVector(Vector3 vec) => transform == null ? vec : transform.TransformVector(vec);
        public Vector3 TransformDirection(Vector3 dir) => transform == null ? dir : transform.TransformDirection(dir);

        public Vector3 InverseTransformPoint(Vector3 point) => transform == null ? point : transform.InverseTransformPoint(point);
        public Vector3 InverseTransformVector(Vector3 vec) => transform == null ? vec : transform.InverseTransformVector(vec);
        public Vector3 InverseTransformDirection(Vector3 dir) => transform == null ? dir : transform.InverseTransformDirection(dir);

    }

    protected GrabPoint[] activeGrabPoints;
    protected List<GrabPoint> activeGrabPoints2 = new List<GrabPoint>(100);

    public bool ValidateSurface(Vector3 worldNormal)
    {

        return Vector3.Dot(Vector3.up, worldNormal) >= grabMinDot;

    }

    public bool ValidateSurface(GrabPoint grabPoint)
    {

        return ValidateSurface(grabPoint.NormWorld);

    }

    protected RaycastHit[] raycastHitsL1 = new RaycastHit[100];
    protected RaycastHit[] raycastHitsL2 = new RaycastHit[100];

    protected void RecalculateGrabPoints(Vector3 position, Quaternion rotation, int layerMask) 
    {

        if (activeGrabPoints2 == null) activeGrabPoints2 = new List<GrabPoint>();

        activeGrabPoints2.Clear();

        AddGrabPoints(position, rotation, layerMask, false);

    }

    protected void AddGrabPoints(Vector3 position, Quaternion rotation, int layerMask, bool trim = true, float mergeThreshold = 0)
    {

        float halfFingerGap = minFingerGap * 0.5f;

        Vector3 TransformPoint(Vector3 point) => (rotation * point) + position;

        if (activeGrabPoints2 == null) activeGrabPoints2 = new List<GrabPoint>();

        float halfWidth = grabWidth * 0.5f;

        Vector3 grabPointOffsetDir = rotation * Vector3.forward;

        Quaternion toLeft = rotation * Quaternion.Euler(0, -90, 0);
        Quaternion toRight = rotation * Quaternion.Euler(0, 90, 0);

        Vector3 grabPointOffsetDirTangentL = toLeft * Vector3.forward;
        Vector3 grabPointOffsetDirTangentR = toRight * Vector3.forward;

        float step = 1 / (raycastPrecision - 1f);
        float spacing = grabWidth * step * (1 - math.EPSILON);

        bool FindGrabPoint(Vector3 start, Quaternion rotation, Vector3 offsetDir, float width, List<GrabPoint> grabPoints)
        {

            bool hasPoints = false;

            Vector3 extents = new Vector3(width, grabPointOffset.x, 0.1f) * 0.5f;
            start = start - (rotation * new Vector3(0, extents.y, 0));

#if UNITY_EDITOR

            void DrawDebug()
            {
                Vector3 p1 = start + rotation * new Vector3(extents.x, extents.y, -extents.z);
                Vector3 p2 = start + rotation * new Vector3(extents.x, -extents.y, -extents.z);
                Vector3 p3 = start + rotation * new Vector3(-extents.x, extents.y, -extents.z);
                Vector3 p4 = start + rotation * new Vector3(-extents.x, -extents.y, -extents.z);

                Debug.DrawLine(p1, p2, Color.magenta);
                Debug.DrawLine(p3, p4, Color.magenta);
                Debug.DrawLine(p1, p3, Color.magenta);
                Debug.DrawLine(p2, p4, Color.magenta);

                Debug.DrawRay(p1, offsetDir * grabPointOffset.y);
                Debug.DrawRay(p2, offsetDir * grabPointOffset.y);
                Debug.DrawRay(p3, offsetDir * grabPointOffset.y);
                Debug.DrawRay(p4, offsetDir * grabPointOffset.y);

            }

            DrawDebug();

#endif

            int hits;
            if ((hits = Physics.BoxCastNonAlloc(start, extents, offsetDir, raycastHitsL1, rotation, grabPointOffset.y, layerMask, QueryTriggerInteraction.Ignore)) > 0)
            {

                for(int a = 0; a < hits; a++)
                {

                    var hit1 = raycastHitsL1[a];

#if UNITY_EDITOR
                    Debug.DrawRay(hit1.point, hit1.normal, Color.red);
#endif

                    Quaternion slantAdjustment = Quaternion.FromToRotation(-offsetDir, hit1.normal);

#if UNITY_EDITOR

                    Debug.DrawRay(hit1.point + (offsetDir * 0.0001f) + slantAdjustment * Vector3.up * (grabPointOffset.x + 0.0001f), slantAdjustment * Vector3.down * (grabPointOffset.x + 0.01f), Color.yellow);
#endif

                    int hits2;
                    if ((hits2 = Physics.RaycastNonAlloc(hit1.point + (offsetDir * 0.0001f) + slantAdjustment * Vector3.up * (grabPointOffset.x + 0.0001f), slantAdjustment * Vector3.down, raycastHitsL2, grabPointOffset.x + 0.01f, layerMask, QueryTriggerInteraction.Ignore)) > 0)
                    {

                        Vector3 pointNormal = hit1.normal;
                        Vector3 pointPosition = hit1.point;

                        bool valid = false;
                        float ceilingHeight = float.MaxValue;

                        for (int b = 0; b < hits2; b++)
                        {

                            var hit2 = raycastHitsL2[b];

                            if (hit2.collider == hit1.collider)
                            {

                                bool valid_ = ValidateSurface(hit2.normal);

                                if ((hit2.point.y > pointPosition.y && (valid_ || !valid)) || (valid_ && !valid))
                                {

                                    pointNormal = hit2.normal;
                                    pointPosition = hit2.point;
                                    valid = valid_;

                                }

                            }

                        }

                        Plane surfacePlane = new Plane(pointNormal, Vector3.zero);
                        Vector3 offset = hit1.point - pointPosition;
                        offset = surfacePlane.ClosestPointOnPlane(offset);

                        if ( // Check for starting raycast inside collider
                            Physics.CheckBox(pointPosition + Vector3.up * (halfFingerGap + 0.00001f), new Vector3(0.001f, halfFingerGap, 0.001f), Quaternion.identity, layerMask, QueryTriggerInteraction.Ignore) ||
                            Physics.CheckBox(pointPosition + offset + hit1.normal * (halfFingerGap + 0.00001f), new Vector3(halfFingerGap, halfFingerGap, halfFingerGap), Quaternion.FromToRotation(Vector3.forward, hit1.normal), layerMask, QueryTriggerInteraction.Ignore)
                            )
                        {

                            continue; 

                        }

                        for(int b = 0; b < hits2; b++)
                        {

                            var hit2 = raycastHitsL2[b];

                            if (hit2.collider != hit1.collider)
                            {

                                if (hit2.point.y > pointPosition.y) ceilingHeight = Mathf.Min(ceilingHeight, hit2.point.y);

                            }

                        }

                        bool canMantle = (ceilingHeight - pointPosition.y) > height;

                        // TODO: Add crouch mantle

                        if (canMantle)
                        {

#if UNITY_EDITOR
                            Debug.DrawRay(pointPosition + Vector3.up * 0.00001f, Vector3.up * (height - 0.00001f), (Color.yellow + Color.blue) / 2f);
#endif

                            canMantle = !Physics.Raycast(pointPosition + Vector3.up * 0.00001f, Vector3.up, height - 0.00001f, layerMask, QueryTriggerInteraction.Ignore);

                        }

                        grabPoints.Add(new GrabPoint(hit1.collider, pointPosition, pointNormal, -hit1.normal, canMantle));
                        //grabPoints.Add(new GrabPoint(hit1.collider, pointPosition + Vector3.Cross(pointNormal, -hit1.normal) * 0.001f, pointNormal, -hit1.normal, canMantle));
                        //grabPoints.Add(new GrabPoint(hit1.collider, pointPosition - Vector3.Cross(pointNormal, -hit1.normal) * 0.001f, pointNormal, -hit1.normal, canMantle));

                        hasPoints = true;

                    }

                }

            }

            return hasPoints;

        }

        Vector3 startPoint;

        for (int i = 0; i < raycastPrecision; i++)
        {

            float t = i / (raycastPrecision - 1f);

            startPoint = TransformPoint(grabPointOrigin + Vector3.Lerp(Vector3.left * halfWidth, Vector3.right * halfWidth, t));
            FindGrabPoint(startPoint, rotation, grabPointOffsetDir, spacing, activeGrabPoints2);

        }
        
        Vector3 startLeft = grabPointOrigin + Vector3.left * halfWidth;
        Vector3 startRight = grabPointOrigin + Vector3.right * halfWidth;

        float minReach = thickness;//(grabWidth * step) + thickness;
        float maxReach = minReach + (grabPointOffset.y - minReach) * grabSideReachMultiplier;
        float reachStep = (maxReach - minReach) / 3f;

        Vector3 sideReach = (Vector3.forward * (minReach + reachStep * 0.5f));

        startPoint = TransformPoint(startLeft + sideReach);
        FindGrabPoint(startPoint, toRight, grabPointOffsetDirTangentR, reachStep, activeGrabPoints2);
        startPoint = TransformPoint(startRight + sideReach);
        FindGrabPoint(startPoint, toLeft, grabPointOffsetDirTangentL, reachStep, activeGrabPoints2);

        Vector3 sideReachFar = (Vector3.forward * (maxReach - reachStep * 0.5f));

        startPoint = TransformPoint(startLeft + sideReachFar);
        FindGrabPoint(startPoint, toRight, grabPointOffsetDirTangentR, reachStep, activeGrabPoints2);
        startPoint = TransformPoint(startRight + sideReachFar);
        FindGrabPoint(startPoint, toLeft, grabPointOffsetDirTangentL, reachStep, activeGrabPoints2);

        Vector3 sideReachCenter = (sideReach + sideReachFar) * 0.5f;

        startPoint = TransformPoint(startLeft + sideReachCenter);
        FindGrabPoint(startPoint, toRight, grabPointOffsetDirTangentR, reachStep, activeGrabPoints2);
        startPoint = TransformPoint(startRight + sideReachCenter);
        FindGrabPoint(startPoint, toLeft, grabPointOffsetDirTangentL, reachStep, activeGrabPoints2);

        if (trim) TrimGrabPoints(mergeThreshold);

    }

    public void PruneGrabPoints(Vector3 position)
    {

        int threshold = Mathf.Max(0, grabPointPruneThreshold);

        bool isHanging = hangingGrabPointIndex >= 0;
        GrabPoint hangingPoint = default;

        if (isHanging) 
        {

            hangingPoint = activeGrabPoints2[hangingGrabPointIndex];

            activeGrabPoints2.RemoveAt(hangingGrabPointIndex); 
        
        }

        activeGrabPoints2 = activeGrabPoints2.OrderByDescending(p => (p.PosWorld - position).sqrMagnitude).ToList();

        if (isHanging)
        {

            activeGrabPoints2.Add(hangingPoint);
            hangingGrabPointIndex = activeGrabPoints2.Count - 1; 

        }

        while (activeGrabPoints2.Count > threshold) activeGrabPoints2.RemoveAt(0);

    }

    public void TryPruneGrabPoints(Vector3 position)
    {

        if (activeGrabPoints2.Count <= grabPointPruneThreshold) return;

        PruneGrabPoints(position);

    }

    public void FindGrabPointsGreedy(Vector3 position, Quaternion rotation, int layerMask = -1, float randomPosition = 5f, float randomAngle = 10, bool trim = true, float mergeThreshold = 0)
    {

        float axisCount = grabPointSearchAxis * 2;

        for (int a = 0; a < axisCount; a++)
        {

            float ta = a / axisCount;

            Quaternion ra = Quaternion.Euler(0, 180 * ta, 0);

            for(int b = 0; b < grabPointSearchResolution; b++)
            {

                float tb = b / (float)grabPointSearchResolution;

                Quaternion rb = Quaternion.AngleAxis(360 * tb, ra * Vector3.right);//Quaternion.Euler(360 * tb, 0, 0);

                Vector3 dir = (rotation * (rb * (ra * Vector3.forward)));

                //Debug.DrawRay(position, dir * grabPointSearchRadius, Color.red, 0.25f);

                if (Physics.Raycast(position, dir, out RaycastHit hit, grabPointSearchRadius, layerMask, QueryTriggerInteraction.Ignore))
                {

                    float dot = Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up));

                    if (dot > 0.9f) continue;

                    Vector3 norm = hit.normal;
                    norm.y = 0;
                    norm = norm.normalized;

                    FindGrabPoints(hit.point + hit.normal * thickness, Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.forward, -norm, Vector3.up), Vector3.up), randomPosition, randomAngle);

                }

            }

        }

        if (trim) TrimGrabPoints(mergeThreshold);

    }

    protected List<GrabPoint> tempGrabPoints = new List<GrabPoint>(100);
    protected bool[] tempGrabPointStates1 = new bool[100];

    public void TrimGrabPoints(float mergeThreshold = 0)
    {

        if (mergeThreshold <= 0) mergeThreshold = grabPointMergeThreshold;

        for (int a = 0; a < 100; a++) tempGrabPointStates1[a] = false;

        float minDist = mergeThreshold * mergeThreshold;

        if (activeGrabPoints2.Count > 100) activeGrabPoints2.RemoveRange(100, activeGrabPoints2.Count - 100);

        tempGrabPoints.Clear();

        tempGrabPoints.AddRange(activeGrabPoints2);

        activeGrabPoints2.Clear();

        int hangingIndex = hangingGrabPointIndex;

        for (int a = tempGrabPoints.Count - 1; a >= 0; a--)
        {

            var gp1 = tempGrabPoints[a];

            if (a != hangingIndex)
            {

                Vector3 worldPos1 = gp1.PosWorld;

                bool removed = false;

                for (int b = 0; b < tempGrabPoints.Count; b++)
                {

                    if (a == b || tempGrabPointStates1[b]) continue;

                    var gp2 = tempGrabPoints[b];

                    Vector3 worldPos2 = gp2.PosWorld;

                    if (gp1.transform != gp2.transform || (worldPos1 - worldPos2).sqrMagnitude > minDist) continue;

                    removed = true;

                    break;

                }

                if (removed)
                {

                    tempGrabPointStates1[a] = true;

                    continue;

                }

            }

            activeGrabPoints2.Insert(0, gp1);

            if (hangingIndex == a) hangingGrabPointIndex = 0; else hangingGrabPointIndex++;

        }

    }

    public void FindGrabPoints(Vector3 position, Quaternion rotation, float randomPosition = 0.1f, float randomAngle = 5)
    {

        AddGrabPoints(position + (new Vector3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value) - Vector3.one * 0.5f) * randomPosition, rotation * Quaternion.Euler(0, (UnityEngine.Random.value - 0.5f) * randomAngle, 0), ~playerLayerMask);

    }

    protected void OnDrawGizmos()
    {

        Gizmos.color = Color.blue;

        Gizmos.DrawRay(transform.position, floorNormal * 3);

        Gizmos.color = Color.red;

        Gizmos.DrawRay(transform.position, wallNormal * 3);

        float halfWidth = grabWidth * 0.5f;

        Gizmos.color = Color.cyan;

        Vector3 startPointTop = GetReachOriginTop(transform.position, transform.rotation);
        Vector3 startPointBot = GetReachOriginBot(transform.position, transform.rotation);

        Gizmos.color = (Color.cyan + Color.red) / 2f;
        Gizmos.DrawWireSphere(startPointTop, grabReachTop);
        Gizmos.color = (Color.cyan + Color.green) / 2f;
        Gizmos.DrawWireSphere(startPointBot, grabReachBot);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetVaultOriginPoint(transform.position, transform.rotation), vaultReach);  

        Gizmos.color = Color.blue;

        Vector3 startPoint = transform.TransformPoint(grabPointOrigin);

        Gizmos.DrawWireCube(startPoint, Vector3.one * 0.1f);
        Gizmos.DrawLine(startPoint, startPoint + transform.rotation * Vector3.down * grabPointOffset.x);
        Gizmos.DrawWireCube(startPoint + transform.rotation * Vector3.down * grabPointOffset.x, Vector3.one * 0.1f);

        Gizmos.color = Color.red;

        Vector3 p1 = transform.TransformPoint(Vector3.up * height * 0.5f);
        Vector3 p2 = transform.TransformPoint(Vector3.down * height * 0.5f);

        Gizmos.DrawWireCube(p1, Vector3.one * 0.1f);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawWireCube(p2, Vector3.one * 0.1f);

        /*for (int i = 0; i < raycastPrecision; i++)
        {

            Vector3 startPoint = transform.TransformPoint(grabPointStartOffset + Vector3.Lerp(Vector3.left * halfWidth, Vector3.right * halfWidth, i / (raycastPrecision - 1f)));

            Gizmos.DrawLine(startPoint, startPoint + (transform.up * grabPointOffset.x) + (transform.forward * grabPointOffset.y));

        }*/

        if (activeGrabPoints2 != null)
        {

            for (int i = 0; i < activeGrabPoints2.Count; i++)
            {
                 
                Gizmos.color = activeGrabPoints2[i].IsValid ? (activeGrabPoints2[i].canMantle ? Color.green : Color.yellow) : Color.red;

                if (activeGrabPoints2[i].IsValid) Gizmos.DrawWireSphere(activeGrabPoints2[i].PosWorld, Mathf.Max((grabWidth / activeGrabPoints2.Count) * 0.5f, 0.065f));

            }

        }

        Gizmos.color = Color.magenta;

        Vector3 feet = FootPosition;

        Gizmos.DrawLine(transform.position, feet);
        Gizmos.DrawSphere(feet, 0.05f);

    }

    protected Vector3 floorMoveVector;

    protected Vector3 floorNormal;
    protected Vector3 wallNormal;

    protected Vector3 lastFloorNormal;
    protected Vector3 lastWallNormal;

    protected Vector3 floorVectorAccumulator;
    protected Vector3 wallVectorAccumulator;

    protected float timeSinceWallTouch;
    protected float timeSinceFloorTouch;

    protected int floorContactCount;
    protected int wallContactCount;

    protected float lastVelocityOverride;

    protected bool m_Grounded;
    public bool IsOnGround => m_Grounded;

    protected bool m_CanJump;
    public bool CanJump => m_CanJump && jumpCooldown <= 0;

    protected float jumpCooldown;

    protected ContactPoint[] contacts = new ContactPoint[16];

    public void Collide(Vector3 contactNormal)
    {

        var dot = Vector3.Dot(Vector3.up, contactNormal);

        if (dot < minFloorDot)
        {

            wallVectorAccumulator += contactNormal;
            wallContactCount++;

        }
        else
        {

            floorVectorAccumulator += contactNormal;
            floorContactCount++;

        }

    }

    public void OnCollisionStay(Collision collision)
    {

        int contactCount = collision.GetContacts(contacts);

        for (int a = 0; a < contactCount; a++)
        {

            Collide(contacts[a].normal);

        }

    }

    public bool MoveTo(Vector3 startPosition, Quaternion rotation, ref Vector3 position, int layerMask = -1, QueryTriggerInteraction triggerInteration = QueryTriggerInteraction.Ignore)
    {
         
        Vector3 offset = position - startPosition;
        float distance = offset.magnitude;

        if (distance <= 0) return true;
         
        Vector3 direction = offset / distance;

        if (m_Rigidbody.Sweep(startPosition, rotation, direction, out RaycastHit hit, distance, layerMask, triggerInteration))
        {

            position = startPosition + direction * hit.distance;

            m_Rigidbody.MovePosition(position);

#if UNITY_EDITOR
            Debug.DrawLine(startPosition, position, Color.red, 3);
#endif

            return false;

        }

        m_Rigidbody.MovePosition(position);

#if UNITY_EDITOR
        Debug.DrawLine(startPosition, position, Color.red, 3);
#endif

        return true;

    }

    private Vector3 debugPosA, debugPosB;

    protected void Update()
    {

        OnUpdate?.Invoke();

        cameraAlignedInput = GetCameraAlignedInput();
        cameraAlignedInputDirection = cameraAlignedInput.normalized;
        cameraAlignedInputDirectionPlanar = GetCameraAlignedInputDirectionPlanar();

        QueryJumpInput();
        QuerySprintInput();

    }

    protected void LateUpdate()
    {

        OnLateUpdate?.Invoke();

    }

    //protected void FixedUpdate()
    public override void SubstepUpdate()
    {

        totalSpeed = (debugPosA - debugPosB).magnitude / Time.fixedDeltaTime;

        debugPosB = debugPosA;
        debugPosA = m_Rigidbody.position;

        OnFixedUpdate?.Invoke();

        Tick(Time.fixedDeltaTime);

    }

    protected void Tick(float deltaTime)
    {

        OnTick?.Invoke();

        PostCollisionStep(deltaTime);

        switch (m_State)
        {

            default:

                MovementDefault(deltaTime);

                break;

            case State.Transition:
            case State.TransitionKinematic:

                Transition(deltaTime);

                break;

            case State.Hanging:

                EdgeHang(deltaTime);

                break;

            case State.Climbing:

                ClimbWall(deltaTime);

                break;

        }

        wallVectorAccumulator = Vector3.zero;
        floorVectorAccumulator = Vector3.zero;

        wallContactCount = 0;
        floorContactCount = 0;

    }

    public Vector3 GetCameraAlignedInputDirectionPlanar()
    {

        return GetCameraAlignedInputDirection2D(inputX, inputY);

    }

    public Vector3 GetCameraAlignedInputDirection2D(float inputX, float inputY)
    {

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;

        camForward = camForward.normalized;
        camRight = camRight.normalized;

        Vector3 inputDir = camForward * inputY + camRight * inputX;
        float inputMag = inputDir.magnitude;

        if (inputMag > 0) inputDir = inputDir / inputMag;

        return inputDir;

    }

    public Vector3 GetCameraAlignedInput()
    {

        return GetCameraAlignedInput(inputX, inputY);

    }

    public Vector3 GetCameraAlignedInput(float inputX, float inputY)
    {

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        Vector3 input = camForward * inputY + camRight * inputX;

        return input;

    }

    public Vector3 GetCameraAlignedInputDirection()
    {

        return GetCameraAlignedInputDirection(inputX, inputY);

    }

    public Vector3 GetCameraAlignedInputDirection(float inputX, float inputY)
    {

        return GetCameraAlignedInput(inputX, inputY).normalized;

    }

    protected void PostCollisionStep(float deltaTime)
    {

        wallNormal = wallVectorAccumulator.normalized;
        floorNormal = floorVectorAccumulator.normalized;

        m_Grounded = false;

        if (floorContactCount > 0)
        {

            timeSinceFloorTouch = 0;
            m_Grounded = true;
            lastFloorNormal = floorNormal;

        } 
        else
        {

            timeSinceFloorTouch += deltaTime;

        }

        if (wallContactCount > 0) 
        {

            timeSinceWallTouch = 0;
            lastWallNormal = wallNormal; 
        
        } 
        else
        {

            timeSinceWallTouch += deltaTime;

        }

    }

    protected void MovementDefault(float deltaTime)
    {

        jumpCooldown = Mathf.Max(0, jumpCooldown - deltaTime);

        Transform transform = this.transform;

        Vector3 inputDir = GetCameraAlignedInputDirection();

        Vector3 facingDir = transform.forward;
        facingDir.y = 0;
        facingDir = facingDir.normalized;

        float angle = Vector3.SignedAngle(facingDir, inputDir, Vector3.up);

        angle = (angle > 0 ? 1 : (angle < 0 ? -1 : 0)) * rotationSpeed * deltaTime * Mathf.Clamp01(Mathf.Abs(angle / (rotationSpeed * rotationEase)));

        m_Rigidbody.MoveRotation(Quaternion.AngleAxis(angle, Vector3.up) * m_Rigidbody.rotation);

        // Move

        Vector3 velocity = m_Rigidbody.velocity;
        float speedPlanar = new Vector3(velocity.x, 0, velocity.y).magnitude;

        float velocityOverride = Mathf.Clamp01(speedPlanar / floorMoveAsForceMaxVelocity); // Determins how much of floor movement should be applied as a force vs a translation based on current velocity. The higher the velocity, the more floor movement is applied as a force

        if (!IsOnGround)
        {

            feetCollider.sharedMaterial = frictionlessMaterial;

        }
        else if (inputSlide)
        {

            feetCollider.sharedMaterial = slidingFeetMaterial;

        }
        else
        {

            feetCollider.sharedMaterial = defaultFeetMaterial;

        }

        if (IsOnGround || timeSinceFloorTouch < floorLossMinTime)
        {

            if (IsOnGround) m_CanJump = velocity.y <= jumpThresholdVelocityY;

            climbToken = true;
            climbTimer = 0;

            float3 floorNormal_ = lastFloorNormal;
            float3 floorPlane_ = math.abs(1 - (float3)lastFloorNormal);

            float floorTilt = 1 - (Mathf.Max(0, Vector3.Dot(lastFloorNormal, Vector3.up) - minFloorDot) / (1 - minFloorDot));

            if (inputSlide)
            {

                // Slide velocity boost

                float floorMoveMag = floorMoveVector.magnitude;

                if (floorMoveMag > 0)
                {

                    Vector3 floorMoveDir = floorMoveVector / floorMoveMag;

                    floorMoveDir = Quaternion.FromToRotation(Vector3.up, lastFloorNormal) * floorMoveDir;

                    float tiltBoost = Vector3.Dot(floorMoveDir, Vector3.down);
                    Debug.Log((1 + (slideBoostPower + (slideBoostPower * slideFloorTiltBoostMultiplier * tiltBoost))) / (1 + Mathf.Max(0, slideBoostThreshold - floorMoveMag)));
                    m_Rigidbody.AddForce((floorMoveDir * floorMoveMag) * ((1 + (slideBoostPower + (slideBoostPower * slideFloorTiltBoostMultiplier * tiltBoost))) / (1 + Mathf.Max(0, slideBoostThreshold - floorMoveMag))) * forceTransferOnFloorLoss * (1 - velocityOverride), ForceMode.VelocityChange);
                    floorMoveVector = Vector3.zero;

                }

                // Redirect current velocity slightly when sliding

                velocity = m_Rigidbody.velocity;
                Vector3 floorVelocity = floorPlane_ * velocity;
                Vector3 floorForward = ((Vector3)(floorPlane_ * transform.forward)).normalized;

                float floorSpeed = floorVelocity.magnitude;

                if (floorSpeed > 0)
                {

                    Vector3 floorDir = floorVelocity / floorSpeed;

                    Quaternion toForward = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(floorDir, floorForward), ((Vector3.Dot(floorForward, floorDir) + 1) * 0.5f) * deltaTime * slideRedirectionSrength);

                    velocity = toForward * velocity;

                    m_Rigidbody.velocity = velocity;

#if UNITY_EDITOR
                    Debug.DrawRay(transform.position, velocity, Color.magenta, 5);
#endif

                }

                //

            }
            else
            {

                Vector3 targetFloorMoveVector = inputDir * (IsHoldingSprint ? speedSprint : speedWalk);
                Vector3 differenceVector = targetFloorMoveVector - floorMoveVector;
                float diffMag = differenceVector.magnitude;

                if (diffMag != 0)
                {

                    differenceVector = differenceVector / diffMag;

                    floorMoveVector = floorMoveVector + (differenceVector * acceleration * deltaTime * Mathf.Clamp01(diffMag / (acceleration * accelerationEase)));

                }

                float move = floorMoveVector.magnitude;

                if (move != 0)
                {

                    Vector3 moveDir = floorMoveVector / move;

                    float moveFacingFactor = Vector3.Dot(m_Rigidbody.rotation * Vector3.forward, moveDir);

                    moveDir = Quaternion.FromToRotation(Vector3.up, lastFloorNormal) * moveDir;

                    move = move * deltaTime;

                    // HANDLE STEPS

                    Vector3 rigidPosition = m_Rigidbody.position;

                    if (moveFacingFactor > stepMinFacingDot) 
                    {

                        Vector3 footPosition = GetFootPosition(rigidPosition, m_Rigidbody.rotation) + Vector3.up * 0.01f;
                        Vector3 nextFootPosition = footPosition + (m_Rigidbody.rotation * Vector3.forward * thickness) + (moveDir * move);
#if UNITY_EDITOR
                        Debug.DrawRay(nextFootPosition + Vector3.up * (stepHeight + 0.01f), Vector3.down * stepHeight, Color.red);
#endif
                        if (Physics.Raycast(nextFootPosition + Vector3.up * (stepHeight + 0.01f), Vector3.down, out RaycastHit hit1, stepHeight, ~playerLayerMask, QueryTriggerInteraction.Ignore))
                        {

                            Vector3 stepOffset = (hit1.point + Vector3.down * 0.01f) - footPosition;
                            float stepDistance = stepOffset.magnitude;

                            if (stepDistance > 0)
                            {

                                Vector3 stepDir = stepOffset / stepDistance;

#if UNITY_EDITOR
                                Debug.DrawRay(footPosition, stepDir * stepDistance, Color.red);
#endif

                                if (Physics.Raycast(footPosition, stepDir, out RaycastHit hit2, stepDistance, ~playerLayerMask, QueryTriggerInteraction.Ignore))
                                {

#if UNITY_EDITOR
                                    Debug.DrawRay(hit2.point, hit2.normal, Vector3.Dot(hit2.normal, Vector3.up) < stepMaxDot ? Color.green : Color.red);
#endif

                                    if (Vector3.Dot(hit2.normal, Vector3.up) < stepMaxDot)
                                    {

                                        float step = Mathf.Max(0, (hit1.point.y - nextFootPosition.y) + 0.01f);
                                        //rigidPosition = rigidPosition + Vector3.up * Mathf.Lerp(step, Mathf.Min(step, Mathf.Max(move, step * deltaTime)), move);
                                        rigidPosition = rigidPosition + Vector3.up * step + stepDir * step;

                                    }

                                }

                            }

                        }

                    }

                    //

                    if (m_Rigidbody.Sweep(rigidPosition, m_Rigidbody.rotation, moveDir, out RaycastHit hit, move, ~playerLayerMask, QueryTriggerInteraction.Ignore))
                    {

                        float dot = 1 - Mathf.Max(0, Vector3.Dot(moveDir, -hit.normal));

                        float redirect = Mathf.Clamp01(dot / maxCollisionRedirectionDot);

                        //pushback = hit.normal * Mathf.Clamp((move - hit.distance) - collisionPadding, 0, move) * (1 - redirect);
                        //rigidPosition = rigidPosition + hit.normal * (move - hit.distance) * (1 - velocityOverride); 

                        //moveDir = Vector3.LerpUnclamped(moveDir, hit.normal, redirect);
                        moveDir = Vector3.RotateTowards(moveDir, hit.normal, 1.5708f * redirect, 1);

                    }

                    Vector3 floorMoveVectorMod = (moveDir * move);

                    m_Rigidbody.MovePosition(rigidPosition + (floorMoveVectorMod + (lastFloorNormal * -0.001f)) * (1 - velocityOverride));

                    Vector3 floorMoveVectorModVel = (moveDir * move) / deltaTime;

                    m_Rigidbody.AddForce(floorMoveVectorModVel * floorMoveAsForce * velocityOverride, ForceMode.Acceleration);

                    if (m_Grounded && m_CanJump) lastVelocityOverride = velocityOverride;

                }

            }

            if (IsHoldingJump || IsHoldingSprint)
            {

                if (TryVault(deltaTime, m_Rigidbody.position, m_Rigidbody.rotation)) return; 

            }

            if (ConsumeJumpToken())
            {

                if (CanJump)
                {

                    m_Grounded = false;
                    m_CanJump = false;

                    floorNormal = Vector3.zero;
                    floorContactCount = 0;

                    m_Rigidbody.AddForce(Vector3.up * powerJump, ForceMode.VelocityChange);

                    //floorMoveVector = floorMoveVector * jumpMoveBoost;

                }

            }

        }
        else
        {

            if (timeSinceWallTouch < specialJumpAirTimeout && (wallJumpToken || velocity.y < 0) && TrySpecialJump(deltaTime, m_Rigidbody.position, m_Rigidbody.rotation, WallJumpsBase, 1, !wallJumpToken)) 
            {

                wallJumpToken = false;

                return;

            }

            jumpToken = false; 

            climbTimer = Mathf.Max(0, climbTimer - deltaTime);

            if ((IsHoldingSprint/* || IsHoldingJump*/) && TryEdgeGrab())
            {

                return;

            }
            else if (IsHoldingJump && TryClimbWall()) 
            {
                
                return; 

            }
            
            // Apply player movement before leaving the ground to air velocity (only the part that was translation based, determined by 'lastVelocityOverride')
            //m_Rigidbody.AddForce(Quaternion.FromToRotation(Vector3.up, lastFloorNormal) * floorMoveVector * forceTransferOnFloorLoss * (1 - lastVelocityOverride), ForceMode.VelocityChange);
            m_Rigidbody.AddForce((Quaternion.FromToRotation(Vector3.up, lastFloorNormal) * floorMoveVector * forceTransferOnFloorLoss * (1 - lastVelocityOverride)) / math.pow(math.max(1, speedPlanar), floorAirForceTransferNerfMagnitudeExp), ForceMode.VelocityChange);
            floorMoveVector = Vector3.zero;
            //

            // Air movement

            velocity = m_Rigidbody.velocity;

            if (inputSlide)
            {

                // Redirect air velocity slightly when sliding

                float speed = velocity.magnitude;

                if (speed > 0)
                {

                    Vector3 direction = velocity / speed;

                    Vector3 airDirection = direction;
                    airDirection.y = 0;
                    airDirection = airDirection.normalized;

                    Vector3 airFacingDir = transform.forward;
                    airFacingDir.y = 0;
                    airFacingDir = airFacingDir.normalized;

                    Quaternion toForward = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(airDirection, airFacingDir), ((Vector3.Dot(airFacingDir, airDirection) + 1) * 0.5f) * deltaTime * slideRedirectionSrengthAir);

                    velocity = toForward * velocity;

                    m_Rigidbody.velocity = velocity;

#if UNITY_EDITOR
                    Debug.DrawRay(transform.position, velocity, Color.cyan, 5);
#endif

                }

                //

            }
            else
            {

                // Air strafe

                Vector3 xzVelocity = new Vector3(velocity.x, 0, velocity.z);

                float speed = xzVelocity.magnitude;
                float airControlFactor = 1;

                if (speed > 0)
                {

                    Vector3 direction = xzVelocity / speed;

                    airControlFactor = 1 - Mathf.Min(1, speed / airControlSpeed);

                    float airControlDirFactor = 1 - math.pow(1 - Mathf.Max(0, Vector3.Dot(inputDir, direction)), 3);

                    airControlFactor = Mathf.Lerp(1, airControlFactor, airControlDirFactor);

                }

                velocity = velocity + (inputDir * airControlSpeed * deltaTime * airControlFactor);

                //

            }

            m_Rigidbody.velocity = velocity;

            //

        }

    }

    protected int hangingGrabPointIndex = -1;
    protected int lastHangingGrabPointIndex = -1;
    protected Vector3 localHangingPos;
    protected Vector3 localHangingDir;

    public Vector3 GetReachOriginTop(Vector3 position, Quaternion rotation)
    {

        return (rotation * grabReachTopCenterOffset) + position;

    }

    public Vector3 GetReachOriginBot(Vector3 position, Quaternion rotation)
    {

        return (rotation * grabReachBotCenterOffset) + position;

    }

    public Vector3 GetBiasedGrabPosition(Vector3 position, Quaternion rotation)
    {

        return position + (rotation * (grabPointOrigin + Vector3.up * hangMaxDepth));

    }

    public Vector3 GetPositionFromBiasedGrabPosition(Vector3 biased, Quaternion rotation)
    {

        return biased - (rotation * (grabPointOrigin + Vector3.up * hangMaxDepth));

    }

    public Vector3 GetFootPosition(Vector3 position, Quaternion rotation, float offset = 0)
    {

        return position + rotation * Vector3.down * (footHeight + offset);

    }

    public Vector3 GetPositionFromFootPosition(Vector3 footPosition, Quaternion rotation, float offset = 0)
    {

        return footPosition + rotation * Vector3.up * (footHeight + offset);

    }

    public int GetClosestGrabPoint(float maxDistance, Vector3 position, Quaternion rotation, float minDot = 0.15f, Vector3 facingWorld = default, float minDotFacing = -2)
    {

        return GetClosestGrabPoint(maxDistance, position, rotation, rotation * Vector3.up, minDot, facingWorld, minDotFacing);

    }

    public int GetClosestGrabPoint(float maxDistance, Vector3 position, Quaternion rotation, Vector3 axisWorld, float minDot = 0.15f, Vector3 facingWorld = default, float minDotFacing = -2)
    {

        Vector3 startPoint = GetReachOriginTop(position, rotation);
        Vector3 startPointBiased = GetBiasedGrabPosition(position, rotation); // Pick points closest to where point of contact will be

        return GetClosestGrabPoint(maxDistance, startPoint, startPointBiased, axisWorld, minDot, facingWorld, minDotFacing);

    }

    public int GetClosestGrabPoint(float maxDistance, Vector3 startPoint, Vector3 startPointBiased, Vector3 axisWorld, float minDot = 0.15f, Vector3 facingWorld = default, float minDotFacing = -2, int ignoredIndex = -1, Collider lastCollider = null, bool withCollisionChecks = true, bool resetFilter = true)
    {

        if (withCollisionChecks && resetFilter) ResetGrabPointsFilter();

        bool facingLimited = facingWorld.x != 0 || facingWorld.y != 0 || facingWorld.z != 0;
        bool colliderPreferring = lastCollider != null;

        float distMin = float.MaxValue;
        int closest = -1;

        for (int a = 0; a < Mathf.Min(activeGrabPointsFilter.Length, activeGrabPoints2.Count); a++)
        {

            if (a == ignoredIndex || (withCollisionChecks && activeGrabPointsFilter[a])) continue;

            var gp = activeGrabPoints2[a];

            if (!gp.IsValid || !ValidateSurface(gp) || (facingLimited && Vector3.Dot(gp.NormFacingWorld, facingWorld) < minDotFacing)) continue;

            Vector3 posW = gp.PosWorld;

            Vector3 offset = (posW - startPoint);

            float dist = offset.magnitude;
            float biasedDistance = (posW - startPointBiased).magnitude;

            if (colliderPreferring && gp.collider == lastCollider) biasedDistance = biasedDistance * 0.9f;

            if (dist > 0)
            {

                offset = offset / dist;

                if (Vector3.Dot(offset, axisWorld) < minDot) continue; 

            }

            if (biasedDistance < distMin && dist <= maxDistance)
            {

                distMin = biasedDistance;
                closest = a;

            }
             
        }

        if (withCollisionChecks && closest >= 0) // Check if player would intersect colliders at ideal hanging position
        {

            GrabPoint closestGP = activeGrabPoints2[closest];

            Vector3 hangingDir = GetWorldHangingDir(closestGP);

            Quaternion rot = Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.forward, hangingDir, Vector3.up), Vector3.up);

            Vector3 idealPos = GetWorldHangingPos(GetPositionFromBiasedGrabPosition(closestGP.PosWorld, rot), rot, closestGP);

            if (m_Rigidbody.CheckCollider(idealPos, rot, ~playerLayerMask, QueryTriggerInteraction.Ignore))
            {

                activeGrabPointsFilter[closest] = true;

                return GetClosestGrabPoint(maxDistance, startPoint, startPointBiased, axisWorld, minDot, facingWorld, minDotFacing, ignoredIndex, lastCollider, withCollisionChecks, false);

            }

        }

        return closest;

    }

    public bool GrabClosestPoint(Vector3 position, Quaternion rotation, Vector3 facingWorld = default, float minDotFacing = -2, int ignoredIndex = -1, Collider lastCollider = null)
    {

        Vector3 grabPosition = GetReachOriginTop(position, rotation);
        Vector3 grabPositionBiased = GetBiasedGrabPosition(position, rotation); // Pick points closest to where point of contact will be

        return GrabClosestPoint(position, rotation, grabPosition, grabPositionBiased, facingWorld, minDotFacing, ignoredIndex, lastCollider);

    }

    public bool GrabClosestPoint(Vector3 position, Quaternion rotation, Vector3 grabPositionBiased, Vector3 facingWorld = default, float minDotFacing = -2, int ignoredIndex = -1, Collider lastCollider = null)
    {

        Vector3 grabPosition = GetReachOriginTop(position, rotation);

        return GrabClosestPoint(position, rotation, grabPosition, grabPositionBiased, facingWorld, minDotFacing, ignoredIndex, lastCollider);

    }

    public bool GrabClosestPoint(Vector3 position, Quaternion rotation, Vector3 grabPosition, Vector3 grabPositionBiased, Vector3 facingWorld = default, float minDotFacing = -2, int ignoredIndex = -1, Collider lastCollider = null)
    {

        hangingGrabPointIndex = -1;

        if (activeGrabPoints2 == null) return false;

        hangingGrabPointIndex = GetClosestGrabPoint(grabReachTop * grabReachClosestPointNerf, grabPosition, grabPositionBiased, rotation * Vector3.up, grabEdgeMinDot, facingWorld, minDotFacing, ignoredIndex, lastCollider);

        if (hangingGrabPointIndex < 0) return false;

        var grabPoint = activeGrabPoints2[hangingGrabPointIndex];

        localHangingPos = GetLocalHangingPos(position, rotation, grabPoint);
        localHangingDir = GetLocalHangingDir(grabPoint);

        return true;

    }

    public int GetHighestGrabPoint(float maxDistance, Vector3 position, Quaternion rotation, float minDot = 0.15f, Vector3 facingWorld = default, float minDotFacing = -2)
    {

        return GetHighestGrabPoint(maxDistance, position, rotation, rotation * Vector3.up, minDot, facingWorld, minDotFacing);

    }

    public int GetHighestGrabPoint(float maxDistance, Vector3 position, Quaternion rotation, Vector3 axisWorld, float minDot = 0.15f, Vector3 facingWorld = default, float minDotFacing = -2)
    {

        Vector3 startPoint = GetReachOriginTop(position, rotation);

        return GetHighestGrabPoint(maxDistance, startPoint, axisWorld, minDot, facingWorld, minDotFacing);

    }

    public int GetHighestGrabPoint(float maxDistance, Vector3 startPoint, Vector3 axisWorld, float minDot = 0.15f, Vector3 facingWorld = default, float minDotFacing = -2, int ignoredIndex = -1)
    {

        bool facingLimited = facingWorld.x != 0 || facingWorld.y != 0 || facingWorld.z != 0;

        float distMax = float.MinValue;
        float distMin = float.MaxValue;
        int highest = -1;

        for (int a = 0; a < activeGrabPoints2.Count; a++)
        {

            if (a == ignoredIndex) continue;

            var gp = activeGrabPoints2[a];

            if (!gp.IsValid || !ValidateSurface(gp) || (facingLimited && Vector3.Dot(gp.NormFacingWorld, facingWorld) < minDotFacing)) continue;

            Vector3 posW = gp.PosWorld;

            Vector3 offset = (posW - startPoint);

            float dist = offset.magnitude;
            float biasedDistance = posW.y;

            if (dist > 0)
            {

                offset = offset / dist;

                if (Vector3.Dot(offset, axisWorld) < minDot) continue;

            }

            if ((biasedDistance > distMax || (biasedDistance == distMax && dist < distMin)) && dist <= maxDistance)
            {

                distMax = biasedDistance;
                distMin = dist;
                highest = a;

            }

        }

        return highest;

    }

    public bool GrabHighestPoint(Vector3 position, Quaternion rotation, Vector3 facingWorld = default, float minDotFacing = -2, int ignoredIndex = -1)
    {

        Vector3 grabPosition = GetReachOriginTop(position, rotation);

        return GrabHighestPoint(position, rotation, grabPosition, facingWorld, minDotFacing, ignoredIndex);

    }

    public bool GrabHighestPoint(Vector3 position, Quaternion rotation, Vector3 grabPosition, Vector3 facingWorld = default, float minDotFacing = -2, int ignoredIndex = -1)
    {

        hangingGrabPointIndex = -1;

        if (activeGrabPoints2 == null) return false;

        hangingGrabPointIndex = GetHighestGrabPoint(grabReachTop, grabPosition, rotation * Vector3.up, grabEdgeMinDot, facingWorld, minDotFacing, ignoredIndex);

        if (hangingGrabPointIndex < 0) return false;

        var grabPoint = activeGrabPoints2[hangingGrabPointIndex];

        localHangingPos = GetLocalHangingPos(position, rotation, grabPoint);
        localHangingDir = GetLocalHangingDir(grabPoint);

        return true;

    }

    public Vector3 GetWorldHangingPos(Vector3 position, Quaternion rotation, GrabPoint grabPoint)
    {

        Vector3 wp = grabPoint.PosWorld;
        Plane facingPlane = new Plane(-grabPoint.NormFacingWorld, wp);

        Vector3 pos = GetBiasedGrabPosition(position, rotation);
        pos = facingPlane.ClosestPointOnPlane(pos);

        /*pos = (pos - wp);
        float mag = pos.magnitude;
        if (mag > 0)
        {
            pos = pos / mag;
            mag = Mathf.Min(mag, (grabPointOrigin.magnitude + hangMaxDepth) * 0.8f);
            pos = pos * mag;
        }
        pos = wp + pos;*/

        pos = pos - grabPoint.NormFacingWorld * thickness;

        return pos;

    }

    public Vector3 GetWorldHangingDir(GrabPoint grabPoint)
    {

        Vector3 fwd = grabPoint.NormFacingWorld;
        fwd.y = 0;
        fwd = fwd.normalized;

        return fwd;

    }

    public Vector3 GetLocalHangingPos(Vector3 position, Quaternion rotation, GrabPoint grabPoint) 
    {

        Vector3 pos = GetWorldHangingPos(position, rotation, grabPoint);

        if (grabPoint.transform != null) return grabPoint.transform.InverseTransformPoint(pos);
        
        return pos;

    } 

    public Vector3 GetLocalHangingDir(GrabPoint grabPoint)
    {

        Vector3 dir = GetWorldHangingDir(grabPoint);

        if (grabPoint.transform != null) return grabPoint.transform.InverseTransformDirection(dir);

        return dir;

    }

    public bool TryEdgeGrabMantle(float timeModifier = 1, TransitionSpeedMultiplierDelegate speedMultiplierDelegate = null, TransitionValidationDelegate validatorDelegate = null)
    {

        if (m_State != State.Default && m_State != State.Climbing && m_State != State.Hanging) return false;

        Vector3 position = m_Rigidbody.position;
        Quaternion rotation = m_Rigidbody.rotation;

        AddGrabPoints(position, rotation, ~playerLayerMask);

        TryPruneGrabPoints(GetBiasedGrabPosition(position, rotation));

        if (!GrabClosestPoint(position, rotation)) return false;

        var gp = activeGrabPoints2[hangingGrabPointIndex];

        if (!CanMantle(rotation, gp, out Vector3 footPosition)) return false;

        SetState(State.Hanging);

        EdgeMantle(footPosition, rotation, gp, timeModifier, speedMultiplierDelegate, 
            (float deltaTime) => WallEdgeMantleValidator(deltaTime) || (validatorDelegate == null ? false : validatorDelegate(deltaTime)),          
            (float deltaTime) => TrySpecialJump(deltaTime, Vector3.zero, Quaternion.identity, null), (float deltaTime) => {

                m_Rigidbody.velocity = Vector3.zero;

                EdgeJumpsBase(deltaTime, m_Rigidbody.position, m_Rigidbody.rotation, specialJumpMantleForceBoost); 
            
            }, State.Default, false);

        return true; 

    }

    public bool TryEdgeGrab()
    {

        if (m_State != State.Default && m_State != State.Climbing) return false;

        Vector3 position = m_Rigidbody.position;
        Quaternion rotation = m_Rigidbody.rotation;

        //RecalculateGrabPoints(position, rotation, ~playerLayerMask);

        AddGrabPoints(position, rotation, ~playerLayerMask);

        TryPruneGrabPoints(GetBiasedGrabPosition(position, rotation));

        if (!GrabClosestPoint(position, rotation)) return false;
        //if (!GrabHighestPoint(position, rotation)) return false;

        var gp = activeGrabPoints2[hangingGrabPointIndex];

        if (!ConsumeSprintToken() && !gp.canMantle) return false;

        TransitionTo(State.Hanging, GetFootPosition(position, rotation, footHeightFloorlessOffset), gp.NormFacingWorld, 0.02f, gp.transform);

        return true;

    }

    protected bool[] activeGrabPointsFilter = new bool[100];
    protected void ResetGrabPointsFilter()
    {

        for (int i = 0; i < Mathf.Min(activeGrabPointsFilter.Length, activeGrabPoints2.Count); i++) activeGrabPointsFilter[i] = false;

    }

    public enum ShimmyState
    {

        Default, Corner, NoNeighbors

    }

    protected bool desperateForGrabPoints;

    public Vector3 GetWallRelativeInput(Vector3 wallFacingNormal, Vector3 surfaceNormal, out float inputMagnitude)
    {

        return GetWallRelativeInput(cameraTransform.forward, CameraAlignedInputDirection, wallFacingNormal, surfaceNormal, new Plane(-wallFacingNormal, Vector3.zero), out inputMagnitude);

    }

    public Vector3 GetWallRelativeInput(Vector3 cameraForward, Vector3 cameraAlignedInput, Vector3 wallFacingNormal, Vector3 surfaceNormal, out float inputMagnitude)
    {

        return GetWallRelativeInput(cameraForward, cameraAlignedInput, wallFacingNormal, surfaceNormal, new Plane(-wallFacingNormal, Vector3.zero), out inputMagnitude);

    }

    public Vector3 GetWallRelativeInput(Vector3 cameraForward, Vector3 cameraAlignedInput, Vector3 wallFacingNormal, Vector3 surfaceNormal, Plane wallOutwardFacingPlane, out float inputMagnitude)
    {

        Vector3 wallRelativeInputDir = cameraAlignedInput;

        float verVal = Vector3.Dot(wallRelativeInputDir, wallFacingNormal) * Mathf.Abs(Vector3.Dot(wallRelativeInputDir, cameraForward)); // When hanging on edge -> Forward = Up, Backward = Down
        wallRelativeInputDir = wallOutwardFacingPlane.ClosestPointOnPlane(wallRelativeInputDir); // Make relative to grab point wall normal

        //inputDir = (rigidRotation * (Vector3.up * (IsHoldingJump ? 1 : verVal))) + inputDir;
        wallRelativeInputDir = (surfaceNormal * (IsHoldingJump ? 1 : verVal)) + wallRelativeInputDir; // Convert forward and back movement into up and down movement. Also apply jump input as upward movement.

        inputMagnitude = wallRelativeInputDir.magnitude;

        if (inputMagnitude > 0) wallRelativeInputDir = wallRelativeInputDir / inputMagnitude;

        return wallRelativeInputDir;

    }

    public Vector3 GetWallRelativeInput(Vector3 wallFacingNormal, Vector3 surfaceNormal)
    {

        return GetWallRelativeInput(cameraTransform.forward, CameraAlignedInputDirection, wallFacingNormal, surfaceNormal, new Plane(-wallFacingNormal, Vector3.zero), out _);

    }

    public Vector3 GetWallRelativeInput(Vector3 cameraForward, Vector3 cameraAlignedInput, Vector3 wallFacingNormal, Vector3 surfaceNormal)
    {

        return GetWallRelativeInput(cameraForward, cameraAlignedInput, wallFacingNormal, surfaceNormal, new Plane(-wallFacingNormal, Vector3.zero), out _);

    }

    public Vector3 GetWallRelativeInput(Vector3 cameraForward, Vector3 cameraAlignedInput, Vector3 wallFacingNormal, Vector3 surfaceNormal, Plane wallOutwardFacingPlane)
    {

        return GetWallRelativeInput(cameraForward, cameraAlignedInput, wallFacingNormal, surfaceNormal, wallOutwardFacingPlane, out _);

    }

    protected void EdgeHang(float deltaTime)
    {

        if (m_State != State.Hanging) return;

        Vector3 startRigidPosition, rigidPosition;
        Quaternion startRigidRotation, rigidRotation;

        rigidPosition = startRigidPosition = m_Rigidbody.position;
        rigidRotation = startRigidRotation = m_Rigidbody.rotation;

        if (activeGrabPoints2.Count > 100) activeGrabPoints2.RemoveRange(100, activeGrabPoints2.Count - 100);

        if (
            hangingGrabPointIndex < 0 || // Index out of range
            hangingGrabPointIndex >= activeGrabPoints2.Count ||  // Index out of range
            ((GetReachOriginTop(rigidPosition, rigidRotation) - activeGrabPoints2[hangingGrabPointIndex].PosWorld).sqrMagnitude > (grabReachTop * grabReachTop)) || // Grab point out of reach
            !ValidateSurface(activeGrabPoints2[hangingGrabPointIndex]) // Grab point surface has too much incline
            )
        {

            GrabClosestPoint(rigidPosition, rigidRotation, rigidRotation * Vector3.forward, grabMinFacingDot);

        }

        if ((!IsHoldingSprint/* && !IsHoldingJump*/) || hangingGrabPointIndex < 0 || hangingGrabPointIndex >= activeGrabPoints2.Count) // Player no longer wants to hang or grab point was lost
        {
            
            SetState(State.Default);

            return;

        }

        climbToken = true;

        if (HandleEdgeJumps(deltaTime, rigidPosition, rigidRotation)) 
        {

            wallJumpToken = false;
            
            return; 
        
        }

        if (IsHoldingJump && TryEdgeMantle()) return; 

        Vector3 velocity = m_Rigidbody.velocity;

        velocity *= Mathf.Clamp01(1f - hangVelocityDrag * deltaTime);

        if (velocity.sqrMagnitude < 0.001f) velocity = Vector3.zero;

        var grabPoint = activeGrabPoints2[hangingGrabPointIndex];

        Vector3 grabPointPosWorld = grabPoint.PosWorld;
        Vector3 grabPointFacingNormalWorld = grabPoint.NormFacingWorld;
        Vector3 grabPointNormalWorld = grabPoint.NormWorld;

#if UNITY_EDITOR
        Debug.DrawLine(grabPointPosWorld, GetBiasedGrabPosition(rigidPosition, rigidRotation), Color.blue);
#endif

        Plane grabPointOutwardFacingPlane = new Plane(-grabPointFacingNormalWorld, Vector3.zero);

        Vector3 worldHangingPos = Vector3.zero;
        Quaternion worldHangingRot = Quaternion.identity;

        void RecalculateWorldHangingPosition()
        {

            if (grabPoint.transform == null)
            {

                worldHangingPos = localHangingPos;

            }
            else
            {

                worldHangingPos = grabPoint.transform.TransformPoint(localHangingPos);

            }

        }

        // Force hanging position relative to grab point
        float forcePositionDelta = hangForcePositionStrengthRuntime * deltaTime;

        startRigidRotation = rigidRotation;
        if (grabPoint.transform == null)
        {

            worldHangingRot = Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.forward, localHangingDir, Vector3.up), Vector3.up);

            rigidRotation = Quaternion.SlerpUnclamped(rigidRotation, worldHangingRot, forcePositionDelta);

        } 
        else
        {

            Vector3 fwd = grabPoint.transform.TransformDirection(localHangingDir);
            fwd.y = 0;
            fwd = fwd.normalized;
            worldHangingRot = Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.forward, fwd, Vector3.up), Vector3.up);

            rigidRotation = Quaternion.SlerpUnclamped(rigidRotation, worldHangingRot, forcePositionDelta);

        }

        if (hangingGrabPointIndex != lastHangingGrabPointIndex)
        {

            hangChangeDirFrequencyNerf = math.saturate(hangChangeDirFrequencyNerf + (((Quaternion.Angle(startRigidRotation, rigidRotation) * hangChangeDirFrequencyNerfFactor) - hangChangeDirFrequencyNerfDecay) * deltaTime));
            rigidRotation = Quaternion.Slerp(rigidRotation, startRigidRotation, hangChangeDirFrequencyNerf);

        }

        m_Rigidbody.MoveRotation(rigidRotation);

        localHangingPos = GetLocalHangingPos(rigidPosition, rigidRotation, grabPoint);
        RecalculateWorldHangingPosition();

        Vector3 offset = (worldHangingPos - GetBiasedGrabPosition(rigidPosition, rigidRotation));

        hangForcePositionStrengthRuntime = CalculateHangForcePositionStrength(offset.magnitude);

        startRigidPosition = rigidPosition;
        rigidPosition = rigidPosition + offset * Mathf.Clamp01(forcePositionDelta);
        MoveTo(startRigidPosition, rigidRotation, ref rigidPosition);
        //

        // PLAYER INPUT
        //Vector2 input = new Vector2(inputX, IsHoldingJump ? 1 : inputY);

        //Vector3 inputDir = rigidRotation * ((Vector3.right * input.x) + (Vector3.up * input.y));
        Vector3 inputDir = GetWallRelativeInput(cameraTransform.forward, CameraAlignedInputDirection, grabPointFacingNormalWorld, grabPointNormalWorld, grabPointOutwardFacingPlane, out float inputMag);
        //

        // Fall below ledge when hanging if not actively moving vertically
        //if (input.y == 0)
        if (Mathf.Abs(inputDir.y) <= 0.1f) 
        {

            Vector3 grabOrigin = GetBiasedGrabPosition(rigidPosition, rigidRotation);
            float hangDepth = grabPointPosWorld.y - grabOrigin.y;
            if (hangDepth < hangMaxDepth)
            {

                rigidPosition = rigidPosition + grabPoint.InverseTransformDirection(Vector3.down) * Mathf.Min(deltaTime * 1.5f, Mathf.Abs(hangMaxDepth - hangDepth));

                localHangingPos = GetLocalHangingPos(rigidPosition, rigidRotation, grabPoint);
                //RecalculateWorldHangingPosition();

            }
            else if (hangDepth > hangMaxDepth)
            {

                rigidPosition = rigidPosition + grabPoint.InverseTransformDirection(Vector3.up) * Mathf.Min(deltaTime * 1.5f, Mathf.Abs(hangMaxDepth - hangDepth));

                localHangingPos = GetLocalHangingPos(rigidPosition, rigidRotation, grabPoint);
                //RecalculateWorldHangingPosition();

            }

        }
        //

        lastHangingGrabPointIndex = hangingGrabPointIndex;

        startRigidPosition = rigidPosition;
        startRigidRotation = rigidRotation;

        float shimmyUpwardDot = 1;

        if (inputMag > 0)
        {

            if (activeGrabPoints2.Count <= 1) FindGrabPoints(rigidPosition, rigidRotation);

            shimmyUpwardDot = Vector3.Dot(inputDir, grabPointNormalWorld);

            ResetGrabPointsFilter();

            activeGrabPointsFilter[hangingGrabPointIndex] = true;
             
            ShimmyState Shimmy(out GrabPoint neighborGP, out Vector3 shimmyVector)
            {

                Vector3 grabOrigin = GetBiasedGrabPosition(rigidPosition, rigidRotation);

                shimmyVector = inputDir;

                neighborGP = default;
                int closestNeighbor = -1;

                Vector3 debugOffset = Vector3.zero;

                float minDist = float.MaxValue;
                float maxDot = 0.65f;
                float shimmyDot = 1;
                bool isPrioritizedCorner = false;
                bool isCorner = false;
                for (int i = 0; i < Mathf.Min(activeGrabPointsFilter.Length, activeGrabPoints2.Count); i++)
                {

                    if (activeGrabPointsFilter[i]) continue;

                    var nearbyGrabPoint = activeGrabPoints2[i];

                    if (!nearbyGrabPoint.IsValid || !ValidateSurface(nearbyGrabPoint)) continue;

                    Vector3 nearbyPosWorld = nearbyGrabPoint.PosWorld;

                    Vector3 offsetA = (nearbyPosWorld - grabPointPosWorld);
                    Vector3 offsetB = nearbyPosWorld - grabOrigin;
                    float distA = offsetA.magnitude;
                    float distB = offsetB.magnitude;

                    if (distA <= 0 || distB <= 0 || distA > grabReachTop) continue;

                    offsetA = offsetA / distA;
                    offsetB = offsetB / distB;
                    float facingDot = Vector3.Dot(grabPointFacingNormalWorld, nearbyGrabPoint.NormFacingWorld);

                    if (facingDot < shimmyCornerMinDot) continue;

                    distB = distB + (distB * shimmyCornerCostRatio * (1 - facingDot));

                    Vector3 offset2D = grabPointOutwardFacingPlane.ClosestPointOnPlane(offsetA);
                    float mag2D = offset2D.magnitude;

                    float dot = mag2D <= 0 ? (distA < shimmyCornerPrioritizationDistance ? 0.965f : shimmyMinDot) : Vector3.Dot(offset2D / mag2D, inputDir);

                    if (dot < shimmyMinDot) continue;

                    bool isCorner_ = facingDot <= shimmyCornerMaxDot;
                    float cornerPriority = 1;// math.pow((shimmyCornerMaxDot - facingDot) / (shimmyCornerMaxDot - shimmyCornerMinDot), 0.5f);

                    bool c1 = isCorner && isCorner_ ? distB < minDist : dot > maxDot;
                    bool c2 = (dot == maxDot && distB < minDist) && (!isPrioritizedCorner || isCorner_);
                    bool c3 = (isCorner_ && !isPrioritizedCorner) && (distA < (shimmyCornerPrioritizationDistance * cornerPriority) && facingDot < shimmyDot && (dot > maxDot - shimmyCornerDotLeeway));

                    if (c1 || c2 || c3) 
                    {
                        debugOffset = offsetB;
                        minDist = distB;
                        maxDot = dot + (c3 ? shimmyCornerDotLeeway : 0);
                        closestNeighbor = i;
                        shimmyDot = facingDot;
                        isPrioritizedCorner = c3 || isPrioritizedCorner;
                        isCorner = isCorner_;

                    }

                }

                shimmyVector = shimmyVector * shimmySpeed;

                if (closestNeighbor >= 0)
                {

                    neighborGP = activeGrabPoints2[closestNeighbor];

                    /*if (isCorner)
                    {

                        Vector3 hangingDir = GetLocalHangingDir(neighbor);

                        if (neighbor.transform != null)
                        {

                            hangingDir = neighbor.transform.TransformDirection(hangingDir);
                            hangingDir.y = 0;
                            hangingDir = hangingDir.normalized;

                        }

                        Quaternion rot = Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.forward, hangingDir, Vector3.up), Vector3.up);
                        Quaternion cornerRot = Quaternion.FromToRotation(grabPointFacingNormalWorld, neighbor.NormFacingWorld);
                        //Vector3 pos = rigidPosition + (((cornerRot * (worldHangingPos - grabPointPosWorld)) + neighbor.PosWorld) - (rigidPosition + (rot * grabPointOrigin)));
                        Vector3 pos = rigidPosition + (((cornerRot * (GetBiasedGrabPosition(rigidPosition, rot) - grabPointPosWorld)) + neighbor.PosWorld) - GetBiasedGrabPoint(rigidPosition, rot));

                        Vector3 idealPos = neighbor.TransformPoint(GetLocalHangingPos(GetPositionFromBiasedGrabPosition(neighbor.PosWorld, rot), rot, neighbor));

                        GrabPoint neighbor_ = neighbor;

                        if (!m_Rigidbody.CheckCollider(idealPos, rot, -1, QueryTriggerInteraction.Ignore))
                        {

                            Debug.Log("corner : " + shimmyDot + " : " + isPrioritizedCorner + " : " + maxDot);
                            Debug.DrawRay(grabPointPosWorld, shimmyDir * 5, Color.magenta, 15);
                            Debug.DrawRay(grabPointPosWorld, debugOffset * 5, Color.red, 15);

                            TransitionTo(m_State, GetFootPosition(pos, rot, footHeightFloorlessOffset), hangingDir, shimmyCornerTime, neighbor.transform, null,

                                () =>
                                {

                                    // Quick fix for getting stuck on corners with no grab point to shimmy to

                                    Vector3 shimmyOffset = (cornerRot * shimmyDir) * 0.0025f;

                                    GrabPoint shimmyProxy = neighbor_;
                                    shimmyProxy.localPoint = shimmyProxy.InverseTransformPoint(neighbor_.PosWorld + shimmyOffset);
                                    activeGrabPoints2.Add(shimmyProxy);

                                    //

                                    localHangingPos = GetLocalHangingPos(m_Rigidbody.position, m_Rigidbody.rotation, neighbor_);
                                    localHangingDir = GetLocalHangingDir(neighbor_);
                                    hangingGrabPointIndex = closestNeighbor;
                                    //GrabClosestPoint(m_Rigidbody.position, m_Rigidbody.rotation, m_Rigidbody.rotation * Vector3.back, 0.15f);

                                }

                                , true, () => { return (!IsHoldingSprint && !IsHoldingJump) || !ValidateSurface(neighbor_); }, State.Default);

                            Debug.DrawLine(grabPointPosWorld, neighbor.PosWorld, Color.green, 15);
                            Debug.DrawRay(grabPointPosWorld, -grabPoint.NormFacingWorld, Color.cyan, 15);
                            Debug.DrawRay(neighbor.PosWorld, -neighbor.NormFacingWorld, Color.cyan, 15);

                            return ShimmyState.Corner;

                        }
                        else
                        {

                            activeGrabPointsFilter[closestNeighbor] = true;

                            return Shimmy(out neighbor, out shimmyVector);

                        }

                    }*/

                    Vector3 hangingDir = GetWorldHangingDir(neighborGP);
                    Quaternion rot = Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.forward, hangingDir, Vector3.up), Vector3.up);
                    Vector3 idealPos = GetWorldHangingPos(GetPositionFromBiasedGrabPosition(neighborGP.PosWorld, rot), rot, neighborGP);

                    if (m_Rigidbody.CheckCollider(idealPos, rot, ~playerLayerMask, QueryTriggerInteraction.Ignore))
                    {

                        activeGrabPointsFilter[closestNeighbor] = true;

                        return Shimmy(out neighborGP, out shimmyVector);  

                    }

                    Vector3 neighborHangingPos = GetWorldHangingPos(rigidPosition, rigidRotation, neighborGP);
                    Vector3 neighborOffset = (neighborHangingPos - GetBiasedGrabPosition(rigidPosition, rigidRotation));

                    float forceStrength = CalculateHangForcePositionStrength(neighborOffset.magnitude) * deltaTime;

                    rigidRotation = Quaternion.Slerp(rigidRotation, Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.forward, GetWorldHangingDir(neighborGP), Vector3.up), Vector3.up), forceStrength);
                    m_Rigidbody.MoveRotation(rigidRotation);

                    return ShimmyState.Default;

                }
                else
                {

                    return ShimmyState.NoNeighbors;

                }

            }

            void ApplyShimmyVector(Vector3 shimmyVector)
            {

                startRigidPosition = rigidPosition;
                rigidPosition = rigidPosition + shimmyVector * deltaTime;

                float distance = (GetReachOriginTop(rigidPosition, rigidRotation) - grabPointPosWorld).magnitude;

                float falloff = math.max(1, math.pow(distance, shimmySpeedFalloffFactor) / grabReachTop);

                rigidPosition = startRigidPosition + ((rigidPosition - startRigidPosition) / falloff);

                float distance2 = (GetBiasedGrabPosition(rigidPosition, rigidRotation) - grabPointPosWorld).magnitude;
                 
                if (shimmyUpwardDot > 0) // Stop player from letting go of ledge when moving up
                {

                    float nerfDistance = (grabReachTop * shimmyUpwardsNerfDistanceRatio);
                    float reach = grabReachTop - nerfDistance;  
                    float t = math.saturate((distance2 - reach) / (nerfDistance * shimmyUpwardsNerfDivisorRatio));

                    rigidPosition = Vector3.LerpUnclamped(rigidPosition, startRigidPosition, t);

                }

                MoveTo(startRigidPosition, rigidRotation, ref rigidPosition);

            }

            MoveTo(startRigidPosition, rigidRotation, ref rigidPosition); 

            int currentIndex = hangingGrabPointIndex;
            Plane grabPointPlane = new Plane(-grabPointFacingNormalWorld, grabPointPosWorld);
            switch (Shimmy(out GrabPoint neighborPoint, out Vector3 shimmyVector))
            {

                default:



                    ApplyShimmyVector(shimmyVector);
#if UNITY_EDITOR
                    Debug.DrawRay(transform.position, shimmyVector, Color.red, 0.5f);
#endif

                    Plane neighborPointPlane = new Plane(-neighborPoint.NormFacingWorld, neighborPoint.PosWorld);
                    //GrabClosestPoint(rigidPosition, rigidRotation, Maths.GetPointOnSegment(GetBiasedGrabPoint(rigidPosition, rigidRotation), grabPointPosWorld, neighborPoint.PosWorld), neighborPoint.NormFacingWorld, 0.36f, -1);
                    GrabClosestPoint(rigidPosition, rigidRotation, neighborPointPlane.ClosestPointOnPlane(GetBiasedGrabPosition(rigidPosition, rigidRotation)), neighborPoint.NormFacingWorld, 0.36f, -1, grabPoint.collider);

                    if (hangingGrabPointIndex < 0)
                    {

                        hangingGrabPointIndex = currentIndex;

                        if (desperateForGrabPoints)
                        {

                            FindGrabPointsGreedy(rigidPosition, rigidRotation, ~playerLayerMask);

                        }
                        else
                        {

                            AddGrabPoints(rigidPosition, rigidRotation, ~playerLayerMask, true, grabPointMergeThreshold * 0.5f);
                            desperateForGrabPoints = true;

                        }

                        TryPruneGrabPoints(GetBiasedGrabPosition(rigidPosition, rigidRotation));

                    }
                    else 
                    {

                        if (desperateForGrabPoints || hangingGrabPointIndex != currentIndex) 
                        {
                            
                            AddGrabPoints(rigidPosition, rigidRotation, ~playerLayerMask, true, grabPointMergeThreshold * 0.5f);

                            TryPruneGrabPoints(GetBiasedGrabPosition(rigidPosition, rigidRotation));

                        }

                        desperateForGrabPoints = false;

                    }

                    break;

                case ShimmyState.Corner:
                    return;

                case ShimmyState.NoNeighbors:

                    ApplyShimmyVector(shimmyVector);
#if UNITY_EDITOR
                    Debug.DrawRay(transform.position, shimmyVector, Color.blue, 0.5f);
#endif
                    // Mantle over edges with nothing to grab above them when holding forward
                    //if (Vector3.Dot(shimmyDir, Vector3.up) > 0.15f && TryEdgeMantle(() => inputY <= 0)) return;
                    if (shimmyUpwardDot > 0.15f && CanMantle(rigidRotation, grabPoint, out Vector3 footPosition))
                    {

                        float localMantleTime = ((footPosition - (GetFootPosition(rigidPosition, rigidRotation, footHeightFloorlessOffset))).magnitude / shimmySpeed);

                        jumpToken = false;

                        EdgeMantle(footPosition, rigidRotation, grabPoint, localMantleTime / MantleTimeModified, 
                            
                        (float deltaTime) =>
                        {

                            return (transitionProgress > transitionTime * mantleCommitRatio || QueryJumpInput()) ? (localMantleTime / MantleTimeModified) : (1 + (transitionProgress / transitionTime) * 3);

                        },

                        WallEdgeMantleValidator, (float deltaTime) => TrySpecialJump(deltaTime, Vector3.zero, Quaternion.identity, null), (float deltaTime) => {

                            m_Rigidbody.velocity = Vector3.zero;

                            EdgeJumpsBase(deltaTime, m_Rigidbody.position, m_Rigidbody.rotation, specialJumpMantleForceBoost);

                        }, State.Default, false); 

                        return;

                    }
                    //

                    if (desperateForGrabPoints)  
                    {

                        FindGrabPointsGreedy(rigidPosition + shimmyVector, rigidRotation, ~playerLayerMask);

                    } 
                    else 
                    {

                        AddGrabPoints(rigidPosition + shimmyVector, rigidRotation, ~playerLayerMask, true, grabPointMergeThreshold * 0.5f);
                        desperateForGrabPoints = true;

                        //GrabClosestPoint(rigidPosition, rigidRotation, grabPointPlane.ClosestPointOnPlane(GetBiasedGrabPoint(rigidPosition, rigidRotation)), neighborPoint.NormFacingWorld, 0.36f, -1);

                        //if (hangingGrabPointIndex < 0) hangingGrabPointIndex = currentIndex;

                    }

                    TryPruneGrabPoints(GetBiasedGrabPosition(rigidPosition, rigidRotation));

                    break;

            }

        }

        m_Rigidbody.velocity = velocity;
        m_Rigidbody.MovePosition(rigidPosition);
        m_Rigidbody.MoveRotation(rigidRotation);

    }

    public bool CanMantle(Quaternion rotation, GrabPoint grabPoint, out Vector3 footPosition)
    {

        footPosition = grabPoint.PosWorld + Vector3.up * 0.005f;

        if (!grabPoint.canMantle) return false;

        if (m_Rigidbody.CheckCollider(GetPositionFromFootPosition(footPosition, rotation, footHeightFloorlessOffset), rotation, ~playerLayerMask, QueryTriggerInteraction.Ignore)) return false;

        return true;

    }

    protected bool TryEdgeMantle(float timeModifier = 1, TransitionSpeedMultiplierDelegate speedMultiplierDelegate = null, TransitionValidationDelegate validatorDelegate = null)
    {

        if (m_State != State.Hanging || hangingGrabPointIndex < 0) return false;

        var gp = activeGrabPoints2[hangingGrabPointIndex];

        Quaternion rotation = m_Rigidbody.rotation;

        if (!CanMantle(rotation, gp, out Vector3 footPosition)) return false;

        EdgeMantle(footPosition, rotation, gp, timeModifier, speedMultiplierDelegate, (float deltaTime) => WallEdgeMantleValidator(deltaTime) || (validatorDelegate == null ? false : validatorDelegate(deltaTime)), (float deltaTime) => TrySpecialJump(deltaTime, Vector3.zero, Quaternion.identity, null), (float deltaTime) => {

            m_Rigidbody.velocity = Vector3.zero;

            EdgeJumpsBase(deltaTime, m_Rigidbody.position, m_Rigidbody.rotation, specialJumpMantleForceBoost);

        }, State.Default, false);

        return true; 

    }

    protected bool WallEdgeMantleValidator(float deltaTime)
    {

        if (hangingGrabPointIndex < 0) return false;

        var gp = activeGrabPoints2[hangingGrabPointIndex];
        Vector3 norm = gp.NormWorld;

        //return ((IsHoldingSprint || IsHoldingJump) && Vector3.Dot(GetWallRelativeInput(gp.NormFacingWorld, norm), norm) > 0.15f);
        return IsHoldingJump || Vector3.Dot(GetWallRelativeInput(gp.NormFacingWorld, norm), norm) > 0.15f;

    }

    protected void EdgeMantle(Vector3 footPosition, Quaternion rotation, GrabPoint grabPoint, float timeModifier = 1, TransitionSpeedMultiplierDelegate speedMultiplierDelegate = null, TransitionValidationDelegate validatorDelegate = null, TransitionValidationDelegate cancelDelegate = null, TransitionCompletionDelegate onCancelDelegate = null, State onCancelState = State.Default, bool setStateOnCancel = true)
    {

        jumpToken = false; // Allow for wall jumps

        TransitionTo(State.Default, footPosition, rotation * Vector3.forward, MantleTimeModified * timeModifier, grabPoint.transform, 
            
            (float deltaTime) => { // Allows shimmy during mantle

                Vector3 gpPos = grabPoint.PosWorld;

                Vector3 gpNormal = grabPoint.NormWorld;
                Vector3 gpFacingNormal = grabPoint.NormFacingWorld;

                Plane gpPlane = new Plane(-gpFacingNormal, Vector3.zero);
                Plane gpPlaneSurface = new Plane(gpNormal, Vector3.zero);

                Vector3 inputDir = GetWallRelativeInput(cameraTransform.forward, CameraAlignedInputDirection, gpFacingNormal, gpNormal, gpPlane, out float inputMag);
                inputDir = gpPlaneSurface.ClosestPointOnPlane(inputDir);

                inputDir = inputDir.normalized;

                Vector3 topOrigin = GetReachOriginTop(m_Rigidbody.position, m_Rigidbody.rotation);

                float factor = 1 - Mathf.Clamp01(((gpPos - topOrigin).magnitude - (grabReachTop * 0.8f)) / (grabReachTop * 0.2f));

                Vector3 move = inputDir * factor * shimmySpeed * deltaTime;

                if (transitionTargetAnchorTransform != null) move = transitionTargetAnchorTransform.InverseTransformVector(move);

                transitionTargetFootPosition = transitionTargetFootPosition + move;

            },
             
            speedMultiplierDelegate,

            (float deltaTime) => (ValidateSurface(grabPoint) && ((transitionProgress > transitionTime * mantleCommitRatio) || QueryJumpInput() || (validatorDelegate == null ? false : validatorDelegate(deltaTime)))), 
            
            (float deltaTime) =>
            {

                jumpToken = false; // Stop accidental jumps after mantle

                m_Rigidbody.velocity = Vector3.zero; 

            }, true, cancelDelegate, onCancelState, onCancelDelegate, setStateOnCancel);

    }

    [Header("Vaulting")]
    public float vaultReach = 1.25f;
    public Vector3 vaultCenterOffset = Vector3.zero;
    public float vaultTimeHeightRatio = 0.5f;
    public float vaultMaxDistance = 1;
    [Tooltip("Maximum height allowed for obstacles that can be vaulted over")]
    public float vaultMaxHeight = 1.45f;
    [Tooltip("Minimum height allowed for obstacles that can be vaulted over")]
    public float vaultMinHeight = 0.4f;
    [Tooltip("Maximum height difference allowed betweem the floor at the start of the vault and the floor at the end")]
    public float vaultMaxHeightDifference = 0.4f;
    public int vaultPrecision = 7;

    protected Vector3 GetVaultOriginPoint(Vector3 position, Quaternion rotation) => position + (rotation * vaultCenterOffset);

    protected bool TryVault(float deltaTime, Vector3 position, Quaternion rotation) 
    {

        if (wallContactCount <= 0) return false;

        RecalculateGrabPoints(position, rotation, ~playerLayerMask);

        if (activeGrabPoints2.Count <= 0) return false;

        Vector3 vaultOrigin = GetVaultOriginPoint(position, rotation);
        
        int grabIndex = GetClosestGrabPoint(vaultReach, vaultOrigin, vaultOrigin, rotation * Vector3.up, float.MinValue, rotation * Vector3.forward, 0.7071f, -1, null, false);

        if (grabIndex < 0) return false;

        GrabPoint gp = activeGrabPoints2[grabIndex];

        Vector3 vaultAnchorPosition = gp.PosWorld;
        Vector3 vaultDirection = gp.NormFacingWorld;

        Vector3 footPosition = GetFootPosition(position, rotation, footHeightFloorlessOffset);

        float obstacleHeight = vaultAnchorPosition.y - footPosition.y;  
        if (obstacleHeight > vaultMaxHeight || obstacleHeight <= Mathf.Max(vaultMinHeight, stepHeight)) return false;

        Vector3 startPosition = footPosition + (rotation * Vector3.forward * thickness) + Vector3.up * (vaultMaxHeight + 0.1f);

        Vector3 GetVaultPosition(Vector3 hitPoint)
        {

            return GetPositionFromFootPosition(hitPoint, rotation, 0.0251f);

        }

        float minDist = float.MaxValue;
        RaycastHit closestHit = default;

        for (int i = 0; i < vaultPrecision; i++)
        {

            float t = i / (float)vaultPrecision;

            Vector3 pos = startPosition + vaultDirection * t * vaultMaxDistance; 

#if UNITY_EDITOR
            Debug.DrawRay(pos, Vector3.down * (vaultMaxHeight + vaultMaxHeightDifference + 0.1f), Color.blue, 5);
#endif

            if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, vaultMaxHeight + vaultMaxHeightDifference + 0.1f, ~playerLayerMask, QueryTriggerInteraction.Ignore))
            {

                if (Mathf.Abs(hit.point.y - footPosition.y) > vaultMaxHeightDifference) continue;

                Vector3 vaultPos = GetVaultPosition(hit.point);

                if (m_Rigidbody.CheckCollider(vaultPos, rotation, ~playerLayerMask, QueryTriggerInteraction.Ignore)) continue;

                float dist = (hit.point - footPosition).sqrMagnitude;

                if (dist < minDist)
                {

                    minDist = dist;
                    closestHit = hit;

                }

            }

        }

        if (minDist < float.MaxValue)
        {

            Debug.DrawRay(closestHit.point, Vector3.RotateTowards(closestHit.normal, (footPosition - closestHit.point).normalized, 0.78f, 1), Color.green, 5);

            Vector3 vaultPosition = GetVaultPosition(closestHit.point);

            if (m_Rigidbody.Sweep(vaultPosition, rotation, -vaultDirection, out RaycastHit sweepHit, Mathf.Sqrt(minDist), ~playerLayerMask, QueryTriggerInteraction.Ignore))
            {

                vaultPosition = vaultPosition - vaultDirection * (sweepHit.distance - 0.1f);

            }

            if (m_Rigidbody.CheckCollider(vaultPosition, rotation, ~playerLayerMask, QueryTriggerInteraction.Ignore)) return false;

            TransitionTo(State.Default, GetFootPosition(vaultPosition, rotation, footHeightFloorlessOffset), vaultDirection, (gp.PosWorld.y - footPosition.y) * vaultTimeHeightRatio, gp.transform, null, null, null, (float deltaTime) => { m_Rigidbody.velocity = Vector3.zero; }, true);

            return true;

        }

        return false;

    }

    [Header("Jumps")]
    public float specialJumpWindow = 0.3f;
    protected float specialJumpTimer;
    public float specialJumpAirTimeout = 0.2f;

    public float specialJumpBackTwistMinDot = 0.25f;
    public float specialJumpBackTwistTime = 0.5f;

    public float specialJumpMantleForceBoost = 1.35f;

    protected bool wallJumpToken;
    public bool ConsumeWallJumpToken()
    {

        bool valid = wallJumpToken;

        wallJumpToken = false;

        return valid;

    }

    public delegate void SpecialJumpDelegate(float deltaTime, Vector3 position, Quaternion rotation, float forceMultiplier = 1, bool clampY = false, float maxForceY = 0);

    protected void ResetJumpState()
    {

        jumpToken = false;
        jumpReleaseToken = false;
        specialJumpTimer = 0;
        jumpIgnoreToken = false;

    }

    protected bool TrySpecialJump(float deltaTime, Vector3 position, Quaternion rotation, SpecialJumpDelegate jumpDelegate, float forceMultiplier = 1, bool clampY = false, float maxForceY = 0)
    {

        if (specialJumpTimer > 0)
        {

            if (!IsHoldingJump && ConsumeJumpReleaseToken())
            {

                specialJumpTimer = 0;
                //jumpIgnoreToken = false;

                jumpDelegate?.Invoke(deltaTime, position, rotation, forceMultiplier, clampY, maxForceY);

                return true;

            }

            specialJumpTimer -= deltaTime;  

            if (specialJumpTimer <= 0)
            {

                specialJumpTimer = 0;
                jumpIgnoreToken = false;

            }

        }

        if (ConsumeJumpToken())
        {

            specialJumpTimer = specialJumpWindow;
            //jumpIgnoreToken = true;
             
        }

        return false;

    }

    public float hangSpecialJumpReachRatio = 1;
    public float hangSpecialJumpPower = 5;
    protected Vector3 lastWallJumpNormal;

    protected void InclineSpecialJump(Vector3 inputDir, Vector3 wallFacingNormal, Vector3 upFacingNormal, float forceMultiplier = 1, bool clampY = false, float maxForceY = 0)
    {

        inputDir = inputDir.normalized;

        sprintIgnoreToken = true;
        m_Rigidbody.isKinematic = false;
        m_Rigidbody.useGravity = true;

        Vector3 direction = Vector3.zero;

        if (inputDir.magnitude == 0)
        {

            direction = Vector3.RotateTowards(-wallFacingNormal, upFacingNormal, 0.27f, 1);

            TransitionTo(State.Default, Vector3.zero, Vector3.zero, false, false, specialJumpBackTwistTime, null, null, null, null, null, false, null, State.Default, (float deltaTime) => sprintIgnoreToken = false);

        }
        else
        {

#if UNITY_EDITOR
            Debug.DrawRay(transform.position, inputDir * 100, Color.magenta, 5);
#endif

            if (Vector3.Dot(inputDir, -wallFacingNormal) >= specialJumpBackTwistMinDot)
            {

                direction = Vector3.RotateTowards(inputDir, upFacingNormal, 0.52f, 1);

                TransitionToRotation(State.Default, inputDir, specialJumpBackTwistTime, null, null, null, null, null, false);

            }
            else
            {

                Vector3 facing2D = wallFacingNormal;
                facing2D.y = 0;
                facing2D = facing2D.normalized;

                Vector3 cross2D = Vector3.Cross(wallFacingNormal, upFacingNormal);
                cross2D.y = 0;
                cross2D = cross2D.normalized;

                //float fwdDot = Mathf.Max(0, Vector3.Dot(inputDir, facing2D));
                //direction = Vector3.RotateTowards(gp.NormWorld, Vector3.RotateTowards(cross2D * Mathf.Sign(Vector3.Dot(inputDir, cross2D)), gp.NormWorld, 0.52f, 1), 1.5708f * (1 - fwdDot), 1);

                direction = Vector3.RotateTowards(GetWallRelativeInput(wallFacingNormal, upFacingNormal), upFacingNormal, 0.52f, 1);

                TransitionTo(State.Default, Vector3.zero, Vector3.zero, false, false, specialJumpBackTwistTime, null, null, null, null, null, false, null, State.Default, (float deltaTime) => sprintIgnoreToken = false);

            }

        }

        direction = direction.normalized;

        Vector3 force = direction * hangSpecialJumpPower * forceMultiplier;

        if (clampY) force.y = Mathf.Min(force.y, maxForceY);

        Vector3 velocity = m_Rigidbody.velocity;

        float mag = velocity.magnitude;

        float mul = 1;

        if (clampY)
        {

            mul = 1 - Mathf.Max(0, Vector3.Dot(wallFacingNormal, lastWallJumpNormal)); // Stop wall jump spam across same surface

        }
        
        if (mag != 0)
        {

            Vector3 dir = velocity / mag;
            float forceMag = force.magnitude;

            if (forceMag != 0)
            {

                Vector3 forceDir = force / forceMag;

                float mix = clampY ? Mathf.Max(0, Vector3.Dot(forceDir, dir)) : 0;

                velocity = velocity + (Vector3.Lerp(force, forceDir * Mathf.Max(0, forceMag - mag), mix)/* / (clampY ? Mathf.Max(1, velocity.y) : 1)*/) * mul;

            }

        } 
        else
        {

            velocity = force * mul;

        }

        m_Rigidbody.velocity = velocity;
        //m_Rigidbody.AddForce(force, ForceMode.VelocityChange);

        lastWallJumpNormal = wallFacingNormal;

    }

    protected void EdgeJumpsBase(float deltaTime, Vector3 position, Quaternion rotation, float forceMultiplier = 1, bool clampY = false, float maxForceY = 0) 
    {

        GrabPoint gp = activeGrabPoints2[hangingGrabPointIndex];

        Vector3 origin = GetBiasedGrabPosition(position, rotation);

        float maxDist = grabReachTop * hangSpecialJumpReachRatio;
        maxDist = maxDist * maxDist;

        if ((gp.PosWorld - origin).sqrMagnitude > maxDist) return;

        InclineSpecialJump(CameraAlignedInputDirection, gp.NormFacingWorld, gp.NormWorld, forceMultiplier, clampY, maxForceY);

    }

    protected bool HandleEdgeJumps(float deltaTime, Vector3 position, Quaternion rotation, float forceMultiplier = 1, bool clampY = false, float maxForceY = 0)
    {

        if (hangingGrabPointIndex < 0) return false;

        return TrySpecialJump(deltaTime, position, rotation, EdgeJumpsBase, forceMultiplier, clampY, maxForceY);

    }

    [Header("Wall Climb")]
    public float climbCooldown = 3; 
    public float climbPower = 2;
    public float climbControlForce = 5;
    public float climbGravityNerf = 0.9f;
    public float climbVelocityNerf = 0.55f;
    protected float climbTimer;
    public float climbTimeout = 0.1f;
    public float climbMinDot = 0.45f;
    public float climbNoResetMinVelocityY = 0.1f;
    public float climbLedgeGrabMinVelocityY = 0.01f;
    public float climbLedgeGrabIgnoranceWindow = 0.5f;
    protected float climbLedgeGrabIgnoranceTimer;

    protected Vector3 climbWallFacingDir;
    protected bool climbToken; 
    public bool ConsumeClimbToken()
    {

        bool valid = climbToken;

        climbToken = false;

        return valid;

    }

    public bool TryClimbWall()
    {

        if ((!climbToken && !(climbTimer <= 0 && m_Rigidbody.velocity.y >= climbNoResetMinVelocityY)) || m_State != State.Default || wallContactCount <= 0 || Vector3.Dot(m_Rigidbody.rotation * Vector3.back, wallNormal) < climbMinDot) return false;
         
        climbWallFacingDir = -wallNormal;
        climbTimer = 0;
        climbToken = false;
        jumpToken = false;
        wallJumpToken = true;

        TransitionTo(State.Climbing, GetFootPosition(m_Rigidbody.position, m_Rigidbody.rotation, footHeightFloorlessOffset), climbWallFacingDir, 0.02f, null, null, null, null, null, false);

        return true; 

    } 

    protected void ClimbWall(float deltaTime)
    {

        if (wallContactCount <= 0)
        {

            climbTimer += deltaTime; 

            if (climbTimer >= climbTimeout)
            {

                SetState(State.Default);

                return;

            }

        } 
        else
        {

            climbTimer = 0;

            climbWallFacingDir = -wallNormal;

        }

        if (TrySpecialJump(deltaTime, m_Rigidbody.position, m_Rigidbody.rotation, WallJumpsBase)) 
        {

            climbTimer = 0;
            wallJumpToken = false;

            return; 
        
        }

        climbLedgeGrabIgnoranceTimer -= deltaTime;

        m_Rigidbody.AddForce(-Physics.gravity * climbGravityNerf, ForceMode.Acceleration);
        m_Rigidbody.AddForce(climbWallFacingDir * 10, ForceMode.Acceleration);
         
        Vector3 controlForce = GetCameraAlignedInputDirection(inputX, math.min(0, inputY)) * climbControlForce;
        m_Rigidbody.AddForce(controlForce, ForceMode.Acceleration);
         
        if (m_Rigidbody.velocity.y < 0 || !QueryJumpInput())
        {

            climbTimer = climbCooldown;

            SetState(State.Default);

        }
        else 
        {

            if (HandleWallJumps(deltaTime, m_Rigidbody.position, m_Rigidbody.rotation)) return;

            //if ((IsHoldingJump && !TryEdgeGrabMantle()) && ((IsHoldingSprint || (IsHoldingJump && m_Rigidbody.velocity.y < climbLedgeGrabMinVelocityY)) && climbLedgeGrabIgnoranceTimer <= 0)) TryEdgeGrab();
            if ((IsHoldingJump && !TryEdgeGrabMantle()) && IsHoldingSprint) {

                if (TryEdgeGrab()) ResetJumpState();
                 
            }

        }


    }

    protected void WallJumpsBase(float deltaTime, Vector3 position, Quaternion rotation, float forceMultiplier = 1, bool clampY = false, float maxForceY = 0)
    {

        Vector3 input = CameraAlignedInputDirection;

        Plane wallPlane = new Plane(lastWallNormal, Vector3.zero);

        if (Vector3.Dot(input, -lastWallNormal) > 0) input = wallPlane.ClosestPointOnPlane(input);

        float mul = 1 - Mathf.Max(0, Vector3.Dot(input, Vector3.up));

        input.y = Mathf.Min(input.y, 0); 
        input = input.normalized;

        InclineSpecialJump(input, -lastWallNormal, Vector3.up, mul * forceMultiplier, clampY, maxForceY);

    }

    protected bool HandleWallJumps(float deltaTime, Vector3 position, Quaternion rotation, float forceMultiplier = 1, bool clampY = false, float maxForceY = 0)
    {

        if (wallContactCount <= 0) return false;

        return TrySpecialJump(deltaTime, position, rotation, WallJumpsBase, forceMultiplier, clampY, maxForceY);

    }

}
