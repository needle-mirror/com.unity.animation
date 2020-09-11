using NUnit.Framework;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;

namespace Unity.Animation.Tests
{
    public class AnimatedMatricesTests : AnimationTestsFixture
    {
        static readonly float3 k_RootLocalT = new float3(1f, -2f, 5f);
        static readonly quaternion k_RootLocalR = quaternion.RotateY(math.radians(30f));
        static readonly float3 k_RootLocalS = new float3(0.5f, 1f, 1.5f);

        static readonly float3 k_ChildLocalT = new float3(5f, 3f, -3f);
        static readonly quaternion k_ChildLocalR = quaternion.RotateZ(math.radians(60f));
        static readonly float3 k_ChildLocalS = new float3(3f, 2f, 1f);
        static readonly float4x4 k_ChildLocalTx = float4x4.TRS(k_ChildLocalT, k_ChildLocalR, k_ChildLocalS);

        Rig m_Rig_RootIdentity;
        Rig m_Rig_RootOffset;

        BlobAssetReference<RigDefinition> CreateRigDefinition(float3 rootT, quaternion rootR, float3 rootS)
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode { Id = "Root", ParentIndex = -1, AxisIndex = -1, LocalTranslationDefaultValue = rootT, LocalRotationDefaultValue = rootR, LocalScaleDefaultValue = rootS },
                new SkeletonNode { Id = "Child", ParentIndex = 0, AxisIndex = -1, LocalTranslationDefaultValue = k_ChildLocalT, LocalRotationDefaultValue = k_ChildLocalR, LocalScaleDefaultValue = k_ChildLocalS }
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

        void ExecuteSystemPipeline()
        {
            m_PreAnimationGraph.Update();
            World.GetOrCreateSystem<EndFrameParentSystem>().Update();
            World.GetOrCreateSystem<EndFrameLocalToParentSystem>().Update();
            World.GetOrCreateSystem<RigComputeMatricesSystem>().Update();
            m_Manager.CompleteAllJobs();
        }

        void ValidateAnimatedLocalToRoot(Entity rigEntity)
        {
            var rigAnimatedL2R = m_Manager.GetBuffer<AnimatedLocalToRoot>(rigEntity);
            var expected = new NativeArray<float4x4>(2, Allocator.Temp);

            if (!m_Manager.HasComponent<DisableRootTransformReadWriteTag>(rigEntity))
            {
                // When root transform management is enabled, element data at index 0 of animation stream is
                // copied back to the RigRoot entity transform components and reset to identity in animation stream.
                expected[0] = float4x4.identity;
                expected[1] = k_ChildLocalTx;
            }
            else
            {
                // When root transform management is disabled, everything remains in the animation stream
                var rig = m_Manager.GetComponentData<Rig>(rigEntity);
                var rigAnimatedData = m_Manager.GetBuffer<AnimatedData>(rigEntity);
                var stream = AnimationStream.CreateReadOnly(rig, rigAnimatedData.AsNativeArray());

                expected[0] = stream.GetLocalToParentMatrix(0);
                expected[1] = math.mul(expected[0], k_ChildLocalTx);
            }

            using (expected)
            {
                for (int i = 0; i < 2; ++i)
                {
                    Assert.That(rigAnimatedL2R[i].Value, Is.EqualTo(expected[i]).Using(Float4x4Comparer));
                }
            }
        }

        void ValidateAnimatedLocalToWorld(Entity rigEntity)
        {
            var rigRoot = m_Manager.GetComponentData<RigRootEntity>(rigEntity);
            var rootL2W = m_Manager.GetComponentData<LocalToWorld>(rigRoot.Value).Value;

            var rigAnimatedL2W = m_Manager.GetBuffer<AnimatedLocalToWorld>(rigEntity);
            var expected = new NativeArray<float4x4>(2, Allocator.Temp);

            if (!m_Manager.HasComponent<DisableRootTransformReadWriteTag>(rigEntity))
            {
                // When root transform management is enabled, element data at index 0 of animation stream is
                // copied back to the RigRoot entity transform components and reset to identity in animation stream.
                expected[0] = rootL2W;
                expected[1] = math.mul(expected[0], k_ChildLocalTx);
            }
            else
            {
                // When root transform is disabled, everything remains in the animation stream
                var rig = m_Manager.GetComponentData<Rig>(rigEntity);
                var rigAnimatedData = m_Manager.GetBuffer<AnimatedData>(rigEntity);
                var stream = AnimationStream.CreateReadOnly(rig, rigAnimatedData.AsNativeArray());

                expected[0] = math.mul(rootL2W, stream.GetLocalToParentMatrix(0));
                expected[1] = math.mul(expected[0], k_ChildLocalTx);
            }

            using (expected)
            {
                for (int i = 0; i < 2; ++i)
                {
                    Assert.That(rigAnimatedL2W[i].Value, Is.EqualTo(expected[i]).Using(Float4x4Comparer));
                }
            }
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            m_Rig_RootIdentity = new Rig { Value = CreateRigDefinition(float3.zero, quaternion.identity, 1f) };
            m_Rig_RootOffset   = new Rig { Value = CreateRigDefinition(k_RootLocalT, k_RootLocalR, k_RootLocalS) };
        }

