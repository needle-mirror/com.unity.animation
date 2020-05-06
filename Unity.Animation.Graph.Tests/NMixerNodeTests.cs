using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.TestTools;

namespace Unity.Animation.Tests
{
    public class NMixerNodeTests : AnimationTestsFixture, IPrebuildSetup
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

        private float3 m_ClipRootLocalTranslation3 => new float3(0.0f, 0.0f, 100.0f);
        private quaternion m_ClipRootLocalRotation3 => quaternion.RotateZ(math.radians(90.0f));
        private float3 m_ClipRootLocalScale3 => new float3(1.0f, 10.0f, 0.0f);

        private Rig m_Rig;
        private BlobAssetReference<Clip> m_ConstantRootClip1;
        private BlobAssetReference<Clip> m_ConstantRootClip2;
        private BlobAssetReference<Clip> m_ConstantRootClip3;
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

            return RigBuilder.CreateRigDefinition(skeletonNodes);
        }

        public void Setup()
        {
#if UNITY_EDITOR
            PlaymodeTestsEditorSetup.CreateStreamingAssetsDirectory();

            var constantRootClip1 = CreateConstantDenseClip(
                new[] { ("Root", m_ClipRootLocalTranslation1) },
                new[] { ("Root", m_ClipRootLocalRotation1) },
                new[] { ("Root", m_ClipRootLocalScale1) });

            var blobPath = "NMixerNodeTestsConstantRootClip1.blob";
            BlobFile.WriteBlobAsset(ref constantRootClip1, blobPath);

            var constantHierarchyClip1 = CreateConstantDenseClip(
                new[] { ("Root", m_ClipRootLocalTranslation1), ("Child1", m_ClipChildLocalTranslation1) },
                new[] { ("Root", m_ClipRootLocalRotation1), ("Child1", m_ClipChildLocalRotation1) },
                new[] { ("Root", m_ClipRootLocalScale1), ("Child1", m_ClipChildLocalScale1) });

            blobPath = "NMixerNodeTestsConstantHierarchyClip1.blob";
            BlobFile.WriteBlobAsset(ref constantHierarchyClip1, blobPath);


            var constantRootClip2 = CreateConstantDenseClip(
                new[] { ("Root", m_ClipRootLocalTranslation2) },
                new[] { ("Root", m_ClipRootLocalRotation2) },
                new[] { ("Root", m_ClipRootLocalScale2) });

            blobPath = "NMixerNodeTestsConstantRootClip2.blob";
            BlobFile.WriteBlobAsset(ref constantRootClip2, blobPath);

            var constantHierarchyClip2 = CreateConstantDenseClip(
                new[] { ("Root", m_ClipRootLocalTranslation2), ("Child1", m_ClipChildLocalTranslation2) },
                new[] { ("Root", m_ClipRootLocalRotation2), ("Child1", m_ClipChildLocalRotation2) },
                new[] { ("Root", m_ClipRootLocalScale2), ("Child1", m_ClipChildLocalScale2) });

            blobPath = "NMixerNodeTestsConstantHierarchyClip2.blob";
            BlobFile.WriteBlobAsset(ref constantHierarchyClip2, blobPath);

            var constantRootClip3 = CreateConstantDenseClip(
                new[] { ("Root", m_ClipRootLocalTranslation3) },
                new[] { ("Root", m_ClipRootLocalRotation3) },
                new[] { ("Root", m_ClipRootLocalScale3) });

            blobPath = "NMixerNodeTestsConstantRootClip3.blob";
            BlobFile.WriteBlobAsset(ref constantRootClip3, blobPath);
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
                    var path = "NMixerNodeTestsConstantRootClip1.blob";
                    m_ConstantRootClip1 = BlobFile.ReadBlobAsset<Clip>(path);

                    ClipManager.Instance.GetClipFor(m_Rig, m_ConstantRootClip1);
                }

                // Constant hierarchy clip
                {
                    var path = "NMixerNodeTestsConstantHierarchyClip1.blob";
                    m_ConstantHierarchyClip1 = BlobFile.ReadBlobAsset<Clip>(path);

                    ClipManager.Instance.GetClipFor(m_Rig, m_ConstantHierarchyClip1);
                }
            }

            // Clip #2
            {
                // Constant root clip
                {
                    var path = "NMixerNodeTestsConstantRootClip2.blob";
                    m_ConstantRootClip2 = BlobFile.ReadBlobAsset<Clip>(path);

                    ClipManager.Instance.GetClipFor(m_Rig, m_ConstantRootClip2);
                }

                // Constant hierarchy clip
                {
                    var path = "NMixerNodeTestsConstantHierarchyClip2.blob";
                    m_ConstantHierarchyClip2 = BlobFile.ReadBlobAsset<Clip>(path);

                    ClipManager.Instance.GetClipFor(m_Rig, m_ConstantHierarchyClip2);
                }
            }

            // Clip #3
            {
                // Constant root clip
                {
                    var path = "NMixerNodeTestsConstantRootClip3.blob";
                    m_ConstantRootClip3 = BlobFile.ReadBlobAsset<Clip>(path);

                    ClipManager.Instance.GetClipFor(m_Rig, m_ConstantRootClip3);
                }
            }
        }

        [Test]
        [TestCase(0.0f, Description = "Mix at 0%")]
        [TestCase(0.3f, Description = "Mix at 30%")]
        [TestCase(1.0f, Description = "Mix at 100%")]
        public void CanNMix2SimpleClips(float weight)
        {
            var expectedLocalTranslation = math.lerp(m_ClipRootLocalTranslation1, m_ClipRootLocalTranslation2, weight);

            var q1 = new quaternion(m_ClipRootLocalRotation1.value * (1.0f - weight));
            var q2 = new quaternion(m_ClipRootLocalRotation2.value * weight);
            var expectedLocalRotation = math.normalizesafe(mathex.add(q1, q2));

            var expectedLocalScale = math.lerp(m_ClipRootLocalScale1, m_ClipRootLocalScale2, weight);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipNode1 = CreateNode<ClipNode>();
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Clip, m_ConstantRootClip1);

            var clipNode2 = CreateNode<ClipNode>();
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Clip, m_ConstantRootClip2);

            var mixerNode = CreateNode<NMixerNode>();
            set.SetPortArraySize(mixerNode, NMixerNode.KernelPorts.Inputs, 2);
            set.SetPortArraySize(mixerNode, NMixerNode.KernelPorts.Weights, 2);

            set.Connect(clipNode1, ClipNode.KernelPorts.Output, mixerNode, NMixerNode.KernelPorts.Inputs, 0);
            set.SetData(mixerNode, NMixerNode.KernelPorts.Weights, 0, 1.0f - weight);
            set.Connect(clipNode2, ClipNode.KernelPorts.Output, mixerNode, NMixerNode.KernelPorts.Inputs, 1);
            set.SetData(mixerNode, NMixerNode.KernelPorts.Weights, 1, weight);
            set.SendMessage(mixerNode, NMixerNode.SimulationPorts.Rig, m_Rig);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerNode, NMixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(0), Is.EqualTo(expectedLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(1.0f, 0.0f, 0.0f)]
        [TestCase(0.0f, 1.0f, 0.0f)]
        [TestCase(0.0f, 0.0f, 1.0f)]
        [TestCase(0.1f, 0.3f, 0.5f)]
        [TestCase(0.3f, 0.5f, 0.1f)]
        [TestCase(0.5f, 0.1f, 0.3f)]
        [TestCase(0.5f, 0.5f, 0.5f)]
        public void CanNMix3SimpleClips(float w1, float w2, float w3)
        {
            var sumW = w1 + w2 + w3;

            var expectedLocalTranslation = w1 * m_ClipRootLocalTranslation1 + w2 * m_ClipRootLocalTranslation2 + w3 * m_ClipRootLocalTranslation3;

            var q1 = new quaternion(m_ClipRootLocalRotation1.value * w1);
            var q2 = new quaternion(m_ClipRootLocalRotation2.value * w2);
            var q3 = new quaternion(m_ClipRootLocalRotation3.value * w3);
            var q4 = mathex.select(new quaternion(0, 0, 0, 0), new quaternion(quaternion.identity.value * (1.0f - sumW)), sumW < 1.0f);
            var expectedLocalRotation = math.normalizesafe(mathex.add(mathex.add(mathex.add(q1, q2), q3), q4));

            var s4 = math.select(0.0f, 1.0f - sumW, sumW < 1.0f);
            var expectedLocalScale = w1 * m_ClipRootLocalScale1 + w2 * m_ClipRootLocalScale2 + w3 * m_ClipRootLocalScale3 + new float3(s4);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipNode1 = CreateNode<ClipNode>();
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Clip, m_ConstantRootClip1);

            var clipNode2 = CreateNode<ClipNode>();
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Clip, m_ConstantRootClip2);

            var clipNode3 = CreateNode<ClipNode>();
            set.SendMessage(clipNode3, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode3, ClipNode.SimulationPorts.Clip, m_ConstantRootClip3);

            var mixerNode = CreateNode<NMixerNode>();
            set.SetPortArraySize(mixerNode, NMixerNode.KernelPorts.Inputs, 3);
            set.SetPortArraySize(mixerNode, NMixerNode.KernelPorts.Weights, 3);

            set.Connect(clipNode1, ClipNode.KernelPorts.Output, mixerNode, NMixerNode.KernelPorts.Inputs, 0);
            set.SetData(mixerNode, NMixerNode.KernelPorts.Weights, 0, w1);
            set.Connect(clipNode2, ClipNode.KernelPorts.Output, mixerNode, NMixerNode.KernelPorts.Inputs, 1);
            set.SetData(mixerNode, NMixerNode.KernelPorts.Weights, 1, w2);
            set.Connect(clipNode3, ClipNode.KernelPorts.Output, mixerNode, NMixerNode.KernelPorts.Inputs, 2);
            set.SetData(mixerNode, NMixerNode.KernelPorts.Weights, 2, w3);
            set.SendMessage(mixerNode, NMixerNode.SimulationPorts.Rig, m_Rig);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerNode, NMixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(0), Is.EqualTo(expectedLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.0f, Description = "Mix at 0%")]
        [TestCase(0.3f, Description = "Mix at 30%")]
        [TestCase(1.0f, Description = "Mix at 100%")]
        public void CanNMix2ClipsWithHierarchy(float weight)
        {
            var expectedLocalTranslation = math.lerp(m_ClipChildLocalTranslation1, m_ClipChildLocalTranslation2, weight);

            var q1 = new quaternion(m_ClipChildLocalRotation1.value * (1.0f - weight));
            var q2 = new quaternion(m_ClipChildLocalRotation2.value * weight);
            var expectedLocalRotation = math.normalizesafe(mathex.add(q1, q2));

            var expectedLocalScale = math.lerp(m_ClipChildLocalScale1, m_ClipChildLocalScale2, weight);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipNode1 = CreateNode<ClipNode>();
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Clip, m_ConstantHierarchyClip1);

            var clipNode2 = CreateNode<ClipNode>();
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Clip, m_ConstantHierarchyClip2);

            var mixerNode = CreateNode<NMixerNode>();
            set.SetPortArraySize(mixerNode, NMixerNode.KernelPorts.Inputs, 2);
            set.SetPortArraySize(mixerNode, NMixerNode.KernelPorts.Weights, 2);

            set.Connect(clipNode1, ClipNode.KernelPorts.Output, mixerNode, NMixerNode.KernelPorts.Inputs, 0);
            set.SetData(mixerNode, NMixerNode.KernelPorts.Weights, 0, 1.0f - weight);
            set.Connect(clipNode2, ClipNode.KernelPorts.Output, mixerNode, NMixerNode.KernelPorts.Inputs, 1);
            set.SetData(mixerNode, NMixerNode.KernelPorts.Weights, 1, weight);
            set.SendMessage(mixerNode, NMixerNode.SimulationPorts.Rig, m_Rig);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerNode, NMixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
        }

        [Test]
        public void NMixerOutputDefaultValuesWhenNoInputConnected()
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

            var rig = new Rig { Value = RigBuilder.CreateRigDefinition(skeletonNodes) };

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, rig);

            var set = Set;
            var mixerNode = CreateNode<NMixerNode>();
            set.SendMessage(mixerNode, NMixerNode.SimulationPorts.Rig, rig);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerNode, NMixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            for (int i = 0; i < skeletonNodes.Length; i++)
            {
                Assert.That(streamECS.GetLocalToParentTranslation(i), Is.EqualTo(skeletonNodes[i].LocalTranslationDefaultValue).Using(TranslationComparer));
                Assert.That(streamECS.GetLocalToParentRotation(i), Is.EqualTo(skeletonNodes[i].LocalRotationDefaultValue).Using(RotationComparer));
                Assert.That(streamECS.GetLocalToParentScale(i), Is.EqualTo(skeletonNodes[i].LocalScaleDefaultValue).Using(ScaleComparer));
            }
        }

        [Test]
        [TestCase(0.0f, Description = "Mix at 0%")]
        [TestCase(0.3f, Description = "Mix at 30%")]
        [TestCase(1.0f, Description = "Mix at 100%")]
        public void NMixing2ClipsWithInput0NotConnectedReturnsMixBetweenBindPoseAndClip(float weight)
        {
            var expectedLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation2, weight);
            var expectedLocalRotation = math.slerp(quaternion.identity, m_ClipChildLocalRotation2, weight);
            var expectedLocalScale = math.lerp(new float3(1), m_ClipChildLocalScale2, weight);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var clipNode2 = CreateNode<ClipNode>();
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Clip, m_ConstantHierarchyClip2);

            var mixerNode = CreateNode<NMixerNode>();
            set.SetPortArraySize(mixerNode, NMixerNode.KernelPorts.Inputs, 2);
            set.SetPortArraySize(mixerNode, NMixerNode.KernelPorts.Weights, 2);

            set.Connect(clipNode2, ClipNode.KernelPorts.Output, mixerNode, NMixerNode.KernelPorts.Inputs, 1);
            set.SetData(mixerNode, NMixerNode.KernelPorts.Weights, 1, weight);
            set.SendMessage(mixerNode, NMixerNode.SimulationPorts.Rig, m_Rig);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerNode, NMixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.0f, Description = "Mix at 0%")]
        [TestCase(0.3f, Description = "Mix at 30%")]
        [TestCase(1.0f, Description = "Mix at 100%")]
        public void NMixing2ClipsWithInput1NotConnectedReturnsMixBetweenBindPoseAndClip(float weight)
        {
            var expectedLocalTranslation = math.lerp(m_ClipChildLocalTranslation1, float3.zero, weight);
            var expectedLocalRotation = math.slerp(m_ClipChildLocalRotation1, quaternion.identity, weight);
            var expectedLocalScale = math.lerp(m_ClipChildLocalScale1, new float3(1), weight);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var clipNode1 = CreateNode<ClipNode>();
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Clip, m_ConstantHierarchyClip1);

            var mixerNode = CreateNode<NMixerNode>();
            set.SetPortArraySize(mixerNode, NMixerNode.KernelPorts.Inputs, 2);
            set.SetPortArraySize(mixerNode, NMixerNode.KernelPorts.Weights, 2);

            set.Connect(clipNode1, ClipNode.KernelPorts.Output, mixerNode, NMixerNode.KernelPorts.Inputs, 0);
            set.SetData(mixerNode, NMixerNode.KernelPorts.Weights, 0, 1.0f - weight);
            set.SendMessage(mixerNode, NMixerNode.SimulationPorts.Rig, m_Rig);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerNode, NMixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
        }

        [Test]
        public void NMixerReturnsDefaultPoseWhenSumWeightEqualsZero()
        {
            var expectedLocalTranslation = float3.zero;
            var expectedLocalRotation = quaternion.identity;
            var expectedLocalScale = float3.zero;

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipNode1 = CreateNode<ClipNode>();
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Clip, m_ConstantRootClip1);

            var clipNode2 = CreateNode<ClipNode>();
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Clip, m_ConstantRootClip2);

            var mixerNode = CreateNode<NMixerNode>();
            set.SetPortArraySize(mixerNode, NMixerNode.KernelPorts.Inputs, 2);
            set.SetPortArraySize(mixerNode, NMixerNode.KernelPorts.Weights, 2);

            set.Connect(clipNode1, ClipNode.KernelPorts.Output, mixerNode, NMixerNode.KernelPorts.Inputs, 0);
            set.SetData(mixerNode, NMixerNode.KernelPorts.Weights, 0, 0);
            set.Connect(clipNode2, ClipNode.KernelPorts.Output, mixerNode, NMixerNode.KernelPorts.Inputs, 1);
            set.SetData(mixerNode, NMixerNode.KernelPorts.Weights, 1, 0);
            set.SendMessage(mixerNode, NMixerNode.SimulationPorts.Rig, m_Rig);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerNode, NMixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            var defaultStream = AnimationStream.FromDefaultValues(m_Rig);
            for (int i = 0; i < m_Rig.Value.Value.Skeleton.BoneCount; i++)
            {
                Assert.That(streamECS.GetLocalToParentTranslation(i), Is.EqualTo(defaultStream.GetLocalToParentTranslation(i)));
                Assert.That(streamECS.GetLocalToParentRotation(i), Is.EqualTo(defaultStream.GetLocalToParentRotation(i)));
                Assert.That(streamECS.GetLocalToParentScale(i), Is.EqualTo(defaultStream.GetLocalToParentScale(i)));
            }
        }

        [Test]
        public void NMixerNodeNormalizeRotation()
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipNode1 = CreateNode<ClipNode>();
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Clip, m_ConstantRootClip1);

            var mixerNode = CreateNode<NMixerNode>();
            set.SetPortArraySize(mixerNode, NMixerNode.KernelPorts.Inputs, 1);
            set.SetPortArraySize(mixerNode, NMixerNode.KernelPorts.Weights, 1);

            set.SendMessage(mixerNode, NMixerNode.SimulationPorts.Rig, m_Rig);
            set.Connect(clipNode1, ClipNode.KernelPorts.Output, mixerNode, NMixerNode.KernelPorts.Inputs, 0);
            // Set explicitly sumweight to 1 to force MixerEndNode to normalize rotation
            set.SetData(mixerNode, NMixerNode.KernelPorts.Weights, 0, 1.0f);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerNode, NMixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            for (int i = 0; i < m_Rig.Value.Value.Skeleton.BoneCount; i++)
            {
                var length = math.length(streamECS.GetLocalToParentRotation(i));

                Assert.That(length, Is.EqualTo(1.0f));
            }
        }

        [Test]
        [TestCase(0.0f, Description = "Mix at 0%")]
        [TestCase(0.33f, Description = "Mix at 33%")]
        [TestCase(1.0f, Description = "Mix at 100%")]
        public void CanNMixOneClip(float weight)
        {
            var expectedLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation1, weight);

            var q1 = new quaternion(m_ClipChildLocalRotation1.value * weight);
            var q2 = mathex.select(new quaternion(0, 0, 0, 0), new quaternion(quaternion.identity.value * (1.0f - weight)), weight < 1.0f);
            var expectedLocalRotation = math.normalize(mathex.add(q1, q2));
            var expectedLocalScale = math.lerp(mathex.one(), m_ClipChildLocalScale1, weight);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var clipNode1 = CreateNode<ClipNode>();
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Clip, m_ConstantHierarchyClip1);

            var mixerNode = CreateNode<NMixerNode>();
            set.SetPortArraySize(mixerNode, NMixerNode.KernelPorts.Inputs, 1);
            set.SetPortArraySize(mixerNode, NMixerNode.KernelPorts.Weights, 1);

            set.SendMessage(mixerNode, NMixerNode.SimulationPorts.Rig, m_Rig);
            set.Connect(clipNode1, ClipNode.KernelPorts.Output, mixerNode, NMixerNode.KernelPorts.Inputs, 0);
            set.SetData(mixerNode, NMixerNode.KernelPorts.Weights, 0, weight);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerNode, NMixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
        }
    }
}
