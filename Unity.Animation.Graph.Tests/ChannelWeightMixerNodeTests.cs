using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.TestTools;

namespace Unity.Animation.Tests
{
    public class ChannelWeightMixerNodeTests : AnimationTestsFixture, IPrebuildSetup
    {
        private float3 m_ClipRootLocalTranslation1 => new float3(100.0f, 0.0f, 0.0f);
        private quaternion m_ClipRootLocalRotation1 => quaternion.RotateX(math.radians(90.0f));
        private float3 m_ClipRootLocalScale1 => new float3(10.0f, 1.0f, 1.0f);
        private int m_ClipRootInt1 => 1;

        private float3 m_ClipChildLocalTranslation1 => new float3(0.0f, 100.0f, 0.0f);
        private quaternion m_ClipChildLocalRotation1 => quaternion.RotateY(math.radians(90.0f));
        private float3 m_ClipChildLocalScale1 => new float3(1.0f, 1.0f, 10.0f);

        private float3 m_ClipRootLocalTranslation2 => new float3(0.0f, 100.0f, 0.0f);
        private quaternion m_ClipRootLocalRotation2 => quaternion.RotateY(math.radians(90.0f));
        private float3 m_ClipRootLocalScale2 => new float3(1.0f, 1.0f, 10.0f);
        private int m_ClipRootInt2 => 4;

        private float3 m_ClipChildLocalTranslation2 => new float3(100.0f, 0.0f, 0.0f);
        private quaternion m_ClipChildLocalRotation2 => quaternion.RotateX(math.radians(90.0f));
        private float3 m_ClipChildLocalScale2 => new float3(1.0f, 10.0f, 1.0f);

        private Rig m_Rig;
        private BlobAssetReference<Clip> m_ConstantRootClip1;
        private BlobAssetReference<Clip> m_ConstantRootClip2;
        private BlobAssetReference<Clip> m_ConstantHierarchyClip1;
        private BlobAssetReference<Clip> m_ConstantHierarchyClip2;

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

            var animationChannels = new IAnimationChannel[]
            {
                new IntChannel { DefaultValue = 0, Id = new StringHash("Int1") }
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannels);
        }

