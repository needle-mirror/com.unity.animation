using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.CompilerServices;

namespace Unity.Animation
{
    [BurstCompatible]
    public struct BindingSet
    {
        public BlobArray<StringHash> TranslationBindings;
        public BlobArray<StringHash> RotationBindings;
        public BlobArray<StringHash> ScaleBindings;
        public BlobArray<StringHash> FloatBindings;
        public BlobArray<StringHash> IntBindings;

        internal static readonly int k_RotationDataChunkSize = UnsafeUtility.SizeOf<quaternion4>() / UnsafeUtility.SizeOf<AnimatedData>();
        internal static readonly int k_DiscreteDataChunkSize = UnsafeUtility.SizeOf<int4>() / UnsafeUtility.SizeOf<AnimatedData>();
        internal static readonly int k_InterpolatedDataChunkSize = UnsafeUtility.SizeOf<float4>() / UnsafeUtility.SizeOf<AnimatedData>();

        // Key type float count
        public const int TranslationKeyFloatCount = 3;
        public const int RotationKeyFloatCount = 4;
        public const int ScaleKeyFloatCount = 3;
        public const int FloatKeyFloatCount = 1;
        public const int IntKeyFloatCount = 1;

        // Binding count
        public int RotationBindingIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int TranslationBindingIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int ScaleBindingIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int FloatBindingIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int IntBindingIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int BindingCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        // Curve count
        public int TranslationCurveCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int RotationCurveCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int ScaleCurveCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int FloatCurveCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int IntCurveCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int CurveCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        // Sample offsets
        public int RotationSamplesOffset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int TranslationSamplesOffset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int ScaleSamplesOffset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int FloatSamplesOffset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int IntSamplesOffset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int ChannelMaskOffset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        public int RotationDataChunkCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int DiscreteDataChunkCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int InterpolatedDataChunkCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int ChannelSize { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int StreamSize { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
    }

    [BurstCompatible]
    public static class ClipExt
    {
        public static BindingSet CreateBindingSet(this in Clip _, int translationCount, int rotationCount, int scaleCount, int floatCount, int intCount)
        {
            var dataLayout = new BindingSet
            {
                TranslationCurveCount = translationCount * BindingSet.TranslationKeyFloatCount,
                RotationCurveCount = rotationCount * BindingSet.RotationKeyFloatCount,
                ScaleCurveCount = scaleCount * BindingSet.ScaleKeyFloatCount,
                FloatCurveCount = floatCount * BindingSet.FloatKeyFloatCount,
                IntCurveCount = intCount * BindingSet.IntKeyFloatCount
            };

            dataLayout.CurveCount =  dataLayout.TranslationCurveCount + dataLayout.RotationCurveCount + dataLayout.ScaleCurveCount + dataLayout.FloatCurveCount + dataLayout.IntCurveCount;
            dataLayout.RotationDataChunkCount = 0;
            dataLayout.DiscreteDataChunkCount = 0;
            dataLayout.InterpolatedDataChunkCount = 0;

            dataLayout.ChannelSize = dataLayout.CurveCount;
            dataLayout.StreamSize  = dataLayout.CurveCount;

            dataLayout.TranslationSamplesOffset = 0;
            dataLayout.ScaleSamplesOffset = dataLayout.TranslationSamplesOffset + dataLayout.TranslationCurveCount;
            dataLayout.FloatSamplesOffset = dataLayout.ScaleSamplesOffset + dataLayout.ScaleCurveCount;
            dataLayout.IntSamplesOffset = dataLayout.FloatSamplesOffset + dataLayout.FloatCurveCount;
            dataLayout.RotationSamplesOffset = dataLayout.IntSamplesOffset + dataLayout.IntCurveCount;
            dataLayout.ChannelMaskOffset = 0;

            dataLayout.TranslationBindingIndex = 0;
            dataLayout.ScaleBindingIndex = dataLayout.TranslationBindingIndex + translationCount;
            dataLayout.FloatBindingIndex = dataLayout.ScaleBindingIndex + scaleCount;
            dataLayout.IntBindingIndex = dataLayout.FloatBindingIndex + floatCount;
            dataLayout.RotationBindingIndex = dataLayout.IntBindingIndex + intCount;
            dataLayout.BindingCount = translationCount + rotationCount + scaleCount + floatCount + intCount;

            return dataLayout;
        }
    }

    [BurstCompatible]
    public static class RigDefinitionExt
    {
        public static BindingSet CreateBindingSet(this in RigDefinition _, int translationCount, int rotationCount, int scaleCount, int floatCount, int intCount)
        {
            var dataLayout = new BindingSet
            {
                TranslationCurveCount = translationCount * BindingSet.TranslationKeyFloatCount,
                RotationCurveCount = rotationCount * BindingSet.RotationKeyFloatCount,
                ScaleCurveCount = scaleCount * BindingSet.ScaleKeyFloatCount,
                FloatCurveCount = floatCount * BindingSet.FloatKeyFloatCount,
                IntCurveCount = intCount * BindingSet.IntKeyFloatCount
            };

            dataLayout.CurveCount =  dataLayout.TranslationCurveCount + dataLayout.RotationCurveCount + dataLayout.ScaleCurveCount + dataLayout.FloatCurveCount + dataLayout.IntCurveCount;

            var dataCurveCount = (dataLayout.CurveCount - dataLayout.RotationCurveCount - dataLayout.IntCurveCount);

            dataLayout.InterpolatedDataChunkCount = (int)math.ceil(dataCurveCount / (float)BindingSet.k_InterpolatedDataChunkSize);
            dataLayout.DiscreteDataChunkCount = (int)math.ceil(dataLayout.IntCurveCount / (float)BindingSet.k_DiscreteDataChunkSize);
            dataLayout.RotationDataChunkCount = (int)math.ceil(dataLayout.RotationCurveCount / (float)BindingSet.k_RotationDataChunkSize);

            dataLayout.ChannelSize = dataLayout.InterpolatedDataChunkCount * BindingSet.k_InterpolatedDataChunkSize + dataLayout.DiscreteDataChunkCount * BindingSet.k_DiscreteDataChunkSize + dataLayout.RotationDataChunkCount * BindingSet.k_RotationDataChunkSize;

            dataLayout.TranslationSamplesOffset = 0;
            dataLayout.ScaleSamplesOffset = dataLayout.TranslationSamplesOffset + dataLayout.TranslationCurveCount;
            dataLayout.FloatSamplesOffset = dataLayout.ScaleSamplesOffset + dataLayout.ScaleCurveCount;
            dataLayout.IntSamplesOffset = dataLayout.InterpolatedDataChunkCount * BindingSet.k_InterpolatedDataChunkSize;
            dataLayout.RotationSamplesOffset = dataLayout.IntSamplesOffset +  dataLayout.DiscreteDataChunkCount * BindingSet.k_DiscreteDataChunkSize;
            dataLayout.ChannelMaskOffset = dataLayout.RotationSamplesOffset + dataLayout.RotationDataChunkCount * BindingSet.k_RotationDataChunkSize;

            dataLayout.TranslationBindingIndex = 0;
            dataLayout.ScaleBindingIndex = dataLayout.TranslationBindingIndex + translationCount;
            dataLayout.FloatBindingIndex = dataLayout.ScaleBindingIndex + scaleCount;
            dataLayout.IntBindingIndex = dataLayout.FloatBindingIndex + floatCount;
            dataLayout.RotationBindingIndex = dataLayout.IntBindingIndex + intCount;
            dataLayout.BindingCount = translationCount + rotationCount + scaleCount + floatCount + intCount;

            // Mask size must be multiple of 8-bytes. see UnsafeBitArray
            // StreamSize is the number of 4-bytes needed
            const int bitsInBytes = 8;
            const int bitsIn8Bytes = 8 * bitsInBytes;
            const int bitsIn4Bytes = 4 * bitsInBytes;
            var maskSizeIn4Bytes = Core.AlignUp(dataLayout.BindingCount, bitsIn8Bytes) / bitsIn4Bytes;
            dataLayout.StreamSize = dataLayout.ChannelSize + (maskSizeIn4Bytes * 2);

            return dataLayout;
        }
    }
}
