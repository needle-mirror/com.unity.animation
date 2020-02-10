using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace Unity.Animation
{
    static public partial class Core
    {
        public static int WeightDataSize(BlobAssetReference<RigDefinition> rig)
        {
            Assert.IsTrue(rig.IsCreated);
            ref var bindings = ref rig.Value.Bindings;
            return bindings.DataChunkCount * BindingSet.k_DataPadding + bindings.RotationCurveCount;
        }

        internal static int ChannelIndexToWeightDataOffset(BlobAssetReference<RigDefinition> rig, int channelIndex)
        {
            Assert.IsTrue(rig.IsCreated);
            ref var bindings = ref rig.Value.Bindings;

            Assert.IsTrue(channelIndex < bindings.CurveCount);

            if (channelIndex < bindings.ScaleBindingIndex)
                return math.mad(channelIndex - bindings.TranslationBindingIndex, BindingSet.TranslationKeyFloatCount, bindings.TranslationSamplesOffset);

            if (channelIndex < bindings.FloatBindingIndex)
                return math.mad(channelIndex - bindings.ScaleBindingIndex, BindingSet.ScaleKeyFloatCount, bindings.ScaleSamplesOffset);

            if (channelIndex < bindings.IntBindingIndex)
                return math.mad(channelIndex - bindings.FloatBindingIndex, BindingSet.FloatKeyFloatCount, bindings.FloatSamplesOffset);

            if (channelIndex < bindings.RotationBindingIndex)
                return math.mad(channelIndex - bindings.IntBindingIndex, BindingSet.IntKeyFloatCount, bindings.IntSamplesOffset);

            return (channelIndex - bindings.RotationBindingIndex) + bindings.RotationSamplesOffset;
        }

        internal static unsafe void SetWeightValueFromOffset(BlobAssetReference<RigDefinition> rig, float weight, int weightOffset, NativeArray<WeightData> weightData)
        {
            Assert.IsTrue(weightOffset < weightData.Length);
            Assert.AreEqual(weightData.Length, WeightDataSize(rig));

            float* ptr = (float*)weightData.GetUnsafePtr() + weightOffset;
            if (weightOffset < rig.Value.Bindings.FloatSamplesOffset)
                *((float3*)ptr) = weight;
            else
                *ptr = weight;
        }

        public static void SetWeightValueFromChannelIndex(BlobAssetReference<RigDefinition> rig, float weight, int channelIndex, NativeArray<WeightData> weightData)
        {
            Assert.IsTrue(channelIndex < rig.Value.Bindings.CurveCount);
            Assert.AreEqual(weightData.Length, WeightDataSize(rig));

            SetWeightValueFromOffset(rig, weight, ChannelIndexToWeightDataOffset(rig, channelIndex), weightData);
        }

        internal static unsafe void SetDefaultWeight(
            float defaultWeight,
            NativeArray<WeightData> weightData
            )
        {
            // 4-wide assignement of default values
            float4* outFloat4 = (float4*)weightData.GetUnsafePtr();
            int length = weightData.Length >> 2;
            for (int i = 0; i < length; ++i)
            {
                outFloat4[i] = defaultWeight;
            }

            // Remaining values
            float* outFloat = (float*)weightData.GetUnsafePtr();
            for (int i = length << 2, count = weightData.Length; i < count; ++i)
            {
                outFloat[i] = defaultWeight;
            }
        }

        public static unsafe void ComputeWeightDataFromChannelIndices(
            BlobAssetReference<RigDefinition> rig,
            float defaultWeight,
            NativeArray<int> channelIndices,
            NativeArray<float> channelWeights,
            NativeArray<WeightData> weightData
            )
        {
            Assert.IsTrue(rig.IsCreated);
            Assert.AreEqual(channelIndices.Length, channelWeights.Length);
            Assert.AreEqual(WeightDataSize(rig), weightData.Length);

            SetDefaultWeight(defaultWeight, weightData);
            for (int i = 0, count = channelIndices.Length; i < count; ++i)
            {
                SetWeightValueFromChannelIndex(rig, channelWeights[i], channelIndices[i], weightData);
            }
        }

        internal static unsafe void ComputeWeightDataFromWeightOffsets(
            BlobAssetReference<RigDefinition> rig,
            float defaultWeight,
            NativeArray<int> weightOffsets,
            NativeArray<float> channelWeights,
            NativeArray<WeightData> weightData
            )
        {
            Assert.IsTrue(rig.IsCreated);
            Assert.IsTrue(channelWeights.Length <= weightOffsets.Length);
            Assert.AreEqual(WeightDataSize(rig), weightData.Length);

            SetDefaultWeight(defaultWeight, weightData);
            for (int i = 0, count = channelWeights.Length; i < count; ++i)
            {
                SetWeightValueFromOffset(rig, channelWeights[i], weightOffsets[i], weightData);
            }
        }
    }
}