        [OneTimeTearDown]
        protected override void OneTimeTearDown()
        {
            m_Rig_RootIdentity.Value.Dispose();
            m_Rig_RootOffset.Value.Dispose();
            base.OneTimeTearDown();
        }

        [Test]
        public void CanComputeAnimatedLocalToRoot_NoRigParenting()
        {
            var rig1 = m_Rig_RootIdentity;
            var rig1Transforms = CreateRigTransforms(rig1, Entity.Null);
            SetupRigEntity(rig1Transforms[0], rig1, rig1Transforms[0]);
            m_Manager.AddBuffer<AnimatedLocalToRoot>(rig1Transforms[0]).ResizeUninitialized(2);

            var rig2 = m_Rig_RootOffset;
            var rig2Transforms = CreateRigTransforms(rig2, Entity.Null);
            SetupRigEntity(rig2Transforms[0], rig2, rig2Transforms[0]);
            m_Manager.AddBuffer<AnimatedLocalToRoot>(rig2Transforms[0]).ResizeUninitialized(2);

            var rig3 = m_Rig_RootOffset;
            var rig3Transforms = CreateRigTransforms(rig3, Entity.Null);
            SetupRigEntity(rig3Transforms[0], rig3, rig3Transforms[0]);
            m_Manager.AddComponent<DisableRootTransformReadWriteTag>(rig3Transforms[0]);
            m_Manager.AddBuffer<AnimatedLocalToRoot>(rig3Transforms[0]).ResizeUninitialized(2);

            ExecuteSystemPipeline();
            ValidateAnimatedLocalToRoot(rig1Transforms[0]);
            ValidateAnimatedLocalToRoot(rig2Transforms[0]);
            ValidateAnimatedLocalToRoot(rig3Transforms[0]);

            // Update random transform components
            m_Manager.SetComponentData(rig1Transforms[0], new Translation { Value = new float3(0f, 4f, 1f) });
            m_Manager.SetComponentData(rig2Transforms[0], new Rotation { Value = quaternion.RotateZ(math.radians(10f)) });
            m_Manager.SetComponentData(rig3Transforms[0], new Translation { Value = new float3(1f, 2f, 5f) });

            ExecuteSystemPipeline();
            ValidateAnimatedLocalToRoot(rig1Transforms[0]);
            ValidateAnimatedLocalToRoot(rig2Transforms[0]);
            ValidateAnimatedLocalToRoot(rig3Transforms[0]);
        }

        [Test]
        public void CanComputeAnimatedLocalToRoot_WithRigParenting()
        {
            var rig1 = m_Rig_RootIdentity;
            var rig1Entity = m_Manager.CreateEntity();
            SetupTransformComponents(rig1Entity, math.float3(1f, 2f, 3f), quaternion.RotateY(math.radians(5f)), 1f, Entity.Null);
            var rig1Transforms = CreateRigTransforms(rig1, rig1Entity);
            SetupRigEntity(rig1Entity, rig1, rig1Transforms[0]);
            m_Manager.AddBuffer<AnimatedLocalToRoot>(rig1Entity).ResizeUninitialized(2);

            var rig2 = m_Rig_RootOffset;
            var rig2Entity = m_Manager.CreateEntity();
            SetupTransformComponents(rig2Entity, math.float3(1f, 2f, -3f), quaternion.RotateY(math.radians(-5f)), 1f, Entity.Null);
            var rig2Transforms = CreateRigTransforms(rig2, rig2Entity);
            SetupRigEntity(rig2Entity, rig2, rig2Transforms[0]);
            m_Manager.AddBuffer<AnimatedLocalToRoot>(rig2Entity).ResizeUninitialized(2);

            var rig3 = m_Rig_RootOffset;
            var rig3Entity = m_Manager.CreateEntity();
            SetupTransformComponents(rig3Entity, math.float3(3f, 2f, 1f), quaternion.RotateX(math.radians(-5f)), 1f, Entity.Null);
            var rig3Transforms = CreateRigTransforms(rig3, Entity.Null);
            SetupRigEntity(rig3Entity, rig3, rig3Transforms[0]);
            m_Manager.AddComponent<DisableRootTransformReadWriteTag>(rig3Entity);
            m_Manager.AddBuffer<AnimatedLocalToRoot>(rig3Entity).ResizeUninitialized(2);

            ExecuteSystemPipeline();
            ValidateAnimatedLocalToRoot(rig1Entity);
            ValidateAnimatedLocalToRoot(rig2Entity);
            ValidateAnimatedLocalToRoot(rig3Entity);

            // Update transform components
            m_Manager.SetComponentData(rig1Entity, new Translation { Value = new float3(0f, 4f, 1f) });
            m_Manager.SetComponentData(rig1Transforms[0], new Rotation { Value = quaternion.RotateZ(math.radians(10f)) });
            m_Manager.SetComponentData(rig2Entity, new Translation { Value = new float3(1f, 3f, 3f) });
            m_Manager.SetComponentData(rig2Transforms[0], new Rotation { Value = quaternion.RotateX(math.radians(10f)) });
            m_Manager.SetComponentData(rig3Entity, new Rotation { Value = quaternion.RotateY(math.radians(10f)) });
            m_Manager.SetComponentData(rig3Transforms[0], new Translation { Value = new float3(0f, 4f, 1f) });

            ExecuteSystemPipeline();
            ValidateAnimatedLocalToRoot(rig1Entity);
            ValidateAnimatedLocalToRoot(rig2Entity);
            ValidateAnimatedLocalToRoot(rig3Entity);
        }

