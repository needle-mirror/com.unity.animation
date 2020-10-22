using NUnit.Framework;
using Unity.Mathematics;
using Unity.Entities;

namespace Unity.Animation.Tests
{
    public class RigRemapperNodeTests : AnimationTestsFixture
    {
        readonly float3      m_ExpectedSourceTranslation = new float3(10.0f, 0.0f, 0.0f);
        readonly quaternion  m_ExpectedSourceRotation = new quaternion(1.0f, 0.0f, 0.0f, 0.0f);
        readonly float3      m_ExpectedSourceScale = new float3(10.0f, 1.0f, 1.0f);
        readonly float       m_ExpectedSourceFloat = 10.0f;
        readonly int         m_ExpectedSourceInt = 10;

        readonly float3      m_ExpectedDestinationTranslation = new float3(0.0f, 0.0f, 5.0f);
        readonly quaternion  m_ExpectedDestinationRotation = new quaternion(0.0f, 0.0f, 1.0f, 0.0f);
        readonly float3      m_ExpectedDestinationScale = new float3(1.0f, 1.0f, 5.0f);
        readonly float       m_ExpectedDestinationFloat = 5.0f;
        readonly int         m_ExpectedDestinationInt = 5;

        [Test]
        public void CanSetSourceRigDefinition()
        {
            var channels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "Root",   DefaultValue = float3.zero },
                new LocalTranslationChannel { Id = "Child1", DefaultValue = float3.zero },
                new LocalTranslationChannel { Id = "Child2", DefaultValue = float3.zero },
            };

            var rig = new Rig { Value = RigBuilder.CreateRigDefinition(channels) };

