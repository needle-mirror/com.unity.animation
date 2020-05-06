using Unity.Collections;
using Unity.Entities;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Jobs;

namespace Unity.Animation
{
    public static class ClipInstanceBuilder
    {
        [BurstCompile]
        internal struct CreateClipInstanceJob : IJob
        {
            public BlobBuilder BlobBuilder;
            public BlobAssetReference<RigDefinition> RigDefinition;
            public BlobAssetReference<Clip> SourceClip;

            public void Execute()
            {
                ref var clipInstance = ref BlobBuilder.ConstructRoot<ClipInstance>();
                clipInstance.RigHashCode = RigDefinition.Value.GetHashCode();
                clipInstance.ClipHashCode = SourceClip.Value.GetHashCode();
                clipInstance.Clip.Duration = SourceClip.Value.Duration;
                clipInstance.Clip.SampleRate = SourceClip.Value.SampleRate;

                var synchronizationTags = BlobBuilder.Allocate(ref clipInstance.Clip.SynchronizationTags, SourceClip.Value.SynchronizationTags.Length);
                synchronizationTags.CopyFrom(ref SourceClip.Value.SynchronizationTags);

                CreateClipInstance(ref clipInstance);
            }

            void CreateClipInstance(ref ClipInstance clipInstance)
            {
                ref var bindings = ref clipInstance.Clip.Bindings;
                ref var rigBindings = ref RigDefinition.Value.Bindings;
                ref var srcClipBindings = ref SourceClip.Value.Bindings;

                var translationBindings = CreateInstanceBindings(ref bindings.TranslationBindings, ref clipInstance.TranslationBindingMap, ref rigBindings.TranslationBindings, ref srcClipBindings.TranslationBindings);
                var rotationBindings = CreateInstanceBindings(ref bindings.RotationBindings, ref clipInstance.RotationBindingMap, ref rigBindings.RotationBindings, ref srcClipBindings.RotationBindings);
                var scaleBindings = CreateInstanceBindings(ref bindings.ScaleBindings, ref clipInstance.ScaleBindingMap, ref rigBindings.ScaleBindings, ref srcClipBindings.ScaleBindings);
                var floatBindings = CreateInstanceBindings(ref bindings.FloatBindings, ref clipInstance.FloatBindingMap, ref rigBindings.FloatBindings, ref srcClipBindings.FloatBindings);
                var intBindings = CreateInstanceBindings(ref bindings.IntBindings, ref clipInstance.IntBindingMap, ref rigBindings.IntBindings, ref srcClipBindings.IntBindings);

                clipInstance.Clip.Bindings = clipInstance.Clip.CreateBindingSet(translationBindings.Length, rotationBindings.Length, scaleBindings.Length, floatBindings.Length, intBindings.Length);

                int sampleCount = clipInstance.Clip.Bindings.CurveCount * (clipInstance.Clip.FrameCount + 1);
                if (sampleCount == 0)
                    return;

                var sample = BlobBuilder.Allocate(ref clipInstance.Clip.Samples, sampleCount);
                ref var srcClip = ref SourceClip.Value;

                // Translation bindings and curves.
                FillCurvesForBindings(ref sample, BindingSet.TranslationKeyFloatCount,
                    ref srcClip, ref srcClipBindings.TranslationBindings, srcClipBindings.TranslationSamplesOffset,
                    ref translationBindings, bindings.TranslationSamplesOffset, bindings.CurveCount);

                // Scale bindings and curves.
                FillCurvesForBindings(ref sample, BindingSet.ScaleKeyFloatCount,
                    ref srcClip, ref srcClipBindings.ScaleBindings, srcClipBindings.ScaleSamplesOffset,
                    ref scaleBindings, bindings.ScaleSamplesOffset, bindings.CurveCount);

                // Float bindings and curves.
                FillCurvesForBindings(ref sample, BindingSet.FloatKeyFloatCount,
                    ref srcClip, ref srcClipBindings.FloatBindings, srcClipBindings.FloatSamplesOffset,
                    ref floatBindings, bindings.FloatSamplesOffset, bindings.CurveCount);

                // Int bindings and curves.
                FillCurvesForBindings(ref sample, BindingSet.IntKeyFloatCount,
                    ref srcClip, ref srcClipBindings.IntBindings, srcClipBindings.IntSamplesOffset,
                    ref intBindings, bindings.IntSamplesOffset, bindings.CurveCount);

                // Rotation bindings and curves.
                FillCurvesForBindings(ref sample, BindingSet.RotationKeyFloatCount,
                    ref srcClip, ref srcClipBindings.RotationBindings, srcClipBindings.RotationSamplesOffset,
                    ref rotationBindings, bindings.RotationSamplesOffset, bindings.CurveCount);
            }

