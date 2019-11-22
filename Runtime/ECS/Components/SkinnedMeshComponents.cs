using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    public struct SkinnedMeshRigEntity : IComponentData
    {
        public Entity Value;
    }

    public struct SkinnedMeshToRigIndex : IBufferElementData
    {
        public int Value;
    }

    public struct BindPose : IBufferElementData
    {
        public float4x4 Value;
    }

    public struct SkinMatrix : IBufferElementData
    {
        public float3x4 Value;
    }
}
