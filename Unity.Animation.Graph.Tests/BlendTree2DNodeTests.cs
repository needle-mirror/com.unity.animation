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

        Rig m_Rig;
        BlobAssetReference<BlendTree2DSimpleDirectional>     m_BlendTree;

        readonly string[] blobPath = new string[]
        {
            "BlendTreeNode2DTestsClip1.blob",
            "BlendTreeNode2DTestsClip2.blob",
            "BlendTreeNode2DTestsClip3.blob",
            "BlendTreeNode2DTestsClip4.blob"
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
#endif
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // Create rig
            m_Rig = new Rig { Value = CreateTestRigDefinition() };

            var motionData = new BlendTree2DMotionData[]
            {
                new BlendTree2DMotionData { MotionPosition = new float2(-2, 0), MotionSpeed = 0.2f, },
                new BlendTree2DMotionData { MotionPosition = new float2(2, 0), MotionSpeed = 0.4f,  },
                new BlendTree2DMotionData { MotionPosition = new float2(0, 2), MotionSpeed = 0.6f,  },
                new BlendTree2DMotionData { MotionPosition = new float2(0, -2), MotionSpeed = 0.8f, },
            };

            for (int i = 0; i < blobPath.Length; i++)
            {
                motionData[i].Motion.Clip = BlobFile.ReadBlobAsset<Clip>(blobPath[i]);
            }

            m_BlendTree = BlendTreeBuilder.CreateBlendTree2DSimpleDirectional(motionData);
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [Test]
        public void SettingBlendTreeAssetWithInvalidMotionThrow()
        {
            var motionData = new[]
            {
                new BlendTree2DMotionData { MotionPosition = 0, MotionSpeed = 1.0f },
                new BlendTree2DMotionData { MotionPosition = 0, MotionSpeed = 1.0f },
            };

            var blendTreeAsset = BlendTreeBuilder.CreateBlendTree2DSimpleDirectional(motionData);

            var blendTreeNode = CreateNode<BlendTree2DNode>();

            Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.Rig, m_Rig);

            Assert.Throws(Is.TypeOf<System.NullReferenceException>()
                .And.Message.EqualTo("Clip is null."),
                () => Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.BlendTree, blendTreeAsset));
        }

#endif

        [Test]
        public void CanSetRigDefinition()
        {
            var blendTree = CreateNode<BlendTree2DNode>();

            Set.SendMessage(blendTree, BlendTree2DNode.SimulationPorts.Rig, m_Rig);

            Set.SendTest(blendTree, (BlendTree2DNode.Data nodeData) =>
            {
                Assert.That(nodeData.m_RigDefinition.Value.GetHashCode(), Is.EqualTo(m_Rig.Value.Value.GetHashCode()));
            });
        }

        [Test]
        public void CanSetRigDefinitionThenBlendTreeAsset()
        {
            var blendTreeNode = CreateNode<BlendTree2DNode>();

            Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.BlendTree, m_BlendTree);

            Set.SendTest(blendTreeNode, (BlendTree2DNode.Data nodeData) =>
            {
                Assert.That(nodeData.m_Motions.Count, Is.EqualTo(4));
                Assert.That(nodeData.m_Motions.Count, Is.EqualTo(m_BlendTree.Value.Motions.Length));

                for (int i = 0; i < nodeData.m_Motions.Count; i++)
                {
                    var strongHandle = Set.CastHandle<UberClipNode>(nodeData.m_Motions[i]);
                    Set.SendTest(strongHandle, (UberClipNode.Data data) =>
                    {
                        Assert.That(data.m_Clip, Is.Not.Null);
                    });
                }
            });
        }

        [Test]
        public void CanSetBlendTreeThenRigDefinitionAsset()
        {
            var blendTreeNode = CreateNode<BlendTree2DNode>();

            Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.BlendTree, m_BlendTree);
            Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.Rig, m_Rig);

            Set.SendTest(blendTreeNode, (BlendTree2DNode.Data nodeData) =>
            {
                Assert.That(nodeData.m_Motions.Count, Is.EqualTo(4));
                Assert.That(nodeData.m_Motions.Count, Is.EqualTo(m_BlendTree.Value.Motions.Length));

                for (int i = 0; i < nodeData.m_Motions.Count; i++)
                {
                    var strongHandle = Set.CastHandle<UberClipNode>(nodeData.m_Motions[i]);
                    Set.SendTest(strongHandle, (UberClipNode.Data data) =>
                    {
                        Assert.That(data.m_Clip, Is.Not.Null);
                    });
                }
            });
        }
    }
}
