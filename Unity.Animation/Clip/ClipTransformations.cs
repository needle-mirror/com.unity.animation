using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using UnityEngine.Assertions;


namespace Unity.Animation
{
    public static class ClipTransformations
    {
        /// <summary>
        /// Create a clone of a BlobAssetReference
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static unsafe BlobAssetReference<T> Clone<T>(this BlobAssetReference<T> source) where T : struct
        {
            if (source == BlobAssetReference<T>.Null)
                return BlobAssetReference<T>.Null;

            var writer = new MemoryBinaryWriter();
            writer.Write(source);
            var reader = new MemoryBinaryReader(writer.Data);
            var clone = reader.Read<T>();
            writer.Dispose();
            reader.Dispose();

            return clone;
        }

        /// <summary>
        /// Given a clip and frame, creates a blob asset of a single pose at that frame
        /// </summary>
        /// <param name="source"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static unsafe BlobAssetReference<Clip> CreatePose(BlobAssetReference<Clip> source, int frame = 0)
        {
            if (source == BlobAssetReference<Clip>.Null)
                return BlobAssetReference<Clip>.Null;

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var clip = ref blobBuilder.ConstructRoot<Clip>();

            // Copy the bindings
            CopyArray(ref blobBuilder, ref clip.Bindings.TranslationBindings, ref source.Value.Bindings.TranslationBindings);
            CopyArray(ref blobBuilder, ref clip.Bindings.RotationBindings, ref source.Value.Bindings.RotationBindings);
            CopyArray(ref blobBuilder, ref clip.Bindings.ScaleBindings, ref source.Value.Bindings.ScaleBindings);
            CopyArray(ref blobBuilder, ref clip.Bindings.FloatBindings, ref source.Value.Bindings.FloatBindings);
            CopyArray(ref blobBuilder, ref clip.Bindings.IntBindings, ref source.Value.Bindings.IntBindings);

            clip.SampleRate = source.Value.SampleRate;
            clip.Duration = 0;
            clip.Bindings = clip.CreateBindingSet(
                source.Value.Bindings.TranslationBindings.Length,
                source.Value.Bindings.RotationBindings.Length,
                source.Value.Bindings.ScaleBindings.Length,
                source.Value.Bindings.FloatBindings.Length,
                source.Value.Bindings.IntBindings.Length
            );

            int sampleCount = source.Value.Bindings.CurveCount;
            var samples = blobBuilder.Allocate(ref clip.Samples, sampleCount);

            frame = math.clamp(frame, 0, source.Value.FrameCount);

            // copy the frame from the source blob
            var pSrc = (float*)source.Value.Samples.GetUnsafePtr();
            UnsafeUtility.MemCpy(samples.GetUnsafePtr(), pSrc + sampleCount * frame, sampleCount * UnsafeUtility.SizeOf<float>());

            var outputClip = blobBuilder.CreateBlobAssetReference<Clip>(Allocator.Persistent);
            blobBuilder.Dispose();

            outputClip.Value.m_HashCode = (int)HashUtils.ComputeHash(ref outputClip);

            return outputClip;
        }

        /// <summary>
        /// Reverses a clip.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static unsafe BlobAssetReference<Clip> Reverse(BlobAssetReference<Clip> source)
        {
            if (source == BlobAssetReference<Clip>.Null)
                return BlobAssetReference<Clip>.Null;

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var clip = ref blobBuilder.ConstructRoot<Clip>();

            // Copy the bindings
            CopyArray(ref blobBuilder, ref clip.Bindings.TranslationBindings, ref source.Value.Bindings.TranslationBindings);
            CopyArray(ref blobBuilder, ref clip.Bindings.RotationBindings, ref source.Value.Bindings.RotationBindings);
            CopyArray(ref blobBuilder, ref clip.Bindings.ScaleBindings, ref source.Value.Bindings.ScaleBindings);
            CopyArray(ref blobBuilder, ref clip.Bindings.FloatBindings, ref source.Value.Bindings.FloatBindings);
            CopyArray(ref blobBuilder, ref clip.Bindings.IntBindings, ref source.Value.Bindings.IntBindings);

            clip.SampleRate = source.Value.SampleRate;
            clip.Duration = source.Value.Duration;
            clip.Bindings = clip.CreateBindingSet(
                source.Value.Bindings.TranslationBindings.Length,
                source.Value.Bindings.RotationBindings.Length,
                source.Value.Bindings.ScaleBindings.Length,
                source.Value.Bindings.FloatBindings.Length,
                source.Value.Bindings.IntBindings.Length
            );

            var samples = blobBuilder.Allocate(ref clip.Samples, source.Value.Samples.Length);
            var src = (float*)source.Value.Samples.GetUnsafePtr();
            var dest = (float*)samples.GetUnsafePtr();

            var len = source.Value.FrameCount + 1;
            var curveCount = source.Value.Bindings.CurveCount;
            var copySize = curveCount * UnsafeUtility.SizeOf<float>();

            var startOffset = 0;
            var endOffset = (len - 1) * curveCount;

            var error = source.Value.LastFrameError;
            if (error < 1.0f && len > 1)
            {
                var t = 1f - error;

                // the clip isn't frame aligned, so we need to resample. This will introduce an error on the last frame, as we no longer have the curve
                // to sample at -error
                for (var i = 0; i < len - 1; i++, startOffset += curveCount, endOffset -= curveCount)
                {
                    var prevOffset = endOffset - curveCount;

                    float* pDest = dest + startOffset;
                    float* pSrcA = src + prevOffset;
                    float* pSrcB = src + endOffset;
                    for (int j = 0; j < curveCount; j++, pDest++, pSrcA++, pSrcB++)
                        *pDest = math.lerp(*pSrcA, *pSrcB, t);
                }

                // adjust the last frame to account for sampling error, so that dest(duration) evaluates to src(0)
                {
                    float* pDest = dest + startOffset;
                    float* pPrev = dest + startOffset - curveCount;
                    float* pTargetValue = src;
                    for (int j = 0; j < curveCount; j++, pDest++, pPrev++, pTargetValue++)
                        *pDest = ((*pTargetValue) + (t - 1f) * (*pPrev)) / t;
                }
            }
            else
            {
                for (var i = 0; i < len; i++, startOffset += curveCount, endOffset -= curveCount)
                    UnsafeUtility.MemCpy(dest + startOffset, src + endOffset, copySize);
            }


            var outputClip = blobBuilder.CreateBlobAssetReference<Clip>(Allocator.Persistent);
            blobBuilder.Dispose();

            outputClip.Value.m_HashCode = (int)HashUtils.ComputeHash(ref outputClip);

            return outputClip;
        }

