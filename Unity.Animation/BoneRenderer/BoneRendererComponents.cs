using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    namespace BoneRenderer
    {
        public struct BoneColor : IComponentData
        {
            public float4 Value;
        }

        public struct BoneRendererEntity : IComponentData
        {
            public Entity Value;
        }

        public struct BoneShape : ISharedComponentData
        {
            public BoneRendererUtils.BoneShape Value;
        }

        public struct BoneSize : IComponentData
        {
            public float Value;
        }

        public struct BoneWorldMatrix : IBufferElementData
        {
            public float4x4 Value;
        }

        public struct RigIndex : IBufferElementData
        {
            public int Value;
        }

        public struct RigParentIndex : IBufferElementData
        {
            public int Value;
        }
    }
}
