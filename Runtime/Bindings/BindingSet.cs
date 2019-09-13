using Unity.Entities;

namespace Unity.Animation
{
    public struct BindingSet
    {
        public BlobArray<StringHash> TranslationBindings;
        public BlobArray<StringHash> RotationBindings;
        public BlobArray<StringHash> ScaleBindings;
        public BlobArray<StringHash> FloatBindings;
        public BlobArray<StringHash> IntBindings;

        // Binding count
        public int TranslationBindingIndex => 0;
        public int RotationBindingIndex => TranslationBindingIndex + TranslationBindings.Length;
        public int ScaleBindingIndex => RotationBindingIndex + RotationBindings.Length;
        public int BindingCount => TranslationBindings.Length + RotationBindings.Length + ScaleBindings.Length + FloatBindings.Length + IntBindings.Length;

        // Key type float count
        public static int TranslationKeyFloatCount => 3;
        public static int RotationKeyFloatCount => 4;
        public static int ScaleKeyFloatCount => 3;
        public static int FloatKeyFloatCount => 1;
        public static int IntKeyFloatCount => 1;

        // Curve count
        public int TranslationCurveCount => TranslationBindings.Length * TranslationKeyFloatCount;
        public int RotationCurveCount => RotationBindings.Length * RotationKeyFloatCount;
        public int ScaleCurveCount => ScaleBindings.Length * ScaleKeyFloatCount;
        public int FloatCurveCount => FloatBindings.Length * FloatKeyFloatCount;
        public int IntCurveCount => IntBindings.Length * IntKeyFloatCount;
        public int CurveCount => TranslationCurveCount + RotationCurveCount + ScaleCurveCount + FloatCurveCount + IntCurveCount;

        // Sample offsets
        public int TranslationSamplesOffset => 0;
        public int RotationSamplesOffset => TranslationSamplesOffset + TranslationCurveCount;
        public int ScaleSamplesOffset => RotationSamplesOffset + RotationCurveCount;
        public int FloatSamplesOffset => ScaleSamplesOffset + ScaleCurveCount;
        public int IntSamplesOffset => FloatSamplesOffset + FloatCurveCount;
    }
}
