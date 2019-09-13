using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.TestTools;

namespace Unity.Animation.Tests
{
    public class DeltaNodeTests : AnimationTestsFixture, IPrebuildSetup
    {
        private float3 m_ClipRootLocalTranslation => new float3(100.0f, 0.0f, 0.0f);
        private quaternion m_ClipRootLocalRotation => quaternion.RotateX(math.radians(90.0f));
        private float3 m_ClipRootLocalScale => new float3(10.0f, 1.0f, 1.0f);
        private float m_ClipRootFloat => 10.0f;
        private int m_ClipRootInteger => 20;

        private float3 m_ClipChildLocalTranslation => new float3(0.0f, 100.0f, 0.0f);
        private quaternion m_ClipChildLocalRotation => quaternion.RotateY(math.radians(90.0f));
        private float3 m_ClipChildLocalScale => new float3(1.0f, 1.0f, 10.0f);
        private float m_ClipChildFloat => 10.0f;
        private int m_ClipChildInteger => 20;

        private BlobAssetReference<RigDefinition> m_Rig;
        private BlobAssetReference<ClipInstance> m_ConstantHierarchyClip;
        private BlobAssetReference<ClipInstance> m_LinearHierarchyClip;

        private static BlobAssetReference<RigDefinition> CreateTestRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode { ParentIndex = -1, Id = "Root", AxisIndex = -1 },
                new SkeletonNode { ParentIndex = 0, Id = "Child1", AxisIndex = -1 },
            };

