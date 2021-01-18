using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;


namespace Unity.Animation
{
    [BurstCompatible]
    internal static class HashUtils
    {
        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        internal static unsafe uint ComputeHash<T>(ref BlobArray<T> array, uint seed = 0)
            where T : struct
        {
            return math.hash(array.GetUnsafePtr(), array.Length * UnsafeUtility.SizeOf<T>(), seed);
        }

        internal static uint ComputeHash(ref BlobAssetReference<RigDefinition> rig)
        {
            unchecked
            {
                uint hashCode = ComputeHash(ref rig.Value.Skeleton.Ids);
                hashCode = ComputeHash(ref rig.Value.Skeleton.ParentIndexes, hashCode);
                hashCode = ComputeHash(ref rig.Value.Skeleton.AxisIndexes, hashCode);
                hashCode = ComputeHash(ref rig.Value.Skeleton.Axis, hashCode);

                hashCode = ComputeHash(ref rig.Value.Bindings.TranslationBindings, hashCode);
                hashCode = ComputeHash(ref rig.Value.Bindings.RotationBindings, hashCode);
                hashCode = ComputeHash(ref rig.Value.Bindings.ScaleBindings, hashCode);
                hashCode = ComputeHash(ref rig.Value.Bindings.FloatBindings, hashCode);
                hashCode = ComputeHash(ref rig.Value.Bindings.IntBindings, hashCode);
                hashCode = ComputeHash(ref rig.Value.DefaultValues, hashCode);

                return hashCode;
            }
        }

        internal static uint ComputeHash(ref BlobAssetReference<Clip> clip)
        {
            unchecked
            {
                uint hashCode = ComputeHash(ref clip.Value.Samples);
                hashCode = ComputeHash(ref clip.Value.SynchronizationTags, hashCode);
                hashCode = ComputeHash(ref clip.Value.Bindings.TranslationBindings, hashCode);
                hashCode = ComputeHash(ref clip.Value.Bindings.RotationBindings, hashCode);
                hashCode = ComputeHash(ref clip.Value.Bindings.ScaleBindings, hashCode);
                hashCode = ComputeHash(ref clip.Value.Bindings.FloatBindings, hashCode);
                hashCode = ComputeHash(ref clip.Value.Bindings.IntBindings, hashCode);

                return hashCode ^ math.hash(new float2(clip.Value.Duration, clip.Value.SampleRate));
            }
        }
    }
}
