using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidSpring : MonoBehaviour
{

    private void Awake()
    {

        gameObject.layer = LayerMask.NameToLayer("Spring");

    }

}
