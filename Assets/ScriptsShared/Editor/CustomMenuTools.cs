using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

using Swole;
using Swole.API.Unity;

public class CustomMenuTools
{

    private const string _resourceFolderStr = "Resources/";
    [MenuItem("Tools/BulkOut/RefreshRuntimePaths")]
    public static void RefreshRuntimePaths()
    {

        Debug.Log("Refreshing Runtime Paths...");

        string GetResourceLocalPath(UnityEngine.Object asset)
        {

            string path = AssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrEmpty(path)) return "";

            int startIndex = path.IndexOf(_resourceFolderStr);
            if (startIndex < 0) return path;

            path = path.Substring(startIndex + _resourceFolderStr.Length);

            int extensionIndex = path.LastIndexOf('.');
            if (extensionIndex < 0) return path;

            path = path.Substring(0, extensionIndex);

            return path;

        }

        var database = AssetDatabase.LoadAssetAtPath<InternalResources>(InternalResources._mainPathEditor);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<InternalResources>();
            AssetDatabase.CreateAsset(database, InternalResources._mainPathEditor);
        }

        TileCollection[] tileCollections = Resources.LoadAll<TileCollection>("");

        if (tileCollections != null)
        {

            database.tileCollections = new InternalResources.Locator[tileCollections.Length];

            for (int a = 0; a < tileCollections.Length; a++) database.tileCollections[a] = new InternalResources.Locator() { id = tileCollections[a].ID, path = GetResourceLocalPath(tileCollections[a]) };

        }

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssetIfDirty(database); 

        AssetDatabase.Refresh();

    }

}
