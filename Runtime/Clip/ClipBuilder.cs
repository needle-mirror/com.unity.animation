using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEditor;

namespace Unity.Animation
{
    public static class ClipBuilder
    {
#if UNITY_EDITOR
        public static BlobAssetReference<Clip> AnimationClipToDenseClip(AnimationClip sourceClip)
        {
            if (sourceClip == null)
                return new BlobAssetReference<Clip>();

            var srcBindings = AnimationUtility.GetCurveBindings(sourceClip);

            var translationBindings = new List<EditorCurveBinding>();
            var rotationBindings = new List<EditorCurveBinding>();
            var scaleBindings = new List<EditorCurveBinding>();
            var floatBindings = new List<EditorCurveBinding>();
            var intBindings = new List<EditorCurveBinding>();

            foreach (var binding in srcBindings)
            {
                if (binding.propertyName == "m_LocalPosition.x")
                {
                    translationBindings.Add(binding);
                }
                else if (binding.propertyName == "m_LocalRotation.x")
                {
                    rotationBindings.Add(binding);
                }
                else if (binding.propertyName == "m_LocalScale.x")
                {
                    scaleBindings.Add(binding);
                }
                else if(binding.type == typeof(Animator))
                {
                    floatBindings.Add(binding);
                }
            }

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var clip = ref blobBuilder.ConstructRoot<Clip>();

            CreateBindings(translationBindings, rotationBindings, scaleBindings, floatBindings, intBindings, ref blobBuilder, ref clip);
            FillCurves(sourceClip, translationBindings, rotationBindings, scaleBindings, floatBindings, intBindings, ref blobBuilder, ref clip);

            var outputClip = blobBuilder.CreateBlobAssetReference<Clip>(Allocator.Persistent);

            blobBuilder.Dispose();

            return outputClip;
        }

        private static void CreateBindings(IReadOnlyList<EditorCurveBinding> translationBindings,
            IReadOnlyList<EditorCurveBinding> rotationBindings, IReadOnlyList<EditorCurveBinding> scaleBindings,
            IReadOnlyList<EditorCurveBinding> floatBindings, IReadOnlyList<EditorCurveBinding> intBindings,
            ref BlobBuilder blobBuilder, ref Clip clip)
        {
            ref var bindings = ref clip.Bindings;

            FillBlobTransformBindingBuffer(translationBindings, ref blobBuilder, ref bindings.TranslationBindings);
            FillBlobTransformBindingBuffer(rotationBindings, ref blobBuilder, ref bindings.RotationBindings);
            FillBlobTransformBindingBuffer(scaleBindings, ref blobBuilder, ref bindings.ScaleBindings);
            FillBlobBindingBuffer(floatBindings, ref blobBuilder, ref bindings.FloatBindings);
            FillBlobBindingBuffer(intBindings, ref blobBuilder, ref bindings.IntBindings);
        }

        private static void FillBlobTransformBindingBuffer(IReadOnlyList<EditorCurveBinding> bindings, ref BlobBuilder blobBuilder, ref BlobArray<StringHash> blobBuffer)
        {
            if (bindings == null || bindings.Count == 0)
                return;

            var arrayBuilder = blobBuilder.Allocate(ref blobBuffer, bindings.Count);
            for (var i = 0; i != bindings.Count; ++i)
                arrayBuilder[i] = bindings[i].path;
        }

        private static void FillBlobBindingBuffer(IReadOnlyList<EditorCurveBinding> bindings, ref BlobBuilder blobBuilder, ref BlobArray<StringHash> blobBuffer)
        {
            if (bindings == null || bindings.Count == 0)
                return;

            var arrayBuilder = blobBuilder.Allocate(ref blobBuffer, bindings.Count);
            for (var i = 0; i != bindings.Count; ++i)
                arrayBuilder[i] = bindings[i].propertyName;
        }

