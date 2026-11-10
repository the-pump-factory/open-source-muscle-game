using Swole;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Mathematics;

public class BulkOutCamera : MonoBehaviour
{

    [SerializeField]
    new protected Camera camera;

    [SerializeField]
    protected CustomizableActionCamera actionCamera;
    public CustomizableActionCamera ActionCamera => actionCamera;

    public CustomizableActionCamera.OrbitState State
    {
        get => actionCamera.State;
        set => actionCamera.State = value;
    }

    public void TransitionToState(CustomizableActionCamera.OrbitState state, float transitionTime) => actionCamera.TransitionToState(state, transitionTime);
    public virtual void SetViewGroup(OrbitCameraViewGroup viewGroup, float transitionTime = 0f) => actionCamera.SetViewGroup(viewGroup, transitionTime);
    public virtual void SetView(OrbitCameraView view, float transitionTime = 0f) => actionCamera.SetView(view, transitionTime);
    public virtual void SetView(string id, float transitionTime = 0f) => actionCamera.SetView(id, transitionTime);

    [Serializable]
    public enum Mode
    {
        ThirdPerson
    }

    [SerializeField]
    protected Mode mode;

    public bool RecalculateOriginOnModeSwitch
    {
        get => actionCamera.recalculateOrbitOriginOnModeSwitch;
        set => actionCamera.recalculateOrbitOriginOnModeSwitch = value;
    }

    public GameObject crosshair;

    public Vector3 center;
    public float3 GetCenter() => center;
    public void SetCenter(float3 center) => this.center = center;
    public float3 GetWorldCenter() => target == null ? center : target.TransformPoint(center);
    public void SetWorldCenter(float3 worldCenter) => this.center = target == null ? worldCenter : target.InverseTransformPoint(worldCenter); 

    protected IRaycastTarget raycastTarget;
    [SerializeField]
    protected Transform target;
    public void SetTarget(Transform target)
    {
        this.target = target;

        raycastTarget = null;
        if (target != null) raycastTarget = target.GetComponent<IRaycastTarget>(); 
    }

    public bool RaycastAgainstTarget(float3 rayOrigin, float3 rayOffset, out Maths.RaycastHitResult hit)
    {
        if (raycastTarget != null) return raycastTarget.RaycastAgainst(raycastTarget.DefaultRaycastLOD, rayOrigin, rayOffset, out hit, 0.05f); 

        hit = default;
        return false;
    }

    public float collisionRadius = 0.1f;
    public LayerMask collisionMask = ~0;

    [Range(0f, 1f)]
    public float distance;
    public float distanceMin = 3f;
    public float distanceMax = 6f;
    public float DistanceFromTarget => Mathf.LerpUnclamped(distanceMin, distanceMax, distance);

    protected float prevDistance;

    [SerializeField]
    protected float yaw;
    [SerializeField]
    protected float pitch = -7f;

    public Vector2 pitchRange = new Vector2(-80f, 35f);

    public float rotationSyncSpeed = 1000f;
    public float positionSyncSpeed = 100f;

    public float yawSpeed = 1f;
    public float pitchSpeed = 1f;
    public float zoomSpeed = 1f;

    public void SetFOV(float fov) => camera.fieldOfView = fov;
    public float GetFOV() => camera.fieldOfView;

    public bool disableControl;
    private void SetActiveState(bool state) 
    { 
        if (disableControl && state)
        {
            prevDistance = DistanceFromTarget; 
        }

        disableControl = !state; 
    }
    private bool GetActiveState() => !disableControl;
    private void SetDistanceRange(float minDistance, float maxDistance)
    {
        distanceMin = minDistance;
        distanceMax = maxDistance;
    }
    private float2 GetDistanceRange() => new float2(distanceMin, distanceMax);
    private void SetZoom(float zoom) => distance = Mathf.Clamp01(zoom);
    private float GetZoom() => distance;

    private float3 GetTargetPosition() => target == null ? float3.zero : target.position;

    private float GetRotInputX() => InputProxy.InitiateZoom ? 0f : Mathf.Clamp(InputProxy.MainRightJoystickHorizontal - InputProxy.CursorAxisX, -1f, 1f);
    private float GetRotInputY() => InputProxy.InitiateZoom ? 0f : Mathf.Clamp(InputProxy.CursorAxisY + InputProxy.MainRightJoystickVertical, -1f, 1f);
    private float GetZoomInput() => InputProxy.InitiateZoom ? Mathf.Clamp(InputProxy.CursorAxisY + InputProxy.MainRightJoystickVertical, -1f, 1f) : 0f;

    private float GetMoveInputX() => Mathf.Clamp(InputProxy.MoveAxisX, -1f, 1f);
    private float GetMoveInputY() => Mathf.Clamp((InputProxy.JumpButton ? 1f : 0f) - (InputProxy.CrouchButton ? 1f : 0f), -1f, 1f); 
    private float GetMoveInputZ() => Mathf.Clamp(InputProxy.MoveAxisY, -1f, 1f);