            var rigRemapper = CreateNode<RigRemapperNode>();
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, rig);

            Set.SendTest(rigRemapper, (RigRemapperNode.Data data) =>
            {
                Assert.That(data.m_KernelData.SourceRigDefinition.Value.GetHashCode(), Is.EqualTo(rig.Value.Value.GetHashCode()));
            });
        }

        [Test]
        public void CanSetDestinationRigDefinition()
        {
            var channels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "Root",   DefaultValue = float3.zero },
                new LocalTranslationChannel { Id = "Child1", DefaultValue = float3.zero },
                new LocalTranslationChannel { Id = "Child2", DefaultValue = float3.zero },
            };

            var rig = new Rig { Value = RigBuilder.CreateRigDefinition(channels) };

            var rigRemapper = CreateNode<RigRemapperNode>();
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, rig);

            Set.SendTest(rigRemapper, (RigRemapperNode.Data data) =>
            {
                Assert.That(data.m_KernelData.DestinationRigDefinition.Value.GetHashCode(), Is.EqualTo(rig.Value.Value.GetHashCode()));
            });
        }

        [Test]
        public void CanRemapAllTranslationChannel()
        {
            var sourceChannels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "Root",   DefaultValue = m_ExpectedSourceTranslation },
                new LocalTranslationChannel { Id = "Child1", DefaultValue = m_ExpectedSourceTranslation },
                new LocalTranslationChannel { Id = "Child2", DefaultValue = m_ExpectedSourceTranslation },
            };

            var sourceRig = new Rig { Value = RigBuilder.CreateRigDefinition(sourceChannels) };

            var destinationChannels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "AnotherRoot",   DefaultValue = float3.zero },
                new LocalTranslationChannel { Id = "AnotherChild1", DefaultValue = float3.zero },
                new LocalTranslationChannel { Id = "AnotherChild2", DefaultValue = float3.zero },
            };
            var destinationRig = new Rig { Value = RigBuilder.CreateRigDefinition(destinationChannels) };
            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, destinationRig, Entity.Null);

            var rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child1", DestinationId = "AnotherChild1" },
                    new ChannelMap { SourceId = "Child2", DestinationId = "AnotherChild2" }
                }
            };

            var remapTable = rigRemapQuery.ToRigRemapTable(sourceRig, destinationRig);

            // Here I'm using a layerMixer with no inputs connected
            // the expected result is to inject the default pose into Graph samples buffer
            var layerMixer = CreateNode<LayerMixerNode>();
            Set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, sourceRig);

            var rigRemapper = CreateNode<RigRemapperNode>();

            Set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, rigRemapper, RigRemapperNode.KernelPorts.Input);

            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, sourceRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, destinationRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.RemapTable, remapTable);

            var entityNode = CreateComponentNode(rigEntity);
            Set.Connect(rigRemapper, RigRemapperNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(m_ExpectedSourceTranslation).Using(TranslationComparer), "Channel localTranslation 1 doesn't match source rig default value");
            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(m_ExpectedSourceTranslation).Using(TranslationComparer), "Channel localTranslation 2 doesn't match source rig default value");
            Assert.That(streamECS.GetLocalToParentTranslation(2), Is.EqualTo(m_ExpectedSourceTranslation).Using(TranslationComparer), "Channel localTranslation 3 doesn't match source rig default value");
        }

        [Test]
        public void CanRemapPartialTranslationChannel()
        {
            var sourceChannels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "Root",   DefaultValue = m_ExpectedSourceTranslation },
                new LocalTranslationChannel { Id = "Child1", DefaultValue = float3.zero },
                new LocalTranslationChannel { Id = "Child2", DefaultValue = m_ExpectedSourceTranslation },
            };

            var sourceRig = new Rig { Value = RigBuilder.CreateRigDefinition(sourceChannels) };

            var destinationChannels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "AnotherRoot",   DefaultValue = float3.zero },
                new LocalTranslationChannel { Id = "AnotherChild1", DefaultValue = m_ExpectedDestinationTranslation },
                new LocalTranslationChannel { Id = "AnotherChild2", DefaultValue = float3.zero },
            };
            var destinationRig = new Rig { Value = RigBuilder.CreateRigDefinition(destinationChannels) };
            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, destinationRig, Entity.Null);

            var rigRemapQuery = new RigRemapQuery
            {
                TranslationChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child2", DestinationId = "AnotherChild2" }
                }
            };

            var remapTable = rigRemapQuery.ToRigRemapTable(sourceRig, destinationRig);

            // Here I'm using a layerMixer with no inputs connected
            // the expected result is to inject the default pose into Graph samples buffer
            var layerMixer = CreateNode<LayerMixerNode>();
            Set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, sourceRig);

            var rigRemapper = CreateNode<RigRemapperNode>();

            Set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, rigRemapper, RigRemapperNode.KernelPorts.Input);

            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, sourceRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, destinationRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.RemapTable, remapTable);

            var entityNode = CreateComponentNode(rigEntity);
            Set.Connect(rigRemapper, RigRemapperNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(m_ExpectedSourceTranslation).Using(TranslationComparer), "Channel localTranslation 1 doesn't match source rig default value");
            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(m_ExpectedDestinationTranslation).Using(TranslationComparer), "Channel localTranslation 2 doesn't match destination rig default value");
            Assert.That(streamECS.GetLocalToParentTranslation(2), Is.EqualTo(m_ExpectedSourceTranslation).Using(TranslationComparer), "Channel localTranslation 3 doesn't match source rig default value");
        }

        [Test]
        public void CanRemapRigTranslationOffset()
        {
            var sourceChannels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "Root",   DefaultValue = m_ExpectedSourceTranslation },
                new LocalTranslationChannel { Id = "Child", DefaultValue = m_ExpectedSourceTranslation },
            };

            var sourceRig = new Rig { Value = RigBuilder.CreateRigDefinition(sourceChannels) };

            var destinationChannels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "AnotherRoot",   DefaultValue = float3.zero },
                new LocalTranslationChannel { Id = "AnotherChild", DefaultValue = float3.zero },
            };

            var destinationRig = new Rig { Value = RigBuilder.CreateRigDefinition(destinationChannels) };
            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, destinationRig, Entity.Null);

            var rigRemapQuery = new RigRemapQuery
            {
                TranslationChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child", DestinationId = "AnotherChild", OffsetIndex = 1 }
                },

                TranslationOffsets = new[]
                {
                    new RigTranslationOffset(),
                    new RigTranslationOffset { Scale = 2, Rotation = math.normalize(math.quaternion(0, 1, 0, 1)) }
                }
            };

            var remapTable = rigRemapQuery.ToRigRemapTable(sourceRig, destinationRig);

            // Here I'm using a layerMixer with no inputs connected
            // the expected result is to inject the default pose into Graph samples buffer
            var layerMixer = CreateNode<LayerMixerNode>();
            Set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, sourceRig);

            var rigRemapper = CreateNode<RigRemapperNode>();

            Set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, rigRemapper, RigRemapperNode.KernelPorts.Input);

            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, sourceRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, destinationRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.RemapTable, remapTable);

            var entityNode = CreateComponentNode(rigEntity);

            Set.Connect(rigRemapper, RigRemapperNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(math.mul(rigRemapQuery.TranslationOffsets[1].Rotation, m_ExpectedSourceTranslation * rigRemapQuery.TranslationOffsets[1].Scale)).Using(TranslationComparer), "Channel localTranslation doesn't match source rig default value with rig offset");
        }

        [Test]
        public void CanRemapAllRotationChannel()
        {
            var sourceChannels = new IAnimationChannel[]
            {
                new LocalRotationChannel { Id = "Root",   DefaultValue = m_ExpectedSourceRotation },
                new LocalRotationChannel { Id = "Child1", DefaultValue = m_ExpectedSourceRotation },
                new LocalRotationChannel { Id = "Child2", DefaultValue = m_ExpectedSourceRotation },
            };

            var sourceRig = new Rig { Value = RigBuilder.CreateRigDefinition(sourceChannels) };

            var destinationChannels = new IAnimationChannel[]
            {
                new LocalRotationChannel { Id = "AnotherRoot",   DefaultValue = quaternion.identity },
                new LocalRotationChannel { Id = "AnotherChild1", DefaultValue = quaternion.identity },
                new LocalRotationChannel { Id = "AnotherChild2", DefaultValue = quaternion.identity },
            };
            var destinationRig = new Rig { Value = RigBuilder.CreateRigDefinition(destinationChannels) };
            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, destinationRig, Entity.Null);

            var rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child1", DestinationId = "AnotherChild1" },
                    new ChannelMap { SourceId = "Child2", DestinationId = "AnotherChild2" }
                }
            };

            var remapTable = rigRemapQuery.ToRigRemapTable(sourceRig, destinationRig);

            // Here I'm using a layerMixer with no inputs connected
            // the expected result is to inject the default pose into Graph samples buffer
            var layerMixer = CreateNode<LayerMixerNode>();
            Set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, sourceRig);

            var rigRemapper = CreateNode<RigRemapperNode>();

            Set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, rigRemapper, RigRemapperNode.KernelPorts.Input);

            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, sourceRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, destinationRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.RemapTable, remapTable);

            var entityNode = CreateComponentNode(rigEntity);
            Set.Connect(rigRemapper, RigRemapperNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentRotation(0), Is.EqualTo(m_ExpectedSourceRotation).Using(RotationComparer), "Channel localRotation 1 doesn't match source rig default value");
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(m_ExpectedSourceRotation).Using(RotationComparer), "Channel localRotation 2 doesn't match source rig default value");
            Assert.That(streamECS.GetLocalToParentRotation(2), Is.EqualTo(m_ExpectedSourceRotation).Using(RotationComparer), "Channel localRotation 3 doesn't match source rig default value");
        }

        [Test]
        public void CanRemapPartialRotationChannel()
        {
            var sourceChannels = new IAnimationChannel[]
            {
                new LocalRotationChannel { Id = "Root",   DefaultValue = m_ExpectedSourceRotation },
                new LocalRotationChannel { Id = "Child1", DefaultValue = quaternion.identity },
                new LocalRotationChannel { Id = "Child2", DefaultValue = m_ExpectedSourceRotation },
            };

            var sourceRig = new Rig { Value = RigBuilder.CreateRigDefinition(sourceChannels) };

            var destinationChannels = new IAnimationChannel[]
            {
                new LocalRotationChannel { Id = "AnotherRoot",   DefaultValue = quaternion.identity },
                new LocalRotationChannel { Id = "AnotherChild1", DefaultValue = m_ExpectedDestinationRotation },
                new LocalRotationChannel { Id = "AnotherChild2", DefaultValue = quaternion.identity },
            };
            var destinationRig = new Rig { Value = RigBuilder.CreateRigDefinition(destinationChannels) };
            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, destinationRig, Entity.Null);

            var rigRemapQuery = new RigRemapQuery
            {
                RotationChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child2", DestinationId = "AnotherChild2" }
                }
            };

            var remapTable = rigRemapQuery.ToRigRemapTable(sourceRig, destinationRig);

            // Here I'm using a layerMixer with no inputs connected
            // the expected result is to inject the default pose into Graph samples buffer
            var layerMixer = CreateNode<LayerMixerNode>();
            Set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, sourceRig);

            var rigRemapper = CreateNode<RigRemapperNode>();

            Set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, rigRemapper, RigRemapperNode.KernelPorts.Input);

            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, sourceRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, destinationRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.RemapTable, remapTable);

            var entityNode = CreateComponentNode(rigEntity);
            Set.Connect(rigRemapper, RigRemapperNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentRotation(0), Is.EqualTo(m_ExpectedSourceRotation).Using(RotationComparer), "Channel localRotation 1 doesn't match source rig default value");
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(m_ExpectedDestinationRotation).Using(RotationComparer), "Channel localRotation 2 doesn't match destination rig default value");
            Assert.That(streamECS.GetLocalToParentRotation(2), Is.EqualTo(m_ExpectedSourceRotation).Using(RotationComparer), "Channel localRotation 3 doesn't match source rig default value");
        }

        [Test]
        public void CanRemapRigRotationOffset()
        {
            var sourceChannels = new IAnimationChannel[]
            {
                new LocalRotationChannel { Id = "Root",   DefaultValue = m_ExpectedSourceRotation },
                new LocalRotationChannel { Id = "Child", DefaultValue = m_ExpectedSourceRotation },
            };

            var sourceRig = new Rig { Value = RigBuilder.CreateRigDefinition(sourceChannels) };

            var destinationChannels = new IAnimationChannel[]
            {
                new LocalRotationChannel { Id = "AnotherRoot",   DefaultValue = quaternion.identity },
                new LocalRotationChannel { Id = "AnotherChild", DefaultValue = quaternion.identity },
            };

            var destinationRig = new Rig { Value = RigBuilder.CreateRigDefinition(destinationChannels) };
            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, destinationRig, Entity.Null);

            var rigRemapQuery = new RigRemapQuery
            {
                RotationChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child", DestinationId = "AnotherChild", OffsetIndex = 1 }
                },

                RotationOffsets = new[]
                {
                    new RigRotationOffset(),
                    new RigRotationOffset { PreRotation = math.normalize(math.quaternion(1, 2, 3, 4)), PostRotation = math.normalize(math.quaternion(5, 6, 7, 8))}
                }
            };

            var remapTable = rigRemapQuery.ToRigRemapTable(sourceRig, destinationRig);

            // Here I'm using a layerMixer with no inputs connected
            // the expected result is to inject the default pose into Graph samples buffer
            var layerMixer = CreateNode<LayerMixerNode>();
            Set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, sourceRig);

            var rigRemapper = CreateNode<RigRemapperNode>();

            Set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, rigRemapper, RigRemapperNode.KernelPorts.Input);

            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, sourceRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, destinationRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.RemapTable, remapTable);

            var entityNode = CreateComponentNode(rigEntity);
            Set.Connect(rigRemapper, RigRemapperNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(math.mul(rigRemapQuery.RotationOffsets[1].PreRotation, math.mul(m_ExpectedSourceRotation, rigRemapQuery.RotationOffsets[1].PostRotation))).Using(RotationComparer), "Channel localRotation doesn't match destination rig default value with rig rotation offset");
        }

        [Test]
        public void CanRemapAllScaleChannel()
        {
            var sourceChannels = new IAnimationChannel[]
            {
                new LocalScaleChannel { Id = "Root",   DefaultValue = m_ExpectedSourceScale },
                new LocalScaleChannel { Id = "Child1", DefaultValue = m_ExpectedSourceScale },
                new LocalScaleChannel { Id = "Child2", DefaultValue = m_ExpectedSourceScale },
            };

            var sourceRig = new Rig { Value = RigBuilder.CreateRigDefinition(sourceChannels) };

            var destinationChannels = new IAnimationChannel[]
            {
                new LocalScaleChannel { Id = "AnotherRoot",   DefaultValue = float3.zero },
                new LocalScaleChannel { Id = "AnotherChild1", DefaultValue = float3.zero },
                new LocalScaleChannel { Id = "AnotherChild2", DefaultValue = float3.zero },
            };
            var destinationRig = new Rig { Value = RigBuilder.CreateRigDefinition(destinationChannels) };
            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, destinationRig, Entity.Null);

            var rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child1", DestinationId = "AnotherChild1" },
                    new ChannelMap { SourceId = "Child2", DestinationId = "AnotherChild2" }
                }
            };

            var remapTable = rigRemapQuery.ToRigRemapTable(sourceRig, destinationRig);

            // Here I'm using a layerMixer with no inputs connected
            // the expected result is to inject the default pose into Graph samples buffer
            var layerMixer = CreateNode<LayerMixerNode>();
            Set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, sourceRig);

            var rigRemapper = CreateNode<RigRemapperNode>();

            Set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, rigRemapper, RigRemapperNode.KernelPorts.Input);

            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, sourceRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, destinationRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.RemapTable, remapTable);

            var entityNode = CreateComponentNode(rigEntity);
            Set.Connect(rigRemapper, RigRemapperNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(m_ExpectedSourceScale).Using(ScaleComparer), "Channel localScale 0 doesn't match source rig default value");
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(m_ExpectedSourceScale).Using(ScaleComparer), "Channel localScale 1 doesn't match source rig default value");
            Assert.That(streamECS.GetLocalToParentScale(2), Is.EqualTo(m_ExpectedSourceScale).Using(ScaleComparer), "Channel localScale 2 doesn't match source rig default value");
        }

        [Test]
        public void CanRemapPartialScaleChannel()
        {
            var sourceChannels = new IAnimationChannel[]
            {
                new LocalScaleChannel { Id = "Root",   DefaultValue = m_ExpectedSourceScale },
                new LocalScaleChannel { Id = "Child1", DefaultValue = float3.zero },
                new LocalScaleChannel { Id = "Child2", DefaultValue = m_ExpectedSourceScale },
            };

            var sourceRig = new Rig { Value = RigBuilder.CreateRigDefinition(sourceChannels) };

            var destinationChannels = new IAnimationChannel[]
            {
                new LocalScaleChannel { Id = "AnotherRoot",   DefaultValue = float3.zero },
                new LocalScaleChannel { Id = "AnotherChild1", DefaultValue = m_ExpectedDestinationScale },
                new LocalScaleChannel { Id = "AnotherChild2", DefaultValue = float3.zero },
            };
            var destinationRig = new Rig { Value = RigBuilder.CreateRigDefinition(destinationChannels) };
            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, destinationRig, Entity.Null);

            var rigRemapQuery = new RigRemapQuery
            {
                ScaleChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child2", DestinationId = "AnotherChild2" }
                }
            };
            var remapTable = rigRemapQuery.ToRigRemapTable(sourceRig, destinationRig);

            // Here I'm using a layerMixer with no inputs connected
            // the expected result is to inject the default pose into Graph samples buffer
            var layerMixer = CreateNode<LayerMixerNode>();
            Set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, sourceRig);

            var rigRemapper = CreateNode<RigRemapperNode>();

            Set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, rigRemapper, RigRemapperNode.KernelPorts.Input);

            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, sourceRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, destinationRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.RemapTable, remapTable);

            var entityNode = CreateComponentNode(rigEntity);
            Set.Connect(rigRemapper, RigRemapperNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(m_ExpectedSourceScale).Using(ScaleComparer), "Channel localScale 0 doesn't match source rig default value");
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(m_ExpectedDestinationScale).Using(ScaleComparer), "Channel localScale 1 doesn't match destination rig default value");
            Assert.That(streamECS.GetLocalToParentScale(2), Is.EqualTo(m_ExpectedSourceScale).Using(ScaleComparer), "Channel localScale 2 doesn't match source rig default value");
        }

        [Test]
        public void CanRemapAllFloatChannel()
        {
            var sourceChannels = new IAnimationChannel[]
            {
                new FloatChannel { Id = "Root",   DefaultValue = m_ExpectedSourceFloat },
                new FloatChannel { Id = "Child1", DefaultValue = m_ExpectedSourceFloat },
                new FloatChannel { Id = "Child2", DefaultValue = m_ExpectedSourceFloat },
            };

            var sourceRig = new Rig { Value = RigBuilder.CreateRigDefinition(sourceChannels) };

            var destinationChannels = new IAnimationChannel[]
            {
                new FloatChannel { Id = "AnotherRoot",   DefaultValue = 0.0f },
                new FloatChannel { Id = "AnotherChild1", DefaultValue = 0.0f },
                new FloatChannel { Id = "AnotherChild2", DefaultValue = 0.0f },
            };
            var destinationRig = new Rig { Value = RigBuilder.CreateRigDefinition(destinationChannels) };
            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, destinationRig, Entity.Null);

            var rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child1", DestinationId = "AnotherChild1" },
                    new ChannelMap { SourceId = "Child2", DestinationId = "AnotherChild2" }
                }
            };
            var remapTable = rigRemapQuery.ToRigRemapTable(sourceRig, destinationRig);

            // Here I'm using a layerMixer with no inputs connected
            // the expected result is to inject the default pose into Graph samples buffer
            var layerMixer = CreateNode<LayerMixerNode>();
            Set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, sourceRig);

            var rigRemapper = CreateNode<RigRemapperNode>();

            Set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, rigRemapper, RigRemapperNode.KernelPorts.Input);

            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, sourceRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, destinationRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.RemapTable, remapTable);

            var entityNode = CreateComponentNode(rigEntity);
            Set.Connect(rigRemapper, RigRemapperNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );

            Assert.That(streamECS.GetFloat(0), Is.EqualTo(m_ExpectedSourceFloat), "Channel float 0 doesn't match source rig default value");
            Assert.That(streamECS.GetFloat(1), Is.EqualTo(m_ExpectedSourceFloat), "Channel float 1 doesn't match source rig default value");
            Assert.That(streamECS.GetFloat(2), Is.EqualTo(m_ExpectedSourceFloat), "Channel float 2 doesn't match source rig default value");
        }

        [Test]
        public void CanRemapPartialFloatChannel()
        {
            var sourceChannels = new IAnimationChannel[]
            {
                new FloatChannel { Id = "Root",   DefaultValue = m_ExpectedSourceFloat },
                new FloatChannel { Id = "Child1", DefaultValue = 0.0f },
                new FloatChannel { Id = "Child2", DefaultValue = m_ExpectedSourceFloat },
            };

            var sourceRig = new Rig { Value = RigBuilder.CreateRigDefinition(sourceChannels) };

            var destinationChannels = new IAnimationChannel[]
            {
                new FloatChannel { Id = "AnotherRoot",   DefaultValue = 0.0f },
                new FloatChannel { Id = "AnotherChild1", DefaultValue = m_ExpectedDestinationFloat },
                new FloatChannel { Id = "AnotherChild2", DefaultValue = 0.0f },
            };
            var destinationRig = new Rig { Value = RigBuilder.CreateRigDefinition(destinationChannels) };
            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, destinationRig, Entity.Null);

            var rigRemapQuery = new RigRemapQuery
            {
                FloatChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child2", DestinationId = "AnotherChild2" }
                }
            };
            var remapTable = rigRemapQuery.ToRigRemapTable(sourceRig, destinationRig);

            // Here I'm using a layerMixer with no inputs connected
            // the expected result is to inject the default pose into Graph samples buffer
            var layerMixer = CreateNode<LayerMixerNode>();
            Set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, sourceRig);

            var rigRemapper = CreateNode<RigRemapperNode>();

            Set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, rigRemapper, RigRemapperNode.KernelPorts.Input);

            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, sourceRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, destinationRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.RemapTable, remapTable);

            var entityNode = CreateComponentNode(rigEntity);
            Set.Connect(rigRemapper, RigRemapperNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );

            Assert.That(streamECS.GetFloat(0), Is.EqualTo(m_ExpectedSourceFloat), "Channel float 0 doesn't match source rig default value");
            Assert.That(streamECS.GetFloat(1), Is.EqualTo(m_ExpectedDestinationFloat), "Channel float 1 doesn't match destination rig default value");
            Assert.That(streamECS.GetFloat(2), Is.EqualTo(m_ExpectedSourceFloat), "Channel float 2 doesn't match source rig default value");
        }

        [Test]
        public void CanRemapAllIntChannel()
        {
            var sourceChannels = new IAnimationChannel[]
            {
                new IntChannel { Id = "Root",   DefaultValue = m_ExpectedSourceInt },
                new IntChannel { Id = "Child1", DefaultValue = m_ExpectedSourceInt },
                new IntChannel { Id = "Child2", DefaultValue = m_ExpectedSourceInt },
            };

            var sourceRig = new Rig { Value = RigBuilder.CreateRigDefinition(sourceChannels) };

            var destinationChannels = new IAnimationChannel[]
            {
                new IntChannel { Id = "AnotherRoot",   DefaultValue = 0 },
                new IntChannel { Id = "AnotherChild1", DefaultValue = 0 },
                new IntChannel { Id = "AnotherChild2", DefaultValue = 0 },
            };
            var destinationRig = new Rig { Value = RigBuilder.CreateRigDefinition(destinationChannels) };
            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, destinationRig, Entity.Null);

            var rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child1", DestinationId = "AnotherChild1" },
                    new ChannelMap { SourceId = "Child2", DestinationId = "AnotherChild2" }
                }
            };
            var remapTable = rigRemapQuery.ToRigRemapTable(sourceRig, destinationRig);

            // Here I'm using a layerMixer with no inputs connected
            // the expected result is to inject the default pose into Graph samples buffer
            var layerMixer = CreateNode<LayerMixerNode>();
            Set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, sourceRig);

            var rigRemapper = CreateNode<RigRemapperNode>();

            Set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, rigRemapper, RigRemapperNode.KernelPorts.Input);

            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, sourceRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, destinationRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.RemapTable, remapTable);

            var entityNode = CreateComponentNode(rigEntity);
            Set.Connect(rigRemapper, RigRemapperNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );

            Assert.That(streamECS.GetInt(0), Is.EqualTo(m_ExpectedSourceInt), "Channel int 0 doesn't match source rig default value");
            Assert.That(streamECS.GetInt(1), Is.EqualTo(m_ExpectedSourceInt), "Channel int 1 doesn't match source rig default value");
            Assert.That(streamECS.GetInt(2), Is.EqualTo(m_ExpectedSourceInt), "Channel int 2 doesn't match source rig default value");
        }

        [Test]
        public void CanRemapPartialIntChannel()
        {
            var sourceChannels = new IAnimationChannel[]
            {
                new IntChannel { Id = "Root",   DefaultValue = m_ExpectedSourceInt },
                new IntChannel { Id = "Child1", DefaultValue = 0 },
                new IntChannel { Id = "Child2", DefaultValue = m_ExpectedSourceInt },
            };

            var sourceRig = new Rig { Value = RigBuilder.CreateRigDefinition(sourceChannels) };

            var destinationChannels = new IAnimationChannel[]
            {
                new IntChannel { Id = "AnotherRoot",   DefaultValue = 0 },
                new IntChannel { Id = "AnotherChild1", DefaultValue = m_ExpectedDestinationInt },
                new IntChannel { Id = "AnotherChild2", DefaultValue = 0 },
            };
            var destinationRig = new Rig { Value = RigBuilder.CreateRigDefinition(destinationChannels) };
            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, destinationRig, Entity.Null);

            var rigRemapQuery = new RigRemapQuery
            {
                IntChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child2", DestinationId = "AnotherChild2" }
                }
            };
            var remapTable = rigRemapQuery.ToRigRemapTable(sourceRig, destinationRig);

            // Here I'm using a layerMixer with no inputs connected
            // the expected result is to inject the default pose into Graph samples buffer
            var layerMixer = CreateNode<LayerMixerNode>();
            Set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, sourceRig);

            var rigRemapper = CreateNode<RigRemapperNode>();

            Set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, rigRemapper, RigRemapperNode.KernelPorts.Input);

            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, sourceRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, destinationRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.RemapTable, remapTable);

            var entityNode = CreateComponentNode(rigEntity);
            Set.Connect(rigRemapper, RigRemapperNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );

            Assert.That(streamECS.GetInt(0), Is.EqualTo(m_ExpectedSourceInt), "Channel int 0 doesn't match source rig default value");
            Assert.That(streamECS.GetInt(1), Is.EqualTo(m_ExpectedDestinationInt), "Channel int 1 doesn't match destination rig default value");
            Assert.That(streamECS.GetInt(2), Is.EqualTo(m_ExpectedSourceInt), "Channel int 2 doesn't match source rig default value");
        }

        [Test]
        public void DefaultValuesAreReturnedWhenSourceRigOrRemapTableIsInvalid()
        {
            var sourceChannels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "Root",   DefaultValue = m_ExpectedSourceTranslation },
                new LocalTranslationChannel { Id = "Child1", DefaultValue = m_ExpectedSourceTranslation },

                new LocalRotationChannel { Id = "Root", DefaultValue = m_ExpectedSourceRotation },
                new LocalRotationChannel { Id = "Child1", DefaultValue = m_ExpectedSourceRotation },

                new LocalScaleChannel { Id = "Root", DefaultValue = m_ExpectedSourceScale },
                new LocalScaleChannel { Id = "Child1", DefaultValue = m_ExpectedSourceScale },

                new FloatChannel { Id = "Float0", DefaultValue = m_ExpectedSourceFloat },
                new FloatChannel { Id = "Float1", DefaultValue = m_ExpectedSourceFloat },

                new IntChannel { Id = "Int0", DefaultValue = m_ExpectedSourceInt },
                new IntChannel { Id = "Int1", DefaultValue = m_ExpectedSourceInt }
            };
            var sourceRig = new Rig { Value = RigBuilder.CreateRigDefinition(sourceChannels) };

            var destinationChannels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "AnotherRoot",   DefaultValue = m_ExpectedDestinationTranslation },
                new LocalTranslationChannel { Id = "AnotherChild1", DefaultValue = m_ExpectedDestinationTranslation },

                new LocalRotationChannel { Id = "AnotherRoot", DefaultValue = m_ExpectedDestinationRotation },
                new LocalRotationChannel { Id = "AnotherChild1", DefaultValue = m_ExpectedDestinationRotation },

                new LocalScaleChannel { Id = "AnotherRoot", DefaultValue = m_ExpectedDestinationScale },
                new LocalScaleChannel { Id = "AnotherChild1", DefaultValue = m_ExpectedDestinationScale },

                new FloatChannel { Id = "AnotherFloat0", DefaultValue = m_ExpectedDestinationFloat },
                new FloatChannel { Id = "AnotherFloat1", DefaultValue = m_ExpectedDestinationFloat },

                new IntChannel { Id = "AnotherInt0", DefaultValue = m_ExpectedDestinationInt },
                new IntChannel { Id = "AnotherInt1", DefaultValue = m_ExpectedDestinationInt }
            };
            var destinationRig = new Rig { Value = RigBuilder.CreateRigDefinition(destinationChannels) };

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, destinationRig, Entity.Null);

            var rigRemapQuery = new RigRemapQuery
            {
                TranslationChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child1", DestinationId = "AnotherChild1" }
                },

                RotationChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child1", DestinationId = "AnotherChild1" }
                },

                ScaleChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child1", DestinationId = "AnotherChild1" }
                },

                FloatChannels = new[]
                {
                    new ChannelMap { SourceId = "Float0", DestinationId = "AnotherFloat0" },
                    new ChannelMap { SourceId = "Float1", DestinationId = "AnotherFloat1" }
                },

                IntChannels = new[]
                {
                    new ChannelMap { SourceId = "Int0", DestinationId = "AnotherInt0" },
                    new ChannelMap { SourceId = "Int1", DestinationId = "AnotherInt1" }
                }
            };
            var remapTable = rigRemapQuery.ToRigRemapTable(sourceRig, destinationRig);

            var rigRemapper = CreateNode<RigRemapperNode>();
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, destinationRig);

            var entityNode = CreateComponentNode(rigEntity);
            Set.Connect(rigRemapper, RigRemapperNode.KernelPorts.Output, entityNode);

            var dstStream = AnimationStream.Create(destinationRig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            dstStream.ResetToZero();

            m_AnimationGraphSystem.Update();

            // Only the destination rig was set on the rig remapper node. The result should then be to only have
            // the default destination rig values in the stream
            dstStream = AnimationStream.CreateReadOnly(destinationRig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            Assert.That(dstStream.GetLocalToParentTranslation(0), Is.EqualTo(m_ExpectedDestinationTranslation).Using(TranslationComparer), "Channel localTranslation 0 doesn't match expected value");
            Assert.That(dstStream.GetLocalToParentTranslation(1), Is.EqualTo(m_ExpectedDestinationTranslation).Using(TranslationComparer), "Channel localTranslation 1 doesn't match expected value");

            Assert.That(dstStream.GetLocalToParentRotation(0), Is.EqualTo(m_ExpectedDestinationRotation).Using(RotationComparer), "Channel localRotation 0 doesn't match expected value");
            Assert.That(dstStream.GetLocalToParentRotation(1), Is.EqualTo(m_ExpectedDestinationRotation).Using(RotationComparer), "Channel localRotation 1 doesn't match expected value");

            Assert.That(dstStream.GetLocalToParentScale(0), Is.EqualTo(m_ExpectedDestinationScale).Using(TranslationComparer), "Channel localScale 0 doesn't match expected value");
            Assert.That(dstStream.GetLocalToParentScale(1), Is.EqualTo(m_ExpectedDestinationScale).Using(TranslationComparer), "Channel localScale 1 doesn't match expected value");

            Assert.That(dstStream.GetFloat(0), Is.EqualTo(m_ExpectedDestinationFloat), "Channel float 0 doesn't match expected value");
            Assert.That(dstStream.GetFloat(1), Is.EqualTo(m_ExpectedDestinationFloat), "Channel float 1 doesn't match expected value");

            Assert.That(dstStream.GetInt(0), Is.EqualTo(m_ExpectedDestinationInt), "Channel int 0 doesn't match expected value");
            Assert.That(dstStream.GetInt(1), Is.EqualTo(m_ExpectedDestinationInt), "Channel int 1 doesn't match expected value");

            // Set the source rig on the remapper node, without a remap table
            // The expected result should be to only have the default destination rig values in the stream
            dstStream = AnimationStream.Create(destinationRig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            dstStream.ResetToZero();
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, sourceRig);
            m_AnimationGraphSystem.Update();

            dstStream = AnimationStream.CreateReadOnly(destinationRig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            Assert.That(dstStream.GetLocalToParentTranslation(0), Is.EqualTo(m_ExpectedDestinationTranslation).Using(TranslationComparer), "Channel localTranslation 0 doesn't match expected value");
            Assert.That(dstStream.GetLocalToParentTranslation(1), Is.EqualTo(m_ExpectedDestinationTranslation).Using(TranslationComparer), "Channel localTranslation 1 doesn't match expected value");

            Assert.That(dstStream.GetLocalToParentRotation(0), Is.EqualTo(m_ExpectedDestinationRotation).Using(RotationComparer), "Channel localRotation 0 doesn't match expected value");
            Assert.That(dstStream.GetLocalToParentRotation(1), Is.EqualTo(m_ExpectedDestinationRotation).Using(RotationComparer), "Channel localRotation 1 doesn't match expected value");

            Assert.That(dstStream.GetLocalToParentScale(0), Is.EqualTo(m_ExpectedDestinationScale).Using(TranslationComparer), "Channel localScale 0 doesn't match expected value");
            Assert.That(dstStream.GetLocalToParentScale(1), Is.EqualTo(m_ExpectedDestinationScale).Using(TranslationComparer), "Channel localScale 1 doesn't match expected value");

            Assert.That(dstStream.GetFloat(0), Is.EqualTo(m_ExpectedDestinationFloat), "Channel float 0 doesn't match expected value");
            Assert.That(dstStream.GetFloat(1), Is.EqualTo(m_ExpectedDestinationFloat), "Channel float 1 doesn't match expected value");

            Assert.That(dstStream.GetInt(0), Is.EqualTo(m_ExpectedDestinationInt), "Channel int 0 doesn't match expected value");
            Assert.That(dstStream.GetInt(1), Is.EqualTo(m_ExpectedDestinationInt), "Channel int 1 doesn't match expected value");

            // Set a valid rig remap table. The expected result is that we should have the default values of the
            // destination rig since the stream input is still unconnected.
            dstStream = AnimationStream.Create(destinationRig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            dstStream.ResetToZero();
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.RemapTable, remapTable);
            m_AnimationGraphSystem.Update();

            dstStream = AnimationStream.CreateReadOnly(destinationRig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            Assert.That(dstStream.GetLocalToParentTranslation(0), Is.EqualTo(m_ExpectedDestinationTranslation).Using(TranslationComparer), "Channel localTranslation 0 doesn't match expected value");
            Assert.That(dstStream.GetLocalToParentTranslation(1), Is.EqualTo(m_ExpectedDestinationTranslation).Using(TranslationComparer), "Channel localTranslation 1 doesn't match expected value");

            Assert.That(dstStream.GetLocalToParentRotation(0), Is.EqualTo(m_ExpectedDestinationRotation).Using(RotationComparer), "Channel localRotation 0 doesn't match expected value");
            Assert.That(dstStream.GetLocalToParentRotation(1), Is.EqualTo(m_ExpectedDestinationRotation).Using(RotationComparer), "Channel localRotation 1 doesn't match expected value");

            Assert.That(dstStream.GetLocalToParentScale(0), Is.EqualTo(m_ExpectedDestinationScale).Using(TranslationComparer), "Channel localScale 0 doesn't match expected value");
            Assert.That(dstStream.GetLocalToParentScale(1), Is.EqualTo(m_ExpectedDestinationScale).Using(TranslationComparer), "Channel localScale 1 doesn't match expected value");

            Assert.That(dstStream.GetFloat(0), Is.EqualTo(m_ExpectedDestinationFloat), "Channel float 0 doesn't match expected value");
            Assert.That(dstStream.GetFloat(1), Is.EqualTo(m_ExpectedDestinationFloat), "Channel float 1 doesn't match expected value");

            Assert.That(dstStream.GetInt(0), Is.EqualTo(m_ExpectedDestinationInt), "Channel int 0 doesn't match expected value");
            Assert.That(dstStream.GetInt(1), Is.EqualTo(m_ExpectedDestinationInt), "Channel int 1 doesn't match expected value");

            // Finally create a source stream input and connect it to the rigremap node. The expected result is that we should have
            // the default values of the source rig properly remapped to the destination rig
            var defaultValuesNode = CreateNode<DefaultValuesNode>();
            Set.SendMessage(defaultValuesNode, DefaultValuesNode.SimulationPorts.Rig, sourceRig);
            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, rigRemapper, RigRemapperNode.KernelPorts.Input);
            dstStream = AnimationStream.Create(destinationRig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            dstStream.ResetToZero();
            m_AnimationGraphSystem.Update();

            dstStream = AnimationStream.CreateReadOnly(destinationRig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            Assert.That(dstStream.GetLocalToParentTranslation(0), Is.EqualTo(m_ExpectedSourceTranslation).Using(TranslationComparer), "Channel localTranslation 0 doesn't match expected value");
            Assert.That(dstStream.GetLocalToParentTranslation(1), Is.EqualTo(m_ExpectedSourceTranslation).Using(TranslationComparer), "Channel localTranslation 1 doesn't match expected value");

            Assert.That(dstStream.GetLocalToParentRotation(0), Is.EqualTo(m_ExpectedSourceRotation).Using(RotationComparer), "Channel localRotation 0 doesn't match expected value");
            Assert.That(dstStream.GetLocalToParentRotation(1), Is.EqualTo(m_ExpectedSourceRotation).Using(RotationComparer), "Channel localRotation 1 doesn't match expected value");

            Assert.That(dstStream.GetLocalToParentScale(0), Is.EqualTo(m_ExpectedSourceScale).Using(TranslationComparer), "Channel localScale 0 doesn't match expected value");
            Assert.That(dstStream.GetLocalToParentScale(1), Is.EqualTo(m_ExpectedSourceScale).Using(TranslationComparer), "Channel localScale 1 doesn't match expected value");

            Assert.That(dstStream.GetFloat(0), Is.EqualTo(m_ExpectedSourceFloat), "Channel float 0 doesn't match expected value");
            Assert.That(dstStream.GetFloat(1), Is.EqualTo(m_ExpectedSourceFloat), "Channel float 1 doesn't match expected value");

            Assert.That(dstStream.GetInt(0), Is.EqualTo(m_ExpectedSourceInt), "Channel int 0 doesn't match expected value");
            Assert.That(dstStream.GetInt(1), Is.EqualTo(m_ExpectedSourceInt), "Channel int 1 doesn't match expected value");
        }

        [Test]
        public void CanRemapWithDefaultValuesOverride()
        {
            var sourceChannels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "Root",   DefaultValue = m_ExpectedSourceTranslation },
                new LocalTranslationChannel { Id = "Child2", DefaultValue = m_ExpectedSourceTranslation },

                new LocalRotationChannel { Id = "Root", DefaultValue = m_ExpectedSourceRotation },
                new LocalRotationChannel { Id = "Child2", DefaultValue = m_ExpectedSourceRotation },

                new LocalScaleChannel { Id = "Root", DefaultValue = m_ExpectedSourceScale },
                new LocalScaleChannel { Id = "Child2", DefaultValue = m_ExpectedSourceScale },

                new FloatChannel { Id = "Float0", DefaultValue = m_ExpectedSourceFloat },
                new FloatChannel { Id = "Float2", DefaultValue = m_ExpectedSourceFloat },

                new IntChannel { Id = "Int0", DefaultValue = m_ExpectedSourceInt },
                new IntChannel { Id = "Int2", DefaultValue = m_ExpectedSourceInt }
            };
            var sourceRig = new Rig { Value = RigBuilder.CreateRigDefinition(sourceChannels) };

            var destinationChannels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "AnotherRoot",   DefaultValue = m_ExpectedDestinationTranslation },
                new LocalTranslationChannel { Id = "AnotherChild1",   DefaultValue = m_ExpectedDestinationTranslation },
                new LocalTranslationChannel { Id = "AnotherChild2", DefaultValue = m_ExpectedDestinationTranslation },

                new LocalRotationChannel { Id = "AnotherRoot", DefaultValue = m_ExpectedDestinationRotation },
                new LocalRotationChannel { Id = "AnotherChild1", DefaultValue = m_ExpectedDestinationRotation },
                new LocalRotationChannel { Id = "AnotherChild2", DefaultValue = m_ExpectedDestinationRotation },

                new LocalScaleChannel { Id = "AnotherRoot", DefaultValue = m_ExpectedDestinationScale },
                new LocalScaleChannel { Id = "AnotherChild1", DefaultValue = m_ExpectedDestinationScale },
                new LocalScaleChannel { Id = "AnotherChild2", DefaultValue = m_ExpectedDestinationScale },

                new FloatChannel { Id = "AnotherFloat0", DefaultValue = m_ExpectedDestinationFloat },
                new FloatChannel { Id = "AnotherFloat1", DefaultValue = m_ExpectedDestinationFloat },
                new FloatChannel { Id = "AnotherFloat2", DefaultValue = m_ExpectedDestinationFloat },

                new IntChannel { Id = "AnotherInt0", DefaultValue = m_ExpectedDestinationInt },
                new IntChannel { Id = "AnotherInt1", DefaultValue = m_ExpectedDestinationInt },
                new IntChannel { Id = "AnotherInt2", DefaultValue = m_ExpectedDestinationInt }
            };
            var destinationRig = new Rig { Value = RigBuilder.CreateRigDefinition(destinationChannels) };

            var rigEntity = m_Manager.CreateEntity();
            var defaultValuesOverrideEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, destinationRig, Entity.Null);
            SetupRigEntity(defaultValuesOverrideEntity, destinationRig, Entity.Null);

            var rigRemapQuery = new RigRemapQuery
            {
                TranslationChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child2", DestinationId = "AnotherChild2" }
                },

                RotationChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child2", DestinationId = "AnotherChild2" }
                },

                ScaleChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child2", DestinationId = "AnotherChild2" }
                },

                FloatChannels = new[]
                {
                    new ChannelMap { SourceId = "Float0", DestinationId = "AnotherFloat0" },
                    new ChannelMap { SourceId = "Float2", DestinationId = "AnotherFloat2" }
                },

                IntChannels = new[]
                {
                    new ChannelMap { SourceId = "Int0", DestinationId = "AnotherInt0" },
                    new ChannelMap { SourceId = "Int2", DestinationId = "AnotherInt2" }
                }
            };
            var remapTable = rigRemapQuery.ToRigRemapTable(sourceRig, destinationRig);

            var expectedOverrideTranslation = math.float3(100f, 200f, 300f);
            var expectedOverrideRotation = quaternion.AxisAngle(math.float3(0f, 0f, 1f), math.radians(32f));
            var expectedOverrideScale = math.float3(0.1f, 0.2f, 0.3f);
            var expectedOverrideFloat = 2000f;
            var expectedOverrideInt = 500;

            var overrideStream = AnimationStream.Create(destinationRig, m_Manager.GetBuffer<AnimatedData>(defaultValuesOverrideEntity).AsNativeArray());
            for (int i = 0; i < 3; ++i)
            {
                overrideStream.SetLocalToParentTranslation(i, expectedOverrideTranslation);
                overrideStream.SetLocalToParentRotation(i, expectedOverrideRotation);
                overrideStream.SetLocalToParentScale(i, expectedOverrideScale);
                overrideStream.SetFloat(i, expectedOverrideFloat);
                overrideStream.SetInt(i, expectedOverrideInt);
            }

            var defaultValuesNode = CreateNode<DefaultValuesNode>();
            Set.SendMessage(defaultValuesNode, DefaultValuesNode.SimulationPorts.Rig, sourceRig);

            var rigRemapper = CreateNode<RigRemapperNode>();
            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, rigRemapper, RigRemapperNode.KernelPorts.Input);

            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, sourceRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, destinationRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.RemapTable, remapTable);

            var overrideNode = CreateComponentNode(defaultValuesOverrideEntity);
            Set.Connect(overrideNode, rigRemapper, RigRemapperNode.KernelPorts.DefaultPoseInput);

            var entityNode = CreateComponentNode(rigEntity);
            Set.Connect(rigRemapper, RigRemapperNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var stream = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );

            Assert.That(stream.GetLocalToParentTranslation(0), Is.EqualTo(m_ExpectedSourceTranslation).Using(TranslationComparer), "Channel localTranslation 1 doesn't match source rig default value");
            Assert.That(stream.GetLocalToParentTranslation(1), Is.EqualTo(expectedOverrideTranslation).Using(TranslationComparer), "Channel localTranslation 2 doesn't match override rig default value");
            Assert.That(stream.GetLocalToParentTranslation(2), Is.EqualTo(m_ExpectedSourceTranslation).Using(TranslationComparer), "Channel localTranslation 3 doesn't match source rig default value");

            Assert.That(stream.GetLocalToParentRotation(0), Is.EqualTo(m_ExpectedSourceRotation).Using(RotationComparer), "Channel localRotation 1 doesn't match source rig default value");
            Assert.That(stream.GetLocalToParentRotation(1), Is.EqualTo(expectedOverrideRotation).Using(RotationComparer), "Channel localRotation 2 doesn't match override rig default value");
            Assert.That(stream.GetLocalToParentRotation(2), Is.EqualTo(m_ExpectedSourceRotation).Using(RotationComparer), "Channel localRotation 3 doesn't match source rig default value");

            Assert.That(stream.GetLocalToParentScale(0), Is.EqualTo(m_ExpectedSourceScale).Using(TranslationComparer), "Channel localScale 1 doesn't match source rig default value");
            Assert.That(stream.GetLocalToParentScale(1), Is.EqualTo(expectedOverrideScale).Using(TranslationComparer), "Channel localScale 2 doesn't match override rig default value");
            Assert.That(stream.GetLocalToParentScale(2), Is.EqualTo(m_ExpectedSourceScale).Using(TranslationComparer), "Channel localScale 3 doesn't match source rig default value");

            Assert.That(stream.GetFloat(0), Is.EqualTo(m_ExpectedSourceFloat), "Channel float 1 doesn't match source rig default value");
            Assert.That(stream.GetFloat(1), Is.EqualTo(expectedOverrideFloat), "Channel float 2 doesn't match override rig default value");
            Assert.That(stream.GetFloat(2), Is.EqualTo(m_ExpectedSourceFloat), "Channel float 3 doesn't match source rig default value");

            Assert.That(stream.GetInt(0), Is.EqualTo(m_ExpectedSourceInt), "Channel int 1 doesn't match source rig default value");
            Assert.That(stream.GetInt(1), Is.EqualTo(expectedOverrideInt), "Channel int 2 doesn't match override rig default value");
            Assert.That(stream.GetInt(2), Is.EqualTo(m_ExpectedSourceInt), "Channel int 3 doesn't match source rig default value");
        }

        [Test]
        public void CanRemapAllChannelsUsingAutoRigDefinitionBindingMatcher()
        {
            var sourceChannels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "Root",  DefaultValue = m_ExpectedSourceTranslation },
                new LocalTranslationChannel { Id = "Child2", DefaultValue = m_ExpectedSourceTranslation },

                new LocalRotationChannel { Id = "Root", DefaultValue = m_ExpectedSourceRotation },
                new LocalRotationChannel { Id = "Child2", DefaultValue = m_ExpectedSourceRotation },

                new LocalScaleChannel { Id = "Root", DefaultValue = m_ExpectedSourceScale },
                new LocalScaleChannel { Id = "Child2", DefaultValue = m_ExpectedSourceScale },

                new FloatChannel { Id = "Float0", DefaultValue = m_ExpectedSourceFloat },
                new FloatChannel { Id = "Float2", DefaultValue = m_ExpectedSourceFloat },

                new IntChannel { Id = "Int0", DefaultValue = m_ExpectedSourceInt },
                new IntChannel { Id = "Int2", DefaultValue = m_ExpectedSourceInt }
            };
            var sourceRig = new Rig { Value = RigBuilder.CreateRigDefinition(sourceChannels) };

            var destinationChannels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "Root",   DefaultValue = m_ExpectedDestinationTranslation },
                new LocalTranslationChannel { Id = "Child1", DefaultValue = m_ExpectedDestinationTranslation },
                new LocalTranslationChannel { Id = "Child2", DefaultValue = m_ExpectedDestinationTranslation },

                new LocalRotationChannel { Id = "Root",  DefaultValue = m_ExpectedDestinationRotation },
                new LocalRotationChannel { Id = "Child1", DefaultValue = m_ExpectedDestinationRotation },
                new LocalRotationChannel { Id = "Child2", DefaultValue = m_ExpectedDestinationRotation },

                new LocalScaleChannel { Id = "Root", DefaultValue = m_ExpectedDestinationScale },
                new LocalScaleChannel { Id = "Child1", DefaultValue = m_ExpectedDestinationScale },
                new LocalScaleChannel { Id = "Child2", DefaultValue = m_ExpectedDestinationScale },

                new FloatChannel { Id = "Float0", DefaultValue = m_ExpectedDestinationFloat },
                new FloatChannel { Id = "Float1", DefaultValue = m_ExpectedDestinationFloat },
                new FloatChannel { Id = "Float2", DefaultValue = m_ExpectedDestinationFloat },

                new IntChannel { Id = "Int0", DefaultValue = m_ExpectedDestinationInt },
                new IntChannel { Id = "Int1", DefaultValue = m_ExpectedDestinationInt },
                new IntChannel { Id = "Int2", DefaultValue = m_ExpectedDestinationInt }
            };
            var destinationRig = new Rig { Value = RigBuilder.CreateRigDefinition(destinationChannels) };
            var remapTable = RigRemapUtils.CreateRemapTable(sourceRig, destinationRig);

            Assert.AreEqual(remapTable.Value.TranslationMappings.Length, 2);
            Assert.AreEqual(remapTable.Value.RotationMappings.Length, 2);
            Assert.AreEqual(remapTable.Value.ScaleMappings.Length, 2);
            Assert.AreEqual(remapTable.Value.FloatMappings.Length, 2);
            Assert.AreEqual(remapTable.Value.IntMappings.Length, 2);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, destinationRig, Entity.Null);

            var defaultValuesNode = CreateNode<DefaultValuesNode>();
            Set.SendMessage(defaultValuesNode, DefaultValuesNode.SimulationPorts.Rig, sourceRig);

            var rigRemapper = CreateNode<RigRemapperNode>();
            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, rigRemapper, RigRemapperNode.KernelPorts.Input);

            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, sourceRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, destinationRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.RemapTable, remapTable);

            var entityNode = CreateComponentNode(rigEntity);
            Set.Connect(rigRemapper, RigRemapperNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var stream = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );

            Assert.That(stream.GetLocalToParentTranslation(0), Is.EqualTo(m_ExpectedSourceTranslation).Using(TranslationComparer), "Channel localTranslation 1 doesn't match source rig default value");
            Assert.That(stream.GetLocalToParentTranslation(1), Is.EqualTo(m_ExpectedDestinationTranslation).Using(TranslationComparer), "Channel localTranslation 2 doesn't match destination rig default value");
            Assert.That(stream.GetLocalToParentTranslation(2), Is.EqualTo(m_ExpectedSourceTranslation).Using(TranslationComparer), "Channel localTranslation 3 doesn't match source rig default value");

            Assert.That(stream.GetLocalToParentRotation(0), Is.EqualTo(m_ExpectedSourceRotation).Using(RotationComparer), "Channel localRotation 1 doesn't match source rig default value");
            Assert.That(stream.GetLocalToParentRotation(1), Is.EqualTo(m_ExpectedDestinationRotation).Using(RotationComparer), "Channel localRotation 2 doesn't match destination rig default value");
            Assert.That(stream.GetLocalToParentRotation(2), Is.EqualTo(m_ExpectedSourceRotation).Using(RotationComparer), "Channel localRotation 3 doesn't match source rig default value");

            Assert.That(stream.GetLocalToParentScale(0), Is.EqualTo(m_ExpectedSourceScale).Using(TranslationComparer), "Channel localScale 1 doesn't match source rig default value");
            Assert.That(stream.GetLocalToParentScale(1), Is.EqualTo(m_ExpectedDestinationScale).Using(TranslationComparer), "Channel localScale 2 doesn't match destination rig default value");
            Assert.That(stream.GetLocalToParentScale(2), Is.EqualTo(m_ExpectedSourceScale).Using(TranslationComparer), "Channel localScale 3 doesn't match source rig default value");

            Assert.That(stream.GetFloat(0), Is.EqualTo(m_ExpectedSourceFloat), "Channel float 1 doesn't match source rig default value");
            Assert.That(stream.GetFloat(1), Is.EqualTo(m_ExpectedDestinationFloat), "Channel float 2 doesn't match destination rig default value");
            Assert.That(stream.GetFloat(2), Is.EqualTo(m_ExpectedSourceFloat), "Channel float 3 doesn't match source rig default value");

            Assert.That(stream.GetInt(0), Is.EqualTo(m_ExpectedSourceInt), "Channel int 1 doesn't match source rig default value");
            Assert.That(stream.GetInt(1), Is.EqualTo(m_ExpectedDestinationInt), "Channel int 2 doesn't match destination rig default value");
            Assert.That(stream.GetInt(2), Is.EqualTo(m_ExpectedSourceInt), "Channel int 3 doesn't match source rig default value");
        }

        [TestCase(RigRemapUtils.ChannelFilter.None)]
        [TestCase(RigRemapUtils.ChannelFilter.Translation)]
        [TestCase(RigRemapUtils.ChannelFilter.Rotation)]
        [TestCase(RigRemapUtils.ChannelFilter.Scale)]
        [TestCase(RigRemapUtils.ChannelFilter.Float)]
        [TestCase(RigRemapUtils.ChannelFilter.Int)]
        [TestCase(RigRemapUtils.ChannelFilter.Translation | RigRemapUtils.ChannelFilter.Rotation)]
        [TestCase(RigRemapUtils.ChannelFilter.Rotation | RigRemapUtils.ChannelFilter.Scale)]
        [TestCase(RigRemapUtils.ChannelFilter.Translation | RigRemapUtils.ChannelFilter.Scale)]
        [TestCase(RigRemapUtils.ChannelFilter.Translation | RigRemapUtils.ChannelFilter.Rotation | RigRemapUtils.ChannelFilter.Scale)]
        [TestCase(RigRemapUtils.ChannelFilter.Translation | RigRemapUtils.ChannelFilter.Float)]
        [TestCase(RigRemapUtils.ChannelFilter.Rotation | RigRemapUtils.ChannelFilter.Int)]
        [TestCase(RigRemapUtils.ChannelFilter.Scale | RigRemapUtils.ChannelFilter.Float | RigRemapUtils.ChannelFilter.Int)]
        [TestCase(RigRemapUtils.ChannelFilter.All)]
        public void CanRemapSpecificChannelsUsingAutoRigDefinitionBindingMatcher(RigRemapUtils.ChannelFilter filter)
        {
            var sourceChannels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "Root",  DefaultValue = m_ExpectedSourceTranslation },
                new LocalTranslationChannel { Id = "Child1", DefaultValue = m_ExpectedSourceTranslation },

                new LocalRotationChannel { Id = "Root", DefaultValue = m_ExpectedSourceRotation },
                new LocalRotationChannel { Id = "Child1", DefaultValue = m_ExpectedSourceRotation },

                new LocalScaleChannel { Id = "Root", DefaultValue = m_ExpectedSourceScale },
                new LocalScaleChannel { Id = "Child1", DefaultValue = m_ExpectedSourceScale },

                new FloatChannel { Id = "Float0", DefaultValue = m_ExpectedSourceFloat },
                new FloatChannel { Id = "Float1", DefaultValue = m_ExpectedSourceFloat },

                new IntChannel { Id = "Int0", DefaultValue = m_ExpectedSourceInt },
                new IntChannel { Id = "Int1", DefaultValue = m_ExpectedSourceInt }
            };
            var sourceRig = new Rig { Value = RigBuilder.CreateRigDefinition(sourceChannels) };

            var destinationChannels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "Root",   DefaultValue = m_ExpectedDestinationTranslation },
                new LocalTranslationChannel { Id = "Child1", DefaultValue = m_ExpectedDestinationTranslation },

                new LocalRotationChannel { Id = "Root",  DefaultValue = m_ExpectedDestinationRotation },
                new LocalRotationChannel { Id = "Child1", DefaultValue = m_ExpectedDestinationRotation },

                new LocalScaleChannel { Id = "Root", DefaultValue = m_ExpectedDestinationScale },
                new LocalScaleChannel { Id = "Child1", DefaultValue = m_ExpectedDestinationScale },

                new FloatChannel { Id = "Float0", DefaultValue = m_ExpectedDestinationFloat },
                new FloatChannel { Id = "Float1", DefaultValue = m_ExpectedDestinationFloat },

                new IntChannel { Id = "Int0", DefaultValue = m_ExpectedDestinationInt },
                new IntChannel { Id = "Int1", DefaultValue = m_ExpectedDestinationInt },
            };
            var destinationRig = new Rig { Value = RigBuilder.CreateRigDefinition(destinationChannels) };
            var remapTable = RigRemapUtils.CreateRemapTable(sourceRig, destinationRig, filter);

            bool hasTranslations = (filter & RigRemapUtils.ChannelFilter.Translation) != RigRemapUtils.ChannelFilter.None;
            bool hasRotations = (filter & RigRemapUtils.ChannelFilter.Rotation) != RigRemapUtils.ChannelFilter.None;
            bool hasScales = (filter & RigRemapUtils.ChannelFilter.Scale) != RigRemapUtils.ChannelFilter.None;
            bool hasFloats = (filter & RigRemapUtils.ChannelFilter.Float) != RigRemapUtils.ChannelFilter.None;
            bool hasInts = (filter & RigRemapUtils.ChannelFilter.Int) != RigRemapUtils.ChannelFilter.None;

            Assert.AreEqual(remapTable.Value.TranslationMappings.Length, hasTranslations ? 2 : 0);
            Assert.AreEqual(remapTable.Value.RotationMappings.Length, hasRotations ? 2 : 0);
            Assert.AreEqual(remapTable.Value.ScaleMappings.Length, hasScales ? 2 : 0);
            Assert.AreEqual(remapTable.Value.FloatMappings.Length, hasFloats ? 2 : 0);
            Assert.AreEqual(remapTable.Value.IntMappings.Length, hasInts ? 2 : 0);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, destinationRig, Entity.Null);

            var defaultValuesNode = CreateNode<DefaultValuesNode>();
            Set.SendMessage(defaultValuesNode, DefaultValuesNode.SimulationPorts.Rig, sourceRig);

            var rigRemapper = CreateNode<RigRemapperNode>();
            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, rigRemapper, RigRemapperNode.KernelPorts.Input);

            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, sourceRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, destinationRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.RemapTable, remapTable);

            var entityNode = CreateComponentNode(rigEntity);
            Set.Connect(rigRemapper, RigRemapperNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var stream = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );

            var expectedTranslation = hasTranslations ? m_ExpectedSourceTranslation : m_ExpectedDestinationTranslation;
            Assert.That(stream.GetLocalToParentTranslation(0), Is.EqualTo(expectedTranslation).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentTranslation(1), Is.EqualTo(expectedTranslation).Using(TranslationComparer));

            var expectedRotation = hasRotations ? m_ExpectedSourceRotation : m_ExpectedDestinationRotation;
            Assert.That(stream.GetLocalToParentRotation(0), Is.EqualTo(expectedRotation).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentRotation(1), Is.EqualTo(expectedRotation).Using(RotationComparer));

            var expectedScale = hasScales ? m_ExpectedSourceScale : m_ExpectedDestinationScale;
            Assert.That(stream.GetLocalToParentScale(0), Is.EqualTo(expectedScale).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentScale(1), Is.EqualTo(expectedScale).Using(TranslationComparer));

            var expectedFloat = hasFloats ? m_ExpectedSourceFloat : m_ExpectedDestinationFloat;
            Assert.That(stream.GetFloat(0), Is.EqualTo(expectedFloat));
            Assert.That(stream.GetFloat(1), Is.EqualTo(expectedFloat));

            var expectedInt = hasInts ? m_ExpectedSourceInt : m_ExpectedDestinationInt;
            Assert.That(stream.GetInt(0), Is.EqualTo(expectedInt));
            Assert.That(stream.GetInt(1), Is.EqualTo(expectedInt));
        }

        [Test]
        public void CanRemapSkeletonNodesUsingAutoRigDefinitionBindingMatcherWithOverrides()
        {
            var srcSkeletonNodes = new SkeletonNode[]
            {
                new SkeletonNode { Id = "Root", ParentIndex = -1, AxisIndex = -1, LocalTranslationDefaultValue = math.float3(0f, 0.5f, 0f), LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = math.float3(1f) },
                new SkeletonNode { Id = "Root/Hips", ParentIndex = 0, AxisIndex = -1, LocalTranslationDefaultValue = math.float3(0f, 0f, 4f), LocalRotationDefaultValue = quaternion.AxisAngle(math.float3(0f, 1f, 0f), math.radians(20f)), LocalScaleDefaultValue = math.float3(1f)},
                new SkeletonNode { Id = "Root/Hips/LeftUpLeg", ParentIndex = 1, AxisIndex = -1, LocalTranslationDefaultValue = math.float3(2f, 0f, 0f), LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = math.float3(1f) },
                new SkeletonNode { Id = "Root/Hips/RightUpLeg", ParentIndex = 1, AxisIndex = -1, LocalTranslationDefaultValue = math.float3(-2f, 0f, 0f), LocalRotationDefaultValue = quaternion.AxisAngle(math.float3(1f, 0f, 0f), math.radians(40f)), LocalScaleDefaultValue = math.float3(1f) },
            };
            var sourceRig = new Rig { Value = RigBuilder.CreateRigDefinition(srcSkeletonNodes) };

            var dstSkeletonNodes = new SkeletonNode[]
            {
                new SkeletonNode { Id = "Root", ParentIndex = -1, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.AxisAngle(math.float3(1f, 0f, 0f), math.radians(10f)), LocalScaleDefaultValue = math.float3(1f) },
                new SkeletonNode { Id = "Root/Hips", ParentIndex = 0, AxisIndex = -1, LocalTranslationDefaultValue = math.float3(0f, 0f, 1f), LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = math.float3(1f) },
                new SkeletonNode { Id = "Root/Hips/LeftUpLeg", ParentIndex = 1, AxisIndex = -1, LocalTranslationDefaultValue = math.float3(1f, 0f, 0f), LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = math.float3(1f) },
                new SkeletonNode { Id = "Root/Hips/RightUpLeg", ParentIndex = 1, AxisIndex = -1, LocalTranslationDefaultValue = math.float3(-1f, 0f, 0f), LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = math.float3(1f) },
            };
            var destinationRig = new Rig { Value = RigBuilder.CreateRigDefinition(dstSkeletonNodes) };

            var overrides = new RigRemapUtils.OffsetOverrides(2, Collections.Allocator.Temp);
            overrides.AddTranslationOffsetOverride("Root/Hips", new RigTranslationOffset { Rotation = quaternion.identity, Scale = 1f, Space = RigRemapSpace.LocalToRoot });
            overrides.AddRotationOffsetOverride("Root/Hips/RightUpLeg", new RigRotationOffset { PreRotation = quaternion.identity, PostRotation = quaternion.identity, Space = RigRemapSpace.LocalToRoot });
            var remapTable = RigRemapUtils.CreateRemapTable(sourceRig, destinationRig, RigRemapUtils.ChannelFilter.All, overrides);

            Assert.AreEqual(4, remapTable.Value.TranslationMappings.Length);
            Assert.AreEqual(4, remapTable.Value.RotationMappings.Length);
            Assert.AreEqual(4, remapTable.Value.ScaleMappings.Length);
            Assert.AreEqual(1, remapTable.Value.TranslationOffsets.Length - 1); // First offset is mute (since OffsetIndex = 0 is irrelevant)
            Assert.AreEqual(1, remapTable.Value.RotationOffsets.Length - 1); // First offset is mute (since OffsetIndex = 0 is irrelevant)

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, destinationRig, Entity.Null);

            var defaultValuesNode = CreateNode<DefaultValuesNode>();
            Set.SendMessage(defaultValuesNode, DefaultValuesNode.SimulationPorts.Rig, sourceRig);

            var rigRemapper = CreateNode<RigRemapperNode>();
            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, rigRemapper, RigRemapperNode.KernelPorts.Input);

            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.SourceRig, sourceRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.DestinationRig, destinationRig);
            Set.SendMessage(rigRemapper, RigRemapperNode.SimulationPorts.RemapTable, remapTable);

            var entityNode = CreateComponentNode(rigEntity);
            Set.Connect(rigRemapper, RigRemapperNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var srcStream = AnimationStream.FromDefaultValues(sourceRig);
            var dstStream = AnimationStream.CreateReadOnly(destinationRig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());

            Assert.That(dstStream.GetLocalToParentTranslation(0), Is.EqualTo(srcStream.GetLocalToParentTranslation(0)).Using(TranslationComparer), "LocalToParent [Root] translation doesn't match source rig value");
            Assert.That(dstStream.GetLocalToRootTranslation(1), Is.EqualTo(srcStream.GetLocalToRootTranslation(1)).Using(TranslationComparer), "LocalToRoot [Root/Hips] translation doesn't match source rig value");

            Assert.That(dstStream.GetLocalToParentRotation(0), Is.EqualTo(srcStream.GetLocalToParentRotation(0)).Using(RotationComparer), "LocalToParent [Root] rotation doesn't match source rig default value");
            Assert.That(dstStream.GetLocalToRootRotation(3), Is.EqualTo(srcStream.GetLocalToRootRotation(3)).Using(RotationComparer), "LocalToRoot [Root/Hips/RightUpLeg] rotation doesn't match source rig default value");
        }
    }
}
