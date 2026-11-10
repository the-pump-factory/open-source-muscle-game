using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotater : MonoBehaviour
{
    public Transform target;

    public float degreesPerSecond = 5;
    public Vector3 axis = Vector3.up;

    // Start is called before the first frame update
    void Awake()
    {
        if (target == null) target = transform;
    }

    // Update is called once per frame
    void Update()
    {
        target.rotation = Quaternion.AngleAxis(degreesPerSecond * Time.deltaTime, axis) * target.rotation;
    }
}