        /// <summary>
        /// Given two clips, returns a clip that contains only data for the given binding set
        /// </summary>
        /// <param name="source"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static unsafe BlobAssetReference<Clip> FilterBindings(BlobAssetReference<Clip> source, ref BindingSet filter)
        {
            if (source == BlobAssetReference<Clip>.Null || filter.BindingCount == 0)
                return BlobAssetReference<Clip>.Null;

            // if they are already equal, return a clone of the clip.
            var isEqual =
                ArrayEqual(ref source.Value.Bindings.TranslationBindings, ref filter.TranslationBindings) &&
                ArrayEqual(ref source.Value.Bindings.RotationBindings, ref filter.RotationBindings) &&
                ArrayEqual(ref source.Value.Bindings.ScaleBindings, ref filter.ScaleBindings) &&
                ArrayEqual(ref source.Value.Bindings.FloatBindings, ref filter.FloatBindings) &&
                ArrayEqual(ref source.Value.Bindings.IntBindings, ref filter.IntBindings);

            if (isEqual)
                return source.Clone();

            var list = new NativeList<StringHash>(source.Value.Bindings.BindingCount, Allocator.Temp);
            var blobBuilder = new BlobBuilder(Allocator.Temp);

            ref var clip = ref blobBuilder.ConstructRoot<Clip>();
            clip.SampleRate = source.Value.SampleRate;
            clip.Duration = source.Value.Duration;

            // compute the intersection
            Intersection(ref source.Value.Bindings.TranslationBindings, ref filter.TranslationBindings, ref list);
            SetBlobArray(ref blobBuilder, ref clip.Bindings.TranslationBindings, ref list, out var transBindings);

            Intersection(ref source.Value.Bindings.RotationBindings, ref filter.RotationBindings, ref list);
            SetBlobArray(ref blobBuilder, ref clip.Bindings.RotationBindings, ref list, out var rotBindings);

            Intersection(ref source.Value.Bindings.ScaleBindings, ref filter.ScaleBindings, ref list);
            SetBlobArray(ref blobBuilder, ref clip.Bindings.ScaleBindings, ref list, out var scaleBindings);

            Intersection(ref source.Value.Bindings.FloatBindings, ref filter.FloatBindings, ref list);
            SetBlobArray(ref blobBuilder, ref clip.Bindings.FloatBindings, ref list, out var floatBindings);

            Intersection(ref source.Value.Bindings.IntBindings, ref filter.IntBindings, ref list);
            SetBlobArray(ref blobBuilder, ref clip.Bindings.IntBindings, ref list, out var intBindings);

            clip.Bindings = clip.CreateBindingSet(
                transBindings.Length,
                rotBindings.Length,
                scaleBindings.Length,
                floatBindings.Length,
                intBindings.Length
            );

            var destStride = clip.Bindings.CurveCount;
            var numSamples = source.Value.FrameCount + 1;
            var destSize = destStride * numSamples;
            var samples = blobBuilder.Allocate(ref clip.Samples, destSize);
            var sourceStride = source.Value.Bindings.CurveCount;

            // copy the subset of data
            var destPtr = (float*)samples.GetUnsafePtr();
            var srcPtr = (float*)source.Value.Samples.GetUnsafePtr();

#if !UNITY_DISABLE_ANIMATION_CHECKS
            // will trigger if the data layout order changes.
            Assert.IsTrue(source.Value.Bindings.TranslationSamplesOffset <= source.Value.Bindings.ScaleSamplesOffset &&
                source.Value.Bindings.ScaleSamplesOffset <= source.Value.Bindings.FloatSamplesOffset &&
                source.Value.Bindings.FloatSamplesOffset <= source.Value.Bindings.IntSamplesOffset &&
                source.Value.Bindings.IntSamplesOffset <= source.Value.Bindings.RotationSamplesOffset, "Clip Data Layout has changed.");
#endif

            InterleavedBlit(ref rotBindings, ref source.Value.Bindings.RotationBindings, destPtr + clip.Bindings.RotationSamplesOffset, srcPtr + source.Value.Bindings.RotationSamplesOffset, BindingSet.RotationKeyFloatCount, destStride, sourceStride, numSamples);

            InterleavedBlit(ref transBindings, ref source.Value.Bindings.TranslationBindings, destPtr + clip.Bindings.TranslationSamplesOffset, srcPtr + source.Value.Bindings.TranslationSamplesOffset, BindingSet.TranslationKeyFloatCount, destStride, sourceStride, numSamples);

            InterleavedBlit(ref scaleBindings, ref source.Value.Bindings.ScaleBindings, destPtr + clip.Bindings.ScaleSamplesOffset, srcPtr + source.Value.Bindings.ScaleSamplesOffset, BindingSet.ScaleKeyFloatCount, destStride, sourceStride, numSamples);

            InterleavedBlit(ref floatBindings, ref source.Value.Bindings.FloatBindings, destPtr + clip.Bindings.FloatSamplesOffset, srcPtr + source.Value.Bindings.FloatSamplesOffset, BindingSet.FloatKeyFloatCount, destStride, sourceStride, numSamples);

            InterleavedBlit(ref floatBindings, ref source.Value.Bindings.IntBindings, destPtr + clip.Bindings.IntSamplesOffset, srcPtr + source.Value.Bindings.IntSamplesOffset, BindingSet.IntKeyFloatCount, destStride, sourceStride, numSamples);

            list.Dispose();
            var outputClip = blobBuilder.CreateBlobAssetReference<Clip>(Allocator.Persistent);
            blobBuilder.Dispose();

            outputClip.Value.m_HashCode = (int)HashUtils.ComputeHash(ref outputClip);

            return outputClip;
        }

