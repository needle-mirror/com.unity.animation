using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using UnityEngine.TestTools;

namespace Unity.Animation.Tests
{
    public class AnimationStreamTests : AnimationTestsFixture, IPrebuildSetup
    {
        // Very simple decomposition, not very precise but good enough with relatively small scales (< 2f)
        // should not be used elsewhere
        static void Decompose(AffineTransform m, out float3 translation, out quaternion rotation, out float3 scale)
        {
            translation = m.t;

            // Consider signed scale
            scale = new float3(math.length(m.rs.c0), math.length(m.rs.c1), math.length(m.rs.c2));
            if (math.determinant(m) < 0f)
                scale = -scale;

            float3x3 rMat = m.rs;
            if (math.all(scale))
            {
                rMat.c0 /= scale.x;
                rMat.c1 /= scale.y;
                rMat.c2 /= scale.z;
                rotation = new quaternion(rMat);
            }
            else
                rotation = quaternion.identity;
        }

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
            SetupRigEntity(entity, rig, Entity.Null);

            var clipNode = CreateNode<ClipNode>();
            var entityNode = CreateComponentNode(entity);

            Set.Connect(clipNode, ClipNode.KernelPorts.Output, entityNode);

            Set.SendMessage(clipNode, ClipNode.SimulationPorts.Rig, rig);
            Set.SendMessage(clipNode, ClipNode.SimulationPorts.Clip, clip);
            Set.SetData(clipNode, ClipNode.KernelPorts.Time, time);

            var data = new TestData { Entity = entity, Buffer = CreateGraphValue(clipNode, ClipNode.KernelPorts.Output) };

            m_AnimationGraphSystem.Update();

            return data;
        }

