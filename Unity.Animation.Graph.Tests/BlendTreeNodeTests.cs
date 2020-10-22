using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Mathematics;
using UnityEngine.TestTools;

namespace Unity.Animation.Tests
{
    public class BlendTreeNode1DTests : AnimationTestsFixture, IPrebuildSetup
    {
        float3          m_Clip1LocalTranslation => new float3(10.0f, 10.0f, 10.0f);
        quaternion      m_Clip1LocalRotation => quaternion.RotateX(math.radians(10.0f));
        float3          m_Clip1LocalScale => new float3(10.0f, 10.0f, 10.0f);

        float3          m_Clip2LocalTranslation => new float3(20.0f, 20.0f, 20.0f);
        quaternion      m_Clip2LocalRotation => quaternion.RotateX(math.radians(20.0f));
        float3          m_Clip2LocalScale => new float3(20.0f, 20.0f, 20.0f);

        float3          m_Clip3LocalTranslation => new float3(30.0f, 30.0f, 30.0f);
        quaternion      m_Clip3LocalRotation => quaternion.RotateX(math.radians(30.0f));
        float3          m_Clip3LocalScale => new float3(30.0f, 30.0f, 30.0f);

        float3          m_Clip4LocalTranslation => new float3(40.0f, 40.0f, 40.0f);
        quaternion      m_Clip4LocalRotation => quaternion.RotateX(math.radians(40.0f));
        float3          m_Clip4LocalScale => new float3(40.0f, 40.0f, 40.0f);

        float3          m_Clip5LocalTranslation => new float3(50.0f, 50.0f, 50.0f);
        quaternion      m_Clip5LocalRotation => quaternion.RotateX(math.radians(50.0f));
        float3          m_Clip5LocalScale => new float3(50.0f, 50.0f, 50.0f);

        Rig m_Rig;
        BlobAssetReference<BlendTree1D> m_BlendTree;

        List<BlobAssetReference<Clip>> m_Clips;

        readonly string[] blobPath = new string[]
        {
            "BlendTreeNode1DTestsClip1.blob",
            "BlendTreeNode1DTestsClip2.blob",
            "BlendTreeNode1DTestsClip3.blob",
            "BlendTreeNode1DTestsClip4.blob",
            "BlendTreeNode1DTestsClip5.blob",
        };

