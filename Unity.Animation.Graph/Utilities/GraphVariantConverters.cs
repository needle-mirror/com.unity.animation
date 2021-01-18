using Unity.Mathematics;

namespace Unity.Animation
{
    public interface IFromGraphVariantConverter<T>
        where T : struct
    {
        GraphVariant.ValueType ExpectedType { get; }
        T ConvertFrom(GraphVariant variant);
        GraphVariant ConvertTo(T value);
    }

    public struct GraphVariantToBoolConverter : IFromGraphVariantConverter<bool>
    {
        public GraphVariant ConvertTo(bool value)
        {
            return new GraphVariant() {Bool = value};
        }

        public bool ConvertFrom(GraphVariant variant)
        {
            return variant.Bool;
        }

        public GraphVariant.ValueType ExpectedType => GraphVariant.ValueType.Bool;
    }

    public struct GraphVariantToIntConverter : IFromGraphVariantConverter<int>
    {
        public GraphVariant ConvertTo(int value)
        {
            return new GraphVariant() {Int = value};
        }

        public int ConvertFrom(GraphVariant variant)
        {
            return variant.Int;
        }

        public GraphVariant.ValueType ExpectedType => GraphVariant.ValueType.Int;
    }

    public struct GraphVariantToUIntConverter : IFromGraphVariantConverter<uint>
    {
        public GraphVariant ConvertTo(uint value)
        {
            return new GraphVariant() {UInt = value};
        }

        public uint ConvertFrom(GraphVariant variant)
        {
            return variant.UInt;
        }

        public GraphVariant.ValueType ExpectedType => GraphVariant.ValueType.UInt;
    }

    public struct GraphVariantToShortConverter : IFromGraphVariantConverter<short>
    {
        public GraphVariant ConvertTo(short value)
        {
            return new GraphVariant() {Short = value};
        }

        public short ConvertFrom(GraphVariant variant)
        {
            return variant.Short;
        }

        public GraphVariant.ValueType ExpectedType => GraphVariant.ValueType.Short;
    }

    public struct GraphVariantToUShortConverter : IFromGraphVariantConverter<ushort>
    {
        public GraphVariant ConvertTo(ushort value)
        {
            return new GraphVariant() {UShort = value};
        }

        public ushort ConvertFrom(GraphVariant variant)
        {
            return variant.UShort;
        }

        public GraphVariant.ValueType ExpectedType => GraphVariant.ValueType.UShort;
    }

    public struct GraphVariantToLongConverter : IFromGraphVariantConverter<long>
    {
        public GraphVariant ConvertTo(long value)
        {
            return new GraphVariant() {Long = value};
        }

        public long ConvertFrom(GraphVariant variant)
        {
            return variant.Long;
        }

        public GraphVariant.ValueType ExpectedType => GraphVariant.ValueType.Long;
    }

    public struct GraphVariantToULongConverter : IFromGraphVariantConverter<ulong>
    {
        public GraphVariant ConvertTo(ulong value)
        {
            return new GraphVariant() {ULong = value};
        }

        public ulong ConvertFrom(GraphVariant variant)
        {
            return variant.ULong;
        }

        public GraphVariant.ValueType ExpectedType => GraphVariant.ValueType.ULong;
    }

    public struct GraphVariantToFloatConverter : IFromGraphVariantConverter<float>
    {
        public GraphVariant ConvertTo(float value)
        {
            return new GraphVariant() {Float = value};
        }

        public float ConvertFrom(GraphVariant variant)
        {
            return variant.Float;
        }

        public GraphVariant.ValueType ExpectedType => GraphVariant.ValueType.Float;
    }

    public struct GraphVariantToFloat2Converter : IFromGraphVariantConverter<float2>
    {
        public GraphVariant ConvertTo(float2 value)
        {
            return new GraphVariant() {Float2 = value};
        }

        public float2 ConvertFrom(GraphVariant variant)
        {
            return variant.Float2;
        }

        public GraphVariant.ValueType ExpectedType => GraphVariant.ValueType.Float2;
    }

    public struct GraphVariantToFloat3Converter : IFromGraphVariantConverter<float3>
    {
        public GraphVariant ConvertTo(float3 value)
        {
            return new GraphVariant() {Float3 = value};
        }

        public float3 ConvertFrom(GraphVariant variant)
        {
            return variant.Float3;
        }

        public GraphVariant.ValueType ExpectedType => GraphVariant.ValueType.Float3;
    }

    public struct GraphVariantToFloat4Converter : IFromGraphVariantConverter<float4>
    {
        public GraphVariant ConvertTo(float4 value)
        {
            return new GraphVariant() {Float4 = value};
        }

        public float4 ConvertFrom(GraphVariant variant)
        {
            return variant.Float4;
        }

        public GraphVariant.ValueType ExpectedType => GraphVariant.ValueType.Float4;
    }

    public struct GraphVariantToQuaternionConverter : IFromGraphVariantConverter<quaternion>
    {
        public GraphVariant ConvertTo(quaternion value)
        {
            return new GraphVariant() {Quaternion = value};
        }

        public quaternion ConvertFrom(GraphVariant variant)
        {
            return variant.Quaternion;
        }

        public GraphVariant.ValueType ExpectedType => GraphVariant.ValueType.Quaternion;
    }

    public struct GraphVariantToHash128Converter : IFromGraphVariantConverter<Entities.Hash128>
    {
        public GraphVariant ConvertTo(Entities.Hash128 value)
        {
            return new GraphVariant() {Hash128 = value};
        }

        public Entities.Hash128 ConvertFrom(GraphVariant variant)
        {
            return variant.Hash128;
        }

        public GraphVariant.ValueType ExpectedType => GraphVariant.ValueType.Hash128;
    }

    public struct GraphVariantToInt4Converter : IFromGraphVariantConverter<int4>
    {
        public GraphVariant ConvertTo(int4 value)
        {
            return new GraphVariant() {Int4 = value};
        }

        public int4 ConvertFrom(GraphVariant variant)
        {
            return variant.Int4;
        }

        public GraphVariant.ValueType ExpectedType => GraphVariant.ValueType.Int4;
    }
}
