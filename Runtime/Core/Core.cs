using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine.Assertions;

namespace Unity.Animation
{
    static public partial class Core
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe static internal ref T GetDataInSample<T>(ref BlobArray<float> samples, int offset)
            where T : unmanaged
        {
            return ref *(T*)((float*)samples.GetUnsafePtr() + offset);
        }

        /// Find a binding in a binding array.
        public static int FindBindingIndex(ref BlobArray<StringHash> bindings, StringHash searchedBinding)
        {
            var i = 0;
            while (i < bindings.Length && !bindings[i].Equals(searchedBinding))
                ++i;
            return i < bindings.Length ? i : -1;
        }

        static public void Blend<TOutputDescriptor, TInputDescriptor1, TInputDescriptor2>(
            ref AnimationStream<TOutputDescriptor> output,
            ref AnimationStream<TInputDescriptor1> input1,
            ref AnimationStream<TInputDescriptor2> input2,
            float weight
            )
            where TOutputDescriptor : struct, IAnimationStreamDescriptor
            where TInputDescriptor1 : struct, IAnimationStreamDescriptor
            where TInputDescriptor2 : struct, IAnimationStreamDescriptor
        {
            Assert.IsFalse(output.IsNull);
            Assert.IsTrue(output.Rig == input1.Rig);
            Assert.IsTrue(output.Rig == input2.Rig);

            ref var bindings = ref output.Rig.Value.Bindings;

            for (int i = 0, count = bindings.TranslationBindings.Length; i < count; ++i)
            {
                var lhs = input1.GetLocalToParentTranslation(i);
                var rhs = input2.GetLocalToParentTranslation(i);
                output.SetLocalToParentTranslation(i, math.lerp(lhs, rhs, weight));
            }

            for (int i = 0, count = bindings.RotationBindings.Length; i < count; ++i)
            {
                var lhs = input1.GetLocalToParentRotation(i);
                var rhs = input2.GetLocalToParentRotation(i);
                output.SetLocalToParentRotation(i, math.slerp(lhs, rhs, weight));
            }

            for (int i = 0, count = bindings.ScaleBindings.Length; i < count; ++i)
            {
                var lhs = input1.GetLocalToParentScale(i);
                var rhs = input2.GetLocalToParentScale(i);
                output.SetLocalToParentScale(i, math.lerp(lhs, rhs, weight));
            }

            for (int i = 0, count = bindings.FloatBindings.Length; i < count; ++i)
            {
                output.SetFloat(i, math.lerp(input1.GetFloat(i), input2.GetFloat(i), weight));
            }

            for (int i = 0, count = bindings.IntBindings.Length; i < count; ++i)
            {
                output.SetInt(i, (int)math.lerp(input1.GetInt(i), input2.GetInt(i), weight));
            }
        }

        static public void BlendOverrideLayer<TOutputDescriptor, TInputDescriptor>(
            ref AnimationStream<TOutputDescriptor> output,
            ref AnimationStream<TInputDescriptor> input,
            float weight,
            NativeBitSet mask
            )
            where TOutputDescriptor : struct, IAnimationStreamDescriptor
            where TInputDescriptor : struct, IAnimationStreamDescriptor
        {
            Assert.IsFalse(output.IsNull);
            Assert.IsTrue(output.Rig == input.Rig);

            var channelIndex = 0;
            ref var rig = ref output.Rig.Value;

            for (int i = 0, count = rig.Bindings.TranslationBindings.Length; i < count; i++, channelIndex++)
            {
                if (mask.Test(channelIndex))
                {
                    var value = math.lerp(output.GetLocalToParentTranslation(i), input.GetLocalToParentTranslation(i), weight);
                    output.SetLocalToParentTranslation(i, value);
                }
            }

            for (int i = 0, count = rig.Bindings.RotationBindings.Length; i < count; i++, channelIndex++)
            {
                if (mask.Test(channelIndex))
                {
                    var value = math.slerp(output.GetLocalToParentRotation(i), input.GetLocalToParentRotation(i), weight);
                    output.SetLocalToParentRotation(i, value);
                }
            }

            for (int i = 0, count = rig.Bindings.ScaleBindings.Length; i < count; i++, channelIndex++)
            {
                if (mask.Test(channelIndex))
                {
                    var value = math.lerp(output.GetLocalToParentScale(i), input.GetLocalToParentScale(i), weight);
                    output.SetLocalToParentScale(i, value);
                }
            }

            for (int i = 0, count = rig.Bindings.FloatBindings.Length; i < count; i++, channelIndex++)
            {
                if (mask.Test(channelIndex))
                {
                    var value = math.lerp(output.GetFloat(i), input.GetFloat(i), weight);
                    output.SetFloat(i, value);
                }
            }

            for (int i = 0, count = rig.Bindings.IntBindings.Length; i < count; i++, channelIndex++)
            {
                if (mask.Test(channelIndex))
                {
                    // TODO: should we cast to int after each mixing or when the whole mixing pipe is finish?
                    var value = math.lerp(output.GetInt(i), input.GetInt(i), weight);
                    output.SetInt(i, (int)value);
                }
            }
        }

