using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using UnityEngine.TestTools;

namespace Unity.Animation.Tests
{
    public class AnimationStreamEditorTests : AnimationTestsFixture, IPrebuildSetup
    {
        float3 m_ClipRootLocalTranslation => new float3(100.0f, 0.0f, 0.0f);
        quaternion m_ClipRootLocalRotation => quaternion.RotateX(math.radians(45.0f));
        float3 m_ClipRootLocalScale => new float3(1.0f, 2.0f, 1.0f);

        float3 m_ClipChildLocalTranslation => new float3(0.0f, 100.0f, 0.0f);
        quaternion m_ClipChildLocalRotation => quaternion.RotateY(math.radians(45.0f));
        float3 m_ClipChildLocalScale => new float3(1.0f, 1.0f, 2.0f);

        const float k_DefaultFloatValue = 2f;
        const int k_DefaultIntValue = 5;

        Rig m_Rig;
        BlobAssetReference<Clip> m_ConstantHierarchyClip;

        const int k_RootId = 0;
        const int k_Child1Id = 1;
        const int k_Child2Id = 2;
        const int k_FloatId = 0;
        const int k_IntId = 0;

        static BlobAssetReference<RigDefinition> CreateTestRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode { ParentIndex = -1, Id = "Root", AxisIndex = -1 },
                new SkeletonNode { ParentIndex = 0, Id = "Child1", AxisIndex = -1 },
                new SkeletonNode { ParentIndex = 0, Id = "Child2", AxisIndex = -1 }
            };

