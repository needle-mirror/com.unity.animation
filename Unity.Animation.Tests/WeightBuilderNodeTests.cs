using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation.Tests
{
    public class WeightBuilderNodeTests : AnimationTestsFixture
    {
        private Rig m_Rig;
        private static BlobAssetReference<RigDefinition> CreateTestRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode
                {
                    ParentIndex = -1, Id = "Root", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(0, 0, 0),
                    LocalRotationDefaultValue = new quaternion(0, 0, 0, 1),
                    LocalScaleDefaultValue = new float3(1, 1, 1),
                },
                new SkeletonNode
                {
                    ParentIndex = 0, Id = "Child1", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(0, 0, 0),
                    LocalRotationDefaultValue = new quaternion(0, 0, 0, 1),
                    LocalScaleDefaultValue = new float3(1, 1, 1),
                },
                new SkeletonNode
                {
                    ParentIndex = 0, Id = "Child2", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(0, 0, 0),
                    LocalRotationDefaultValue = new quaternion(0, 0, 0, 1),
                    LocalScaleDefaultValue = new float3(1, 1, 1),
                }
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes);
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // Create rig
            m_Rig = new Rig { Value = CreateTestRigDefinition() };
        }

        [Test]
        public void WeightBuilderNodeOutputHasRigSize()
        {
            var node = CreateNode<WeightBuilderNode>();
            Set.SendMessage(node, WeightBuilderNode.SimulationPorts.Rig, m_Rig);

            var output = Set.CreateGraphValue(node, WeightBuilderNode.KernelPorts.Output);
            Set.Update(default);

            var resolver = Set.GetGraphValueResolver(out var valueResolverDeps);
            valueResolverDeps.Complete();
            var result = resolver.Resolve(output);

            Assert.That(result.Length, Is.EqualTo(Core.WeightDataSize(m_Rig.Value)));

            Set.ReleaseGraphValue(output);
        }

        [TestCase(-0.5f)]
        [TestCase(0.3f)]
        [TestCase(1.5f)]
        public void WeightBuilderNodeReturnsDefaultWeight(float defaultWeight)
        {
            var node = CreateNode<WeightBuilderNode>();
            Set.SendMessage(node, WeightBuilderNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(node, WeightBuilderNode.KernelPorts.DefaultWeight, defaultWeight);

            var output = Set.CreateGraphValue(node, WeightBuilderNode.KernelPorts.Output);
            Set.Update(default);

            var resolver = Set.GetGraphValueResolver(out var valueResolverDeps);
            valueResolverDeps.Complete();
            var result = resolver.Resolve(output);

            for (int i = 0; i < result.Length; ++i)
            {
                Assert.That(result[i].Value, Is.EqualTo(defaultWeight).Using(FloatComparer));
            }

            Set.ReleaseGraphValue(output);
        }

        [TestCase(0.5f, 0, 0.8f)]
        [TestCase(0.9f, 6, 0.1f)]
        [TestCase(0.0f, 3, 1.0f)]
        public void WeightBuilderNodeSetsChannelWeight(float defaultWeight, int channelIndex, float channelWeight)
        {
            var node = CreateNode<WeightBuilderNode>();
            Set.SendMessage(node, WeightBuilderNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(node, WeightBuilderNode.KernelPorts.DefaultWeight, defaultWeight);

            Set.SetPortArraySize(node, WeightBuilderNode.KernelPorts.ChannelIndices, 1);
            Set.SetPortArraySize(node, WeightBuilderNode.KernelPorts.ChannelWeights, 1);
            Set.SetData(node, WeightBuilderNode.KernelPorts.ChannelIndices, 0, channelIndex);
            Set.SetData(node, WeightBuilderNode.KernelPorts.ChannelWeights, 0, channelWeight);

            var output = Set.CreateGraphValue(node, WeightBuilderNode.KernelPorts.Output);
            Set.Update(default);

            var resolver = Set.GetGraphValueResolver(out var valueResolverDeps);
            valueResolverDeps.Complete();
            var result = resolver.Resolve(output);

            var channelIndices = new NativeArray<int>(1, Allocator.Temp);
            channelIndices[0] = channelIndex;
            var channelWeights = new NativeArray<float>(1, Allocator.Temp);
            channelWeights[0] = channelWeight;
            var weightData = new NativeArray<WeightData>(Core.WeightDataSize(m_Rig), Allocator.Temp);

            Core.ComputeWeightDataFromChannelIndices(m_Rig, defaultWeight,
                channelIndices, channelWeights, weightData);

            for (var i = 0; i < weightData.Length; ++i)
            {
                Assert.That(result[i].Value, Is.EqualTo(weightData[i].Value).Using(FloatComparer));
            }

            Set.ReleaseGraphValue(output);
        }
    }
}
