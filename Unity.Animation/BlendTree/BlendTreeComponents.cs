using Unity.Entities;

namespace Unity.Animation
{
    public struct BlendTree1DResource : IBufferElementData
    {
        public int        MotionCount;
        public int        MotionStartIndex;
    }

    public struct BlendTree2DResource : IBufferElementData
    {
        public int        MotionCount;
        public int        MotionStartIndex;
    }
}
