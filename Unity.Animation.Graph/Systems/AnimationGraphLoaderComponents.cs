using System;
using Unity.DataFlowGraph;
using Unity.Entities;

namespace Unity.Animation
{
    public struct ClipRegister : IAssetRegister<Clip>
    {
        public Hash128 ID { get; set; }
        public BlobAssetReference<Clip> Asset { get; set; }
        public int Index { get; set; }
    }
    public struct BlendTree1DAsset : IGraphAsset<BlendTree1D>
    {
        public NodeID Node { get; set; }
        public PortID Port { get; set; }
        public int Index { get; set; }
        public BlobAssetReference<BlendTree1D> Value { get; set; }
    }

    public struct BlendTree2DAsset : IGraphAsset<BlendTree2DSimpleDirectional>
    {
        public NodeID Node { get; set; }
        public PortID Port { get; set; }
        public int Index { get; set; }
        public BlobAssetReference<BlendTree2DSimpleDirectional> Value { get; set; }
    }

    public struct MotionID : IConvertibleObject<uint>
    {
        public uint Value { get; set; }
    }

    public interface IAnimationAssetsMsgHandler :
        IMsgHandler<BlendTree1DAsset>,
        IMsgHandler<DynamicBuffer<BlendTree1DAsset>>,
        IMsgHandler<BlendTree2DAsset>,
        IMsgHandler<DynamicBuffer<BlendTree2DAsset>>
    {
    }
}
