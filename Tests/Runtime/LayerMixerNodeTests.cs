using NUnit.Framework;
using Unity.Mathematics;
using Unity.Entities;

using UnityEngine.TestTools;


namespace Unity.Animation.Tests
{
    public class LayerMixerNodeTests : AnimationTestsFixture, IPrebuildSetup
    {
        private float3 m_ClipRootLocalTranslation1 => new float3(100.0f, 0.0f, 0.0f);
        private quaternion m_ClipRootLocalRotation1 => quaternion.RotateX(math.radians(90.0f));
        private float3 m_ClipRootLocalScale1 => new float3(10.0f, 1.0f, 1.0f);

        private float3 m_ClipChildLocalTranslation1 => new float3(0.0f, 100.0f, 0.0f);
        private quaternion m_ClipChildLocalRotation1 => quaternion.RotateY(math.radians(90.0f));
        private float3 m_ClipChildLocalScale1 => new float3(1.0f, 1.0f, 10.0f);

        private float3 m_ClipRootLocalTranslation2 => new float3(0.0f, 100.0f, 0.0f);
        private quaternion m_ClipRootLocalRotation2 => quaternion.RotateY(math.radians(90.0f));
        private float3 m_ClipRootLocalScale2 => new float3(1.0f, 1.0f, 10.0f);

        private float3 m_ClipChildLocalTranslation2 => new float3(100.0f, 0.0f, 0.0f);
        private quaternion m_ClipChildLocalRotation2 => quaternion.RotateX(math.radians(90.0f));
        private float3 m_ClipChildLocalScale2 => new float3(1.0f, 10.0f, 1.0f);

        private Rig m_Rig;
        private BlobAssetReference<RigDefinition> m_AnotherRig;
        private BlobAssetReference<Clip> m_ConstantClip1;
        private BlobAssetReference<Clip> m_ConstantClip2;

