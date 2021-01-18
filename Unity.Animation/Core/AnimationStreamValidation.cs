using System.Diagnostics;

using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Animation
{
    [BurstCompatible]
    public partial struct AnimationStream
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void ValidateIsNotNull()
        {
            if (IsNull)
                throw new System.NullReferenceException("AnimationStream is invalid, it contains a null rig definition.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void ValidateIsWritable()
        {
            if (IsReadOnly)
                throw new System.InvalidOperationException("AnimationStream is read only.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateIndexBoundsForSkeleton(int index)
        {
            if ((uint)index >= Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException($"AnimationStream: Skeleton index '{index}' is out of range of '{Rig.Value.Skeleton.BoneCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateIndexBoundsForTranslation(int index)
        {
            if ((uint)index >= TranslationCount)
                throw new System.IndexOutOfRangeException($"AnimationStream: Translation index '{index}' is out of range of '{TranslationCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateIndexBoundsForRotation(int index)
        {
            if ((uint)index >= RotationCount)
                throw new System.IndexOutOfRangeException($"AnimationStream: Rotation index '{index}' is out of range of '{RotationCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateIndexBoundsForScale(int index)
        {
            if ((uint)index >= ScaleCount)
                throw new System.IndexOutOfRangeException($"AnimationStream: Scale index '{index}' is out of range of '{ScaleCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateIndexBoundsForFloat(int index)
        {
            if ((uint)index >= FloatCount)
                throw new System.IndexOutOfRangeException($"AnimationStream: Float index '{index}' is out of range of '{FloatCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateIndexBoundsForInt(int index)
        {
            if ((uint)index >= IntCount)
                throw new System.IndexOutOfRangeException($"AnimationStream: Int index '{index}' is out of range of '{IntCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateIsFinite(float value)
        {
            if (!math.isfinite(value))
                throw new System.NotFiniteNumberException($"AnimationStream: float is not finite");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateIsFinite(float3 value)
        {
            if (!math.all(math.isfinite(value)))
                throw new System.NotFiniteNumberException($"AnimationStream: float3 is not finite");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateIsFinite(quaternion value)
        {
            if (!math.all(math.isfinite(value.value)))
                throw new System.NotFiniteNumberException($"AnimationStream: quaternion is not finite");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void ValidateRigEquality(ref AnimationStream other)
        {
            other.ValidateIsNotNull();
            if (Rig.Value.GetHashCode() != other.Rig.Value.GetHashCode())
                throw new System.InvalidOperationException("AnimationStream rigs do not match.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void ValidateCompatibility(ref AnimationStream other)
        {
            other.ValidateIsNotNull();

            // Validate data layout compatibility
            if (TranslationCount != other.TranslationCount ||
                RotationCount != other.RotationCount ||
                ScaleCount != other.ScaleCount ||
                FloatCount != other.FloatCount ||
                IntCount != other.IntCount ||
                Rig.Value.Bindings.StreamSize != other.Rig.Value.Bindings.StreamSize)
                throw new System.InvalidOperationException("AnimationStream data layouts are not compatible.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void ValidateCompatibility(BlobAssetReference<ClipInstance> clipInstance)
        {
            if (Rig.Value.GetHashCode() != clipInstance.Value.RigHashCode)
                throw new System.InvalidOperationException("ClipInstance is not compatible with this AnimationStream.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void ValidateCompatibility(NativeArray<WeightData> weights)
        {
            if (Core.WeightDataSize(Rig) != weights.Length)
                throw new System.InvalidOperationException("WeightData is not compatible with this AnimationStream.");
        }
    }
}
