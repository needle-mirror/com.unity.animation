using System;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.TestTools;
using Unity.Transforms;
using Unity.DataFlowGraph;

namespace Unity.Animation.Tests
{
    public class ChannelMaskTests : AnimationTestsFixture, IPrebuildSetup
    {
        Rig m_Rig;

        BlobAssetReference<RigDefinition> CreateRigDefinition(int boneCount)
        {
            var skeleton = new SkeletonNode[boneCount];

            skeleton[0] = new SkeletonNode { Id = "Root", ParentIndex = -1, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1) };
            for (int i = 1; i < boneCount; i++)
            {
                skeleton[i] = new SkeletonNode { Id = $"{i}", ParentIndex = i - 1, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1) };
            }
            ;

            return RigBuilder.CreateRigDefinition(skeleton);
        }

        List<Entity> CreateRigEntityTransforms(BlobAssetReference<RigDefinition> rig)
        {
            var list = new List<Entity>(rig.Value.Skeleton.BoneCount);

            for (int i = 0; i < rig.Value.Skeleton.BoneCount; i++)
            {
                list.Add(m_Manager.CreateEntity());

                m_Manager.AddComponentData(list[i], new LocalToWorld { Value = float4x4.identity });
                m_Manager.AddComponentData(list[i], new Translation { Value = new float3(i) });
                m_Manager.AddComponentData(list[i], new Rotation { Value = quaternion.identity });
                m_Manager.AddComponentData(list[i], new NonUniformScale { Value = new float3(1) });

                if (rig.Value.Skeleton.ParentIndexes[i] != -1)
                {
                    var parent = list[rig.Value.Skeleton.ParentIndexes[i]];
                    m_Manager.AddComponentData(list[i], new Parent { Value = parent });
                    m_Manager.AddComponentData(list[i], new LocalToParent());
                }
            }
            return list;
        }

        public void Setup()
        {
#if UNITY_EDITOR
            PlaymodeTestsEditorSetup.CreateStreamingAssetsDirectory();
#endif
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // Create rig
            m_Rig = new Rig { Value = CreateRigDefinition(50) };
        }

        NodeHandle<ClipNode> CreateGraph(Entity entity, in Rig rig, NodeSet set, BlobAssetReference<Clip> clip)
        {
            var clipNode = CreateNode<ClipNode>(set);
            var entityNode = CreateComponentNode(entity, set);

            set.Connect(clipNode, ClipNode.KernelPorts.Output, entityNode);
            set.SendMessage(clipNode, ClipNode.SimulationPorts.Rig, rig);
            set.SendMessage(clipNode, ClipNode.SimulationPorts.Clip, clip);
            set.SetData(clipNode, ClipNode.KernelPorts.Time, 0f);

            return clipNode;
        }

        [Test]
        public void ChannelPassMask_ShouldBeClearedAtEachNewEmptyPhase_ForRigWithoutRootManagement()
        {
            var entityTransforms = CreateRigEntityTransforms(m_Rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, m_Rig, Entity.Null);

            var stream = AnimationStream.Create(m_Rig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            stream.PassMask.Set(true);
            Assert.IsTrue(stream.PassMask.HasAll(), "Mask should be set to true for all channels after explicit set.");

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            stream = AnimationStream.Create(m_Rig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            Assert.IsTrue(stream.PassMask.HasNone(), "Mask should be set to false for all channels after ProcessDefaultAnimationGraph.");

            stream.SetMasks(true);
            Assert.IsTrue(stream.PassMask.HasAll(), "Mask should be set to true for all channels after explicit set.");

            m_PostAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            stream = AnimationStream.CreateReadOnly(m_Rig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            Assert.IsTrue(stream.PassMask.HasNone(), "Mask should be set to false for all channels after ProcessLateAnimationGraph.");
        }

        [Test]
        public void ChannelFrameMask_ShouldntBeClearedAtEachNewEmptyPhase_ForRigWithoutRootManagement()
        {
            var entityTransforms = CreateRigEntityTransforms(m_Rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, m_Rig, Entity.Null);

            var stream = AnimationStream.Create(m_Rig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            stream.FrameMask.Set(true);
            Assert.IsTrue(stream.FrameMask.HasAll(), "Mask should be set to true for all channels after explicit set.");

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            stream = AnimationStream.Create(m_Rig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            Assert.IsTrue(stream.FrameMask.HasAll(), "Mask should be set to true for all channels after ProcessDefaultAnimationGraph.");

            stream.FrameMask.Set(true);
            Assert.IsTrue(stream.FrameMask.HasAll(), "Mask should be set to true for all channels after explicit set.");

            m_PostAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            stream = AnimationStream.CreateReadOnly(m_Rig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            Assert.IsTrue(stream.FrameMask.HasAll(), "Mask should be set to true for all channels after ProcessLateAnimationGraph.");
        }

        [Test]
        public void ChannelPassMask_ShouldBeClearedExceptForRootAtEachNewEmptyPhase_ForRigWithRootManagement()
        {
            var entityTransforms = CreateRigEntityTransforms(m_Rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, m_Rig, entityTransforms[0]);

            var stream = AnimationStream.Create(m_Rig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            stream.PassMask.Set(true);
            Assert.IsTrue(stream.PassMask.HasAll(), "Mask should be set to true for all channels after explicit set.");

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            stream = AnimationStream.Create(m_Rig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            Assert.That(stream.PassMask.CountChannels(), Is.EqualTo(3), "Mask should be set to false for all channels after ProcessDefaultAnimationGraph except for the root.");
            Assert.That(stream.PassMask.IsTranslationSet(0), Is.True, "Mask should be set to true for root after ProcessDefaultAnimationGraph.");
            Assert.That(stream.PassMask.IsRotationSet(0), Is.True, "Mask should be set to true for root after ProcessDefaultAnimationGraph.");
            Assert.That(stream.PassMask.IsScaleSet(0), Is.True, "Mask should be set to true for root after ProcessDefaultAnimationGraph.");

            stream.PassMask.Set(true);
            Assert.IsTrue(stream.PassMask.HasAll(), "Mask should be set to true for all channels after explicit set.");

            m_PostAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            stream = AnimationStream.CreateReadOnly(m_Rig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            Assert.That(stream.PassMask.CountChannels(), Is.EqualTo(3), "Mask should be set to false for all channels after ProcessLateAnimationGraph except for the root.");
            Assert.That(stream.PassMask.IsTranslationSet(0), Is.True, "Mask should be set to true for root after ProcessLateAnimationGraph.");
            Assert.That(stream.PassMask.IsRotationSet(0), Is.True, "Mask should be set to true for root after ProcessLateAnimationGraph.");
            Assert.That(stream.PassMask.IsScaleSet(0), Is.True, "Mask should be set to true for root after ProcessLateAnimationGraph.");
        }

        [Test]
        public void ChannelFrameAndPassMask_ShouldBeClearedAtEachNewFrame()
        {
            var entityTransforms = CreateRigEntityTransforms(m_Rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, m_Rig, Entity.Null);

            var stream = AnimationStream.Create(m_Rig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            stream.FrameMask.Set(true);
            stream.PassMask.Set(true);
            Assert.IsTrue(stream.FrameMask.HasAll(), "Frame Mask should be set to true for all channels after explicit set.");
            Assert.IsTrue(stream.PassMask.HasAll(), "Pass Mask should be set to true for all channels after explicit set.");

            m_InitializeAnimation.Update();
            m_Manager.CompleteAllJobs();

            stream = AnimationStream.Create(m_Rig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            Assert.IsTrue(stream.FrameMask.HasNone(), "Frame Mask should be set to false for all channels after InitializeAnimation.");
            Assert.IsTrue(stream.PassMask.HasNone(), "Pass Mask should be set to false for all channels after InitializeAnimation.");

            stream.FrameMask.Set(true);
            Assert.IsTrue(stream.FrameMask.HasAll(), "Mask should be set to true for all channels after explicit set.");

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            stream = AnimationStream.CreateReadOnly(m_Rig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            Assert.IsTrue(stream.FrameMask.HasAll(), "Mask should be set to true for all channels after ProcessDefaultAnimationGraph.");

            m_PostAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            stream = AnimationStream.CreateReadOnly(m_Rig, m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray());
            Assert.IsTrue(stream.FrameMask.HasAll(), "Mask should be set to true for all channels after ProcessLateAnimationGraph.");
        }
    }
}