        static public void BlendAdditiveLayer<TOutputDescriptor, TInputDescriptor>(
            ref AnimationStream<TOutputDescriptor> output,
            ref AnimationStream<TInputDescriptor> input,
            float weight,
            NativeBitSet mask
            )
            where TOutputDescriptor : struct, IAnimationStreamDescriptor
            where TInputDescriptor : struct, IAnimationStreamDescriptor
        {
            Assert.IsFalse(output.IsNull);
            Assert.IsTrue(output.Rig == input.Rig);

            var channelIndex = 0;
            ref var rig = ref output.Rig.Value;

            for (int i = 0, count = rig.Bindings.TranslationBindings.Length; i < count; i++, channelIndex++)
            {
                if (mask.Test(channelIndex))
                {
                    var baseValue = output.GetLocalToParentTranslation(i);
                    var layerValue = input.GetLocalToParentTranslation(i) * weight;
                    output.SetLocalToParentTranslation(i, baseValue + layerValue);
                }
            }

            for (int i = 0, count = rig.Bindings.RotationBindings.Length; i < count; i++, channelIndex++)
            {
                if (mask.Test(channelIndex))
                {
                    var baseValue = output.GetLocalToParentRotation(i);
                    var layerValue = mathex.quatWeight(input.GetLocalToParentRotation(i), weight);
                    output.SetLocalToParentRotation(i, math.mul(baseValue, layerValue));
                }
            }

            for (int i = 0, count = rig.Bindings.ScaleBindings.Length; i < count; i++, channelIndex++)
            {
                if (mask.Test(channelIndex))
                {
                    var baseValue = output.GetLocalToParentScale(i);
                    var layerValue = input.GetLocalToParentScale(i) * weight;
                    output.SetLocalToParentScale(i, baseValue + layerValue);
                }
            }

            for (int i = 0, count = rig.Bindings.FloatBindings.Length; i < count; i++, channelIndex++)
            {
                if (mask.Test(channelIndex))
                {
                    output.SetFloat(i, output.GetFloat(i) + input.GetFloat(i) * weight);
                }
            }

            for (int i = 0, count = rig.Bindings.IntBindings.Length; i < count; i++, channelIndex++)
            {
                if (mask.Test(channelIndex))
                {
                    output.SetInt(i, (int)(output.GetInt(i) + input.GetInt(i) * weight));
                }
            }
        }

