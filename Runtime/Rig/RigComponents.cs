using System;
using Unity.Entities;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

namespace Unity.Animation
{
    public interface IAnimationChannel : IEquatable<IAnimationChannel>
    {
        StringHash Id { get; set; }
    }

    public interface IAnimationChannel<T>  : IAnimationChannel
        where T : struct
    {
        T DefaultValue { get; set; }
    }

    public struct LocalTranslationChannel : IAnimationChannel<float3>
    {
        public StringHash Id { get; set; }

        public float3 DefaultValue { get; set; }

        public bool Equals(IAnimationChannel other)
        {
            return other.GetType() == typeof(LocalTranslationChannel) && Id.Equals(other.Id);
        }
    }

    public struct LocalRotationChannel : IAnimationChannel<quaternion>
    {
        public StringHash Id { get; set; }

        public quaternion DefaultValue { get; set; }

        public bool Equals(IAnimationChannel other)
        {
            return other.GetType() == typeof(LocalRotationChannel) && Id.Equals(other.Id);
        }
    }

    public struct LocalScaleChannel : IAnimationChannel<float3>
    {
        public StringHash Id { get; set; }

        public float3 DefaultValue { get; set; }

        public bool Equals(IAnimationChannel other)
        {
            return other.GetType() == typeof(LocalScaleChannel) && Id.Equals(other.Id);
        }
    }

    public struct FloatChannel : IAnimationChannel<float>
    {
        public StringHash Id { get; set; }

        public float DefaultValue { get; set; }

        public bool Equals(IAnimationChannel other)
        {
            return other.GetType() == typeof(FloatChannel) && Id.Equals(other.Id);
        }
    }

    public struct IntChannel : IAnimationChannel<int>
    {
        public StringHash Id { get; set; }

        public int DefaultValue { get; set; }

        public bool Equals(IAnimationChannel other)
        {
            return other.GetType() == typeof(IntChannel) && Id.Equals(other.Id);
        }
    }

    public struct SkeletonNode
    {
        public StringHash Id;
        public int        ParentIndex;
        public int        AxisIndex;

        public float3       LocalTranslationDefaultValue;
        public quaternion   LocalRotationDefaultValue;
        public float3       LocalScaleDefaultValue;
    }

    public struct Axis
    {
        public float3       RotationOffset;
        public float3       RotationPivot;
        public quaternion   PreRotation;
        public quaternion   PostRotation;
        public float3       ScalingOffset;
        public float3       ScalingPivot;
    }

    public struct Skeleton
    {
        // The 3 following field are stored as separe array rather than in a SkeletonNode to minimize cache pollution,
        // most of the time thoses 3 array are not used together
        public BlobArray<StringHash>  Ids;

        public BlobArray<int>           ParentIndexes;
        public BlobArray<int>           AxisIndexes;

        public BlobArray<Axis>          Axis;

        public int BoneCount => Ids.Length;
    }

    public struct RigDefinition : IEquatable<RigDefinition>
    {
        public Skeleton          Skeleton;

        public BindingSet        Bindings;

        public BlobArray<float>  DefaultValues;

        internal int m_HashCode;

        public override int GetHashCode() => m_HashCode;

        public override bool Equals(object other)
        {
            if (other == null || !(other is RigDefinition))
                return false;

            return m_HashCode == ((RigDefinition)other).m_HashCode;
        }

        public bool Equals(RigDefinition other) =>
            m_HashCode == other.m_HashCode;

        static public bool operator == (RigDefinition lhs, RigDefinition rhs) =>
            lhs.m_HashCode == rhs.m_HashCode;

        static public bool operator != (RigDefinition lhs, RigDefinition rhs) =>
            lhs.m_HashCode != rhs.m_HashCode;
    }

    public struct Rig : IComponentData
    {
        public BlobAssetReference<RigDefinition> Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator BlobAssetReference<RigDefinition>(Rig rig) =>
            rig.Value;
    }

    public struct SharedRigHash : ISharedComponentData
    {
        public int Value;
    }
}