            BlobBuilderArray<StringHash> CreateInstanceBindings(
                ref BlobArray<StringHash> clipInstanceBindings,
                ref BlobArray<int>        clipInstanceBindingMap,
                ref BlobArray<StringHash> rigDefinitionBindings,
                ref BlobArray<StringHash> sourceClipBindings
            )
            {
                // Find the exact size of the future binding map; only clip bindings that exist in the rig bindings.
                var tmp = new NativeList<int>(rigDefinitionBindings.Length, Allocator.Temp);
                for (var i = 0; i < rigDefinitionBindings.Length; ++i)
                {
                    if (Core.FindBindingIndex(ref sourceClipBindings, rigDefinitionBindings[i]) != -1)
                        tmp.Add(i);
                }

                BlobBuilderArray<StringHash> instanceBindings = default;
                if (tmp.Length == 0)
                    return instanceBindings;

                var map = BlobBuilder.Allocate(ref clipInstanceBindingMap, tmp.Length);
                instanceBindings = BlobBuilder.Allocate(ref clipInstanceBindings, map.Length);
                for (int i = 0; i < tmp.Length; ++i)
                {
                    map[i] = tmp[i];
                    instanceBindings[i] = rigDefinitionBindings[tmp[i]];
                }

                return instanceBindings;
            }

            static void FillCurvesForBindings(
                ref BlobBuilderArray<float> samples,
                int keyFloatCount,
                ref Clip sourceClip,
                ref BlobArray<StringHash> sourceClipBindings,
                int sourceClipCurveOffset,
                ref BlobBuilderArray<StringHash> instanceBindings,
                int instanceCurveOffset,
                int instanceCurveCount
            )
            {
                for (var i = 0; i < instanceBindings.Length; ++i)
                {
                    // Find binding in clip bindings.
                    var binding = instanceBindings[i];
                    var clipBindingIndex = Core.FindBindingIndex(ref sourceClipBindings, binding);
#if !UNITY_DISABLE_ANIMATION_CHECKS
                    Assert.IsTrue(clipBindingIndex != -1);
#endif

                    // Copy all the curves for this binding to the clip instance.
                    var clipCurveIndex = sourceClipCurveOffset + clipBindingIndex * keyFloatCount;
                    CopyCurve(ref samples, instanceCurveOffset, instanceCurveCount,
                        ref sourceClip, clipCurveIndex, keyFloatCount);

                    instanceCurveOffset += keyFloatCount;
                }
            }

            static void CopyCurve(
                ref BlobBuilderArray<float> destSamples,
                int destCurveIndex,
                int destCurveCount,
                ref Clip sourceClip,
                int sourceCurveIndex,
                int keyFloatCount
            )
            {
                var sourceCurveCount = sourceClip.Bindings.CurveCount;
                for (int frameIter = 0, count = sourceClip.FrameCount; frameIter <= count; frameIter++)
                {
                    for (var keyIter = 0; keyIter < keyFloatCount; keyIter++)
                    {
                        var v = sourceClip.Samples[frameIter * sourceCurveCount + sourceCurveIndex + keyIter];
                        destSamples[frameIter * destCurveCount + destCurveIndex + keyIter] = v;
                    }
                }
            }
        }

        public static BlobAssetReference<ClipInstance> Create(
            BlobAssetReference<RigDefinition> rigDefinition,
            BlobAssetReference<Clip> sourceClip
        )
        {
            if (sourceClip == default || rigDefinition == default)
                return default;

            var blobBuilder = new BlobBuilder(Allocator.TempJob);
            var job = new CreateClipInstanceJob
            {
                BlobBuilder = blobBuilder,
                RigDefinition = rigDefinition,
                SourceClip = sourceClip
            };
            job.Run();

            var clipInstanceRef = blobBuilder.CreateBlobAssetReference<ClipInstance>(Allocator.Persistent);

            blobBuilder.Dispose();

            return clipInstanceRef;
        }
    }
}