        BlobAssetReference<RigDefinition> CreateTestRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode { ParentIndex = -1, Id = "Root", AxisIndex = -1 }
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes);
        }

        public void Setup()
        {
#if UNITY_EDITOR
            PlaymodeTestsEditorSetup.CreateStreamingAssetsDirectory();

            var denseClip = CreateConstantDenseClip(
                new[] { ("Root", m_Clip1LocalTranslation) },
                new[] { ("Root", m_Clip1LocalRotation) },
                new[] { ("Root", m_Clip1LocalScale)}
            );

            BlobFile.WriteBlobAsset(ref denseClip, blobPath[0]);

            denseClip = CreateConstantDenseClip(
                new[] { ("Root", m_Clip2LocalTranslation) },
                new[] { ("Root", m_Clip2LocalRotation) },
                new[] { ("Root", m_Clip2LocalScale)}
            );

            BlobFile.WriteBlobAsset(ref denseClip, blobPath[1]);

            denseClip = CreateConstantDenseClip(
                new[] { ("Root", m_Clip3LocalTranslation) },
                new[] { ("Root", m_Clip3LocalRotation) },
                new[] { ("Root", m_Clip3LocalScale)}
            );

            BlobFile.WriteBlobAsset(ref denseClip, blobPath[2]);

            denseClip = CreateConstantDenseClip(
                new[] { ("Root", m_Clip4LocalTranslation) },
                new[] { ("Root", m_Clip4LocalRotation) },
                new[] { ("Root", m_Clip4LocalScale)}
            );

            BlobFile.WriteBlobAsset(ref denseClip, blobPath[3]);

            denseClip = CreateConstantDenseClip(
                new[] { ("Root", m_Clip5LocalTranslation) },
                new[] { ("Root", m_Clip5LocalRotation) },
                new[] { ("Root", m_Clip5LocalScale)}
            );

            BlobFile.WriteBlobAsset(ref denseClip, blobPath[4]);
#endif
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // Create rig
            m_Rig = new Rig { Value = CreateTestRigDefinition() };

            var motionData = new BlendTree1DMotionData[]
            {
                new BlendTree1DMotionData { MotionThreshold = 0.2f, MotionSpeed = 0.2f},
                new BlendTree1DMotionData { MotionThreshold = 0.4f, MotionSpeed = 0.4f},
                new BlendTree1DMotionData { MotionThreshold = 0.6f, MotionSpeed = 0.6f},
                new BlendTree1DMotionData { MotionThreshold = 0.8f, MotionSpeed = 0.8f},
                new BlendTree1DMotionData { MotionThreshold = 1.0f, MotionSpeed = 1.0f},
            };

            m_Clips = new List<BlobAssetReference<Clip>>(blobPath.Length);

            for (int i = 0; i < blobPath.Length; i++)
            {
                m_Clips.Add(BlobFile.ReadBlobAsset<Clip>(blobPath[i]));
                motionData[i].Motion.Clip = m_Clips[i];
            }

            m_BlendTree = BlendTreeBuilder.CreateBlendTree(motionData);
        }

        struct TestData
        {
            public Entity Entity;
            public NodeHandle<BlendTree1DNode> BlendTreeNode;
        }

        TestData CreateSimpleBlendTreeGraph(float blendValue, BlobAssetReference<RigDefinition> rig)
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, Entity.Null);

            var blendTreeNode = CreateNode<BlendTree1DNode>();
            var entityNode = CreateComponentNode(entity);

            Set.Connect(blendTreeNode, BlendTree1DNode.KernelPorts.Output, entityNode);

            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, m_BlendTree);
            Set.SetData(blendTreeNode, BlendTree1DNode.KernelPorts.BlendParameter, blendValue);

            return new TestData { Entity = entity, BlendTreeNode = blendTreeNode };
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [Test]
        public void SettingBlendTreeAssetWithInvalidMotionThrow()
        {
            var motionData = new[]
            {
                new BlendTree1DMotionData { MotionThreshold = 1.0f, MotionSpeed = 1.0f },
                new BlendTree1DMotionData { MotionThreshold = 0.8f, MotionSpeed = 1.0f },
            };

            var blendTreeAsset = BlendTreeBuilder.CreateBlendTree(motionData);

            var blendTreeNode = CreateNode<BlendTree1DNode>();

            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.Rig, m_Rig);

            Assert.Throws(Is.TypeOf<System.NullReferenceException>()
                .And.Message.EqualTo("Clip is null."),
                () => Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, blendTreeAsset));
        }

