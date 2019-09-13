using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    public struct Clip
    {
        public BlobArray<float> Samples;
        public BindingSet Bindings;
        public float Duration;
        public float SampleRate;

        public int FrameCount => (int)math.ceil(Duration * SampleRate);
    }
}
