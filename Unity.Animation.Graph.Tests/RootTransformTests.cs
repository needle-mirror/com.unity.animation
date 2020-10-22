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
    public class RootTransformTests : AnimationTestsFixture, IPrebuildSetup
    {
        static readonly float3 k_PreClipRootLocalTranslation = new float3(100f, 0f, 0f);
        static readonly quaternion k_PreClipRootLocalRotation = quaternion.RotateX(math.radians(90f));
        static readonly float3 k_PreClipRootLocalScale = new float3(3f, 1f, 1f);
        static readonly float3 k_PreClipChildLocalTranslation = new float3(0f, 100f, 0f);
        static readonly quaternion k_PreClipChildLocalRotation = quaternion.RotateY(math.radians(90f));
        static readonly float3 k_PreClipChildLocalScale = new float3(1f, 1f, 4f);

        static readonly float3 k_PostClipRootLocalTranslation = new float3(0f, 0f, 100f);
        static readonly quaternion k_PostClipRootLocalRotation = quaternion.RotateY(math.radians(90f));
        static readonly float3 k_PostClipRootLocalScale = new float3(1f, 1f, 5f);
        static readonly float3 k_PostClipChildLocalTranslation = new float3(0f, 50f, 0f);
        static readonly quaternion k_PostClipChildLocalRotation = quaternion.RotateZ(math.radians(90f));
        static readonly float3 k_PostClipChildLocalScale = new float3(1f, 2f, 1f);

        Rig m_Rig;

        static readonly string k_PreGraphClipBlobName = "RootTransformTestsPreGraphDenseClip.blob";
        static readonly string k_PostGraphClipBlobName = "RootTransformTestsPostGraphDenseClip.blob";
        BlobAssetReference<Clip> m_PreGraphClip;
        BlobAssetReference<Clip> m_PostGraphClip;

        static readonly string k_MaskTestClipTChannelsClipBlobName = "RootTransformTestsMaskTestClipT.blob";
        static readonly string k_MaskTestClipRChannelsClipBlobName = "RootTransformTestsMaskTestClipR.blob";
        static readonly string k_MaskTestClipTRChannelsClipBlobName = "RootTransformTestsMaskTestClipTR.blob";
        static readonly string k_MaskTestClipTSChannelsClipBlobName = "RootTransformTestsMaskTestClipTS.blob";
        static readonly string k_MaskTestClipRSChannelsClipBlobName = "RootTransformTestsMaskTestClipRS.blob";
        BlobAssetReference<Clip> m_MaskTestClip_TChannels;
        BlobAssetReference<Clip> m_MaskTestClip_RChannels;
        BlobAssetReference<Clip> m_MaskTestClip_TRChannels;
        BlobAssetReference<Clip> m_MaskTestClip_TSChannels;
        BlobAssetReference<Clip> m_MaskTestClip_RSChannels;

        BlobAssetReference<RigDefinition> CreateRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode { Id = "Root", ParentIndex = -1, AxisIndex = -1, LocalTranslationDefaultValue = 1f, LocalRotationDefaultValue = quaternion.RotateX(math.radians(90f)), LocalScaleDefaultValue = 1f },
                new SkeletonNode { Id = "Child", ParentIndex = 0, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = 1f }
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes);
        }

        List<Entity> CreateRigTransforms(BlobAssetReference<RigDefinition> rig, Entity parent)
        {
            var defaultStream = AnimationStream.FromDefaultValues(rig);
            var list = new List<Entity>(rig.Value.Skeleton.BoneCount);

            for (int i = 0; i < rig.Value.Skeleton.BoneCount; ++i)
            {
                list.Add(m_Manager.CreateEntity());
                defaultStream.GetLocalToParentTRS(i, out float3 t, out quaternion r, out float3 s);

                var pIdx = rig.Value.Skeleton.ParentIndexes[i];
                SetupTransformComponents(list[i], t, r, s, pIdx == -1 ? parent : list[pIdx]);
            }

            return list;
        }

        void SetupTransformComponents(Entity entity, float3 t, quaternion r, float3 s, Entity parent)
        {
            m_Manager.AddComponentData(entity, new Translation { Value = t });
            m_Manager.AddComponentData(entity, new Rotation { Value = r });
            m_Manager.AddComponentData(entity, new NonUniformScale { Value = s });

            if (parent == Entity.Null)
            {
                m_Manager.AddComponentData(entity, new LocalToWorld { Value = float4x4.TRS(t, r, s) });
            }
            else
            {
                m_Manager.AddComponentData(entity, new Parent { Value = parent });
                m_Manager.AddComponentData(entity, new LocalToParent { Value = float4x4.TRS(t, r, s) });
                m_Manager.AddComponentData(entity, new LocalToWorld { Value = float4x4.identity });
            }
        }

        public void Setup()
        {
#if UNITY_EDITOR
            PlaymodeTestsEditorSetup.CreateStreamingAssetsDirectory();

            var densePreClip = CreateConstantDenseClip(
                new[] { ("Root", k_PreClipRootLocalTranslation), ("Child", k_PreClipChildLocalTranslation) },
                new[] { ("Root", k_PreClipRootLocalRotation), ("Child", k_PreClipChildLocalRotation) },
                new[] { ("Root", k_PreClipRootLocalScale),    ("Child", k_PreClipChildLocalScale) });

            BlobFile.WriteBlobAsset(ref densePreClip, k_PreGraphClipBlobName);

            var densePostClip = CreateConstantDenseClip(
                new[] { ("Root", k_PostClipRootLocalTranslation), ("Child", k_PostClipChildLocalTranslation) },
                new[] { ("Root", k_PostClipRootLocalRotation), ("Child", k_PostClipChildLocalRotation) },
                new[] { ("Root", k_PostClipRootLocalScale),    ("Child", k_PostClipChildLocalScale) });

            BlobFile.WriteBlobAsset(ref densePostClip, k_PostGraphClipBlobName);

            var denseMaskClipTChannels = CreateConstantDenseClip(
                new[] { ("Root", k_PreClipRootLocalTranslation), ("Child", k_PreClipChildLocalTranslation) },
                Array.Empty<(string, quaternion)>(),
                Array.Empty<(string, float3)>());

            BlobFile.WriteBlobAsset(ref denseMaskClipTChannels, k_MaskTestClipTChannelsClipBlobName);

            var denseMaskClipRChannels = CreateConstantDenseClip(
                Array.Empty<(string, float3)>(),
                new[] { ("Root", k_PreClipRootLocalRotation), ("Child", k_PreClipChildLocalRotation) },
                Array.Empty<(string, float3)>());

            BlobFile.WriteBlobAsset(ref denseMaskClipRChannels, k_MaskTestClipRChannelsClipBlobName);

            var denseMaskClipTRChannels = CreateConstantDenseClip(
                new[] { ("Root", k_PreClipRootLocalTranslation), ("Child", k_PreClipChildLocalTranslation) },
                new[] { ("Root", k_PreClipRootLocalRotation), ("Child", k_PreClipChildLocalRotation) },
                Array.Empty<(string, float3)>());

            BlobFile.WriteBlobAsset(ref denseMaskClipTRChannels, k_MaskTestClipTRChannelsClipBlobName);

            var denseMaskClipTSChannels = CreateConstantDenseClip(
                new[] { ("Root", k_PreClipRootLocalTranslation), ("Child", k_PreClipChildLocalTranslation) },
                Array.Empty<(string, quaternion)>(),
                new[] { ("Root", k_PreClipRootLocalScale), ("Child", k_PreClipChildLocalScale) });

            BlobFile.WriteBlobAsset(ref denseMaskClipTSChannels, k_MaskTestClipTSChannelsClipBlobName);

            var denseMaskClipRSChannels = CreateConstantDenseClip(
                Array.Empty<(string, float3)>(),
                new[] { ("Root", k_PreClipRootLocalRotation), ("Child", k_PreClipChildLocalRotation) },
                new[] { ("Root", k_PreClipRootLocalScale), ("Child", k_PreClipChildLocalScale) });

            BlobFile.WriteBlobAsset(ref denseMaskClipRSChannels, k_MaskTestClipRSChannelsClipBlobName);
#endif
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // Create rig
            m_Rig = new Rig { Value = CreateRigDefinition() };

            m_PreGraphClip = BlobFile.ReadBlobAsset<Clip>(k_PreGraphClipBlobName);
            m_PostGraphClip = BlobFile.ReadBlobAsset<Clip>(k_PostGraphClipBlobName);
            m_MaskTestClip_TChannels = BlobFile.ReadBlobAsset<Clip>(k_MaskTestClipTChannelsClipBlobName);
            m_MaskTestClip_RChannels = BlobFile.ReadBlobAsset<Clip>(k_MaskTestClipRChannelsClipBlobName);
            m_MaskTestClip_TRChannels = BlobFile.ReadBlobAsset<Clip>(k_MaskTestClipTRChannelsClipBlobName);
            m_MaskTestClip_TSChannels = BlobFile.ReadBlobAsset<Clip>(k_MaskTestClipTSChannelsClipBlobName);
            m_MaskTestClip_RSChannels = BlobFile.ReadBlobAsset<Clip>(k_MaskTestClipRSChannelsClipBlobName);
            ClipManager.Instance.GetClipFor(m_Rig, m_PreGraphClip);
            ClipManager.Instance.GetClipFor(m_Rig, m_PostGraphClip);
            ClipManager.Instance.GetClipFor(m_Rig, m_MaskTestClip_TChannels);
            ClipManager.Instance.GetClipFor(m_Rig, m_MaskTestClip_RChannels);
            ClipManager.Instance.GetClipFor(m_Rig, m_MaskTestClip_TRChannels);
            ClipManager.Instance.GetClipFor(m_Rig, m_MaskTestClip_TSChannels);
            ClipManager.Instance.GetClipFor(m_Rig, m_MaskTestClip_RSChannels);
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
        public void RootTransformComponents_AreUpdated_ByDefault()
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, entity);
            SetupTransformComponents(entity, 0f, quaternion.identity, 1f, Entity.Null);

            CreateGraph(entity, m_Rig, PreSet, m_PreGraphClip);
            CreateGraph(entity, m_Rig, PostSet, m_PostGraphClip);

            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(float3.zero).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(quaternion.identity).Using(RotationComparer));
            Assert.That(m_Manager.GetComponentData<NonUniformScale>(entity).Value, Is.EqualTo(math.float3(1f)).Using(ScaleComparer));

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            // Validate that root transform components have been updated with PreGraph values
            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(k_PreClipRootLocalTranslation).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(k_PreClipRootLocalRotation).Using(RotationComparer));
            Assert.That(m_Manager.GetComponentData<NonUniformScale>(entity).Value, Is.EqualTo(k_PreClipRootLocalScale).Using(ScaleComparer));

            var stream = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            // Validate that stream root values have been reset
            Assert.That(stream.GetLocalToParentTranslation(0), Is.EqualTo(float3.zero).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(0), Is.EqualTo(quaternion.identity).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(0), Is.EqualTo(math.float3(1f)).Using(ScaleComparer));

            // Validate that other stream values have been animated with PreGraph values
            Assert.That(stream.GetLocalToParentTranslation(1), Is.EqualTo(k_PreClipChildLocalTranslation).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(1), Is.EqualTo(k_PreClipChildLocalRotation).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(1), Is.EqualTo(k_PreClipChildLocalScale).Using(ScaleComparer));

            m_PostAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            // Validate that root transform components have been updated with PostGraph values
            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(k_PostClipRootLocalTranslation).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(k_PostClipRootLocalRotation).Using(RotationComparer));
            Assert.That(m_Manager.GetComponentData<NonUniformScale>(entity).Value, Is.EqualTo(k_PostClipRootLocalScale).Using(ScaleComparer));

            stream = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            // Validate that stream root values have been reset
            Assert.That(stream.GetLocalToParentTranslation(0), Is.EqualTo(float3.zero).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(0), Is.EqualTo(quaternion.identity).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(0), Is.EqualTo(math.float3(1f)).Using(ScaleComparer));

            // Validate that other stream values have been animated with PostGraph values
            Assert.That(stream.GetLocalToParentTranslation(1), Is.EqualTo(k_PostClipChildLocalTranslation).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(1), Is.EqualTo(k_PostClipChildLocalRotation).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(1), Is.EqualTo(k_PostClipChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        public void RootTransformComponents_AreUpdated_ConsideringChannelMasks()
        {
            float3 entityTranslation = math.float3(5f, 0f, 2f);
            quaternion entityRotation = quaternion.RotateY(math.radians(20f));
            float3 entityScale = math.float3(2f, 1f, 0.5f);

            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, entity);
            SetupTransformComponents(entity, entityTranslation, entityRotation, entityScale, Entity.Null);

            var clipNode = CreateGraph(entity, m_Rig, PreSet, m_MaskTestClip_TChannels);

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            // Validate that only the Translation component has been updated
            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(k_PreClipRootLocalTranslation).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(entityRotation).Using(RotationComparer));
            Assert.That(m_Manager.GetComponentData<NonUniformScale>(entity).Value, Is.EqualTo(entityScale).Using(ScaleComparer));

            SetupTransformComponents(entity, entityTranslation, entityRotation, entityScale, Entity.Null);
            PreSet.SendMessage(clipNode, ClipNode.SimulationPorts.Clip, m_MaskTestClip_TSChannels);
            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            // Validate that only the Translation and NonUniformScale components have been updated
            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(k_PreClipRootLocalTranslation).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(entityRotation).Using(RotationComparer));
            Assert.That(m_Manager.GetComponentData<NonUniformScale>(entity).Value, Is.EqualTo(k_PreClipRootLocalScale).Using(ScaleComparer));

            SetupTransformComponents(entity, entityTranslation, entityRotation, entityScale, Entity.Null);
            PreSet.SendMessage(clipNode, ClipNode.SimulationPorts.Clip, m_MaskTestClip_RSChannels);
            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            // Validate that only the Rotation and NonUniformScale components have been updated
            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(entityTranslation).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(k_PreClipRootLocalRotation).Using(RotationComparer));
            Assert.That(m_Manager.GetComponentData<NonUniformScale>(entity).Value, Is.EqualTo(k_PreClipRootLocalScale).Using(ScaleComparer));
        }

        [Test]
        public void RootTransformComponents_AreNotUpdated_WithDisableRootTransformReadWriteTag()
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, entity);
            SetupTransformComponents(entity, 0f, quaternion.identity, 1f, Entity.Null);

            CreateGraph(entity, m_Rig, PreSet, m_PreGraphClip);
            CreateGraph(entity, m_Rig, PostSet, m_PostGraphClip);

            // Add tag to disable root transform handling
            m_Manager.AddComponent<DisableRootTransformReadWriteTag>(entity);

            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(float3.zero).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(quaternion.identity).Using(RotationComparer));
            Assert.That(m_Manager.GetComponentData<NonUniformScale>(entity).Value, Is.EqualTo(math.float3(1f)).Using(ScaleComparer));

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            // Validate that root transform components have not been updated
            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(float3.zero).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(quaternion.identity).Using(RotationComparer));
            Assert.That(m_Manager.GetComponentData<NonUniformScale>(entity).Value, Is.EqualTo(math.float3(1f)).Using(ScaleComparer));

            var stream = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            // Validate that stream root values hold the animated values
            Assert.That(stream.GetLocalToParentTranslation(0), Is.EqualTo(k_PreClipRootLocalTranslation).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(0), Is.EqualTo(k_PreClipRootLocalRotation).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(0), Is.EqualTo(k_PreClipRootLocalScale).Using(ScaleComparer));

            // Validate that other stream values have been animated
            Assert.That(stream.GetLocalToParentTranslation(1), Is.EqualTo(k_PreClipChildLocalTranslation).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(1), Is.EqualTo(k_PreClipChildLocalRotation).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(1), Is.EqualTo(k_PreClipChildLocalScale).Using(ScaleComparer));

            m_PostAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            // Validate that root transform components have not been updated
            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(float3.zero).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(quaternion.identity).Using(RotationComparer));
            Assert.That(m_Manager.GetComponentData<NonUniformScale>(entity).Value, Is.EqualTo(math.float3(1f)).Using(ScaleComparer));

            stream = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            // Validate that stream root values hold the animated PostGraph values
            Assert.That(stream.GetLocalToParentTranslation(0), Is.EqualTo(k_PostClipRootLocalTranslation).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(0), Is.EqualTo(k_PostClipRootLocalRotation).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(0), Is.EqualTo(k_PostClipRootLocalScale).Using(ScaleComparer));

            // Validate that other stream values have been animated with PostGraph values
            Assert.That(stream.GetLocalToParentTranslation(1), Is.EqualTo(k_PostClipChildLocalTranslation).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(1), Is.EqualTo(k_PostClipChildLocalRotation).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(1), Is.EqualTo(k_PostClipChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        public void RootTransformComponents_AreUpdated_WithAnimatedRootMotionComponent()
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, entity);
            SetupTransformComponents(entity, 0f, quaternion.identity, 1f, Entity.Null);

            CreateGraph(entity, m_Rig, PreSet, m_PreGraphClip);
            CreateGraph(entity, m_Rig, PostSet, m_PostGraphClip);

            // Add animated root motion component
            m_Manager.AddComponent<ProcessDefaultAnimationGraph.AnimatedRootMotion>(entity);

            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(float3.zero).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(quaternion.identity).Using(RotationComparer));
            Assert.That(m_Manager.GetComponentData<NonUniformScale>(entity).Value, Is.EqualTo(math.float3(1f)).Using(ScaleComparer));

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            // Validate that root transform components have T and R components updated
            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(k_PreClipRootLocalTranslation).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(k_PreClipRootLocalRotation).Using(RotationComparer));

            var stream = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            // Validate that stream root values have been reset
            Assert.That(stream.GetLocalToParentTranslation(0), Is.EqualTo(float3.zero).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(0), Is.EqualTo(quaternion.identity).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(0), Is.EqualTo(math.float3(1f)).Using(ScaleComparer));

            // Validate that other stream values have been animated
            Assert.That(stream.GetLocalToParentTranslation(1), Is.EqualTo(k_PreClipChildLocalTranslation).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(1), Is.EqualTo(k_PreClipChildLocalRotation).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(1), Is.EqualTo(k_PreClipChildLocalScale).Using(ScaleComparer));

            // Tick pre animation graph to force root transform accumulation on the
            // root entity transform components
            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            stream = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            // Validate that root transform components have T and R components updated with accumulated value
            var accum = new RigidTransform(k_PreClipRootLocalRotation, k_PreClipRootLocalTranslation);
            accum = math.mul(accum, accum);
            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(accum.pos).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(accum.rot).Using(RotationComparer));

            // Validate that stream root values have been reset
            Assert.That(stream.GetLocalToParentTranslation(0), Is.EqualTo(float3.zero).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(0), Is.EqualTo(quaternion.identity).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(0), Is.EqualTo(math.float3(1f)).Using(ScaleComparer));

            // Validate that other stream values have been animated
            Assert.That(stream.GetLocalToParentTranslation(1), Is.EqualTo(k_PreClipChildLocalTranslation).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(1), Is.EqualTo(k_PreClipChildLocalRotation).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(1), Is.EqualTo(k_PreClipChildLocalScale).Using(ScaleComparer));

            // Tick post animation graph that is default mode and has a clip that overwrite root values
            m_PostAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            // Validate that root transform components have been updated with post clip values
            // since this system was not in root motion, values should be overwritten by post system.
            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(k_PostClipRootLocalTranslation).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(k_PostClipRootLocalRotation).Using(RotationComparer));
            Assert.That(m_Manager.GetComponentData<NonUniformScale>(entity).Value, Is.EqualTo(k_PostClipRootLocalScale).Using(ScaleComparer));

            stream = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            // Validate that stream root values have been reset
            Assert.That(stream.GetLocalToParentTranslation(0), Is.EqualTo(float3.zero).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(0), Is.EqualTo(quaternion.identity).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(0), Is.EqualTo(math.float3(1f)).Using(ScaleComparer));

            // Validate that other stream values have been animated with PostGraph values
            Assert.That(stream.GetLocalToParentTranslation(1), Is.EqualTo(k_PostClipChildLocalTranslation).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(1), Is.EqualTo(k_PostClipChildLocalRotation).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(1), Is.EqualTo(k_PostClipChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        public void RootTransformComponents_AreUpdated_WithAnimatedRootMotionComponentConsideringChannelMasks()
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, entity);
            SetupTransformComponents(entity, 0f, quaternion.identity, 1f, Entity.Null);

            var clipNode = CreateGraph(entity, m_Rig, PreSet, m_MaskTestClip_TChannels);

            // Add animated root motion component
            m_Manager.AddComponent<ProcessDefaultAnimationGraph.AnimatedRootMotion>(entity);
            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            // Validate that root transform T component is the only one that has been updated, no default values have been inserted.
            var accumRootTx = RigidTransform.identity;
            var deltaRootTx = new RigidTransform(quaternion.identity, k_PreClipRootLocalTranslation);
            accumRootTx = math.mul(accumRootTx, deltaRootTx);
            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(accumRootTx.pos).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(accumRootTx.rot).Using(RotationComparer));

            Set.SendMessage(clipNode, ClipNode.SimulationPorts.Clip, m_MaskTestClip_RChannels);
            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            // Validate that root transform components have T and R components updated with accumulated value. Only channel R is animated.
            deltaRootTx = new RigidTransform(k_PreClipRootLocalRotation, float3.zero);
            accumRootTx = math.mul(accumRootTx, deltaRootTx);
            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(accumRootTx.pos).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(accumRootTx.rot).Using(RotationComparer));

            Set.SendMessage(clipNode, ClipNode.SimulationPorts.Clip, m_MaskTestClip_TRChannels);
            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            // Validate that root transform components have T and R components updated with accumulated value. TR channels are animated.
            deltaRootTx = new RigidTransform(k_PreClipRootLocalRotation, k_PreClipRootLocalTranslation);
            accumRootTx = math.mul(accumRootTx, deltaRootTx);
            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(accumRootTx.pos).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(accumRootTx.rot).Using(RotationComparer));
        }

        [Test]
        public void RootTransformComponents_AreUpdated_WithAnimatedRootMotionAndOffsetComponents()
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, entity);
            SetupTransformComponents(entity, 0f, quaternion.identity, 1f, Entity.Null);

            CreateGraph(entity, m_Rig, PreSet, m_PreGraphClip);
            CreateGraph(entity, m_Rig, PostSet, m_PostGraphClip);

            // Add animated root motion and offset components
            var rmOffset = new RigidTransform(quaternion.AxisAngle(math.float3(1f, 0f, 0f), math.radians(30f)), math.float3(2f, 3f, 4f));
            m_Manager.AddComponent<ProcessDefaultAnimationGraph.AnimatedRootMotion>(entity);
            m_Manager.AddComponentData(entity, new RootMotionOffset { Value = rmOffset });

            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(float3.zero).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(quaternion.identity).Using(RotationComparer));
            Assert.That(m_Manager.GetComponentData<NonUniformScale>(entity).Value, Is.EqualTo(math.float3(1f)).Using(ScaleComparer));

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            // Validate that root transform components have T and R components updated (with offset)
            var rootClipValues = new RigidTransform(k_PreClipRootLocalRotation, k_PreClipRootLocalTranslation);
            var tx = math.mul(rmOffset, rootClipValues);
            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(tx.pos).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(tx.rot).Using(RotationComparer));

            // Validate that RootMotionOffset component has been reset to prevent feedback loop
            rmOffset = m_Manager.GetComponentData<RootMotionOffset>(entity).Value;
            Assert.That(rmOffset.pos, Is.EqualTo(float3.zero).Using(TranslationComparer));
            Assert.That(rmOffset.rot, Is.EqualTo(quaternion.identity).Using(RotationComparer));

            var stream = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            // Validate that stream root values have been reset
            Assert.That(stream.GetLocalToParentTranslation(0), Is.EqualTo(float3.zero).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(0), Is.EqualTo(quaternion.identity).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(0), Is.EqualTo(math.float3(1f)).Using(ScaleComparer));

            // Validate that other stream values have been animated
            Assert.That(stream.GetLocalToParentTranslation(1), Is.EqualTo(k_PreClipChildLocalTranslation).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(1), Is.EqualTo(k_PreClipChildLocalRotation).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(1), Is.EqualTo(k_PreClipChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        public void RootTransformComponents_AreUpdated_WithAnimatedRootMotionAndTeleportation()
        {
            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, m_Rig, entity);
            SetupTransformComponents(entity, 0f, quaternion.identity, 1f, Entity.Null);

            CreateGraph(entity, m_Rig, PreSet, m_PreGraphClip);
            CreateGraph(entity, m_Rig, PostSet, m_PostGraphClip);

            // Add animated root motion and teleport
            m_Manager.AddComponent<ProcessDefaultAnimationGraph.AnimatedRootMotion>(entity);

            var teleport1Tx = new RigidTransform(quaternion.AxisAngle(math.float3(0f, 1f, 0f), math.radians(15f)), math.float3(4f, 1f, 2f));
            m_Manager.SetComponentData(entity, new Translation { Value = teleport1Tx.pos });
            m_Manager.SetComponentData(entity, new Rotation { Value = teleport1Tx.rot });

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            // Validate that root transform components have T and R components updated (considering teleportTx)
            var rootClipValues = new RigidTransform(k_PreClipRootLocalRotation, k_PreClipRootLocalTranslation);
            var tx = math.mul(teleport1Tx, rootClipValues);
            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(tx.pos).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(tx.rot).Using(RotationComparer));

            var stream = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            // Validate that stream root values have been reset
            Assert.That(stream.GetLocalToParentTranslation(0), Is.EqualTo(float3.zero).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(0), Is.EqualTo(quaternion.identity).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(0), Is.EqualTo(math.float3(1f)).Using(ScaleComparer));

            // Validate that other stream values have been animated
            Assert.That(stream.GetLocalToParentTranslation(1), Is.EqualTo(k_PreClipChildLocalTranslation).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(1), Is.EqualTo(k_PreClipChildLocalRotation).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(1), Is.EqualTo(k_PreClipChildLocalScale).Using(ScaleComparer));

            var teleport2Tx = new RigidTransform(quaternion.AxisAngle(math.float3(1f, 0f, 0f), math.radians(30f)), math.float3(1f, 2f, 3f));
            m_Manager.SetComponentData(entity, new Translation { Value = teleport2Tx.pos });
            m_Manager.SetComponentData(entity, new Rotation { Value = teleport2Tx.rot });

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            tx = math.mul(teleport2Tx, rootClipValues);
            Assert.That(m_Manager.GetComponentData<Translation>(entity).Value, Is.EqualTo(tx.pos).Using(TranslationComparer));
            Assert.That(m_Manager.GetComponentData<Rotation>(entity).Value, Is.EqualTo(tx.rot).Using(RotationComparer));

            // Validate that stream root values have been reset
            Assert.That(stream.GetLocalToParentTranslation(0), Is.EqualTo(float3.zero).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(0), Is.EqualTo(quaternion.identity).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(0), Is.EqualTo(math.float3(1f)).Using(ScaleComparer));

            // Validate that other stream values have been animated
            Assert.That(stream.GetLocalToParentTranslation(1), Is.EqualTo(k_PreClipChildLocalTranslation).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(1), Is.EqualTo(k_PreClipChildLocalRotation).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(1), Is.EqualTo(k_PreClipChildLocalScale).Using(ScaleComparer));
        }

        [Test]
        public void RootTransformComponents_AreUpdated_ConsideringOffsetInHierarchy()
        {
            // Create hierarchy setup with offset transform not part of rig definition
            // RigEntity = GO with RigComponent
            //   |-> Offset = intermediate transform not part of definition
            //     |-> Root = bone 0 of rig definition
            //       |-> Child = bone 1 of rig definition

            var rigEntity    = m_Manager.CreateEntity();
            var offsetEntity = m_Manager.CreateEntity();
            var rigBones     = CreateRigTransforms(m_Rig, offsetEntity);

            SetupRigEntity(rigEntity, m_Rig, rigBones[0]);

            var rigT = math.float3(1f, -2f, 1f);
            var rigR = quaternion.RotateZ(math.radians(20f));
            var rigS = math.float3(2f, 1f, 1f);
            SetupTransformComponents(rigEntity, rigT, rigR, rigS, Entity.Null);

            var offsetT = math.float3(0f, 0f, 3f);
            var offsetR = quaternion.RotateY(math.radians(-30f));
            var offsetS = math.float3(1f, 2f, 0.5f);
            SetupTransformComponents(offsetEntity, offsetT, offsetR, offsetS, rigEntity);

            CreateGraph(rigEntity, m_Rig, PreSet, m_PreGraphClip);

            m_PreAnimationGraph.Update();
            World.GetOrCreateSystem<EndFrameParentSystem>().Update();
            World.GetOrCreateSystem<EndFrameLocalToParentSystem>().Update();
            World.GetOrCreateSystem<ComputeRigMatrices>().Update();
            m_Manager.CompleteAllJobs();

            var stream = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );

            // Validate that stream root values have been reset
            Assert.That(stream.GetLocalToParentTranslation(0), Is.EqualTo(float3.zero).Using(TranslationComparer));
            Assert.That(stream.GetLocalToParentRotation(0), Is.EqualTo(quaternion.identity).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentScale(0), Is.EqualTo(math.float3(1f)).Using(ScaleComparer));

            // Validate that root bone transform components have been updated
            var rootT = m_Manager.GetComponentData<Translation>(rigBones[0]).Value;
            var rootR = m_Manager.GetComponentData<Rotation>(rigBones[0]).Value;
            var rootS = m_Manager.GetComponentData<NonUniformScale>(rigBones[0]).Value;
            Assert.That(rootT, Is.EqualTo(k_PreClipRootLocalTranslation).Using(TranslationComparer));
            Assert.That(rootR, Is.EqualTo(k_PreClipRootLocalRotation).Using(RotationComparer));
            Assert.That(rootS, Is.EqualTo(k_PreClipRootLocalScale).Using(ScaleComparer));

            var rigTx    = float4x4.TRS(rigT, rigR, rigS);
            var offsetTx = float4x4.TRS(offsetT, offsetR, offsetS);
            var rootTx   = float4x4.TRS(rootT, rootR, rootS);

            var l2wRootTx = math.mul(math.mul(rigTx, offsetTx), rootTx);
            Assert.That(m_Manager.GetComponentData<LocalToWorld>(rigBones[0]).Value, Is.EqualTo(l2wRootTx).Using(Float4x4Comparer));

            // Validate that AnimatedLocalToWorld have been updated
            var childTx  = float4x4.TRS(k_PreClipChildLocalTranslation, k_PreClipChildLocalRotation, k_PreClipChildLocalScale);
            var l2wChildTx = math.mul(l2wRootTx, childTx);
            var animatedLocal2World = m_Manager.GetBuffer<AnimatedLocalToWorld>(rigEntity);
            Assert.That(animatedLocal2World[0].Value, Is.EqualTo(l2wRootTx).Using(Float4x4Comparer));
            Assert.That(animatedLocal2World[1].Value, Is.EqualTo(l2wChildTx).Using(Float4x4Comparer));

            // Update offset entity transform components and test that local to world values change as expected
            offsetT = math.float3(2f, 3f, 0f);
            offsetR = quaternion.RotateY(math.radians(20f));
            offsetS = math.float3(1f, 1f, 2f);
            offsetTx = float4x4.TRS(offsetT, offsetR, offsetS);
            m_Manager.SetComponentData(offsetEntity, new Translation { Value = offsetT });
            m_Manager.SetComponentData(offsetEntity, new Rotation { Value = offsetR });
            m_Manager.SetComponentData(offsetEntity, new NonUniformScale { Value = offsetS });
            m_Manager.SetComponentData(offsetEntity, new LocalToParent { Value = offsetTx });

            m_PreAnimationGraph.Update();
            World.GetOrCreateSystem<EndFrameParentSystem>().Update();
            World.GetOrCreateSystem<EndFrameLocalToParentSystem>().Update();
            World.GetOrCreateSystem<ComputeRigMatrices>().Update();
            m_Manager.CompleteAllJobs();

            // Validate that root bone LocalToWorld has been updated
            l2wRootTx = math.mul(math.mul(rigTx, offsetTx), rootTx);
            Assert.That(m_Manager.GetComponentData<LocalToWorld>(rigBones[0]).Value, Is.EqualTo(l2wRootTx).Using(Float4x4Comparer));

            // Validate that AnimatedLocalToWorld have been updated
            l2wChildTx = math.mul(l2wRootTx, childTx);
            animatedLocal2World = m_Manager.GetBuffer<AnimatedLocalToWorld>(rigEntity);
            Assert.That(animatedLocal2World[0].Value, Is.EqualTo(l2wRootTx).Using(Float4x4Comparer));
            Assert.That(animatedLocal2World[1].Value, Is.EqualTo(l2wChildTx).Using(Float4x4Comparer));
        }
    }
}
