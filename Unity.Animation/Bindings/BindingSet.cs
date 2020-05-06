using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.CompilerServices;

namespace Unity.Animation
{
    public struct BindingSet
    {
        public BlobArray<StringHash> TranslationBindings;
        public BlobArray<StringHash> RotationBindings;
        public BlobArray<StringHash> ScaleBindings;
        public BlobArray<StringHash> FloatBindings;
        public BlobArray<StringHash> IntBindings;

        internal static readonly int k_RotationPadding = UnsafeUtility.SizeOf<quaternion4>() / UnsafeUtility.SizeOf<AnimatedData>();
        internal static readonly int k_DataPadding = UnsafeUtility.SizeOf<float4>() / UnsafeUtility.SizeOf<AnimatedData>();

        // Key type float count
        public static readonly int TranslationKeyFloatCount = 3;
        public static readonly int RotationKeyFloatCount = 4;
        public static readonly int ScaleKeyFloatCount = 3;
        public static readonly int FloatKeyFloatCount = 1;
        public static readonly int IntKeyFloatCount = 1;

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

        public int RotationChunkCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int DataChunkCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        public int StreamSize { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
    }

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
            dataLayout.RotationChunkCount = 0;
            dataLayout.DataChunkCount = 0;

            dataLayout.StreamSize = dataLayout.CurveCount;

            dataLayout.TranslationSamplesOffset = 0;
            dataLayout.ScaleSamplesOffset = dataLayout.TranslationSamplesOffset + dataLayout.TranslationCurveCount;
            dataLayout.FloatSamplesOffset = dataLayout.ScaleSamplesOffset + dataLayout.ScaleCurveCount;
            dataLayout.IntSamplesOffset = dataLayout.FloatSamplesOffset + dataLayout.FloatCurveCount;
            dataLayout.RotationSamplesOffset = dataLayout.IntSamplesOffset + dataLayout.IntCurveCount;

            dataLayout.TranslationBindingIndex = 0;
            dataLayout.ScaleBindingIndex = dataLayout.TranslationBindingIndex + translationCount;
            dataLayout.FloatBindingIndex = dataLayout.ScaleBindingIndex + scaleCount;
            dataLayout.IntBindingIndex = dataLayout.FloatBindingIndex + floatCount;
            dataLayout.RotationBindingIndex = dataLayout.IntBindingIndex + intCount;
            dataLayout.BindingCount = translationCount + rotationCount + scaleCount + floatCount + intCount;

            return dataLayout;
        }
    }

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

            bool hasRotationPadding = math.modf(rotationCount / 4f, out float rotationChunkCount) > 0f;
            var dataCurveCount = (dataLayout.CurveCount - dataLayout.RotationCurveCount);
            bool hasDataPadding = math.modf(dataCurveCount / 4f, out float dataChunkCount) > 0f;

            dataLayout.DataChunkCount = math.select((int)dataChunkCount, (int)dataChunkCount + 1, hasDataPadding);
            dataLayout.RotationChunkCount = math.select((int)rotationChunkCount, (int)rotationChunkCount + 1, hasRotationPadding);

            dataLayout.StreamSize = dataLayout.DataChunkCount * BindingSet.k_DataPadding + dataLayout.RotationChunkCount * BindingSet.k_RotationPadding;

            dataLayout.TranslationSamplesOffset = 0;
            dataLayout.ScaleSamplesOffset = dataLayout.TranslationSamplesOffset + dataLayout.TranslationCurveCount;
            dataLayout.FloatSamplesOffset = dataLayout.ScaleSamplesOffset + dataLayout.ScaleCurveCount;
            dataLayout.IntSamplesOffset = dataLayout.FloatSamplesOffset + dataLayout.FloatCurveCount;
            dataLayout.RotationSamplesOffset = dataLayout.DataChunkCount * BindingSet.k_DataPadding;

            dataLayout.TranslationBindingIndex = 0;
            dataLayout.ScaleBindingIndex = dataLayout.TranslationBindingIndex + translationCount;
            dataLayout.FloatBindingIndex = dataLayout.ScaleBindingIndex + scaleCount;
            dataLayout.IntBindingIndex = dataLayout.FloatBindingIndex + floatCount;
            dataLayout.RotationBindingIndex = dataLayout.IntBindingIndex + intCount;
            dataLayout.BindingCount = translationCount + rotationCount + scaleCount + floatCount + intCount;

            return dataLayout;
        }
    }
}
