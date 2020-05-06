using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Animation.Hybrid
{
    public static class ClipConversion
    {
        /// <summary>
        /// Converts a UnityEngine AnimationClip to a DOTS dense clip.
        /// </summary>
        /// <param name="sourceClip">The UnityEngine.AnimationClip to convert to a DOTS dense clip format.</param>
        /// <param name="bindingHash">Optional parameter to override the way binding hashes are generated. When no binding hash deletegate is specified, the system wide BindingHashUtils.DefaultBindingHash is used.</param>
        /// <returns>Returns a dense clip BlobAssetReference</returns>
#if UNITY_EDITOR
        public static BlobAssetReference<Clip> ToDenseClip(this AnimationClip sourceClip, BindingHashDelegate bindingHash = null)
        {
            if (sourceClip == null)
                return default;

            var srcBindings = AnimationUtility.GetCurveBindings(sourceClip);

            var translationBindings = new List<EditorCurveBinding>();
            var rotationBindings = new List<EditorCurveBinding>();
            var scaleBindings = new List<EditorCurveBinding>();
            var floatBindings = new List<EditorCurveBinding>();
            var intBindings = new List<EditorCurveBinding>();

            // TODO : Account for missing T, R, S curves
            foreach (var binding in srcBindings)
            {
                if (binding.propertyName == "m_LocalPosition.x")
                {
                    translationBindings.Add(binding);
                }
                else if (binding.propertyName == "m_LocalRotation.x"
                         || binding.propertyName == "localEulerAnglesRaw.x"
                         || binding.propertyName == "localEulerAngles.x")
                {
                    rotationBindings.Add(binding);
                }
                else if (binding.propertyName == "m_LocalScale.x")
                {
                    scaleBindings.Add(binding);
                }
                else if (binding.type == typeof(Animator))
                {
                    floatBindings.Add(binding);
                }
                else if (binding.isDiscreteCurve || binding.type == typeof(UnityEngine.Animation))
                {
                    intBindings.Add(binding);
                }
            }

            var syncTags = ExtractSynchronizationTag(sourceClip);

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var clip = ref blobBuilder.ConstructRoot<Clip>();

            CreateBindings(translationBindings, rotationBindings, scaleBindings, floatBindings, intBindings, ref blobBuilder, ref clip, bindingHash ?? BindingHashUtils.DefaultBindingHash);
            FillCurves(sourceClip, translationBindings, rotationBindings, scaleBindings, floatBindings, intBindings, ref blobBuilder, ref clip);
            FillSynchronizationTag(syncTags, ref blobBuilder, ref clip);

            var outputClip = blobBuilder.CreateBlobAssetReference<Clip>(Allocator.Persistent);

            blobBuilder.Dispose();

            outputClip.Value.m_HashCode = (int)HashUtils.ComputeHash(ref outputClip);

            return outputClip;
        }

        private static AnimationEvent[] ExtractSynchronizationTag(AnimationClip sourceClip)
        {
            var syncTags = new List<AnimationEvent>();
            var animationEvents = AnimationUtility.GetAnimationEvents(sourceClip);
            for (int i = 0; i < animationEvents.Length; i++)
            {
                var go = animationEvents[i].objectReferenceParameter as GameObject;
                if (go != null)
                {
                    var syncTag = go.GetComponent(typeof(ISynchronizationTag));
                    if (syncTag != null)
                    {
                        syncTags.Add(animationEvents[i]);
                    }
                }
            }

            return syncTags.ToArray();
        }

        private static void FillSynchronizationTag(AnimationEvent[] syncTags, ref BlobBuilder blobBuilder, ref Clip clip)
        {
            if (syncTags == null || syncTags.Length == 0)
                return;

            var duration = clip.Duration != 0.0f ? clip.Duration : 1.0f;
            var arrayBuilder = blobBuilder.Allocate(ref clip.SynchronizationTags, syncTags.Length);
            for (var i = 0; i != syncTags.Length; ++i)
            {
                var go = syncTags[i].objectReferenceParameter as GameObject;
                var syncTag = go.GetComponent(typeof(ISynchronizationTag)) as ISynchronizationTag;
                arrayBuilder[i] = new SynchronizationTag { NormalizedTime = syncTags[i].time / duration, Type = syncTag.Type, State = syncTag.State};
            }
        }

        private static void CreateBindings(IReadOnlyList<EditorCurveBinding> translationBindings,
            IReadOnlyList<EditorCurveBinding> rotationBindings, IReadOnlyList<EditorCurveBinding> scaleBindings,
            IReadOnlyList<EditorCurveBinding> floatBindings, IReadOnlyList<EditorCurveBinding> intBindings,
            ref BlobBuilder blobBuilder, ref Clip clip, BindingHashDelegate bindingHash)
        {
            clip.Bindings = clip.CreateBindingSet(translationBindings.Count, rotationBindings.Count, scaleBindings.Count, floatBindings.Count, intBindings.Count);

            FillBlobTransformBindingBuffer(translationBindings, ref blobBuilder, ref clip.Bindings.TranslationBindings, bindingHash);
            FillBlobTransformBindingBuffer(rotationBindings, ref blobBuilder, ref clip.Bindings.RotationBindings, bindingHash);
            FillBlobTransformBindingBuffer(scaleBindings, ref blobBuilder, ref clip.Bindings.ScaleBindings, bindingHash);
            FillBlobBindingBuffer(floatBindings, ref blobBuilder, ref clip.Bindings.FloatBindings, bindingHash);
            FillBlobBindingBuffer(intBindings, ref blobBuilder, ref clip.Bindings.IntBindings, bindingHash);
        }

        private static void FillBlobTransformBindingBuffer(IReadOnlyList<EditorCurveBinding> bindings, ref BlobBuilder blobBuilder, ref BlobArray<StringHash> blobBuffer, BindingHashDelegate bindingHash)
        {
            if (bindings == null || bindings.Count == 0)
                return;

            var arrayBuilder = blobBuilder.Allocate(ref blobBuffer, bindings.Count);
            for (var i = 0; i != bindings.Count; ++i)
                arrayBuilder[i] = bindingHash(bindings[i].path);
        }

        private static void FillBlobBindingBuffer(IReadOnlyList<EditorCurveBinding> bindings, ref BlobBuilder blobBuilder, ref BlobArray<StringHash> blobBuffer, BindingHashDelegate bindingHash)
        {
            if (bindings == null || bindings.Count == 0)
                return;

            var arrayBuilder = blobBuilder.Allocate(ref blobBuffer, bindings.Count);
            for (var i = 0; i != bindings.Count; ++i)
                arrayBuilder[i] = bindingHash(bindings[i].propertyName);
        }

        private static void FillCurves(AnimationClip sourceClip, IReadOnlyList<EditorCurveBinding> translationBindings,
            IReadOnlyList<EditorCurveBinding> rotationBindings, IReadOnlyList<EditorCurveBinding> scaleBindings,
            IReadOnlyList<EditorCurveBinding> floatBindings, IReadOnlyList<EditorCurveBinding> intBindings,
            ref BlobBuilder blobBuilder, ref Clip clip)
        {
            clip.Duration = sourceClip.length;
            clip.SampleRate = sourceClip.frameRate;

            var sampleCount = clip.Bindings.CurveCount * (clip.FrameCount + 1);
            if (sampleCount > 0)
            {
                var arrayBuilder = blobBuilder.Allocate(ref clip.Samples, sampleCount);

                // Translation curves
                for (var i = 0; i != translationBindings.Count; ++i)
                    AddVector3Curve(ref clip, ref arrayBuilder, sourceClip, translationBindings[i], "m_LocalPosition", clip.Bindings.TranslationSamplesOffset + i * BindingSet.TranslationKeyFloatCount, clip.Bindings.CurveCount);

                // Scale curves
                for (var i = 0; i != scaleBindings.Count; ++i)
                    AddVector3Curve(ref clip, ref arrayBuilder, sourceClip, scaleBindings[i], "m_LocalScale", clip.Bindings.ScaleSamplesOffset + i * BindingSet.ScaleKeyFloatCount, clip.Bindings.CurveCount);

                // Float curves
                for (var i = 0; i != floatBindings.Count; ++i)
                    AddFloatCurve(ref clip, ref arrayBuilder, sourceClip, floatBindings[i], clip.Bindings.FloatSamplesOffset + i * BindingSet.FloatKeyFloatCount, clip.Bindings.CurveCount);

                // Int curves
                for (var i = 0; i != intBindings.Count; ++i)
                    AddFloatCurve(ref clip, ref arrayBuilder, sourceClip, intBindings[i], clip.Bindings.IntSamplesOffset + i * BindingSet.IntKeyFloatCount, clip.Bindings.CurveCount);

                // Rotation curves
                for (var i = 0; i != rotationBindings.Count; ++i)
                {
                    // Remove the ".x" at the end.
                    var propertyName = rotationBindings[i].propertyName.Remove(rotationBindings[i].propertyName.Length - 2);
                    if (propertyName.Contains("Euler"))
                    {
                        AddEulerCurve(ref clip, ref arrayBuilder, sourceClip, rotationBindings[i], propertyName, clip.Bindings.RotationSamplesOffset + i * BindingSet.RotationKeyFloatCount, clip.Bindings.CurveCount);
                    }
                    else
                    {
                        AddQuaternionCurve(ref clip, ref arrayBuilder, sourceClip, rotationBindings[i], propertyName, clip.Bindings.RotationSamplesOffset + i * BindingSet.RotationKeyFloatCount, clip.Bindings.CurveCount);
                    }
                }
            }
        }

        private static void AddVector3Curve(ref Clip clip, ref BlobBuilderArray<float> samples, AnimationClip sourceClip, EditorCurveBinding binding,
            string property, int curveIndex, in int curveCount)
        {
            binding.propertyName = property + ".x";
            ConvertCurve(ref clip, ref samples, AnimationUtility.GetEditorCurve(sourceClip, binding), curveIndex++, curveCount);

            binding.propertyName = property + ".y";
            ConvertCurve(ref clip, ref samples, AnimationUtility.GetEditorCurve(sourceClip, binding), curveIndex++, curveCount);

            binding.propertyName = property + ".z";
            ConvertCurve(ref clip, ref samples, AnimationUtility.GetEditorCurve(sourceClip, binding), curveIndex++, curveCount);
        }

        private static void AddQuaternionCurve(ref Clip clip, ref BlobBuilderArray<float> samples, AnimationClip sourceClip, EditorCurveBinding binding,
            string property, int curveIndex, in int curveCount)
        {
            binding.propertyName = property + ".x";
            ConvertCurve(ref clip, ref samples, AnimationUtility.GetEditorCurve(sourceClip, binding), curveIndex++, curveCount);
            binding.propertyName = property + ".y";
            ConvertCurve(ref clip, ref samples, AnimationUtility.GetEditorCurve(sourceClip, binding), curveIndex++, curveCount);
            binding.propertyName = property + ".z";
            ConvertCurve(ref clip, ref samples, AnimationUtility.GetEditorCurve(sourceClip, binding), curveIndex++, curveCount);
            binding.propertyName = property + ".w";
            ConvertCurve(ref clip, ref samples, AnimationUtility.GetEditorCurve(sourceClip, binding), curveIndex++, curveCount);
        }

        private static void AddEulerCurve(ref Clip clip, ref BlobBuilderArray<float> samples, AnimationClip sourceClip, EditorCurveBinding binding,
            string property, int curveIndex, in int curveCount)
        {
            binding.propertyName = property + ".x";
            var xEulerCurve = AnimationUtility.GetEditorCurve(sourceClip, binding);
            binding.propertyName = property + ".y";
            var yEulerCurve = AnimationUtility.GetEditorCurve(sourceClip, binding);
            binding.propertyName = property + ".z";
            var zEulerCurve = AnimationUtility.GetEditorCurve(sourceClip, binding);

            float xLastValue, yLastValue, zLastValue;
            float xValueAtDuration, yValueAtDuration, zValueAtDuration;
            var qLastValue = new Quaternion();
            var qValueAtDuration = new Quaternion();
            var sampleIndex = curveIndex;

            for (var frameIter = 0; frameIter < clip.FrameCount; frameIter++)
            {
                var frameTime = frameIter / clip.SampleRate;
                xLastValue = xEulerCurve.Evaluate(frameTime);
                yLastValue = yEulerCurve.Evaluate(frameTime);
                zLastValue = zEulerCurve.Evaluate(frameTime);
                qLastValue = Quaternion.Euler(xLastValue, yLastValue, zLastValue);

                samples[sampleIndex + 0] = qLastValue.x;
                samples[sampleIndex + 1] = qLastValue.y;
                samples[sampleIndex + 2] = qLastValue.z;
                samples[sampleIndex + 3] = qLastValue.w;

                sampleIndex += curveCount;
            }

            // adjust last frame value to match value at duration
            xValueAtDuration = xEulerCurve.Evaluate(clip.Duration);
            yValueAtDuration = yEulerCurve.Evaluate(clip.Duration);
            zValueAtDuration = zEulerCurve.Evaluate(clip.Duration);
            qValueAtDuration = Quaternion.Euler(xValueAtDuration, yValueAtDuration, zValueAtDuration);

            sampleIndex = curveIndex + clip.FrameCount * curveCount;
            samples[sampleIndex + 0] = Core.AdjustLastFrameValue(qLastValue.x, qValueAtDuration.x, clip.LastFrameError);
            samples[sampleIndex + 1] = Core.AdjustLastFrameValue(qLastValue.y, qValueAtDuration.y, clip.LastFrameError);
            samples[sampleIndex + 2] = Core.AdjustLastFrameValue(qLastValue.z, qValueAtDuration.z, clip.LastFrameError);
            samples[sampleIndex + 3] = Core.AdjustLastFrameValue(qLastValue.w, qValueAtDuration.w, clip.LastFrameError);
        }

        private static void AddFloatCurve(ref Clip clip, ref BlobBuilderArray<float> samples, AnimationClip sourceClip, EditorCurveBinding binding,
            int curveIndex, in int curveCount)
        {
            ConvertCurve(ref clip, ref samples, AnimationUtility.GetEditorCurve(sourceClip, binding), curveIndex, curveCount);
        }

        private static void ConvertCurve(ref Clip clip, ref BlobBuilderArray<float> samples, UnityEngine.AnimationCurve curve, int curveIndex, in int curveCount)
        {
            var lastValue = 0.0f;

            for (var frameIter = 0; frameIter < clip.FrameCount; frameIter++)
            {
                lastValue = curve.Evaluate(frameIter / clip.SampleRate);
                samples[curveIndex + frameIter * curveCount] = lastValue;
            }

            // adjust last frame value to match value at duration
            var valueAtDuration = curve.Evaluate(clip.Duration);

            samples[curveIndex + clip.FrameCount * curveCount] = Core.AdjustLastFrameValue(lastValue, valueAtDuration, clip.LastFrameError);
        }

#else
        public static BlobAssetReference<Clip> ToDenseClip(this AnimationClip sourceClip, BindingHashDelegate bindingHash = null) =>
            default;
#endif
    }
}

namespace Unity.Animation
{
    using Unity.Animation.Hybrid;

    public static class ClipBuilder
    {
        [System.Obsolete("ClipBuilder.AnimationClipToDenseClip has been deprecated. Use AnimationClip.ToDenseClip instead. (RemovedAfter 2020-07-15)")]
        public static BlobAssetReference<Clip> AnimationClipToDenseClip(AnimationClip sourceClip, BindingHashDelegate bindingHash = null) =>
            sourceClip.ToDenseClip(bindingHash);
    }
}
