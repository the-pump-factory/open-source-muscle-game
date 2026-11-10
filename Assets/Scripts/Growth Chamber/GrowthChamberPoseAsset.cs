using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Swole;

public class GrowthChamberPoseAsset : ScriptableObject
{



}

[Serializable]
public struct GrowthChamberPose : IContent
{
    public PackageInfo PackageInfo => throw new NotImplementedException();

    public ContentInfo ContentInfo => throw new NotImplementedException();

    public string Author => throw new NotImplementedException();

    public string CreationDate => throw new NotImplementedException();

    public string LastEditDate => throw new NotImplementedException();

    public string Description => throw new NotImplementedException();

    public string OriginPath => throw new NotImplementedException();

    public string RelativePath => throw new NotImplementedException();

    public bool IsInternalAsset { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public string Name => throw new NotImplementedException();

    public bool IsValid => throw new NotImplementedException();

    public Type AssetType => throw new NotImplementedException();

    public object Asset => throw new NotImplementedException();

    public string CollectionID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public bool HasCollectionID => throw new NotImplementedException();

    public IContent CreateCopyAndReplaceContentInfo(ContentInfo info)
    {
        throw new NotImplementedException();
    }

    public IContent CreateShallowCopyAndReplaceContentInfo(ContentInfo info)
    {
        throw new NotImplementedException();
    }

    public void Delete()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void DisposeSelf()
    {
        throw new NotImplementedException();
    }

    public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
    {
        throw new NotImplementedException();
    }

    public bool IsIdenticalAsset(ISwoleAsset otherAsset)
    {
        throw new NotImplementedException();
    }

    public IContent SetOriginPath(string path)
    {
        throw new NotImplementedException();
    }

    public IContent SetRelativePath(string path)
    {
        throw new NotImplementedException();
    }
}
