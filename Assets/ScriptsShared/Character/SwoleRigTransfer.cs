using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{

    [ExecuteInEditMode]
    public class SwoleRigTransfer : MonoBehaviour
    {

        public bool apply;
        public bool applyOnAwake;

        public bool setUnlocatedBonesToNull;

        public SkinnedMeshRenderer reference;
        public CustomSkinnedMeshRenderer customReference;
        public SkinnedMeshRenderer target;
        public CustomSkinnedMeshRenderer customTarget;
        public bool byName = false;
        [Tooltip("Requires Mesh duplication. Reference and target must be read/write enabled!")]
        public bool transferBindPose = false;

        public void Apply()
        {
            if (target == null && customTarget == null) target = gameObject.GetComponent<SkinnedMeshRenderer>();
            if (target != null)
            {
                if (reference != null) Transfer(reference, target, byName, transferBindPose, setUnlocatedBonesToNull); else if (customReference != null) Transfer(customReference, target, byName, transferBindPose, setUnlocatedBonesToNull);
            }

            if (customTarget == null && target == null) customTarget = gameObject.GetComponent<CustomSkinnedMeshRenderer>();
            if (customTarget != null)
            {
                if (customReference != null) Transfer(customReference, customTarget, byName, setUnlocatedBonesToNull); else if (reference != null) Transfer(reference, customTarget, byName, setUnlocatedBonesToNull);
            }
        }
        public void Awake()
        {
            if (applyOnAwake)
            {
                Apply(); 
            }
        }
        public void Update()
        {

            if (apply)
            {
                apply = false; 

                Apply(); 
            }

        }

        public static void Transfer(SkinnedMeshRenderer reference, SkinnedMeshRenderer target, bool byName = false, bool transferBindPose = false, bool setUnlocatedBonesToNull = false) => Transfer(reference.bones, reference.rootBone, reference.sharedMesh, target, byName, transferBindPose, setUnlocatedBonesToNull);
        public static void Transfer(CustomSkinnedMeshRenderer reference, SkinnedMeshRenderer target, bool byName = false, bool transferBindPose = false, bool setUnlocatedBonesToNull = false) => Transfer(reference.Bones, reference.Bones[0], reference.GetMesh(), target, byName, transferBindPose, setUnlocatedBonesToNull);

        public static void Transfer(Transform[] bonesRef, Transform rootBoneRef, Mesh meshRef, SkinnedMeshRenderer target, bool byName = false, bool transferBindPose = false, bool setUnlocatedBonesToNull = false)
        {

            if (target == null) return;

            Transform[] bonesTarg = target.bones;

            bool IsBone(Transform bone, Transform[] bones)
            {

                for (int a = 0; a < bones.Length; a++) if (bones[a] == bone) return true;

                return false;

            }

            Transform FindBone(string name, Transform rootBone)
            {

                return rootBone.FindDeepChild(name);

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

                    var newBone = FindBone(bonesTarg[a].name, rootRef);
                    if (newBone == null && !setUnlocatedBonesToNull) newBone = bonesTarg[a];
                    bonesTarg[a] = newBone;

                }

            }
            else
            {

                bonesTarg = bonesRef;

            }

            target.bones = bonesTarg;
            target.rootBone = rootBoneRef;

            if (transferBindPose)
            {

                Mesh mesh = target.sharedMesh;
                if (mesh != null && meshRef != null)
                {
                    mesh = MeshUtils.DuplicateMesh(mesh);
                    mesh.bindposes = meshRef.bindposes;
                }
                target.sharedMesh = mesh;

            }

        }

        public static void Transfer(SkinnedMeshRenderer reference, CustomSkinnedMeshRenderer target, bool byName = false, bool setUnlocatedBonesToNull = false) => Transfer(reference.bones, reference.rootBone, reference.sharedMesh, target, byName, setUnlocatedBonesToNull);
        public static void Transfer(CustomSkinnedMeshRenderer reference, CustomSkinnedMeshRenderer target, bool byName = false, bool setUnlocatedBonesToNull = false) => Transfer(reference.Bones, reference.Bones[0], reference.GetMesh(), target, byName, setUnlocatedBonesToNull);

        public static void Transfer(Transform[] bonesRef, Transform rootBoneRef, Mesh meshRef, CustomSkinnedMeshRenderer target, bool byName = false, bool setUnlocatedBonesToNull = false)
        {

            if (target == null) return;

            Transform[] bonesTarg = (Transform[])target.Bones.Clone();

            bool IsBone(Transform bone, Transform[] bones)
            {

                for (int a = 0; a < bones.Length; a++) if (bones[a] == bone) return true;

                return false;

            }

            Transform FindBone(string name, Transform rootBone)
            {

                return rootBone.FindDeepChild(name);

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

                    var newBone = FindBone(bonesTarg[a].name, rootRef);
                    if (newBone == null && !setUnlocatedBonesToNull) newBone = bonesTarg[a];
                    bonesTarg[a] = newBone;

                }

            }
            else
            {

                bonesTarg = bonesRef;

            }

            target.SetBones(bonesTarg);
        }

    }

}