            var animationChannel = new IAnimationChannel[] {
                new FloatChannel {Id = "Root"},
                new FloatChannel {Id = "Child1"},
                new IntChannel {Id = "Root"},
                new IntChannel {Id = "Child1"},
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannel);
        }

        public void Setup()
        {
#if UNITY_EDITOR
            {
                var denseClip = CreateConstantDenseClip(
                        new[] { ("Root", m_ClipRootLocalTranslation), ("Child1", m_ClipChildLocalTranslation) },
                        new[] { ("Root", m_ClipRootLocalRotation), ("Child1", m_ClipChildLocalRotation) },
                        new[] { ("Root", m_ClipRootLocalScale),    ("Child1", m_ClipChildLocalScale) },
                        new[] { ("Root", m_ClipRootFloat),    ("Child1", m_ClipChildFloat) },
                        new[] { ("Root", m_ClipRootInteger),    ("Child1", m_ClipChildInteger) });

                var blobPath = "DeltaNodeTestsDenseClip2.blob";
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
                    },
                    new[]
                    {
                        new LinearBinding<float> { Path = "Root", ValueStart = 0.0f, ValueEnd = m_ClipRootFloat },
                        new LinearBinding<float> { Path = "Child1", ValueStart = 0.0f, ValueEnd = m_ClipChildFloat }
                    },
                    new[]
                    {
                        new LinearBinding<int> { Path = "Root", ValueStart = 0, ValueEnd = m_ClipRootInteger },
                        new LinearBinding<int> { Path = "Child1", ValueStart = 0, ValueEnd = m_ClipChildInteger }
                    }
                    );

                var blobPath = "DeltaNodeTestsLinearHierarchyClip.blob";
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

            // Constant hierarchy clip
            {
                var path = "DeltaNodeTestsDenseClip2.blob";
                var denseClip = BlobFile.ReadBlobAsset<Clip>(path);

                m_ConstantHierarchyClip = ClipManager.Instance.GetClipFor(m_Rig, denseClip);
            }

            // Linear hierarchy clip
            {
                var path = "DeltaNodeTestsLinearHierarchyClip.blob";
                var denseClip = BlobFile.ReadBlobAsset<Clip>(path);

                m_LinearHierarchyClip = ClipManager.Instance.GetClipFor(m_Rig, denseClip);
            }
        }

        [Test]
        [TestCase(0.0f)]
        [TestCase(0.5f)]
        [TestCase(1.0f)]
        public void DeltaNodeOutputDifferenceBetweenBothInput(float time)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var subClipNode = CreateNode<ClipNode>();
            set.SendMessage(subClipNode, ClipNode.SimulationPorts.ClipInstance, m_ConstantHierarchyClip);
            set.SetData(subClipNode, ClipNode.KernelPorts.Time, 0);

            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.ClipInstance, m_LinearHierarchyClip);

            var deltaNode = CreateNode<DeltaNode>();
            set.SendMessage(deltaNode, DeltaNode.SimulationPorts.RigDefinition, in m_Rig);
            set.Connect(subClipNode, ClipNode.KernelPorts.Output, deltaNode, DeltaNode.KernelPorts.Subtract);
            set.Connect(clipNode, ClipNode.KernelPorts.Output, deltaNode, DeltaNode.KernelPorts.Input);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(deltaNode, DeltaNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            set.SetData(clipNode, ClipNode.KernelPorts.Time, time);

            m_AnimationGraphSystem.Update();

            var localTranslationBuffer = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity).Reinterpret<float3>();
            var localRotatioBuffer = m_Manager.GetBuffer<AnimatedLocalRotation>(entity).Reinterpret<quaternion>();
            var localScaleBuffer = m_Manager.GetBuffer<AnimatedLocalScale>(entity).Reinterpret<float3>();
            var localFloatBuffer = m_Manager.GetBuffer<AnimatedFloat>(entity).Reinterpret<float>();
            var localIntBuffer = m_Manager.GetBuffer<AnimatedInt>(entity).Reinterpret<int>();

            Assert.AreEqual(m_Rig.Value.Bindings.TranslationBindings.Length, localTranslationBuffer.Length);
            Assert.AreEqual(m_Rig.Value.Bindings.RotationBindings.Length, localRotatioBuffer.Length);
            Assert.AreEqual(m_Rig.Value.Bindings.ScaleBindings.Length, localScaleBuffer.Length);
            Assert.AreEqual(m_Rig.Value.Bindings.FloatBindings.Length, localFloatBuffer.Length);
            Assert.AreEqual(m_Rig.Value.Bindings.IntBindings.Length, localIntBuffer.Length);

            var expectedLocalTranslation = -m_ClipRootLocalTranslation + math.lerp(float3.zero, m_ClipRootLocalTranslation, time);
            var expectedLocalRotation = math.mul(math.conjugate(m_ClipRootLocalRotation), mathex.lerp(quaternion.identity, m_ClipRootLocalRotation, time));
            var expectedLocalScale = -m_ClipRootLocalScale + math.lerp(float3.zero, m_ClipRootLocalScale, time);

            Assert.That(localTranslationBuffer[0], Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));
            Assert.That(localRotatioBuffer[0], Is.EqualTo(expectedLocalRotation).Using(RotationComparer));
            Assert.That(localScaleBuffer[0], Is.EqualTo(expectedLocalScale).Using(ScaleComparer));

            // TODO@sonny we need to convert float and int curve to activate this
            //Assert.That(localFloatBuffer[0], Is.EqualTo(-m_ClipRootFloat).Within(1).Ulps);
            //Assert.That(localIntBuffer[0], Is.EqualTo(-m_ClipRootInteger));
        }


        [Test]
        [TestCase(0.0f)]
        [TestCase(0.5f)]
        [TestCase(1.0f)]
        public void PlayingClipNodeShouldYieldSameResultThanClipNodeAsAdditivePlusReferencePose(float time)
        {
            var entity = m_Manager.CreateEntity();
            var anotherEntity = m_Manager.CreateEntity();

            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);
            RigEntityBuilder.SetupRigEntity(anotherEntity, m_Manager, m_Rig);

            var set = Set;
            var subClipNode = CreateNode<ClipNode>();
            set.SendMessage(subClipNode, ClipNode.SimulationPorts.ClipInstance, m_ConstantHierarchyClip);
            set.SetData(subClipNode, ClipNode.KernelPorts.Time, 0);

            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.ClipInstance, m_LinearHierarchyClip);

            var deltaNode = CreateNode<DeltaNode>();
            set.SendMessage(deltaNode, DeltaNode.SimulationPorts.RigDefinition, in m_Rig);
            set.Connect(subClipNode, ClipNode.KernelPorts.Output, deltaNode, DeltaNode.KernelPorts.Subtract);
            set.Connect(clipNode, ClipNode.KernelPorts.Output, deltaNode, DeltaNode.KernelPorts.Input);

            var layerMixerNode = CreateNode<LayerMixerNode>();
            set.SendMessage(layerMixerNode, LayerMixerNode.SimulationPorts.RigDefinition, in m_Rig);
            set.SendMessage(layerMixerNode, LayerMixerNode.SimulationPorts.BlendModeInput1, BlendingMode.Additive);
            set.SendMessage(layerMixerNode, LayerMixerNode.SimulationPorts.WeightInput0, 1.0f);
            set.SendMessage(layerMixerNode, LayerMixerNode.SimulationPorts.WeightInput1, 1.0f);

            set.Connect(subClipNode, ClipNode.KernelPorts.Output, layerMixerNode, LayerMixerNode.KernelPorts.Input0);
            set.Connect(deltaNode, DeltaNode.KernelPorts.Output, layerMixerNode, LayerMixerNode.KernelPorts.Input1);

            var output1 = new GraphOutput { Buffer = CreateGraphBuffer(layerMixerNode, LayerMixerNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output1);

            var output2 = new GraphOutput { Buffer = CreateGraphBuffer(clipNode, ClipNode.KernelPorts.Output) };
            m_Manager.AddComponentData(anotherEntity, output2);

            set.SetData(clipNode, ClipNode.KernelPorts.Time, time);

            m_AnimationGraphSystem.Update();

            var localTranslationBuffer = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity).Reinterpret<float3>();
            var localRotatioBuffer = m_Manager.GetBuffer<AnimatedLocalRotation>(entity).Reinterpret<quaternion>();
            var localScaleBuffer = m_Manager.GetBuffer<AnimatedLocalScale>(entity).Reinterpret<float3>();
            var localFloatBuffer = m_Manager.GetBuffer<AnimatedFloat>(entity).Reinterpret<float>();
            var localIntBuffer = m_Manager.GetBuffer<AnimatedInt>(entity).Reinterpret<int>();

            Assert.AreEqual(m_Rig.Value.Bindings.TranslationBindings.Length, localTranslationBuffer.Length);
            Assert.AreEqual(m_Rig.Value.Bindings.RotationBindings.Length, localRotatioBuffer.Length);
            Assert.AreEqual(m_Rig.Value.Bindings.ScaleBindings.Length, localScaleBuffer.Length);
            Assert.AreEqual(m_Rig.Value.Bindings.FloatBindings.Length, localFloatBuffer.Length);
            Assert.AreEqual(m_Rig.Value.Bindings.IntBindings.Length, localIntBuffer.Length);

            var localTranslationAnotherBuffer = m_Manager.GetBuffer<AnimatedLocalTranslation>(anotherEntity).Reinterpret<float3>();
            var localRotatioAnotherBuffer = m_Manager.GetBuffer<AnimatedLocalRotation>(anotherEntity).Reinterpret<quaternion>();
            var localScaleAnotherBuffer = m_Manager.GetBuffer<AnimatedLocalScale>(anotherEntity).Reinterpret<float3>();
            var localFloatAnotherBuffer = m_Manager.GetBuffer<AnimatedFloat>(anotherEntity).Reinterpret<float>();
            var localIntAnotherBuffer = m_Manager.GetBuffer<AnimatedInt>(anotherEntity).Reinterpret<int>();

            Assert.AreEqual(m_Rig.Value.Bindings.TranslationBindings.Length, localTranslationAnotherBuffer.Length);
            Assert.AreEqual(m_Rig.Value.Bindings.RotationBindings.Length, localRotatioAnotherBuffer.Length);
            Assert.AreEqual(m_Rig.Value.Bindings.ScaleBindings.Length, localScaleAnotherBuffer.Length);
            Assert.AreEqual(m_Rig.Value.Bindings.FloatBindings.Length, localFloatAnotherBuffer.Length);
            Assert.AreEqual(m_Rig.Value.Bindings.IntBindings.Length, localIntAnotherBuffer.Length);

            for(int i=0;i<localTranslationAnotherBuffer.Length;i++)
            {
                Assert.That(localTranslationBuffer[i], Is.EqualTo(localTranslationAnotherBuffer[i]).Using(TranslationComparer));
            }

            for(int i=0;i<localRotatioAnotherBuffer.Length;i++)
            {
                Assert.That(localRotatioBuffer[i], Is.EqualTo(localRotatioAnotherBuffer[i]).Using(RotationComparer));
            }

            for(int i=0;i<localScaleAnotherBuffer.Length;i++)
            {
                Assert.That(localScaleBuffer[i], Is.EqualTo(localScaleAnotherBuffer[i]).Using(ScaleComparer));
            }
        }
    }
}
