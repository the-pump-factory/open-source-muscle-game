using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Swole.API.Unity.Animation;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class CreateAvatarFromRenderer : MonoBehaviour
{

    public bool create;

    public string path = "Assets/Resources/Avatars";
    public string fileName = "";

    new public SkinnedMeshRenderer renderer;

    public void Update()
    {

        if (create)
        {
            create = false;
            if (renderer != null)
            {
                if (string.IsNullOrWhiteSpace(fileName)) fileName = "avatar_" + renderer.name;

                Transform[] bones = renderer.bones;
                if (bones != null)
                {
                    CustomAvatar avatar = CustomAvatar.Create(path, fileName);

                    string[] boneNames = new string[bones.Length];
                    for (int a = 0; a < bones.Length; a++) if (bones[a] != null) boneNames[a] = bones[a].name;
                    avatar.bones = boneNames;

#if UNITY_EDITOR
                    AssetDatabase.SaveAssetIfDirty(avatar);
#endif
                }
            }
        }

    }

}
