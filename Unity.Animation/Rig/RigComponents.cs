using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;


namespace Unity.Animation
{
    public interface IAnimationChannel
    {
        StringHash Id { get; set; }
    }

    public interface IAnimationChannel<T> : IAnimationChannel
        where T : struct
    {
        T DefaultValue { get; set; }
    }

    [BurstCompatible]
    public struct LocalTranslationChannel : IAnimationChannel<float3>, IEquatable<LocalTranslationChannel>
    {
        public StringHash Id { get; set; }

        public float3 DefaultValue { get; set; }

        public bool Equals(LocalTranslationChannel other)
        {
            return Id.Equals(other.Id);
        }
    }

    [BurstCompatible]
    public struct LocalRotationChannel : IAnimationChannel<quaternion>, IEquatable<LocalRotationChannel>
    {
        public StringHash Id { get; set; }

        public quaternion DefaultValue { get; set; }

        public bool Equals(LocalRotationChannel other)
        {
            return Id.Equals(other.Id);
        }
    }

    [BurstCompatible]
    public struct LocalScaleChannel : IAnimationChannel<float3>, IEquatable<LocalScaleChannel>
    {
        public StringHash Id { get; set; }

        public float3 DefaultValue { get; set; }

        public bool Equals(LocalScaleChannel other)
        {
            return Id.Equals(other.Id);
        }
    }

    [BurstCompatible]
    public struct FloatChannel : IAnimationChannel<float>, IEquatable<FloatChannel>
    {
        public StringHash Id { get; set; }

        public float DefaultValue { get; set; }

        public bool Equals(FloatChannel other)
        {
            return Id.Equals(other.Id);
        }
    }

    [BurstCompatible]
    public struct IntChannel : IAnimationChannel<int>, IEquatable<IntChannel>
    {
        public StringHash Id { get; set; }

        public int DefaultValue { get; set; }

        public bool Equals(IntChannel other)
        {
            return Id.Equals(other.Id);
        }
    }

    /// <summary>
    /// The representation of a transform in the animation rig.
    /// </summary>
    [BurstCompatible]
    public struct SkeletonNode
    {
        public StringHash Id;
        public int        ParentIndex;
        public int        AxisIndex;

        public float3       LocalTranslationDefaultValue;
        public quaternion   LocalRotationDefaultValue;
        public float3       LocalScaleDefaultValue;
    }

    [BurstCompatible]
    public struct Axis
    {
        public float3       RotationOffset;
        public float3       RotationPivot;
        public quaternion   PreRotation;
        public quaternion   PostRotation;
        public float3       ScalingOffset;
        public float3       ScalingPivot;
    }

    [BurstCompatible]
    public struct Skeleton
    {
        // The 3 following field are stored as separate arrays rather than in a SkeletonNode to minimize cache pollution,
        // most of the time those 3 arrays are not used together
        public BlobArray<StringHash>  Ids;

        public BlobArray<int>           ParentIndexes;
        public BlobArray<int>           AxisIndexes;

        public BlobArray<Axis>          Axis;

        public int BoneCount => Ids.Length;
    }

    [BurstCompatible]
    public struct RigDefinition : IEquatable<RigDefinition>
    {
        public Skeleton          Skeleton;

        public BindingSet        Bindings;

        public BlobArray<float>  DefaultValues;

        internal int m_HashCode;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => m_HashCode;

        [NotBurstCompatible]
        public override bool Equals(object other)
        {
            if (other == null || !(other is RigDefinition))
                return false;

            return m_HashCode == ((RigDefinition)other).m_HashCode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(RigDefinition other) =>
            m_HashCode == other.m_HashCode;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool operator==(RigDefinition lhs, RigDefinition rhs) =>
            lhs.m_HashCode == rhs.m_HashCode;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool operator!=(RigDefinition lhs, RigDefinition rhs) =>
            lhs.m_HashCode != rhs.m_HashCode;
    }

    [DebuggerTypeProxy(typeof(RigDebugView))]
    public struct Rig : IComponentData
    {
        public BlobAssetReference<RigDefinition> Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator BlobAssetReference<RigDefinition>(Rig rig) =>
            rig.Value;
    }

    public struct RigRootEntity : IComponentData
    {
        /// <summary>
        /// Entity reference to the rig root. In other words, the
        /// first transform (or bone) specified in the RigComponent.
        /// </summary>
        public Entity Value;

        /// <summary>
        /// Remap matrix used to compute world to root space. This is not the complete remapping matrix,
        /// it needs to be multiplied with the value of index 0 of the stream and the result of this multiplication
        /// must be inverted.
        /// </summary>
        public AffineTransform RemapToRootMatrix;
    }

    public struct SharedRigHash : ISharedComponentData
    {
        public int Value;
    }
}
