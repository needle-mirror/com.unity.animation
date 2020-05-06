using System.IO;
using Unity.Entities;
using Unity.Animation.Tests;
using UnityEngine;

internal static class WeakAssetReferenceUtils
{
    internal static BlobAssetReference<T> LoadAsset<T>(WeakAssetReference assetRef) where T : struct
    {
        var path = GetBlobAssetPath(assetRef.GetGuidStr());
        if (!File.Exists(path))
            throw new FileNotFoundException("Cannot find file:" + path);

        return BlobAssetReference<T>.Create(File.ReadAllBytes(path));
    }

    public static string GetBlobAssetPath(string guid)
    {
        return Application.streamingAssetsPath + "/BlobAssets/" + guid + ".blob";
    }
}
