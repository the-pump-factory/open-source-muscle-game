using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;

using Swole.API.Unity;
using Swole.API.Unity.Animation;

public class rigsampletest : MonoBehaviour
{

    public SkinnedMeshRenderer smr;

    public string boneName;

    protected int boneIndex;

    protected Vector3 startPosition;

    protected void Awake()
    {

        Rigs.Sampler sampler = Rigs.GetSampler(smr);

        if (sampler != null)
        {

            for(int a = 0; a < sampler.BoneCount; a++)
            {

                Transform bone = sampler.GetBone(a);

                if (bone == null) continue;

                if (bone.name.ToLower().Trim() == boneName.ToLower().Trim()) boneIndex = a;

            }

            //sampler.Track(boneIndex);
             
            startPosition = smr.transform.InverseTransformPoint(transform.position);

        }

    }

    protected void Update()
    {

        Rigs.Sampler sampler = Rigs.GetSampler(smr);

        if (sampler == null) return;

        //sampler.Refresh(false);

        transform.position = math.transform(sampler.Pose[boneIndex], startPosition);

    }

    protected void OnDrawGizmos()
    {

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.05f);

    }

}
