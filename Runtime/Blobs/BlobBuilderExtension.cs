using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animation
{
    internal static class BlobBuilderArrayExtension
    {
        public static unsafe void CopyFrom<T>(this BlobBuilderArray<T> dstArray, ref BlobArray<T> srcArray) where T : unmanaged
        {
            UnsafeUtility.MemCpy(dstArray.GetUnsafePtr(), srcArray.GetUnsafePtr(), srcArray.Length * sizeof(T));
        }
    }
}
