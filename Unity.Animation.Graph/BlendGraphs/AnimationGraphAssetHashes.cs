using Unity.Entities;

namespace Unity.Animation
{
    internal static class AnimationGraphAssetHashes
    {
        public static readonly ulong ClipHash = 1999326547798121539; //@TODO this is the current value of TypeHash.CalculateStableTypeHash(typeof(BlobAssetReference<Clip>)). Hardcoded because we can't use typeof with Burst
        //public static readonly ulong BlendTree2DHash;
        //public static readonly ulong BlendTree1DHash;

//        static AnimationGraphAssetHashes()
//        {
//            ClipHash = TypeHash.CalculateStableTypeHash(typeof(BlobAssetReference<Clip>));
//            BlendTree2DHash = TypeHash.CalculateStableTypeHash(typeof(BlobAssetReference<BlendTree1D>));
//            BlendTree1DHash = TypeHash.CalculateStableTypeHash(typeof(BlobAssetReference<BlendTree2DSimpleDirectional>));
//        }
    }
}