        [Test]
        public void CanReadLocalToParentValues()
        {
            var data = CreateAndEvaluateSimpleClipGraph(0.0f, m_Rig, m_ConstantHierarchyClip);

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            Assert.IsFalse(streamECS.IsNull);
            Assert.That(streamECS.GetLocalToParentTranslation(k_RootId), Is.EqualTo(m_ClipRootLocalTranslation).Using(TranslationComparer), "LocalToParentTranslation doesn't match for root using ECS buffer");
            Assert.That(streamECS.GetLocalToParentRotation(k_RootId), Is.EqualTo(m_ClipRootLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(k_RootId), Is.EqualTo(m_ClipRootLocalScale).Using(ScaleComparer));
            Assert.That(streamECS.GetLocalToParentTranslation(k_Child1Id), Is.EqualTo(m_ClipChildLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(k_Child1Id), Is.EqualTo(m_ClipChildLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(k_Child1Id), Is.EqualTo(m_ClipChildLocalScale).Using(ScaleComparer));

            var readWriteBuffer = DFGUtils.GetGraphValueTempNativeBuffer(Set, data.Buffer);
            var graphStream = AnimationStream.CreateReadOnly(m_Rig, readWriteBuffer);

            Assert.IsFalse(graphStream.IsNull);
            Assert.That(graphStream.GetLocalToParentTranslation(k_RootId), Is.EqualTo(m_ClipRootLocalTranslation).Using(TranslationComparer), "LocalToParentTranslation doesn't match for root using GraphValue");
            Assert.That(graphStream.GetLocalToParentRotation(k_RootId), Is.EqualTo(m_ClipRootLocalRotation).Using(RotationComparer));
            Assert.That(graphStream.GetLocalToParentScale(k_RootId), Is.EqualTo(m_ClipRootLocalScale).Using(ScaleComparer));
            Assert.That(graphStream.GetLocalToParentTranslation(k_Child1Id), Is.EqualTo(m_ClipChildLocalTranslation).Using(TranslationComparer));
            Assert.That(graphStream.GetLocalToParentRotation(k_Child1Id), Is.EqualTo(m_ClipChildLocalRotation).Using(RotationComparer));
            Assert.That(graphStream.GetLocalToParentScale(k_Child1Id), Is.EqualTo(m_ClipChildLocalScale).Using(ScaleComparer));

            readWriteBuffer.Dispose();
        }

        [Test]
        public void CanWriteLocalToParentValues()
        {
            var data = CreateAndEvaluateSimpleClipGraph(0.0f, m_Rig, m_ConstantHierarchyClip);

            float3 newRootLocalTranslation = new float3(30f, 10f, 2f);
            quaternion newRootLocalRotation = quaternion.RotateY(math.radians(45));
            float3 newRootLocalScale = new float3(1f, 0.5f, 0.5f);

            float3 newChildLocalTranslation = new float3(60f, 1f, 300f);
            quaternion newChildLocalRotation = quaternion.RotateZ(math.radians(20));
            float3 newChildLocalScale = new float3(2f, 2f, 1f);

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            Assert.IsFalse(streamECS.IsNull);
            streamECS.SetLocalToParentTranslation(k_RootId, newRootLocalTranslation);
            streamECS.SetLocalToParentRotation(k_RootId, newRootLocalRotation);
            streamECS.SetLocalToParentScale(k_RootId, newRootLocalScale);
            streamECS.SetLocalToParentTranslation(k_Child1Id, newChildLocalTranslation);
            streamECS.SetLocalToParentRotation(k_Child1Id, newChildLocalRotation);
            streamECS.SetLocalToParentScale(k_Child1Id, newChildLocalScale);
            Assert.That(streamECS.GetLocalToParentTranslation(k_RootId), Is.EqualTo(newRootLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(k_RootId), Is.EqualTo(newRootLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(k_RootId), Is.EqualTo(newRootLocalScale).Using(ScaleComparer));
            Assert.That(streamECS.GetLocalToParentTranslation(k_Child1Id), Is.EqualTo(newChildLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(k_Child1Id), Is.EqualTo(newChildLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(k_Child1Id), Is.EqualTo(newChildLocalScale).Using(ScaleComparer));

            var readWriteBuffer = DFGUtils.GetGraphValueTempNativeBuffer(Set, data.Buffer);

            var graphStream = AnimationStream.Create(m_Rig, readWriteBuffer);
            Assert.IsFalse(graphStream.IsNull);
            graphStream.SetLocalToParentTranslation(k_RootId, newRootLocalTranslation);
            graphStream.SetLocalToParentRotation(k_RootId, newRootLocalRotation);
            graphStream.SetLocalToParentScale(k_RootId, newRootLocalScale);
            graphStream.SetLocalToParentTranslation(k_Child1Id, newChildLocalTranslation);
            graphStream.SetLocalToParentRotation(k_Child1Id, newChildLocalRotation);
            graphStream.SetLocalToParentScale(k_Child1Id, newChildLocalScale);
            Assert.That(graphStream.GetLocalToParentTranslation(k_RootId), Is.EqualTo(newRootLocalTranslation).Using(TranslationComparer));
            Assert.That(graphStream.GetLocalToParentRotation(k_RootId), Is.EqualTo(newRootLocalRotation).Using(RotationComparer));
            Assert.That(graphStream.GetLocalToParentScale(k_RootId), Is.EqualTo(newRootLocalScale).Using(ScaleComparer));
            Assert.That(graphStream.GetLocalToParentTranslation(k_Child1Id), Is.EqualTo(newChildLocalTranslation).Using(TranslationComparer));
            Assert.That(graphStream.GetLocalToParentRotation(k_Child1Id), Is.EqualTo(newChildLocalRotation).Using(RotationComparer));
            Assert.That(graphStream.GetLocalToParentScale(k_Child1Id), Is.EqualTo(newChildLocalScale).Using(ScaleComparer));

            readWriteBuffer.Dispose();
        }

        [Test]
        public void CanReadLocalToRootValues()
        {
            // For this test we must lower the precision because Decompose is not precise enough
            float kTranslationTolerance = 1e-4f;
            float kRotationTolerance = 1e-4f;

            var float3Comparer = new Float3AbsoluteEqualityComparer(kTranslationTolerance);
            var quaternionComparer = new QuaternionAbsoluteEqualityComparer(kRotationTolerance);

            var data = CreateAndEvaluateSimpleClipGraph(0.0f, m_Rig, m_ConstantHierarchyClip);

            AffineTransform rootLocalTx = mathex.AffineTransform(m_ClipRootLocalTranslation, m_ClipRootLocalRotation, m_ClipRootLocalScale);
            AffineTransform childLocalTx = mathex.AffineTransform(m_ClipChildLocalTranslation, m_ClipChildLocalRotation, m_ClipChildLocalScale);
            AffineTransform childTx = mathex.mul(rootLocalTx, childLocalTx);
            Decompose(childTx, out float3 childTRef, out quaternion childRRef, out float3 childSRef);

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            Assert.IsFalse(streamECS.IsNull);
            Assert.That(streamECS.GetLocalToRootTranslation(k_Child1Id), Is.EqualTo(childTRef).Using(float3Comparer));
            Assert.That(streamECS.GetLocalToRootRotation(k_Child1Id), Is.EqualTo(childRRef).Using(quaternionComparer));
            Assert.That(streamECS.GetLocalToRootScale(k_Child1Id), Is.EqualTo(childSRef).Using(float3Comparer));

            streamECS.GetLocalToRootTRS(k_Child1Id, out float3 childT, out quaternion childR, out float3 childS);
            Assert.That(childT, Is.EqualTo(childTRef).Using(float3Comparer));
            Assert.That(childR, Is.EqualTo(childRRef).Using(quaternionComparer));
            Assert.That(childS, Is.EqualTo(childSRef).Using(float3Comparer));

            var readWriteBuffer = DFGUtils.GetGraphValueTempNativeBuffer(Set, data.Buffer);
            var graphStream = AnimationStream.CreateReadOnly(m_Rig, readWriteBuffer);

            Assert.IsFalse(graphStream.IsNull);
            Assert.That(graphStream.GetLocalToRootTranslation(k_Child1Id), Is.EqualTo(childTRef).Using(float3Comparer));
            Assert.That(graphStream.GetLocalToRootRotation(k_Child1Id), Is.EqualTo(childRRef).Using(quaternionComparer));
            Assert.That(graphStream.GetLocalToRootScale(k_Child1Id), Is.EqualTo(childSRef).Using(float3Comparer));

            graphStream.GetLocalToRootTRS(k_Child1Id, out childT, out childR, out childS);
            Assert.That(childT, Is.EqualTo(childTRef).Using(float3Comparer));
            Assert.That(childR, Is.EqualTo(childRRef).Using(quaternionComparer));
            Assert.That(childS, Is.EqualTo(childSRef).Using(float3Comparer));

            readWriteBuffer.Dispose();
        }

        [Test]
        public void CanWriteLocalToRootValues()
        {
            var data = CreateAndEvaluateSimpleClipGraph(0.0f, m_Rig, m_ConstantHierarchyClip);

            float3 newRootTranslation = new float3(5f, 3f, 2f);
            quaternion newRootRotation = quaternion.RotateY(math.radians(45));
            float3 newRootScale = new float3(0.5f, 2f, 1f);

            float3 newChildTranslation = new float3(0f, 1f, 2f);
            quaternion newChildRotation = quaternion.RotateZ(math.radians(20));
            float3 newChildScale = new float3(1f, 0.5f, 2f);

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            Assert.IsFalse(streamECS.IsNull);

            // Set root Tx in root space
            streamECS.SetLocalToRootTRS(k_RootId, newRootTranslation, newRootRotation, newRootScale);

            streamECS.GetLocalToRootTRS(k_RootId, out float3 outTranslation, out quaternion outRotation, out float3 outScale);
            Assert.That(outTranslation, Is.EqualTo(newRootTranslation).Using(TranslationComparer));
            Assert.That(outRotation, Is.EqualTo(newRootRotation).Using(RotationComparer));
            Assert.That(outScale, Is.EqualTo(newRootScale).Using(ScaleComparer));
            Assert.That(streamECS.GetLocalToRootTranslation(k_RootId), Is.EqualTo(newRootTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToRootRotation(k_RootId), Is.EqualTo(newRootRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToRootScale(k_RootId), Is.EqualTo(newRootScale).Using(ScaleComparer));

            // Set child Tx in root space (using individual TRS function calls)
            streamECS.SetLocalToRootTranslation(k_Child1Id, newChildTranslation);
            streamECS.SetLocalToRootRotation(k_Child1Id, newChildRotation);
            streamECS.SetLocalToRootScale(k_Child1Id, newChildScale);

            streamECS.GetLocalToRootTRS(k_Child1Id, out outTranslation, out outRotation, out outScale);
            Assert.That(outTranslation, Is.EqualTo(newChildTranslation).Using(TranslationComparer));
            Assert.That(outRotation, Is.EqualTo(newChildRotation).Using(RotationComparer));
            Assert.That(outScale, Is.EqualTo(newChildScale).Using(ScaleComparer));
            Assert.That(streamECS.GetLocalToRootTranslation(k_Child1Id), Is.EqualTo(newChildTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToRootRotation(k_Child1Id), Is.EqualTo(newChildRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToRootScale(k_Child1Id), Is.EqualTo(newChildScale).Using(ScaleComparer));

            var readWriteBuffer = DFGUtils.GetGraphValueTempNativeBuffer(Set, data.Buffer);
            var graphStream = AnimationStream.Create(m_Rig, readWriteBuffer);
            Assert.IsFalse(graphStream.IsNull);

            // Set child Tx in root space
            graphStream.SetLocalToRootTRS(k_Child1Id, newChildTranslation, newChildRotation, newChildScale);

            graphStream.GetLocalToRootTRS(k_Child1Id, out outTranslation, out outRotation, out outScale);
            Assert.That(outTranslation, Is.EqualTo(newChildTranslation).Using(TranslationComparer));
            Assert.That(outRotation, Is.EqualTo(newChildRotation).Using(RotationComparer));
            Assert.That(outScale, Is.EqualTo(newChildScale).Using(ScaleComparer));
            Assert.That(graphStream.GetLocalToRootTranslation(k_Child1Id), Is.EqualTo(newChildTranslation).Using(TranslationComparer));
            Assert.That(graphStream.GetLocalToRootRotation(k_Child1Id), Is.EqualTo(newChildRotation).Using(RotationComparer));
            Assert.That(graphStream.GetLocalToRootScale(k_Child1Id), Is.EqualTo(newChildScale).Using(ScaleComparer));

            // Set root Tx in root space (using individual TRS function calls)
            graphStream.SetLocalToRootTranslation(k_RootId, newRootTranslation);
            graphStream.SetLocalToRootRotation(k_RootId, newRootRotation);
            graphStream.SetLocalToRootScale(k_RootId, newRootScale);

            graphStream.GetLocalToRootTRS(k_RootId, out outTranslation, out outRotation, out outScale);
            Assert.That(outTranslation, Is.EqualTo(newRootTranslation).Using(TranslationComparer));
            Assert.That(outRotation, Is.EqualTo(newRootRotation).Using(RotationComparer));
            Assert.That(outScale, Is.EqualTo(newRootScale).Using(ScaleComparer));
            Assert.That(graphStream.GetLocalToRootTranslation(k_RootId), Is.EqualTo(newRootTranslation).Using(TranslationComparer));
            Assert.That(graphStream.GetLocalToRootRotation(k_RootId), Is.EqualTo(newRootRotation).Using(RotationComparer));
            Assert.That(graphStream.GetLocalToRootScale(k_RootId), Is.EqualTo(newRootScale).Using(ScaleComparer));

            readWriteBuffer.Dispose();
        }

        [Test]
        public void CanReadWriteFloats()
        {
            var data = CreateAndEvaluateSimpleClipGraph(0.0f, m_Rig, m_ConstantHierarchyClip);

            const float k_NewFloat = 10f;

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            Assert.IsFalse(streamECS.IsNull);
            Assert.That(streamECS.GetFloat(k_FloatId), Is.EqualTo(k_DefaultFloatValue));
            streamECS.SetFloat(k_FloatId, k_NewFloat);
            Assert.That(streamECS.GetFloat(k_FloatId), Is.EqualTo(k_NewFloat));

            var readWriteBuffer = DFGUtils.GetGraphValueTempNativeBuffer(Set, data.Buffer);

            var graphStream = AnimationStream.Create(m_Rig, readWriteBuffer);
            Assert.IsFalse(graphStream.IsNull);

            Assert.IsFalse(graphStream.IsNull);
            Assert.That(graphStream.GetFloat(k_FloatId), Is.EqualTo(k_DefaultFloatValue));
            graphStream.SetFloat(k_FloatId, k_NewFloat);
            Assert.That(graphStream.GetFloat(k_FloatId), Is.EqualTo(k_NewFloat));

            readWriteBuffer.Dispose();
        }

        [Test]
        public void CanReadWriteInts()
        {
            var data = CreateAndEvaluateSimpleClipGraph(0.0f, m_Rig, m_ConstantHierarchyClip);

            const int k_NewInt = 20;

            var streamECS = AnimationStream.Create(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            Assert.IsFalse(streamECS.IsNull);
            Assert.That(streamECS.GetInt(k_IntId), Is.EqualTo(k_DefaultIntValue));
            streamECS.SetInt(k_IntId, k_NewInt);
            Assert.That(streamECS.GetInt(k_IntId), Is.EqualTo(k_NewInt));

            var readWriteBuffer = DFGUtils.GetGraphValueTempNativeBuffer(Set, data.Buffer);

            var graphStream = AnimationStream.Create(m_Rig, readWriteBuffer);

            Assert.IsFalse(graphStream.IsNull);
            Assert.That(graphStream.GetInt(k_IntId), Is.EqualTo(k_DefaultIntValue));
            graphStream.SetInt(k_IntId, k_NewInt);
            Assert.That(graphStream.GetInt(k_IntId), Is.EqualTo(k_NewInt));

            readWriteBuffer.Dispose();
        }

        [Test]
        public void CanComputeLocalToParentInverseMatrix()
        {
            var data = CreateAndEvaluateSimpleClipGraph(0.0f, m_Rig, m_ConstantHierarchyClip);

            var stream = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            var mat0 = stream.GetLocalToParentMatrix(0);
            var invMat0 = stream.GetLocalToParentInverseMatrix(0);
            var mat1 = stream.GetLocalToParentMatrix(1);
            var invMat1 = stream.GetLocalToParentInverseMatrix(1);

            var identity0 = mathex.mul(mat0, invMat0);
            Assert.That(identity0.rs.c0.x, Is.EqualTo(1f).Using(FloatComparer));
            Assert.That(identity0.rs.c0.y, Is.EqualTo(0f).Using(FloatComparer));
            Assert.That(identity0.rs.c0.z, Is.EqualTo(0f).Using(FloatComparer));
            Assert.That(identity0.rs.c1.x, Is.EqualTo(0f).Using(FloatComparer));
            Assert.That(identity0.rs.c1.y, Is.EqualTo(1f).Using(FloatComparer));
            Assert.That(identity0.rs.c1.z, Is.EqualTo(0f).Using(FloatComparer));
            Assert.That(identity0.rs.c2.x, Is.EqualTo(0f).Using(FloatComparer));
            Assert.That(identity0.rs.c2.y, Is.EqualTo(0f).Using(FloatComparer));
            Assert.That(identity0.rs.c2.z, Is.EqualTo(1f).Using(FloatComparer));

            Assert.That(identity0.t.x, Is.EqualTo(0f).Using(FloatComparer));
            Assert.That(identity0.t.y, Is.EqualTo(0f).Using(FloatComparer));
            Assert.That(identity0.t.z, Is.EqualTo(0f).Using(FloatComparer));

            var identity1 = mathex.mul(mat1, invMat1);
            Assert.That(identity1.rs.c0.x, Is.EqualTo(1f).Using(FloatComparer));
            Assert.That(identity1.rs.c0.y, Is.EqualTo(0f).Using(FloatComparer));
            Assert.That(identity1.rs.c0.z, Is.EqualTo(0f).Using(FloatComparer));
            Assert.That(identity1.rs.c1.x, Is.EqualTo(0f).Using(FloatComparer));
            Assert.That(identity1.rs.c1.y, Is.EqualTo(1f).Using(FloatComparer));
            Assert.That(identity1.rs.c1.z, Is.EqualTo(0f).Using(FloatComparer));
            Assert.That(identity1.rs.c2.x, Is.EqualTo(0f).Using(FloatComparer));
            Assert.That(identity1.rs.c2.y, Is.EqualTo(0f).Using(FloatComparer));
            Assert.That(identity1.rs.c2.z, Is.EqualTo(1f).Using(FloatComparer));

            Assert.That(identity1.t.x, Is.EqualTo(0f).Using(FloatComparer));
            Assert.That(identity1.t.y, Is.EqualTo(0f).Using(FloatComparer));
            Assert.That(identity1.t.z, Is.EqualTo(0f).Using(FloatComparer));
        }

        [Test]
        public void CanComputeLocalToRootInverseMatrix()
        {
            var data = CreateAndEvaluateSimpleClipGraph(0.0f, m_Rig, m_ConstantHierarchyClip);

            var stream = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(data.Entity).AsNativeArray()
            );

            // Need to lower tolerance a bit
            const float tolerance = 1e-4f;
            var floatComparer = new FloatAbsoluteEqualityComparer(tolerance);

            var mat0 = stream.GetLocalToRootMatrix(0);
            var invMat0 = stream.GetLocalToRootInverseMatrix(0);
            var mat1 = stream.GetLocalToRootMatrix(1);
            var invMat1 = stream.GetLocalToRootInverseMatrix(1);

            var identity0 = mathex.mul(mat0, invMat0);
            Assert.That(identity0.rs.c0.x, Is.EqualTo(1f).Using(floatComparer));
            Assert.That(identity0.rs.c0.y, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity0.rs.c0.z, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity0.rs.c1.x, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity0.rs.c1.y, Is.EqualTo(1f).Using(floatComparer));
            Assert.That(identity0.rs.c1.z, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity0.rs.c2.x, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity0.rs.c2.y, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity0.rs.c2.z, Is.EqualTo(1f).Using(floatComparer));

            Assert.That(identity0.t.x, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity0.t.y, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity0.t.z, Is.EqualTo(0f).Using(floatComparer));

            var identity1 = mathex.mul(mat1, invMat1);
            Assert.That(identity1.rs.c0.x, Is.EqualTo(1f).Using(floatComparer));
            Assert.That(identity1.rs.c0.y, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity1.rs.c0.z, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity1.rs.c1.x, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity1.rs.c1.y, Is.EqualTo(1f).Using(floatComparer));
            Assert.That(identity1.rs.c1.z, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity1.rs.c2.x, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity1.rs.c2.y, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity1.rs.c2.z, Is.EqualTo(1f).Using(floatComparer));

            Assert.That(identity1.t.x, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity1.t.y, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity1.t.z, Is.EqualTo(0f).Using(floatComparer));
        }
    }
}
