using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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

        private BlobAssetReference<RigDefinition> m_Rig;
        private BlobAssetReference<ClipInstance> m_ConstantRootClip;
        private BlobAssetReference<ClipInstance> m_ConstantHierarchyClip;
        private BlobAssetReference<ClipInstance> m_ConstantPartialClip;
        private BlobAssetReference<ClipInstance> m_LinearRootClip;
        private BlobAssetReference<ClipInstance> m_LinearHierarchyClip;
        private BlobAssetReference<ClipInstance> m_LinearHierarchyNoRootClip;
        private BlobAssetReference<ClipInstance> m_LinearPartialClip;

        private static BlobAssetReference<RigDefinition> CreateTestRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode { ParentIndex = -1, Id = "Root", AxisIndex = -1 },
                new SkeletonNode { ParentIndex = 0, Id = "Child1", AxisIndex = -1 },
                new SkeletonNode { ParentIndex = 1, Id = "Child2", AxisIndex = -1 }
            };

            var channels = new IAnimationChannel[]
            {
                new FloatChannel { Id = "IKWeight", DefaultValue = 0.0f },
                new IntChannel { Id = "Type", DefaultValue = 10 }
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

                clip.SetCurve("", typeof(Animator), "IKWeight", GetConstantCurve(m_IKWeight));

                var denseClip = ClipBuilder.AnimationClipToDenseClip(clip);
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
                var denseClip = ClipBuilder.AnimationClipToDenseClip(clip);

                var blobPath = "ClipNodeTestsLinearPartialClip.blob";
                BlobFile.WriteBlobAsset(ref denseClip, blobPath);
            }
