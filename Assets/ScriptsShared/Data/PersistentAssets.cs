using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PersistentAssets
{

    public class AssetReference<T> : IDisposable where T : UnityEngine.Object
    {

        protected string path;
        public string Path;

        public int index;

        protected T asset;
        public T Asset => asset;

        public bool IsValid => asset != null;

        public AssetReference(string path, T asset, int index) 
        {

            this.path = path.ToLower().Trim();
            this.asset = asset;
            this.index = index;

        }

        public override int GetHashCode()
        {
            return asset == null ? 0 : asset.GetHashCode();
        }

        public void Dispose()
        {

            if (asset == null) return;

            Resources.UnloadAsset(asset);
            asset = null;
            
        }

    }

    private static List<AssetReference<Mesh>> meshes = new List<AssetReference<Mesh>>();

    private static void RefreshIndices<T>(this List<AssetReference<T>> list, int start = 0, int count = 0) where T : UnityEngine.Object
    {

        if (list == null) return;

        start = Mathf.Max(0, start);
        if (count <= 0) count = list.Count - start;

        for (int a = 0; a < count; a++) 
        {

            int index = start + a;
            if (index >= list.Count) break;

            list[index].index = index; 

        }

    }

    public static AssetReference<Mesh> LoadMesh(string path)
    {

        path = path.Replace('\\', '/');

        string checkPath = path.ToLower().Trim();
        foreach (var meshRef in meshes) if (meshRef.Path == checkPath) return meshRef;

        Mesh mesh = Resources.Load<Mesh>(path);
        if (mesh == null) return null;

        var assetRef = new AssetReference<Mesh>(path, mesh, meshes.Count);
        meshes.Add(assetRef);

        return assetRef;

    }

    private static bool UnloadMesh(int unloadIndex)
    {
        if (unloadIndex >= 0 && unloadIndex < meshes.Count)
        {
            meshes.RemoveAt(unloadIndex);
            meshes.RefreshIndices(unloadIndex);
            return true;
        }

        return false;
    }
    public static bool Unload<T>(this AssetReference<T> assetRef) where T : UnityEngine.Object
    {

        if (assetRef == null) return false;

        if (typeof(T) == typeof(Mesh)) UnloadMesh(assetRef.index);

        assetRef.Dispose();
        return !assetRef.IsValid;

    }
    public static bool Unload(Mesh mesh)
    {

        for(int a = 0; a < meshes.Count; a++) if (meshes[a].Asset == mesh) return meshes[a].Unload();

        return false;

    }

    public static Mesh GetMesh(int index)
    {

        if (index < 0 || index >= meshes.Count) return null;

        return meshes[index].Asset;
        
    }

    public static bool TryGetMesh(int index, out Mesh mesh)
    {

        mesh = GetMesh(index);

        return mesh != null;

    }

    public static AssetReference<Mesh> FindMeshReference(string meshName)
    {

        for (int a = 0; a < meshes.Count; a++) if (meshes[a].IsValid && meshes[a].Asset.name == meshName) return meshes[a];
        meshName = meshName.ToLower().Trim();
        for (int a = 0; a < meshes.Count; a++) if (meshes[a].IsValid && meshes[a].Asset.name.ToLower().Trim() == meshName) return meshes[a];

        return null;

    }

    public static Mesh FindMesh(string meshName, out AssetReference<Mesh> meshRef)
    {

        meshRef = FindMeshReference(meshName);

        if (meshRef != null) return meshRef.Asset;

        return null;

    }

    public static Mesh FindMesh(string meshName)
    {

        return FindMesh(meshName, out _);

    }

    public static bool TryGetMesh(string meshName, out Mesh mesh)
    {

        mesh = FindMesh(meshName);

        return mesh != null;

    }
    
}
