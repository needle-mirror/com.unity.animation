using Unity.Entities;

namespace Unity.Animation
{
    public struct WeightEntry
    {
        public int Index;
        public float Weight;
    }

    public struct ChannelWeightTable
    {
        public BlobArray<WeightEntry> Weights;
    }
}
