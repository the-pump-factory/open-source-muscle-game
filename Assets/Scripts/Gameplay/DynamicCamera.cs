using Swole;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicCamera : MonoBehaviour
{

    new public Camera camera;

    public Vector3 targetPosition;

    public Transform targetTransform;

    protected void Awake()
    {
        
        if (camera == null) gameObject.AddOrGetComponent<Camera>();

    }

    void LateUpdate()
    {

        if (targetTransform != null) targetPosition = targetTransform.position;

        transform.LookAt(targetPosition, Vector3.up);

    }

}
