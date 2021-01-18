using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Unity.Animation
{
    [BurstCompatible]
    static public partial class Core
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int AlignUp(int a, int b)
        {
            return ((a + b - 1) / b) * b;
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void SetDataInSample<T>(ref BlobArray<float> samples, int offset, T data)
            where T : unmanaged
        {
            *(T*)((float*)samples.GetUnsafePtr() + offset) = data;
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe ref T GetDataInSample<T>(ref BlobArray<float> samples, int offset)
            where T : unmanaged
        {
            return ref *(T*)((float*)samples.GetUnsafePtr() + offset);
        }

        /// Find a binding in a binding array.
        public static int FindBindingIndex(ref BlobArray<StringHash> bindings, StringHash searchedBinding)
        {
            // TODO : Optimize
            var i = 0;
            while (i < bindings.Length && !bindings[i].Equals(searchedBinding))
                ++i;
            return i < bindings.Length ? i : -1;
        }

        unsafe static public void Blend(
            ref AnimationStream output,
            ref AnimationStream input1,
            ref AnimationStream input2,
            float weight,
            NativeArray<WeightData> weightMasks
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(ref input1);
            output.ValidateRigEquality(ref input2);
            output.ValidateCompatibility(weightMasks);

            int wIdx = 0;
            float4* wMask = (float4*)weightMasks.GetUnsafeReadOnlyPtr();

            // Blend 4-wide lerp non rotation data
            float4* input1Data = input1.GetInterpolatedDataChunkUnsafePtr();
            float4* input2Data = input2.GetInterpolatedDataChunkUnsafePtr();
            float4* outputData = output.GetInterpolatedDataChunkUnsafePtr();
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputData[i] = math.lerp(input1Data[i], input2Data[i], wMask[wIdx] * weight);
            }

            int4* input1IntData = input1.GetDiscreteDataChunkUnsafePtr();
            int4* input2IntData = input2.GetDiscreteDataChunkUnsafePtr();
            int4* outputIntData = output.GetDiscreteDataChunkUnsafePtr();
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputIntData[i] = math.select(input1IntData[i], input2IntData[i], wMask[wIdx] * weight > 0.5f);
            }

            // Blend 4-wide rotations
            quaternion4* input1Rot = input1.GetRotationDataChunkUnsafePtr();
            quaternion4* input2Rot = input2.GetRotationDataChunkUnsafePtr();
            quaternion4* outputRot = output.GetRotationDataChunkUnsafePtr();
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputRot[i] = mathex.lerp(input1Rot[i], input2Rot[i], wMask[wIdx] * weight);
            }

            output.OrMasks(ref input1, ref input2);
        }

        unsafe static public void Blend(
            ref AnimationStream output,
            ref AnimationStream input1,
            ref AnimationStream input2,
            float weight
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(ref input1);
            output.ValidateRigEquality(ref input2);

            // Blend 4 wide lerp non rotation data
            float4* input1Data = input1.GetInterpolatedDataChunkUnsafePtr();
            float4* input2Data = input2.GetInterpolatedDataChunkUnsafePtr();
            float4* outputData = output.GetInterpolatedDataChunkUnsafePtr();
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i)
            {
                outputData[i] = math.lerp(input1Data[i], input2Data[i], weight);
            }

            int4* input1IntData = input1.GetDiscreteDataChunkUnsafePtr();
            int4* input2IntData = input2.GetDiscreteDataChunkUnsafePtr();
            int4* outputIntData = output.GetDiscreteDataChunkUnsafePtr();
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i)
            {
                outputIntData[i] = math.select(input1IntData[i], input2IntData[i], weight > 0.5f);
            }

            // Blend 4-wide rotations
            quaternion4* inputRot1 = input1.GetRotationDataChunkUnsafePtr();
            quaternion4* inputRot2 = input2.GetRotationDataChunkUnsafePtr();
            quaternion4* outputRot = output.GetRotationDataChunkUnsafePtr();
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
            {
                outputRot[i] = mathex.lerp(inputRot1[i], inputRot2[i], weight);
            }

            output.OrMasks(ref input1, ref input2);
        }

        static unsafe public void BlendOverrideLayer(
            ref AnimationStream output,
            ref AnimationStream input,
            float weight,
            NativeArray<WeightData> weightMasks
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(ref input);
            output.ValidateCompatibility(weightMasks);

            int wIdx = 0;
            float4* wMask = (float4*)weightMasks.GetUnsafeReadOnlyPtr();

            // Blend 4-wide lerp non rotation data
            float4* inputData  = input.GetInterpolatedDataChunkUnsafePtr();
            float4* outputData = output.GetInterpolatedDataChunkUnsafePtr();
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputData[i] = math.lerp(outputData[i], inputData[i], wMask[wIdx] * weight);
            }

            int4* inputIntData = input.GetDiscreteDataChunkUnsafePtr();
            int4* outputIntData = output.GetDiscreteDataChunkUnsafePtr();
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputIntData[i] = math.select(outputIntData[i], inputIntData[i], wMask[wIdx] * weight > 0.5f);
            }

            // Blend 4-wide rotations
            quaternion4* inputRot  = input.GetRotationDataChunkUnsafePtr();
            quaternion4* outputRot = output.GetRotationDataChunkUnsafePtr();
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputRot[i] = mathex.lerp(outputRot[i], inputRot[i], wMask[wIdx] * weight);
            }

            output.OrMasks(ref input);
        }

        static unsafe public void BlendOverrideLayer(
            ref AnimationStream output,
            ref AnimationStream input,
            float weight
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(ref input);

            // Blend 4-wide lerp non rotation data
            float4* inputData  = input.GetInterpolatedDataChunkUnsafePtr();
            float4* outputData = output.GetInterpolatedDataChunkUnsafePtr();
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i)
            {
                outputData[i] = math.lerp(outputData[i], inputData[i], weight);
            }

            int4* inputIntData = input.GetDiscreteDataChunkUnsafePtr();
            int4* outputIntData = output.GetDiscreteDataChunkUnsafePtr();
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i)
            {
                outputIntData[i] = math.select(outputIntData[i], inputIntData[i], weight > 0.5f);
            }

            // Blend 4-wide rotations
            quaternion4* inputRot  = input.GetRotationDataChunkUnsafePtr();
            quaternion4* outputRot = output.GetRotationDataChunkUnsafePtr();
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
            {
                outputRot[i] = mathex.lerp(outputRot[i], inputRot[i], weight);
            }

            output.OrMasks(ref input);
        }

        static unsafe public void BlendAdditiveLayer(
            ref AnimationStream output,
            ref AnimationStream input,
            float weight,
            NativeArray<WeightData> weightMasks
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(ref input);
            output.ValidateCompatibility(weightMasks);

            int wIdx = 0;
            float4* wMask = (float4*)weightMasks.GetUnsafeReadOnlyPtr();

            // Blend 4-wide lerp non rotation data
            float4* inputData  = input.GetInterpolatedDataChunkUnsafePtr();
            float4* outputData = output.GetInterpolatedDataChunkUnsafePtr();
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputData[i] = math.mad(inputData[i], wMask[wIdx] * weight, outputData[i]);
            }

            int4* inputIntData = input.GetDiscreteDataChunkUnsafePtr();
            int4* outputIntData = output.GetDiscreteDataChunkUnsafePtr();
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputIntData[i] += math.select(0, inputIntData[i], wMask[wIdx] * weight > 0.5f);
            }

            // Blend 4-wide rotations
            quaternion4* inputRot  = input.GetRotationDataChunkUnsafePtr();
            quaternion4* outputRot = output.GetRotationDataChunkUnsafePtr();
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputRot[i] = mathex.mul(outputRot[i], mathex.quatWeight(inputRot[i], wMask[wIdx] * weight));
            }

            output.OrMasks(ref input);
        }

        static unsafe public void BlendAdditiveLayer(
            ref AnimationStream output,
            ref AnimationStream input,
            float weight
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(ref input);

            ref var bindings = ref output.Rig.Value.Bindings;

            // Blend 4-wide lerp non rotation data
            float4* inputData  = input.GetInterpolatedDataChunkUnsafePtr();
            float4* outputData = output.GetInterpolatedDataChunkUnsafePtr();
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i)
            {
                outputData[i] = math.mad(inputData[i], weight, outputData[i]);
            }

            int4* inputIntData = input.GetDiscreteDataChunkUnsafePtr();
            int4* outputIntData = output.GetDiscreteDataChunkUnsafePtr();
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i)
            {
                outputIntData[i] += math.select(0, inputIntData[i], weight > 0.5f);
            }

            // Blend 4-wide rotations
            quaternion4* inputRot  = input.GetRotationDataChunkUnsafePtr();
            quaternion4* outputRot = output.GetRotationDataChunkUnsafePtr();
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
            {
                outputRot[i] = mathex.mul(outputRot[i], mathex.quatWeight(inputRot[i], weight));
            }

            output.OrMasks(ref input);
        }

        static public void RigRemapper(
            BlobAssetReference<RigRemapTable> remapTable,
            ref AnimationStream destinationStream,
            ref AnimationStream sourceStream
        )
        {
            destinationStream.ValidateIsNotNull();
            sourceStream.ValidateIsNotNull();

            ValidateArgumentIsCreated(remapTable);

            ref var remap = ref remapTable.Value;

            for (int i = 0, count = remap.LocalToParentTRCount.x; i < count; i++)
            {
                ref var mapping = ref remap.TranslationMappings[i];
                var value = sourceStream.GetLocalToParentTranslation(mapping.SourceIndex);

                if (mapping.OffsetIndex > 0)
                {
                    var translationOffset = remapTable.Value.TranslationOffsets[mapping.OffsetIndex];
                    value = math.mul(translationOffset.Rotation, value * translationOffset.Scale);
                }

                destinationStream.SetLocalToParentTranslation(mapping.DestinationIndex, value);
            }

            for (int i = 0, count = remap.LocalToParentTRCount.y; i < count; i++)
            {
                ref var mapping = ref remap.RotationMappings[i];
                var value = sourceStream.GetLocalToParentRotation(mapping.SourceIndex);

                if (mapping.OffsetIndex > 0)
                {
                    var rotationOffset = remapTable.Value.RotationOffsets[mapping.OffsetIndex];
                    value = math.mul(math.mul(rotationOffset.PreRotation, value), rotationOffset.PostRotation);
                }

                destinationStream.SetLocalToParentRotation(mapping.DestinationIndex, value);
            }

            for (int i = 0; i < remap.ScaleMappings.Length; i++)
            {
                ref var mapping = ref remap.ScaleMappings[i];
                var value = sourceStream.GetLocalToParentScale(mapping.SourceIndex);
                destinationStream.SetLocalToParentScale(mapping.DestinationIndex, value);
            }

            for (int i = 0; i < remap.FloatMappings.Length; i++)
            {
                ref var mapping = ref remap.FloatMappings[i];
                var value = sourceStream.GetFloat(mapping.SourceIndex);
                destinationStream.SetFloat(mapping.DestinationIndex, value);
            }

            for (int i = 0; i < remap.IntMappings.Length; i++)
            {
                ref var mapping = ref remap.IntMappings[i];
                var value = sourceStream.GetInt(mapping.SourceIndex);
                destinationStream.SetInt(mapping.DestinationIndex, value);
            }

            for (int i = 0; i < remap.SortedLocalToRootTREntries.Length; i++)
            {
                ref var localToRootTREntry = ref remap.SortedLocalToRootTREntries[i];
                if (localToRootTREntry.x != -1 && localToRootTREntry.y != -1)
                {
                    ref var translationMapping = ref remap.TranslationMappings[localToRootTREntry.x];
                    ref var rotationMapping = ref remap.RotationMappings[localToRootTREntry.y];

                    ValidateAreEqual(translationMapping.DestinationIndex, rotationMapping.DestinationIndex);

                    float3 tValue;
                    quaternion rValue;
                    if (translationMapping.SourceIndex == rotationMapping.SourceIndex)
                    {
                        sourceStream.GetLocalToRootTR(translationMapping.SourceIndex, out tValue, out rValue);
                    }
                    else
                    {
                        tValue = sourceStream.GetLocalToRootTranslation(translationMapping.SourceIndex);
                        rValue = sourceStream.GetLocalToRootRotation(rotationMapping.SourceIndex);
                    }

                    if (translationMapping.OffsetIndex > 0)
                    {
                        var offset = remapTable.Value.TranslationOffsets[translationMapping.OffsetIndex];
                        tValue = math.mul(offset.Rotation, tValue * offset.Scale);
                    }

                    if (rotationMapping.OffsetIndex > 0)
                    {
                        var offset = remapTable.Value.RotationOffsets[rotationMapping.OffsetIndex];
                        rValue = math.mul(math.mul(offset.PreRotation, rValue), offset.PostRotation);
                    }

                    destinationStream.SetLocalToRootTR(translationMapping.DestinationIndex, tValue, rValue);
                }
                else if (localToRootTREntry.y == -1)
                {
                    ref var mapping = ref remap.TranslationMappings[localToRootTREntry.x];
                    var value = sourceStream.GetLocalToRootTranslation(mapping.SourceIndex);

                    if (mapping.OffsetIndex > 0)
                    {
                        var offset = remapTable.Value.TranslationOffsets[mapping.OffsetIndex];
                        value = math.mul(offset.Rotation, value * offset.Scale);
                    }

                    destinationStream.SetLocalToRootTranslation(mapping.DestinationIndex, value);
                }
                else
                {
                    ref var mapping = ref remap.RotationMappings[localToRootTREntry.y];
                    var value = sourceStream.GetLocalToRootRotation(mapping.SourceIndex);

                    if (mapping.OffsetIndex > 0)
                    {
                        var offset = remapTable.Value.RotationOffsets[mapping.OffsetIndex];
                        value = math.mul(math.mul(offset.PreRotation, value), offset.PostRotation);
                    }

                    destinationStream.SetLocalToRootRotation(mapping.DestinationIndex, value);
                }
            }
        }

        static private float WeightForIndex(ref BlobArray<float> thresholdArray, int length, int index, float blend)
        {
            if (blend >= thresholdArray[index])
            {
                if (index + 1 == length)
                {
                    return 1.0f;
                }
                else if (thresholdArray[index + 1] < blend)
                {
                    return 0.0f;
                }
                else
                {
                    if (thresholdArray[index] - thresholdArray[index + 1] != 0)
                    {
                        return (blend - thresholdArray[index + 1]) / (thresholdArray[index] - thresholdArray[index + 1]);
                    }
                    else
                    {
                        return 1.0f;
                    }
                }
            }
            else
            {
                if (index == 0)
                {
                    return 1.0f;
                }
                else if (thresholdArray[index - 1] > blend)
                {
                    return 0.0f;
                }
                else
                {
                    if ((thresholdArray[index] - thresholdArray[index - 1]) != 0)
                    {
                        return (blend - thresholdArray[index - 1]) / (thresholdArray[index] - thresholdArray[index - 1]);
                    }
                    else
                    {
                        return 1.0f;
                    }
                }
            }
        }

        static public void ComputeBlendTree1DWeights(BlobAssetReference<BlendTree1D> blendTree, float blendParameter, ref NativeArray<float> outWeights)
        {
            var length = blendTree.Value.MotionThresholds.Length;

            var blend = math.clamp(blendParameter, blendTree.Value.MotionThresholds[0], blendTree.Value.MotionThresholds[length - 1]);
            for (int i = 0; i < length; i++)
            {
                outWeights[i] = WeightForIndex(ref blendTree.Value.MotionThresholds, length, i, blend);
            }
        }

        static public float ComputeBlendTree1DDuration(BlobAssetReference<BlendTree1D> blendTree, ref NativeArray<float> weights)
        {
            var length = blendTree.Value.MotionThresholds.Length;

            var duration = 0.0f;
            for (int i = 0; i < length; i++)
            {
                duration += weights[i] * blendTree.Value.Motions[i].Value.Duration / blendTree.Value.MotionSpeeds[i];
            }

            return duration;
        }

        static public unsafe void ComputeBlendTree2DSimpleDirectionalWeights(BlobAssetReference<BlendTree2DSimpleDirectional> blendTree, float2 blendParameter, ref NativeArray<float> outWeights)
        {
            var length = blendTree.Value.Motions.Length;

            UnsafeUtility.MemClear(outWeights.GetUnsafePtr(), UnsafeUtility.SizeOf<float>() * outWeights.Length);

            // Handle fallback
            if (length < 2)
            {
                if (length == 1)
                    outWeights[0] = 1.0F;
                return;
            }

            // Handle special case when sampled exactly in the middle
            if (math.all(blendParameter == float2.zero))
            {
                // If we have a center motion, give that one all the weight
                for (int i = 0; i < length; i++)
                {
                    if (math.all(blendTree.Value.MotionPositions[i] == float2.zero))
                    {
                        outWeights[i] = 1;
                        return;
                    }
                }

                // Otherwise divide weight evenly
                float sharedWeight = 1.0f / length;
                for (int i = 0; i < length; i++)
                    outWeights[i] = sharedWeight;
                return;
            }

            int indexA = -1;
            int indexB = -1;
            int indexCenter = -1;
            float maxDotForNegCross = -100000.0f;
            float maxDotForPosCross = -100000.0f;
            for (int i = 0; i < length; i++)
            {
                if (math.all(blendTree.Value.MotionPositions[i] == float2.zero))
                {
                    if (indexCenter >= 0)
                        return;
                    indexCenter = i;
                    continue;
                }
                var posNormalized = math.normalize(blendTree.Value.MotionPositions[i]);
                float dot = math.dot(posNormalized, blendParameter);
                float det = posNormalized.x * blendParameter.y - posNormalized.y * blendParameter.x;
                if (det > 0)
                {
                    if (dot > maxDotForPosCross)
                    {
                        maxDotForPosCross = dot;
                        indexA = i;
                    }
                }
                else
                {
                    if (dot > maxDotForNegCross)
                    {
                        maxDotForNegCross = dot;
                        indexB = i;
                    }
                }
            }

            float centerWeight = 0.0F;

            if (indexA < 0 || indexB < 0)
            {
                // Fallback if sampling point is not inside a triangle
                centerWeight = 1;
            }
            else
            {
                var a = blendTree.Value.MotionPositions[indexA];
                var b = blendTree.Value.MotionPositions[indexB];

                // Calculate weights using barycentric coordinates
                // (formulas from http://en.wikipedia.org/wiki/Barycentric_coordinate_system_%28mathematics%29 )
                float det = b.y * a.x - b.x * a.y;        // Simplified from: (b.y-0)*(a.x-0) + (0-b.x)*(a.y-0);
                float wA = (b.y * blendParameter.x - b.x * blendParameter.y) / det; // Simplified from: ((b.y-0)*(l.x-0) + (0-b.x)*(l.y-0)) / det;
                float wB = (a.x * blendParameter.y - a.y * blendParameter.x) / det; // Simplified from: ((0-a.y)*(l.x-0) + (a.x-0)*(l.y-0)) / det;
                centerWeight = 1 - wA - wB;

                // Clamp to be inside triangle
                if (centerWeight < 0)
                {
                    centerWeight = 0;
                    float sum = wA + wB;
                    wA /= sum;
                    wB /= sum;
                }
                else if (centerWeight > 1)
                {
                    centerWeight = 1;
                    wA = 0;
                    wB = 0;
                }

                // Give weight to the two vertices on the periphery that are closest
                outWeights[indexA] = wA;
                outWeights[indexB] = wB;
            }

            if (indexCenter >= 0)
            {
                outWeights[indexCenter] = centerWeight;
            }
            else
            {
                // Give weight to all children when input is in the center
                float sharedWeight = 1.0f / length;
                for (int i = 0; i < length; i++)
                    outWeights[i] += sharedWeight * centerWeight;
            }
        }

        static public float ComputeBlendTree2DSimpleDirectionalDuration(BlobAssetReference<BlendTree2DSimpleDirectional> blendTree, ref NativeArray<float> weights)
        {
            var length = blendTree.Value.MotionPositions.Length;

            var duration = 0.0f;
            for (int i = 0; i < length; i++)
            {
                duration += weights[i] * blendTree.Value.Motions[i].Value.Duration / blendTree.Value.MotionSpeeds[i];
            }

            return duration;
        }

        public struct ClipKeyframe
        {
            public int Left;
            public int Right;
            public float Weight;

            static public ClipKeyframe Create(ref Clip clip, float time)
            {
                return Create(clip.Duration, clip.SampleRate, clip.Bindings.CurveCount, time);
            }

            static public ClipKeyframe Create(ref ClipBuilder clipBuilder, float time)
            {
                // not interleaved
                return Create(clipBuilder.Duration, clipBuilder.SampleRate, 1, time);
            }

            static ClipKeyframe Create(float duration, float frameRate, int curveCount, float time)
            {
                var sampleIndex = math.clamp(time, 0, duration) * frameRate;
                return new ClipKeyframe
                {
                    Left = (int)math.floor(sampleIndex) * curveCount,
                    Right = (int)math.ceil(sampleIndex) * curveCount,
                    Weight = math.frac(sampleIndex)
                };
            }
        }

        static unsafe public void EvaluateClip(BlobAssetReference<ClipInstance> clipInstance, float time, ref AnimationStream stream, int additive)
        {
            stream.ValidateIsNotNull();
            stream.ValidateCompatibility(clipInstance);

            if (additive == 0)
            {
                stream.ResetToDefaultValues();
                stream.ClearMasks();
            }
            else
            {
                stream.ResetToZero();
            }

            ref var clip = ref clipInstance.Value.Clip;
            ref var bindings = ref clip.Bindings;
            var keyframe = ClipKeyframe.Create(ref clip, time);

            float* scratchPtr = stackalloc float[16];
            float* samplesPtr = (float*)clip.Samples.GetUnsafePtr();

            // Rotations
            {
                int i = 0;
                int count = bindings.RotationBindings.Length;
                int curveIndex = bindings.RotationSamplesOffset;
                for (; i + 3 < count; i += 4, curveIndex += BindingSet.RotationKeyFloatCount * 4)
                {
                    var leftKey  = (quaternion*)(samplesPtr + curveIndex + keyframe.Left);
                    var rightKey = (quaternion*)(samplesPtr + curveIndex + keyframe.Right);
                    var result   = (quaternion*)scratchPtr;

                    for (int j = 0; j < 4; ++j)
                    {
                        result[j] = mathex.lerp(leftKey[j], rightKey[j], keyframe.Weight);
                    }

                    stream.SetLocalToParentRotation(clipInstance.Value.RotationBindingMap[i + 0], result[0]);
                    stream.SetLocalToParentRotation(clipInstance.Value.RotationBindingMap[i + 1], result[1]);
                    stream.SetLocalToParentRotation(clipInstance.Value.RotationBindingMap[i + 2], result[2]);
                    stream.SetLocalToParentRotation(clipInstance.Value.RotationBindingMap[i + 3], result[3]);
                }

                for (; i < count; ++i, curveIndex += BindingSet.RotationKeyFloatCount)
                {
                    ref var leftKey = ref GetDataInSample<quaternion>(ref clip.Samples, curveIndex + keyframe.Left);
                    ref var rightKey = ref GetDataInSample<quaternion>(ref clip.Samples, curveIndex + keyframe.Right);
                    stream.SetLocalToParentRotation(clipInstance.Value.RotationBindingMap[i], mathex.lerp(leftKey, rightKey, keyframe.Weight));
                }
            }

            // Translations
            {
                int i = 0;
                int count = bindings.TranslationBindings.Length;
                int curveIndex = bindings.TranslationSamplesOffset;
                for (; i + 4 < count; i += 5, curveIndex += BindingSet.TranslationKeyFloatCount * 5)
                {
                    var leftKey  = (float4*)(samplesPtr + curveIndex + keyframe.Left);
                    var rightKey = (float4*)(samplesPtr + curveIndex + keyframe.Right);
                    var result   = (float4*)scratchPtr;

                    for (int j = 0; j < 4; ++j)
                    {
                        result[j] = math.lerp(leftKey[j], rightKey[j], keyframe.Weight);
                    }

                    var float3Data = (float3*)scratchPtr;
                    stream.SetLocalToParentTranslation(clipInstance.Value.TranslationBindingMap[i + 0], float3Data[0]);
                    stream.SetLocalToParentTranslation(clipInstance.Value.TranslationBindingMap[i + 1], float3Data[1]);
                    stream.SetLocalToParentTranslation(clipInstance.Value.TranslationBindingMap[i + 2], float3Data[2]);
                    stream.SetLocalToParentTranslation(clipInstance.Value.TranslationBindingMap[i + 3], float3Data[3]);
                    stream.SetLocalToParentTranslation(clipInstance.Value.TranslationBindingMap[i + 4], float3Data[4]);
                }

                for (; i < count; ++i, curveIndex += BindingSet.TranslationKeyFloatCount)
                {
                    ref var leftKey = ref GetDataInSample<float3>(ref clip.Samples, curveIndex + keyframe.Left);
                    ref var rightKey = ref GetDataInSample<float3>(ref clip.Samples, curveIndex + keyframe.Right);
                    stream.SetLocalToParentTranslation(clipInstance.Value.TranslationBindingMap[i], math.lerp(leftKey, rightKey, keyframe.Weight));
                }
            }

            // Scales
            {
                int i = 0;
                int count = bindings.ScaleBindings.Length;
                int curveIndex = bindings.ScaleSamplesOffset;
                for (; i + 4 < count; i += 5, curveIndex += BindingSet.ScaleKeyFloatCount * 5)
                {
                    var leftKey  = (float4*)(samplesPtr + curveIndex + keyframe.Left);
                    var rightKey = (float4*)(samplesPtr + curveIndex + keyframe.Right);
                    var result   = (float4*)scratchPtr;

                    for (int j = 0; j < 4; ++j)
                    {
                        result[j] = math.lerp(leftKey[j], rightKey[j], keyframe.Weight);
                    }

                    var float3Data = (float3*)scratchPtr;
                    stream.SetLocalToParentScale(clipInstance.Value.ScaleBindingMap[i + 0], float3Data[0]);
                    stream.SetLocalToParentScale(clipInstance.Value.ScaleBindingMap[i + 1], float3Data[1]);
                    stream.SetLocalToParentScale(clipInstance.Value.ScaleBindingMap[i + 2], float3Data[2]);
                    stream.SetLocalToParentScale(clipInstance.Value.ScaleBindingMap[i + 3], float3Data[3]);
                    stream.SetLocalToParentScale(clipInstance.Value.ScaleBindingMap[i + 4], float3Data[4]);
                }

                for (; i < count; ++i, curveIndex += BindingSet.ScaleKeyFloatCount)
                {
                    ref var leftKey = ref GetDataInSample<float3>(ref clip.Samples, curveIndex + keyframe.Left);
                    ref var rightKey = ref GetDataInSample<float3>(ref clip.Samples, curveIndex + keyframe.Right);
                    stream.SetLocalToParentScale(clipInstance.Value.ScaleBindingMap[i], math.lerp(leftKey, rightKey, keyframe.Weight));
                }
            }

            // Floats
            {
                int i = 0;
                int count = bindings.FloatBindings.Length;
                int curveIndex = bindings.FloatSamplesOffset;
                for (; i + 3 < count; i += 4, curveIndex += BindingSet.FloatKeyFloatCount * 4)
                {
                    var leftKey  = (float4*)(samplesPtr + curveIndex + keyframe.Left);
                    var rightKey = (float4*)(samplesPtr + curveIndex + keyframe.Right);
                    var result   = (float4*)scratchPtr;

                    *result = math.lerp(*leftKey, *rightKey, keyframe.Weight);

                    stream.SetFloat(clipInstance.Value.FloatBindingMap[i + 0], scratchPtr[0]);
                    stream.SetFloat(clipInstance.Value.FloatBindingMap[i + 1], scratchPtr[1]);
                    stream.SetFloat(clipInstance.Value.FloatBindingMap[i + 2], scratchPtr[2]);
                    stream.SetFloat(clipInstance.Value.FloatBindingMap[i + 3], scratchPtr[3]);
                }

                for (; i < count; ++i, curveIndex += BindingSet.FloatKeyFloatCount)
                {
                    var leftKey = clip.Samples[curveIndex + keyframe.Left];
                    var rightKey = clip.Samples[curveIndex + keyframe.Right];
                    stream.SetFloat(clipInstance.Value.FloatBindingMap[i], math.lerp(leftKey, rightKey, keyframe.Weight));
                }
            }

            // Ints
            {
                int i = 0;
                int count = bindings.IntBindings.Length;
                int curveIndex = bindings.IntSamplesOffset;

                // We always take the value of the key that is the closest to the current time.
                var keyOffset = math.select(keyframe.Left, keyframe.Right, keyframe.Weight > 0.5f);
                for (; i + 3 < count; i += 4, curveIndex += BindingSet.IntKeyFloatCount * 4)
                {
                    var key = *(float4*)(samplesPtr + curveIndex + keyOffset);

                    stream.SetInt(clipInstance.Value.IntBindingMap[i + 0], (int)key.x);
                    stream.SetInt(clipInstance.Value.IntBindingMap[i + 1], (int)key.y);
                    stream.SetInt(clipInstance.Value.IntBindingMap[i + 2], (int)key.z);
                    stream.SetInt(clipInstance.Value.IntBindingMap[i + 3], (int)key.w);
                }

                for (; i < count; ++i, curveIndex += BindingSet.IntKeyFloatCount)
                {
                    var key = clip.Samples[curveIndex + keyOffset];
                    stream.SetInt(clipInstance.Value.IntBindingMap[i], (int)key);
                }
            }
        }

        internal static float AdjustLastFrameValue(float beforeLastValue, float atDurationValue, float lastFrameError)
        {
            return lastFrameError < 1.0f ? math.lerp(beforeLastValue, atDurationValue, 1.0f / (1.0f - lastFrameError)) : atDurationValue;
        }

        internal static float3 AdjustLastFrameValue(float3 beforeLastValue, float3 atDurationValue, float lastFrameError)
        {
            return lastFrameError < 1.0f ? math.lerp(beforeLastValue, atDurationValue, 1.0f / (1.0f - lastFrameError)) : atDurationValue;
        }

        internal static float4 AdjustLastFrameValue(float4 beforeLastValue, float4 atDurationValue, float lastFrameError)
        {
            return lastFrameError < 1.0f ? math.lerp(beforeLastValue, atDurationValue, 1.0f / (1.0f - lastFrameError)) : atDurationValue;
        }

        /// <summary>
        /// Struct representing the state of a mixer. A new instance can be created by calling <see cref="Core.MixerBegin"/>,
        /// and the instance can be used in <see cref="Core.MixerAdd"/> and <see cref="Core.MixerEnd"/>.
        /// </summary>
        public struct MixerState
        {
            public float WeightSum;
            public float WeightMax;
        }

        /// <summary>
        /// Starts a new mixer that writes to the output animation stream. Usually, after calling this method, <see cref="MixerAdd"/>
        /// will be called multiple times, and then <see cref="MixerEnd"/> will be called once to finalize the mixing.
        /// </summary>
        /// <param name="output">Animation stream that will be the destination for the mixer. Will be modified.</param>
        /// <returns>The state of the mixer, to be used in <see cref="MixerAdd"/> and <see cref="MixerEnd"/></returns>
        static public MixerState MixerBegin(ref AnimationStream output)
        {
            output.ValidateIsNotNull();

            output.ResetToZero();

            return new MixerState
            {
                WeightSum = 0,
                WeightMax = 0,
            };
        }

        /// <summary>
        /// Ends a mixing on an animation stream by taking care of any leftover weights and normalizing quaternions. Usually,
        /// <see cref="MixerBegin"/> needs to be called to start a mix, and <see cref="MixerAdd"/> is then called multiple times.
        /// Then, in order to end a mix, <see cref="MixerEnd"/> is called.
        /// </summary>
        /// <param name="output">The output animation stream. Will be modified.</param>
        /// <param name="defaultPoseInput">The default pose of the character/rig. This will get mixed using the remaining weight. Will not be modified.</param>
        /// <param name="state">The state of the mixer (created by <see cref="MixerBegin"/>, and updated by <see cref="MixerAdd"/>.</param>
        static unsafe public void MixerEnd(
            ref AnimationStream output,
            ref AnimationStream defaultPoseInput,
            MixerState state
        )
        {
            output.ValidateIsNotNull();

            if (state.WeightSum < 1.0F)
            {
                if (defaultPoseInput.IsNull)
                {
                    var defaultPoseStream = AnimationStream.FromDefaultValues(output.Rig);
                    MixerAdd(ref output, ref defaultPoseStream, 1.0F - state.WeightSum, state);
                }
                else
                    MixerAdd(ref output, ref defaultPoseInput, 1.0F - state.WeightSum, state);
            }

            // Normalize 4-wide rotations
            quaternion4* outputRot = output.GetRotationDataChunkUnsafePtr();
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
            {
                outputRot[i] = mathex.normalizesafe(outputRot[i]);
            }
        }

        /// <summary>
        /// Adds a pose to the mixer. The mixer needs to be started with <see cref="MixerBegin"/>, then <see cref="MixerAdd"/> can be
        /// called multiple times. To end the mixer, <see cref="MixerEnd"/> needs to be called. This method will return
        /// an updated mixer state.
        /// <br/>
        /// Each continuous channel is multiplied by the weight before being added to the output stream. Quaternions
        /// follow the same behaviour.
        /// The discrete channels with the greatest weight will be used. This includes the default pose, which has an
        /// implicit weight of <c>1 - WeightSum</c> (so if the weights are <c>0.1, 0.2, 0.3</c>, the default pose will
        /// be used, since it would have a weight of <c>1.0 - 0.6 = 0.4</c>). If all weights are equal, the first one is
        /// used.
        /// </summary>
        /// <param name="output">The animation stream the mixer will write to. Will be modified.</param>
        /// <param name="add">The pose that will be added to output. Will not be modified.</param>
        /// <param name="weight">The weight of the pose</param>
        /// <param name="state">The state of the mixer (created by <see cref="MixerBegin"/>, and updated by <see cref="MixerAdd"/>.</param>
        /// <returns>The new state of the mixer</returns>
        static unsafe public MixerState MixerAdd(
            ref AnimationStream output,
            ref AnimationStream add,
            float weight,
            MixerState state
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(ref add);

            // Add 4-wide non rotational data
            float4* addData    = add.GetInterpolatedDataChunkUnsafePtr();
            float4* outputData = output.GetInterpolatedDataChunkUnsafePtr();
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i)
            {
                outputData[i] = math.mad(addData[i], weight, outputData[i]);
            }

            if (weight > state.WeightMax)
            {
                state.WeightMax = weight;
                int4* intData    = add.GetDiscreteDataChunkUnsafePtr();
                int4* outputIntData = output.GetDiscreteDataChunkUnsafePtr();
                // We are checking for pointer equality in case the user gives the same animation stream as add and output,
                // and if the memory overlaps, memcopy does not work :(
                if (intData != outputIntData)
                    UnsafeUtility.MemCpy(outputIntData, intData, output.DiscreteDataChunkCount * sizeof(int4));
                else
                    UnsafeUtility.MemMove(outputIntData, intData, output.DiscreteDataChunkCount * sizeof(int4));
            }

            // Add 4-wide rotations
            quaternion4* addRot    = add.GetRotationDataChunkUnsafePtr();
            quaternion4* outputRot = output.GetRotationDataChunkUnsafePtr();
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
            {
                outputRot[i] = mathex.add(outputRot[i], addRot[i] * weight);
            }

            output.OrMasks(ref add);

            state.WeightSum += weight;

            return state;
        }

        static unsafe public void AddPose(
            ref AnimationStream output,
            ref AnimationStream inputA,
            ref AnimationStream inputB
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(ref inputA);
            output.ValidateRigEquality(ref inputB);

            float4* inputAData = inputA.GetInterpolatedDataChunkUnsafePtr();
            float4* inputBData = inputB.GetInterpolatedDataChunkUnsafePtr();
            float4* outputData = output.GetInterpolatedDataChunkUnsafePtr();
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i)
            {
                outputData[i] = inputAData[i] + inputBData[i];
            }

            int4* inputIntAData = inputA.GetDiscreteDataChunkUnsafePtr();
            int4* inputIntBData = inputB.GetDiscreteDataChunkUnsafePtr();
            int4* outputIntData = output.GetDiscreteDataChunkUnsafePtr();
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i)
            {
                outputIntData[i] = inputIntAData[i] + inputIntBData[i];
            }

            // 4-wide add
            quaternion4* inputARot = inputA.GetRotationDataChunkUnsafePtr();
            quaternion4* inputBRot = inputB.GetRotationDataChunkUnsafePtr();
            quaternion4* outputRot = output.GetRotationDataChunkUnsafePtr();
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
            {
                outputRot[i] = mathex.mul(inputARot[i], inputBRot[i]);
            }

            output.OrMasks(ref inputA, ref inputB);
        }

        static unsafe public void InversePose(
            ref AnimationStream output,
            ref AnimationStream input
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(ref input);

            // 4-wide inverse
            float4* inputData  = input.GetInterpolatedDataChunkUnsafePtr();
            float4* outputData = output.GetInterpolatedDataChunkUnsafePtr();
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i)
            {
                outputData[i] = inputData[i] * -1f;
            }

            int4* inputIntData = input.GetDiscreteDataChunkUnsafePtr();
            int4* outputIntData = output.GetDiscreteDataChunkUnsafePtr();
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i)
            {
                outputIntData[i] = -inputIntData[i];
            }

            // Inverse 4-wide rotations
            quaternion4* inputRot  = input.GetRotationDataChunkUnsafePtr();
            quaternion4* outputRot = output.GetRotationDataChunkUnsafePtr();
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
            {
                outputRot[i] = mathex.conjugate(inputRot[i]);
            }

            output.OrMasks(ref input);
        }

        static unsafe public void WeightPose(
            ref AnimationStream output,
            ref AnimationStream input,
            NativeArray<WeightData> weights
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(ref input);
            output.ValidateCompatibility(weights);

            int wIdx = 0;
            float4* weight = (float4*)weights.GetUnsafeReadOnlyPtr();

            // Blend 4-wide lerp non rotation data
            float4* inputData  = input.GetInterpolatedDataChunkUnsafePtr();
            float4* outputData = output.GetInterpolatedDataChunkUnsafePtr();
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputData[i] = inputData[i] * weight[wIdx];
            }

            int4* inputIntData = input.GetDiscreteDataChunkUnsafePtr();
            int4* outputIntData = output.GetDiscreteDataChunkUnsafePtr();
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputIntData[i] = math.select(0, inputIntData[i], weight[wIdx] > 0.5f);
            }

            // Blend 4-wide rotations
            quaternion4* inputRot  = input.GetRotationDataChunkUnsafePtr();
            quaternion4* outputRot = output.GetRotationDataChunkUnsafePtr();
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputRot[i] = mathex.quatWeight(inputRot[i], weight[wIdx]);
            }

            output.OrMasks(ref input);
        }

        /// <summary>
        /// Compute matrices in LocalToRoot space with the specified offset. For example, if you want to compute
        /// the LocalToWorld matrices of the rig, pass in the LocalToWorld matrix of the root as the offset parameter.
        /// </summary>
        /// <param name="stream">Animation stream</param>
        /// <param name="offset">An extra offset which applies to all matrices</param>
        /// <param name="outMatrices">Resulting matrix computation. This array must be preallocated with the same bone count specified by the RigDefinition.</param>
        static public void ComputeLocalToRoot(
            ref AnimationStream stream,
            float4x4 offset,
            NativeArray<float4x4> outMatrices
        )
        {
            if (stream.IsNull)
                return;

            int count = stream.Rig.Value.Skeleton.BoneCount;

            ValidateBufferLengthsAreEqual(outMatrices.Length, count);

            outMatrices[0] = math.mul(offset, mathex.float4x4(stream.GetLocalToParentMatrix(0)));
            for (int i = 1; i != count; ++i)
            {
                var pIdx = stream.Rig.Value.Skeleton.ParentIndexes[i];
                outMatrices[i] = math.mul(outMatrices[pIdx], mathex.float4x4(stream.GetLocalToParentMatrix(i)));
            }
        }

        /// <summary>
        /// Compute two sets of matrices in LocalToRoot space with specified offsets. For example, if you want to compute
        /// the LocalToWorld matrices of the rig, pass in the LocalToWorld matrix of the root as the offset parameter. This method
        /// is used to conveniently compute the LocalToRoot and LocalToWorld matrices of an AnimationStream in one pass.
        /// </summary>
        /// <param name="stream">Animation stream</param>
        /// <param name="offset1">An extra offset which applies to all matrices in outMatrices1</param>
        /// <param name="outMatrices1">Resulting matrix computation using offset1. This array must be preallocated with the same bone count specified by the RigDefinition.</param>
        /// <param name="offset2">An extra offset which applies to all matrices in outMatrices2</param>
        /// <param name="outMatrices2">Resulting matrix computation using offset2. This array must be preallocated with the same bone count specified by the RigDefinition.</param>
        static public void ComputeLocalToRoot(
            ref AnimationStream stream,
            float4x4 offset1,
            NativeArray<float4x4> outMatrices1,
            float4x4 offset2,
            NativeArray<float4x4> outMatrices2
        )
        {
            int count = stream.Rig.Value.Skeleton.BoneCount;

            ValidateBufferLengthsAreEqual(outMatrices1.Length, count);
            ValidateBufferLengthsAreEqual(outMatrices2.Length, count);


            var mat = mathex.float4x4(stream.GetLocalToParentMatrix(0));
            outMatrices1[0] = math.mul(offset1, mat);
            outMatrices2[0] = math.mul(offset2, mat);

            for (int i = 1; i != count; ++i)
            {
                var pIdx = stream.Rig.Value.Skeleton.ParentIndexes[i];
                mat = mathex.float4x4(stream.GetLocalToParentMatrix(i));

                outMatrices1[i] = math.mul(outMatrices1[pIdx], mat);
                outMatrices2[i] = math.mul(outMatrices2[pIdx], mat);
            }
        }

        static public void ComputeLocalToParentLinearVelocities(
            ref AnimationStream input,
            ref AnimationStream previousInput,
            float deltaTime,
            NativeArray<float3> outVelocities
        )
        {
            input.ValidateIsNotNull();
            input.ValidateRigEquality(ref previousInput);

            int translationCount = input.Rig.Value.Skeleton.BoneCount;

            ValidateBufferLengthsAreEqual(translationCount, outVelocities.Length);

            float inverseDeltaTime = math.rcp(deltaTime);

            ValidateIsFinite(inverseDeltaTime);

            for (int j = 0; j < translationCount; ++j)
            {
                float3 from = previousInput.GetLocalToParentTranslation(j);
                float3 to = input.GetLocalToParentTranslation(j);

                float3 deltaTrans = to - from;
                outVelocities[j] = deltaTrans * inverseDeltaTime;
            }
        }

        static public void ComputeLocalToParentAngularVelocities(
            ref AnimationStream input,
            ref AnimationStream previousInput,
            float deltaTime,
            NativeArray<float3> outAngularVelocities
        )
        {
            input.ValidateIsNotNull();
            input.ValidateRigEquality(ref previousInput);

            int rotationCount = input.Rig.Value.Skeleton.BoneCount;

            ValidateBufferLengthsAreEqual(rotationCount, outAngularVelocities.Length);

            float inverseDeltaTime = math.rcp(deltaTime);

            ValidateIsFinite(inverseDeltaTime);

            for (int j = 0; j < rotationCount; ++j)
            {
                quaternion from = previousInput.GetLocalToParentRotation(j);
                quaternion to = input.GetLocalToParentRotation(j);

                if (!to.Equals(from))
                    outAngularVelocities[j] = mathex.toEuler(math.mul(math.inverse(from), to)) * inverseDeltaTime;
                else
                    outAngularVelocities[j] = float3.zero;
            }
        }

        static public void ComputeLocalToWorldLinearVelocities(
            float4x4 localToWorld,
            float4x4 previousLocalToWorld,
            ref AnimationStream input,
            ref AnimationStream previousInput,
            float deltaTime,
            NativeArray<float3> outVelocities
        )
        {
            input.ValidateIsNotNull();
            input.ValidateRigEquality(ref previousInput);

            int translationCount = input.Rig.Value.Skeleton.BoneCount;

            ValidateBufferLengthsAreEqual(translationCount, outVelocities.Length);

            float inverseDeltaTime = math.rcp(deltaTime);
            ValidateIsFinite(inverseDeltaTime);
            for (int j = 0; j < translationCount; ++j)
            {
                float3 from = math.transform(previousLocalToWorld, previousInput.GetLocalToRootTranslation(j));
                float3 to = math.transform(localToWorld, input.GetLocalToRootTranslation(j));

                float3 deltaTrans = to - from;
                outVelocities[j] = deltaTrans * inverseDeltaTime;
            }
        }

        static public void ComputeLocalToWorldAngularVelocities(
            float4x4 localToWorld,
            float4x4 previousLocalToWorld,
            ref AnimationStream input,
            ref AnimationStream previousInput,
            float deltaTime,
            NativeArray<float3> outAngularVelocities
        )
        {
            input.ValidateIsNotNull();
            input.ValidateRigEquality(ref previousInput);

            int rotationCount = input.Rig.Value.Skeleton.BoneCount;

            ValidateBufferLengthsAreEqual(rotationCount, outAngularVelocities.Length);

            float inverseDeltaTime = math.rcp(deltaTime);
            ValidateIsFinite(inverseDeltaTime);

            var previousLocalToWorldRot = new quaternion(previousLocalToWorld);
            var localToWorldRot = new quaternion(localToWorld);

            for (int j = 0; j < rotationCount; ++j)
            {
                quaternion from = math.mul(previousLocalToWorldRot, previousInput.GetLocalToRootRotation(j));
                quaternion to = math.mul(localToWorldRot, input.GetLocalToRootRotation(j));

                outAngularVelocities[j] = mathex.toEuler(math.mul(math.inverse(from), to)) * inverseDeltaTime;
            }
        }
    }
}
