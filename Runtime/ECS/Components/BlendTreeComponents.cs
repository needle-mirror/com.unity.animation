using Unity.Entities;

namespace Unity.Animation
{
    public struct BlendTree1DResource : IBufferElementData
    {
        public StringHash BlendParameter;
        public int        MotionCount;
        public int        MotionStartIndex;
    }

    public struct BlendTree2DResource : IBufferElementData
    {
        public StringHash BlendParameterX;
        public StringHash BlendParameterY;
        public int        MotionCount;
        public int        MotionStartIndex;
    }
}
