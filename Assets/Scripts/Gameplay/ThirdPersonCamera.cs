using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCamera : MonoBehaviour
{

    public Transform focusTransform;
    public Vector3 focusPosition;

    [SerializeField]
    protected float m_Yaw;
    public float Yaw => m_Yaw;

    [SerializeField]
    protected float m_Pitch;
    public float Pitch => m_Pitch;

    public float distance = 4f;
    public float height;

    public float sensitivityX = 1f;
    public float sensitivityY = 1f;

    public float inputAxisX;
    public float inputAxisY;

    protected void LateUpdate()
    { 

        m_Yaw = m_Yaw + inputAxisX * sensitivityX;
        m_Pitch = m_Pitch + inputAxisY * sensitivityY;
        m_Pitch = Mathf.Clamp(m_Pitch, -89, 89);

        Vector3 focusPositionWorld = focusTransform == null ? focusPosition : focusTransform.TransformPoint(focusPosition);

        transform.position = focusPositionWorld + (Vector3.up * height) + (Quaternion.Euler(0, Yaw, 0) * (Quaternion.Euler(Pitch, 0, 0) * Vector3.forward)) * distance; 

        transform.LookAt(focusPositionWorld);

    }

}
