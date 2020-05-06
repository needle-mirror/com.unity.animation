using System;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animation.Hybrid
{
    public static class BlobAssetStoreExtensions
    {
        /// <summary>
        /// Gets the AnimationCurveBlob BlobAssetReference associated with the provided AnimationCurve. If it does not exist in the BlobAssetStore,
        /// the AnimationCurve will be converted to a BlobAssetReference and cached in the BlobAssetStore.
        /// </summary>
        /// <param name="blobAssetStore">The BlobAssetStore</param>
        /// <param name="curve">The AnimationCurve to convert and cache if not already present in the BlobAssetStore</param>
        /// <returns>The AnimationCurveBlob BlobAssetReference associated with the original AnimationCurve</returns>
        public static BlobAssetReference<AnimationCurveBlob> GetAnimationCurve(this BlobAssetStore blobAssetStore, UnityEngine.AnimationCurve curve)
        {
            if (blobAssetStore == null)
                throw new ArgumentNullException(nameof(blobAssetStore));
            if (curve == null)
                return default;

            // TODO: Since an AnimationCurve is not an asset on disk, we generate a hash based on the
            // data that it holds. This needs to be improved since we pay the cost to generate the hash
            // which adds extra work when the curve is already cached.
            var hash = ComputeDataHashCode(curve);
            if (!blobAssetStore.TryGet<AnimationCurveBlob>(hash, out var blob))
            {
                blob = curve.ToAnimationCurveBlobAssetRef();
                blobAssetStore.TryAdd(hash, blob);
            }

            return blob;
        }

        static Hash128 ComputeDataHashCode(UnityEngine.AnimationCurve curve)
        {
            var hash = default(Hash128);
            if (curve == null)
                return hash;

            var keys = curve.keys;
            hash.Value.x = (uint)curve.length;
            hash.Value.y = (uint)curve.postWrapMode + (((uint)curve.preWrapMode) << 16);

            var keyHash = 0ul;
            foreach (var key in keys)
            {
                var k = key;
                keyHash = hashwide(ref k, keyHash);
            }

            hash.Value.z = (uint)(keyHash >> 32);
            hash.Value.w = (uint)(keyHash & 0xFFFFFFFF);

            return hash;
        }

        static unsafe ulong hashwide<T>(ref T obj, ulong seed) where T : struct
        {
            var data = (byte*)UnsafeUtility.AddressOf(ref obj);
            return Core.XXHash.Hash64(data, UnsafeUtility.SizeOf<T>(), seed);
        }
    }
}