        private static void FillCurves(AnimationClip sourceClip, IReadOnlyList<EditorCurveBinding> translationBindings,
            IReadOnlyList<EditorCurveBinding> rotationBindings, IReadOnlyList<EditorCurveBinding> scaleBindings,
            IReadOnlyList<EditorCurveBinding> floatBindings, IReadOnlyList<EditorCurveBinding> intBindings,
            ref BlobBuilder blobBuilder, ref Clip clip)
        {
            clip.Duration = sourceClip.length;
            clip.SampleRate = sourceClip.frameRate;

            var curveCount =
                translationBindings.Count * BindingSet.TranslationKeyFloatCount +
                rotationBindings.Count * BindingSet.RotationKeyFloatCount +
                scaleBindings.Count * BindingSet.ScaleKeyFloatCount +
                floatBindings.Count * BindingSet.FloatKeyFloatCount +
                intBindings.Count * BindingSet.IntKeyFloatCount;

            var sampleCount = curveCount * (clip.FrameCount + 1);
            if (sampleCount > 0)
            {
                var arrayBuilder = blobBuilder.Allocate(ref clip.Samples, sampleCount);

                // Translation curves
                var curveIndex = 0;
                for (var i = 0; i != translationBindings.Count; ++i)
                    curveIndex = AddVector3Curve(ref clip, ref arrayBuilder, sourceClip, translationBindings[i], "m_LocalPosition", curveIndex, curveCount);

                // Rotation curves
                for (var i = 0; i != rotationBindings.Count; ++i)
                    curveIndex = AddQuaternionCurve(ref clip, ref arrayBuilder, sourceClip, rotationBindings[i], "m_LocalRotation", curveIndex, curveCount);

                // Scale curves
                for (var i = 0; i != scaleBindings.Count; ++i)
                    curveIndex = AddVector3Curve(ref clip, ref arrayBuilder, sourceClip, scaleBindings[i], "m_LocalScale", curveIndex, curveCount);

                // Float curves
                for (var i = 0; i != floatBindings.Count; ++i)
                    curveIndex = AddFloatCurve(ref clip, ref arrayBuilder, sourceClip, floatBindings[i], curveIndex, curveCount);

                // Int curves
                for (var i = 0; i != intBindings.Count; ++i)
                    curveIndex = AddFloatCurve(ref clip, ref arrayBuilder, sourceClip, intBindings[i], curveIndex, curveCount);
            }
        }

        private static int AddVector3Curve(ref Clip clip, ref BlobBuilderArray<float> samples, AnimationClip sourceClip, EditorCurveBinding binding,
            string property, int curveIndex, in int curveCount)
        {
            binding.propertyName = property + ".x";
            ConvertCurve(ref clip, ref samples, AnimationUtility.GetEditorCurve(sourceClip, binding), curveIndex++, curveCount);

            binding.propertyName = property + ".y";
            ConvertCurve(ref clip, ref samples, AnimationUtility.GetEditorCurve(sourceClip, binding), curveIndex++, curveCount);

            binding.propertyName = property + ".z";
            ConvertCurve(ref clip, ref samples, AnimationUtility.GetEditorCurve(sourceClip, binding), curveIndex++, curveCount);

            return curveIndex;
        }

        private static int AddQuaternionCurve(ref Clip clip, ref BlobBuilderArray<float> samples, AnimationClip sourceClip, EditorCurveBinding binding,
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

            return curveIndex;
        }

        private static int AddFloatCurve(ref Clip clip, ref BlobBuilderArray<float> samples, AnimationClip sourceClip, EditorCurveBinding binding,
            int curveIndex, in int curveCount)
        {
            ConvertCurve(ref clip, ref samples, AnimationUtility.GetEditorCurve(sourceClip, binding), curveIndex++, curveCount);
            return curveIndex;
        }

        private static void ConvertCurve(ref Clip clip, ref BlobBuilderArray<float> samples, AnimationCurve curve, int curveIndex, in int curveCount)
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
        public static BlobAssetReference<Clip> AnimationClipToDenseClip(AnimationClip sourceClip,
            string skeletonRootPath, string rootMotionPath)
        {
            return new BlobAssetReference<Clip>();
        }
#endif
    }
}