    private Matrix4x4 GetRootL2W() => target == null ? Matrix4x4.identity : target.localToWorldMatrix;

    public CustomizableActionCamera.Mode ActionMode => actionCamera.CurrentMode;

    public virtual void SetActionMode(CustomizableActionCamera.Mode mode) => actionCamera.SetMode(mode);

    private void SetYaw(float yaw) => this.yaw = yaw;
    private float GetYaw() => yaw;

    private void SetPitch(float pitch) => this.pitch = Mathf.Clamp(pitch, pitchRange.x, pitchRange.y);
    private float GetPitch() => pitch;

    protected void Awake()
    {
        if (camera == null) camera = gameObject.GetComponent<Camera>();

        if (actionCamera == null) actionCamera = gameObject.AddOrGetComponent<CustomizableActionCamera>();
        if (actionCamera != null)
        {
            actionCamera.setFov = SetFOV;
            actionCamera.getFov = GetFOV;
            actionCamera.setOrbitActiveState = SetActiveState;
            actionCamera.getOrbitActiveState = GetActiveState;
            actionCamera.setOrbitDistanceRange = SetDistanceRange;
            actionCamera.getOrbitDistanceRange = GetDistanceRange;
            actionCamera.setZoom = SetZoom;
            actionCamera.getZoom = GetZoom;
            actionCamera.setOrbitYaw = SetYaw;
            actionCamera.getOrbitYaw = GetYaw;
            actionCamera.setOrbitPitch = SetPitch;
            actionCamera.getOrbitPitch = GetPitch;
            actionCamera.getOrbitTargetPosition = GetTargetPosition;
            actionCamera.getRootL2W = GetRootL2W;
            actionCamera.setOrbitOriginInRoot = SetCenter;
            actionCamera.getOrbitOriginInRoot = GetCenter;
            actionCamera.setOrbitWorldOrigin = SetWorldCenter;
            actionCamera.getOrbitWorldOrigin = GetWorldCenter;

            actionCamera.raycastAgainstOrbitTarget = RaycastAgainstTarget;

            actionCamera.getInputX = GetRotInputX;
            actionCamera.getInputY = GetRotInputY;
            actionCamera.getMoveInputX = GetMoveInputX;
            actionCamera.getMoveInputY = GetMoveInputY;
            actionCamera.getMoveInputZ = GetMoveInputZ;
        }

        SetTarget(target);
    }

    protected void Start()
    {
        if (target != null)
        {
            yaw = Vector3.SignedAngle(Vector3.forward, target.forward, Vector3.up) + 180f;
        }
    }

    protected void LateUpdate()
    {

        UpdateRotation(GetRotInputX(), GetRotInputY());
        UpdatePosition(GetZoomInput());

        if (crosshair != null)
        {
            if (actionCamera.CurrentMode == CustomizableActionCamera.Mode.Free) 
            {
                if (!crosshair.activeSelf) crosshair.SetActive(true);
            } 
            else
            {
                if (crosshair.activeSelf) crosshair.SetActive(false);
            }
        }

    }

    public void UpdatePosition(float zoomInput)
    {
        if (disableControl || camera == null) return;

        distance = Mathf.Clamp01(distance - zoomInput * Time.deltaTime * zoomSpeed); 

        var camTransform = camera.transform;

        var targetPosition = (target == null ? center : (target.TransformPoint(center)));
        Vector3 camLocationDir = Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
        float camDistance = DistanceFromTarget;

        bool colliding = false;
        if (Physics.SphereCast(targetPosition, collisionRadius, camLocationDir, out var hit, camDistance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            camDistance = Mathf.Min(Mathf.MoveTowards(prevDistance, camDistance, Time.deltaTime * 3f), hit.distance);
        }
        else
        {
            camDistance = Mathf.MoveTowards(prevDistance, camDistance, Time.deltaTime * 3f);
        }

        prevDistance = camDistance;

        var targetCamPosition = targetPosition + camLocationDir * camDistance;
        var camCurrentPosition = camTransform.position;
        var position = positionSyncSpeed <= 0f ? targetCamPosition : Vector3.MoveTowards(camCurrentPosition, targetCamPosition, positionSyncSpeed * Time.deltaTime * (colliding ? 2f : 1f));

        var targetCamRotation = Quaternion.LookRotation(targetPosition - position, Vector3.up);
        var rotation = Quaternion.RotateTowards(camTransform.rotation, targetCamRotation, rotationSyncSpeed * Time.deltaTime);

        camTransform.SetPositionAndRotation(position, rotation);
    }

    public void UpdateRotation(float inputX, float inputY)
    {
        if (disableControl) return;

        yaw = Maths.NormalizeDegrees(yaw + inputX * Time.deltaTime * 100f * yawSpeed);
        pitch = Mathf.Clamp(pitch + inputY * Time.deltaTime * 100f * pitchSpeed, pitchRange.x, pitchRange.y);
    }

}
