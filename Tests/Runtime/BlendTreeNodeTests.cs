using NUnit.Framework;
using Unity.Animation;
using Unity.Entities;
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

        BlobAssetReference<RigDefinition>   m_Rig;
        BlobAssetReference<BlendTree1D>     m_BlendTree;
        BlobAssetReference<BlendTree1D>     m_NestedBlendTree;

        readonly string blendParameter = "speed";
        readonly string[] blobPath = new string[] {
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
            m_Rig = CreateTestRigDefinition();

            var motionData = new BlendTree1DMotionData[]
            {
                new BlendTree1DMotionData { MotionThreshold = 0.2f, MotionSpeed = 0.2f, MotionType = MotionType.Clip },
                new BlendTree1DMotionData { MotionThreshold = 0.4f, MotionSpeed = 0.4f, MotionType = MotionType.Clip },
                new BlendTree1DMotionData { MotionThreshold = 0.6f, MotionSpeed = 0.6f, MotionType = MotionType.Clip },
                new BlendTree1DMotionData { MotionThreshold = 0.8f, MotionSpeed = 0.8f, MotionType = MotionType.Clip },
                new BlendTree1DMotionData { MotionThreshold = 1.0f, MotionSpeed = 1.0f, MotionType = MotionType.Clip },
            };

            for(int i=0;i<blobPath.Length;i++)
            {
                motionData[i].Motion.Clip = BlobFile.ReadBlobAsset<Clip>(blobPath[i]);
            }

            m_BlendTree = BlendTreeBuilder.CreateBlendTree(motionData, new StringHash(blendParameter));

            var nestedMotionData = new BlendTree1DMotionData[]
            {
                new BlendTree1DMotionData { MotionThreshold = 0.2f, MotionSpeed = 0.2f, MotionType = MotionType.BlendTree1D },
                new BlendTree1DMotionData { MotionThreshold = 0.4f, MotionSpeed = 0.4f, MotionType = MotionType.BlendTree1D },
                new BlendTree1DMotionData { MotionThreshold = 0.6f, MotionSpeed = 0.6f, MotionType = MotionType.BlendTree1D },
                new BlendTree1DMotionData { MotionThreshold = 0.8f, MotionSpeed = 0.8f, MotionType = MotionType.BlendTree1D },
                new BlendTree1DMotionData { MotionThreshold = 1.0f, MotionSpeed = 1.0f, MotionType = MotionType.BlendTree1D },
            };

            for(int i=0;i<nestedMotionData.Length;i++)
            {
                nestedMotionData[i].Motion.BlendTree1D = m_BlendTree;
            }

            m_NestedBlendTree = BlendTreeBuilder.CreateBlendTree(nestedMotionData, new StringHash(blendParameter));
        }

        [Test]
        public void CanSetRigDefinition()
        {
            var blendTree = CreateNode<BlendTree1DNode>();

            Set.SendMessage(blendTree, BlendTree1DNode.SimulationPorts.RigDefinition, m_Rig);

            var otherRig = Set.GetFunctionality(blendTree).ExposeNodeData(blendTree).RigDefinition;

            Assert.That(otherRig, Is.EqualTo(m_Rig));
        }

        [Test]
        public void MustSetRigDefinitionBeforeBlendTreeAsset()
        {
            var motionData = new []
            {
                new BlendTree1DMotionData { MotionThreshold = 1.0f, MotionSpeed = 1.0f },
                new BlendTree1DMotionData { MotionThreshold = 0.8f, MotionSpeed = 1.0f },
            };

            var blendTreeAsset = BlendTreeBuilder.CreateBlendTree(motionData, new StringHash());

            var blendTreeNode = CreateNode<BlendTree1DNode>();

            Assert.Throws<System.InvalidOperationException>(() => Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, blendTreeAsset));
        }

        [Test]
        public void SettingBlendTreeAssetWithInvalidMotionThrow()
        {
            var motionData = new []
            {
                new BlendTree1DMotionData { MotionThreshold = 1.0f, MotionSpeed = 1.0f },
                new BlendTree1DMotionData { MotionThreshold = 0.8f, MotionSpeed = 1.0f },
            };

            var blendTreeAsset = BlendTreeBuilder.CreateBlendTree(motionData, new StringHash());

            var blendTreeNode = CreateNode<BlendTree1DNode>();

            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.RigDefinition, m_Rig);

            Assert.Throws<System.InvalidOperationException>(() => Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, blendTreeAsset));
        }

        [Test]
        public void CanSetBlendTreeAsset()
        {
            var blendTreeNode = CreateNode<BlendTree1DNode>();

            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.RigDefinition, m_Rig);
            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, m_BlendTree);

            var nodeData = Set.GetFunctionality(blendTreeNode).ExposeNodeData(blendTreeNode);

            Assert.That(nodeData.BlendParameter.Id, Is.EqualTo(new StringHash(blendParameter)));
            Assert.That(nodeData.Motions.Count, Is.EqualTo(5));
            Assert.That(nodeData.Motions.Count, Is.EqualTo(m_BlendTree.Value.Motions.Length));
            for(int i=0;i<nodeData.Motions.Count;i++)
            {
                var strongHandle = Set.CastHandle<UberClipNode>(nodeData.Motions[i]);
                var data = Set.GetFunctionality(strongHandle).ExposeNodeData(nodeData.Motions[i]);
                Assert.That(data.ClipInstance, Is.Not.Null);
            }
        }

        [TestCase(0.0f)]
        [TestCase(0.5f)]
        [TestCase(1.0f)]
        [Test]
        public void CanSetBlendParameter(float value)
        {
            var blendTreeNode = CreateNode<BlendTree1DNode>();

            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.RigDefinition, m_Rig);
            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, m_BlendTree);

            var param = new Parameter {
                    Id = new StringHash(blendParameter),
                    Value = value
            };
            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.Parameter, param);

            var nodeData = Set.GetFunctionality(blendTreeNode).ExposeNodeData(blendTreeNode);

            Assert.That(nodeData.BlendParameter.Id, Is.EqualTo(new StringHash(blendParameter)));
            Assert.That(nodeData.BlendParameter.Value, Is.EqualTo(value));
        }

        [TestCase(0.0f)]
        [TestCase(0.5f)]
        [TestCase(1.0f)]
        [Test]
        public void CanSetNestedBlendParameter(float value)
        {
            var blendTreeNode = CreateNode<BlendTree1DNode>();

            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.RigDefinition, m_Rig);
            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, m_NestedBlendTree);

            var param = new Parameter {
                    Id = new StringHash(blendParameter),
                    Value = value
            };
            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.Parameter, param);

            var nodeData = Set.GetFunctionality(blendTreeNode).ExposeNodeData(blendTreeNode);

            var length = nodeData.BlendTree.Value.MotionTypes.Length;
            for(int i=0;i<length;i++)
            {
                if(nodeData.BlendTree.Value.MotionTypes[i] == MotionType.BlendTree1D)
                {
                    var strongHandle = Set.CastHandle<BlendTree1DNode>(nodeData.Motions[i]);
                    var childNodeData = Set.GetFunctionality(strongHandle).ExposeNodeData(strongHandle);

                    Assert.That(childNodeData.BlendParameter.Id, Is.EqualTo(new StringHash(blendParameter)));
                    Assert.That(childNodeData.BlendParameter.Value, Is.EqualTo(value));
                }
            }
        }

        [TestCase(-10.0f)]
        [TestCase(0.0f)]
        [TestCase(0.19f)]
        [TestCase(0.2f)]
        [Test]
        public void ReturnFirstMotionWhenBlendValueIsUnderFirstThreashold(float blendValue)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var blendTreeNode = CreateNode<BlendTree1DNode>();

            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.RigDefinition, m_Rig);
            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, m_BlendTree);

            var param = new Parameter {
                    Id = new StringHash(blendParameter),
                    Value = blendValue
            };
            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.Parameter, param);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(blendTreeNode, BlendTree1DNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var localTranslationBuffer = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity).Reinterpret<float3>();
            var localRotationBuffer = m_Manager.GetBuffer<AnimatedLocalRotation>(entity).Reinterpret<quaternion>();
            var localScaleBuffer = m_Manager.GetBuffer<AnimatedLocalScale>(entity).Reinterpret<float3>();

            Assert.That(localTranslationBuffer[0], Is.EqualTo(m_Clip1LocalTranslation).Using(TranslationComparer), "Root localTranslation doesn't match clip localTranslation");
            Assert.That(localRotationBuffer[0], Is.EqualTo(m_Clip1LocalRotation).Using(RotationComparer), "Root localRotation doesn't match clip localRotation");
            Assert.That(localScaleBuffer[0], Is.EqualTo(m_Clip1LocalScale).Using(ScaleComparer), "Root localScale doesn't match clip localScale");
        }

        [TestCase(10.0f)]
        [TestCase(2.0f)]
        [TestCase(1.01f)]
        [TestCase(1.0f)]
        [Test]
        public void ReturnLastMotionWhenBlendValueIsOverLastThreashold(float blendValue)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var blendTreeNode = CreateNode<BlendTree1DNode>();

            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.RigDefinition, m_Rig);
            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, m_BlendTree);

            var param = new Parameter {
                    Id = new StringHash(blendParameter),
                    Value = blendValue
            };
            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.Parameter, param);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(blendTreeNode, BlendTree1DNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var localTranslationBuffer = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity).Reinterpret<float3>();
            var localRotationBuffer = m_Manager.GetBuffer<AnimatedLocalRotation>(entity).Reinterpret<quaternion>();
            var localScaleBuffer = m_Manager.GetBuffer<AnimatedLocalScale>(entity).Reinterpret<float3>();

            Assert.That(localTranslationBuffer[0], Is.EqualTo(m_Clip5LocalTranslation).Using(TranslationComparer), "Root localTranslation doesn't match clip localTranslation");
            Assert.That(localRotationBuffer[0], Is.EqualTo(m_Clip5LocalRotation).Using(RotationComparer), "Root localRotation doesn't match clip localRotation");
            Assert.That(localScaleBuffer[0], Is.EqualTo(m_Clip5LocalScale).Using(ScaleComparer), "Root localScale doesn't match clip localScale");
        }

        float3 GetExpectedLocalTranslation(float blendValue)
        {
            if (blendValue <= 0.2f)
                return m_Clip1LocalTranslation;
            else if(blendValue > 0.2f && blendValue <= 0.4f)
            {
                return math.lerp(m_Clip1LocalTranslation, m_Clip2LocalTranslation, (blendValue-0.2f)/0.2f);
            }
            else if(blendValue > 0.4f && blendValue <= 0.6f)
            {
                return math.lerp(m_Clip2LocalTranslation, m_Clip3LocalTranslation, (blendValue-0.4f)/0.2f);
            }
            else if(blendValue > 0.6f && blendValue <= 0.8f)
            {
                return math.lerp(m_Clip3LocalTranslation, m_Clip4LocalTranslation, (blendValue-0.6f)/0.2f);
            }
            else if(blendValue > 0.8f && blendValue <= 1.0f)
            {
                return math.lerp(m_Clip4LocalTranslation, m_Clip5LocalTranslation, (blendValue-0.8f)/0.2f);
            }
            else if(blendValue > 1.0f)
            {
                return m_Clip5LocalTranslation;
            }

            return float3.zero;
        }

        quaternion GetExpectedLocalRotation(float blendValue)
        {
            if (blendValue <= 0.2f)
                return m_Clip1LocalRotation;
            else if(blendValue > 0.2f && blendValue <= 0.4f)
            {
                return math.slerp(m_Clip1LocalRotation, m_Clip2LocalRotation, (blendValue-0.2f)/0.2f);
            }
            else if(blendValue > 0.4f && blendValue <= 0.6f)
            {
                return math.slerp(m_Clip2LocalRotation, m_Clip3LocalRotation, (blendValue-0.4f)/0.2f);
            }
            else if(blendValue > 0.6f && blendValue <= 0.8f)
            {
                return math.slerp(m_Clip3LocalRotation, m_Clip4LocalRotation, (blendValue-0.6f)/0.2f);
            }
            else if(blendValue > 0.8f && blendValue <= 1.0f)
            {
                return math.slerp(m_Clip4LocalRotation, m_Clip5LocalRotation, (blendValue-0.8f)/0.2f);
            }
            else if(blendValue > 1.0f)
            {
                return m_Clip5LocalRotation;
            }

            return quaternion.identity;
        }

        float3 GetExpectedLocalScale(float blendValue)
        {
            if (blendValue <= 0.2f)
                return m_Clip1LocalScale;
            else if(blendValue > 0.2f && blendValue <= 0.4f)
            {
                return math.lerp(m_Clip1LocalScale, m_Clip2LocalScale, (blendValue-0.2f)/0.2f);
            }
            else if(blendValue > 0.4f && blendValue <= 0.6f)
            {
                return math.lerp(m_Clip2LocalScale, m_Clip3LocalScale, (blendValue-0.4f)/0.2f);
            }
            else if(blendValue > 0.6f && blendValue <= 0.8f)
            {
                return math.lerp(m_Clip3LocalScale, m_Clip4LocalScale, (blendValue-0.6f)/0.2f);
            }
            else if(blendValue > 0.8f && blendValue <= 1.0f)
            {
                return math.lerp(m_Clip4LocalScale, m_Clip5LocalScale, (blendValue-0.8f)/0.2f);
            }
            else if(blendValue > 1.0f)
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
        [Test]
        public void CanBlendMotion(float blendValue)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var blendTreeNode = CreateNode<BlendTree1DNode>();

            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.RigDefinition, m_Rig);
            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, m_BlendTree);

            var param = new Parameter {
                    Id = new StringHash(blendParameter),
                    Value = blendValue
            };
            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.Parameter, param);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(blendTreeNode, BlendTree1DNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            var expectedLocalTranslation = GetExpectedLocalTranslation(blendValue);
            var expectedLocalRotation = GetExpectedLocalRotation(blendValue);
            var expectedLocalScale = GetExpectedLocalScale(blendValue);

            // Get local translation, rotation, scale.
            var localTranslationBuffer = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity).Reinterpret<float3>();
            var localRotationBuffer = m_Manager.GetBuffer<AnimatedLocalRotation>(entity).Reinterpret<quaternion>();
            var localScaleBuffer = m_Manager.GetBuffer<AnimatedLocalScale>(entity).Reinterpret<float3>();

            Assert.That(localTranslationBuffer[0], Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer), "Root localTranslation doesn't match clip localTranslation");
            Assert.That(localRotationBuffer[0], Is.EqualTo(expectedLocalRotation).Using(RotationComparer), "Root localRotation doesn't match clip localRotation");
            Assert.That(localScaleBuffer[0], Is.EqualTo(expectedLocalScale).Using(ScaleComparer), "Root localScale doesn't match clip localScale");
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
        [Test]
        public void CanBlendNestedBlendTree(float blendValue)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var blendTreeNode = CreateNode<BlendTree1DNode>();

            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.RigDefinition, m_Rig);
            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, m_NestedBlendTree);

            var param = new Parameter {
                    Id = new StringHash(blendParameter),
                    Value = blendValue
            };
            Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.Parameter, param);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(blendTreeNode, BlendTree1DNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            var expectedLocalTranslation = GetExpectedLocalTranslation(blendValue);
            var expectedLocalRotation = GetExpectedLocalRotation(blendValue);
            var expectedLocalScale = GetExpectedLocalScale(blendValue);

            // Get local translation, rotation, scale.
            var localTranslationBuffer = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity).Reinterpret<float3>();
            var localRotationBuffer = m_Manager.GetBuffer<AnimatedLocalRotation>(entity).Reinterpret<quaternion>();
            var localScaleBuffer = m_Manager.GetBuffer<AnimatedLocalScale>(entity).Reinterpret<float3>();

            Assert.That(localTranslationBuffer[0], Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer), "Root localTranslation doesn't match clip localTranslation");
            Assert.That(localRotationBuffer[0], Is.EqualTo(expectedLocalRotation).Using(RotationComparer), "Root localRotation doesn't match clip localRotation");
            Assert.That(localScaleBuffer[0], Is.EqualTo(expectedLocalScale).Using(ScaleComparer), "Root localScale doesn't match clip localScale");
        }
    }
}