        private static BlobAssetReference<RigDefinition> CreateTestRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode
                {
                    ParentIndex = -1, Id = "Root", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(0,0,0),
                    LocalRotationDefaultValue = new quaternion(0,0,0,1),
                    LocalScaleDefaultValue = new float3(1,1,1),
                },
                new SkeletonNode
                {
                    ParentIndex = 0, Id = "Child1", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(0,0.95f,0),
                    LocalRotationDefaultValue = new quaternion(0,0,0,1),
                    LocalScaleDefaultValue = new float3(2,2,2),
                },
                new SkeletonNode
                {
                    ParentIndex = 0, Id = "Child2", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(0,0.10f,0),
                    LocalRotationDefaultValue = new quaternion(0,0,0,1),
                    LocalScaleDefaultValue = new float3(3,3,3),
                },
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes);
        }

        private static BlobAssetReference<RigDefinition> CreateAnotherRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode
                {
                    ParentIndex = -1, Id = "Root", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(0,0,0),
                    LocalRotationDefaultValue = new quaternion(0,0,0,1),
                    LocalScaleDefaultValue = new float3(1,1,1),
                },
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes);
        }


        public void Setup()
        {
#if UNITY_EDITOR
            PlaymodeTestsEditorSetup.CreateStreamingAssetsDirectory();

            var denseClip1 = CreateConstantDenseClip(
                        new[] { ("Root", m_ClipRootLocalTranslation1), ("Child1", m_ClipChildLocalTranslation1) },
                        new[] { ("Root", m_ClipRootLocalRotation1), ("Child1", m_ClipChildLocalRotation1) },
                        new[] { ("Root", m_ClipRootLocalScale1),    ("Child1", m_ClipChildLocalScale1) });

            var blobPath = "LayerMixerNodeTestsDenseClip1.blob";
            BlobFile.WriteBlobAsset(ref denseClip1, blobPath);

            var denseClip2 = CreateConstantDenseClip(
                        new[] { ("Root", m_ClipRootLocalTranslation2), ("Child1", m_ClipChildLocalTranslation2) },
                        new[] { ("Root", m_ClipRootLocalRotation2), ("Child1", m_ClipChildLocalRotation2) },
                        new[] { ("Root", m_ClipRootLocalScale2),    ("Child1", m_ClipChildLocalScale2) });

            blobPath = "LayerMixerNodeTestsDenseClip2.blob";
            BlobFile.WriteBlobAsset(ref denseClip2, blobPath);
#endif
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // Create rig
            m_Rig = new Rig { Value = CreateTestRigDefinition() };
            m_AnotherRig = CreateAnotherRigDefinition();

            // Clip #1
            {
                var path = "LayerMixerNodeTestsDenseClip1.blob";
                m_ConstantClip1 = BlobFile.ReadBlobAsset<Clip>(path);
                ClipManager.Instance.GetClipFor(m_Rig, m_ConstantClip1);
            }

            // Clip #2
            {
                var path = "LayerMixerNodeTestsDenseClip2.blob";
                m_ConstantClip2 = BlobFile.ReadBlobAsset<Clip>(path);
                ClipManager.Instance.GetClipFor(m_Rig, m_ConstantClip2);
            }
        }

        [Test]
        public void CanSetRigDefinition()
        {
            var set = Set;
            var layerMixer = CreateNode<LayerMixerNode>();

            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, m_Rig);

            var otherRig = set.GetDefinition(layerMixer).ExposeKernelData(layerMixer).RigDefinition;

            Assert.That(otherRig.Value.GetHashCode(), Is.EqualTo(m_Rig.Value.Value.GetHashCode()));
        }

        [Test]
        public void CanSetLayerCount()
        {
            var set = Set;
            var layerMixer = CreateNode<LayerMixerNode>();

            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.LayerCount, (ushort) 5 );

            var layerCount = set.GetDefinition(layerMixer).ExposeKernelData(layerMixer).LayerCount;

            Assert.That(layerCount, Is.EqualTo(5));
        }

        [Test]
        public void CannotSendMessageOnPortArrayBeforeSettingLayerCount()
        {
            var set = Set;
            var layerMixer = CreateNode<LayerMixerNode>();

            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, m_Rig);

            Assert.Throws<System.IndexOutOfRangeException>(() => set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendingModes, 0, BlendingMode.Override));
        }

        [Test]
        public unsafe void CanSetBlendingMode()
        {
            var set = Set;
            var layerMixer = CreateNode<LayerMixerNode>();

            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.LayerCount, (ushort) 4 );

            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendingModes, 0, BlendingMode.Override);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendingModes, 1, BlendingMode.Override);

            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendingModes, 2, BlendingMode.Additive);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendingModes, 3, BlendingMode.Additive);

            var blendingModes = set.GetDefinition(layerMixer).ExposeKernelData(layerMixer).BlendingModes;

            Assert.That(blendingModes->Length, Is.EqualTo(4));


            Assert.AreEqual(BlendingMode.Override, LayerMixerNode.ItemAt<BlendingMode>(blendingModes, 0));
            Assert.AreEqual(BlendingMode.Override, LayerMixerNode.ItemAt<BlendingMode>(blendingModes, 1));

            Assert.AreEqual(BlendingMode.Additive, LayerMixerNode.ItemAt<BlendingMode>(blendingModes, 2));
            Assert.AreEqual(BlendingMode.Additive, LayerMixerNode.ItemAt<BlendingMode>(blendingModes, 3));
        }

        [Test]
        public void OutputDefaultValuesWhenNoInputConnected()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode
                {
                    ParentIndex = -1, Id = "Root", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(1,2,3),
                    LocalRotationDefaultValue = new quaternion(1,0,0,0),
                    LocalScaleDefaultValue = new float3(2,2,2),
                },
                new SkeletonNode
                {
                    ParentIndex = 0, Id = "Child1", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(4,5,6),
                    LocalRotationDefaultValue = new quaternion(0,1,0,0),
                    LocalScaleDefaultValue = new float3(3,3,3),
                },
                new SkeletonNode
                {
                    ParentIndex = 0, Id = "Child2", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(7,8,9),
                    LocalRotationDefaultValue = new quaternion(0,0,1,0),
                    LocalScaleDefaultValue = new float3(4,4,4),
                }
            };

            var rig = new Rig { Value = RigBuilder.CreateRigDefinition(skeletonNodes) };

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, rig);

            var set = Set;
            var layerMixer = CreateNode<LayerMixerNode>();
            var entityNode = CreateComponentNode(entity);

            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, rig);
            set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
                );

            for(int i=0;i<skeletonNodes.Length;i++)
            {
                Assert.That(streamECS.GetLocalToParentTranslation(i), Is.EqualTo(skeletonNodes[i].LocalTranslationDefaultValue).Using(TranslationComparer));
                Assert.That(streamECS.GetLocalToParentRotation(i), Is.EqualTo(skeletonNodes[i].LocalRotationDefaultValue).Using(RotationComparer));
                Assert.That(streamECS.GetLocalToParentScale(i), Is.EqualTo(skeletonNodes[i].LocalScaleDefaultValue).Using(ScaleComparer));
            }
        }

        [Test]
        public void CanMixOneOverrideLayer()
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var clipNode = CreateNode<ClipNode>();
            var entityNode = CreateComponentNode(entity);

            set.SendMessage(clipNode, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode, ClipNode.SimulationPorts.Clip, m_ConstantClip1);

            var layerMixer = CreateNode<LayerMixerNode>();
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.LayerCount, (ushort) 1);
            set.SetData(layerMixer, LayerMixerNode.KernelPorts.Weights, 0, 1.0f);

            set.Connect(clipNode, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Inputs, 0);
            set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
                );

            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(m_ClipRootLocalTranslation1).Using(TranslationComparer), "Root localTranslation doesn't match clip localTranslation");
            Assert.That(streamECS.GetLocalToParentRotation(0), Is.EqualTo(m_ClipRootLocalRotation1).Using(RotationComparer), "Root localRotation doesn't match clip localRotation");
            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(m_ClipRootLocalScale1).Using(ScaleComparer), "Root localScale doesn't match clip localScale");

            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(m_ClipChildLocalTranslation1).Using(TranslationComparer), "Child1 localTranslation doesn't match clip localTranslation");
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(m_ClipChildLocalRotation1).Using(RotationComparer), "Child1 localRotation doesn't match clip localRotation");
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(m_ClipChildLocalScale1).Using(ScaleComparer), "Child1 localScale doesn't match clip localScale");
        }

        [Test]
        public void CanMixMultipleOverrideLayer()
        {
            const float k_BlendValue = 0.3f;
            var expectedLocalTranslation = math.lerp(m_ClipChildLocalTranslation1, m_ClipChildLocalTranslation2, k_BlendValue);
            var expectedLocalRotation = mathex.lerp(m_ClipChildLocalRotation1, m_ClipChildLocalRotation2, k_BlendValue);
            var expectedLocalScale = math.lerp(m_ClipChildLocalScale1, m_ClipChildLocalScale2, k_BlendValue);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var clipNode1 = CreateNode<ClipNode>();
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Clip, m_ConstantClip1);

            var clipNode2 = CreateNode<ClipNode>();
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Clip, m_ConstantClip2);

            var layerMixer = CreateNode<LayerMixerNode>();
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.LayerCount, (ushort) 2);
            set.SetData(layerMixer, LayerMixerNode.KernelPorts.Weights, 0, 1.0f);
            set.SetData(layerMixer, LayerMixerNode.KernelPorts.Weights, 1, k_BlendValue);

            set.Connect(clipNode1, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Inputs, 0);
            set.Connect(clipNode2, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Inputs, 1);

            var entityNode = CreateComponentNode(entity);

            set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
                );

            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer), "Child1 localTranslation doesn't match blended value");
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedLocalRotation).Using(RotationComparer), "Child1 localRotation doesn't match blended value");
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedLocalScale).Using(ScaleComparer), "Child1 localScale doesn't match blended value");
        }

        [Test]
        [TestCase(0.0f)]
        [TestCase(0.5f)]
        [TestCase(1.0f)]
        public void CanMixOneAdditiveLayer(float layerWeight)
        {
            var defaultStream = AnimationStream.FromDefaultValues(m_Rig);

            var expectedRootLocalTranslation1 = defaultStream.GetLocalToParentTranslation(0) + (m_ClipRootLocalTranslation1 * layerWeight);
            var expectedRootLocalRotation1 = mathex.mul(defaultStream.GetLocalToParentRotation(0), mathex.quatWeight(m_ClipRootLocalRotation1, layerWeight));
            var expectedRootLocalScale1 = defaultStream.GetLocalToParentScale(0) + (m_ClipRootLocalScale1 * layerWeight);

            var expectedChildLocalTranslation1 = defaultStream.GetLocalToParentTranslation(1) + (m_ClipChildLocalTranslation1 * layerWeight);
            var expectedChildLocalRotation1 = mathex.mul(defaultStream.GetLocalToParentRotation(1), mathex.quatWeight(m_ClipChildLocalRotation1, layerWeight));
            var expectedChildLocalScale1 = defaultStream.GetLocalToParentScale(1) + (m_ClipChildLocalScale1 * layerWeight);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode, ClipNode.SimulationPorts.Clip, m_ConstantClip1);

            var layerMixer = CreateNode<LayerMixerNode>();
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.LayerCount, (ushort) 1);
            set.SetData(layerMixer, LayerMixerNode.KernelPorts.Weights, 0, layerWeight);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendingModes, 0, BlendingMode.Additive);

            set.Connect(clipNode, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Inputs, 0);

            var entityNode = CreateComponentNode(entity);
            set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
                );

            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(expectedRootLocalTranslation1).Using(TranslationComparer), "Root localTranslation doesn't match expected value");
            Assert.That(streamECS.GetLocalToParentRotation(0), Is.EqualTo(expectedRootLocalRotation1).Using(RotationComparer), "Root localRotation doesn't match expected value");
            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(expectedRootLocalScale1).Using(ScaleComparer), "Root localScale doesn't match expected value");

            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedChildLocalTranslation1).Using(TranslationComparer), "Child1 localTranslation doesn't match expected value");
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedChildLocalRotation1).Using(RotationComparer), "Child1 localRotation doesn't match expected value");
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedChildLocalScale1).Using(ScaleComparer), "Child1 localScale doesn't match expected value");
        }

        [Test]
        [TestCase(0.0f, 0.0f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(1.0f, 1.0f)]
        [TestCase(0.0f, 0.5f)]
        [TestCase(0.0f, 1.0f)]
        public void CanMixMultipleAdditiveLayer(float layer1Weight, float layer2Weight)
        {
            var defaultStream = AnimationStream.FromDefaultValues(m_Rig);

            var expectedRootLocalTranslation1 = defaultStream.GetLocalToParentTranslation(0) + (m_ClipRootLocalTranslation1 * layer1Weight) + (m_ClipRootLocalTranslation2 * layer2Weight);
            var expectedRootLocalRotation1 = mathex.mul(math.mul(defaultStream.GetLocalToParentRotation(0),  mathex.quatWeight(m_ClipRootLocalRotation1, layer1Weight)), mathex.quatWeight(m_ClipRootLocalRotation2, layer2Weight));
            var expectedRootLocalScale1 = defaultStream.GetLocalToParentScale(0) + (m_ClipRootLocalScale1 * layer1Weight) + (m_ClipRootLocalScale2 * layer2Weight);

            var expectedChildLocalTranslation1 = defaultStream.GetLocalToParentTranslation(1) + (m_ClipChildLocalTranslation1 * layer1Weight) + (m_ClipChildLocalTranslation2 * layer2Weight);
            var expectedChildLocalRotation1 = mathex.mul(math.mul(defaultStream.GetLocalToParentRotation(1),  mathex.quatWeight(m_ClipChildLocalRotation1, layer1Weight)), mathex.quatWeight(m_ClipChildLocalRotation2, layer2Weight));
            var expectedChildLocalScale1 = defaultStream.GetLocalToParentScale(1) + (m_ClipChildLocalScale1 * layer1Weight) + (m_ClipChildLocalScale2 * layer2Weight);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode, ClipNode.SimulationPorts.Clip, m_ConstantClip1);

            var clipNode2 = CreateNode<ClipNode>();
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Clip, m_ConstantClip2);

            var layerMixer = CreateNode<LayerMixerNode>();
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.LayerCount, (ushort) 2);
            set.SetData(layerMixer, LayerMixerNode.KernelPorts.Weights, 0, layer1Weight);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendingModes, 0, BlendingMode.Additive);
            set.SetData(layerMixer, LayerMixerNode.KernelPorts.Weights, 1, layer2Weight);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendingModes, 1, BlendingMode.Additive);

            set.Connect(clipNode, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Inputs, 0);
            set.Connect(clipNode2, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Inputs, 1);

            var entityNode = CreateComponentNode(entity);
            set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
                );

            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(expectedRootLocalTranslation1).Using(TranslationComparer), "Root localTranslation doesn't match expected value");
            Assert.That(streamECS.GetLocalToParentRotation(0), Is.EqualTo(expectedRootLocalRotation1).Using(RotationComparer), "Root localRotation doesn't match expected value");
            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(expectedRootLocalScale1).Using(ScaleComparer), "Root localScale doesn't match expected value");

            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedChildLocalTranslation1).Using(TranslationComparer), "Child1 localTranslation doesn't match expected value");
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedChildLocalRotation1).Using(RotationComparer), "Child1 localRotation doesn't match expected value");
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedChildLocalScale1).Using(ScaleComparer), "Child1 localScale doesn't match expected value");
        }

        [Test]
        [TestCase(0.0f, 0.0f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(1.0f, 1.0f)]
        [TestCase(0.0f, 0.5f)]
        [TestCase(0.0f, 1.0f)]
        public void CanMixMultipleAdditiveLayerAndMaskChannel (float layer0Weight, float layer1Weight)
        {
            var defaultStream = AnimationStream.FromDefaultValues(m_Rig);

            // Root is masked on layer 2
            var expectedRootLocalTranslation1 = defaultStream.GetLocalToParentTranslation(0) + (m_ClipRootLocalTranslation1 * layer0Weight);
            var expectedRootLocalRotation1 = mathex.mul(defaultStream.GetLocalToParentRotation(0), mathex.quatWeight(m_ClipRootLocalRotation1, layer0Weight));
            var expectedRootLocalScale1 = defaultStream.GetLocalToParentScale(0) + (m_ClipRootLocalScale1 * layer0Weight);

            var expectedChildLocalTranslation1 = defaultStream.GetLocalToParentTranslation(1) + (m_ClipChildLocalTranslation1 * layer0Weight) + (m_ClipChildLocalTranslation2 * layer1Weight);
            var expectedChildLocalRotation1 = mathex.mul(math.mul(defaultStream.GetLocalToParentRotation(1), mathex.quatWeight(m_ClipChildLocalRotation1, layer0Weight)), mathex.quatWeight(m_ClipChildLocalRotation2, layer1Weight));
            var expectedChildLocalScale1 = defaultStream.GetLocalToParentScale(1) + (m_ClipChildLocalScale1 * layer0Weight) + (m_ClipChildLocalScale2 * layer1Weight);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode, ClipNode.SimulationPorts.Clip, m_ConstantClip1);

            var clipNode2 = CreateNode<ClipNode>();
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Clip, m_ConstantClip2);

            var weightNode = CreateNode<WeightBuilderNode>();
            set.SendMessage(weightNode, WeightBuilderNode.SimulationPorts.Rig, m_Rig);
            set.SetPortArraySize(weightNode, WeightBuilderNode.KernelPorts.ChannelIndices, 3);
            set.SetPortArraySize(weightNode, WeightBuilderNode.KernelPorts.ChannelWeights, 3);

            // layer 2 only affect Child1 translation, rotation, and scale
            set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelIndices, 0, m_Rig.Value.Value.Bindings.TranslationBindingIndex + 1);
            set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelIndices, 1, m_Rig.Value.Value.Bindings.RotationBindingIndex + 1);
            set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelIndices, 2, m_Rig.Value.Value.Bindings.ScaleBindingIndex + 1);

            set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelWeights, 0, 1f);
            set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelWeights, 1, 1f);
            set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelWeights, 2, 1f);

            var layerMixer = CreateNode<LayerMixerNode>();
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.LayerCount, (ushort) 2);
            set.SetData(layerMixer, LayerMixerNode.KernelPorts.Weights, 0, layer0Weight);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendingModes, 0, BlendingMode.Additive);

            set.SetData(layerMixer, LayerMixerNode.KernelPorts.Weights, 1, layer1Weight);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendingModes, 1, BlendingMode.Additive);

            set.Connect(clipNode, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Inputs, 0);
            set.Connect(clipNode2, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Inputs, 1);
            set.Connect(weightNode, WeightBuilderNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.WeightMasks, 1);

            var entityNode = CreateComponentNode(entity);
            set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
                );

            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(expectedRootLocalTranslation1).Using(TranslationComparer), "Root localTranslation doesn't match expected value");
            Assert.That(streamECS.GetLocalToParentRotation(0), Is.EqualTo(expectedRootLocalRotation1).Using(RotationComparer), "Root localRotation doesn't match expected value");
            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(expectedRootLocalScale1).Using(ScaleComparer), "Root localScale doesn't match expected value");

            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedChildLocalTranslation1).Using(TranslationComparer), "Child1 localTranslation doesn't match expected value");
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedChildLocalRotation1).Using(RotationComparer), "Child1 localRotation doesn't match expected value");
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedChildLocalScale1).Using(ScaleComparer), "Child1 localScale doesn't match expected value");
        }

        [Test]
        public void BlendingInAnUnconnectedLayerDoesntCrash()
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var layerMixer = CreateNode<LayerMixerNode>();
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.LayerCount, (ushort) 4);
            set.SetData(layerMixer, LayerMixerNode.KernelPorts.Weights, 0, 1.0f);
            set.SetData(layerMixer, LayerMixerNode.KernelPorts.Weights, 1, 1.0f);
            set.SetData(layerMixer, LayerMixerNode.KernelPorts.Weights, 2, 1.0f);
            set.SetData(layerMixer, LayerMixerNode.KernelPorts.Weights, 3, 1.0f);

            var entityNode = CreateComponentNode(entity);
            set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            Assert.DoesNotThrow(() => m_AnimationGraphSystem.Update());
        }

        [Test]
        public void CanMaskChannel()
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode, ClipNode.SimulationPorts.Clip, m_ConstantClip1);

            var weightNode = CreateNode<WeightBuilderNode>();
            set.SendMessage(weightNode, WeightBuilderNode.SimulationPorts.Rig, m_Rig);
            set.SetPortArraySize(weightNode, WeightBuilderNode.KernelPorts.ChannelIndices, 3);
            set.SetPortArraySize(weightNode, WeightBuilderNode.KernelPorts.ChannelWeights, 3);

            set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelIndices, 0, m_Rig.Value.Value.Bindings.TranslationBindingIndex + 1);
            set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelIndices, 1, m_Rig.Value.Value.Bindings.RotationBindingIndex + 1);
            set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelIndices, 2, m_Rig.Value.Value.Bindings.ScaleBindingIndex + 1);

            set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelWeights, 0, 1f);
            set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelWeights, 1, 1f);
            set.SetData(weightNode, WeightBuilderNode.KernelPorts.ChannelWeights, 2, 1f);

            var layerMixer = CreateNode<LayerMixerNode>();
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.LayerCount, (ushort) 1);
            set.SetData(layerMixer, LayerMixerNode.KernelPorts.Weights, 0, 1.0f);

            set.Connect(clipNode, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Inputs, 0);
            set.Connect(weightNode, WeightBuilderNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.WeightMasks, 0);

            var entityNode = CreateComponentNode(entity);
            set.Connect(layerMixer, LayerMixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
                );

            var defaultStream = AnimationStream.FromDefaultValues(m_Rig);
            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(defaultStream.GetLocalToParentTranslation(0)).Using(TranslationComparer), "Root localTranslation doesn't match DefaultValues localTranslation");
            Assert.That(streamECS.GetLocalToParentRotation(0), Is.EqualTo(defaultStream.GetLocalToParentRotation(0)).Using(RotationComparer), "Root localRotation doesn't match DefaultValues localRotation");
            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(defaultStream.GetLocalToParentScale(0)).Using(ScaleComparer), "Root localScale doesn't match DefaultValues localScale");

            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(m_ClipChildLocalTranslation1).Using(TranslationComparer), "Child1 localTranslation doesn't match clip localTranslation");
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(m_ClipChildLocalRotation1).Using(RotationComparer), "Child1 localRotation doesn't match clip localRotation");
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(m_ClipChildLocalScale1).Using(ScaleComparer), "Child1 localScale doesn't match clip localScale");
        }
    }
}
