using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class BulkTechFacilityWall : MonoBehaviour
{

#if UNITY_EDITOR

    protected void OnDrawGizmos()
    {
        if (Application.isPlaying || hideInEditor || socketIn == BulkTechFacilityRoom.RoomConnectionSocket.None || socketOut == BulkTechFacilityRoom.RoomConnectionSocket.None) return;

        UnityEngine.Random.InitState(GetInstanceID());

        var col = UnityEngine.Random.ColorHSV(0, 1, 0.7f, 1, 0.9f, 1);
        col.a = 0.3f;

        Gizmos.color = col;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(boundsCenter, boundsExtents * 2);

        col = UnityEngine.Random.ColorHSV(0, 1, 0.7f, 1, 0.9f, 1);

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = col;
        col.a = 0.5f;
        Gizmos.DrawSphere(transform.TransformPoint(connectionPoint), 0.1f);
        Gizmos.DrawRay(transform.TransformPoint(connectionPoint), transform.TransformDirection(connectionNormal));
        Gizmos.DrawRay(transform.TransformPoint(connectionPoint), transform.TransformDirection(connectionAngleAxis));
    }

#endif

    public bool hideInEditor;

    public int group;

    public bool requireConnection;
    public bool locked;
    public bool mandatory;

    public BulkTechFacilityRoom.RoomConnectionSocket socketIn;
    public BulkTechFacilityRoom.RoomConnectionSocket socketOut;

    public Vector3 connectionPoint;
    public Vector3 connectionNormal = Vector3.forward;
    public Vector3 connectionAngleAxis = Vector3.up;

    public Vector3 boundsCenter;
    public Vector3 boundsExtents = Vector3.one;
}
