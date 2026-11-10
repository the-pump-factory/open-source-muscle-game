using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RootMotion.FinalIK;

public class FinalIK_ForceSetBendNormal : MonoBehaviour
{

    public bool apply;
    public bool applyOnAwake;
    public bool applyOnStart;

    public IK ik;

    public Vector3 bendNormal;
    public bool relativeToSecondBone;

    public void Awake()
    {
        if (applyOnAwake)
        {
            Apply();
        }
    }
    public void Start()
    {
        if (applyOnStart)
        {
            Apply();
        }
    }

    public void OnValidate()
    {
        if (apply)
        {
            apply = false;
            Apply(); 
        }
    }

    public void Apply()
    {
        if (ik == null) ik = gameObject.GetComponent<IK>();
        if (ik != null)
        {
            var solver = ik.GetIKSolver();
            if (solver is IKSolverTrigonometric trigSolver)
            {
                if (relativeToSecondBone)
                {
                    if (trigSolver.bone1 != null && trigSolver.bone2 != null)
                    {
                        trigSolver.bendNormal = trigSolver.bone2.transform.TransformDirection(bendNormal).normalized;
                    }
                } 
                else
                {
                    trigSolver.bendNormal = bendNormal.normalized;
                }

                trigSolver.InitiateBones(); 
            }
        }
    }

}
