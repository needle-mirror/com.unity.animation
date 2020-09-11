#if UNITY_EDITOR

using System;
using Unity.Entities;
using UnityEngine;

namespace Unity.Animation.Hybrid
{
    public static class BlobAssetStoreExtensions
    {
        /// <summary>
        /// Gets the RigDefinition BlobAssetReference associated with the provided RigComponent. If it does not exist in the BlobAssetStore,
        /// the RigComponent will be converted to a BlobAssetReference and cached in the BlobAssetStore.
        ///
        /// NOTE: This extension is not supported in the Player.
        /// </summary>
        /// <param name="blobAssetStore">The BlobAssetStore</param>
        /// <param name="rigComponent">The RigComponent to convert and cache if not already present in the BlobAssetStore</param>
        /// <returns>The RigDefintion BlobAssetReference associated with the original RigComponent</returns>
        public static BlobAssetReference<RigDefinition> GetRigDefinition(this BlobAssetStore blobAssetStore, RigComponent rigComponent) =>
            blobAssetStore.GetBlobAsset(rigComponent,
                (x) => x.ToRigDefinition()
            );

        /// <summary>
        /// Gets the Clip BlobAssetReference associated with the provided AnimationClip. If it does not exist in the BlobAssetStore,
        /// the AnimationClip will be converted to a BlobAssetReference and cached in the BlobAssetStore.
        ///
        /// NOTE: This extension is not supported in the Player.
        /// </summary>
        /// <param name="blobAssetStore">The BlobAssetStore</param>
        /// <param name="clip">The AnimationClip to convert and cache if not already present in the BlobAssetStore</param>
        /// <returns>The Clip BlobAssetReference associated with the original AnimationClip</returns>
        public static BlobAssetReference<Clip> GetClip(this BlobAssetStore blobAssetStore, AnimationClip clip) =>
            blobAssetStore.GetBlobAsset(clip, (x) => x.ToDenseClip());

        static BlobAssetReference<T> GetBlobAsset<T, U>(
            this BlobAssetStore blobAssetStore,
            U source,
            Func<U, BlobAssetReference<T>> conversionMethod,
            Entities.Hash128 assetHash
        )
            where T : struct
        {
            if (blobAssetStore == null)
                throw new ArgumentNullException(nameof(blobAssetStore));
            if (source == null)
                return default;

            if (blobAssetStore.TryGet<T>(assetHash, out var blob) && blob.IsCreated)
                return blob;

            blob = conversionMethod(source);
            if (!blob.IsCreated)
                throw new InvalidOperationException("Expected conversion to return a valid blob asset");

            blobAssetStore.TryAdd(assetHash, blob);
            return blob;
        }

        static BlobAssetReference<T> GetBlobAsset<T, U>(
            this BlobAssetStore blobAssetStore,
            U source,
            Func<U, BlobAssetReference<T>> conversionMethod
        )
            where T : struct
            where U : UnityEngine.Object
        {
            return GetBlobAsset(blobAssetStore, source, conversionMethod, GetAssetHash(source));
        }

        static Entities.Hash128 GetAssetHash(UnityEngine.Object asset)
        {
            if (asset == null)
                return default;

            var result = new Entities.Hash128();
            if (UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long fileID))
            {
                result = new Entities.Hash128(guid);
                result.Value.w ^= (uint)fileID;
                result.Value.z ^= (uint)(fileID >> 32);
            }
            else
            {
                result.Value.x = (uint)asset.GetInstanceID();
                result.Value.y = (uint)asset.GetType().GetHashCode();
                result.Value.z = 0xABCD;
                result.Value.w = 0xEF01;
            }

            return result;
        }
    }
}

#endif