        [Test]
        public void CanComputeAnimatedLocalToWorld_NoRigParenting()
        {
            var rig1 = m_Rig_RootIdentity;
            var rig1Transforms = CreateRigTransforms(rig1, Entity.Null);
            SetupRigEntity(rig1Transforms[0], rig1, rig1Transforms[0]);

            var rig2 = m_Rig_RootOffset;
            var rig2Transforms = CreateRigTransforms(rig2, Entity.Null);
            SetupRigEntity(rig2Transforms[0], rig2, rig2Transforms[0]);

            var rig3 = m_Rig_RootOffset;
            var rig3Transforms = CreateRigTransforms(rig3, Entity.Null);
            SetupRigEntity(rig3Transforms[0], rig3, rig3Transforms[0]);
            m_Manager.AddComponent<DisableRootTransformReadWriteTag>(rig3Transforms[0]);

            ExecuteSystemPipeline();
            ValidateAnimatedLocalToWorld(rig1Transforms[0]);
            ValidateAnimatedLocalToWorld(rig2Transforms[0]);
            ValidateAnimatedLocalToWorld(rig3Transforms[0]);

            // Update random transform components
            m_Manager.SetComponentData(rig1Transforms[0], new Translation { Value = new float3(0f, 4f, 1f) });
            m_Manager.SetComponentData(rig2Transforms[0], new Rotation { Value = quaternion.RotateZ(math.radians(10f)) });
            m_Manager.SetComponentData(rig3Transforms[0], new Translation { Value = new float3(1f, 2f, 5f) });

            ExecuteSystemPipeline();
            ValidateAnimatedLocalToWorld(rig1Transforms[0]);
            ValidateAnimatedLocalToWorld(rig2Transforms[0]);
            ValidateAnimatedLocalToWorld(rig3Transforms[0]);
        }

        [Test]
        public void CanComputeAnimatedLocalToWorld_WithRigParenting()
        {
            var rig1 = m_Rig_RootIdentity;
            var rig1Entity = m_Manager.CreateEntity();
            SetupTransformComponents(rig1Entity, math.float3(1f, 2f, 3f), quaternion.RotateY(math.radians(5f)), 1f, Entity.Null);
            var rig1Transforms = CreateRigTransforms(rig1, rig1Entity);
            SetupRigEntity(rig1Entity, rig1, rig1Transforms[0]);

            var rig2 = m_Rig_RootOffset;
            var rig2Entity = m_Manager.CreateEntity();
            SetupTransformComponents(rig2Entity, math.float3(1f, 2f, -3f), quaternion.RotateY(math.radians(-5f)), 1f, Entity.Null);
            var rig2Transforms = CreateRigTransforms(rig2, rig2Entity);
            SetupRigEntity(rig2Entity, rig2, rig2Transforms[0]);

            var rig3 = m_Rig_RootOffset;
            var rig3Entity = m_Manager.CreateEntity();
            SetupTransformComponents(rig3Entity, math.float3(3f, 2f, 1f), quaternion.RotateX(math.radians(-5f)), 1f, Entity.Null);
            var rig3Transforms = CreateRigTransforms(rig3, Entity.Null);
            SetupRigEntity(rig3Entity, rig3, rig3Transforms[0]);
            m_Manager.AddComponent<DisableRootTransformReadWriteTag>(rig3Entity);

            ExecuteSystemPipeline();
            ValidateAnimatedLocalToWorld(rig1Entity);
            ValidateAnimatedLocalToWorld(rig2Entity);
            ValidateAnimatedLocalToWorld(rig3Entity);

            // Update transform components
            m_Manager.SetComponentData(rig1Entity, new Translation { Value = new float3(0f, 4f, 1f) });
            m_Manager.SetComponentData(rig1Transforms[0], new Rotation { Value = quaternion.RotateZ(math.radians(10f)) });
            m_Manager.SetComponentData(rig2Entity, new Translation { Value = new float3(1f, 3f, 3f) });
            m_Manager.SetComponentData(rig2Transforms[0], new Rotation { Value = quaternion.RotateX(math.radians(10f)) });
            m_Manager.SetComponentData(rig3Entity, new Rotation { Value = quaternion.RotateY(math.radians(10f)) });
            m_Manager.SetComponentData(rig3Transforms[0], new Translation { Value = new float3(0f, 4f, 1f) });

            ExecuteSystemPipeline();
            ValidateAnimatedLocalToWorld(rig1Entity);
            ValidateAnimatedLocalToWorld(rig2Entity);
            ValidateAnimatedLocalToWorld(rig3Entity);
        }
    }
}
