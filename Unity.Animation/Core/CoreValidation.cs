using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    static public partial class Core
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated(BlobAssetReference<RigDefinition> rig)
        {
            if (!rig.IsCreated)
                throw new System.ArgumentNullException("RigDefinition is null.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated(BlobAssetReference<Clip> clip)
        {
            if (!clip.IsCreated)
                throw new System.NullReferenceException("Clip is null.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated(BlobAssetReference<BlendTree1D> blendTree1D)
        {
            if (!blendTree1D.IsCreated)
                throw new System.ArgumentNullException("BlendTree1D is null.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated(BlobAssetReference<BlendTree2DSimpleDirectional> blendTree2D)
        {
            if (!blendTree2D.IsCreated)
                throw new System.ArgumentNullException("BlendTree2D is null.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated(BlobAssetReference<ClipInstance> clipInstance)
        {
            if (!clipInstance.IsCreated)
                throw new System.ArgumentNullException("ClipInstance is null.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated(BlobAssetReference<RigRemapTable> remapTable)
        {
            if (!remapTable.IsCreated)
                throw new System.ArgumentNullException("RigRemapTable is null.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated(NativeList<RigRemapEntry> entries)
        {
            if (!entries.IsCreated)
                throw new System.ArgumentNullException("RigRemapEntry list is null.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated(NativeList<RigTranslationOffset> offsets)
        {
            if (!offsets.IsCreated)
                throw new System.ArgumentNullException("RigTranslationOffset list is null.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated(NativeList<RigRotationOffset> offsets)
        {
            if (!offsets.IsCreated)
                throw new System.ArgumentNullException("RigRotationOffset list is null.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated(NativeList<ChannelMap> channelsList)
        {
            if (!channelsList.IsCreated)
                throw new System.ArgumentNullException("ChannelMap list is null.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated(NativeList<ChannelWeightMap> channels)
        {
            if (!channels.IsCreated)
                throw new System.ArgumentNullException("ChannelWeightMap list is null.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated(NativeList<WeightEntry> entries)
        {
            if (!entries.IsCreated)
                throw new System.ArgumentNullException("WeightEntry list is null.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateAreEqual(int expected, int value)
        {
            if (expected != value)
                throw new System.InvalidOperationException($"Value must match: '{expected}' and '{value}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateGreaterOrEqual(int value, int expected)
        {
            if (!(value >= expected))
                throw new System.InvalidOperationException($"Value '{value}' must be greater or equal to '{expected}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateGreaterOrEqual(float value, float expected)
        {
            if (!(value >= expected))
                throw new System.InvalidOperationException($"Value '{value}' must be greater or equal to '{expected}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateLessOrEqual(int value, int expected)
        {
            if (!(value <= expected))
                throw new System.InvalidOperationException($"Value '{value}' must be less or equal to '{expected}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateLessOrEqual(float value, float expected)
        {
            if (!(value <= expected))
                throw new System.InvalidOperationException($"Value '{value}' must be less or equal to '{expected}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateGreater(int value, int expected)
        {
            if (!(value > expected))
                throw new System.InvalidOperationException($"Value '{value}' must be greater than '{expected}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateGreater(float value, float expected)
        {
            if (!(value > expected))
                throw new System.InvalidOperationException($"Value '{value}' must be greater than '{expected}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateBufferLengthsAreEqual(int expected, int value)
        {
            if (expected != value)
                throw new System.InvalidOperationException($"Buffer length must match: '{expected}' and '{value}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateBufferIndexBounds(int index, int Length)
        {
            if ((uint)index >= Length)
                throw new System.IndexOutOfRangeException($"index '{index}' is out of range of '{Length}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsFinite(float value)
        {
            if (!math.isfinite(value))
                throw new System.NotFiniteNumberException("Value is not finite.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void NotImplementedException()
        {
            throw new System.NotImplementedException();
        }
    }
}
