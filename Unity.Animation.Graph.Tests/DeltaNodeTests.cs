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

        private Rig m_Rig;
        private BlobAssetReference<Clip> m_ConstantHierarchyClip;
        private BlobAssetReference<Clip> m_LinearHierarchyClip;

        private static BlobAssetReference<RigDefinition> CreateTestRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode { ParentIndex = -1, Id = "Root", AxisIndex = -1 },
                new SkeletonNode { ParentIndex = 0, Id = "Child1", AxisIndex = -1 },
            };

            var animationChannel = new IAnimationChannel[]
            {
                new FloatChannel {Id = "Root", DefaultValue = 1000.0f},
                new FloatChannel {Id = "Child1", DefaultValue = 1000.0f},
                new IntChannel {Id = "Root", DefaultValue = 1000},
                new IntChannel {Id = "Child1", DefaultValue = 1000},
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannel);
        }

        public void Setup()
        {
#if UNITY_EDITOR
            PlaymodeTestsEditorSetup.CreateStreamingAssetsDirectory();

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
            m_Rig = new Rig { Value = CreateTestRigDefinition() };

            // Constant hierarchy clip
            {
                var path = "DeltaNodeTestsDenseClip2.blob";
                m_ConstantHierarchyClip = BlobFile.ReadBlobAsset<Clip>(path);

                ClipManager.Instance.GetClipFor(m_Rig, m_ConstantHierarchyClip);
            }

            // Linear hierarchy clip
            {
                var path = "DeltaNodeTestsLinearHierarchyClip.blob";
                m_LinearHierarchyClip = BlobFile.ReadBlobAsset<Clip>(path);

                ClipManager.Instance.GetClipFor(m_Rig, m_LinearHierarchyClip);
            }
        }

        [TestCase(0.0f)]
        [TestCase(0.5f)]
        [TestCase(1.0f)]
        public void DeltaNodeOutputDifferenceBetweenBothInput(float time)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var subClipNode = CreateNode<ClipNode>();
            Set.SendMessage(subClipNode, ClipNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(subClipNode, ClipNode.SimulationPorts.Clip, m_ConstantHierarchyClip);
            Set.SetData(subClipNode, ClipNode.KernelPorts.Time, 0);

            var clipNode = CreateNode<ClipNode>();
            Set.SendMessage(clipNode, ClipNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(clipNode, ClipNode.SimulationPorts.Clip, m_LinearHierarchyClip);

            var deltaNode = CreateNode<DeltaPoseNode>();
            Set.SendMessage(deltaNode, DeltaPoseNode.SimulationPorts.Rig, new Rig(){ Value = m_Rig });
            Set.Connect(subClipNode, ClipNode.KernelPorts.Output, deltaNode, DeltaPoseNode.KernelPorts.Subtract);
            Set.Connect(clipNode, ClipNode.KernelPorts.Output, deltaNode, DeltaPoseNode.KernelPorts.Input);

            var entityNode = CreateComponentNode(entity);
            Set.Connect(deltaNode, DeltaPoseNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(entity);

            Set.SetData(clipNode, ClipNode.KernelPorts.Time, time);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            var expectedLocalTranslation = -m_ClipRootLocalTranslation + math.lerp(float3.zero, m_ClipRootLocalTranslation, time);
            var expectedLocalRotation = mathex.mul(math.conjugate(m_ClipRootLocalRotation), mathex.lerp(quaternion.identity, m_ClipRootLocalRotation, time));
            var expectedLocalScale = -m_ClipRootLocalScale + math.lerp(float3.zero, m_ClipRootLocalScale, time);
            var expectedFloat = -m_ClipRootFloat + math.lerp(0.0f, m_ClipRootFloat, time);
            var expectedInt = -m_ClipRootInteger + math.lerp(0, m_ClipRootInteger, time);

            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(0), Is.EqualTo(expectedLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
            Assert.That(streamECS.GetFloat(0), Is.EqualTo(expectedFloat).Within(1).Ulps);

            // TODO@sonny we need to convert int curve to activate this
            //Assert.That(streamECS.GetInt(0), Is.EqualTo(expectedInt));
        }

        [TestCase(0.0f)]
        [TestCase(0.5f)]
        [TestCase(1.0f)]
        public void PlayingClipNodeShouldYieldSameResultThanClipNodeAsAdditivePlusReferencePose(float time)
        {
            var entity = m_Manager.CreateEntity();
            var anotherEntity = m_Manager.CreateEntity();

            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);
            RigEntityBuilder.SetupRigEntity(anotherEntity, m_Manager, m_Rig);

            var subClipNode = CreateNode<ClipNode>();
            Set.SendMessage(subClipNode, ClipNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(subClipNode, ClipNode.SimulationPorts.Clip, m_ConstantHierarchyClip);
            Set.SetData(subClipNode, ClipNode.KernelPorts.Time, 0);

            var clipNode = CreateNode<ClipNode>();
            Set.SendMessage(clipNode, ClipNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(clipNode, ClipNode.SimulationPorts.Clip, m_LinearHierarchyClip);

            var deltaNode = CreateNode<DeltaPoseNode>();

            Set.SendMessage(deltaNode, DeltaPoseNode.SimulationPorts.Rig, new Rig(){ Value = m_Rig });
            Set.Connect(subClipNode, ClipNode.KernelPorts.Output, deltaNode, DeltaPoseNode.KernelPorts.Subtract);
            Set.Connect(clipNode, ClipNode.KernelPorts.Output, deltaNode, DeltaPoseNode.KernelPorts.Input);

            var layerMixerNode = CreateNode<LayerMixerNode>();
            Set.SendMessage(layerMixerNode, LayerMixerNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(layerMixerNode, LayerMixerNode.SimulationPorts.LayerCount, (ushort)2);
            Set.SetData(layerMixerNode, LayerMixerNode.KernelPorts.BlendingModes, 1, BlendingMode.Additive);
            Set.SetData(layerMixerNode, LayerMixerNode.KernelPorts.Weights, 0, 1.0f);
            Set.SetData(layerMixerNode, LayerMixerNode.KernelPorts.Weights, 1, 1.0f);

            Set.Connect(subClipNode, ClipNode.KernelPorts.Output, layerMixerNode, LayerMixerNode.KernelPorts.Inputs, 0);
            Set.Connect(deltaNode, DeltaPoseNode.KernelPorts.Output, layerMixerNode, LayerMixerNode.KernelPorts.Inputs, 1);

            var entityNode = CreateComponentNode(entity);
            var anotherEntityNode = CreateComponentNode(anotherEntity);

            Set.Connect(layerMixerNode, LayerMixerNode.KernelPorts.Output, entityNode);
            Set.Connect(clipNode, ClipNode.KernelPorts.Output, anotherEntityNode);

            Set.SetData(clipNode, ClipNode.KernelPorts.Time, time);

            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(entity);
            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(anotherEntity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            var anotherStreamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            for (int i = 0; i < m_Rig.Value.Value.Bindings.TranslationBindings.Length; i++)
            {
                Assert.That(streamECS.GetLocalToParentTranslation(i), Is.EqualTo(anotherStreamECS.GetLocalToParentTranslation(i)).Using(TranslationComparer));
            }

            for (int i = 0; i < m_Rig.Value.Value.Bindings.RotationBindings.Length; i++)
            {
                Assert.That(streamECS.GetLocalToParentRotation(i), Is.EqualTo(anotherStreamECS.GetLocalToParentRotation(i)).Using(RotationComparer));
            }

            for (int i = 0; i < m_Rig.Value.Value.Bindings.ScaleBindings.Length; i++)
            {
                Assert.That(streamECS.GetLocalToParentScale(i), Is.EqualTo(anotherStreamECS.GetLocalToParentScale(i)).Using(ScaleComparer));
            }
        }
    }
}
