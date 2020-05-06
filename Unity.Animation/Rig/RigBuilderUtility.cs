using System.Collections.Generic;
using Unity.Entities;

namespace Unity.Animation
{
    public static class RigBuilderUtility
    {
        public static IAnimationChannel[] ExtractAnimationChannelsFromClips(BlobAssetReference<Clip>[] clips)
        {
            var animationChannels = new List<IAnimationChannel>();

            for (var i = 0; i < clips.Length; ++i)
            {
                var clip = clips[i];
                ref var clipBindings = ref clip.Value.Bindings;

                // Translations.
                for (var j = 0; j < clipBindings.TranslationBindings.Length; ++j)
                {
                    var channel = new LocalTranslationChannel { Id = clipBindings.TranslationBindings[j] };
                    if (!animationChannels.Contains(channel))
                        animationChannels.Add(channel);
                }

                // Rotations.
                for (var j = 0; j < clipBindings.RotationBindings.Length; ++j)
                {
                    var channel = new LocalRotationChannel { Id = clipBindings.RotationBindings[j] };
                    if (!animationChannels.Contains(channel))
                        animationChannels.Add(channel);
                }

                // Scales.
                for (var j = 0; j < clipBindings.ScaleBindings.Length; ++j)
                {
                    var channel = new LocalScaleChannel { Id = clipBindings.ScaleBindings[j] };
                    if (!animationChannels.Contains(channel))
                        animationChannels.Add(channel);
                }

                // Floats.
                for (var j = 0; j < clipBindings.FloatBindings.Length; ++j)
                {
                    var channel = new FloatChannel { Id = clipBindings.FloatBindings[j] };
                    if (!animationChannels.Contains(channel))
                        animationChannels.Add(channel);
                }

                // Intergers.
                for (var j = 0; j < clipBindings.IntBindings.Length; ++j)
                {
                    var channel = new IntChannel { Id = clipBindings.IntBindings[j] };
                    if (!animationChannels.Contains(channel))
                        animationChannels.Add(channel);
                }
            }

            return animationChannels.ToArray();
        }
    }
}
