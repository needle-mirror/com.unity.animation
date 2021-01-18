using Unity.Collections;
using Unity.Entities;

namespace Unity.Animation
{
    [BurstCompatible]
    public struct WeightEntry
    {
        public int Index;
        public float Weight;
    }

    [BurstCompatible]
    public struct ChannelWeightTable
    {
        public BlobArray<WeightEntry> Weights;
    }
}