        public void Setup()
        {
#if UNITY_EDITOR
            PlaymodeTestsEditorSetup.CreateStreamingAssetsDirectory();

            var constantRootClip1 = CreateConstantDenseClip(
                new[] { ("Root", m_ClipRootLocalTranslation1) },
                new[] { ("Root", m_ClipRootLocalRotation1) },
                new[] { ("Root", m_ClipRootLocalScale1) },
                new(string, float)[0],
                new[] {("Int1", m_ClipRootInt1)});

            var blobPath = "ChannelWeightMixerNodeTestsConstantRootClip1.blob";
            BlobFile.WriteBlobAsset(ref constantRootClip1, blobPath);

            var constantHierarchyClip1 = CreateConstantDenseClip(
                new[] { ("Root", m_ClipRootLocalTranslation1), ("Child1", m_ClipChildLocalTranslation1) },
                new[] { ("Root", m_ClipRootLocalRotation1), ("Child1", m_ClipChildLocalRotation1) },
                new[] { ("Root", m_ClipRootLocalScale1), ("Child1", m_ClipChildLocalScale1) },
                new(string, float)[0],
                new[] {("Int1", m_ClipRootInt1)});

            blobPath = "ChannelWeightMixerNodeTestsConstantHierarchyClip1.blob";
            BlobFile.WriteBlobAsset(ref constantHierarchyClip1, blobPath);


            var constantRootClip2 = CreateConstantDenseClip(
                new[] { ("Root", m_ClipRootLocalTranslation2) },
                new[] { ("Root", m_ClipRootLocalRotation2) },
                new[] { ("Root", m_ClipRootLocalScale2) },
                new(string, float)[0],
                new[] {("Int1", m_ClipRootInt2)});

            blobPath = "ChannelWeightMixerNodeTestsConstantRootClip2.blob";
            BlobFile.WriteBlobAsset(ref constantRootClip2, blobPath);

            var constantHierarchyClip2 = CreateConstantDenseClip(
                new[] { ("Root", m_ClipRootLocalTranslation2), ("Child1", m_ClipChildLocalTranslation2) },
                new[] { ("Root", m_ClipRootLocalRotation2), ("Child1", m_ClipChildLocalRotation2) },
                new[] { ("Root", m_ClipRootLocalScale2), ("Child1", m_ClipChildLocalScale2) },
                new(string, float)[0],
                new[] {("Int1", m_ClipRootInt2)});

            blobPath = "ChannelWeightMixerNodeTestsConstantHierarchyClip2.blob";
            BlobFile.WriteBlobAsset(ref constantHierarchyClip2, blobPath);
#endif
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // Create rig
            m_Rig = new Rig { Value = CreateTestRigDefinition() };

            // Clip #1
            {
                // Constant root clip
                {
                    var path = "ChannelWeightMixerNodeTestsConstantRootClip1.blob";
                    m_ConstantRootClip1 = BlobFile.ReadBlobAsset<Clip>(path);

                    ClipManager.Instance.GetClipFor(m_Rig, m_ConstantRootClip1);
                }

                // Constant hierarchy clip
                {
                    var path = "ChannelWeightMixerNodeTestsConstantHierarchyClip1.blob";
                    m_ConstantHierarchyClip1 = BlobFile.ReadBlobAsset<Clip>(path);

                    ClipManager.Instance.GetClipFor(m_Rig, m_ConstantHierarchyClip1);
                }
            }

            // Clip #2
            {
                // Constant root clip
                {
                    var path = "ChannelWeightMixerNodeTestsConstantRootClip2.blob";
                    m_ConstantRootClip2 = BlobFile.ReadBlobAsset<Clip>(path);

                    ClipManager.Instance.GetClipFor(m_Rig, m_ConstantRootClip2);
                }

                // Constant hierarchy clip
                {
                    var path = "ChannelWeightMixerNodeTestsConstantHierarchyClip2.blob";
                    m_ConstantHierarchyClip2 = BlobFile.ReadBlobAsset<Clip>(path);

                    ClipManager.Instance.GetClipFor(m_Rig, m_ConstantHierarchyClip2);
                }
            }
        }

