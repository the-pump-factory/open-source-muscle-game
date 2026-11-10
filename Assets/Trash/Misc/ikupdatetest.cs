using RootMotion.FinalIK;
using Swole.API.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ikupdatetest : MonoBehaviour
{

    public IK ik;

    void Start()
    {

        ik.enabled = false;
        ProxyBoneJobs.ExecuteBeforeLateUpdate(ik.GetIKSolver().Update);

    }

}
