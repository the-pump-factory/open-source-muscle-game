using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Swole;

using Swole.Morphing;
using Swole.API.Unity.Animation;
using System.IO; 

[ExecuteAlways]
public class CharacterAdditionCreator : MonoBehaviour
{

    public bool create;

    public string defaultSavePath;

    public List<CustomAvatar> avatars;
    private CustomAvatar FindAvatarByRigContainer(string rigContainer)
    {
        if (avatars == null) return null;

        foreach(var avatar in avatars) if (avatar.rigContainer == rigContainer) return avatar;
        return null;
    }

    [Serializable]
    public class SkinnedMeshRef
    {
        public string name;
        public SkinnedMeshRenderer renderer;
        public CustomAvatar avatar;
        public string overrideSavePath;
    }

    public List<SkinnedMeshRef> skinnedMeshes;

    [Serializable]
    public class CustomizableCharacterMeshRef
    {
        public string name;
        public CustomizableCharacterMesh mesh; 
        public string overrideSavePath;
    }

    public List<CustomizableCharacterMeshRef> customizableCharacterMeshes;

    void Update()
    {

#if UNITY_EDITOR
        if (create)
        {
            create = false;

            if (skinnedMeshes != null)
            {
                foreach (var skinnedMesh in skinnedMeshes)
                {
                    var addition = ScriptableObject.CreateInstance<CharacterAddition>();

                    addition.meshType = CharacterAddition.MeshType.UnitySkinnedMesh;

                    addition.name = skinnedMesh.name;
                    if (string.IsNullOrWhiteSpace(addition.name)) addition.name = skinnedMesh.renderer.name;

                    addition.mesh = skinnedMesh.renderer.sharedMesh;
                    addition.materials = skinnedMesh.renderer.sharedMaterials;

                    addition.boundsCenter = skinnedMesh.renderer.localBounds.center;
                    addition.boundsExtents = skinnedMesh.renderer.localBounds.extents;

                    var transform = skinnedMesh.renderer.transform;
                    addition.offset = transform.localPosition;
                    addition.rotation = transform.localRotation.eulerAngles;
                    addition.scale = transform.localScale;

                    addition.avatar = skinnedMesh.avatar;
                    if (addition.avatar == null)
                    {
                        if (skinnedMesh.renderer.rootBone != null)
                        {
                            Transform rigContainer = skinnedMesh.renderer.rootBone;
                            addition.avatar = FindAvatarByRigContainer(rigContainer.name);

                            while (addition.avatar == null && rigContainer.parent != null)
                            {
                                rigContainer = rigContainer.parent;
                                addition.avatar = FindAvatarByRigContainer(rigContainer.name); 
                            }
                        }
                    }

                    string path = string.IsNullOrWhiteSpace(skinnedMesh.overrideSavePath) ? defaultSavePath : skinnedMesh.overrideSavePath;
                    if (!path.ToLower().StartsWith("assets/")) path = "Assets/" + path;

                    string savePath = Path.Combine(path, addition.name + ".asset");

                    if (!AssetDatabase.IsNativeAsset(addition))
                    {
                        AssetDatabase.DeleteAsset(savePath);
                        AssetDatabase.CreateAsset(addition, savePath);
                    }
                    else
                    {
                        AssetDatabase.SaveAssetIfDirty(addition);
                    }
                }

                foreach (var ccm in customizableCharacterMeshes)
                {
                    var addition = ScriptableObject.CreateInstance<CharacterAddition>();

                    addition.meshType = CharacterAddition.MeshType.CustomizableCharacterMesh;

                    addition.name = ccm.name;
                    if (string.IsNullOrWhiteSpace(addition.name)) addition.name = ccm.mesh.name;

                    addition.customizableCharacterMeshData = ccm.mesh.CharacterMeshData;

                    addition.boundsCenter = addition.customizableCharacterMeshData.boundsCenter;
                    addition.boundsExtents = addition.customizableCharacterMeshData.boundsExtents;

                    var transform = ccm.mesh.transform;
                    addition.offset = transform.localPosition;
                    addition.rotation = transform.localRotation.eulerAngles;
                    addition.scale = transform.localScale;

                    addition.meshGroupIndex = ccm.mesh.MeshGroupIndex;
                    addition.subMeshIndex = ccm.mesh.SubMeshIndex;

                    addition.avatar = ccm.mesh.Avatar;

                    addition.rigBufferId = ccm.mesh.LocalRigBufferID;
                    addition.shapeBufferId = ccm.mesh.LocalShapeBufferID;
                    addition.morphBufferId = ccm.mesh.LocalMorphBufferID;

                    addition.shapesInstanceReference = ccm.mesh.shapesInstanceReference == null ? string.Empty : ccm.mesh.shapesInstanceReference.name; 
                    addition.rigInstanceReference = ccm.mesh.rigInstanceReference == null ? string.Empty : ccm.mesh.rigInstanceReference.name;
                    addition.characterInstanceReference = ccm.mesh.characterInstanceReference == null ? string.Empty : ccm.mesh.characterInstanceReference.name;

                    string path = string.IsNullOrWhiteSpace(ccm.overrideSavePath) ? defaultSavePath : ccm.overrideSavePath;
                    if (!path.ToLower().StartsWith("assets/")) path = "Assets/" + path; 

                    string savePath = Path.Combine(path, addition.name + ".asset");

                    if (!AssetDatabase.IsNativeAsset(addition))
                    {
                        AssetDatabase.DeleteAsset(savePath);
                        AssetDatabase.CreateAsset(addition, savePath);
                    }
                    else
                    {
                        AssetDatabase.SaveAssetIfDirty(addition);
                    }
                }
            }
        }
#endif

    }

}
