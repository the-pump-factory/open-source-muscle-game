#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreastPhysicsBasic : MonoBehaviour
{

    public float mix;
    public AnimationCurve mixCurve;

    public Transform breastBone;
    private Vector3 velocity;

    public float velocityTransfer = 0.35f;
    public float maxVelocity = 8;
    public float damping = 2;
    public float maxDistance = 0.3f;

    private Vector3 prevPosition;
    private Vector3 origPosition;

    protected void Awake()
    {
        if (breastBone == null) breastBone = transform;
        prevPosition = breastBone.position;
        origPosition = breastBone.localPosition;
    }
    protected void Update()
    {
        if (mix <= 0) return;
        Swole.API.Unity.Animation.CustomAnimatorUpdater.AddPostLateUpdateWork(OnUpdate);
    }
    public void OnUpdate()
    {
        var p1 = breastBone.parent.position;
        var p2 = breastBone.position;

        float mix = mixCurve == null ? this.mix : mixCurve.Evaluate(this.mix); 

        Vector3 origPos = breastBone.parent.TransformPoint(origPosition);
        prevPosition = Vector3.Lerp(prevPosition, origPos, (1 - mix) * 30 * Time.deltaTime);
        Vector3 translation = origPos - prevPosition;
        float moveDist = translation.magnitude;
        translation = translation * velocityTransfer;
        if ((velocity + translation).magnitude * Time.deltaTime <= moveDist * Mathf.LerpUnclamped(1, 50, (-Vector3.Dot(velocity.normalized, translation.normalized)) + 1)) velocity = (velocity + translation); 
        breastBone.position = prevPosition + velocity * Time.deltaTime;
        velocity = velocity.normalized * Mathf.Min(maxVelocity, velocity.magnitude);
        velocity = velocity - (velocity.normalized * velocity.sqrMagnitude * Time.deltaTime);
        velocity *= Mathf.Pow(damping, Time.deltaTime);
        Vector3 offset = (breastBone.position - p1);
        float dist = offset.magnitude;
        if (dist > 0 && dist >= maxDistance)
        {
            offset = offset / dist;
            breastBone.position = p1 + offset * maxDistance; 
        }

        prevPosition = breastBone.position;
    }

}

#endif