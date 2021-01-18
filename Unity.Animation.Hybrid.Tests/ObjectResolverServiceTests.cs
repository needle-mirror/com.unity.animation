using System;
using System.Reflection;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace Unity.Animation.Hybrid.Tests
{
    public class ObjectResolverServiceTests
    {
        struct IntConvertibleObject : IConvertibleObject<int>
        {
            public int Value { get; set; }
        }

        struct FloatConvertibleObject : IConvertibleObject<float>
        {
            public float Value { get; set; }
        }

        class IntObjectResolver : IObjectResolver
        {
            public Type Type => typeof(IntConvertibleObject);

            public GraphVariant ResolveValue(UnityEngine.Object objectReference, Component context)
            {
                return 2;
            }
        }

        class FloatObjectResolver : IObjectResolver
        {
            public Type Type => typeof(FloatConvertibleObject);

            public GraphVariant ResolveValue(UnityEngine.Object objectReference, Component context)
            {
                return 2f;
            }
        }

        [Test]
        public void DetectAvailableObjectResolvers()
        {
            ObjectResolverService.CachedAssemblies = new[] {Assembly.Load("Unity.Animation.Hybrid.Tests")};
            Assert.AreEqual(2, ObjectResolverService.AvailableResolvers.Count);
            Assert.IsTrue(
                ObjectResolverService.AvailableResolvers.ContainsKey(
                    TypeHash.CalculateStableTypeHash(typeof(IntConvertibleObject))));
            Assert.IsTrue(
                ObjectResolverService.AvailableResolvers.ContainsKey(
                    TypeHash.CalculateStableTypeHash(typeof(FloatConvertibleObject))));
            ObjectResolverService.CachedAssemblies = null;
        }

        [Test]
        public void ResolveExpectedValues()
        {
            ObjectResolverService.CachedAssemblies = new[] {Assembly.Load("Unity.Animation.Hybrid.Tests")};
            var resolvedValue =
                ObjectResolverService.ResolveObjectReference(
                    TypeHash.CalculateStableTypeHash(typeof(IntConvertibleObject)), null, null);
            Assert.AreEqual(2, resolvedValue.Int);
            resolvedValue =
                ObjectResolverService.ResolveObjectReference(
                    TypeHash.CalculateStableTypeHash(typeof(FloatConvertibleObject)), null, null);
            Assert.AreEqual(2f, resolvedValue.Float);
            ObjectResolverService.CachedAssemblies = null;
        }
    }
}