#endif

        [Test]
        public void CanSetRigDefinition()
        {
            var blendTree = CreateNode<BlendTree1DNode>();

            Set.SendMessage(blendTree, BlendTree1DNode.SimulationPorts.Rig, m_Rig);

            Set.SendTest(blendTree, (BlendTree1DNode.Data data) =>
            {
                Assert.That(data.m_RigDefinition.Value.GetHashCode(), Is.EqualTo(m_Rig.Value.Value.GetHashCode()));
            });
        }

        [Test]
        public void CanSetRigDefinitionThenBlendTreeAsset()
        {
            var blendTreeNode = CreateNode<BlendTree1DNode>();

            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, m_BlendTree);

            Set.SendTest(blendTreeNode, (BlendTree1DNode.Data blendTreeData) =>
            {
                Assert.That(blendTreeData.m_Motions.Count, Is.EqualTo(5));
                Assert.That(blendTreeData.m_Motions.Count, Is.EqualTo(m_BlendTree.Value.Motions.Length));

                for (int i = 0; i < blendTreeData.m_Motions.Count; ++i)
                {
                    var handle = Set.CastHandle<UberClipNode>(blendTreeData.m_Motions[i]);
                    Set.SendTest(handle, (UberClipNode.Data clipNodeData) =>
                    {
                        Assert.That(clipNodeData.m_Clip, Is.Not.Null);
                    });
                }
            });
        }

        [Test]
        public void CanSetBlendTreeThenRigDefinitionAsset()
        {
            var blendTreeNode = CreateNode<BlendTree1DNode>();

            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, m_BlendTree);
            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.Rig, m_Rig);

            Set.SendTest(blendTreeNode, (BlendTree1DNode.Data blendTreeData) =>
            {
                Assert.That(blendTreeData.m_Motions.Count, Is.EqualTo(5));
                Assert.That(blendTreeData.m_Motions.Count, Is.EqualTo(m_BlendTree.Value.Motions.Length));

                for (int i = 0; i < blendTreeData.m_Motions.Count; ++i)
                {
                    var handle = Set.CastHandle<UberClipNode>(blendTreeData.m_Motions[i]);
                    Set.SendTest(handle, (UberClipNode.Data clipNodeData) =>
                    {
                        Assert.That(clipNodeData.m_Clip, Is.Not.Null);
                    });
                }
            });
        }

        List<BlobAssetReference<ClipInstance>> GetBlendTreeNodeClipInstances(NodeSet set, NodeHandle<BlendTree1DNode> nodeHandle)
        {
            var list = new List<BlobAssetReference<ClipInstance>>();

            set.SendTest(nodeHandle, (BlendTree1DNode.Data blendTreeNodeData) =>
            {
                for (int i = 0; i < blendTreeNodeData.m_Motions.Count; i++)
                {
                    var uberClipNode = Set.CastHandle<UberClipNode>(blendTreeNodeData.m_Motions[i]);
                    Assert.AreNotEqual(default(NodeHandle<UberClipNode>), uberClipNode);

                    set.SendTest(uberClipNode, (UberClipNode.Data uberClipNodeData) =>
                    {
                        var configurableClipNode = uberClipNodeData.m_ClipNode;
                        Assert.AreNotEqual(default(NodeHandle<ConfigurableClipNode>), configurableClipNode);

                        set.SendTest(configurableClipNode, (ConfigurableClipNode.Data configurableClipNodeData) =>
                        {
                            var clipNode = configurableClipNodeData.m_ClipNode;
                            Assert.AreNotEqual(default(NodeHandle<ClipNode>), clipNode);

                            set.SendTest(clipNode, (ClipNode.Data clipNodeData) =>
                            {
                                Assert.AreNotEqual(BlobAssetReference<ClipInstance>.Null, clipNodeData.m_KernelData.ClipInstance);
                                list.Add(clipNodeData.m_KernelData.ClipInstance);
                            });
                        });
                    });
                }
                ;
            });

            return list;
        }

        [Test]
        public void ChangingRigDefinitionShouldUpdateClipInstance()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode { ParentIndex = -1, Id = "Root", AxisIndex = -1 },
                new SkeletonNode { ParentIndex = 0, Id = "Hips", AxisIndex = -1 }
            };

            var rig2 = new Rig { Value = RigBuilder.CreateRigDefinition(skeletonNodes) };

            var expectedRig1ClipInstances = new List<BlobAssetReference<ClipInstance>>(m_Clips.Count);
            var expectedRig2ClipInstances = new List<BlobAssetReference<ClipInstance>>(m_Clips.Count);
            for (int i = 0; i < m_Clips.Count; i++)
            {
                expectedRig1ClipInstances.Add(ClipManager.Instance.GetClipFor(m_Rig, m_Clips[i]));
                expectedRig2ClipInstances.Add(ClipManager.Instance.GetClipFor(rig2, m_Clips[i]));
            }
            Assert.That(expectedRig1ClipInstances.Count, Is.EqualTo(m_BlendTree.Value.Motions.Length));
            Assert.That(expectedRig2ClipInstances.Count, Is.EqualTo(m_BlendTree.Value.Motions.Length));

            // Validate that both list are unique
            for (int i = 0; i < m_Clips.Count; i++)
            {
                Assert.That(expectedRig1ClipInstances[i], Is.Not.EqualTo(expectedRig2ClipInstances[i]));
            }

            var blendTreeNode = CreateNode<BlendTree1DNode>();

            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, m_BlendTree);
            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.Rig, m_Rig);

            Set.SendTest(blendTreeNode, (BlendTree1DNode.Data nodeData) =>
            {
                Assert.That(nodeData.m_Motions.Count, Is.EqualTo(5));
                Assert.That(nodeData.m_Motions.Count, Is.EqualTo(m_BlendTree.Value.Motions.Length));
            });

            var clipInstances = GetBlendTreeNodeClipInstances(Set, blendTreeNode);
            Assert.That(clipInstances.Count, Is.EqualTo(m_BlendTree.Value.Motions.Length));

            for (int i = 0; i < m_Clips.Count; i++)
            {
                Assert.That(clipInstances[i], Is.EqualTo(expectedRig1ClipInstances[i]));
            }

            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.Rig, rig2);
            clipInstances = GetBlendTreeNodeClipInstances(Set, blendTreeNode);
            Assert.That(clipInstances.Count, Is.EqualTo(m_BlendTree.Value.Motions.Length));

            for (int i = 0; i < m_Clips.Count; i++)
            {
                Assert.That(clipInstances[i], Is.EqualTo(expectedRig2ClipInstances[i]));
            }
        }

        [TestCase(-10.0f)]
        [TestCase(0.0f)]
        [TestCase(0.19f)]
        [TestCase(0.2f)]
        public void ReturnFirstMotionWhenBlendValueIsUnderFirstThreashold(float blendValue)
        {
            var data = CreateSimpleBlendTreeGraph(blendValue, m_Rig);
            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(m_Clip1LocalTranslation).Using(TranslationComparer), "Root localTranslation doesn't match clip localTranslation");
            Assert.That(streamECS.GetLocalToParentRotation(0), Is.EqualTo(m_Clip1LocalRotation).Using(RotationComparer), "Root localRotation doesn't match clip localRotation");
            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(m_Clip1LocalScale).Using(ScaleComparer), "Root localScale doesn't match clip localScale");
        }

        [TestCase(10.0f)]
        [TestCase(2.0f)]
        [TestCase(1.01f)]
        [TestCase(1.0f)]
        public void ReturnLastMotionWhenBlendValueIsOverLastThreashold(float blendValue)
        {
            var data = CreateSimpleBlendTreeGraph(blendValue, m_Rig);
            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(m_Clip5LocalTranslation).Using(TranslationComparer), "Root localTranslation doesn't match clip localTranslation");
            Assert.That(streamECS.GetLocalToParentRotation(0), Is.EqualTo(m_Clip5LocalRotation).Using(RotationComparer), "Root localRotation doesn't match clip localRotation");
            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(m_Clip5LocalScale).Using(ScaleComparer), "Root localScale doesn't match clip localScale");
        }

        float3 GetExpectedLocalTranslation(float blendValue)
        {
            if (blendValue <= 0.2f)
                return m_Clip1LocalTranslation;
            else if (blendValue > 0.2f && blendValue <= 0.4f)
            {
                return math.lerp(m_Clip1LocalTranslation, m_Clip2LocalTranslation, (blendValue - 0.2f) / 0.2f);
            }
            else if (blendValue > 0.4f && blendValue <= 0.6f)
            {
                return math.lerp(m_Clip2LocalTranslation, m_Clip3LocalTranslation, (blendValue - 0.4f) / 0.2f);
            }
            else if (blendValue > 0.6f && blendValue <= 0.8f)
            {
                return math.lerp(m_Clip3LocalTranslation, m_Clip4LocalTranslation, (blendValue - 0.6f) / 0.2f);
            }
            else if (blendValue > 0.8f && blendValue <= 1.0f)
            {
                return math.lerp(m_Clip4LocalTranslation, m_Clip5LocalTranslation, (blendValue - 0.8f) / 0.2f);
            }
            else if (blendValue > 1.0f)
            {
                return m_Clip5LocalTranslation;
            }

            return float3.zero;
        }

        quaternion GetExpectedLocalRotation(float blendValue)
        {
            if (blendValue <= 0.2f)
                return m_Clip1LocalRotation;
            else if (blendValue > 0.2f && blendValue <= 0.4f)
            {
                return math.slerp(m_Clip1LocalRotation, m_Clip2LocalRotation, (blendValue - 0.2f) / 0.2f);
            }
            else if (blendValue > 0.4f && blendValue <= 0.6f)
            {
                return math.slerp(m_Clip2LocalRotation, m_Clip3LocalRotation, (blendValue - 0.4f) / 0.2f);
            }
            else if (blendValue > 0.6f && blendValue <= 0.8f)
            {
                return math.slerp(m_Clip3LocalRotation, m_Clip4LocalRotation, (blendValue - 0.6f) / 0.2f);
            }
            else if (blendValue > 0.8f && blendValue <= 1.0f)
            {
                return math.slerp(m_Clip4LocalRotation, m_Clip5LocalRotation, (blendValue - 0.8f) / 0.2f);
            }
            else if (blendValue > 1.0f)
            {
                return m_Clip5LocalRotation;
            }

            return quaternion.identity;
        }

        float3 GetExpectedLocalScale(float blendValue)
        {
            if (blendValue <= 0.2f)
                return m_Clip1LocalScale;
            else if (blendValue > 0.2f && blendValue <= 0.4f)
            {
                return math.lerp(m_Clip1LocalScale, m_Clip2LocalScale, (blendValue - 0.2f) / 0.2f);
            }
            else if (blendValue > 0.4f && blendValue <= 0.6f)
            {
                return math.lerp(m_Clip2LocalScale, m_Clip3LocalScale, (blendValue - 0.4f) / 0.2f);
            }
            else if (blendValue > 0.6f && blendValue <= 0.8f)
            {
                return math.lerp(m_Clip3LocalScale, m_Clip4LocalScale, (blendValue - 0.6f) / 0.2f);
            }
            else if (blendValue > 0.8f && blendValue <= 1.0f)
            {
                return math.lerp(m_Clip4LocalScale, m_Clip5LocalScale, (blendValue - 0.8f) / 0.2f);
            }
            else if (blendValue > 1.0f)
            {
                return m_Clip5LocalScale;
            }

            return float3.zero;
        }

        [TestCase(0.2f)]
        [TestCase(0.3f)]
        [TestCase(0.4f)]
        [TestCase(0.5f)]
        [TestCase(0.6f)]
        [TestCase(0.7f)]
        [TestCase(0.8f)]
        [TestCase(0.9f)]
        [TestCase(1.0f)]
        public void CanBlendMotion(float blendValue)
        {
            var data = CreateSimpleBlendTreeGraph(blendValue, m_Rig);
            m_AnimationGraphSystem.Update();

            var expectedLocalTranslation = GetExpectedLocalTranslation(blendValue);
            var expectedLocalRotation = GetExpectedLocalRotation(blendValue);
            var expectedLocalScale = GetExpectedLocalScale(blendValue);

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer), "Root localTranslation doesn't match clip localTranslation");
            Assert.That(streamECS.GetLocalToParentRotation(0), Is.EqualTo(expectedLocalRotation).Using(RotationComparer), "Root localRotation doesn't match clip localRotation");
            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(expectedLocalScale).Using(ScaleComparer), "Root localScale doesn't match clip localScale");
        }

        // Clip Duration can be computed like this:
        // Clip Duration / Speed * Clip Weight
        [TestCase(0.2f, 1.0f / 0.2f)]
        [TestCase(0.3f, (1.0f / 0.2f * 0.5f) + (1.0f / 0.4f * 0.5f))]
        [TestCase(0.4f, 1.0f / 0.4f)]
        [TestCase(0.5f, (1.0f / 0.4f * 0.5f) + (1.0f / 0.6f * 0.5f))]
        [TestCase(0.6f, 1.0f / 0.6f)]
        [TestCase(0.7f, (1.0f / 0.6f * 0.5f) + (1.0f / 0.8f * 0.5f))]
        [TestCase(0.8f, 1.0f / 0.8f)]
        [TestCase(0.9f, (1.0f / 0.8f * 0.5f) + (1.0f / 1.0f * 0.5f))]
        [TestCase(1.0f, 1.0f / 1.0f)]
        public void CanComputeDuration(float blendValue, float expectedDuration)
        {
            var data = CreateSimpleBlendTreeGraph(blendValue, m_Rig);
            var graphValue = CreateGraphValue(data.BlendTreeNode, BlendTree1DNode.KernelPorts.Duration);

            m_AnimationGraphSystem.Update();

            var value = Set.GetValueBlocking(graphValue);

            Assert.That(value, Is.EqualTo(expectedDuration).Using(FloatComparer), "BlendTree duration doesn't expected value");
        }
    }
}
