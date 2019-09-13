using System.IO;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.TestTools;

namespace Unity.Animation.Tests
{
    public class BlendTree2DNodeTests : AnimationTestsFixture, IPrebuildSetup
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
        BlobAssetReference<BlendTree2DSimpleDirectionnal>     m_BlendTree;
        BlobAssetReference<BlendTree2DSimpleDirectionnal>     m_NestedBlendTree;

        readonly string blendParameterX = "directionX";
        readonly string blendParameterY = "directionY";

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
            var motionData = new []
            {
                new BlendTree2DMotionData { MotionPosition = new float2(-2, 0), MotionSpeed = 0.2f, Motion = new WeakAssetReference(System.Guid.NewGuid().ToString()), MotionType = MotionType.Clip },
                new BlendTree2DMotionData { MotionPosition = new float2(2, 0), MotionSpeed = 0.4f, Motion = new WeakAssetReference(System.Guid.NewGuid().ToString()), MotionType = MotionType.Clip },
                new BlendTree2DMotionData { MotionPosition = new float2(0, 2), MotionSpeed = 0.6f, Motion = new WeakAssetReference(System.Guid.NewGuid().ToString()), MotionType = MotionType.Clip },
                new BlendTree2DMotionData { MotionPosition = new float2(0, -2), MotionSpeed = 0.8f, Motion = new WeakAssetReference(System.Guid.NewGuid().ToString()), MotionType = MotionType.Clip },
            };


            var blendTree = BlendTreeBuilder.CreateBlendTree2DSimpleDirectionnal(motionData, new StringHash(blendParameterX), new StringHash(blendParameterY));
            var blobPath = "BlendTreeNode2DSimpleDirectionnalTests.blob";
            BlobFile.WriteBlobAsset(ref blendTree, blobPath);

            var denseClip = CreateConstantDenseClip(
                        new[] { ("Root", m_Clip1LocalTranslation) },
                        new[] { ("Root", m_Clip1LocalRotation) },
                        new[] { ("Root", m_Clip1LocalScale)}
            );

            blobPath = motionData[0].Motion.GetGuidStr() + ".blob";
            BlobFile.WriteBlobAsset(ref denseClip, blobPath);

            denseClip = CreateConstantDenseClip(
                        new[] { ("Root", m_Clip2LocalTranslation) },
                        new[] { ("Root", m_Clip2LocalRotation) },
                        new[] { ("Root", m_Clip2LocalScale)}
            );

            blobPath = motionData[1].Motion.GetGuidStr() + ".blob";
            BlobFile.WriteBlobAsset(ref denseClip, blobPath);

            denseClip = CreateConstantDenseClip(
                        new[] { ("Root", m_Clip3LocalTranslation) },
                        new[] { ("Root", m_Clip3LocalRotation) },
                        new[] { ("Root", m_Clip3LocalScale)}
            );

            blobPath = motionData[2].Motion.GetGuidStr() + ".blob";
            BlobFile.WriteBlobAsset(ref denseClip, blobPath);

            denseClip = CreateConstantDenseClip(
                        new[] { ("Root", m_Clip4LocalTranslation) },
                        new[] { ("Root", m_Clip4LocalRotation) },
                        new[] { ("Root", m_Clip4LocalScale)}
            );

            blobPath = motionData[3].Motion.GetGuidStr() + ".blob";
            BlobFile.WriteBlobAsset(ref denseClip, blobPath);

            var assetRef = new WeakAssetReference(System.Guid.NewGuid().ToString());
            var nestedMotionData = new []
            {
                new BlendTree2DMotionData { MotionPosition = new float2(-2, 0), MotionSpeed = 0.2f, Motion = assetRef, MotionType = MotionType.BlendTree2DSimpleDirectionnal },
                new BlendTree2DMotionData { MotionPosition = new float2(2, 0),  MotionSpeed = 0.4f, Motion = assetRef, MotionType = MotionType.BlendTree2DSimpleDirectionnal },
                new BlendTree2DMotionData { MotionPosition = new float2(0, 2),  MotionSpeed = 0.6f, Motion = assetRef, MotionType = MotionType.BlendTree2DSimpleDirectionnal },
                new BlendTree2DMotionData { MotionPosition = new float2(0, -2), MotionSpeed = 0.8f, Motion = assetRef, MotionType = MotionType.BlendTree2DSimpleDirectionnal },
            };


            var nestedBlendTree = BlendTreeBuilder.CreateBlendTree2DSimpleDirectionnal(nestedMotionData, new StringHash(blendParameterX), new StringHash(blendParameterX));
            blobPath = "BlendTreeNode2DTestsNestedBlendTree2DSimpleDirectionnal.blob";
            BlobFile.WriteBlobAsset(ref nestedBlendTree, blobPath);

