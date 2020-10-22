using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Unity.PerformanceTesting;

namespace Unity.Animation.PerformanceTests
{
    [Category("Performance"), Category("Animation")]
    public class RigBuilderPerformanceTests
    {
        [Performance]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(700)]
        [TestCase(1000)]
        public void BuildRigs(int boneCount)
        {
            var skeleton = new SkeletonNode[boneCount];

            skeleton[0] = new SkeletonNode { Id = "Root", ParentIndex = -1, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1) };
            for (int i = 1; i < boneCount; i++)
            {
                skeleton[i] = new SkeletonNode { Id = $"{i}", ParentIndex = i - 1, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1) };
            }

            Measure.Method(
                () => RigBuilder.CreateRigDefinition(skeleton)
            )
                .SampleGroup("CreateRigDefinition")
                .WarmupCount(100)
                .MeasurementCount(500)
                .Run();
        }

        [Performance]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(700)]
        [TestCase(1000)]
        public void BuildRigs_FromData(int boneCount)
        {
            var data = new RigBuilderData(Allocator.Persistent);
            data.SkeletonNodes.Capacity = boneCount;

            data.SkeletonNodes.Add(new SkeletonNode { Id = "Root", ParentIndex = -1, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1) });
            for (int i = 1; i < boneCount; i++)
            {
                data.SkeletonNodes.Add(new SkeletonNode { Id = $"{i}", ParentIndex = i - 1, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1) });
            }

            Measure.Method(
                () => RigBuilder.CreateRigDefinition(data)
            )
                .SampleGroup("CreateRigDefinition")
                .WarmupCount(100)
                .MeasurementCount(500)
                .Run();

            data.Dispose();
        }

        [Performance]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(700)]
        [TestCase(1000)]
        public void JobifiedBuildRigs(int boneCount)
        {
            var data = new RigBuilderData(Allocator.Persistent);
            data.SkeletonNodes.Capacity = boneCount;

            data.SkeletonNodes.Add(new SkeletonNode { Id = "Root", ParentIndex = -1, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1) });
            for (int i = 1; i < boneCount; i++)
            {
                data.SkeletonNodes.Add(new SkeletonNode { Id = $"{i}", ParentIndex = i - 1, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1) });
            }

            Measure.Method(
                () => RigBuilder.RunCreateRigDefinitionJob(data)
            )
                .SampleGroup("CreateRigDefinition")
                .WarmupCount(100)
                .MeasurementCount(500)
                .Run();

            data.Dispose();
        }
    }
}
