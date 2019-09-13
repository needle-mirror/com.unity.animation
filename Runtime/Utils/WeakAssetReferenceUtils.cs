using System.IO;
using Unity.Entities;
using Unity.Animation;
using UnityEngine;

public static class WeakAssetReferenceUtils
{
    public static BlobAssetReference<T> LoadAsset<T>(WeakAssetReference assetRef) where T : struct
    {
        var path = GetBlobAssetPath(assetRef.GetGuidStr());
        if (!File.Exists(path))
            throw new FileNotFoundException("Cannot find file:" + path);

        return BlobAssetReference<T>.Create(File.ReadAllBytes(path));
    }

    public static BlobAssetReference<RigDefinition> LoadRigDefinition(WeakAssetReference assetRef)
    {
        var path = GetBlobAssetPath(assetRef.GetGuidStr());
        if (!File.Exists(path))
            throw new FileNotFoundException("Cannot find file:" + path);

        return BlobAssetReference<RigDefinition>.Create(File.ReadAllBytes(path));
    }

    public static BlobAssetReference<Clip> LoadClip(WeakAssetReference assetRef)
    {
        var path = GetBlobAssetPath(assetRef.GetGuidStr());
        if (!File.Exists(path))
         throw new FileNotFoundException("Cannot find file:" + path);

        return BlobAssetReference<Clip>.Create(File.ReadAllBytes(path));
    }

    public static BlobAssetReference<BlendTree1D> LoadBlendTree(WeakAssetReference assetRef)
    {
        var path = GetBlobAssetPath(assetRef.GetGuidStr());
        if (!File.Exists(path))
         throw new FileNotFoundException("Cannot find file:" + path);

        return BlobAssetReference<BlendTree1D>.Create(File.ReadAllBytes(path));
    }

    public static string GetBlobAssetPath(string guid)
    {
        return Application.streamingAssetsPath + "/BlobAssets/" + guid + ".blob";
    }
}
