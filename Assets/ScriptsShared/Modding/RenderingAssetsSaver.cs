using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Swole;

public class RenderingAssetsSaver : MonoBehaviour
{

    public bool save;

    public string savePath = "Assets/Baked/Bin/";

    public bool prefixAssetsWithGameObjectName;

    public GameObject rootObject;

#if UNITY_EDITOR

    void Update()
    {
        
        if (save)
        {

            if (rootObject == null) rootObject = gameObject; 

            save = false;

            MeshFilter[] filters = rootObject.GetComponentsInChildren<MeshFilter>();
            foreach(MeshFilter filter in filters)
            {

                if (filter == null) continue;

                string prefix = string.Empty;
                if (prefixAssetsWithGameObjectName) prefix = filter.name + "_";

                SaveAsset(filter.sharedMesh, savePath, prefix);
                MeshRenderer renderer = filter.gameObject.GetComponent<MeshRenderer>();
                if (renderer == null) continue;
                Material[] mats = renderer.sharedMaterials;
                if (mats == null) continue;
                foreach (Material mat in mats) SaveAsset(mat, savePath, prefix);

            }

            SkinnedMeshRenderer[] skinnedRenderers = rootObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer skinnedRenderer in skinnedRenderers)
            {

                if (skinnedRenderer == null) continue;

                string prefix = string.Empty;
                if (prefixAssetsWithGameObjectName) prefix = skinnedRenderer.name + "_";

                SaveAsset(skinnedRenderer.sharedMesh, savePath, prefix);
                Material[] mats = skinnedRenderer.sharedMaterials;
                if (mats == null) continue;
                foreach (Material mat in mats) SaveAsset(mat, savePath, prefix);

            }

            CustomSkinnedMeshRenderer[] customSkinnedRenderers = rootObject.GetComponentsInChildren<CustomSkinnedMeshRenderer>();
            foreach (CustomSkinnedMeshRenderer customSkinnedRenderer in customSkinnedRenderers)
            {

                if (customSkinnedRenderer == null) continue;

                string prefix = string.Empty;
                if (prefixAssetsWithGameObjectName) prefix = customSkinnedRenderer.name + "_";

                SaveAsset(customSkinnedRenderer.GetMesh(), savePath, prefix);
                if (customSkinnedRenderer.meshRenderer == null) continue;
                Material[] mats = customSkinnedRenderer.meshRenderer.sharedMaterials;
                if (mats == null) continue;
                foreach (Material mat in mats) SaveAsset(mat, savePath, prefix);

            }

        }

    }

    protected static void SaveAsset(Object obj, string path, string prefix = "")
    {

        if (obj == null || AssetDatabase.Contains(obj)) 
        {
            //Debug.Log($"Failed to save {(obj == null ? "null asset" : obj.name)}");
            return;
        }

        if (!path.StartsWith("Assets/")) path = "Assets/" + path;
        if (!path.EndsWith('/')) path = path + "/";

        string extension = ".asset";
        if (obj is Material) extension = ".mat";

        string assetName = obj.name;
        if (!string.IsNullOrWhiteSpace(prefix)) assetName = prefix + assetName;
        string savePath = path + assetName + extension;
        //Debug.Log($"Saving asset {obj.name} to {savePath}");
        AssetDatabase.CreateAsset(obj, savePath);

    }

#endif

}
