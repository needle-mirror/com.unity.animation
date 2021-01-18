using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.Animation.Hybrid;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Animation.Tests
{
    public class ClipNodeTests : AnimationTestsFixture, IPrebuildSetup
    {
        private float3 m_ClipRootLocalTranslation => new float3(100.0f, 0.0f, 0.0f);
        private quaternion m_ClipRootLocalRotation => quaternion.RotateX(math.radians(90.0f));
        private float3 m_ClipRootLocalScale => new float3(10.0f, 1.0f, 1.0f);

        private float3 m_ClipChildLocalTranslation => new float3(0.0f, 100.0f, 0.0f);
        private quaternion m_ClipChildLocalRotation => quaternion.RotateY(math.radians(90.0f));
        private float3 m_ClipChildLocalScale => new float3(1.0f, 1.0f, 10.0f);

        public const float m_LinearHierarchyNoRootClipDuration = 1.11f;
        private float m_IKWeight => 1.0f;
        private int m_DefaultType => 10;

        private Rig m_Rig;
        private BlobAssetReference<Clip> m_ConstantRootClip;
        private BlobAssetReference<Clip> m_ConstantHierarchyClip;
        private BlobAssetReference<Clip> m_ConstantPartialClip;
        private BlobAssetReference<Clip> m_LinearRootClip;
        private BlobAssetReference<Clip> m_LinearHierarchyClip;
        private BlobAssetReference<Clip> m_LinearHierarchyNoRootClip;
        private BlobAssetReference<Clip> m_LinearPartialClip;
        private BlobAssetReference<Clip> m_ConstantSOAClip;

        private static BlobAssetReference<RigDefinition> CreateTestRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode { ParentIndex = -1, Id = TransformChannelID("Root"), AxisIndex = -1 },
                new SkeletonNode { ParentIndex = 0, Id = TransformChannelID("Child1"), AxisIndex = -1 },
                new SkeletonNode { ParentIndex = 1, Id = TransformChannelID("Child2"), AxisIndex = -1 },
                new SkeletonNode { ParentIndex = 2, Id = TransformChannelID("Child3"), AxisIndex = -1 },
                new SkeletonNode { ParentIndex = 3, Id = TransformChannelID("Child4"), AxisIndex = -1 },
            };

            var channels = new IAnimationChannel[]
            {
                new FloatChannel { Id = FloatChannelID("IKWeight"), DefaultValue = 0.0f },
                new IntChannel { Id = IntegerChannelID("Type"), DefaultValue = 10 }
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes, null, channels);
        }

        public void Setup()
        {
#if UNITY_EDITOR
            PlaymodeTestsEditorSetup.CreateStreamingAssetsDirectory();

            {
                var denseClip = CreateConstantDenseClip(
                    new[] { ("Root", m_ClipRootLocalTranslation) },
                    new[] { ("Root", m_ClipRootLocalRotation) },
                    new[] { ("Root", m_ClipRootLocalScale) });

                var blobPath = "ClipNodeTestsDenseClip1.blob";
                BlobFile.WriteBlobAsset(ref denseClip, blobPath);
            }
            {
                var denseClip = CreateConstantDenseClip(
                    new[] { ("Root", m_ClipRootLocalTranslation), ("Child1", m_ClipChildLocalTranslation) },
                    new[] { ("Root", m_ClipRootLocalRotation), ("Child1", m_ClipChildLocalRotation) },
                    new[] { ("Root", m_ClipRootLocalScale),    ("Child1", m_ClipChildLocalScale) });

                var blobPath = "ClipNodeTestsDenseClip2.blob";
                BlobFile.WriteBlobAsset(ref denseClip, blobPath);
            }
            {
                var clip = new AnimationClip();
                clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.x", GetConstantCurve(m_ClipRootLocalTranslation.x));
                clip.SetCurve("Root", typeof(Transform), "m_LocalScale.x", GetConstantCurve(m_ClipRootLocalScale.x));

                clip.SetCurve("IKWeight", typeof(Animator), "", GetConstantCurve(m_IKWeight));

                var denseClip = clip.ToDenseClip();
                var blobPath = "ClipNodeTestsConstantPartial.blob";
                BlobFile.WriteBlobAsset(ref denseClip, blobPath);
            }
            {
                var denseClip = CreateLinearDenseClip(
                    new[] { new LinearBinding<float3> { Path = "Root", ValueStart = float3.zero, ValueEnd = m_ClipRootLocalTranslation } },
                    new[] { new LinearBinding<quaternion> { Path = "Root", ValueStart = quaternion.identity, ValueEnd = m_ClipRootLocalRotation } },
                    new[] { new LinearBinding<float3> { Path = "Root", ValueStart = float3.zero, ValueEnd = m_ClipRootLocalScale } }
                );
                var blobPath = "ClipNodeTestsLinearRootClip.blob";
                BlobFile.WriteBlobAsset(ref denseClip, blobPath);
            }
            {
                var denseClip = CreateLinearDenseClip(
                    new[]
                    {
                        new LinearBinding<float3> { Path = "Root", ValueStart = float3.zero, ValueEnd = m_ClipRootLocalTranslation },
                        new LinearBinding<float3> { Path = "Child1", ValueStart = float3.zero, ValueEnd = m_ClipChildLocalTranslation }
                    },
                    new[]
                    {
                        new LinearBinding<quaternion> { Path = "Root", ValueStart = quaternion.identity, ValueEnd = m_ClipRootLocalRotation },
                        new LinearBinding<quaternion> { Path = "Child1", ValueStart = quaternion.identity, ValueEnd = m_ClipChildLocalRotation }
                    },
                    new[]
                    {
                        new LinearBinding<float3> { Path = "Root", ValueStart = float3.zero, ValueEnd = m_ClipRootLocalScale },
                        new LinearBinding<float3> { Path = "Child1", ValueStart = float3.zero, ValueEnd = m_ClipChildLocalScale }
                    }
                );

                var blobPath = "ClipNodeTestsLinearHierarchyClip.blob";
                BlobFile.WriteBlobAsset(ref denseClip, blobPath);
            }
            {
                var denseClip = CreateLinearDenseClip(
                    new[]
                    {
                        new LinearBinding<float3> { Path = "Child1", ValueStart = float3.zero, ValueEnd = m_ClipChildLocalTranslation },
                        new LinearBinding<float3> { Path = "Child2", ValueStart = float3.zero, ValueEnd = m_ClipChildLocalTranslation }
                    },
                    new[]
                    {
                        new LinearBinding<quaternion> { Path = "Child1", ValueStart = quaternion.identity, ValueEnd = m_ClipChildLocalRotation },
                        new LinearBinding<quaternion> { Path = "Child2", ValueStart = quaternion.identity, ValueEnd = m_ClipChildLocalRotation }
                    },
                    new[]
                    {
                        new LinearBinding<float3> { Path = "Child1", ValueStart = float3.zero, ValueEnd = m_ClipChildLocalScale },
                        new LinearBinding<float3> { Path = "Child2", ValueStart = float3.zero, ValueEnd = m_ClipChildLocalScale }
                    }, 0.0f, m_LinearHierarchyNoRootClipDuration
                );

                var blobPath = "ClipNodeTestsLinearHierarchyNoRootClip.blob";
                BlobFile.WriteBlobAsset(ref denseClip, blobPath);
            }
            {
                var clip = new AnimationClip();
                clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.x", GetLinearCurve(0.0f, m_ClipRootLocalTranslation.x));
                clip.SetCurve("Root", typeof(Transform), "m_LocalScale.x", GetLinearCurve(0.0f, m_ClipRootLocalScale.x));
                var denseClip = clip.ToDenseClip();

                var blobPath = "ClipNodeTestsLinearPartialClip.blob";
                BlobFile.WriteBlobAsset(ref denseClip, blobPath);
            }
            {
                var denseClip = CreateConstantDenseClip(
                    new[] { ("Root", m_ClipRootLocalTranslation), ("Child1", m_ClipRootLocalTranslation), ("Child2", m_ClipRootLocalTranslation), ("Child3", m_ClipRootLocalTranslation), ("Child4", m_ClipRootLocalTranslation)},
                    new[] { ("Root", m_ClipRootLocalRotation), ("Child1", m_ClipRootLocalRotation), ("Child2", m_ClipRootLocalRotation), ("Child3", m_ClipRootLocalRotation), ("Child4", m_ClipRootLocalRotation) },
                    new[] { ("Root", m_ClipRootLocalScale), ("Child1", m_ClipRootLocalScale), ("Child2", m_ClipRootLocalScale), ("Child3", m_ClipRootLocalScale), ("Child4", m_ClipRootLocalScale) });

                var blobPath = "ClipNodeTestsDenseClipSOA.blob";
                BlobFile.WriteBlobAsset(ref denseClip, blobPath);
            }
#endif
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // Create rig
            m_Rig = new Rig { Value = CreateTestRigDefinition() };

            // Constant root clip
            {
                var path = "ClipNodeTestsDenseClip1.blob";
                m_ConstantRootClip = BlobFile.ReadBlobAsset<Clip>(path);
                ClipManager.Instance.GetClipFor(m_Rig, m_ConstantRootClip);
            }

            // Constant hierarchy clip
            {
                var path = "ClipNodeTestsDenseClip2.blob";
                m_ConstantHierarchyClip = BlobFile.ReadBlobAsset<Clip>(path);

                ClipManager.Instance.GetClipFor(m_Rig, m_ConstantHierarchyClip);
            }

            // Constant partial clip
            {
                var path = "ClipNodeTestsConstantPartial.blob";
                m_ConstantPartialClip = BlobFile.ReadBlobAsset<Clip>(path);

                ClipManager.Instance.GetClipFor(m_Rig, m_ConstantPartialClip);
            }

            // Linear root clip
            {
                var path = "ClipNodeTestsLinearRootClip.blob";
                m_LinearRootClip = BlobFile.ReadBlobAsset<Clip>(path);

                ClipManager.Instance.GetClipFor(m_Rig, m_LinearRootClip);
            }

            // Linear hierarchy clip
            {
                var path = "ClipNodeTestsLinearHierarchyClip.blob";
                m_LinearHierarchyClip = BlobFile.ReadBlobAsset<Clip>(path);

                ClipManager.Instance.GetClipFor(m_Rig, m_LinearHierarchyClip);
            }

            // Linear hierarchy with no root anim clip
            {
                var path = "ClipNodeTestsLinearHierarchyNoRootClip.blob";
                m_LinearHierarchyNoRootClip = BlobFile.ReadBlobAsset<Clip>(path);

                ClipManager.Instance.GetClipFor(m_Rig, m_LinearHierarchyNoRootClip);
            }

            // Linear partial clip
            {
                var path = "ClipNodeTestsLinearPartialClip.blob";
                m_LinearPartialClip = BlobFile.ReadBlobAsset<Clip>(path);

                ClipManager.Instance.GetClipFor(m_Rig, m_LinearPartialClip);
            }

            // SOA clip
            {
                var path = "ClipNodeTestsDenseClipSOA.blob";
                m_ConstantSOAClip = BlobFile.ReadBlobAsset<Clip>(path);

                ClipManager.Instance.GetClipFor(m_Rig, m_ConstantSOAClip);
            }
        }

        struct TestDataClipNode
        {
            public Entity Entity;
            public NodeHandle<ClipNode> ClipNode;
        }

        TestDataClipNode CreateClipNodeGraph(float time, in Rig rig, BlobAssetReference<Clip> clip)
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, Entity.Null);

            var set = Set;
            var clipNode = CreateNode<ClipNode>();
            var entityNode = CreateComponentNode(entity);

            set.Connect(clipNode, ClipNode.KernelPorts.Output, entityNode);

            set.SendMessage(clipNode, ClipNode.SimulationPorts.Rig, rig);
            set.SendMessage(clipNode, ClipNode.SimulationPorts.Clip, clip);
            set.SetData(clipNode, ClipNode.KernelPorts.Time, time);

            return new TestDataClipNode { Entity = entity, ClipNode = clipNode };
        }

        struct TestDataUberClipNode
        {
            public Entity Entity;
            public NodeHandle<UberClipNode> UberClipNode;
        }

        TestDataUberClipNode CreateUberClipNodeGraph(float time, in Rig rig, BlobAssetReference<Clip> clip, ClipConfiguration config)
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, Entity.Null);

            var set = Set;
            var uberNode = CreateNode<UberClipNode>();
            var entityNode = CreateComponentNode(entity);

            set.Connect(uberNode, UberClipNode.KernelPorts.Output, entityNode);

            set.SendMessage(uberNode, UberClipNode.SimulationPorts.Rig, rig);
            set.SendMessage(uberNode, UberClipNode.SimulationPorts.Clip, clip);
            Set.SendMessage(uberNode, UberClipNode.SimulationPorts.Configuration, config);
            set.SetData(uberNode, UberClipNode.KernelPorts.Time, time);

            return new TestDataUberClipNode { Entity = entity, UberClipNode = uberNode };
        }

        struct TestDataClipPlayerNode
        {
            public Entity Entity;
            public NodeHandle<ClipPlayerNode> ClipPlayerNode;
        }

        TestDataClipPlayerNode CreateClipPlayerNodeGraph(float deltaTime, float speed, in Rig rig, BlobAssetReference<Clip> clip, ClipConfiguration config)
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, Entity.Null);

            var set = Set;
            var clipPlayerNode = CreateNode<ClipPlayerNode>();
            var entityNode = CreateComponentNode(entity);

            set.Connect(clipPlayerNode, ClipPlayerNode.KernelPorts.Output, entityNode);

            Set.SendMessage(clipPlayerNode, ClipPlayerNode.SimulationPorts.Configuration, config);
            set.SendMessage(clipPlayerNode, ClipPlayerNode.SimulationPorts.Rig, rig);
            set.SendMessage(clipPlayerNode, ClipPlayerNode.SimulationPorts.Clip, clip);
            set.SetData(clipPlayerNode, ClipPlayerNode.KernelPorts.Speed, speed);
            set.SetData(clipPlayerNode, ClipPlayerNode.KernelPorts.DeltaTime, deltaTime);

            return new TestDataClipPlayerNode { Entity = entity, ClipPlayerNode = clipPlayerNode };
        }

        [Test]
        [TestCase(0.0f, Description = "Play at beginning")]
        [TestCase(1.0f / 7.0f, Description = "Play in between keys (not a factor of 60)")]
        [TestCase(0.5f, Description = "Play on a key (a factor of 60)")]
        [TestCase(1.0f, Description = "Play at end")]
        public void CanPlayConstantClip(float time)
        {
            var data = CreateClipNodeGraph(time, m_Rig, m_ConstantRootClip);
            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(m_ClipRootLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(0), Is.EqualTo(m_ClipRootLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(m_ClipRootLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.0f, Description = "Play at beginning")]
        [TestCase(1.0f / 7.0f, Description = "Play in between keys (not a factor of 60)")]
        [TestCase(0.5f, Description = "Play on a key (a factor of 60)")]
        [TestCase(1.0f, Description = "Play at end")]
        public void CanPlayConstantClipWithPartialBindings(float time)
        {
            var data = CreateClipNodeGraph(time, m_Rig, m_ConstantPartialClip);
            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            var expectedLocalTranslation = new float3(m_ClipRootLocalTranslation.x, 0.0f, 0.0f);
            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));

            var expectedLocalScale = new float3(m_ClipRootLocalScale.x, 1.0f, 1.0f);
            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));

            Assert.That(streamECS.GetFloat(0), Is.EqualTo(m_IKWeight).Within(1).Ulps);
            Assert.That(streamECS.GetInt(0), Is.EqualTo(m_DefaultType));
        }

        [Test]
        [TestCase(0.0f, Description = "Play at beginning")]
        [TestCase(1.0f / 7.0f, Description = "Play in between keys (not a factor of 60)")]
        [TestCase(0.5f, Description = "Play on a key (a factor of 60)")]
        [TestCase(1.0f, Description = "Play at end")]
        public void CanPlayConstantClipWithHierarchy(float time)
        {
            var data = CreateClipNodeGraph(time, m_Rig, m_ConstantHierarchyClip);
            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(m_ClipChildLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(m_ClipChildLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(m_ClipChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.0f, Description = "Play at beginning")]
        [TestCase(1.0f / 7.0f, Description = "Play in between keys (not a factor of 60)")]
        [TestCase(0.5f, Description = "Play on a key (a factor of 60)")]
        [TestCase(0.99f, Description = "Play near the end")]
        [TestCase(1.0f, Description = "Play at end")]
        public void CanPlayLinearClip(float time)
        {
            var data = CreateClipNodeGraph(time, m_Rig, m_LinearRootClip);
            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            var expectedLocalTranslation = math.lerp(float3.zero, m_ClipRootLocalTranslation, time);
            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));

            var expectedLocalRotation = mathex.lerp(quaternion.identity, m_ClipRootLocalRotation, time);
            Assert.That(streamECS.GetLocalToParentRotation(0), Is.EqualTo(expectedLocalRotation).Using(RotationComparer));

            var expectedLocalScale = math.lerp(float3.zero, m_ClipRootLocalScale, time);
            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.0f, Description = "Play at beginning")]
        [TestCase(1.0f / 7.0f, Description = "Play in between keys (not a factor of 60)")]
        [TestCase(0.5f, Description = "Play on a key (a factor of 60)")]
        [TestCase(0.99f, Description = "Play near the end")]
        [TestCase(1.0f, Description = "Play at end")]
        public void CanPlayLinearClipWithPartialBindings(float time)
        {
            var data = CreateClipNodeGraph(time, m_Rig, m_LinearPartialClip);
            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            var expectedLocalTranslation = new float3(math.lerp(0.0f, m_ClipRootLocalTranslation.x, time), 0.0f, 0.0f);
            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));

            var expectedLocalScale = new float3(math.lerp(0.0f, m_ClipRootLocalScale.x, time), 1.0f, 1.0f);
            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.0f, Description = "Play at beginning")]
        [TestCase(1.0f / 7.0f, Description = "Play in between keys (not a factor of 60)")]
        [TestCase(0.5f, Description = "Play on a key (a factor of 60)")]
        [TestCase(0.99f, Description = "Play near the end")]
        [TestCase(1.0f, Description = "Play at end")]
        public void CanPlayLinearClipWithHierarchy(float time)
        {
            var data = CreateClipNodeGraph(time, m_Rig, m_LinearHierarchyClip);
            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            var expectedChildLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation, time);
            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedChildLocalTranslation).Using(TranslationComparer));

            var expectedChildLocalRotation = mathex.lerp(quaternion.identity, m_ClipChildLocalRotation, time);
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedChildLocalRotation).Using(RotationComparer));

            var expectedChildLocalScale = math.lerp(float3.zero, m_ClipChildLocalScale, time);
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        public void CanPlayClipWithMoreThan4TransformHierarchy()
        {
            var data = CreateClipNodeGraph(0.5f, m_Rig, m_ConstantSOAClip);
            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            for (int i = 0; i < m_Rig.Value.Value.Skeleton.BoneCount; i++)
            {
                Assert.That(streamECS.GetLocalToParentTranslation(i), Is.EqualTo(m_ClipRootLocalTranslation).Using(TranslationComparer));

                Assert.That(streamECS.GetLocalToParentRotation(i), Is.EqualTo(m_ClipRootLocalRotation).Using(RotationComparer));

                Assert.That(streamECS.GetLocalToParentScale(i), Is.EqualTo(m_ClipRootLocalScale).Using(ScaleComparer));
            }
        }

        [Test]
        [TestCase(0.0f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play at beginning")]
        [TestCase(1.0f / 7.0f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play in between keys (not a factor of 60)")]
        [TestCase(0.5f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play on a key (a factor of 60)")]
        [TestCase(0.99f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play near the end")]
        [TestCase(1.0f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play at end")]
        public void CanPlayLinearClipWithHierarchyWithDurationNotA60FPSMutliple(float time)
        {
            var data = CreateClipNodeGraph(time, m_Rig, m_LinearHierarchyNoRootClip);
            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            var interpolationTime = time / m_LinearHierarchyNoRootClip.Value.Duration;

            var expectedChildLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation, interpolationTime);
            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedChildLocalTranslation).Using(TranslationComparer));

            var expectedChildLocalRotation = mathex.lerp(quaternion.identity, m_ClipChildLocalRotation, interpolationTime);
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedChildLocalRotation).Using(RotationComparer));

            var expectedChildLocalScale = math.lerp(float3.zero, m_ClipChildLocalScale, interpolationTime);
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.0f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play at beginning")]
        [TestCase(1.0f / 7.0f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play in between keys (not a factor of 60)")]
        [TestCase(0.5f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play on a key (a factor of 60)")]
        [TestCase(0.99f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play near the end")]
        [TestCase(1.0f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play at end")]
        public void CanPlayBakedClip(float time)
        {
            var bakedClip = UberClipNode.Bake(m_Rig, m_LinearHierarchyNoRootClip, new ClipConfiguration());
            var data = CreateClipNodeGraph(time, m_Rig, bakedClip);
            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            var interpolationTime = time / bakedClip.Value.Duration;

            var expectedChildLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation, interpolationTime);
            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedChildLocalTranslation).Using(TranslationComparer));

            var expectedChildLocalRotation = mathex.lerp(quaternion.identity, m_ClipChildLocalRotation, interpolationTime);
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedChildLocalRotation).Using(RotationComparer));

            var expectedChildLocalScale = math.lerp(float3.zero, m_ClipChildLocalScale, interpolationTime);
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedChildLocalScale).Using(ScaleComparer));
        }

        [Test(Description = "Bake handles clips with only one of translation or rotation channels, but not both")]
        public void CanBakePartialClip()
        {
            Assert.DoesNotThrow(() => UberClipNode.Bake(m_Rig, m_LinearPartialClip, new ClipConfiguration() { MotionID = "Root" }));
        }

        [Test]
        [TestCase(0.999f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play near the very end")]
        [TestCase(1.0f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play at end")]
        public void CanPlayBakedLoopTransformClip(float time)
        {
            var bakedClip = UberClipNode.Bake(m_Rig, m_LinearHierarchyNoRootClip, new ClipConfiguration { Mask = ClipConfigurationMask.LoopValues });
            var data = CreateClipNodeGraph(time, m_Rig, bakedClip);
            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            var interpolationTime = time / bakedClip.Value.Duration;

            // stop values should be equal to start values
            var expectedChildLocalTranslation = float3.zero;
            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedChildLocalTranslation).Using(TranslationComparer));

            var expectedChildLocalRotation = quaternion.identity;
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedChildLocalRotation).Using(RotationComparer));
        }

        [Test]
        [TestCase(0.0f, 1.1f, Description = "From start to after the clip length")]
        [TestCase(0.0f, 2.0f, Description = "From start to double the clip length")]
        [TestCase(0.0f, 2.1f, Description = "From start to more than double the clip length")]
        [TestCase(0.9f, 1.1f, Description = "From near the end to after the clip length")]
        [TestCase(0.9f, 2.0f, Description = "From near the end to double the clip length")]
        [TestCase(0.9f, 2.1f, Description = "From near the end to more than double the clip length")]
        [TestCase(1.0f, 1.1f, Description = "From the end to after the clip length")]
        [TestCase(1.0f, 2.0f, Description = "From the end to double the clip length")]
        [TestCase(1.0f, 2.1f, Description = "From the end to more than double the clip length")]
        public void CanLoopClip(float startTime, float nextTime)
        {
            var data = CreateUberClipNodeGraph(startTime, m_Rig, m_LinearHierarchyClip, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
            m_AnimationGraphSystem.Update();
            Set.SetData(data.UberClipNode, UberClipNode.KernelPorts.Time, nextTime);
            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            // Get new time within clip length.
            var clipLength = m_LinearHierarchyClip.Value.Duration;
            var currentTime = math.fmod(nextTime, clipLength);

            var expectedChildLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation, currentTime);
            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedChildLocalTranslation).Using(TranslationComparer));

            var expectedChildLocalRotation = mathex.lerp(quaternion.identity, m_ClipChildLocalRotation, currentTime);
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedChildLocalRotation).Using(RotationComparer));

            var expectedChildLocalScale = math.lerp(float3.zero, m_ClipChildLocalScale, currentTime);
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.0f, Description = "Play clip at 0%")]
        [TestCase(0.1f, Description = "Play clip at 10%")]
        [TestCase(0.33f, Description = "Play clip at 33%")]
        [TestCase(0.5f, Description = "Play clip at 50%")]
        [TestCase(0.9f, Description = "Play clip at 90%")]
        [TestCase(1.0f, Description = "Play clip at 100%")]
        public void CanEvaluateNormalizedTimeClip(float normalizedTime)
        {
            var data = CreateUberClipNodeGraph(normalizedTime, m_Rig, m_LinearHierarchyClip, new ClipConfiguration { Mask = ClipConfigurationMask.NormalizedTime });
            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            var expectedChildLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation, normalizedTime);
            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedChildLocalTranslation).Using(TranslationComparer));

            var expectedChildLocalRotation = mathex.lerp(quaternion.identity, m_ClipChildLocalRotation, normalizedTime);
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedChildLocalRotation).Using(RotationComparer));

            var expectedChildLocalScale = math.lerp(float3.zero, m_ClipChildLocalScale, normalizedTime);
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(1.0f / 30.0f, 1.5f,  Description = "Play clip with delta time = 1/30 and speed 150%")]
        [TestCase(1.0f / 30.0f, 0.5f,  Description = "Play clip with delta time = 1/30 and speed 50%")]
        [TestCase(1.0f / 60.0f, 1.0f,  Description = "Play clip with delta time = 1/60 and speed 100%")]
        [TestCase(1.0f / 15.0f, 0.1f,  Description = "Play clip with delta time = 1/15 and speed 10%")]
        public void CanEvaluateClipLoopPlayer(float deltaTime, float speed)
        {
            var data = CreateClipPlayerNodeGraph(deltaTime, speed, m_Rig, m_LinearHierarchyClip, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });

            var currentTime = 0.0f;
            for (var frameIter = 0; frameIter < 5; frameIter++)
            {
                m_AnimationGraphSystem.Update();
                currentTime += deltaTime * speed;
            }

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            var expectedChildLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation, currentTime);
            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedChildLocalTranslation).Using(TranslationComparer));

            var expectedChildLocalRotation = mathex.lerp(quaternion.identity, m_ClipChildLocalRotation, currentTime);
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedChildLocalRotation).Using(RotationComparer));

            var expectedChildLocalScale = math.lerp(float3.zero, m_ClipChildLocalScale, currentTime);
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(1.0f / 30.0f, 1.5f, 0.1f,  Description = "Play clip at time = 0.1s")]
        [TestCase(1.0f / 30.0f, 0.5f, 0.3f,  Description = "Play clip at time = 0.3s")]
        [TestCase(1.0f / 60.0f, 1.0f, 0.5f,  Description = "Play clip at time = 0.5s")]
        [TestCase(1.0f / 15.0f, 0.1f, 0.7f,  Description = "Play clip at time = 0.7s")]
        public void CanSetTimeOnClipLoopPlayer(float deltaTime, float speed, float time)
        {
            var data = CreateClipPlayerNodeGraph(deltaTime, speed, m_Rig, m_LinearHierarchyClip, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });

            var currentTime = 0.0f;
            for (var frameIter = 0; frameIter < 5; frameIter++)
            {
                m_AnimationGraphSystem.Update();
                currentTime += deltaTime * speed;
            }

            // override time at this point
            Set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Time, time);
            currentTime = time;

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            var expectedChildLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation, currentTime);
            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedChildLocalTranslation).Using(TranslationComparer));

            var expectedChildLocalRotation = mathex.lerp(quaternion.identity, m_ClipChildLocalRotation, currentTime);
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedChildLocalRotation).Using(RotationComparer));

            var expectedChildLocalScale = math.lerp(float3.zero, m_ClipChildLocalScale, currentTime);
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        public void CanInstantiateAndDeleteClipLoopPlayer()
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, Entity.Null);

            var set = Set;

            var layerMixerNode = CreateNode<LayerMixerNode>();
            var entityNode = CreateComponentNode(entity);

            set.Connect(layerMixerNode, LayerMixerNode.KernelPorts.Output, entityNode);

            set.SendMessage(layerMixerNode, LayerMixerNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(layerMixerNode, LayerMixerNode.SimulationPorts.LayerCount, (ushort)1);
            set.SetData(layerMixerNode, LayerMixerNode.KernelPorts.Weights, 0, 1f);

            var clipLoopPlayerNode = Set.Create<ClipPlayerNode>();

            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.Clip, m_LinearHierarchyClip);
            set.SetData(clipLoopPlayerNode, ClipPlayerNode.KernelPorts.Speed, 1.0f);
            set.SetData(clipLoopPlayerNode, ClipPlayerNode.KernelPorts.DeltaTime, World.Time.DeltaTime);

            set.Connect(clipLoopPlayerNode, ClipPlayerNode.KernelPorts.Output, layerMixerNode, LayerMixerNode.KernelPorts.Inputs, 0);

            m_AnimationGraphSystem.Update();

            set.Destroy(clipLoopPlayerNode);

            m_AnimationGraphSystem.Update();

            clipLoopPlayerNode = Set.Create<ClipPlayerNode>();

            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.Clip, m_LinearHierarchyClip);
            set.SetData(clipLoopPlayerNode, ClipPlayerNode.KernelPorts.Speed, 1.0f);
            set.SetData(clipLoopPlayerNode, ClipPlayerNode.KernelPorts.DeltaTime, World.Time.DeltaTime);

            set.Connect(clipLoopPlayerNode, ClipPlayerNode.KernelPorts.Output, layerMixerNode, LayerMixerNode.KernelPorts.Inputs, 0);

            m_AnimationGraphSystem.Update();

            set.Destroy(clipLoopPlayerNode);
        }

        [Test]
        public void ConsecutiveCallsToSetTimeDoesNotThrow()
        {
            var data = CreateClipPlayerNodeGraph(0.2f, 1.0f, m_Rig, m_LinearHierarchyClip, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });

            Assert.DoesNotThrow(() => { Set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Time, 0.5f); });
            Assert.DoesNotThrow(() => { Set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Time, 0.2f); });
            Assert.DoesNotThrow(() => { m_AnimationGraphSystem.Update(); });
        }
    }
}
