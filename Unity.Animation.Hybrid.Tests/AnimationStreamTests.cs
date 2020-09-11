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

            var customChannels = new IAnimationChannel[]
            {
                new FloatChannel { Id = "myFloat", DefaultValue = k_DefaultFloatValue },
                new IntChannel { Id = "myInt", DefaultValue = k_DefaultIntValue }
            };

            // Create rig
            m_Rig = new Rig { Value = CreateTestRigDefinition(3, customChannels) };

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
            SetupRigEntity(entity, rig, Entity.Null);

            var clipNode = CreateNode<ClipNode>();
            Set.SendMessage(clipNode, ClipNode.SimulationPorts.Rig, rig);
            Set.SendMessage(clipNode, ClipNode.SimulationPorts.Clip, clip);
            Set.SetData(clipNode, ClipNode.KernelPorts.Time, time);

            var entityNode = CreateComponentNode(entity);
            Set.Connect(clipNode, ClipNode.KernelPorts.Output, entityNode);

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
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToParentInverseMatrix(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToParentTR(0, out float3 _, out quaternion _));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToParentTRS(0, out float3 _, out quaternion _, out float3 _));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToParentTranslation(0, float3.zero));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToParentRotation(0, quaternion.identity));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToParentScale(0, float3.zero));
            Assert.Throws<System.NullReferenceException>(() => stream.SetFloat(0, 0f));
            Assert.Throws<System.NullReferenceException>(() => stream.SetInt(0, 0));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToParentTR(0, float3.zero, quaternion.identity));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToParentTRS(0, float3.zero, quaternion.identity, float3.zero));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToRootTranslation(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToRootRotation(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToRootScale(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToRootMatrix(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToRootInverseMatrix(0));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToRootTR(0, out float3 _, out quaternion _));
            Assert.Throws<System.NullReferenceException>(() => stream.GetLocalToRootTRS(0, out float3 _, out quaternion _, out float3 _));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToRootTranslation(0, float3.zero));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToRootRotation(0, quaternion.identity));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToRootScale(0, float3.zero));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToRootTR(0, float3.zero, quaternion.identity));
            Assert.Throws<System.NullReferenceException>(() => stream.SetLocalToRootTRS(0, float3.zero, quaternion.identity, float3.zero));
        }

        [Test]
        public void CannotWriteToReadOnlyStream()
        {
            var data = CreateAndEvaluateSimpleClipGraph(0.0f, m_Rig, m_ConstantHierarchyClip);

            var stream = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            Assert.Throws<System.InvalidOperationException>(() => stream.ClearMasks());
            Assert.Throws<System.InvalidOperationException>(() => stream.SetMasks(true));
            Assert.Throws<System.InvalidOperationException>(() => stream.SetFloat(0, 0.0f));
            Assert.Throws<System.InvalidOperationException>(() => stream.SetInt(0, 0));
            Assert.Throws<System.InvalidOperationException>(() => stream.SetLocalToParentRotation(0, quaternion.identity));
            Assert.Throws<System.InvalidOperationException>(() => stream.SetLocalToParentScale(0, float3.zero));
            Assert.Throws<System.InvalidOperationException>(() => stream.SetLocalToParentTR(0, float3.zero, quaternion.identity));
            Assert.Throws<System.InvalidOperationException>(() => stream.SetLocalToParentTranslation(0, float3.zero));

            Assert.Throws<System.InvalidOperationException>(() => stream.SetLocalToParentTRS(0, float3.zero, quaternion.identity, float3.zero));
            Assert.Throws<System.InvalidOperationException>(() => stream.SetLocalToRootRotation(0, quaternion.identity));
            Assert.Throws<System.InvalidOperationException>(() => stream.SetLocalToRootScale(0, float3.zero));
            Assert.Throws<System.InvalidOperationException>(() => stream.SetLocalToRootTR(0, float3.zero, quaternion.identity));
            Assert.Throws<System.InvalidOperationException>(() => stream.SetLocalToRootTranslation(0, float3.zero));
            Assert.Throws<System.InvalidOperationException>(() => stream.SetLocalToRootTRS(0, float3.zero, quaternion.identity, float3.zero));
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
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToParentInverseMatrix(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToParentTR(-1, out float3 _, out quaternion _));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToParentTRS(-1, out float3 _, out quaternion _, out float3 _));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToParentTranslation(-1, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToParentRotation(-1, quaternion.identity));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToParentScale(-1, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetFloat(-1, 0f));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetInt(-1, 0));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToParentTR(-1, float3.zero, quaternion.identity));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToParentTRS(-1, float3.zero, quaternion.identity, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToRootTranslation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToRootRotation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToRootScale(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToRootMatrix(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToRootInverseMatrix(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToRootTR(-1, out float3 _, out quaternion _));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.GetLocalToRootTRS(-1, out float3 _, out quaternion _, out float3 _));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToRootTranslation(-1, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToRootRotation(-1, quaternion.identity));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToRootScale(-1, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToRootTR(-1, float3.zero, quaternion.identity));
            Assert.Throws<System.IndexOutOfRangeException>(() => streamECS.SetLocalToRootTRS(-1, float3.zero, quaternion.identity, float3.zero));

            var readWriteBuffer = DFGUtils.GetGraphValueTempNativeBuffer(Set, data.Buffer);
            var graphStream = AnimationStream.Create(m_Rig, readWriteBuffer);

            Assert.IsFalse(graphStream.IsNull);
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToParentTranslation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToParentRotation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToParentScale(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetFloat(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetInt(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToParentMatrix(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToParentInverseMatrix(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToParentTR(-1, out float3 _, out quaternion _));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToParentTRS(-1, out float3 _, out quaternion _, out float3 _));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToParentTranslation(-1, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToParentRotation(-1, quaternion.identity));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToParentScale(-1, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetFloat(-1, 0f));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetInt(-1, 0));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToParentTR(-1, float3.zero, quaternion.identity));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToParentTRS(-1, float3.zero, quaternion.identity, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToRootTranslation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToRootRotation(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToRootScale(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToRootMatrix(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToRootInverseMatrix(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToRootTR(-1, out float3 _, out quaternion _));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.GetLocalToRootTRS(-1, out float3 _, out quaternion _, out float3 _));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToRootTranslation(-1, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToRootRotation(-1, quaternion.identity));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToRootScale(-1, float3.zero));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToRootTR(-1, float3.zero, quaternion.identity));
            Assert.Throws<System.IndexOutOfRangeException>(() => graphStream.SetLocalToRootTRS(-1, float3.zero, quaternion.identity, float3.zero));

            readWriteBuffer.Dispose();
        }

        [Test]
        public void AnimationStreamThrowsNotFiniteNumberExceptionsOnInvalidInput()
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
            Assert.Throws<System.NotFiniteNumberException>(() => streamECS.SetLocalToParentTranslation(0, infFloat3));
            Assert.Throws<System.NotFiniteNumberException>(() => streamECS.SetLocalToParentRotation(0, infQuaternion));
            Assert.Throws<System.NotFiniteNumberException>(() => streamECS.SetLocalToParentScale(0, infFloat3));
            Assert.Throws<System.NotFiniteNumberException>(() => streamECS.SetFloat(0, float.PositiveInfinity));
            Assert.Throws<System.NotFiniteNumberException>(() => streamECS.SetLocalToParentTR(0, infFloat3, infQuaternion));
            Assert.Throws<System.NotFiniteNumberException>(() => streamECS.SetLocalToParentTRS(0, infFloat3, infQuaternion, infFloat3));
            Assert.Throws<System.NotFiniteNumberException>(() => streamECS.SetLocalToRootTranslation(1, nanFloat3));
            Assert.Throws<System.NotFiniteNumberException>(() => streamECS.SetLocalToRootRotation(1, nanQuaternion));
            Assert.Throws<System.NotFiniteNumberException>(() => streamECS.SetLocalToRootScale(1, nanFloat3));
            Assert.Throws<System.NotFiniteNumberException>(() => streamECS.SetLocalToRootTR(1, nanFloat3, nanQuaternion));
            Assert.Throws<System.NotFiniteNumberException>(() => streamECS.SetLocalToRootTRS(1, nanFloat3, nanQuaternion, nanFloat3));

            var readWriteBuffer = DFGUtils.GetGraphValueTempNativeBuffer(Set, data.Buffer);
            var graphStream = AnimationStream.Create(m_Rig, readWriteBuffer);

            Assert.IsFalse(graphStream.IsNull);
            Assert.Throws<System.NotFiniteNumberException>(() => graphStream.SetLocalToParentTranslation(0, infFloat3));
            Assert.Throws<System.NotFiniteNumberException>(() => graphStream.SetLocalToParentRotation(0, infQuaternion));
            Assert.Throws<System.NotFiniteNumberException>(() => graphStream.SetLocalToParentScale(0, infFloat3));
            Assert.Throws<System.NotFiniteNumberException>(() => graphStream.SetFloat(0, float.PositiveInfinity));
            Assert.Throws<System.NotFiniteNumberException>(() => graphStream.SetLocalToParentTR(0, infFloat3, infQuaternion));
            Assert.Throws<System.NotFiniteNumberException>(() => graphStream.SetLocalToParentTRS(0, infFloat3, infQuaternion, infFloat3));
            Assert.Throws<System.NotFiniteNumberException>(() => graphStream.SetLocalToRootTranslation(1, nanFloat3));
            Assert.Throws<System.NotFiniteNumberException>(() => graphStream.SetLocalToRootRotation(1, nanQuaternion));
            Assert.Throws<System.NotFiniteNumberException>(() => graphStream.SetLocalToRootScale(1, nanFloat3));
            Assert.Throws<System.NotFiniteNumberException>(() => graphStream.SetLocalToRootTR(1, nanFloat3, nanQuaternion));
            Assert.Throws<System.NotFiniteNumberException>(() => graphStream.SetLocalToRootTRS(1, nanFloat3, nanQuaternion, nanFloat3));

            readWriteBuffer.Dispose();
        }

        [Test]
        [TestCase(1, 0, 0)]
        [TestCase(2, 0, 0)]
        [TestCase(30, 0, 0)]
        [TestCase(130, 0, 0)]
        [TestCase(0, 1, 0)]
        [TestCase(0, 10, 0)]
        [TestCase(0, 98, 0)]
        [TestCase(0, 0, 1)]
        [TestCase(0, 0, 10)]
        [TestCase(0, 0, 54)]
        [TestCase(33, 5, 8)]
        [TestCase(44, 2, 9)]
        [TestCase(200, 200, 200)]
        [TestCase(1000, 1000, 1000)]
        public void AnimationStreamSizeMatchRigDefinitionSize(int skeletonCount, int floatCount, int intCount)
        {
            var customChannels = new IAnimationChannel[floatCount + intCount];
            for (int i = 0; i < floatCount; i++)
            {
                customChannels[i] = new FloatChannel { Id = $"float{i}", DefaultValue = 0.0f };
            }

            for (int i = 0; i < intCount; i++)
            {
                customChannels[floatCount + i] = new IntChannel { Id = $"int{i}", DefaultValue = 0 };
            }

            var rig = CreateTestRigDefinition(skeletonCount, customChannels);

            var interpolatedCurveCount =  rig.Value.Bindings.CurveCount - rig.Value.Bindings.RotationCurveCount - rig.Value.Bindings.IntCurveCount;
            var rotationCurveCount =  rig.Value.Bindings.RotationCurveCount;
            var discreetCurveCount =  rig.Value.Bindings.IntCurveCount;

            var interpolatedDataChunkCount = (int)math.ceil(interpolatedCurveCount / (float)BindingSet.k_InterpolatedDataChunkSize);
            var DiscreteDataChunkCount = (int)math.ceil(discreetCurveCount / (float)BindingSet.k_DiscreteDataChunkSize);
            var RotationDataChunkCount = (int)math.ceil(rotationCurveCount / (float)BindingSet.k_RotationDataChunkSize);

            var ChannelSize = interpolatedDataChunkCount * BindingSet.k_InterpolatedDataChunkSize + DiscreteDataChunkCount * BindingSet.k_DiscreteDataChunkSize + RotationDataChunkCount * BindingSet.k_RotationDataChunkSize;

            const int bitsInBytes = 8;
            const int bitsIn8Bytes = 8 * bitsInBytes;
            const int bitsIn4Bytes = 4 * bitsInBytes;
            var maskSizeIn4Bytes = Core.AlignUp(rig.Value.Bindings.BindingCount, bitsIn8Bytes) / bitsIn4Bytes;
            var expectedStreamSize = ChannelSize + (maskSizeIn4Bytes * 2);


            Assert.That(rig.Value.Bindings.StreamSize, Is.EqualTo(expectedStreamSize), "Stream size doesn't match expected value");
        }
    }
}