            blobPath = assetRef.GetGuidStr() + ".blob";
            BlobFile.WriteBlobAsset(ref blendTree, blobPath);

#endif
        }
        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // Create rig
            m_Rig = CreateTestRigDefinition();

            var path = "BlendTreeNode2DSimpleDirectionnalTests.blob";
            m_BlendTree = BlobFile.ReadBlobAsset<BlendTree2DSimpleDirectionnal>(path);

            path = "BlendTreeNode2DTestsNestedBlendTree2DSimpleDirectionnal.blob";
            m_NestedBlendTree = BlobFile.ReadBlobAsset<BlendTree2DSimpleDirectionnal>(path);
        }

        [Test]
        public void CanSetRigDefinition()
        {
            var blendTree = CreateNode<BlendTree2DNode>();

            Set.SendMessage(blendTree, BlendTree2DNode.SimulationPorts.RigDefinition, m_Rig);

            var otherRig = Set.GetFunctionality(blendTree).ExposeNodeData(blendTree).RigDefinition;

            Assert.That(otherRig, Is.EqualTo(m_Rig));
        }

        [Test]
        public void MustSetRigDefinitionBeforeBlendTreeAsset()
        {
            var motionData = new []
            {
                new BlendTree2DMotionData { MotionPosition = 0, MotionSpeed = 1.0f },
                new BlendTree2DMotionData { MotionPosition = 0, MotionSpeed = 1.0f },
            };

            var blendTreeAsset = BlendTreeBuilder.CreateBlendTree2DSimpleDirectionnal(motionData, new StringHash(), new StringHash());

            var blendTreeNode = CreateNode<BlendTree2DNode>();

            Assert.Throws<System.InvalidOperationException>(() => Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.BlendTree, blendTreeAsset));
        }

        [Test]
        public void SettingBlendTreeAssetWithMotionFileNotFoundThrow()
        {
            var motionData = new []
            {
                new BlendTree2DMotionData { MotionPosition = 0, MotionSpeed = 1.0f },
                new BlendTree2DMotionData { MotionPosition = 0, MotionSpeed = 1.0f },
            };

            var blendTreeAsset = BlendTreeBuilder.CreateBlendTree2DSimpleDirectionnal(motionData, new StringHash(), new StringHash());

            var blendTreeNode = CreateNode<BlendTree2DNode>();

            Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.RigDefinition, m_Rig);

            Assert.Throws<FileNotFoundException>(() => Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.BlendTree, blendTreeAsset));
        }

        [Test]
        public void CanSetBlendTreeAsset()
        {
            var blendTreeNode = CreateNode<BlendTree2DNode>();

            Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.RigDefinition, m_Rig);
            Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.BlendTree, m_BlendTree);

            var nodeData = Set.GetFunctionality(blendTreeNode).ExposeNodeData(blendTreeNode);

            Assert.That(nodeData.BlendParameterX.Id, Is.EqualTo(new StringHash(blendParameterX)));
            Assert.That(nodeData.BlendParameterY.Id, Is.EqualTo(new StringHash(blendParameterY)));
            Assert.That(nodeData.Motions.Count, Is.EqualTo(4));
            Assert.That(nodeData.Motions.Count, Is.EqualTo(m_BlendTree.Value.Motions.Length));
            for(int i=0;i<nodeData.Motions.Count;i++)
            {
                var strongHandle = Set.CastHandle<UberClipNode>(nodeData.Motions[i]);
                var data = Set.GetFunctionality(strongHandle).ExposeNodeData(nodeData.Motions[i]);
                Assert.That(data.ClipInstance, Is.Not.Null);
            }
        }

        [TestCase(0.0f, 1.0f)]
        [TestCase(0.5f, -1.0f)]
        [TestCase(1.0f, 10.0f)]
        [Test]
        public void CanSetBlendParameter(float valueX, float valueY)
        {
            var blendTreeNode = CreateNode<BlendTree2DNode>();

            Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.RigDefinition, m_Rig);
            Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.BlendTree, m_BlendTree);

            var paramX = new Parameter {
                    Id = new StringHash(blendParameterX),
                    Value = valueX
            };
            Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.Parameter, paramX);

            var paramY = new Parameter {
                    Id = new StringHash(blendParameterY),
                    Value = valueY
            };
            Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.Parameter, paramY);

            var nodeData = Set.GetFunctionality(blendTreeNode).ExposeNodeData(blendTreeNode);

            Assert.That(nodeData.BlendParameterX.Id, Is.EqualTo(new StringHash(blendParameterX)));
            Assert.That(nodeData.BlendParameterX.Value, Is.EqualTo(valueX));
            Assert.That(nodeData.BlendParameterY.Id, Is.EqualTo(new StringHash(blendParameterY)));
            Assert.That(nodeData.BlendParameterY.Value, Is.EqualTo(valueY));
        }

        [TestCase(0.0f, 1.0f)]
        [TestCase(0.5f, -1.0f)]
        [TestCase(1.0f, 10.0f)]
        [Test]
        public void CanSetNestedBlendParameter(float valueX, float valueY)
        {
            var blendTreeNode = CreateNode<BlendTree2DNode>();

            Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.RigDefinition, m_Rig);
            Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.BlendTree, m_NestedBlendTree);

            var paramX = new Parameter {
                    Id = new StringHash(blendParameterX),
                    Value = valueX
            };
            Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.Parameter, paramX);

            var paramY = new Parameter {
                    Id = new StringHash(blendParameterY),
                    Value = valueY
            };
            Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.Parameter, paramY);

            var nodeData = Set.GetFunctionality(blendTreeNode).ExposeNodeData(blendTreeNode);

            var length = nodeData.BlendTree.Value.Motions.Length;
            for(int i=0;i<length;i++)
            {
                if(nodeData.BlendTree.Value.MotionTypes[i] == MotionType.BlendTree2DSimpleDirectionnal)
                {
                    var strongHandle = Set.CastHandle<BlendTree2DNode>(nodeData.Motions[i]);
                    var childNodeData = Set.GetFunctionality(strongHandle).ExposeNodeData(strongHandle);

                    Assert.That(childNodeData.BlendParameterX.Id, Is.EqualTo(new StringHash(blendParameterX)));
                    Assert.That(childNodeData.BlendParameterX.Value, Is.EqualTo(valueX));
                    Assert.That(childNodeData.BlendParameterY.Id, Is.EqualTo(new StringHash(blendParameterY)));
                    Assert.That(childNodeData.BlendParameterY.Value, Is.EqualTo(valueY));
                }
            }
        }
    }
}
