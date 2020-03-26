using Unity.Collections;
using Unity.Entities;
using UnityEngine.Assertions;

namespace Unity.Animation
{
    public struct ClipInstance
    {
        public BlobAssetReference<RigDefinition> RigDefinition;
        public Clip Clip;
        public BlobArray<int> TranslationBindingMap;
        public BlobArray<int> RotationBindingMap;
        public BlobArray<int> ScaleBindingMap;
        public BlobArray<int> FloatBindingMap;
        public BlobArray<int> IntBindingMap;

        static public BlobAssetReference<ClipInstance> Create(
            BlobAssetReference<RigDefinition> rigDefinition,
            BlobAssetReference<Clip> sourceClip)
        {
            if (sourceClip == default || rigDefinition == default)
                return new BlobAssetReference<ClipInstance>();

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var clipInstance = ref blobBuilder.ConstructRoot<ClipInstance>();

            clipInstance.RigDefinition = rigDefinition;
            clipInstance.Clip.Duration = sourceClip.Value.Duration;
            clipInstance.Clip.SampleRate = sourceClip.Value.SampleRate;
            clipInstance.CreateCurves(ref blobBuilder, ref sourceClip.Value);

            var clipInstanceRef = blobBuilder.CreateBlobAssetReference<ClipInstance>(Allocator.Persistent);

            blobBuilder.Dispose();

            return clipInstanceRef;
        }

        private void CreateCurves(ref BlobBuilder blobBuilder, ref Clip sourceClip)
        {
            var translationBindings = CreateInstanceBindings(ref blobBuilder, ref sourceClip, (ref BindingSet set) => ref set.TranslationBindings, ref TranslationBindingMap);
            var rotationBindings = CreateInstanceBindings(ref blobBuilder, ref sourceClip, (ref BindingSet set) => ref set.RotationBindings, ref RotationBindingMap);
            var scaleBindings = CreateInstanceBindings(ref blobBuilder, ref sourceClip, (ref BindingSet set) => ref set.ScaleBindings, ref ScaleBindingMap);
            var floatBindings = CreateInstanceBindings(ref blobBuilder, ref sourceClip, (ref BindingSet set) => ref set.FloatBindings, ref FloatBindingMap);
            var intBindings = CreateInstanceBindings(ref blobBuilder, ref sourceClip, (ref BindingSet set) => ref set.IntBindings, ref IntBindingMap);

            Clip.Bindings = Clip.CreateBindingSet(translationBindings.Length, rotationBindings.Length, scaleBindings.Length, floatBindings.Length, intBindings.Length);

            int sampleCount = Clip.Bindings.CurveCount * (Clip.FrameCount + 1);
            if (sampleCount == 0)
                return;

            var sample = blobBuilder.Allocate(ref Clip.Samples, sampleCount);
            ref var sourceClipBindingSet = ref sourceClip.Bindings;

            // Translation bindings and curves.
            FillCurvesForBindings(ref sample, BindingSet.TranslationKeyFloatCount,
                ref sourceClip, ref sourceClipBindingSet.TranslationBindings, sourceClipBindingSet.TranslationSamplesOffset,
                ref translationBindings, Clip.Bindings.TranslationSamplesOffset, Clip.Bindings.CurveCount);

            // Scale bindings and curves.
            FillCurvesForBindings(ref sample, BindingSet.ScaleKeyFloatCount,
                ref sourceClip, ref sourceClipBindingSet.ScaleBindings, sourceClipBindingSet.ScaleSamplesOffset,
                ref scaleBindings, Clip.Bindings.ScaleSamplesOffset, Clip.Bindings.CurveCount);

            // Float bindings and curves.
            FillCurvesForBindings(ref sample, BindingSet.FloatKeyFloatCount,
                ref sourceClip, ref sourceClipBindingSet.FloatBindings, sourceClipBindingSet.FloatSamplesOffset,
                ref floatBindings, Clip.Bindings.FloatSamplesOffset, Clip.Bindings.CurveCount);

            // Int bindings and curves.
            FillCurvesForBindings(ref sample, BindingSet.IntKeyFloatCount,
                ref sourceClip, ref sourceClipBindingSet.IntBindings, sourceClipBindingSet.IntSamplesOffset,
                ref intBindings, Clip.Bindings.IntSamplesOffset, Clip.Bindings.CurveCount);

            // Rotation bindings and curves.
            FillCurvesForBindings(ref sample, BindingSet.RotationKeyFloatCount,
                ref sourceClip, ref sourceClipBindingSet.RotationBindings, sourceClipBindingSet.RotationSamplesOffset,
                ref rotationBindings, Clip.Bindings.RotationSamplesOffset, Clip.Bindings.CurveCount);
        }

        private delegate ref BlobArray<StringHash> GetBindings(ref BindingSet set);

        private BlobBuilderArray<StringHash> CreateInstanceBindings(
            ref BlobBuilder blobBuilder,
            ref Clip sourceClip,
            GetBindings getBindings,
            ref BlobArray<int> bindingMap)
        {
            ref var rigBindings = ref getBindings(ref RigDefinition.Value.Bindings);
            ref var sourceClipBindings = ref getBindings(ref sourceClip.Bindings);

            // Create temporary list to cache the binding indices.
            var tmp = new NativeList<int>(rigBindings.Length, Allocator.Temp);

            // Find the exact size of the future binding map; only clip bindings that exist in the rig bindings.
            for (var i = 0; i < rigBindings.Length; ++i)
            {
                if (Core.FindBindingIndex(ref sourceClipBindings, rigBindings[i]) != -1)
                    tmp.Add(i);
            }

            BlobBuilderArray<StringHash> instanceBindings = default;
            if (tmp.Length == 0)
                return instanceBindings;

            var map = blobBuilder.Allocate(ref bindingMap, tmp.Length);
            instanceBindings = blobBuilder.Allocate(ref getBindings(ref Clip.Bindings), map.Length);
            for (int i = 0; i < tmp.Length; ++i)
            {
                map[i] = tmp[i];
                instanceBindings[i] = rigBindings[tmp[i]];
            }

            return instanceBindings;
        }

        private void FillCurvesForBindings(
            ref BlobBuilderArray<float> samples,
            int keyFloatCount,
            ref Clip sourceClip,
            ref BlobArray<StringHash> sourceClipBindings,
            int sourceClipCurveOffset,
            ref BlobBuilderArray<StringHash> instanceBindings,
            int instanceCurveOffset,
            int instanceCurveCount)
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

        private void CopyCurve(ref BlobBuilderArray<float> destSamples, int destCurveIndex, int destCurveCount,
            ref Clip sourceClip, int sourceCurveIndex, int keyFloatCount)
        {
            var sourceCurveCount = sourceClip.Bindings.CurveCount;
            var numFrames = Clip.FrameCount;
            for (var frameIter = 0; frameIter <= Clip.FrameCount; frameIter++)
            {
                for (var keyIter = 0; keyIter < keyFloatCount; keyIter++)
                {
                    var v = sourceClip.Samples[frameIter * sourceCurveCount + sourceCurveIndex + keyIter];
                    destSamples[frameIter * destCurveCount + destCurveIndex + keyIter] = v;
                }
            }
        }
    }
}