        static public void RigRemapper<TDestinationDescriptor, TSourceDescriptor>(
            BlobAssetReference<RigRemapTable> remapTable,
            ref AnimationStream<TDestinationDescriptor> destinationStream,
            ref AnimationStream<TSourceDescriptor> sourceStream
            )
            where TDestinationDescriptor : struct, IAnimationStreamDescriptor
            where TSourceDescriptor : struct, IAnimationStreamDescriptor
        {
            ref var remap = ref remapTable.Value;

            for (int i=0;i<remap.TranslationMappings.Length;i++)
            {
                ref var mapping = ref remap.TranslationMappings[i];
                var value = sourceStream.GetLocalToParentTranslation(mapping.SourceIndex);

                var offsetIndex = remapTable.Value.TranslationMappings[i].OffsetIndex;

                if (offsetIndex > 0)
                {
                    var translationOffset = remapTable.Value.TranslationOffsets[offsetIndex];
                    value = math.mul(translationOffset.Rotation, value * translationOffset.Scale);
                }

                destinationStream.SetLocalToParentTranslation(mapping.DestinationIndex, value);
            }

            for (int i=0;i<remap.RotationMappings.Length;i++)
            {
                ref var mapping = ref remap.RotationMappings[i];
                var value = sourceStream.GetLocalToParentRotation(mapping.SourceIndex);

                var offsetIndex = remapTable.Value.RotationMappings[i].OffsetIndex;

                if (offsetIndex > 0)
                {
                    var rotationOffset = remapTable.Value.RotationOffsets[offsetIndex];
                    value = math.mul(math.mul(rotationOffset.PreRotation, value), rotationOffset.PostRotation);
                }

                destinationStream.SetLocalToParentRotation(mapping.DestinationIndex, value);
            }

            for (int i=0;i<remap.ScaleMappings.Length;i++)
            {
                ref var mapping = ref remap.ScaleMappings[i];
                var value = sourceStream.GetLocalToParentScale(mapping.SourceIndex);
                destinationStream.SetLocalToParentScale(mapping.DestinationIndex, value);
            }

            for (int i=0;i<remap.FloatMappings.Length;i++)
            {
                ref var mapping = ref remap.FloatMappings[i];
                var value = sourceStream.GetFloat(mapping.SourceIndex);
                destinationStream.SetFloat(mapping.DestinationIndex, value);
            }

            for (int i=0;i<remap.IntMappings.Length;i++)
            {
                ref var mapping = ref remap.IntMappings[i];
                var value = sourceStream.GetInt(mapping.SourceIndex);
                destinationStream.SetInt(mapping.DestinationIndex, value);
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
            for(int i=0;i<length;i++)
            {
                outWeights[i] = WeightForIndex(ref blendTree.Value.MotionThresholds, length, i, blend);
            }
        }

        static public unsafe void ComputeBlendTree2DSimpleDirectionnalWeights(BlobAssetReference<BlendTree2DSimpleDirectionnal> blendTree, float2 blendParameter, ref NativeArray<float> outWeights)
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

        public struct ClipKeyframe
        {
            public int Left;
            public int Right;
            public float Weight;

            static public ClipKeyframe Create(ref Clip clip, float time)
            {
                var curveCount = clip.Bindings.CurveCount;
                var sampleIndex = math.clamp(time * clip.SampleRate, 0, clip.FrameCount);

                return new ClipKeyframe {
                    Left = (int)(math.floor(sampleIndex) * curveCount),
                    Right = (int)(math.ceil(sampleIndex) * curveCount),
                    Weight = sampleIndex - math.floor(sampleIndex)
                };
            }
        }

        static public void EvaluateClip<T>(ref ClipInstance clipInstance, float time, ref AnimationStream<T> stream, int additive)
            where T : struct, IAnimationStreamDescriptor
        {
            Assert.IsTrue(clipInstance.RigDefinition == stream.Rig);

            if(additive != 0)
                AnimationStreamUtils.MemClear(ref stream);
            else
                AnimationStreamUtils.SetDefaultValues(ref stream);

            ref var clip = ref clipInstance.Clip;
            ref var bindings = ref clip.Bindings;
            var keyframe = ClipKeyframe.Create(ref clip, time);

            for (int i = 0, count = bindings.TranslationBindings.Length, curveIndex = bindings.TranslationSamplesOffset; i < count; ++i, curveIndex += 3)
            {
                var index = clipInstance.TranslationBindingMap[i];
                Assert.IsTrue(index != -1);

                ref var leftKey = ref GetDataInSample<float3>(ref clip.Samples, curveIndex + keyframe.Left);
                ref var rightKey = ref GetDataInSample<float3>(ref clip.Samples, curveIndex + keyframe.Right);
                stream.SetLocalToParentTranslation(index, math.lerp(leftKey, rightKey, keyframe.Weight));
            }

            for (int i = 0, count = bindings.RotationBindings.Length, curveIndex = bindings.RotationSamplesOffset; i < count; ++i, curveIndex += 4)
            {
                var index = clipInstance.RotationBindingMap[i];
                Assert.IsTrue(index != -1);

                ref var leftKey = ref GetDataInSample<quaternion>(ref clip.Samples, curveIndex + keyframe.Left);
                ref var rightKey = ref GetDataInSample<quaternion>(ref clip.Samples, curveIndex + keyframe.Right);
                stream.SetLocalToParentRotation(index, mathex.lerp(leftKey, rightKey, keyframe.Weight));
            }

            for (int i = 0, count = bindings.ScaleBindings.Length, curveIndex = bindings.ScaleSamplesOffset; i < count; ++i, curveIndex += 3)
            {
                var index = clipInstance.ScaleBindingMap[i];
                Assert.IsTrue(index != -1);

                ref var leftKey = ref GetDataInSample<float3>(ref clip.Samples, curveIndex + keyframe.Left);
                ref var rightKey = ref GetDataInSample<float3>(ref clip.Samples, curveIndex + keyframe.Right);
                stream.SetLocalToParentScale(index, math.lerp(leftKey, rightKey, keyframe.Weight));
            }

            for (int i = 0, count = bindings.FloatBindings.Length, curveIndex = bindings.FloatSamplesOffset; i < count; ++i, ++curveIndex)
            {
                var index = clipInstance.FloatBindingMap[i];
                Assert.IsTrue(index != -1);

                var leftKey = clip.Samples[curveIndex + keyframe.Left];
                var rightKey = clip.Samples[curveIndex + keyframe.Right];
                stream.SetFloat(index, math.lerp(leftKey, rightKey, keyframe.Weight));
            }

            // TODO: Find the algorithm we want. Right now take the left most key.
            for (int i = 0, count = bindings.IntBindings.Length, curveIndex = bindings.IntSamplesOffset; i < count; ++i, ++curveIndex)
            {
                var index = clipInstance.IntBindingMap[i];
                Assert.IsTrue(index != -1);

                var leftKey = clip.Samples[curveIndex + keyframe.Left];
                //var rightKey = clip.Samples[curveIndex + keyframe.Right];

                stream.SetInt(index, (int)leftKey);
            }
        }


        static public void MixerBegin<TOutputDescriptor>(
            ref AnimationStream<TOutputDescriptor> output
            )
            where TOutputDescriptor : struct, IAnimationStreamDescriptor
        {
            AnimationStreamUtils.MemClear(ref output);
        }

        static public void MixerEnd<TOutputDescriptor, TInputDescriptor, TDefaultPoseInputDescriptor>(
            ref AnimationStream<TOutputDescriptor> output,
            ref AnimationStream<TInputDescriptor> input,
            ref AnimationStream<TDefaultPoseInputDescriptor> defaultPoseInput,
            float sumWeight
            )
            where TOutputDescriptor : struct, IAnimationStreamDescriptor
            where TInputDescriptor : struct, IAnimationStreamDescriptor
            where TDefaultPoseInputDescriptor : struct, IAnimationStreamDescriptor
        {
            ref var rig = ref output.Rig.Value;

            if(sumWeight < 1.0F)
            {
                if(defaultPoseInput.IsNull)
                {
                    var defaultPoseStream = AnimationStreamProvider.CreateReadOnly(output.Rig,
                        ref rig.DefaultValues.LocalTranslations,
                        ref rig.DefaultValues.LocalRotations,
                        ref rig.DefaultValues.LocalScales,
                        ref rig.DefaultValues.Floats,
                        ref rig.DefaultValues.Integers);
                    MixerAdd(ref output, ref input, ref defaultPoseStream, 1.0F - sumWeight, 0);
                }
                else
                    MixerAdd(ref output, ref input, ref defaultPoseInput, 1.0F - sumWeight, 0);
            }
            else
                AnimationStreamUtils.MemCpy(ref output, ref input);

            for (int i = 0, count = rig.Bindings.RotationBindings.Length; i < count; ++i)
            {
                // need to use a normalizesafe to avoid nan when length == 0
                var value = output.GetLocalToParentRotation(i);
                output.SetLocalToParentRotation(i, math.normalizesafe(value));
            }
        }

        static public float MixerAdd<TOutputDescriptor, TInputDescriptor, TAddDescriptor>(
            ref AnimationStream<TOutputDescriptor> output,
            ref AnimationStream<TInputDescriptor> input,
            ref AnimationStream<TAddDescriptor> add,
            float weight,
            float sumWeight
            )
            where TOutputDescriptor : struct, IAnimationStreamDescriptor
            where TInputDescriptor : struct, IAnimationStreamDescriptor
            where TAddDescriptor : struct, IAnimationStreamDescriptor
        {
            Assert.IsFalse(output.IsNull);
            Assert.IsFalse(input.IsNull);
            Assert.IsTrue(output.Rig == input.Rig);

            if(weight > 0.0f && !add.IsNull)
            {
                ref var rig = ref output.Rig.Value;

                for (int i = 0, count = rig.Bindings.TranslationBindings.Length; i < count; i++)
                {
                    var baseValue = input.GetLocalToParentTranslation(i);
                    var addValue = add.GetLocalToParentTranslation(i) * weight;
                    output.SetLocalToParentTranslation(i, baseValue + addValue);
                }

                for (int i = 0, count = rig.Bindings.RotationBindings.Length; i < count; i++)
                {
                    var baseValue = input.GetLocalToParentRotation(i);
                    var addValue = new quaternion(add.GetLocalToParentRotation(i).value * weight);
                    output.SetLocalToParentRotation(i, mathex.add(baseValue, addValue));
                }

                for (int i = 0, count = rig.Bindings.ScaleBindings.Length; i < count; i++)
                {
                    var baseValue = input.GetLocalToParentScale(i);
                    var addValue = add.GetLocalToParentScale(i) * weight;
                    output.SetLocalToParentScale(i, baseValue + addValue);
                }

                for (int i = 0, count = rig.Bindings.FloatBindings.Length; i < count; i++)
                {
                    output.SetFloat(i, input.GetFloat(i) + add.GetFloat(i) * weight);
                }

                for (int i = 0, count = rig.Bindings.IntBindings.Length; i < count; i++)
                {
                    output.SetInt(i, (int)(input.GetInt(i) + add.GetInt(i) * weight));
                }

                sumWeight += weight;
            }
            else
                AnimationStreamUtils.MemCpy(ref output, ref input);

            return sumWeight;
        }

        static public void AddPose<TOutputDescriptor, TInputADescriptor, TInputBDescriptor>(
            ref AnimationStream<TOutputDescriptor> output,
            ref AnimationStream<TInputADescriptor> inputA,
            ref AnimationStream<TInputBDescriptor> inputB
            )
            where TOutputDescriptor : struct, IAnimationStreamDescriptor
            where TInputADescriptor : struct, IAnimationStreamDescriptor
            where TInputBDescriptor : struct, IAnimationStreamDescriptor
        {
            Assert.IsFalse(output.IsNull);
            Assert.IsFalse(inputA.IsNull);
            Assert.IsFalse(inputB.IsNull);
            Assert.IsTrue(output.Rig == inputA.Rig);
            Assert.IsTrue(output.Rig == inputB.Rig);

            var tCount = output.m_StreamDescriptor.TranslationCount;

            for (int tIter = 0; tIter < tCount; tIter++)
            {
                output.SetLocalToParentTranslation(tIter, inputA.GetLocalToParentTranslation(tIter) + inputB.GetLocalToParentTranslation(tIter));
            }

            var rCount = output.m_StreamDescriptor.RotationCount;

            for (int rIter = 0; rIter < rCount; rIter++)
            {
                output.SetLocalToParentRotation(rIter, math.mul(inputA.GetLocalToParentRotation(rIter), inputB.GetLocalToParentRotation(rIter)));
            }

            var sCount = output.m_StreamDescriptor.ScaleCount;

            for (int sIter = 0; sIter < sCount; sIter++)
            {
                output.SetLocalToParentScale(sIter, inputA.GetLocalToParentScale(sIter) + inputB.GetLocalToParentScale(sIter));
            }

            var fCount = output.m_StreamDescriptor.FloatCount;

            for (int fIter = 0; fIter < fCount; fIter++)
            {
                output.SetFloat(fIter, inputA.GetFloat(fIter) + inputB.GetFloat(fIter));
            }

            var iCount = output.m_StreamDescriptor.IntCount;

            for (int iIter = 0; iIter < iCount; iIter++)
            {
                output.SetInt(iIter, inputA.GetInt(iIter) + inputB.GetInt(iIter));
            }
        }

        static public void InversePose<TOutputDescriptor, TInputDescriptor>(
            ref AnimationStream<TOutputDescriptor> output,
            ref AnimationStream<TInputDescriptor> input
        )
            where TOutputDescriptor : struct, IAnimationStreamDescriptor
            where TInputDescriptor : struct, IAnimationStreamDescriptor
        {
            Assert.IsFalse(output.IsNull);
            Assert.IsFalse(input.IsNull);
            Assert.IsTrue(output.Rig == input.Rig);

            var tCount = output.m_StreamDescriptor.TranslationCount;

            for (int tIter = 0; tIter < tCount; tIter++)
            {
                output.SetLocalToParentTranslation(tIter, input.GetLocalToParentTranslation(tIter) * -1.0f);
            }

            var rCount = output.m_StreamDescriptor.RotationCount;

            for (int rIter = 0; rIter < rCount; rIter++)
            {
                output.SetLocalToParentRotation(rIter, math.conjugate(input.GetLocalToParentRotation(rIter)));
            }

            var sCount = output.m_StreamDescriptor.ScaleCount;

            for (int sIter = 0; sIter < sCount; sIter++)
            {
                output.SetLocalToParentScale(sIter, input.GetLocalToParentScale(sIter) * -1.0f);
            }

            var fCount = output.m_StreamDescriptor.FloatCount;

            for (int fIter = 0; fIter < fCount; fIter++)
            {
                output.SetFloat(fIter, input.GetFloat(fIter) * -1.0f);
            }

            var iCount = output.m_StreamDescriptor.IntCount;

            for (int iIter = 0; iIter < iCount; iIter++)
            {
                output.SetInt(iIter, input.GetInt(iIter) * -1);
            }
        }

        static public void WeightPose<TOutputDescriptor, TInputDescriptor>(
            ref AnimationStream<TOutputDescriptor> output,
            ref AnimationStream<TInputDescriptor> input,
            ref NativeArray<float> weight
        )
            where TOutputDescriptor : struct, IAnimationStreamDescriptor
            where TInputDescriptor : struct, IAnimationStreamDescriptor
        {
            Assert.IsFalse(output.IsNull);
            Assert.IsFalse(input.IsNull);
            Assert.IsTrue(weight != null);
            Assert.IsTrue(output.Rig == input.Rig);
            Assert.IsTrue(output.Rig.Value.Bindings.BindingCount == weight.Length);

            var channelIndex = 0;

            var tCount = output.m_StreamDescriptor.TranslationCount;

            for (int tIter = 0; tIter < tCount; tIter++, channelIndex++)
            {
                output.SetLocalToParentTranslation(tIter, input.GetLocalToParentTranslation(tIter) * weight[channelIndex]);
            }

            var rCount = output.m_StreamDescriptor.RotationCount;

            for (int rIter = 0; rIter < rCount; rIter++, channelIndex++)
            {
                output.SetLocalToParentRotation(rIter, mathex.quatWeight(input.GetLocalToParentRotation(rIter), weight[channelIndex]));
            }

            var sCount = output.m_StreamDescriptor.ScaleCount;

            for (int sIter = 0; sIter < sCount; sIter++, channelIndex++)
            {
                output.SetLocalToParentScale(sIter, input.GetLocalToParentScale(sIter) * weight[channelIndex]);
            }

            var fCount = output.m_StreamDescriptor.FloatCount;

            for (int fIter = 0; fIter < fCount; fIter++, channelIndex++)
            {
                output.SetFloat(fIter, input.GetFloat(fIter) * weight[channelIndex]);
            }

            var iCount = output.m_StreamDescriptor.IntCount;

            for (int iIter = 0; iIter < iCount; iIter++, channelIndex++)
            {
                output.SetInt(iIter, (int)(input.GetInt(iIter) * weight[channelIndex]));
            }
        }
    }
}

