using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    public sealed class ClipManager
    {
        // Singleton.
        private ClipManager() {}
        static ClipManager() {}

        public static ClipManager Instance { get; } = new ClipManager();

        private readonly Dictionary<uint, BlobAssetReference<ClipInstance>> m_ClipInstanceMap =
            new Dictionary<uint, BlobAssetReference<ClipInstance>>();

        public void Clear() => m_ClipInstanceMap.Clear();

        public BlobAssetReference<ClipInstance> GetClipFor(BlobAssetReference<RigDefinition> rigDefinition, BlobAssetReference<Clip> clip)
        {
            var key = math.hash(new int2(rigDefinition.Value.GetHashCode(), clip.Value.GetHashCode()));
            if (!m_ClipInstanceMap.TryGetValue(key, out var clipInstance))
            {
                clipInstance = ClipInstanceBuilder.Create(rigDefinition, clip);
                m_ClipInstanceMap[key] = clipInstance;
            }

            Core.ValidateIsCreated(clipInstance);
            Core.ValidateAreEqual(clipInstance.Value.RigHashCode, rigDefinition.Value.GetHashCode());
            Core.ValidateAreEqual(clipInstance.Value.ClipHashCode, clip.Value.GetHashCode());

            return clipInstance;
        }
    }
}
