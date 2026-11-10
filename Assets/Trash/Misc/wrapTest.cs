using Swole.API.Unity.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class wrapTest : MonoBehaviour
{

    new public bool print;

    public float time;

    public WrapMode wrapMode;

    public bool backwards;

    // Update is called once per frame
    void Update()
    {
        if (print)
        {
            print = false;
            Debug.Log(backwards ? AnimationUtils.WrapNormalizedTimeBackward(time, wrapMode) : AnimationUtils.WrapNormalizedTimeForward(time, wrapMode)); 
        }
    }
}
