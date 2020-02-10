using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Unity.Animation
{
    public sealed class ClipManager
    {
        // Singleton.
        private ClipManager() { }
        static ClipManager() { }

        public static ClipManager Instance { get; } = new ClipManager();

        private readonly Dictionary<uint, BlobAssetReference<ClipInstance>> m_ClipInstanceMap =
            new Dictionary<uint, BlobAssetReference<ClipInstance>>();

        public void Clear() => m_ClipInstanceMap.Clear();

        public BlobAssetReference<ClipInstance> GetClipFor(BlobAssetReference<RigDefinition> rigDefinition, BlobAssetReference<Clip> clip)
        {
            var key = math.hash(new int2(rigDefinition.Value.GetHashCode(), clip.Value.GetHashCode()));
            if (!m_ClipInstanceMap.TryGetValue(key, out var clipInstance))
            {
                clipInstance = ClipInstance.Create(rigDefinition, clip);
                m_ClipInstanceMap[key] = clipInstance;
            }

            if (!clipInstance.IsCreated)
                throw new System.InvalidOperationException("ClipManager contains a stale ClipInstance reference.");

            Assert.AreEqual(clipInstance.Value.RigDefinition.Value.GetHashCode(), rigDefinition.Value.GetHashCode());
            Assert.AreEqual(clipInstance.Value.RigDefinition.Value.Bindings.CurveCount, rigDefinition.Value.Bindings.CurveCount);
            Assert.AreEqual(clipInstance.Value.RigDefinition.Value.Bindings.TranslationBindings.Length, rigDefinition.Value.Bindings.TranslationBindings.Length);
            Assert.AreEqual(clipInstance.Value.RigDefinition.Value.Bindings.RotationBindings.Length, rigDefinition.Value.Bindings.RotationBindings.Length);
            Assert.AreEqual(clipInstance.Value.RigDefinition.Value.Bindings.ScaleBindings.Length, rigDefinition.Value.Bindings.ScaleBindings.Length);
            Assert.AreEqual(clipInstance.Value.RigDefinition.Value.Bindings.FloatBindings.Length, rigDefinition.Value.Bindings.FloatBindings.Length);
            Assert.AreEqual(clipInstance.Value.RigDefinition.Value.Bindings.IntBindings.Length, rigDefinition.Value.Bindings.IntBindings.Length);

            return clipInstance;
        }
    }
}
