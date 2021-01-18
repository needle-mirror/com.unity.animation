using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    public struct RigEntity : IComponentData
    {
        public Entity Value;
    }

    internal struct SkinnedMeshToRigIndexMapping : IBufferElementData
    {
        public int RigIndex;
        public int SkinMeshIndex;
    }

    internal struct SkinnedMeshToRigIndexIndirectMapping : IBufferElementData
    {
        public AffineTransform Offset;
        public int RigIndex;
        public int SkinMeshIndex;
    }

    internal struct SkinnedMeshRootEntity : IComponentData
    {
        public Entity Value;
    }

    internal struct BindPose : IBufferElementData
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
}
