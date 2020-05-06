using NUnit.Framework;

using Unity.Mathematics;
using Unity.Animation.Hybrid;
using UnityEngine;

using Unity.Animation.Tests;
using Unity.PerformanceTesting;

namespace Unity.Animation.PerformanceTests
{
    [Category("performance"), Category("animation")]
    public class ClipInstanceBuilderPerformanceTests : AnimationTestsFixture
    {
        [Performance]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(800)]
        [TestCase(1600)]
        public void CreateClipInstance(int boneCount)
        {
            var skeleton = new SkeletonNode[boneCount];
            skeleton[0] = new SkeletonNode { Id = "Root", ParentIndex = -1, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1) };

            var clip = new AnimationClip();
            for (int i = 1; i < boneCount; ++i)
            {
                skeleton[i] = new SkeletonNode { Id = $"{i}", ParentIndex = i - 1, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1) };
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalPosition.x", GetConstantCurve(1f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalPosition.y", GetConstantCurve(2f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalPosition.z", GetConstantCurve(3f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalRotation.x", GetConstantCurve(0f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalRotation.y", GetConstantCurve(0.5f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalRotation.z", GetConstantCurve(0f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalRotation.w", GetConstantCurve(0.5f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalScale.x", GetConstantCurve(1f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalScale.y", GetConstantCurve(1f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalScale.z", GetConstantCurve(1f));
            }
            ;

            var rigDefintion = RigBuilder.CreateRigDefinition(skeleton);
            var denseClip = clip.ToDenseClip();

            Measure.Method(
                () => ClipInstanceBuilder.Create(rigDefintion, denseClip)
            )
                .SampleGroup("ClipInstanceBuilder.Create")
                .WarmupCount(100)
                .MeasurementCount(500)
                .Run();
        }
    }
}
