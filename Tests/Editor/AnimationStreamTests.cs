using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
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

        BlobAssetReference<RigDefinition> m_Rig;
        BlobAssetReference<ClipInstance> m_ConstantHierarchyClip;

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
            m_Rig = CreateTestRigDefinition();

            // Constant hierarchy clip
            {
                var path = "AnimationStreamTestsConstantHierarchyClip.blob";
                var denseClip = BlobFile.ReadBlobAsset<Clip>(path);
                m_ConstantHierarchyClip = ClipManager.Instance.GetClipFor(m_Rig, denseClip);
            }
        }

        protected Entity CreateAndEvaluateSimpleClipGraph(float time, BlobAssetReference<ClipInstance> clip)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.ClipInstance, clip);
            set.SetData(clipNode, ClipNode.KernelPorts.Time, time);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(clipNode, ClipNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            return entity;
        }

        [Test]
        public void InvalidAnimationStreamThrowsExceptions()
        {
            var stream = new AnimationStream<AnimationStreamOffsetPtrDescriptor>();
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
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToRigTranslation(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToRigRotation(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToRigMatrix(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToRigTR(0, out float3 _, out quaternion _));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToRigTranslation(0, float3.zero));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToRigRotation(0, quaternion.identity));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToRigTR(0, float3.zero, quaternion.identity));
        }

        [Test]
        public void AnimationStreamThrowsIndexOutOfRangeExpections()
        {
            var entity = CreateAndEvaluateSimpleClipGraph(0.0f, m_ConstantHierarchyClip);

            var streamECS = AnimationStreamProvider.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedLocalTranslation>(entity),
                m_Manager.GetBuffer<AnimatedLocalRotation>(entity),
                m_Manager.GetBuffer<AnimatedLocalScale>(entity),
                m_Manager.GetBuffer<AnimatedFloat>(entity),
                m_Manager.GetBuffer<AnimatedInt>(entity)
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
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToRigTranslation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToRigRotation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToRigMatrix(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToRigTR(-1, out float3 _, out quaternion _));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToRigTranslation(-1, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToRigRotation(-1, quaternion.identity));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToRigTR(-1, float3.zero, quaternion.identity));

            var readWriteBuffer = GetGraphValueTempNativeBuffer(m_Manager.GetComponentData<GraphOutput>(entity).Buffer);

            var graphStream = AnimationStreamProvider.Create(m_Rig, readWriteBuffer);

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
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToRigTranslation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToRigRotation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToRigMatrix(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToRigTR(-1, out float3 _, out quaternion _));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToRigTranslation(-1, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToRigRotation(-1, quaternion.identity));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToRigTR(-1, float3.zero, quaternion.identity));

            readWriteBuffer.Dispose();
        }

        [Test]
        public void AnimationStreamThrowsArithmeticExceptionsOnInvalidInput()
        {
            var entity = CreateAndEvaluateSimpleClipGraph(0.0f, m_ConstantHierarchyClip);

            var streamECS = AnimationStreamProvider.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedLocalTranslation>(entity),
                m_Manager.GetBuffer<AnimatedLocalRotation>(entity),
                m_Manager.GetBuffer<AnimatedLocalScale>(entity),
                m_Manager.GetBuffer<AnimatedFloat>(entity),
                m_Manager.GetBuffer<AnimatedInt>(entity)
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
            Assert.Throws<System.ArithmeticException>(() => streamECS.SetLocalToRigTranslation(1, nanFloat3));
            Assert.Throws<System.ArithmeticException>(() => streamECS.SetLocalToRigRotation(1, nanQuaternion));
            Assert.Throws<System.ArithmeticException>(() => streamECS.SetLocalToRigTR(1, nanFloat3, nanQuaternion));

            var readWriteBuffer = GetGraphValueTempNativeBuffer(m_Manager.GetComponentData<GraphOutput>(entity).Buffer);
            var graphStream = AnimationStreamProvider.Create(m_Rig, readWriteBuffer);

            Assert.IsFalse(graphStream.IsNull);
            Assert.Throws<System.ArithmeticException>(() => graphStream.SetLocalToParentTranslation(0, infFloat3));
            Assert.Throws<System.ArithmeticException>(() => graphStream.SetLocalToParentRotation(0, infQuaternion));
            Assert.Throws<System.ArithmeticException>(() => graphStream.SetLocalToParentScale(0, infFloat3));
            Assert.Throws<System.ArithmeticException>(() => graphStream.SetFloat(0, float.PositiveInfinity));
            Assert.Throws<System.ArithmeticException>(() => graphStream.SetLocalToRigTranslation(1, nanFloat3));
            Assert.Throws<System.ArithmeticException>(() => graphStream.SetLocalToRigRotation(1, nanQuaternion));
            Assert.Throws<System.ArithmeticException>(() => graphStream.SetLocalToRigTR(1, nanFloat3, nanQuaternion));

            readWriteBuffer.Dispose();
        }
    }
}
