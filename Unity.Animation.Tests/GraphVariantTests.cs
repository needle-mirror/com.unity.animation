using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation.Tests
{
    class GraphVariantTests
    {
        private static IEnumerable AllValidValueTypes()
        {
            return Enum.GetValues(typeof(GraphVariant.ValueType)).OfType<GraphVariant.ValueType>().Except(new[] { GraphVariant.ValueType.Unknown });
        }

        private static IEnumerable AllValidTypes()
        {
            yield return typeof(bool);
            yield return typeof(short);
            yield return typeof(ushort);
            yield return typeof(int);
            yield return typeof(uint);
            yield return typeof(long);
            yield return typeof(ulong);
            yield return typeof(float);
            yield return typeof(float2);
            yield return typeof(float3);
            yield return typeof(float4);
            yield return typeof(quaternion);
            yield return typeof(Entity);
            yield return typeof(Hash128);
            yield return typeof(int4);
        }

        public class CompareTestData
        {
            public delegate bool CompareDelegate(GraphVariant v);
            public GraphVariant v;
            public CompareDelegate Compare;
            public string Description;
            public override string ToString()
            {
                return Description;
            }
        }

        private static readonly CompareTestData[] CreateTestSources =
        {
            new CompareTestData(){ v = new GraphVariant { Bool = true },                                    Compare = (v) => v.Bool,                                                Description = "Bool" },
            new CompareTestData(){ v = new GraphVariant { Int = Int32.MinValue },                           Compare = (v) => v.Int == Int32.MinValue,                               Description = "Int"  },
            new CompareTestData(){ v = new GraphVariant { UInt = UInt32.MaxValue },                         Compare = (v) => v.UInt == UInt32.MaxValue,                             Description = "UInt"  },
            new CompareTestData(){ v = new GraphVariant { Long = Int64.MinValue },                          Compare = (v) => v.Long == Int64.MinValue,                              Description = "Long"  },
            new CompareTestData(){ v = new GraphVariant { ULong = UInt64.MaxValue},                         Compare = (v) => v.ULong == UInt64.MaxValue,                            Description = "ULong"  },
            new CompareTestData(){ v = new GraphVariant { Short = Int16.MinValue},                          Compare = (v) => v.Short == Int16.MinValue,                             Description = "Short"  },
            new CompareTestData(){ v = new GraphVariant { UShort = UInt16.MaxValue},                        Compare = (v) => v.UShort == UInt16.MaxValue,                           Description = "UShort"  },
            new CompareTestData(){ v = new GraphVariant { Float = 2F },                                     Compare = (v) => v.Float == 2F,                                         Description = "Float"  },
            new CompareTestData(){ v = new GraphVariant { Float2 = new float2(2f, 4f) },                    Compare = (v) => v.Float2.Equals(new float2(2f, 4f)),                   Description = "Float2"  },
            new CompareTestData(){ v = new GraphVariant { Float3 = new float3(2f, 4f, 8f) },                Compare = (v) => v.Float3.Equals(new float3(2f, 4f, 8f)),               Description = "Float3"  },
            new CompareTestData(){ v = new GraphVariant { Float4 = new float4(2f, 4f, 8f, 16f) },           Compare = (v) => v.Float4.Equals(new float4(2f, 4f, 8f, 16f)),          Description = "Float4"  },
            new CompareTestData(){ v = new GraphVariant { Quaternion = new quaternion(2f, 4f, 8f, 16f) },   Compare = (v) => v.Quaternion.Equals(new quaternion(2f, 4f, 8f, 16f)),  Description = "Quaternion"  },
            new CompareTestData(){ v = new GraphVariant { Hash128 = new Hash128("4fb16d384de56ba44abed9ffe2fc0370") },   Compare = (v) => v.Hash128.Equals(new Hash128("4fb16d384de56ba44abed9ffe2fc0370")),  Description = "Hash128"  },
            new CompareTestData(){ v = new GraphVariant { Int4 = new int4(-1, 1, -2, 8) },                  Compare = (v) => v.Int4.Equals(new int4(-1, 1, -2, 8)),                 Description = "Int4"  },
        };

        [Test]
        public void Create_WithValidType_StoresValidValue([ValueSource("CreateTestSources")] CompareTestData test)
        {
            Assert.That(test.Compare(test.v), "Create failed with type {0} for test {1}", test.v.Type, test.Description);
        }

        private static readonly CompareTestData[] ConversionsTestSources =
        {
            // To Int
            new CompareTestData(){ v = new GraphVariant { Bool = true },                                    Compare = (v) => v.Int == 1,                                            Description = "Bool to Int" },
            new CompareTestData(){ v = new GraphVariant { Short = Int16.MinValue },                         Compare = (v) => v.Int == Int16.MinValue,                               Description = "Short to Int" },
            new CompareTestData(){ v = new GraphVariant { UShort = UInt16.MaxValue },                       Compare = (v) => v.Int == UInt16.MaxValue,                              Description = "UShort to Int" },
            new CompareTestData(){ v = new GraphVariant { Float = 2F },                                     Compare = (v) => v.Int == 2F,                                           Description = "Float to Int" },

            // To UInt
            new CompareTestData(){ v = new GraphVariant { Bool = true },                                    Compare = (v) => v.UInt == 1,                                           Description = "Bool to UInt" },
            new CompareTestData(){ v = new GraphVariant { UShort = UInt16.MaxValue },                       Compare = (v) => v.UInt == UInt16.MaxValue,                             Description = "UShort to UInt" },

            // To Long
            new CompareTestData(){ v = new GraphVariant { Bool = true },                                    Compare = (v) => v.Long == 1,                                           Description = "Bool to Long" },
            new CompareTestData(){ v = new GraphVariant { Short = Int16.MinValue },                         Compare = (v) => v.Long == Int16.MinValue,                              Description = "Short to Long" },
            new CompareTestData(){ v = new GraphVariant { UShort = UInt16.MaxValue },                       Compare = (v) => v.Long == UInt16.MaxValue,                             Description = "UShort to Long" },
            new CompareTestData(){ v = new GraphVariant { Int = Int32.MinValue },                           Compare = (v) => v.Long == Int32.MinValue,                              Description = "Int to Long" },
            new CompareTestData(){ v = new GraphVariant { UInt = UInt32.MaxValue },                         Compare = (v) => v.Long == UInt32.MaxValue,                             Description = "UInt to Long" },
            new CompareTestData(){ v = new GraphVariant { UInt = UInt32.MaxValue },                         Compare = (v) => v.Long == UInt32.MaxValue,                             Description = "UInt to Long" },
            new CompareTestData(){ v = new GraphVariant { Float = 4F},                                      Compare = (v) => v.Long == 4F,                                          Description = "Float to Long" },

            // To ULong
            new CompareTestData(){ v = new GraphVariant { Bool = true },                                    Compare = (v) => v.ULong == 1,                                          Description = "Bool to ULong" },
            new CompareTestData(){ v = new GraphVariant { UShort = UInt16.MaxValue },                       Compare = (v) => v.ULong == UInt16.MaxValue,                            Description = "UShort to ULong" },
            new CompareTestData(){ v = new GraphVariant { UInt = UInt32.MaxValue },                         Compare = (v) => v.ULong == UInt32.MaxValue,                            Description = "UInt to ULong" },

            // To Float
            new CompareTestData(){ v = new GraphVariant { Bool = true },                                    Compare = (v) => v.Float == 1,                                          Description = "Bool to Float" },
            new CompareTestData(){ v = new GraphVariant { Short = Int16.MinValue },                         Compare = (v) => v.Float == Int16.MinValue,                             Description = "Short to Float" },
            new CompareTestData(){ v = new GraphVariant { UShort = UInt16.MaxValue },                       Compare = (v) => v.Float == UInt16.MaxValue,                            Description = "UShort to Float" },

            // To Short
            new CompareTestData(){ v = new GraphVariant { Bool = true },                                    Compare = (v) => v.Short == 1,                                          Description = "Bool to Short" },

            // To UShort
            new CompareTestData(){ v = new GraphVariant { Bool = true },                                    Compare = (v) => v.UShort == 1,                                         Description = "Bool to UShort" },

            // To Float
            new CompareTestData(){ v = new GraphVariant { Bool = true },                                    Compare = (v) => v.Float == 1,                                          Description = "Bool to Float" },
            new CompareTestData(){ v = new GraphVariant { UShort = UInt16.MaxValue},                        Compare = (v) => v.Float == UInt16.MaxValue,                            Description = "UShort to Float" },
            new CompareTestData(){ v = new GraphVariant { Short = Int16.MinValue},                          Compare = (v) => v.Float == Int16.MinValue,                             Description = "Short to Float" },
            new CompareTestData(){ v = new GraphVariant { Float2 = new float2(2F, 4F) },                    Compare = (v) => v.Float == 2F,                                         Description = "Float2 to Float" },
            new CompareTestData(){ v = new GraphVariant { Float3 = new float3(2F, 4F, 8F) },                Compare = (v) => v.Float == 2F,                                         Description = "Float3 to Float" },
            new CompareTestData(){ v = new GraphVariant { Float4 = new float4(2F, 4F, 8F, 16F) },           Compare = (v) => v.Float == 2F,                                         Description = "Float4 to Float" },

            // To Float2
            new CompareTestData(){ v = new GraphVariant { Float = 2F },                                     Compare = (v) => v.Float2.x == 2F,                                      Description = "Float to Float2" },
            new CompareTestData(){ v = new GraphVariant { Float3 = new float3(2F, 4F, 8F) },                Compare = (v) => v.Float2.Equals(new float2(2F, 4F)),                   Description = "Float3 to Float2" },
            new CompareTestData(){ v = new GraphVariant { Float4 = new float4(2F, 4F, 8F, 16F) },           Compare = (v) => v.Float2.Equals(new float2(2F, 4F)),                   Description = "Float4 to Float2" },

            // To Float3
            new CompareTestData(){ v = new GraphVariant { Float = 2F },                                     Compare = (v) => v.Float3.x == 2F,                                      Description = "Float to Float3" },
            new CompareTestData(){ v = new GraphVariant { Float2 = new float2(2F, 4F) },                    Compare = (v) => v.Float3.xy.Equals(new float2(2F, 4F)),                Description = "Float2 to Float3" },
            new CompareTestData(){ v = new GraphVariant { Float4 = new float4(2F, 4F, 8F, 16F) },           Compare = (v) => v.Float3.Equals(new float3(2F, 4F, 8F)),               Description = "Float4 to Float3" },

            // To Float4
            new CompareTestData(){ v = new GraphVariant { Float = 2F },                                     Compare = (v) => v.Float4.x == 2F,                                      Description = "Float to Float4" },
            new CompareTestData(){ v = new GraphVariant { Float2 = new float2(2F, 4F) },                    Compare = (v) => v.Float4.xy.Equals(new float2(2F, 4F)),                Description = "Float2 to Float4" },
            new CompareTestData(){ v = new GraphVariant { Float3 = new float3(2F, 4F, 8F) },                Compare = (v) => v.Float4.xyz.Equals(new float3(2F, 4F, 8F)),           Description = "Float3 to Float4" },
            new CompareTestData(){ v = new GraphVariant { Quaternion = new quaternion(2F, 4F, 8F, 16F) },   Compare = (v) => v.Float4.Equals(new quaternion(2F, 4F, 8F, 16F).value), Description = "Quaternion to Float4" },

            // To Quaternion
            new CompareTestData(){ v = new GraphVariant { Float4 = new float4(2F, 4F, 8F, 16F) },           Compare = (v) => v.Quaternion.value.Equals(new float4(2F, 4F, 8F, 16F)), Description = "Float4 to Quaternion" },
        };

        [Test]
        public void Conversion_WithValidType_ConvertsValue([ValueSource("ConversionsTestSources")] CompareTestData test)
        {
            Assert.That(test.Compare(test.v), "Conversions failed with type {0} for test {1}", test.v.Type, test.Description);
        }

        [Test]
        public void Convert_FromUnsupportedType_ToBool_Throws(
            [Values(
                GraphVariant.ValueType.Unknown,
                GraphVariant.ValueType.Int, GraphVariant.ValueType.UInt,
                GraphVariant.ValueType.Short, GraphVariant.ValueType.UShort,
                GraphVariant.ValueType.Long, GraphVariant.ValueType.ULong,
                GraphVariant.ValueType.Float, GraphVariant.ValueType.Float2,
                GraphVariant.ValueType.Float3, GraphVariant.ValueType.Float4,
                GraphVariant.ValueType.Quaternion, GraphVariant.ValueType.Entity,
                GraphVariant.ValueType.Hash128, GraphVariant.ValueType.Int4)]
            GraphVariant.ValueType type)
        {
            var variant = new GraphVariant { Type = type };
            bool v;
            Assert.Throws<InvalidDataException>(() => v = variant.Bool);
        }

        [Test]
        public void Convert_FromUnsupportedType_ToFloat_Throws(
            [Values(
                GraphVariant.ValueType.Unknown,
                GraphVariant.ValueType.Int, GraphVariant.ValueType.UInt,
                GraphVariant.ValueType.Long, GraphVariant.ValueType.ULong,
                GraphVariant.ValueType.Quaternion, GraphVariant.ValueType.Entity,
                GraphVariant.ValueType.Hash128, GraphVariant.ValueType.Int4
             )]
            GraphVariant.ValueType type)
        {
            var variant = new GraphVariant { Type = type };
            float v;
            Assert.Throws<InvalidDataException>(() => v = variant.Float);
        }

        [Test]
        public void Convert_FromUnsupportedType_ToInt_Throws(
            [Values(
                GraphVariant.ValueType.Unknown, GraphVariant.ValueType.Long,
                GraphVariant.ValueType.UInt, GraphVariant.ValueType.Quaternion,
                GraphVariant.ValueType.Float2, GraphVariant.ValueType.Entity,
                GraphVariant.ValueType.Float3, GraphVariant.ValueType.Float4,
                GraphVariant.ValueType.Hash128, GraphVariant.ValueType.Int4)]
            GraphVariant.ValueType type)
        {
            var variant = new GraphVariant { Type = type };
            int v;
            Assert.Throws<InvalidDataException>(() => v = variant.Int);
        }

        [Test]
        public void Convert_FromUnsupportedType_ToUInt_Throws(
            [Values(
                GraphVariant.ValueType.Unknown, GraphVariant.ValueType.Long,
                GraphVariant.ValueType.Int, GraphVariant.ValueType.Short,
                GraphVariant.ValueType.Float, GraphVariant.ValueType.Float2,
                GraphVariant.ValueType.Float3, GraphVariant.ValueType.Float4,
                GraphVariant.ValueType.Quaternion, GraphVariant.ValueType.Entity,
                GraphVariant.ValueType.Hash128, GraphVariant.ValueType.Int4)]
            GraphVariant.ValueType type)
        {
            var variant = new GraphVariant { Type = type };
            uint v;
            Assert.Throws<InvalidDataException>(() => v = variant.UInt);
        }

        [Test]
        public void Convert_FromUnsupportedType_ToLong_Throws(
            [Values(
                GraphVariant.ValueType.Unknown, GraphVariant.ValueType.ULong,
                GraphVariant.ValueType.Entity, GraphVariant.ValueType.Float2,
                GraphVariant.ValueType.Float3, GraphVariant.ValueType.Float4,
                GraphVariant.ValueType.Quaternion,
                GraphVariant.ValueType.Hash128, GraphVariant.ValueType.Int4)]
            GraphVariant.ValueType type)
        {
            var variant = new GraphVariant { Type = type };
            long v;
            Assert.Throws<InvalidDataException>(() => v = variant.Long);
        }

        [Test]
        public void Convert_FromUnsupportedType_ToShort_Throws(
            [Values(
                GraphVariant.ValueType.Unknown, GraphVariant.ValueType.ULong,
                GraphVariant.ValueType.UInt, GraphVariant.ValueType.UShort,
                GraphVariant.ValueType.Int, GraphVariant.ValueType.Long,
                GraphVariant.ValueType.Float, GraphVariant.ValueType.Float2,
                GraphVariant.ValueType.Float3, GraphVariant.ValueType.Float4,
                GraphVariant.ValueType.Quaternion, GraphVariant.ValueType.Entity,
                GraphVariant.ValueType.Hash128, GraphVariant.ValueType.Int4)]
            GraphVariant.ValueType type)
        {
            var variant = new GraphVariant { Type = type };
            short v;
            Assert.Throws<InvalidDataException>(() => v = variant.Short);
        }

        [Test]
        public void Convert_FromUnsupportedType_ToUShort_Throws(
            [Values(
                GraphVariant.ValueType.Unknown, GraphVariant.ValueType.ULong,
                GraphVariant.ValueType.UInt, GraphVariant.ValueType.Short,
                GraphVariant.ValueType.Int, GraphVariant.ValueType.Long,
                GraphVariant.ValueType.Float, GraphVariant.ValueType.Float2,
                GraphVariant.ValueType.Float3, GraphVariant.ValueType.Float4,
                GraphVariant.ValueType.Quaternion, GraphVariant.ValueType.Entity,
                GraphVariant.ValueType.Hash128, GraphVariant.ValueType.Int4)]
            GraphVariant.ValueType type)
        {
            var variant = new GraphVariant { Type = type };
            ushort v;
            Assert.Throws<InvalidDataException>(() => v = variant.UShort);
        }

        [Test]
        public void Convert_FromUnsupportedType_ToFloat2_Throws(
            [Values(
                GraphVariant.ValueType.Unknown, GraphVariant.ValueType.ULong,
                GraphVariant.ValueType.UInt, GraphVariant.ValueType.Short,
                GraphVariant.ValueType.Int, GraphVariant.ValueType.Long,
                GraphVariant.ValueType.UShort, GraphVariant.ValueType.Bool,
                GraphVariant.ValueType.Quaternion, GraphVariant.ValueType.Entity,
                GraphVariant.ValueType.Hash128, GraphVariant.ValueType.Int4)]
            GraphVariant.ValueType type)
        {
            var variant = new GraphVariant { Type = type };
            float2 v;
            Assert.Throws<InvalidDataException>(() => v = variant.Float2);
        }

        [Test]
        public void Convert_FromUnsupportedType_ToFloat3_Throws(
            [Values(
                GraphVariant.ValueType.Unknown, GraphVariant.ValueType.ULong,
                GraphVariant.ValueType.UInt, GraphVariant.ValueType.Short,
                GraphVariant.ValueType.Int, GraphVariant.ValueType.Long,
                GraphVariant.ValueType.UShort, GraphVariant.ValueType.Bool,
                GraphVariant.ValueType.Quaternion, GraphVariant.ValueType.Entity,
                GraphVariant.ValueType.Hash128, GraphVariant.ValueType.Int4)]
            GraphVariant.ValueType type)
        {
            var variant = new GraphVariant { Type = type };
            float3 v;
            Assert.Throws<InvalidDataException>(() => v = variant.Float3);
        }

        [Test]
        public void Convert_FromUnsupportedType_ToFloat4_Throws(
            [Values(
                GraphVariant.ValueType.Unknown, GraphVariant.ValueType.ULong,
                GraphVariant.ValueType.UInt, GraphVariant.ValueType.Short,
                GraphVariant.ValueType.Int, GraphVariant.ValueType.Long,
                GraphVariant.ValueType.UShort, GraphVariant.ValueType.Bool,
                GraphVariant.ValueType.Entity,
                GraphVariant.ValueType.Hash128, GraphVariant.ValueType.Int4)]
            GraphVariant.ValueType type)
        {
            var variant = new GraphVariant { Type = type };
            float4 v;
            Assert.Throws<InvalidDataException>(() => v = variant.Float4);
        }

        [Test]
        public void Convert_FromUnsupportedType_ToQuaternion_Throws(
            [Values(
                GraphVariant.ValueType.Unknown, GraphVariant.ValueType.Bool,
                GraphVariant.ValueType.Int, GraphVariant.ValueType.UInt,
                GraphVariant.ValueType.Short, GraphVariant.ValueType.UShort,
                GraphVariant.ValueType.Long, GraphVariant.ValueType.ULong,
                GraphVariant.ValueType.Float, GraphVariant.ValueType.Float2,
                GraphVariant.ValueType.Float3, GraphVariant.ValueType.Entity,
                GraphVariant.ValueType.Hash128, GraphVariant.ValueType.Int4)]
            GraphVariant.ValueType type)
        {
            var variant = new GraphVariant { Type = type };
            quaternion v;
            Assert.Throws<InvalidDataException>(() => v = variant.Quaternion);
        }

        private static readonly CompareTestData[] FromObjectTestSources =
        {
            new CompareTestData(){ v = GraphVariant.FromObject(8),                                  Compare = (g) => g.Int == 8,                                            Description = "Int" },
            new CompareTestData(){ v = GraphVariant.FromObject(8U),                                 Compare = (g) => g.UInt == 8U,                                          Description = "UInt" },
            new CompareTestData(){ v = GraphVariant.FromObject(8L),                                 Compare = (g) => g.Long == 8L,                                          Description = "Long" },
            new CompareTestData(){ v = GraphVariant.FromObject(8UL),                                Compare = (g) => g.ULong == 8,                                          Description = "ULong" },
            new CompareTestData(){ v = GraphVariant.FromObject((short)8),                           Compare = (g) => g.Short == 8,                                          Description = "Short" },
            new CompareTestData(){ v = GraphVariant.FromObject((ushort)8),                          Compare = (g) => g.UShort == 8,                                         Description = "UShort" },
            new CompareTestData(){ v = GraphVariant.FromObject(8f),                                 Compare = (g) => g.Float == 8f,                                         Description = "Float" },
            new CompareTestData(){ v = GraphVariant.FromObject(true),                               Compare = (g) => g.Bool,                                                Description = "Bool" },
            new CompareTestData(){ v = GraphVariant.FromObject(new float2(2f, 4f)),                 Compare = (g) => g.Float2.Equals(new float2(2f, 4f)),                   Description = "Float2" },
            new CompareTestData(){ v = GraphVariant.FromObject(new float3(2f, 4f, 8f)),             Compare = (g) => g.Float3.Equals(new float3(2f, 4f, 8f)),               Description = "Float3" },
            new CompareTestData(){ v = GraphVariant.FromObject(new float4(2f, 4f, 8f, 16f)),        Compare = (g) => g.Float4.Equals(new float4(2f, 4f, 8f, 16f)),          Description = "Float4" },
            new CompareTestData(){ v = GraphVariant.FromObject(new quaternion(2f, 4f, 8f, 16f)),    Compare = (g) => g.Quaternion.Equals(new quaternion(2f, 4f, 8f, 16f)),  Description = "Quaternion" },
            new CompareTestData(){ v = GraphVariant.FromObject(new Hash128("4fb16d384de56ba44abed9ffe2fc0370")),    Compare = (g) => g.Hash128.Equals(new Hash128("4fb16d384de56ba44abed9ffe2fc0370")),  Description = "Hash128" },
            new CompareTestData(){ v = GraphVariant.FromObject(new int4(-1, 1, -2, 8)),    Compare = (g) => g.Int4.Equals(new int4(-1, 1, -2, 8)),  Description = "Int4" },
        };

        [Test]
        public void FromObject_WithValidType_StoresValidValue([ValueSource("FromObjectTestSources")] CompareTestData test)
        {
            Assert.That(test.Compare(test.v), "FromObject failed with type {0} for test {1}", test.v.Type, test.Description);
        }

        [Test]
        public void FromObject_WithUnsupportedTypes_Throws()
        {
            Assert.Throws<InvalidDataException>(() => GraphVariant.FromObject((byte)8));
        }

        [TestCase(typeof(bool), GraphVariant.ValueType.Bool)]
        [TestCase(typeof(short), GraphVariant.ValueType.Short)]
        [TestCase(typeof(ushort), GraphVariant.ValueType.UShort)]
        [TestCase(typeof(int), GraphVariant.ValueType.Int)]
        [TestCase(typeof(uint), GraphVariant.ValueType.UInt)]
        [TestCase(typeof(long), GraphVariant.ValueType.Long)]
        [TestCase(typeof(ulong), GraphVariant.ValueType.ULong)]
        [TestCase(typeof(float), GraphVariant.ValueType.Float)]
        [TestCase(typeof(float2), GraphVariant.ValueType.Float2)]
        [TestCase(typeof(float3), GraphVariant.ValueType.Float3)]
        [TestCase(typeof(float4), GraphVariant.ValueType.Float4)]
        [TestCase(typeof(quaternion), GraphVariant.ValueType.Quaternion)]
        [TestCase(typeof(Hash128), GraphVariant.ValueType.Hash128)]
        [TestCase(typeof(int4), GraphVariant.ValueType.Int4)]
        public void ValueTypeFromType_WithValidType_ReturnsValidValueType(Type type, GraphVariant.ValueType expectedValueType)
        {
            Assert.AreEqual(expectedValueType, GraphVariant.ValueTypeFromType(type));
        }

        public void ValueTypeFromType_WithUnsupportedType_ReturnsUnknown()
        {
            Assert.That(GraphVariant.ValueTypeFromType(typeof(byte)), Is.EqualTo(GraphVariant.ValueType.Unknown));
        }

        [TestCase(GraphVariant.ValueType.Bool, typeof(bool))]
        [TestCase(GraphVariant.ValueType.Short, typeof(short))]
        [TestCase(GraphVariant.ValueType.UShort, typeof(ushort))]
        [TestCase(GraphVariant.ValueType.Int, typeof(int))]
        [TestCase(GraphVariant.ValueType.UInt, typeof(uint))]
        [TestCase(GraphVariant.ValueType.Long, typeof(long))]
        [TestCase(GraphVariant.ValueType.ULong, typeof(ulong))]
        [TestCase(GraphVariant.ValueType.Float, typeof(float))]
        [TestCase(GraphVariant.ValueType.Float2, typeof(float2))]
        [TestCase(GraphVariant.ValueType.Float3, typeof(float3))]
        [TestCase(GraphVariant.ValueType.Float4, typeof(float4))]
        [TestCase(GraphVariant.ValueType.Quaternion, typeof(quaternion))]
        [TestCase(GraphVariant.ValueType.Entity, typeof(Entity))]
        [TestCase(GraphVariant.ValueType.Hash128, typeof(Hash128))]
        [TestCase(GraphVariant.ValueType.Int4, typeof(int4))]
        public void TypeFromValueType_WithValidValueType_ReturnsValidType(GraphVariant.ValueType valueType, Type expectedType)
        {
            Assert.AreEqual(expectedType, GraphVariant.TypeFromValueType(valueType));
        }

        [Test]
        public void TypeFromValueType_WithValidValueType_ReturnsNotNull([ValueSource("AllValidValueTypes")] GraphVariant.ValueType type)
        {
            Assert.That(GraphVariant.TypeFromValueType(type), Is.Not.Null);
        }

        [Test]
        public void Equals_WithValidValueType_UsingSameType_AreEqual([ValueSource("AllValidValueTypes")] GraphVariant.ValueType type)
        {
            var variant1 = new GraphVariant() { Type = type };
            var variant2 = new GraphVariant() { Type = type };

            Assert.That(variant1.Equals(variant2));
        }

        [Test]
        public void Equals_WithValidValueType_UsingDifferentType_AreNotEqual(
            [ValueSource("AllValidValueTypes")] GraphVariant.ValueType type1,
            [ValueSource("AllValidValueTypes")] GraphVariant.ValueType type2)
        {
            if (type1 != type2)
            {
                var variant1 = new GraphVariant() { Type = type1 };
                var variant2 = new GraphVariant() { Type = type2 };
                Assert.That(!variant1.Equals(variant2));
            }
        }

        [Test]
        public void Equals_WithUnsupportedType_AreNotEqual()
        {
            var variant1 = new GraphVariant() { Type = GraphVariant.ValueType.Bool };
            Assert.That(!variant1.Equals(8 as object));
        }

        private static readonly CompareTestData[] ImplicitOperatorTests =
        {
            new CompareTestData(){ v = 8,                                  Compare = (g) => g.Int == 8,                                            Description = "Int" },
            new CompareTestData(){ v = 8U,                                 Compare = (g) => g.UInt == 8U,                                          Description = "UInt" },
            new CompareTestData(){ v = 8L,                                 Compare = (g) => g.Long == 8L,                                          Description = "Long" },
            new CompareTestData(){ v = 8UL,                                Compare = (g) => g.ULong == 8,                                          Description = "ULong" },
            new CompareTestData(){ v = (short)8,                           Compare = (g) => g.Short == 8,                                          Description = "Short" },
            new CompareTestData(){ v = (ushort)8,                          Compare = (g) => g.UShort == 8,                                         Description = "UShort" },
            new CompareTestData(){ v = 8f,                                 Compare = (g) => g.Float == 8f,                                         Description = "Float" },
            new CompareTestData(){ v = true,                               Compare = (g) => g.Bool,                                                Description = "Bool" },
            new CompareTestData(){ v = new float2(2f, 4f),                 Compare = (g) => g.Float2.Equals(new float2(2f, 4f)),                   Description = "Float2" },
            new CompareTestData(){ v = new float3(2f, 4f, 8f),             Compare = (g) => g.Float3.Equals(new float3(2f, 4f, 8f)),               Description = "Float3" },
            new CompareTestData(){ v = new float4(2f, 4f, 8f, 16f),        Compare = (g) => g.Float4.Equals(new float4(2f, 4f, 8f, 16f)),          Description = "Float4" },
            new CompareTestData(){ v = new quaternion(2f, 4f, 8f, 16f),    Compare = (g) => g.Quaternion.Equals(new quaternion(2f, 4f, 8f, 16f)),  Description = "Quaternion" },
            new CompareTestData(){ v = new Hash128("4fb16d384de56ba44abed9ffe2fc0370"),    Compare = (g) => g.Hash128.Equals(new Hash128("4fb16d384de56ba44abed9ffe2fc0370")),  Description = "Hash128" },
            new CompareTestData(){ v = new int4(-1, 1, -2, 8),    Compare = (g) => g.Int4.Equals(new int4(-1, 1, -2, 8)),  Description = "Int4" },
        };

        [Test]
        public void ImplicitOperator_WithValidValueType_CreatesValidVariant(
            [ValueSource("ImplicitOperatorTests")] CompareTestData test)
        {
            Assert.That(test.Compare(test.v), "Implicit operator failed with type {0} for test {1}", test.v.Type, test.Description);
        }

        [Test]
        public void GetHashCode_WithSameValueAndDifferentType_AreNotEqual()
        {
            var variant1 = new GraphVariant() { Type = GraphVariant.ValueType.Float, Float = 4F };
            var variant2 = variant1;
            variant2.Type = GraphVariant.ValueType.Int;
            Assert.AreNotEqual(variant1.GetHashCode(), variant2.GetHashCode());
        }

        [Test]
        public void GetHashCode_WithDifferentValueAndSameType_AreNotEqual()
        {
            var variant1 = new GraphVariant() { Type = GraphVariant.ValueType.Float, Float = 4F };
            var variant2 = new GraphVariant() { Type = GraphVariant.ValueType.Float, Float = 2F };
            Assert.AreNotEqual(variant1.GetHashCode(), variant2.GetHashCode());
        }

        [Test]
        public void GetHashCode_WithSameValueAndSameType_AreEqual()
        {
            var variant1 = new GraphVariant() { Type = GraphVariant.ValueType.Float, Float = 4F };
            var variant2 = new GraphVariant() { Type = GraphVariant.ValueType.Float, Float = 4F };
            Assert.AreEqual(variant1.GetHashCode(), variant2.GetHashCode());
        }
    }
}
