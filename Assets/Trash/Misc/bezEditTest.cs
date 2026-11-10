using Swole.API.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bezEditTest : MonoBehaviour
{

    public BezierCurveEditor2D editor;

    // Start is called before the first frame update
    void Start()
    {
        editor.SetCurve(new ExternalBezierCurve()); 
    }
}
