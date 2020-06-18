using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    public struct RigEntity : IComponentData
    {
        public Entity Value;
    }

    public struct SkinnedMeshToRigIndexMapping : IBufferElementData
    {
        public int RigIndex;
        public int SkinMeshIndex;
    }

    public struct BindPose : IBufferElementData
    {
        public float4x4 Value;
    }

    internal struct BlendShapeToRigIndexMapping : IBufferElementData
    {
        public int RigIndex;
        public int BlendShapeIndex;
    }

    internal struct BlendShapeChunkMapping : IComponentData
    {
        public int RigIndex;
        public int Size;
    }

    [System.Obsolete("SkinMatrix is deprecated use Unity.Deformations.SkinMatrix instead. (RemovedAfter 2020-08-19)", false)]
    public struct SkinMatrix : IBufferElementData
    {
        public float3x4 Value;
    }

    [System.Obsolete("SkinnedMeshRigEntity is deprecated use RigEntity instead. (RemovedAfter 2020-08-19). (UnityUpgradable) -> RigEntity", false)]
    public struct SkinnedMeshRigEntity : IComponentData
    {
        public Entity Value;
    }
}