            var customChannels = new IAnimationChannel[]
            {
                new FloatChannel { Id = "myFloat", DefaultValue = k_DefaultFloatValue },
                new IntChannel { Id = "myInt", DefaultValue = k_DefaultIntValue }
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes, null, customChannels);
        }

        public void Setup()
        {
#if UNITY_EDITOR
            PlaymodeTestsEditorSetup.CreateStreamingAssetsDirectory();

            var constantHierarchyClip = CreateConstantDenseClip(
                        new[] { ("Root", m_ClipRootLocalTranslation), ("Child1", m_ClipChildLocalTranslation) },
                        new[] { ("Root", m_ClipRootLocalRotation), ("Child1", m_ClipChildLocalRotation) },
                        new[] { ("Root", m_ClipRootLocalScale),    ("Child1", m_ClipChildLocalScale) });

            var blobPath = "AnimationStreamTestsConstantHierarchyClip.blob";
            BlobFile.WriteBlobAsset(ref constantHierarchyClip, blobPath);
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
                var path = "AnimationStreamTestsConstantHierarchyClip.blob";
                m_ConstantHierarchyClip = BlobFile.ReadBlobAsset<Clip>(path);
                ClipManager.Instance.GetClipFor(m_Rig, m_ConstantHierarchyClip);
            }
        }

        struct TestData
        {
            public Entity Entity;
            public GraphValue<Buffer<AnimatedData>> Buffer;
        }

        TestData CreateAndEvaluateSimpleClipGraph(float time, in Rig rig, BlobAssetReference<Clip> clip)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var clipNode = CreateNode<ClipNode>();
            Set.SendMessage(clipNode, ClipNode.SimulationPorts.Rig, rig);
            Set.SendMessage(clipNode, ClipNode.SimulationPorts.Clip, clip);
            Set.SetData(clipNode, ClipNode.KernelPorts.Time, time);

            var entityNode = CreateComponentNode(entity);
            Set.Connect(clipNode, ClipNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(entity);

            var data = new TestData { Entity = entity, Buffer = CreateGraphValue(clipNode, ClipNode.KernelPorts.Output) };

            m_AnimationGraphSystem.Update();

            return data;
        }

        [Test]
        public void InvalidAnimationStreamThrowsExceptions()
        {
            AnimationStream stream = default;
            Assert.IsTrue(stream.IsNull);
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToParentTranslation(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToParentRotation(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToParentScale(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetFloat(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetInt(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToParentMatrix(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToParentTRS(0, out float3 _, out quaternion _, out float3 _));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToParentTranslation(0, float3.zero));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToParentRotation(0, quaternion.identity));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToParentScale(0, float3.zero));
            Assert.Throws<System.NullReferenceException>(() => stream.SetFloat(0, 0f));
            Assert.Throws<System.NullReferenceException>(() => stream.SetInt(0, 0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToRootTranslation(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToRootRotation(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToRootMatrix(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToRootTR(0, out float3 _, out quaternion _));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToRootTranslation(0, float3.zero));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToRootRotation(0, quaternion.identity));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToRootTR(0, float3.zero, quaternion.identity));
        }

        [Test]
        public void AnimationStreamThrowsIndexOutOfRangeExpections()
        {
            var data = CreateAndEvaluateSimpleClipGraph(0.0f, m_Rig, m_ConstantHierarchyClip);

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
                );

            Assert.IsFalse(streamECS.IsNull);
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToParentTranslation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToParentRotation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToParentScale(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetFloat(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetInt(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToParentMatrix(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToParentTRS(-1, out float3 _, out quaternion _, out float3 _));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToParentTranslation(-1, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToParentRotation(-1, quaternion.identity));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToParentScale(-1, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetFloat(-1, 0f));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetInt(-1, 0));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToRootTranslation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToRootRotation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToRootMatrix(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToRootTR(-1, out float3 _, out quaternion _));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToRootTranslation(-1, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToRootRotation(-1, quaternion.identity));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToRootTR(-1, float3.zero, quaternion.identity));

            var readWriteBuffer = DFGUtils.GetGraphValueTempNativeBuffer(Set, data.Buffer);
            var graphStream = AnimationStream.Create(m_Rig, readWriteBuffer);

            Assert.IsFalse(graphStream.IsNull);
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToParentTranslation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToParentRotation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToParentScale(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetFloat(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetInt(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToParentMatrix(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToParentTRS(-1, out float3 _, out quaternion _, out float3 _));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToParentTranslation(-1, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToParentRotation(-1, quaternion.identity));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToParentScale(-1, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetFloat(-1, 0f));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetInt(-1, 0));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToRootTranslation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToRootRotation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToRootMatrix(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToRootTR(-1, out float3 _, out quaternion _));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToRootTranslation(-1, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToRootRotation(-1, quaternion.identity));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToRootTR(-1, float3.zero, quaternion.identity));

            readWriteBuffer.Dispose();
        }

        [Test]
        public void AnimationStreamThrowsArithmeticExceptionsOnInvalidInput()
        {
            var data = CreateAndEvaluateSimpleClipGraph(0.0f, m_Rig, m_ConstantHierarchyClip);

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
                );

            float3 infFloat3 = new float3(5f, float.PositiveInfinity, 0f);
            quaternion infQuaternion = new quaternion(0f, 0f, float.NegativeInfinity, 1f);
            float3 nanFloat3 = new float3(float.NaN, 0f, 1f);
            quaternion nanQuaternion = new quaternion(0f, 0f, 0f, float.NaN);

            Assert.IsFalse(streamECS.IsNull);
            Assert.Throws<System.ArithmeticException>(() => streamECS.SetLocalToParentTranslation(0, infFloat3));
            Assert.Throws<System.ArithmeticException>(() => streamECS.SetLocalToParentRotation(0, infQuaternion));
            Assert.Throws<System.ArithmeticException>(() => streamECS.SetLocalToParentScale(0, infFloat3));
            Assert.Throws<System.ArithmeticException>(() => streamECS.SetFloat(0, float.PositiveInfinity));
            Assert.Throws<System.ArithmeticException>(() => streamECS.SetLocalToRootTranslation(1, nanFloat3));
            Assert.Throws<System.ArithmeticException>(() => streamECS.SetLocalToRootRotation(1, nanQuaternion));
            Assert.Throws<System.ArithmeticException>(() => streamECS.SetLocalToRootTR(1, nanFloat3, nanQuaternion));

            var readWriteBuffer = DFGUtils.GetGraphValueTempNativeBuffer(Set, data.Buffer);
            var graphStream = AnimationStream.Create(m_Rig, readWriteBuffer);

            Assert.IsFalse(graphStream.IsNull);
            Assert.Throws<System.ArithmeticException>(() => graphStream.SetLocalToParentTranslation(0, infFloat3));
            Assert.Throws<System.ArithmeticException>(() => graphStream.SetLocalToParentRotation(0, infQuaternion));
            Assert.Throws<System.ArithmeticException>(() => graphStream.SetLocalToParentScale(0, infFloat3));
            Assert.Throws<System.ArithmeticException>(() => graphStream.SetFloat(0, float.PositiveInfinity));
            Assert.Throws<System.ArithmeticException>(() => graphStream.SetLocalToRootTranslation(1, nanFloat3));
            Assert.Throws<System.ArithmeticException>(() => graphStream.SetLocalToRootRotation(1, nanQuaternion));
            Assert.Throws<System.ArithmeticException>(() => graphStream.SetLocalToRootTR(1, nanFloat3, nanQuaternion));

            readWriteBuffer.Dispose();
        }
    }
}
