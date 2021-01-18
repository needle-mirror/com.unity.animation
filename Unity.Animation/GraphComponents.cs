using Unity.Entities;

namespace Unity.Animation
{
    public struct GraphReference : IComponentData
    {
        public GraphID GraphID;
        public Hash128 GraphParametersID;
    }

    public struct GraphReady : IComponentData
    {
    }

    public interface IGraphAsset<TAsset> : IGraphTarget<BlobAssetReference<TAsset>>
        where TAsset : struct
    {
    }

    // Represents an asset that will be registered to the corresponding AssetManager
    public interface IAssetRegister<T> : IBufferElementData
        where T :  struct
    {
        Hash128 ID { get; set; } //TODO Maybe use a wrapper struct or reuse the GraphID struct?
        BlobAssetReference<T> Asset { get; set; }
    }

    public interface IGraphTarget<TMsg> : IPortIdentifier
        where TMsg : struct
    {
        TMsg Value { get; set; }
    }

    public interface IPortIdentifier : IBufferElementData
    {
        NodeID Node { get; set; }
        PortID Port { get; set; }
    }

    public struct ContextReference : IEntityReference, IComponentData
    {
        public Entity Entity { get; set; }
    }

    public struct InputReference : IBufferElementData
    {
        public ulong TypeHash;
        public int TypeIndex;
        public Entity Entity;
        public int Size;
    }

    public interface IEntityReference
    {
        Entity Entity { get; }
    }

    public interface IConvertibleObject<TIn>
    {
        TIn Value { get; set; }
    }
}
