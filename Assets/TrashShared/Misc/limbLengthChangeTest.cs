#if UNITY_EDITOR

using Swole.DataStructures;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

[ExecuteAlways]
public class limbLengthChangeTest : MonoBehaviour
{

    public Transform parent;
    public Transform start;
    public Transform middle;
    public Transform end;

    public Vector3 extensionAxis = Vector3.up;
    public Vector3 bendAxis = Vector3.right;

    public bool setDefaultData;
    public TransformDataPair defaultSetupParent;
    public TransformDataPair defaultSetupStart;
    public TransformDataPair defaultSetupMiddle;
    public TransformDataPair defaultSetupEnd;

    public bool resetToDefault;

    public void OnDrawGizmos()
    {
        if (setDefaultData)
        {
            setDefaultData = false;

            defaultSetupParent = new TransformDataPair() { position = parent.position, rotation = parent.rotation };
            defaultSetupStart = new TransformDataPair() { position = start.position, rotation = start.rotation };
            defaultSetupMiddle = new TransformDataPair() { position = middle.position, rotation = middle.rotation };
            defaultSetupEnd = new TransformDataPair() { position = end.position, rotation = end.rotation };
        }
        if (resetToDefault)
        {
            resetToDefault = false;

            defaultSetupParent.Apply(parent);
            defaultSetupStart.Apply(start);
            defaultSetupMiddle.Apply(middle);
            defaultSetupEnd.Apply(end);

            normLengthA = 1f;
            normLengthB = 1f;

            prevLengthA = defaultLengthA;
            prevLengthB = defaultLengthB; 
        }

        if (start.parent != parent) start.SetParent(parent, true);
        if (middle.parent != start) middle.SetParent(start, true);
        if (end.parent != middle) end.SetParent(middle, true);

        Gizmos.matrix = parent.localToWorldMatrix;

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.03f, 0.03f, 0.03f));

        Gizmos.matrix = Matrix4x4.identity;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(start.position, start.TransformDirection(extensionAxis) * defaultLengthA);
        Gizmos.DrawRay(middle.position, middle.TransformDirection(extensionAxis) * defaultLengthB);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(start.position, middle.position);
        Gizmos.DrawLine(middle.position, end.position);

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(end.position, 0.01f);
    }

    public bool run;
    public bool preserveAngle;

    public float defaultLengthA, defaultLengthB;
    public float prevLengthA, prevLengthB;

    [Range(0.5f, 2)]
    public float normLengthA;
    [Range(0.5f, 2)]
    public float normLengthB;

    public bool CalcRiseBend(quaternion parentRot, quaternion startToEndRot, float height, float lengthA, float lengthB, out quaternion riseRot, out quaternion bendRot)
    {
        riseRot = quaternion.identity;
        bendRot = quaternion.identity;

        float rise = math.acos(((height * height) + (lengthA * lengthA) - (lengthB * lengthB)) / (2 * height * lengthA));
        float bend = math.acos(((lengthA * lengthA) + (lengthB * lengthB) - (height * height)) / (2 * lengthA * lengthB));

        if (float.IsNaN(rise) || float.IsInfinity(rise) || float.IsNaN(bend) || float.IsInfinity(bend)) return false;

        riseRot = quaternion.AxisAngle(bendAxis, -rise);
        bendRot = quaternion.AxisAngle(bendAxis, (Mathf.Deg2Rad * 180) - bend);
        
        riseRot = math.mul(parentRot, math.mul(startToEndRot, riseRot));
        bendRot = math.mul(riseRot, bendRot);

        return true;
    }
    public void Update()
    {
        if (run)
        {
            if (defaultLengthA <= 0)
            {
                Quaternion toAxis = Quaternion.FromToRotation(start.TransformDirection(extensionAxis), Vector3.up);
                defaultLengthA = (toAxis * (middle.position - start.position)).y;
                prevLengthA = defaultLengthA;
            }
            if (defaultLengthB <= 0)
            {
                Quaternion toAxis = Quaternion.FromToRotation(middle.TransformDirection(extensionAxis), Vector3.up);
                defaultLengthB = (toAxis * (end.position - middle.position)).y;
                prevLengthB = defaultLengthB;
            }

            float newLengthA = defaultLengthA * normLengthA;
            float newLengthB = defaultLengthB * normLengthB;

            float lengthChangeA = newLengthA - prevLengthA;
            float lengthChangeB = newLengthB - prevLengthB;

            float3 startPos = start.position;
            float3 endPos = end.position;

            float3 toEnd = endPos - startPos;
            float height = math.length(toEnd);
            float3 toEndDir = parent.InverseTransformDirection(toEnd / height);

            Quaternion parentRot = parent.rotation;
            Quaternion startToEndRot = Quaternion.FromToRotation(extensionAxis, toEndDir);
            bool flagA = CalcRiseBend(parentRot, startToEndRot, height, prevLengthA, prevLengthB, out var oldRiseRot, out var oldBendRot);
            bool flagB = CalcRiseBend(parentRot, startToEndRot, height, newLengthA, newLengthB, out var riseRot, out var bendRot);
            bool flag = flagA && flagB;

            riseRot = (math.mul(math.inverse(oldRiseRot), riseRot)) * start.rotation;
            bendRot = (math.mul(math.inverse(oldBendRot), bendRot)) * middle.rotation;

            middle.position = middle.position + start.rotation * extensionAxis * lengthChangeA;
            end.position = end.position + middle.rotation * extensionAxis * lengthChangeB;

            start.rotation = (quaternion)math.select(((quaternion)start.rotation).value, riseRot.value, flag);
            middle.rotation = (quaternion)math.select(((quaternion)middle.rotation).value, bendRot.value, flag);

            prevLengthA = newLengthA;
            prevLengthB = newLengthB;
        }
    }

}

#endif