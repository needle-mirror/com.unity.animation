using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animation
{
    [BurstCompatible]
    internal static class BlobBuilderArrayExtension
    {
        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        public static unsafe void CopyFrom<T>(this BlobBuilderArray<T> dstArray, ref BlobArray<T> srcArray) where T : unmanaged
        {
            UnsafeUtility.MemCpy(dstArray.GetUnsafePtr(), srcArray.GetUnsafePtr(), srcArray.Length * sizeof(T));
        }
    }
}