#endif
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // Create rig
            m_Rig = CreateTestRigDefinition();

            // Constant root clip
            {
                var path = "ClipNodeTestsDenseClip1.blob";
                var denseClip = BlobFile.ReadBlobAsset<Clip>(path);

                m_ConstantRootClip = ClipManager.Instance.GetClipFor(m_Rig, denseClip);
            }

            // Constant hierarchy clip
            {
                var path = "ClipNodeTestsDenseClip2.blob";
                var denseClip = BlobFile.ReadBlobAsset<Clip>(path);

                m_ConstantHierarchyClip = ClipManager.Instance.GetClipFor(m_Rig, denseClip);
            }

            // Constant partial clip
            {
                var path = "ClipNodeTestsConstantPartial.blob";
                var denseClip = BlobFile.ReadBlobAsset<Clip>(path);

                m_ConstantPartialClip = ClipManager.Instance.GetClipFor(m_Rig, denseClip);
            }

            // Linear root clip
            {
                var path = "ClipNodeTestsLinearRootClip.blob";
                var denseClip = BlobFile.ReadBlobAsset<Clip>(path);

                m_LinearRootClip = ClipManager.Instance.GetClipFor(m_Rig, denseClip);
            }

            // Linear hierarchy clip
            {
                var path = "ClipNodeTestsLinearHierarchyClip.blob";
                var denseClip = BlobFile.ReadBlobAsset<Clip>(path);

                m_LinearHierarchyClip = ClipManager.Instance.GetClipFor(m_Rig, denseClip);
            }

            // Linear hierarchy with no root anim clip
            {
                var path = "ClipNodeTestsLinearHierarchyNoRootClip.blob";
                var denseClip = BlobFile.ReadBlobAsset<Clip>(path);

                m_LinearHierarchyNoRootClip = ClipManager.Instance.GetClipFor(m_Rig, denseClip);
            }

            // Linear partial clip
            {
                var path = "ClipNodeTestsLinearPartialClip.blob";
                var denseClip = BlobFile.ReadBlobAsset<Clip>(path);

                m_LinearPartialClip = ClipManager.Instance.GetClipFor(m_Rig, denseClip);
            }
        }

        [Test]
        [TestCase(0.0f, Description = "Play at beginning")]
        [TestCase(1.0f / 7.0f, Description = "Play in between keys (not a factor of 60)")]
        [TestCase(0.5f, Description = "Play on a key (a factor of 60)")]
        [TestCase(1.0f, Description = "Play at end")]
        public void CanPlayConstantClip(float time)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.ClipInstance, m_ConstantRootClip);
            set.SetData(clipNode, ClipNode.KernelPorts.Time, time);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(clipNode, ClipNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var localTranslation = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity)[0].Value;
            var localRotation = m_Manager.GetBuffer<AnimatedLocalRotation>(entity)[0].Value;
            var localScale = m_Manager.GetBuffer<AnimatedLocalScale>(entity)[0].Value;

            Assert.That(localTranslation, Is.EqualTo(m_ClipRootLocalTranslation).Using(TranslationComparer));
            Assert.That(localRotation, Is.EqualTo(m_ClipRootLocalRotation).Using(RotationComparer));
            Assert.That(localScale, Is.EqualTo(m_ClipRootLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.0f, Description = "Play at beginning")]
        [TestCase(1.0f / 7.0f, Description = "Play in between keys (not a factor of 60)")]
        [TestCase(0.5f, Description = "Play on a key (a factor of 60)")]
        [TestCase(1.0f, Description = "Play at end")]
        public void CanPlayConstantClipWithPartialBindings(float time)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.ClipInstance, m_ConstantPartialClip);
            set.SetData(clipNode, ClipNode.KernelPorts.Time, time);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(clipNode, ClipNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var localTranslation = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity)[0].Value;
            var localScale = m_Manager.GetBuffer<AnimatedLocalScale>(entity)[0].Value;
            var ikWeight = m_Manager.GetBuffer<AnimatedFloat>(entity)[0].Value;
            var type = m_Manager.GetBuffer<AnimatedInt>(entity)[0].Value;

            var expectedLocalTranslation = new float3(m_ClipRootLocalTranslation.x, 0.0f, 0.0f);
            Assert.That(localTranslation, Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));

            var expectedLocalScale = new float3(m_ClipRootLocalScale.x, 1.0f, 1.0f);
            Assert.That(localScale, Is.EqualTo(expectedLocalScale).Using(ScaleComparer));

            Assert.That(ikWeight, Is.EqualTo(m_IKWeight).Within(1).Ulps);
            Assert.That(type, Is.EqualTo(m_DefaultType));
        }

        [Test]
        [TestCase(0.0f, Description = "Play at beginning")]
        [TestCase(1.0f / 7.0f, Description = "Play in between keys (not a factor of 60)")]
        [TestCase(0.5f, Description = "Play on a key (a factor of 60)")]
        [TestCase(1.0f, Description = "Play at end")]
        public void CanPlayConstantClipWithHierarchy(float time)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.ClipInstance, m_ConstantHierarchyClip);
            set.SetData(clipNode, ClipNode.KernelPorts.Time, time);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(clipNode, ClipNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var childLocalTranslation = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity)[1].Value;
            var childLocalRotation = m_Manager.GetBuffer<AnimatedLocalRotation>(entity)[1].Value;
            var childLocalScale = m_Manager.GetBuffer<AnimatedLocalScale>(entity)[1].Value;

            Assert.That(childLocalTranslation, Is.EqualTo(m_ClipChildLocalTranslation).Using(TranslationComparer));
            Assert.That(childLocalRotation, Is.EqualTo(m_ClipChildLocalRotation).Using(RotationComparer));
            Assert.That(childLocalScale, Is.EqualTo(m_ClipChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.0f, Description = "Play at beginning")]
        [TestCase(1.0f / 7.0f, Description = "Play in between keys (not a factor of 60)")]
        [TestCase(0.5f, Description = "Play on a key (a factor of 60)")]
        [TestCase(0.99f, Description = "Play near the end")]
        [TestCase(1.0f, Description = "Play at end")]
        public void CanPlayLinearClip(float time)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.ClipInstance, m_LinearRootClip);
            set.SetData(clipNode, ClipNode.KernelPorts.Time, time);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(clipNode, ClipNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var localTranslation = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity)[0].Value;
            var localRotation = m_Manager.GetBuffer<AnimatedLocalRotation>(entity)[0].Value;
            var localScale = m_Manager.GetBuffer<AnimatedLocalScale>(entity)[0].Value;

            var expectedLocalTranslation = math.lerp(float3.zero, m_ClipRootLocalTranslation, time);
            Assert.That(localTranslation, Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));

            var expectedLocalRotation = mathex.lerp(quaternion.identity, m_ClipRootLocalRotation, time);
            Assert.That(localRotation, Is.EqualTo(expectedLocalRotation).Using(RotationComparer));

            var expectedLocalScale = math.lerp(float3.zero, m_ClipRootLocalScale, time);
            Assert.That(localScale, Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.0f, Description = "Play at beginning")]
        [TestCase(1.0f / 7.0f, Description = "Play in between keys (not a factor of 60)")]
        [TestCase(0.5f, Description = "Play on a key (a factor of 60)")]
        [TestCase(0.99f, Description = "Play near the end")]
        [TestCase(1.0f, Description = "Play at end")]
        public void CanPlayLinearClipWithPartialBindings(float time)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.ClipInstance, m_LinearPartialClip);
            set.SetData(clipNode, ClipNode.KernelPorts.Time, time);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(clipNode, ClipNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var localTranslation = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity)[0].Value;
            var localScale = m_Manager.GetBuffer<AnimatedLocalScale>(entity)[0].Value;

            var expectedLocalTranslation = new float3(math.lerp(0.0f, m_ClipRootLocalTranslation.x, time), 0.0f, 0.0f);
            Assert.That(localTranslation, Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));

            var expectedLocalScale = new float3(math.lerp(0.0f, m_ClipRootLocalScale.x, time), 1.0f, 1.0f);
            Assert.That(localScale, Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.0f, Description = "Play at beginning")]
        [TestCase(1.0f / 7.0f, Description = "Play in between keys (not a factor of 60)")]
        [TestCase(0.5f, Description = "Play on a key (a factor of 60)")]
        [TestCase(0.99f, Description = "Play near the end")]
        [TestCase(1.0f, Description = "Play at end")]
        public void CanPlayLinearClipWithHierarchy(float time)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.ClipInstance, m_LinearHierarchyClip);
            set.SetData(clipNode, ClipNode.KernelPorts.Time, time);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(clipNode, ClipNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var childLocalTranslation = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity)[1].Value;
            var childLocalRotation = m_Manager.GetBuffer<AnimatedLocalRotation>(entity)[1].Value;
            var childLocalScale = m_Manager.GetBuffer<AnimatedLocalScale>(entity)[1].Value;

            var expectedChildLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation, time);
            Assert.That(childLocalTranslation, Is.EqualTo(expectedChildLocalTranslation).Using(TranslationComparer));

            var expectedChildLocalRotation = mathex.lerp(quaternion.identity, m_ClipChildLocalRotation, time);
            Assert.That(childLocalRotation, Is.EqualTo(expectedChildLocalRotation).Using(RotationComparer));

            var expectedChildLocalScale = math.lerp(float3.zero, m_ClipChildLocalScale, time);
            Assert.That(childLocalScale, Is.EqualTo(expectedChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.0f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play at beginning")]
        [TestCase(1.0f / 7.0f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play in between keys (not a factor of 60)")]
        [TestCase(0.5f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play on a key (a factor of 60)")]
        [TestCase(0.99f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play near the end")]
        [TestCase(1.0f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play at end")]
        public void CanPlayLinearClipWithHierarchyWithDurationNotA60FPSMutliple(float time)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.ClipInstance, m_LinearHierarchyNoRootClip);
            set.SetData(clipNode, ClipNode.KernelPorts.Time, time);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(clipNode, ClipNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var childLocalTranslation = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity)[1].Value;
            var childLocalRotation = m_Manager.GetBuffer<AnimatedLocalRotation>(entity)[1].Value;
            var childLocalScale = m_Manager.GetBuffer<AnimatedLocalScale>(entity)[1].Value;

            var interpolationTime = time / m_LinearHierarchyNoRootClip.Value.Clip.Duration;

            var expectedChildLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation, interpolationTime);
            Assert.That(childLocalTranslation, Is.EqualTo(expectedChildLocalTranslation).Using(TranslationComparer));

            var expectedChildLocalRotation = mathex.lerp(quaternion.identity, m_ClipChildLocalRotation, interpolationTime);
            Assert.That(childLocalRotation, Is.EqualTo(expectedChildLocalRotation).Using(RotationComparer));

            var expectedChildLocalScale = math.lerp(float3.zero, m_ClipChildLocalScale, interpolationTime);
            Assert.That(childLocalScale, Is.EqualTo(expectedChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.0f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play at beginning")]
        [TestCase(1.0f / 7.0f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play in between keys (not a factor of 60)")]
        [TestCase(0.5f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play on a key (a factor of 60)")]
        [TestCase(0.99f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play near the end")]
        [TestCase(1.0f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play at end")]
        public void CanPlayBakedClip(float time)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var bakedClip = UberClipNode.Bake(m_LinearHierarchyNoRootClip, new ClipConfiguration());
            var bakedClipInstance = ClipInstance.Create(m_Rig, bakedClip);

            var set = Set;

            var bakedClipNode = CreateNode<UberClipNode>();
            set.SendMessage(bakedClipNode, UberClipNode.SimulationPorts.ClipInstance, bakedClipInstance);
            set.SetData(bakedClipNode, UberClipNode.KernelPorts.Time, time);
            var output = new GraphOutput { Buffer = CreateGraphBuffer(bakedClipNode, UberClipNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var childLocalTranslation = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity)[1].Value;
            var childLocalRotation = m_Manager.GetBuffer<AnimatedLocalRotation>(entity)[1].Value;
            var childLocalScale = m_Manager.GetBuffer<AnimatedLocalScale>(entity)[1].Value;

            var interpolationTime = time / bakedClipInstance.Value.Clip.Duration;

            var expectedChildLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation, interpolationTime);
            Assert.That(childLocalTranslation, Is.EqualTo(expectedChildLocalTranslation).Using(TranslationComparer));

            var expectedChildLocalRotation = mathex.lerp(quaternion.identity, m_ClipChildLocalRotation, interpolationTime);
            Assert.That(childLocalRotation, Is.EqualTo(expectedChildLocalRotation).Using(RotationComparer));

            var expectedChildLocalScale = math.lerp(float3.zero, m_ClipChildLocalScale, interpolationTime);
            Assert.That(childLocalScale, Is.EqualTo(expectedChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.999f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play near the very end")]
        [TestCase(1.0f * ClipNodeTests.m_LinearHierarchyNoRootClipDuration, Description = "Play at end")]
        public void CanPlayBakedLoopTransformClip(float time)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var bakedClip = UberClipNode.Bake(m_LinearHierarchyNoRootClip, new ClipConfiguration { Mask = (int)ClipConfigurationMask.LoopValues });
            var bakedClipInstance = ClipInstance.Create(m_Rig, bakedClip);

            var set = Set;

            var bakedClipNode = CreateNode<UberClipNode>();
            set.SendMessage(bakedClipNode, UberClipNode.SimulationPorts.ClipInstance, bakedClipInstance);
            set.SetData(bakedClipNode, UberClipNode.KernelPorts.Time, time);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(bakedClipNode, UberClipNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            var childLocalTranslation = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity)[1].Value;
            var childLocalRotation = m_Manager.GetBuffer<AnimatedLocalRotation>(entity)[1].Value;

            var interpolationTime = time / bakedClipInstance.Value.Clip.Duration;

            // stop values should be equal to start values
            var expectedChildLocalTranslation = float3.zero;
            Assert.That(childLocalTranslation, Is.EqualTo(expectedChildLocalTranslation).Using(TranslationComparer));

            var expectedChildLocalRotation = quaternion.identity;
            Assert.That(childLocalRotation, Is.EqualTo(expectedChildLocalRotation).Using(RotationComparer));
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
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipLoopNode = CreateNode<UberClipNode>();
            Set.SendMessage(clipLoopNode,UberClipNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = (int)ClipConfigurationMask.LoopTime });
            set.SendMessage(clipLoopNode, UberClipNode.SimulationPorts.ClipInstance, m_LinearHierarchyClip);
            set.SetData(clipLoopNode, UberClipNode.KernelPorts.Time, startTime);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(clipLoopNode, UberClipNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            set.SetData(clipLoopNode, UberClipNode.KernelPorts.Time, nextTime);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var childLocalTranslation = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity)[1].Value;
            var childLocalRotation = m_Manager.GetBuffer<AnimatedLocalRotation>(entity)[1].Value;
            var childLocalScale = m_Manager.GetBuffer<AnimatedLocalScale>(entity)[1].Value;

            // Get new time within clip length.
            var clipLength = m_LinearHierarchyClip.Value.Clip.Duration;
            var currentTime = math.fmod(nextTime,clipLength);

            var expectedChildLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation, currentTime);
            Assert.That(childLocalTranslation, Is.EqualTo(expectedChildLocalTranslation).Using(TranslationComparer));

            var expectedChildLocalRotation = mathex.lerp(quaternion.identity, m_ClipChildLocalRotation, currentTime);
            Assert.That(childLocalRotation, Is.EqualTo(expectedChildLocalRotation).Using(RotationComparer));

            var expectedChildLocalScale = math.lerp(float3.zero, m_ClipChildLocalScale, currentTime);
            Assert.That(childLocalScale, Is.EqualTo(expectedChildLocalScale).Using(ScaleComparer));
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
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var normalizedTimeClipNode = CreateNode<UberClipNode>();
            Set.SendMessage(normalizedTimeClipNode,UberClipNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = (int)ClipConfigurationMask.NormalizedTime });
            set.SendMessage(normalizedTimeClipNode, UberClipNode.SimulationPorts.ClipInstance, m_LinearHierarchyClip);
            set.SetData(normalizedTimeClipNode, UberClipNode.KernelPorts.Time, normalizedTime);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(normalizedTimeClipNode, UberClipNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var childLocalTranslation = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity)[1].Value;
            var childLocalRotation = m_Manager.GetBuffer<AnimatedLocalRotation>(entity)[1].Value;
            var childLocalScale = m_Manager.GetBuffer<AnimatedLocalScale>(entity)[1].Value;

            var expectedChildLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation, normalizedTime);
            Assert.That(childLocalTranslation, Is.EqualTo(expectedChildLocalTranslation).Using(TranslationComparer));

            var expectedChildLocalRotation = mathex.lerp(quaternion.identity, m_ClipChildLocalRotation, normalizedTime);
            Assert.That(childLocalRotation, Is.EqualTo(expectedChildLocalRotation).Using(RotationComparer));

            var expectedChildLocalScale = math.lerp(float3.zero, m_ClipChildLocalScale, normalizedTime);
            Assert.That(childLocalScale, Is.EqualTo(expectedChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(1.0f/30.0f, 1.5f,  Description = "Play clip with delta time = 1/30 and speed 150%")]
        [TestCase(1.0f/30.0f, 0.5f,  Description = "Play clip with delta time = 1/30 and speed 50%")]
        [TestCase(1.0f/60.0f, 1.0f,  Description = "Play clip with delta time = 1/60 and speed 100%")]
        [TestCase(1.0f/15.0f, 0.1f,  Description = "Play clip with delta time = 1/15 and speed 10%")]
        public void CanEvaluateClipLoopPlayer(float deltaTime, float speed)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipLoopPlayerNode = CreateNode<ClipPlayerNode>();
            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration{ Mask = (int)ClipConfigurationMask.LoopTime });
            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.ClipInstance, m_LinearHierarchyClip);
            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.Speed, speed);
            set.SetData(clipLoopPlayerNode, ClipPlayerNode.KernelPorts.DeltaTime, deltaTime);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(clipLoopPlayerNode, ClipPlayerNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            var currentTime = 0.0f;

            for (var frameIter = 0; frameIter < 5; frameIter++)
            {
                m_AnimationGraphSystem.Update();
                currentTime += deltaTime * speed;
            }

            // Get local translation, rotation, scale.
            var childLocalTranslation = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity)[1].Value;
            var childLocalRotation = m_Manager.GetBuffer<AnimatedLocalRotation>(entity)[1].Value;
            var childLocalScale = m_Manager.GetBuffer<AnimatedLocalScale>(entity)[1].Value;

            var expectedChildLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation, currentTime);
            Assert.That(childLocalTranslation, Is.EqualTo(expectedChildLocalTranslation).Using(TranslationComparer));

            var expectedChildLocalRotation = mathex.lerp(quaternion.identity, m_ClipChildLocalRotation, currentTime);
            Assert.That(childLocalRotation, Is.EqualTo(expectedChildLocalRotation).Using(RotationComparer));

            var expectedChildLocalScale = math.lerp(float3.zero, m_ClipChildLocalScale, currentTime);
            Assert.That(childLocalScale, Is.EqualTo(expectedChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(1.0f/30.0f, 1.5f, 0.1f,  Description = "Play clip at time = 0.1s")]
        [TestCase(1.0f/30.0f, 0.5f, 0.3f,  Description = "Play clip at time = 0.3s")]
        [TestCase(1.0f/60.0f, 1.0f, 0.5f,  Description = "Play clip at time = 0.5s")]
        [TestCase(1.0f/15.0f, 0.1f, 0.7f,  Description = "Play clip at time = 0.7s")]
        public void CanSetTimeOnClipLoopPlayer(float deltaTime, float speed, float time)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipLoopPlayerNode = CreateNode<ClipPlayerNode>();
            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = (int)ClipConfigurationMask.LoopTime });
            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.ClipInstance, m_LinearHierarchyClip);
            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.Speed, speed);
            set.SetData(clipLoopPlayerNode, ClipPlayerNode.KernelPorts.DeltaTime, deltaTime);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(clipLoopPlayerNode, ClipPlayerNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            var currentTime = 0.0f;

            for (var frameIter = 0; frameIter < 5; frameIter++)
            {
                m_AnimationGraphSystem.Update();
                currentTime += deltaTime * speed;
            }

            // override time at this point
            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.Time, time);
            currentTime = time;

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var childLocalTranslation = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity)[1].Value;
            var childLocalRotation = m_Manager.GetBuffer<AnimatedLocalRotation>(entity)[1].Value;
            var childLocalScale = m_Manager.GetBuffer<AnimatedLocalScale>(entity)[1].Value;

            var expectedChildLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation, currentTime);
            Assert.That(childLocalTranslation, Is.EqualTo(expectedChildLocalTranslation).Using(TranslationComparer));

            var expectedChildLocalRotation = mathex.lerp(quaternion.identity, m_ClipChildLocalRotation, currentTime);
            Assert.That(childLocalRotation, Is.EqualTo(expectedChildLocalRotation).Using(RotationComparer));

            var expectedChildLocalScale = math.lerp(float3.zero, m_ClipChildLocalScale, currentTime);
            Assert.That(childLocalScale, Is.EqualTo(expectedChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        public void CanInstantiateAndDeleteClipLoopPlayer()
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var layerMixerNode = CreateNode<LayerMixerNode>();

            set.SendMessage(layerMixerNode, LayerMixerNode.SimulationPorts.WeightInput0, 1f);
            set.SendMessage(layerMixerNode, LayerMixerNode.SimulationPorts.RigDefinition, m_Rig);

            var clipLoopPlayerNode = Set.Create<ClipPlayerNode>();
            var deltaTimeNode = Set.Create<DeltaTimeNode>();

            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = (int)ClipConfigurationMask.LoopTime });
            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.ClipInstance, m_LinearHierarchyClip);
            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.Speed, 1.0f);

            set.Connect(deltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime, clipLoopPlayerNode, ClipPlayerNode.KernelPorts.DeltaTime);
            set.Connect(clipLoopPlayerNode, ClipPlayerNode.KernelPorts.Output, layerMixerNode, LayerMixerNode.KernelPorts.Input0);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(layerMixerNode, LayerMixerNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            set.Destroy(clipLoopPlayerNode);
            set.Destroy(deltaTimeNode);

            m_AnimationGraphSystem.Update();

            clipLoopPlayerNode = Set.Create<ClipPlayerNode>();
            deltaTimeNode = Set.Create<DeltaTimeNode>();

            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = (int)ClipConfigurationMask.LoopTime });
            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.ClipInstance, m_LinearHierarchyClip);
            set.SendMessage(clipLoopPlayerNode, ClipPlayerNode.SimulationPorts.Speed, 1.0f);

            set.Connect(deltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime, clipLoopPlayerNode, ClipPlayerNode.KernelPorts.DeltaTime);
            set.Connect(clipLoopPlayerNode, ClipPlayerNode.KernelPorts.Output, layerMixerNode, LayerMixerNode.KernelPorts.Input0);

            m_AnimationGraphSystem.Update();

            set.Destroy(clipLoopPlayerNode);
            set.Destroy(deltaTimeNode);
        }
    }
}
