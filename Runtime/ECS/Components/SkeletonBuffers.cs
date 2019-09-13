using Unity.Entities;

namespace Unity.Animation
{
    public struct SharedRigDefinition : ISharedComponentData
    {
        public BlobAssetReference<RigDefinition> Value;
    }
}
