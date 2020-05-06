using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    /// <summary>
    /// DOTS dense clip representation of an AnimationClip
    /// </summary>
    public struct Clip
    {
        public BlobArray<float> Samples;
        public BlobArray<SynchronizationTag> SynchronizationTags;
        public BindingSet Bindings;
        public float Duration;
        public float SampleRate;

        internal int m_HashCode;

        public int FrameCount => (int)math.ceil(Duration * SampleRate);
        public float LastFrameError => FrameCount - Duration * SampleRate;

        public override int GetHashCode() => m_HashCode;
    }

    /// <summary>
    /// Clip instance is a filtered version of the dense clip holding only relevant sorted curves
    /// given a specific rig defintion
    /// </summary>
    public struct ClipInstance
    {
        public Clip Clip;
        public BlobArray<int> TranslationBindingMap;
        public BlobArray<int> RotationBindingMap;
        public BlobArray<int> ScaleBindingMap;
        public BlobArray<int> FloatBindingMap;
        public BlobArray<int> IntBindingMap;

        // RigDefinition hash code used to generate this clip instance
        public int RigHashCode;
        // Clip hash code used to generate this clip instance
        public int ClipHashCode;

        [System.Obsolete("ClipInstance.Create has been deprecated. Use ClipInstanceBuilder.Create instead. (RemovedAfter 2020-07-15)")]
        public static BlobAssetReference<ClipInstance> Create(BlobAssetReference<RigDefinition> rigDefinition, BlobAssetReference<Clip> sourceClip) =>
            ClipInstanceBuilder.Create(rigDefinition, sourceClip);
    }
}
