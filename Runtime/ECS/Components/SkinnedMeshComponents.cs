using System;

using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace Unity.Animation
{
    public struct SkinRenderer : ISharedComponentData, IEquatable<SkinRenderer>
    {
        public Material          Material0;
        public Material          Material1;

        public Mesh              Mesh;
        public int               Layer;
        public bool              ReceiveShadows;
        public ShadowCastingMode CastShadows;

        public bool Equals(SkinRenderer other)
        {
            return Material0 == other.Material0 &&
                Material1 == other.Material1 &&
                Mesh == other.Mesh &&
                Layer == other.Layer &&
                ReceiveShadows == other.ReceiveShadows &&
                CastShadows == other.CastShadows;
        }

        public override int GetHashCode()
        {
            return (Material0 != null ? Material0.GetHashCode() : 0) ^
                (Material1 != null ? Material1.GetHashCode() : 0) ^
                (Mesh != null ? Mesh.GetHashCode() : 0) ^ Layer;
        }
    }

    public struct SkinnedMeshComponentData : IComponentData
    {
        public Entity RigEntity;
    }

    public struct SkinnedMeshToSkeletonBone : IBufferElementData
    {
        public int Value;
    }

    public struct SkinMatrix : IBufferElementData
    {
        public float3x4 Value;
    }

    public struct BindPose : IBufferElementData
    {
        public float4x4 Value;
    }
}