        private static unsafe void CopyArray<T>(ref BlobBuilder builder, ref BlobArray<T> dest, ref BlobArray<T> src) where T : struct
        {
            var bindings = builder.Allocate(ref dest, src.Length);
            UnsafeUtility.MemCpy(bindings.GetUnsafePtr(), src.GetUnsafePtr(), src.Length * UnsafeUtility.SizeOf<T>());
        }

        private static unsafe bool ArrayEqual<T>(ref BlobArray<T> a, ref BlobArray<T> b) where T : struct
        {
            return (a.Length == b.Length) &&
                ((a.Length == 0) || (UnsafeUtility.MemCmp(a.GetUnsafePtr(), b.GetUnsafePtr(), a.Length * UnsafeUtility.SizeOf<T>()) == 0));
        }

        private static void Intersection<T>(ref BlobArray<T> a, ref BlobArray<T> b, ref NativeList<T> result) where T : struct, IEquatable<T>
        {
            result.Clear();
            for (int i = 0; i < a.Length; i++)
            {
                T x = a[i];
                for (int j = 0; j < b.Length; j++)
                {
                    if (x.Equals(b[j]))
                    {
                        result.Add(x);
                        break;
                    }
                }
            }
        }

        private static unsafe void SetBlobArray<T>(ref BlobBuilder builder, ref BlobArray<T> a, ref NativeList<T> result, out BlobBuilderArray<T> blobArray) where T : struct
        {
            blobArray = builder.Allocate(ref a, result.Length);
            UnsafeUtility.MemCpy(blobArray.GetUnsafePtr(), result.GetUnsafePtr(), result.Length * UnsafeUtility.SizeOf<T>());
        }

        private static unsafe void InterleavedBlit(ref BlobBuilderArray<StringHash> destBindings, ref BlobArray<StringHash> sourceBindings, float* dest, float* src, int dataItemCount, int destStride, int sourceStride, int samples)
        {
            int dataStride = dataItemCount * UnsafeUtility.SizeOf<float>();
            // copy data from the source clip
            int destOffset = 0;
            for (int i = 0; i < destBindings.Length; i++, destOffset += dataItemCount)
            {
                var b = destBindings[i];
                var sourceIndex = Core.FindBindingIndex(ref sourceBindings, b);
                for (int j = 0; j < samples; j++)
                {
                    var d = destOffset + j * destStride;
                    var s = sourceIndex * dataItemCount + j * sourceStride;
                    UnsafeUtility.MemCpy(dest + d, src + s, dataStride);
                }
            }
        }
    }
}
