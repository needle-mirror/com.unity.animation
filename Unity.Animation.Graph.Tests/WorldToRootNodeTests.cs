using NUnit.Framework;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.Animation.Tests
{
    public class WorldToRootNodeTests : AnimationTestsFixture
    {
        static readonly float3     k_RigLocalTranslation = new float3(1.0f, 0.0f, 0.0f);
        static readonly quaternion k_RigLocalRotation = quaternion.RotateX(math.radians(90.0f));
        static readonly float3     k_RigLocalScale = new float3(1.0f, 1.0f, 1.0f);

        static readonly float3     k_OffsetLocalTranslation = new float3(0.0f, 0.0f, 1.0f);
        static readonly quaternion k_OffsetLocalRotation = quaternion.RotateY(math.radians(60.0f));
        static readonly float3     k_OffsetLocalScale = new float3(0.5f, 0.5f, 0.5f);

        static readonly float3     k_RootLocalTranslation = new float3(1.0f, 0.0f, 0.0f);
        static readonly quaternion k_RootLocalRotation = quaternion.identity;
        static readonly float3     k_RootLocalScale = new float3(1.0f, 1.0f, 1.0f);

        static readonly float3     k_TargetLocalTranslation = new float3(5.0f, 1.0f, 2.0f);
        static readonly quaternion k_TargetLocalRotation = quaternion.RotateY(math.radians(60.0f));
        static readonly float3     k_TargetLocalScale = new float3(1.0f, 1.0f, 1.0f);

        Rig m_Rig;
        Rig m_RootOffsetRig;

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

                var localToParent = float4x4.TRS(t, r, s);
                m_Manager.AddComponentData(entity, new LocalToParent { Value = localToParent });

                var parentMatrix = m_Manager.GetComponentData<LocalToWorld>(parent).Value;
                m_Manager.AddComponentData(entity, new LocalToWorld {Value = math.mul(parentMatrix, localToParent)});
            }
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // Create rig
            var skeletonNodes = new[]
            {
                new SkeletonNode
                {
                    Id = "Rig",
                    ParentIndex = -1,
                    AxisIndex = -1,
                    LocalTranslationDefaultValue = k_RigLocalTranslation,
                    LocalRotationDefaultValue = k_RigLocalRotation,
                    LocalScaleDefaultValue = k_RigLocalScale
                },
                new SkeletonNode
                {
                    Id = "Offset",
                    ParentIndex = 0,
                    AxisIndex = -1,
                    LocalTranslationDefaultValue = k_OffsetLocalTranslation,
                    LocalRotationDefaultValue = k_OffsetLocalRotation,
                    LocalScaleDefaultValue = k_OffsetLocalScale
                },
                new SkeletonNode
                {
                    Id = "Root",
                    ParentIndex = 1,
                    AxisIndex = -1,
                    LocalTranslationDefaultValue = k_RootLocalTranslation,
                    LocalRotationDefaultValue = k_RootLocalRotation,
                    LocalScaleDefaultValue = k_RootLocalScale
                },
            };
            m_Rig = new Rig { Value = RigBuilder.CreateRigDefinition(skeletonNodes) };

            var offsetSkeletonNodes = new[]
            {
                new SkeletonNode
                {
                    Id = "Root",
                    ParentIndex = -1,
                    AxisIndex = -1,
                    LocalTranslationDefaultValue = k_RootLocalTranslation,
                    LocalRotationDefaultValue = k_RootLocalRotation,
                    LocalScaleDefaultValue = k_RootLocalScale
                },
            };
            m_RootOffsetRig = new Rig { Value = RigBuilder.CreateRigDefinition(offsetSkeletonNodes) };
        }

        NodeHandle<WorldToRootNode> CreateGraph(Entity rigEntity, Entity targetEntity, in Rig rig, NodeSet set)
        {
            var worldToRootNode = CreateNode<WorldToRootNode>(set);
            var rigEntityNode = CreateComponentNode(rigEntity, set);
            var targetEntityNode = CreateComponentNode(targetEntity, set);

            // Connect the AnimatedData
            set.Connect(rigEntityNode, worldToRootNode, WorldToRootNode.KernelPorts.Input);
            // Connect the RigRootEntity component
            set.Connect(rigEntityNode, worldToRootNode, WorldToRootNode.KernelPorts.RootEntity);
            // Connect the LocalToWorld component of the target Entity
            set.Connect(targetEntityNode, worldToRootNode, WorldToRootNode.KernelPorts.LocalToWorldToRemap);

            set.SendMessage(worldToRootNode, WorldToRootNode.SimulationPorts.Rig, rig);

            return worldToRootNode;
        }

        /// <summary>
        /// Basic setup where the rig entity that has the AnimatedData buffer, is also
        /// the root entity.
        /// </summary>
        [Test]
        public void RemapTarget_RigIsRoot()
        {
            var rigEntity = m_Manager.CreateEntity();
            SetupTransformComponents(rigEntity, k_RigLocalTranslation, k_RigLocalRotation, k_RigLocalScale, Entity.Null);
            SetupRigEntity(rigEntity, m_Rig, rigEntity);

            var targetEntity = m_Manager.CreateEntity();
            SetupTransformComponents(targetEntity, k_TargetLocalTranslation, k_TargetLocalRotation, k_TargetLocalScale, Entity.Null);

            var w2rNode = CreateGraph(rigEntity, targetEntity, m_Rig, PreSet);
            var output = CreateGraphValue(w2rNode, WorldToRootNode.KernelPorts.Output, PreSet);

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            var remapMatrix = (float4x4)m_Manager.GetComponentData<RigRootEntity>(rigEntity).RemapToRootMatrix;
            var expectedRemapMatrix = math.inverse(float4x4.TRS(k_RigLocalTranslation, k_RigLocalRotation, k_RigLocalScale));
            Assert.That(remapMatrix, Is.EqualTo(expectedRemapMatrix).Using(Float4x4Comparer));

            var expectedIndex0 = float4x4.TRS(k_RigLocalTranslation, k_RigLocalRotation, k_RigLocalScale);

            var target = m_Manager.GetComponentData<LocalToWorld>(targetEntity).Value;
            var expectedTarget = float4x4.TRS(k_TargetLocalTranslation, k_TargetLocalRotation, k_TargetLocalScale);
            Assert.That(target, Is.EqualTo(expectedTarget).Using(Float4x4Comparer));

            var value = Set.GetValueBlocking(output);
            var expectedValue = math.mul(math.inverse(math.mul(expectedRemapMatrix, expectedIndex0)), expectedTarget);
            Assert.That(value, Is.EqualTo(expectedValue).Using(Float4x4Comparer));
        }

        /// <summary>
        /// Here the rig entity is also the root entity, but with the added DisableRootTransformReadWriteTag tag.
        /// As such, the animation pipeline does not manage the root at all, and we store the rig entity
        /// LocalToWorld as the remap matrix.
        /// </summary>
        [Test]
        public void RemapTarget_RootDisabled()
        {
            var rigEntity = m_Manager.CreateEntity();
            SetupTransformComponents(rigEntity, k_RigLocalTranslation, k_RigLocalRotation, k_RigLocalScale, Entity.Null);
            SetupRigEntity(rigEntity, m_Rig, rigEntity);
            m_Manager.AddComponent<DisableRootTransformReadWriteTag>(rigEntity);

            var targetEntity = m_Manager.CreateEntity();
            SetupTransformComponents(targetEntity, k_TargetLocalTranslation, k_TargetLocalRotation, k_TargetLocalScale, Entity.Null);

            var w2rNode = CreateGraph(rigEntity, targetEntity, m_Rig, PreSet);
            var output = CreateGraphValue(w2rNode, WorldToRootNode.KernelPorts.Output, PreSet);

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            var remapMatrix = (float4x4)m_Manager.GetComponentData<RigRootEntity>(rigEntity).RemapToRootMatrix;
            var expectedRemapMatrix = float4x4.TRS(k_RigLocalTranslation, k_RigLocalRotation, k_RigLocalScale);
            Assert.That(remapMatrix, Is.EqualTo(expectedRemapMatrix).Using(Float4x4Comparer));

            var expectedIndex0 = float4x4.TRS(k_RigLocalTranslation, k_RigLocalRotation, k_RigLocalScale);

            var target = m_Manager.GetComponentData<LocalToWorld>(targetEntity).Value;
            var expectedTarget = float4x4.TRS(k_TargetLocalTranslation, k_TargetLocalRotation, k_TargetLocalScale);
            Assert.That(target, Is.EqualTo(expectedTarget).Using(Float4x4Comparer));

            var value = Set.GetValueBlocking(output);
            var expectedValue = math.mul(math.inverse(math.mul(expectedRemapMatrix, expectedIndex0)), expectedTarget);
            Assert.That(value, Is.EqualTo(expectedValue).Using(Float4x4Comparer));
        }

        /// <summary>
        /// This is a case where the rig and the root are not the same entity, but the AnimatedData
        /// contains the whole hierarchy (which in this test is Rig <- Offset <- Root).
        /// </summary>
        [Test]
        public void RemapTarget_WithOffset()
        {
            var rigEntity = m_Manager.CreateEntity();
            SetupTransformComponents(rigEntity, k_RigLocalTranslation, k_RigLocalRotation, k_RigLocalScale, Entity.Null);
            var offsetEntity = m_Manager.CreateEntity();
            SetupTransformComponents(offsetEntity, k_OffsetLocalTranslation, k_OffsetLocalRotation, k_OffsetLocalScale, rigEntity);
            var rootEntity = m_Manager.CreateEntity();
            SetupTransformComponents(rootEntity, k_RootLocalTranslation, k_RootLocalRotation, k_RootLocalScale, offsetEntity);

            SetupRigEntity(rigEntity, m_Rig, rootEntity);

            var targetEntity = m_Manager.CreateEntity();
            SetupTransformComponents(targetEntity, k_TargetLocalTranslation, k_TargetLocalRotation, k_TargetLocalScale, Entity.Null);

            var w2rNode = CreateGraph(rigEntity, targetEntity, m_Rig, PreSet);
            var output = CreateGraphValue(w2rNode, WorldToRootNode.KernelPorts.Output, PreSet);

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            var remapMatrix = (float4x4)m_Manager.GetComponentData<RigRootEntity>(rigEntity).RemapToRootMatrix;
            var expectedRemapMatrix =  m_Manager.GetComponentData<LocalToWorld>(rootEntity).Value;
            Assert.That(remapMatrix, Is.EqualTo(expectedRemapMatrix).Using(Float4x4Comparer));

            var expectedIndex0 = float4x4.TRS(k_RootLocalTranslation, k_RootLocalRotation, k_RootLocalScale);

            var target = m_Manager.GetComponentData<LocalToWorld>(targetEntity).Value;
            var expectedTarget = float4x4.TRS(k_TargetLocalTranslation, k_TargetLocalRotation, k_TargetLocalScale);
            Assert.That(target, Is.EqualTo(expectedTarget).Using(Float4x4Comparer));

            var value = Set.GetValueBlocking(output);
            var expectedValue = math.mul(math.inverse(math.mul(expectedRemapMatrix, expectedIndex0)), expectedTarget);
            Assert.That(value, Is.EqualTo(expectedValue).Using(Float4x4Comparer));
        }

        /// <summary>
        /// This is a case where the rig entity is different from the root entity, and the rig
        /// only contains the root bone. It does not have the rig bone nor the offset bone.
        /// </summary>
        [Test]
        public void RemapTarget_ReducedRig()
        {
            var rigEntity = m_Manager.CreateEntity();
            SetupTransformComponents(rigEntity, k_RigLocalTranslation, k_RigLocalRotation, k_RigLocalScale, Entity.Null);
            var offsetEntity = m_Manager.CreateEntity();
            SetupTransformComponents(offsetEntity, k_OffsetLocalTranslation, k_OffsetLocalRotation, k_OffsetLocalScale, rigEntity);
            var rootEntity = m_Manager.CreateEntity();
            SetupTransformComponents(rootEntity, k_RootLocalTranslation, k_RootLocalRotation, k_RootLocalScale, offsetEntity);

            SetupRigEntity(rigEntity, m_RootOffsetRig, rootEntity);

            var targetEntity = m_Manager.CreateEntity();
            SetupTransformComponents(targetEntity, k_TargetLocalTranslation, k_TargetLocalRotation, k_TargetLocalScale, Entity.Null);

            var w2rNode = CreateGraph(rigEntity, targetEntity, m_RootOffsetRig, PreSet);
            var output = CreateGraphValue(w2rNode, WorldToRootNode.KernelPorts.Output, PreSet);

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            var remapMatrix = (float4x4)m_Manager.GetComponentData<RigRootEntity>(rigEntity).RemapToRootMatrix;
            var expectedRemapMatrix =  m_Manager.GetComponentData<LocalToWorld>(rootEntity).Value;
            Assert.That(remapMatrix, Is.EqualTo(expectedRemapMatrix).Using(Float4x4Comparer));

            var expectedIndex0 = float4x4.TRS(k_RootLocalTranslation, k_RootLocalRotation, k_RootLocalScale);

            var target = m_Manager.GetComponentData<LocalToWorld>(targetEntity).Value;
            var expectedTarget = float4x4.TRS(k_TargetLocalTranslation, k_TargetLocalRotation, k_TargetLocalScale);
            Assert.That(target, Is.EqualTo(expectedTarget).Using(Float4x4Comparer));

            var value = Set.GetValueBlocking(output);
            var expectedValue = math.mul(math.inverse(math.mul(expectedRemapMatrix, expectedIndex0)), expectedTarget);
            Assert.That(value, Is.EqualTo(expectedValue).Using(Float4x4Comparer));
        }

        /// <summary>
        /// A test with an added root motion offset.
        /// </summary>
        [Test]
        public void RemapTarget_RootMotion()
        {
            var rigEntity = m_Manager.CreateEntity();
            SetupTransformComponents(rigEntity, k_RigLocalTranslation, k_RigLocalRotation, k_RigLocalScale, Entity.Null);

            SetupRigEntity(rigEntity, m_Rig, rigEntity);

            // Add animated root motion and offset components
            var rmOffset = new RigidTransform(quaternion.AxisAngle(math.float3(1f, 0f, 0f), math.radians(30f)), math.float3(2f, 3f, 4f));
            m_Manager.AddComponent<ProcessDefaultAnimationGraph.AnimatedRootMotion>(rigEntity);
            m_Manager.AddComponentData(rigEntity, new RootMotionOffset { Value = rmOffset });

            var targetEntity = m_Manager.CreateEntity();
            SetupTransformComponents(targetEntity, k_TargetLocalTranslation, k_TargetLocalRotation, k_TargetLocalScale, Entity.Null);

            var w2rNode = CreateGraph(rigEntity, targetEntity, m_Rig, PreSet);
            var output = CreateGraphValue(w2rNode, WorldToRootNode.KernelPorts.Output, PreSet);

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            var remapMatrix = (float4x4)m_Manager.GetComponentData<RigRootEntity>(rigEntity).RemapToRootMatrix;
            var expectedRemapMatrix = float4x4.TRS(k_RigLocalTranslation, k_RigLocalRotation, k_RigLocalScale);
            Assert.That(remapMatrix, Is.EqualTo(expectedRemapMatrix).Using(Float4x4Comparer));

            var expectedIndex0 = float4x4.TRS(k_RigLocalTranslation, k_RigLocalRotation, k_RigLocalScale);

            var target = m_Manager.GetComponentData<LocalToWorld>(targetEntity).Value;
            var expectedTarget = float4x4.TRS(k_TargetLocalTranslation, k_TargetLocalRotation, k_TargetLocalScale);
            Assert.That(target, Is.EqualTo(expectedTarget).Using(Float4x4Comparer));

            var value = Set.GetValueBlocking(output);
            var expectedValue = math.mul(math.inverse(math.mul(expectedRemapMatrix, expectedIndex0)), expectedTarget);
            Assert.That(value, Is.EqualTo(expectedValue).Using(Float4x4Comparer));

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            remapMatrix = m_Manager.GetComponentData<RigRootEntity>(rigEntity).RemapToRootMatrix;
            expectedRemapMatrix = math.mul(new float4x4(rmOffset), expectedRemapMatrix);
            Assert.That(remapMatrix, Is.EqualTo(expectedRemapMatrix).Using(Float4x4Comparer));

            expectedIndex0 = float4x4.identity;

            target = m_Manager.GetComponentData<LocalToWorld>(targetEntity).Value;
            expectedTarget = float4x4.TRS(k_TargetLocalTranslation, k_TargetLocalRotation, k_TargetLocalScale);
            Assert.That(target, Is.EqualTo(expectedTarget).Using(Float4x4Comparer));

            value = Set.GetValueBlocking(output);
            expectedValue = math.mul(math.inverse(math.mul(expectedRemapMatrix, expectedIndex0)), expectedTarget);
            Assert.That(value, Is.EqualTo(expectedValue).Using(Float4x4Comparer));
        }
    }
}
