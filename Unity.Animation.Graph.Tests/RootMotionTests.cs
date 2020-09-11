using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.TestTools;
using Unity.DataFlowGraph;
using Unity.Transforms;

namespace Unity.Animation.Tests
{
    class RootMotionTests : AnimationTestsFixture, IPrebuildSetup
    {
        private static float3 m_StartTranslation => new float3(0, 0, 0);
        private static float3 m_StopTranslation => new float3(1, 1, 0);

        static quaternion  m_StartRotation => new quaternion(0, 0, 0, 1);
        static quaternion  m_StopRotation => new quaternion(0, 1, 1, 1);

        private Rig m_Rig;
        private BlobAssetReference<Clip> m_Clip;
        private static BlobAssetReference<RigDefinition> CreateTestRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode { ParentIndex = -1, Id = "Root", AxisIndex = -1,  LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1, 1, 1)},
                new SkeletonNode { ParentIndex = 0, Id = "Between", AxisIndex = -1 ,  LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1, 1, 1)},
                new SkeletonNode { ParentIndex = 1, Id = "Motion", AxisIndex = -1,  LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1, 1, 1)}
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes);
        }

        public void Setup()
        {
#if UNITY_EDITOR
            PlaymodeTestsEditorSetup.CreateStreamingAssetsDirectory();

            {
                var motionClip = CreateLinearDenseClip
                    (
                    new[]
                    {
                        new LinearBinding<float3> { Path = "Motion", ValueStart = m_StartTranslation, ValueEnd = m_StopTranslation },
                    },
                    new[]
                    {
                        new LinearBinding<quaternion> { Path = "Motion", ValueStart = math.normalize(m_StartRotation), ValueEnd = math.normalize(m_StopRotation) },
                    },
                    new[]
                    {
                        new LinearBinding<float3> { Path = "Motion", ValueStart = new float3(1, 1, 1), ValueEnd = new float3(1, 1, 1) }
                    }
                    );

                var blobPath = "MotionClip.blob";
                BlobFile.WriteBlobAsset(ref motionClip, blobPath);
            }
#endif
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // Create rig
            m_Rig = new Rig { Value = CreateTestRigDefinition() };


            var path = "MotionClip.blob";
            m_Clip = BlobFile.ReadBlobAsset<Clip>(path);

            ClipManager.Instance.GetClipFor(m_Rig, m_Clip);
        }

        [Test]
        public void CanApplyInPlaceNode()
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, Entity.Null);

            var clipNode = CreateNode<ClipNode>();
            Set.SendMessage(clipNode, ClipNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(clipNode, ClipNode.SimulationPorts.Clip, m_Clip);
            Set.SetData(clipNode, ClipNode.KernelPorts.Time, 1.0f);

            var inPlaceNode = CreateNode<InPlaceMotionNode>();

            Set.SendMessage(inPlaceNode, InPlaceMotionNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(inPlaceNode, InPlaceMotionNode.SimulationPorts.Configuration, new ClipConfiguration { MotionID = new StringHash("Motion") });

            Set.Connect(clipNode, ClipNode.KernelPorts.Output, inPlaceNode, InPlaceMotionNode.KernelPorts.Input);

            var entityNode = CreateComponentNode(entity);

            Set.Connect(inPlaceNode, InPlaceMotionNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var translation = m_StopTranslation;
            var rotation = m_StopRotation;
            var projRotation = math.normalize(new quaternion(0, rotation.value.y / rotation.value.w, 0, 1));

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            // validate projection of motion on root
            var rootTranslation = streamECS.GetLocalToParentTranslation(0);
            Assert.That(rootTranslation, Is.EqualTo(new float3(translation.x, 0, translation.z)).Using(TranslationComparer));
            var rootRotation = streamECS.GetLocalToParentRotation(0);
            Assert.That(rootRotation, Is.EqualTo(projRotation).Using(RotationComparer));

            // validate that motion is now in place
            var motionTranslation = streamECS.GetLocalToParentTranslation(2);
            Assert.That(motionTranslation, Is.EqualTo(new float3(0, translation.y, 0)).Using(TranslationComparer));
            var motionRotation = streamECS.GetLocalToParentRotation(2);
            Assert.That(motionRotation, Is.EqualTo(mathex.mul(math.conjugate(projRotation), rotation)).Using(RotationComparer));
        }

        [TestCase(0)]
        [TestCase(0.5f)]
        [TestCase(1)]
        public void CanEvaluateInPlaceClipNode(float time)
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, Entity.Null);

            var clipNode = CreateNode<ConfigurableClipNode>();
            Set.SendMessage(clipNode, ConfigurableClipNode.SimulationPorts.Configuration, new ClipConfiguration { MotionID = new StringHash("Motion") });
            Set.SendMessage(clipNode, ConfigurableClipNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(clipNode, ConfigurableClipNode.SimulationPorts.Clip, m_Clip);
            Set.SetData(clipNode, ConfigurableClipNode.KernelPorts.Time, time);

            var entityNode = CreateComponentNode(entity);
            Set.Connect(clipNode, ConfigurableClipNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var translation = m_StopTranslation * time + m_StartTranslation * (1f - time);
            var rotation = math.normalize(new quaternion(math.normalize(m_StopRotation).value * time + math.normalize(m_StartRotation).value * (1f - time)));
            var projRotation = math.normalize(new quaternion(0, rotation.value.y / rotation.value.w, 0, 1));

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            // validate projection of motion on root
            var rootTranslation = streamECS.GetLocalToParentTranslation(0);
            Assert.That(rootTranslation, Is.EqualTo(new float3(translation.x, 0, translation.z)).Using(TranslationComparer));
            var rootRotation = streamECS.GetLocalToParentRotation(0);
            Assert.That(rootRotation, Is.EqualTo(projRotation).Using(RotationComparer));

            // validate that motion is now in place
            var motionTranslation = streamECS.GetLocalToParentTranslation(2);
            Assert.That(motionTranslation, Is.EqualTo(new float3(0, translation.y, 0)).Using(TranslationComparer));
            var motionRotation = streamECS.GetLocalToParentRotation(2);
            Assert.That(motionRotation, Is.EqualTo(mathex.mul(math.conjugate(projRotation), rotation)).Using(RotationComparer));
        }

        [TestCase(-10)]
        [TestCase(-2.3f)]
        [TestCase(0)]
        [TestCase(0.5f)]
        [TestCase(1)]
        [TestCase(1.5f)]
        [TestCase(6)]
        [TestCase(15.6f)]
        public void CanEvaluateCycleRootClipNode(float time)
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, Entity.Null);

            var clipNode = CreateNode<ConfigurableClipNode>();
            Set.SendMessage(clipNode, ConfigurableClipNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.CycleRootMotion, MotionID = new StringHash("Motion") });
            Set.SendMessage(clipNode, ConfigurableClipNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(clipNode, ConfigurableClipNode.SimulationPorts.Clip, m_Clip);
            Set.SetData(clipNode, ConfigurableClipNode.KernelPorts.Time, time);

            var entityNode = CreateComponentNode(entity);
            Set.Connect(clipNode, ConfigurableClipNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();

            var normalizedTime = time;
            var normalizedTimeInt = (int)normalizedTime;
            var cycle = math.select(normalizedTimeInt, normalizedTimeInt - 1, normalizedTime < 0);
            normalizedTime = math.select(normalizedTime - normalizedTimeInt, normalizedTime - normalizedTimeInt + 1, normalizedTime < 0);

            // start and stop root transform
            var startTProj = new float3(m_StartTranslation.x, 0, m_StartTranslation.z);
            var startRProj =  math.normalize(new quaternion(0, m_StartRotation.value.y / m_StartRotation.value.w, 0, 1));

            var stopTProj = new float3(m_StopTranslation.x, 0, m_StopTranslation.z);
            var stopRProj =  math.normalize(new quaternion(0, m_StopRotation.value.y / m_StopRotation.value.w, 0, 1));

            // current root transform
            var translation = m_StopTranslation * normalizedTime + m_StartTranslation * (1f - normalizedTime);
            var rotation = math.normalize(new quaternion(math.normalize(m_StopRotation).value * normalizedTime + math.normalize(m_StartRotation).value * (1f - normalizedTime)));

            var tProj = new float3(translation.x, 0, translation.z);
            var rProj =  math.normalize(new quaternion(0, rotation.value.y / rotation.value.w, 0, 1));

            // cycled root transform
            var startX = cycle >= 0 ? new RigidTransform(startRProj, startTProj) : new RigidTransform(stopRProj, stopTProj);
            var stopX = cycle >= 0 ? new RigidTransform(stopRProj, stopTProj) : new RigidTransform(startRProj, startTProj);

            var x = new RigidTransform(rProj, tProj);
            RigidTransform cycleX = mathex.rigidPow(math.mul(stopX, math.inverse(startX)), math.asuint(math.abs(cycle)));
            x = math.mul(cycleX, x);


            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            // validate
            var rootTranslation = streamECS.GetLocalToParentTranslation(0);
            Assert.That(rootTranslation, Is.EqualTo(x.pos));
            var rootRotation = streamECS.GetLocalToParentRotation(0);
            Assert.That(rootRotation, Is.EqualTo(x.rot).Using(RotationComparer));
        }

        [TestCase(-2.3f)]
        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(1.5f)]
        [TestCase(3f)]
        [TestCase(6f)]
        [TestCase(20f)]
        public void AnimatedRootMotion_Updates_RootTransformComponents(float time)
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, entity);

            m_Manager.AddComponent<PreAnimationGraphSystem.AnimatedRootMotion>(entity);

            var entityTx = new RigidTransform(quaternion.AxisAngle(math.float3(0f, 1f, 0f), math.radians(40f)), math.float3(2f, 0f, 5f));
            m_Manager.AddComponentData(entity, new Translation { Value = entityTx.pos });
            m_Manager.AddComponentData(entity, new Rotation { Value = entityTx.rot });

            var clipNode = CreateNode<ConfigurableClipNode>();
            Set.SendMessage(clipNode, ConfigurableClipNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.CycleRootMotion, MotionID = "Motion" });
            Set.SendMessage(clipNode, ConfigurableClipNode.SimulationPorts.Rig, m_Rig);
            Set.SendMessage(clipNode, ConfigurableClipNode.SimulationPorts.Clip, m_Clip);
            Set.SetData(clipNode, ConfigurableClipNode.KernelPorts.Time, time);

            var entityNode = CreateComponentNode(entity);
            Set.Connect(clipNode, ConfigurableClipNode.KernelPorts.Output, entityNode);

            m_AnimationGraphSystem.Update();
            m_Manager.CompleteAllJobs();

            var normalizedTime = time;
            var normalizedTimeInt = (int)normalizedTime;
            var cycle = math.select(normalizedTimeInt, normalizedTimeInt - 1, normalizedTime < 0);
            normalizedTime = math.select(normalizedTime - normalizedTimeInt, normalizedTime - normalizedTimeInt + 1, normalizedTime < 0);

            // start and stop root transform
            var startTProj = new float3(m_StartTranslation.x, 0, m_StartTranslation.z);
            var startRProj =  math.normalize(new quaternion(0, m_StartRotation.value.y / m_StartRotation.value.w, 0, 1));

            var stopTProj = new float3(m_StopTranslation.x, 0, m_StopTranslation.z);
            var stopRProj =  math.normalize(new quaternion(0, m_StopRotation.value.y / m_StopRotation.value.w, 0, 1));

            // current root transform
            var translation = m_StopTranslation * normalizedTime + m_StartTranslation * (1f - normalizedTime);
            var rotation = math.normalize(new quaternion(math.normalize(m_StopRotation).value * normalizedTime + math.normalize(m_StartRotation).value * (1f - normalizedTime)));

            var tProj = new float3(translation.x, 0, translation.z);
            var rProj =  math.normalize(new quaternion(0, rotation.value.y / rotation.value.w, 0, 1));

            // cycled root transform
            var startX = cycle >= 0 ? new RigidTransform(startRProj, startTProj) : new RigidTransform(stopRProj, stopTProj);
            var stopX = cycle >= 0 ? new RigidTransform(stopRProj, stopTProj) : new RigidTransform(startRProj, startTProj);

            var x = new RigidTransform(rProj, tProj);
            RigidTransform cycleX = mathex.rigidPow(math.mul(stopX, math.inverse(startX)), math.asuint(math.abs(cycle)));
            x = math.mul(cycleX, x);

            // Validate that root motion values are now on the entity transform components
            var newEntityTx = math.mul(entityTx, x);
            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(newEntityTx.pos).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(newEntityTx.rot).Using(RotationComparer));

            // Validate that the values in AnimationRootMotion component have been updated
            var animatedRM = m_Manager.GetComponentData<PreAnimationGraphSystem.AnimatedRootMotion>(entity);
            Assert.That(animatedRM.Delta.pos, Is.EqualTo(x.pos).Using(TranslationComparer));
            Assert.That(animatedRM.Delta.rot, Is.EqualTo(x.rot).Using(RotationComparer));

            // Validate that root values have been reset in the animation stream
            var stream = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            Assert.That(stream.GetLocalToParentTranslation(0), Is.EqualTo(float3.zero).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(0), Is.EqualTo(quaternion.identity).Using(RotationComparer));
        }
    }
}
