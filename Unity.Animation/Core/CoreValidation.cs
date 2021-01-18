using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    [BurstCompatible]
    static public partial class Core
    {
        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated<T>(BlobAssetReference<T> blob) where T : struct
        {
            if (!blob.IsCreated)
                throw new System.NullReferenceException($"BlobAssetReference of {typeof(T).Name} is not created.");
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateArgumentIsCreated<T>(BlobAssetReference<T> blob) where T : struct
        {
            if (!blob.IsCreated)
                throw new System.ArgumentNullException($"BlobAssetReference of {typeof(T).Name} is not created.");
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated<T>(NativeList<T> list) where T : struct
        {
            if (!list.IsCreated)
                throw new System.NullReferenceException($"List of {typeof(T).Name} is not created.");
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateArgumentIsCreated<T>(NativeList<T> list) where T : struct
        {
            if (!list.IsCreated)
                throw new System.ArgumentNullException($"List of {typeof(T).Name} is not created.");
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
        internal static void ValidateBufferIndexBounds(int index, int length)
        {
            if ((uint)index >= length)
                throw new System.IndexOutOfRangeException($"index '{index}' is out of range of '{length}'.");
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
