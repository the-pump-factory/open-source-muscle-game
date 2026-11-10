using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole;

[ExecuteInEditMode]
public class RigTransfer : MonoBehaviour
{

    public bool apply;
    public bool applyOnAwake;

    public SkinnedMeshRenderer reference;
    public SkinnedMeshRenderer target;
    public bool byName = false;
    [Tooltip("Requires Mesh duplication. Reference and target must be read/write enabled!")]
    public bool transferBindPose = false;

    public void Awake()
    {
        if (applyOnAwake)
        {
            if (target == null) target = gameObject.GetComponent<SkinnedMeshRenderer>();
            Transfer(reference, target, byName, transferBindPose);
        }
    }
    public void Update()
    {
        
        if (apply)
        {
            apply = false;
            if (target == null) target = gameObject.GetComponent<SkinnedMeshRenderer>();
            Transfer(reference, target, byName, transferBindPose);
        }

    }

    public static void Transfer(SkinnedMeshRenderer reference, SkinnedMeshRenderer target, bool byName = false, bool transferBindPose = false)
    {

        if (reference == null || target == null) return;

        Transform[] bonesRef = reference.bones;
        Transform[] bonesTarg = target.bones;

        bool IsBone(Transform bone, Transform[] bones)
        {

            for (int a = 0; a < bones.Length; a++) if (bones[a] == bone) return true;

            return false;

        }

        Transform FindBone(string name, Transform rootBone)
        {

            return rootBone.FindDeepChildLiberal(name);

        }

        Transform FindRootBone(Transform[] bones)
        {

            Transform bone = bones[0];

            while (bone.parent != null && IsBone(bone.parent, bones)) bone = bone.parent;

            return bone;

        }

        if (byName)
        {

            Transform rootRef = FindRootBone(bonesRef);

            for (int a = 0; a < bonesTarg.Length; a++) 
            {
                
                bonesTarg[a] = FindBone(bonesTarg[a].name, rootRef); 
            
            }

        }
        else
        {

            bonesTarg = bonesRef;

        }

        target.bones = bonesTarg;
        target.rootBone = reference.rootBone;

        if (transferBindPose)
        {

            Mesh mesh = target.sharedMesh;
            if (mesh != null && reference.sharedMesh != null)
            {
                mesh = MeshUtils.DuplicateMesh(mesh);
                mesh.bindposes = reference.sharedMesh.bindposes;
            }
            target.sharedMesh = mesh;

        }

    }

}
