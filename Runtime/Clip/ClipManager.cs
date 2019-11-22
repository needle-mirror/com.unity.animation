using System.Collections.Generic;
using Unity.Entities;

namespace Unity.Animation
{
    public class ClipManager
    {
        // Singleton.
        private ClipManager()
        {
        }

        private static ClipManager m_Instance;
        public static ClipManager Instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new ClipManager();
                return m_Instance;
            }
        }

        private struct RigClipPair
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public BlobAssetReference<Clip> Clip;
        }

        private readonly Dictionary<RigClipPair, BlobAssetReference<ClipInstance>> m_ClipInstanceMap =
            new Dictionary<RigClipPair, BlobAssetReference<ClipInstance>>();

        public BlobAssetReference<ClipInstance> GetClipFor(BlobAssetReference<RigDefinition> rigDefinition, BlobAssetReference<Clip> clip)
        {
            var key = new RigClipPair { RigDefinition = rigDefinition, Clip = clip };
            if (!m_ClipInstanceMap.TryGetValue(key, out var clipInstance))
            {
                clipInstance = ClipInstance.Create(rigDefinition, clip);
                m_ClipInstanceMap[key] = clipInstance;
            }

            return clipInstance;
        }
    }
}