        [TestCase(0.0f, 0.5f, "Root", 0.8f)]
        [TestCase(0.3f, 0.9f, "Root", 0.1f)]
        [TestCase(1.0f, 0.0f, "Root", 1.0f)]
        [TestCase(1.0f, 0.0f, "Child1", 1.0f)]
        [TestCase(1.0f, 0.0f, "Int1", 0.8f)]
        [TestCase(1.0f, 0.0f, "Int1", 0.1f)]
        [TestCase(1.0f, 0.0f, "Int1", 0.5f)]
        public void ChannelWeightMix2SimpleClips(float weight, float defaultWeight, string channelId, float channelWeight)
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, Entity.Null);

            var clipNode1 = CreateNode<ClipNode>();
            Set.SendMessage(clipNode1, ClipNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(clipNode1, ClipNode.SimulationPorts.Clip, m_ConstantRootClip1);

            var clipNode2 = CreateNode<ClipNode>();
            Set.SendMessage(clipNode2, ClipNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(clipNode2, ClipNode.SimulationPorts.Clip, m_ConstantRootClip2);

            // Find channels from channel ID
            var channel = new ChannelWeightMap() { Id = channelId, Weight = channelWeight };
            var featherBlendQuery = new ChannelWeightQuery();
            featherBlendQuery.Channels = new ChannelWeightMap[] { channel };
            var table = featherBlendQuery.ToChannelWeightTable(m_Rig);
            var weightLength = (ushort)table.Value.Weights.Length;

            var weightNode = CreateNode<WeightBuilderNode>();
            Set.SendMessage(weightNode, WeightBuilderNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(weightNode, WeightBuilderNode.KernelPorts.DefaultWeight, defaultWeight);
            Set.SetPortArraySize(weightNode, WeightBuilderNode.KernelPorts.ChannelIndices, weightLength);
            Set.SetPortArraySize(weightNode, WeightBuilderNode.KernelPorts.ChannelWeights, weightLength);
            for (ushort i = 0; i < weightLength; ++i)
            {
                var w = table.Value.Weights[i];
                Set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelIndices, i, w.Index);
                Set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelWeights, i, w.Weight);
            }

            // Connect to mixer node
            var mixerNode = CreateNode<ChannelWeightMixerNode>();
            Set.Connect(clipNode1, ClipNode.KernelPorts.Output, mixerNode, ChannelWeightMixerNode.KernelPorts.Input0);
            Set.Connect(clipNode2, ClipNode.KernelPorts.Output, mixerNode, ChannelWeightMixerNode.KernelPorts.Input1);
            Set.Connect(weightNode, WeightBuilderNode.KernelPorts.Output, mixerNode, ChannelWeightMixerNode.KernelPorts.WeightMasks);
            Set.SetData(mixerNode, ChannelWeightMixerNode.KernelPorts.Weight, weight);
            Set.SendMessage(mixerNode, ChannelWeightMixerNode.SimulationPorts.Rig, m_Rig);

            var entityNode = CreateComponentNode(entity);
            Set.Connect(mixerNode, ChannelWeightMixerNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            var blendWeight = weight * (channelId == "Root" ? channelWeight : defaultWeight);
            var expectedLocalTranslation = math.lerp(m_ClipRootLocalTranslation1, m_ClipRootLocalTranslation2, blendWeight);
            var expectedLocalRotation = mathex.lerp(m_ClipRootLocalRotation1, m_ClipRootLocalRotation2, blendWeight);
            var expectedLocalScale = math.lerp(m_ClipRootLocalScale1, m_ClipRootLocalScale2, blendWeight);
            var intBlendWeight = weight * (channelId == "Int1" ? channelWeight : defaultWeight);
            var expectedInt = math.select(m_ClipRootInt1, m_ClipRootInt2, intBlendWeight > 0.5f);

            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(0), Is.EqualTo(expectedLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
            Assert.That(streamECS.GetInt(0), Is.EqualTo(expectedInt));
        }

        [TestCase(0.0f, 0.5f, "Child1", 0.8f)]
        [TestCase(0.3f, 0.9f, "Child1", 0.1f)]
        [TestCase(1.0f, 0.0f, "Child1", 1.0f)]
        [TestCase(1.0f, 0.0f, "Root", 1.0f)]
        [TestCase(1.0f, 0.0f, "Int1", 0.8f)]
        [TestCase(1.0f, 0.0f, "Int1", 0.1f)]
        [TestCase(1.0f, 0.0f, "Int1", 0.5f)]
        public void ChannelWeightMix2ClipsWithHierarchy(float weight, float defaultWeight, string channelId, float channelWeight)
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, Entity.Null);

            var clipNode1 = CreateNode<ClipNode>();
            Set.SendMessage(clipNode1, ClipNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(clipNode1, ClipNode.SimulationPorts.Clip, m_ConstantHierarchyClip1);

            var clipNode2 = CreateNode<ClipNode>();
            Set.SendMessage(clipNode2, ClipNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(clipNode2, ClipNode.SimulationPorts.Clip, m_ConstantHierarchyClip2);

            // Find channels from channel ID
            var channel = new ChannelWeightMap() { Id = channelId, Weight = channelWeight };
            var featherBlendQuery = new ChannelWeightQuery();
            featherBlendQuery.Channels = new ChannelWeightMap[] { channel };
            var table = featherBlendQuery.ToChannelWeightTable(m_Rig);
            var weightLength = (ushort)table.Value.Weights.Length;

            var weightNode = CreateNode<WeightBuilderNode>();
            Set.SendMessage(weightNode, WeightBuilderNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(weightNode, WeightBuilderNode.KernelPorts.DefaultWeight, defaultWeight);
            Set.SetPortArraySize(weightNode, WeightBuilderNode.KernelPorts.ChannelIndices, weightLength);
            Set.SetPortArraySize(weightNode, WeightBuilderNode.KernelPorts.ChannelWeights, weightLength);
            for (ushort i = 0; i < weightLength; ++i)
            {
                var w = table.Value.Weights[i];
                Set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelIndices, i, w.Index);
                Set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelWeights, i, w.Weight);
            }

            // Connect to mixer node
            var mixerNode = CreateNode<ChannelWeightMixerNode>();
            Set.Connect(clipNode1, ClipNode.KernelPorts.Output, mixerNode, ChannelWeightMixerNode.KernelPorts.Input0);
            Set.Connect(clipNode2, ClipNode.KernelPorts.Output, mixerNode, ChannelWeightMixerNode.KernelPorts.Input1);
            Set.Connect(weightNode, WeightBuilderNode.KernelPorts.Output, mixerNode, ChannelWeightMixerNode.KernelPorts.WeightMasks);
            Set.SetData(mixerNode, ChannelWeightMixerNode.KernelPorts.Weight, weight);
            Set.SendMessage(mixerNode, ChannelWeightMixerNode.SimulationPorts.Rig, m_Rig);

            var entityNode = CreateComponentNode(entity);
            Set.Connect(mixerNode, ChannelWeightMixerNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            var blendWeight = weight * (channelId == "Child1" ? channelWeight : defaultWeight);
            var expectedLocalTranslation = math.lerp(m_ClipChildLocalTranslation1, m_ClipChildLocalTranslation2, blendWeight);
            var expectedLocalRotation = mathex.lerp(m_ClipChildLocalRotation1, m_ClipChildLocalRotation2, blendWeight);
            var expectedLocalScale = math.lerp(m_ClipChildLocalScale1, m_ClipChildLocalScale2, blendWeight);
            var intBlendWeight = weight * (channelId == "Int1" ? channelWeight : defaultWeight);
            var expectedInt = math.select(m_ClipRootInt1, m_ClipRootInt2, intBlendWeight > 0.5f);

            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
            Assert.That(streamECS.GetInt(0), Is.EqualTo(expectedInt));
        }

        [Test]
        public void ChannelWeightMixerOutputsDefaultValuesWhenNoInputConnected()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode
                {
                    ParentIndex = -1, Id = "Root", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(1, 2, 3),
                    LocalRotationDefaultValue = new quaternion(1, 0, 0, 0),
                    LocalScaleDefaultValue = new float3(2, 2, 2),
                },
                new SkeletonNode
                {
                    ParentIndex = 0, Id = "Child1", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(4, 5, 6),
                    LocalRotationDefaultValue = new quaternion(0, 1, 0, 0),
                    LocalScaleDefaultValue = new float3(3, 3, 3),
                },
                new SkeletonNode
                {
                    ParentIndex = 0, Id = "Child2", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(7, 8, 9),
                    LocalRotationDefaultValue = new quaternion(0, 0, 1, 0),
                    LocalScaleDefaultValue = new float3(4, 4, 4),
                }
            };

            var animationChannels = new IAnimationChannel[]
            {
                new IntChannel { DefaultValue = 5, Id = new StringHash("Int1") }
            };

            var rig = new Rig { Value = RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannels) };

            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, Entity.Null);

            var mixerNode = CreateNode<ChannelWeightMixerNode>();
            Set.SendMessage(mixerNode, ChannelWeightMixerNode.SimulationPorts.Rig, rig);

            var weightNode = CreateNode<WeightBuilderNode>();
            Set.SendMessage(weightNode, WeightBuilderNode.SimulationPorts.Rig, rig);
            Set.Connect(weightNode, WeightBuilderNode.KernelPorts.Output, mixerNode, ChannelWeightMixerNode.KernelPorts.WeightMasks);

            var entityNode = CreateComponentNode(entity);
            Set.Connect(mixerNode, ChannelWeightMixerNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            for (var i = 0; i < skeletonNodes.Length; ++i)
            {
                Assert.That(streamECS.GetLocalToParentTranslation(i), Is.EqualTo(skeletonNodes[i].LocalTranslationDefaultValue).Using(TranslationComparer));
                Assert.That(streamECS.GetLocalToParentRotation(i), Is.EqualTo(skeletonNodes[i].LocalRotationDefaultValue).Using(RotationComparer));
                Assert.That(streamECS.GetLocalToParentScale(i), Is.EqualTo(skeletonNodes[i].LocalScaleDefaultValue).Using(ScaleComparer));
            }

            Assert.That(streamECS.GetInt(0), Is.EqualTo(((IntChannel)animationChannels[0]).DefaultValue));
        }

        [TestCase(0.0f, 0.5f, "Child1", 0.8f)]
        [TestCase(0.3f, 0.9f, "Child1", 0.1f)]
        [TestCase(1.0f, 0.0f, "Child1", 1.0f)]
        [TestCase(1.0f, 0.0f, "Root", 1.0f)]
        [TestCase(1.0f, 0.0f, "Int1", 0.8f)]
        [TestCase(1.0f, 0.0f, "Int1", 0.1f)]
        [TestCase(1.0f, 0.0f, "Int1", 0.5f)]
        public void ChannelMixerWithInput0NotConnectedReturnsMixBetweenBindPoseAndClip(float weight, float defaultWeight, string channelId, float channelWeight)
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, Entity.Null);

            var clipNode2 = CreateNode<ClipNode>();
            Set.SendMessage(clipNode2, ClipNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(clipNode2, ClipNode.SimulationPorts.Clip, m_ConstantHierarchyClip2);

            // Find channels from channel ID
            var channel = new ChannelWeightMap() { Id = channelId, Weight = channelWeight };
            var featherBlendQuery = new ChannelWeightQuery();
            featherBlendQuery.Channels = new ChannelWeightMap[] { channel };
            var table = featherBlendQuery.ToChannelWeightTable(m_Rig);
            var weightLength = (ushort)table.Value.Weights.Length;

            var weightNode = CreateNode<WeightBuilderNode>();
            Set.SendMessage(weightNode, WeightBuilderNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(weightNode, WeightBuilderNode.KernelPorts.DefaultWeight, defaultWeight);
            Set.SetPortArraySize(weightNode, WeightBuilderNode.KernelPorts.ChannelIndices, weightLength);
            Set.SetPortArraySize(weightNode, WeightBuilderNode.KernelPorts.ChannelWeights, weightLength);
            for (ushort i = 0; i < weightLength; ++i)
            {
                var w = table.Value.Weights[i];
                Set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelIndices, i, w.Index);
                Set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelWeights, i, w.Weight);
            }

            // Connect to mixer node
            var mixerNode = CreateNode<ChannelWeightMixerNode>();
            Set.Connect(clipNode2, ClipNode.KernelPorts.Output, mixerNode, ChannelWeightMixerNode.KernelPorts.Input1);
            Set.Connect(weightNode, WeightBuilderNode.KernelPorts.Output, mixerNode, ChannelWeightMixerNode.KernelPorts.WeightMasks);
            Set.SetData(mixerNode, ChannelWeightMixerNode.KernelPorts.Weight, weight);
            Set.SendMessage(mixerNode, ChannelWeightMixerNode.SimulationPorts.Rig, m_Rig);

            var entityNode = CreateComponentNode(entity);
            Set.Connect(mixerNode, ChannelWeightMixerNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            var blendWeight = weight * (channelId == "Child1" ? channelWeight : defaultWeight);
            var expectedLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation2, blendWeight);
            var expectedLocalRotation = mathex.lerp(quaternion.identity, m_ClipChildLocalRotation2, blendWeight);
            var expectedLocalScale = math.lerp(new float3(1), m_ClipChildLocalScale2, blendWeight);
            var intBlendWeight = weight * (channelId == "Int1" ? channelWeight : defaultWeight);
            var expectedInt = math.select(0, m_ClipRootInt2, intBlendWeight > 0.5f);

            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
            Assert.That(streamECS.GetInt(0), Is.EqualTo(expectedInt));
        }

        [TestCase(0.0f, 0.5f, "Child1", 0.8f)]
        [TestCase(0.3f, 0.9f, "Child1", 0.1f)]
        [TestCase(1.0f, 0.0f, "Child1", 1.0f)]
        [TestCase(1.0f, 0.0f, "Root", 1.0f)]
        [TestCase(1.0f, 0.0f, "Int1", 0.8f)]
        [TestCase(1.0f, 0.0f, "Int1", 0.1f)]
        [TestCase(1.0f, 0.0f, "Int1", 0.5f)]
        public void ChannelMixerWithInput1NotConnectedReturnsMixBetweenBindPoseAndClip(float weight, float defaultWeight, string channelId, float channelWeight)
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, Entity.Null);

            var clipNode1 = CreateNode<ClipNode>();
            Set.SendMessage(clipNode1, ClipNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(clipNode1, ClipNode.SimulationPorts.Clip, m_ConstantHierarchyClip1);

            // Find channels from channel ID
            var channel = new ChannelWeightMap() { Id = channelId, Weight = channelWeight };
            var featherBlendQuery = new ChannelWeightQuery();
            featherBlendQuery.Channels = new ChannelWeightMap[] { channel };
            var table = featherBlendQuery.ToChannelWeightTable(m_Rig);
            var weightLength = (ushort)table.Value.Weights.Length;

            var weightNode = CreateNode<WeightBuilderNode>();
            Set.SendMessage(weightNode, WeightBuilderNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(weightNode, WeightBuilderNode.KernelPorts.DefaultWeight, defaultWeight);
            Set.SetPortArraySize(weightNode, WeightBuilderNode.KernelPorts.ChannelIndices, weightLength);
            Set.SetPortArraySize(weightNode, WeightBuilderNode.KernelPorts.ChannelWeights, weightLength);
            for (ushort i = 0; i < weightLength; ++i)
            {
                var w = table.Value.Weights[i];
                Set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelIndices, i, w.Index);
                Set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelWeights, i, w.Weight);
            }

            // Connect to mixer node
            var mixerNode = CreateNode<ChannelWeightMixerNode>();
            Set.Connect(clipNode1, ClipNode.KernelPorts.Output, mixerNode, ChannelWeightMixerNode.KernelPorts.Input0);
            Set.Connect(weightNode, WeightBuilderNode.KernelPorts.Output, mixerNode, ChannelWeightMixerNode.KernelPorts.WeightMasks);
            Set.SetData(mixerNode, ChannelWeightMixerNode.KernelPorts.Weight, weight);
            Set.SendMessage(mixerNode, ChannelWeightMixerNode.SimulationPorts.Rig, m_Rig);

            var entityNode = CreateComponentNode(entity);
            Set.Connect(mixerNode, ChannelWeightMixerNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            var blendWeight = weight * (channelId == "Child1" ? channelWeight : defaultWeight);
            var expectedLocalTranslation = math.lerp(m_ClipChildLocalTranslation1, float3.zero, blendWeight);
            var expectedLocalRotation = mathex.lerp(m_ClipChildLocalRotation1, quaternion.identity, blendWeight);
            var expectedLocalScale = math.lerp(m_ClipChildLocalScale1, new float3(1), blendWeight);
            var intBlendWeight = weight * (channelId == "Int1" ? channelWeight : defaultWeight);
            var expectedInt = math.select(m_ClipRootInt1, 0, intBlendWeight > 0.5f);

            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
            Assert.That(streamECS.GetInt(0), Is.EqualTo(expectedInt));
        }
    }
}
