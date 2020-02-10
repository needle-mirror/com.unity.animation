using NUnit.Framework;
using Unity.Mathematics;

namespace Unity.Animation.Tests
{
    public class RigRemapperNodeTests : AnimationTestsFixture
    {
        float3      m_ExpectedSourceTranslation => new float3(10.0f, 0.0f, 0.0f);
        quaternion  m_ExpectedSourceRotation => new quaternion(1.0f, 0.0f, 0.0f, 0.0f);
        float3      m_ExpectedSourceScale => new float3(10.0f, 1.0f, 1.0f);
        float       m_ExpectedSourceFloat => 10.0f;
        int         m_ExpectedSourceInt => 10;

        float3      m_ExpectedDestinationTranslation => new float3(0.0f, 0.0f, 5.0f);
        quaternion  m_ExpectedDestinationRotation => new quaternion(0.0f, 0.0f, 1.0f, 0.0f);
        float3      m_ExpectedDestinationScale => new float3(1.0f, 1.0f, 5.0f);
        float       m_ExpectedDestinationFloat => 5.0f;
        int         m_ExpectedDestinationInt => 5;

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

            var otherRig = Set.GetDefinition(rigRemapper).ExposeKernelData(rigRemapper).SourceRigDefinition;

            Assert.That(otherRig.Value.GetHashCode(), Is.EqualTo(rig.Value.Value.GetHashCode()));
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

            var otherRig = Set.GetDefinition(rigRemapper).ExposeKernelData(rigRemapper).DestinationRigDefinition;

            Assert.That(otherRig.Value.GetHashCode(), Is.EqualTo(rig.Value.Value.GetHashCode()));
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
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, destinationRig);

            var rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new [] {
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

            m_Manager.AddComponent<PreAnimationGraphTag>(rigEntity);

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
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, destinationRig);

            var rigRemapQuery = new RigRemapQuery
            {
                TranslationChannels = new [] {
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

            m_Manager.AddComponent<PreAnimationGraphTag>(rigEntity);

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
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, destinationRig);

            var rigRemapQuery = new RigRemapQuery
            {
                TranslationChannels = new [] {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child", DestinationId = "AnotherChild", OffsetIndex = 1 }
                },

                TranslationOffsets = new []
                {
                    new RigTranslationOffset(),
                    new RigTranslationOffset { Scale = 2, Rotation = math.normalize(math.quaternion(0,1,0,1)) }
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

            m_Manager.AddComponent<PreAnimationGraphTag>(rigEntity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
                );

            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(math.mul(rigRemapQuery.TranslationOffsets[1].Rotation,m_ExpectedSourceTranslation*rigRemapQuery.TranslationOffsets[1].Scale)).Using(TranslationComparer), "Channel localTranslation doesn't match source rig default value with rig offset");
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
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, destinationRig);

            var rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new [] {
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

            m_Manager.AddComponent<PreAnimationGraphTag>(rigEntity);

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
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, destinationRig);

            var rigRemapQuery = new RigRemapQuery
            {
                RotationChannels = new [] {
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

            m_Manager.AddComponent<PreAnimationGraphTag>(rigEntity);

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
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, destinationRig);

            var rigRemapQuery = new RigRemapQuery
            {
                RotationChannels = new []
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Child", DestinationId = "AnotherChild", OffsetIndex = 1 }
                },

                RotationOffsets = new []
                {
                    new RigRotationOffset(),
                    new RigRotationOffset { PreRotation = math.normalize(math.quaternion(1,2,3,4)), PostRotation = math.normalize(math.quaternion(5,6,7,8))}
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

            m_Manager.AddComponent<PreAnimationGraphTag>(rigEntity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
                );

            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(math.mul(rigRemapQuery.RotationOffsets[1].PreRotation,math.mul(m_ExpectedSourceRotation,rigRemapQuery.RotationOffsets[1].PostRotation))).Using(RotationComparer), "Channel localRotation doesn't match destination rig default value with rig rotation offset");
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
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, destinationRig);

            var rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new [] {
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

            m_Manager.AddComponent<PreAnimationGraphTag>(rigEntity);

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
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, destinationRig);

            var rigRemapQuery = new RigRemapQuery
            {
                ScaleChannels = new [] {
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

            m_Manager.AddComponent<PreAnimationGraphTag>(rigEntity);

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
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, destinationRig);

            var rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new [] {
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

            m_Manager.AddComponent<PreAnimationGraphTag>(rigEntity);

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
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, destinationRig);

            var rigRemapQuery = new RigRemapQuery
            {
                FloatChannels = new [] {
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

            m_Manager.AddComponent<PreAnimationGraphTag>(rigEntity);

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
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, destinationRig);

            var rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new [] {
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

            m_Manager.AddComponent<PreAnimationGraphTag>(rigEntity);

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
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, destinationRig);

            var rigRemapQuery = new RigRemapQuery
            {
                IntChannels = new [] {
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

            m_Manager.AddComponent<PreAnimationGraphTag>(rigEntity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                destinationRig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
                );

            Assert.That(streamECS.GetInt(0), Is.EqualTo(m_ExpectedSourceInt), "Channel int 0 doesn't match source rig default value");
            Assert.That(streamECS.GetInt(1), Is.EqualTo(m_ExpectedDestinationInt), "Channel int 1 doesn't match destination rig default value");
            Assert.That(streamECS.GetInt(2), Is.EqualTo(m_ExpectedSourceInt), "Channel int 2 doesn't match source rig default value");
        }
    }
}
