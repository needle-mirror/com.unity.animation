using System;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation.Tests
{
    class GraphVariantConverterTests
    {
        internal class GraphVariantConverterTestCase<TConverter, TPrimitive>
            where TConverter : IFromGraphVariantConverter<TPrimitive>, new()
            where TPrimitive : struct
        {
            public static void Test(TPrimitive primitiveValue, GraphVariant variantValue)
            {
                var converter = new TConverter();
                Assert.AreEqual(converter.ConvertTo(primitiveValue), variantValue);
                Assert.AreEqual(converter.ConvertFrom(variantValue), primitiveValue);
                Assert.AreEqual(converter.ExpectedType, variantValue.Type);
            }
        }

        [Test]
        public void ConvertToAndFrom_Bool_ConvertsValue()
        {
            var v = true;
            GraphVariantConverterTestCase<GraphVariantToBoolConverter, bool>.Test(
                v,
                new GraphVariant{
                    Type = GraphVariant.ValueType.Bool,
                    Bool = v
                }
            );
        }

        [Test]
        public void ConvertToAndFrom_Short_ConvertsValue()
        {
            var v = Int16.MinValue;
            GraphVariantConverterTestCase<GraphVariantToShortConverter, short>.Test(
                v,
                new GraphVariant{
                    Type = GraphVariant.ValueType.Short,
                    Short = v
                }
            );
        }

        [Test]
        public void ConvertToAndFrom_UShort_ConvertsValue()
        {
            var v = UInt16.MinValue;
            GraphVariantConverterTestCase<GraphVariantToUShortConverter, ushort>.Test(
                v,
                new GraphVariant{
                    Type = GraphVariant.ValueType.UShort,
                    UShort = v
                }
            );
        }

        [Test]
        public void ConvertToAndFrom_Int_ConvertsValue()
        {
            var v = Int32.MinValue;
            GraphVariantConverterTestCase<GraphVariantToIntConverter, int>.Test(
                v,
                new GraphVariant{
                    Type = GraphVariant.ValueType.Int,
                    Int = v
                }
            );
        }

        [Test]
        public void ConvertToAndFrom_UInt_ConvertsValue()
        {
            var v = UInt32.MinValue;
            GraphVariantConverterTestCase<GraphVariantToUIntConverter, uint>.Test(
                v,
                new GraphVariant{
                    Type = GraphVariant.ValueType.UInt,
                    UInt = v
                }
            );
        }

        [Test]
        public void ConvertToAndFrom_Long_ConvertsValue()
        {
            var v = Int64.MinValue;
            GraphVariantConverterTestCase<GraphVariantToLongConverter, long>.Test(
                v,
                new GraphVariant{
                    Type = GraphVariant.ValueType.Long,
                    Long = v
                }
            );
        }

        [Test]
        public void ConvertToAndFrom_ULong_ConvertsValue()
        {
            var v = UInt64.MinValue;
            GraphVariantConverterTestCase<GraphVariantToULongConverter, ulong>.Test(
                v,
                new GraphVariant{
                    Type = GraphVariant.ValueType.ULong,
                    ULong = v
                }
            );
        }

        [Test]
        public void ConvertToAndFrom_Float_ConvertsValue()
        {
            var v = 2F;
            GraphVariantConverterTestCase<GraphVariantToFloatConverter, float>.Test(
                v,
                new GraphVariant{
                    Type = GraphVariant.ValueType.Float,
                    Float = v
                }
            );
        }

        [Test]
        public void ConvertToAndFrom_Float2_ConvertsValue()
        {
            var v = new float2(2f, 4f);
            GraphVariantConverterTestCase<GraphVariantToFloat2Converter, float2>.Test(
                v,
                new GraphVariant{
                    Type = GraphVariant.ValueType.Float2,
                    Float2 = v
                }
            );
        }

        [Test]
        public void ConvertToAndFrom_Float3_ConvertsValue()
        {
            var v = new float3(2f, 4f, 8f);
            GraphVariantConverterTestCase<GraphVariantToFloat3Converter, float3>.Test(
                v,
                new GraphVariant{
                    Type = GraphVariant.ValueType.Float3,
                    Float3 = v
                }
            );
        }

        [Test]
        public void ConvertToAndFrom_Float4_ConvertsValue()
        {
            var v = new float4(2f, 4f, 8f, 16f);
            GraphVariantConverterTestCase<GraphVariantToFloat4Converter, float4>.Test(
                v ,
                new GraphVariant{
                    Type = GraphVariant.ValueType.Float4,
                    Float4 = v
                }
            );
        }

        [Test]
        public void ConvertToAndFrom_Quaternion_ConvertsValue()
        {
            var v = new quaternion(2f, 4f, 8f, 16f);
            GraphVariantConverterTestCase<GraphVariantToQuaternionConverter, quaternion>.Test(
                v,
                new GraphVariant{
                    Type = GraphVariant.ValueType.Quaternion,
                    Quaternion = v
                }
            );
        }

        [Test]
        public void ConvertToAndFrom_Hash128_ConvertsValue()
        {
            var v = new Hash128("4fb16d384de56ba44abed9ffe2fc0370");
            GraphVariantConverterTestCase<GraphVariantToHash128Converter, Hash128>.Test(
                v,
                new GraphVariant{
                    Type = GraphVariant.ValueType.Hash128,
                    Hash128 = v
                }
            );
        }

        [Test]
        public void ConvertToAndFrom_Int4_ConvertsValue()
        {
            var v = new int4(-1, 1, -2, 8);
            GraphVariantConverterTestCase<GraphVariantToInt4Converter, int4>.Test(
                v,
                new GraphVariant{
                    Type = GraphVariant.ValueType.Int4,
                    Int4 = v
                }
            );
        }
    }
}
