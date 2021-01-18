using System.Collections.Generic;

using NUnit.Framework;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.Collections;
using Unity.Jobs;

namespace Unity.Animation.Tests
{
    public class ExposeTransformTests : AnimationTestsFixture
    {
        uint m_Version;

        BlobAssetReference<RigDefinition> CreateRigDefinition(int boneCount)
        {
            var skeleton = new SkeletonNode[boneCount];

            skeleton[0] = new SkeletonNode
            {
                Id = "Root", ParentIndex = -1,
                AxisIndex = -1,
                LocalTranslationDefaultValue = float3.zero,
                LocalRotationDefaultValue = quaternion.identity,
                LocalScaleDefaultValue = new float3(1)
            };

            // Create a chain of transforms.
            for (int i = 1; i < boneCount; i++)
            {
                skeleton[i] = new SkeletonNode
                {
                    Id = $"{i}",
                    ParentIndex = i - 1,
                    AxisIndex = -1,
                    LocalTranslationDefaultValue = float3.zero,
                    LocalRotationDefaultValue = quaternion.identity,
                    LocalScaleDefaultValue = new float3(1)
                };
            }

            return RigBuilder.CreateRigDefinition(skeleton);
        }

        void SetupEntityTransformComponent(Entity entity)
        {
            m_Manager.AddComponentData(entity, new LocalToWorld { Value = float4x4.identity });
            m_Manager.AddComponentData(entity, new Translation { Value = float3.zero });
            m_Manager.AddComponentData(entity, new Rotation { Value = quaternion.identity });
            m_Manager.AddComponentData(entity, new NonUniformScale { Value = new float3(1) });
        }

        List<Entity> CreateRigTransforms(BlobAssetReference<RigDefinition> rig)
        {
            var list = new List<Entity>(rig.Value.Skeleton.BoneCount);

            for (int i = 0; i < rig.Value.Skeleton.BoneCount; i++)
            {
                list.Add(m_Manager.CreateEntity());

                m_Manager.AddComponentData(list[i], new LocalToWorld { Value = i == 0 ? float4x4.identity : float4x4.TRS(math.float3(i), quaternion.identity, math.float3(1)) });
                m_Manager.AddComponentData(list[i], new Translation { Value = i == 0 ? float3.zero : new float3(1) });
                m_Manager.AddComponentData(list[i], new Rotation { Value = quaternion.identity });
                m_Manager.AddComponentData(list[i], new NonUniformScale { Value = new float3(1) });

                if (rig.Value.Skeleton.ParentIndexes[i] != -1)
                {
                    var parent = list[rig.Value.Skeleton.ParentIndexes[i]];
                    var l2p = float4x4.TRS(math.float3(1), quaternion.identity, math.float3(1));
                    m_Manager.AddComponentData(list[i], new Parent { Value = parent });
                    m_Manager.AddComponentData(list[i], new LocalToParent { Value = l2p });
                }
            }
            return list;
        }

        List<Entity> CreateRigTransformsWithScale(BlobAssetReference<RigDefinition> rig)
        {
            var list = new List<Entity>(rig.Value.Skeleton.BoneCount);

            // Root
            var root = m_Manager.CreateEntity();
            SetupEntityTransformComponent(root);
            list.Add(root);

            for (int i = 1; i < rig.Value.Skeleton.BoneCount; i++)
            {
                list.Add(m_Manager.CreateEntity());

                var translation = new float3(1);
                var rotation = quaternion.identity;
                var scale = 2.0f;
                m_Manager.AddComponentData(list[i], new Translation { Value = translation});
                m_Manager.AddComponentData(list[i], new Rotation { Value = rotation });
                m_Manager.AddComponentData(list[i], new NonUniformScale { Value = scale });

                var parent = list[rig.Value.Skeleton.ParentIndexes[i]];
                var l2p = float4x4.TRS(translation, rotation, scale);

                m_Manager.AddComponentData(list[i], new Parent { Value = parent });
                m_Manager.AddComponentData(list[i], new LocalToParent { Value = l2p });

                // Translation is the geometric serie (sum of the geometric progression terms) with common ratio = 2.
                // Let a be the first term, r the common ration, the progression is defined as a_i = a * r ^ (i-1)
                // The sum of n components can be calculated with: a * (1 - r ^ n)/(1 - r)
                // Here a = 1 (scale of the root) and r = 2 (scale of the children) so it can be simplified to 2^n - 1.
                var l2w = float4x4.TRS(math.pow(2, i) - 1, quaternion.identity, math.pow(2, i));
                m_Manager.AddComponentData(list[i], new LocalToWorld { Value = l2w });
            }
            return list;
        }

        void ValidateAnimationStream(ref AnimationStream stream, ref AnimationStream expectedValue)
        {
            Assert.That(stream.Rig.Value.Skeleton.BoneCount, Is.EqualTo(expectedValue.Rig.Value.Skeleton.BoneCount));

            for (int i = 1; i < stream.Rig.Value.Skeleton.BoneCount; i++)
            {
                // No need to use a specialized comparator here
                // All value are integer which are always exact
                Assert.That(stream.GetLocalToParentTranslation(i), Is.EqualTo(expectedValue.GetLocalToParentTranslation(i)).Using(TranslationComparer), $"Translation mismatch for transform '{i}'");
                Assert.That(stream.GetLocalToParentRotation(i), Is.EqualTo(expectedValue.GetLocalToParentRotation(i)).Using(RotationComparer),  $"Rotation mismatch for transform '{i}'");
                Assert.That(stream.GetLocalToParentScale(i), Is.EqualTo(expectedValue.GetLocalToParentScale(i)).Using(ScaleComparer), $"Scale mismatch for transform '{i}'");
            }
        }

        internal class TestNode
            : SimulationKernelNodeDefinition<TestNode.SimPorts, TestNode.KernelDefs>
            , IRigContextHandler<TestNode.Data>
        {
#pragma warning disable 0649
            public struct SimPorts : ISimulationPortDefinition
            {
                public MessageInput<TestNode, Rig> Rig;
            }

            public struct KernelDefs : IKernelPortDefinition
            {
                public DataInput<TestNode, Buffer<AnimatedData>>  Input;
                public DataOutput<TestNode, Buffer<AnimatedData>> Output;
            }
#pragma warning restore 0649

            struct Data : INodeData, IMsgHandler<Rig>
            {
                public void HandleMessage(MessageContext ctx, in Rig rig)
                {
                    ctx.UpdateKernelData(new KernelData
                    {
                        RigDefinition = rig
                    });

                    ctx.Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0));
                }
            }

            struct KernelData : IKernelData
            {
                public BlobAssetReference<RigDefinition> RigDefinition;
            }

            [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
            struct Kernel : IGraphKernel<KernelData, KernelDefs>
            {
                public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
                {
                    var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                    if (outputStream.IsNull)
                        throw new System.InvalidOperationException($"TestNode Output is invalid.");

                    var inputStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));

                    outputStream.CopyFrom(ref inputStream);
                }
            }

            InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
                (InputPortID)SimulationPorts.Rig;
        }

        readonly static int BoneCount = 10;

        NativeArray<AnimatedData> m_ExpectedStreamBufferAll;
        NativeArray<AnimatedData> m_ExpectedStreamBufferHalf;
        NativeArray<AnimatedData> m_ExpectedStreamBufferDefault;
        AnimationStream m_ExpectedStreamAll;
        AnimationStream m_ExpectedStreamHalf;
        AnimationStream m_ExpectedStreamDefault;
        BlobAssetReference<RigDefinition> m_Rig;

        [OneTimeSetUp]
        override protected void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            m_Rig = CreateRigDefinition(BoneCount);
            m_ExpectedStreamBufferAll = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Persistent);
            m_ExpectedStreamBufferHalf = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Persistent);
            m_ExpectedStreamBufferDefault = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Persistent);

            var stream = AnimationStream.Create(m_Rig, m_ExpectedStreamBufferAll);
            stream.ResetToDefaultValues();
            for (int i = 1; i < stream.Rig.Value.Skeleton.BoneCount; i++)
            {
                var tmp = float4x4.TRS(new float3(i), quaternion.identity, new float3(1));
                stream.SetLocalToRootTR(i, tmp.c3.xyz, math.quaternion(tmp));
            }

            stream = AnimationStream.Create(m_Rig, m_ExpectedStreamBufferHalf);
            stream.ResetToDefaultValues();
            for (int i = 1; i < stream.Rig.Value.Skeleton.BoneCount; i++)
            {
                if (i % 2 == 0)
                {
                    var tmp = float4x4.TRS(new float3(i), quaternion.identity, new float3(1));
                    stream.SetLocalToRootTR(i, tmp.c3.xyz, math.quaternion(tmp));
                }
                else
                {
                    stream.SetLocalToParentTranslation(i, float3.zero);
                    stream.SetLocalToParentRotation(i, quaternion.identity);
                    stream.SetLocalToParentScale(i, new float3(1));
                }
            }

            stream = AnimationStream.Create(m_Rig, m_ExpectedStreamBufferDefault);
            stream.ResetToDefaultValues();

            m_ExpectedStreamAll = AnimationStream.CreateReadOnly(m_Rig, m_ExpectedStreamBufferAll);
            m_ExpectedStreamHalf = AnimationStream.CreateReadOnly(m_Rig, m_ExpectedStreamBufferHalf);
            m_ExpectedStreamDefault = AnimationStream.CreateReadOnly(m_Rig, m_ExpectedStreamBufferDefault);
        }

        [OneTimeTearDown]
        override protected void OneTimeTearDown()
        {
            m_Rig.Dispose();
            m_ExpectedStreamBufferAll.Dispose();
            m_ExpectedStreamBufferHalf.Dispose();
            m_ExpectedStreamBufferDefault.Dispose();

            base.OneTimeTearDown();
        }

        [Test]
        public void CanReadTRSFromAllEntitiesAndWriteToPreAnimationStream()
        {
            var rig = m_Rig;
            var entityTransforms = CreateRigTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);
            SetupEntityTransformComponent(rigEntity);

            for (int i = 0; i < entityTransforms.Count; i++)
            {
                RigEntityBuilder.AddReadTransformHandle<ProcessDefaultAnimationGraph.ReadTransformHandle>(
                    m_Manager, rigEntity, entityTransforms[i], i
                );
            }

            var entityNode = CreateComponentNode(rigEntity, PreSet);
            var testNode = CreateNode<TestNode>(PreSet);

            PreSet.SendMessage(testNode, TestNode.SimulationPorts.Rig, new Rig { Value = rig });
            PreSet.Connect(entityNode, ComponentNode.Output<AnimatedData>(), testNode, TestNode.KernelPorts.Input);

            var graphBuffer = CreateGraphValue(testNode, TestNode.KernelPorts.Output, PreSet);

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            // Validate that the ProcessDefaultAnimationGraph system copy the transform value into the stream
            var ecsStream = AnimationStream.Create(
                rig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamAll);

            // Validate that the system copy the ecs stream into dfg
            var dfgStream = AnimationStream.CreateReadOnly(rig, DFGUtils.GetGraphValueTempNativeBuffer(PreSet, graphBuffer));
            ValidateAnimationStream(ref dfgStream, ref m_ExpectedStreamAll);

            // Reset the ecs stream to default values
            ecsStream = AnimationStream.Create(
                rig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );
            ecsStream.ResetToDefaultValues();
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamDefault);

            // Validate that the ProcessLateAnimationGraph system doesn't copy the transform value into the stream since we are targeting only ProcessDefaultAnimationGraph
            World.GetOrCreateSystem<EndFrameParentSystem>().Update();
            World.GetOrCreateSystem<EndFrameLocalToParentSystem>().Update();
            m_PostAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            ecsStream = AnimationStream.Create(
                rig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamDefault);
        }

        [Test]
        public void CanReadTRSFromHalfEntitiesAndWriteToPreAnimationStream()
        {
            var rig = m_Rig;
            var entityTransforms = CreateRigTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);
            SetupEntityTransformComponent(rigEntity);

            for (int i = 1; i < entityTransforms.Count; i++)
            {
                if (i % 2 == 0)
                {
                    RigEntityBuilder.AddReadTransformHandle<ProcessDefaultAnimationGraph.ReadTransformHandle>(
                        m_Manager, rigEntity, entityTransforms[i], i
                    );
                }
            }

            var entityNode = CreateComponentNode(rigEntity, PreSet);
            var testNode = CreateNode<TestNode>(PreSet);

            PreSet.SendMessage(testNode, TestNode.SimulationPorts.Rig, new Rig { Value = rig });
            PreSet.Connect(entityNode, ComponentNode.Output<AnimatedData>(), testNode, TestNode.KernelPorts.Input);

            var graphBuffer = CreateGraphValue(testNode, TestNode.KernelPorts.Output, PreSet);

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            // Validate that the ProcessDefaultAnimationGraph system copy the transform value into the stream
            var ecsStream = AnimationStream.Create(
                rig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamHalf);

            // Validate that the system copy the ecs stream into dfg
            var dfgStream = AnimationStream.CreateReadOnly(rig, DFGUtils.GetGraphValueTempNativeBuffer(PreSet, graphBuffer));
            ValidateAnimationStream(ref dfgStream, ref m_ExpectedStreamHalf);

            // Reset the ecs stream to default values
            ecsStream = AnimationStream.Create(
                rig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );
            ecsStream.ResetToDefaultValues();
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamDefault);

            // Validate that the ProcessLateAnimationGraph system doesn't copy the transform value into the stream since we are targeting only ProcessDefaultAnimationGraph
            World.GetOrCreateSystem<EndFrameParentSystem>().Update();
            World.GetOrCreateSystem<EndFrameLocalToParentSystem>().Update();
            m_PostAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            ecsStream = AnimationStream.Create(
                rig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamDefault);
        }

        [Test]
        public void CanReadTRSFromAllEntitiesAndWriteToPostAnimationStream()
        {
            var rig = m_Rig;
            var entityTransforms = CreateRigTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);
            SetupEntityTransformComponent(rigEntity);

            for (int i = 0; i < entityTransforms.Count; i++)
            {
                RigEntityBuilder.AddReadTransformHandle<ProcessLateAnimationGraph.ReadTransformHandle>(
                    m_Manager, rigEntity, entityTransforms[i], i
                );
            }

            var entityNode = CreateComponentNode(rigEntity, PostSet);
            var testNode = CreateNode<TestNode>(PostSet);

            PostSet.SendMessage(testNode, TestNode.SimulationPorts.Rig, new Rig { Value = rig });
            PostSet.Connect(entityNode, testNode, TestNode.KernelPorts.Input);

            var graphBuffer = CreateGraphValue(testNode, TestNode.KernelPorts.Output, PostSet);

            // Validate that the ProcessDefaultAnimationGraph system doesn't copy the transform value into the stream
            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            var ecsStream = AnimationStream.Create(
                rig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamDefault);

            // Validate that the ProcessLateAnimationGraph system copy the transform value into the stream since we are targeting only ProcessLateAnimationGraphTag
            World.GetOrCreateSystem<EndFrameParentSystem>().Update();
            World.GetOrCreateSystem<EndFrameLocalToParentSystem>().Update();
            m_PostAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            ecsStream = AnimationStream.Create(
                rig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamAll);

            // Validate that the system copy the ecs stream into dfg
            var dfgStream = AnimationStream.CreateReadOnly(rig, DFGUtils.GetGraphValueTempNativeBuffer(PostSet, graphBuffer));
            ValidateAnimationStream(ref dfgStream, ref m_ExpectedStreamAll);
        }

        [Test]
        public void CanReadTRSFromHalfEntitiesAndWriteToPostAnimationStream()
        {
            var rig = m_Rig;
            var entityTransforms = CreateRigTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);
            SetupEntityTransformComponent(rigEntity);

            for (int i = 1; i < entityTransforms.Count; i++)
            {
                if (i % 2 == 0)
                {
                    RigEntityBuilder.AddReadTransformHandle<ProcessLateAnimationGraph.ReadTransformHandle>(
                        m_Manager, rigEntity, entityTransforms[i], i
                    );
                }
            }

            var entityNode = CreateComponentNode(rigEntity, PostSet);
            var testNode = CreateNode<TestNode>(PostSet);

            PostSet.SendMessage(testNode, TestNode.SimulationPorts.Rig, new Rig { Value = rig });
            PostSet.Connect(entityNode, ComponentNode.Output<AnimatedData>(), testNode, TestNode.KernelPorts.Input);

            var graphBuffer = CreateGraphValue(testNode, TestNode.KernelPorts.Output, PostSet);

            // Validate that the ProcessDefaultAnimationGraph system doesn't copy the transform value into the stream
            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            var ecsStream = AnimationStream.Create(
                rig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamDefault);

            // Validate that the ProcessLateAnimationGraph system copy the transform value into the stream since we are targeting only ProcessDefaultAnimationGraph
            World.GetOrCreateSystem<EndFrameParentSystem>().Update();
            World.GetOrCreateSystem<EndFrameLocalToParentSystem>().Update();
            m_PostAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            ecsStream = AnimationStream.Create(
                rig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamHalf);

            // Validate that the system copy the ecs stream into dfg
            var dfgStream = AnimationStream.CreateReadOnly(rig, DFGUtils.GetGraphValueTempNativeBuffer(PostSet, graphBuffer));
            ValidateAnimationStream(ref dfgStream, ref m_ExpectedStreamHalf);
        }

        [Test]
        public void CanReadTRSFromAllEntitiesAndWriteToPreAndPostAnimationStream()
        {
            var rig = m_Rig;
            var entityTransforms = CreateRigTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);
            SetupEntityTransformComponent(rigEntity);

            for (int i = 0; i < entityTransforms.Count; i++)
            {
                RigEntityBuilder.AddReadTransformHandle<ProcessDefaultAnimationGraph.ReadTransformHandle>(
                    m_Manager, rigEntity, entityTransforms[i], i
                );
                RigEntityBuilder.AddReadTransformHandle<ProcessLateAnimationGraph.ReadTransformHandle>(
                    m_Manager, rigEntity, entityTransforms[i], i
                );
            }

            var preEntityNode = CreateComponentNode(rigEntity, PreSet);
            var preTestNode = CreateNode<TestNode>(PreSet);

            PreSet.SendMessage(preTestNode, TestNode.SimulationPorts.Rig, new Rig { Value = rig });
            PreSet.Connect(preEntityNode, ComponentNode.Output<AnimatedData>(), preTestNode, TestNode.KernelPorts.Input);

            var preGraphBuffer = CreateGraphValue(preTestNode, TestNode.KernelPorts.Output, PreSet);

            var postEntityNode = CreateComponentNode(rigEntity, PostSet);
            var postTestNode = CreateNode<TestNode>(PostSet);

            PostSet.SendMessage(postTestNode, TestNode.SimulationPorts.Rig, new Rig { Value = rig });
            PostSet.Connect(postEntityNode, ComponentNode.Output<AnimatedData>(), postTestNode, TestNode.KernelPorts.Input);

            var postGraphBuffer = CreateGraphValue(postTestNode, TestNode.KernelPorts.Output, PostSet);

            // Validate that the ProcessDefaultAnimationGraph system copy the transform value into the stream
            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            var ecsStream = AnimationStream.Create(
                rig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamAll);

            // Validate that the system copy the ecs stream into dfg
            var dfgStream = AnimationStream.CreateReadOnly(rig, DFGUtils.GetGraphValueTempNativeBuffer(PreSet, preGraphBuffer));
            ValidateAnimationStream(ref dfgStream, ref m_ExpectedStreamAll);

            // Reset the ecs stream to default values
            ecsStream = AnimationStream.Create(
                rig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );
            ecsStream.ResetToDefaultValues();
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamDefault);

            // Validate that the ProcessLateAnimationGraph system copy the transform value into the stream
            World.GetOrCreateSystem<EndFrameParentSystem>().Update();
            World.GetOrCreateSystem<EndFrameLocalToParentSystem>().Update();
            m_PostAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            ecsStream = AnimationStream.Create(
                rig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
            );
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamAll);

            // Validate that the system copy the ecs stream into dfg
            dfgStream = AnimationStream.CreateReadOnly(rig, DFGUtils.GetGraphValueTempNativeBuffer(PostSet, postGraphBuffer));
            ValidateAnimationStream(ref dfgStream, ref m_ExpectedStreamAll);
        }

        [Test]
        public void CanReadFromAnimationStreamAndWriteLocalToWorldToAllEntitiesInPostAnim()
        {
            var rig = m_Rig;
            var entityTransforms = CreateRigTransformsWithScale(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);
            SetupEntityTransformComponent(rigEntity);

            for (int i = 1; i < entityTransforms.Count; i++)
            {
                RigEntityBuilder.AddWriteTransformHandle<ProcessLateAnimationGraph.WriteTransformHandle>(
                    m_Manager, rigEntity, entityTransforms[i], i
                );
            }

            for (int i = 1; i < entityTransforms.Count; i++)
            {
                var localToWorld = m_Manager.GetComponentData<LocalToWorld>(entityTransforms[i]);
                Assert.That(localToWorld.Value, Is.EqualTo(float4x4.TRS(math.pow(2, i) - 1, quaternion.identity, math.pow(2, i))));
            }

            World.GetOrCreateSystem<ProcessDefaultAnimationGraph>().Update();
            World.GetOrCreateSystem<EndFrameParentSystem>().Update();
            World.GetOrCreateSystem<EndFrameLocalToParentSystem>().Update();
            World.GetOrCreateSystem<ProcessLateAnimationGraph>().Update();
            m_Manager.CompleteAllJobs();

            // The animation stream start with default values, so after writing into entities they all should have the identity matrix
            for (int i = 1; i < entityTransforms.Count; i++)
            {
                var localToWorld = m_Manager.GetComponentData<LocalToWorld>(entityTransforms[i]);
                Assert.That(localToWorld.Value, Is.EqualTo(float4x4.identity));
            }
        }

        [Test]
        public void CanReadFromAnimationStreamAndWriteLocalToWorldToHalfEntitiesInPostAnim()
        {
            var rig = m_Rig;
            var entityTransforms = CreateRigTransformsWithScale(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);
            SetupEntityTransformComponent(rigEntity);

            for (int i = 1; i < entityTransforms.Count; i++)
            {
                if (i % 2 == 0)
                {
                    RigEntityBuilder.AddWriteTransformHandle<ProcessLateAnimationGraph.WriteTransformHandle>(
                        m_Manager, rigEntity, entityTransforms[i], i
                    );
                }
            }

            for (int i = 1; i < entityTransforms.Count; i++)
            {
                var localToWorld = m_Manager.GetComponentData<LocalToWorld>(entityTransforms[i]);
                Assert.That(localToWorld.Value, Is.EqualTo(float4x4.TRS(math.pow(2, i) - 1, quaternion.identity, math.pow(2, i))));
            }

            World.GetOrCreateSystem<ProcessDefaultAnimationGraph>().Update();
            World.GetOrCreateSystem<EndFrameParentSystem>().Update();
            World.GetOrCreateSystem<EndFrameLocalToParentSystem>().Update();
            World.GetOrCreateSystem<ProcessLateAnimationGraph>().Update();
            m_Manager.CompleteAllJobs();

            // The animation stream start with default values, so after writing into entities they all should have the identity matrix
            for (int i = 1; i < entityTransforms.Count; i++)
            {
                var localToWorld = m_Manager.GetComponentData<LocalToWorld>(entityTransforms[i]);
                if (i % 2 == 0)
                {
                    Assert.That(localToWorld.Value, Is.EqualTo(float4x4.identity));
                }
                else
                {
                    Assert.That(localToWorld.Value, Is.EqualTo(float4x4.TRS(math.pow(2, i) - 1, quaternion.identity, math.pow(2, i))));
                }
            }
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(9)]
        public void CanReadFromAnimationStreamAndWriteLocalToWorldToEntityInPreAnim(int index)
        {
            var rig = m_Rig;
            var entityTransforms = CreateRigTransformsWithScale(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);
            SetupEntityTransformComponent(rigEntity);

            // Writing to a transform in the hierarchy should update the children.
            RigEntityBuilder.AddWriteTransformHandle<ProcessDefaultAnimationGraph.WriteTransformHandle>(
                m_Manager, rigEntity, entityTransforms[index], index
            );

            for (int i = 1; i < entityTransforms.Count; i++)
            {
                var localToWorld = m_Manager.GetComponentData<LocalToWorld>(entityTransforms[i]);
                Assert.That(localToWorld.Value, Is.EqualTo(float4x4.TRS(math.pow(2, i) - 1, quaternion.identity, math.pow(2, i))));
            }

            World.GetOrCreateSystem<ProcessDefaultAnimationGraph>().Update();
            m_Manager.CompleteAllJobs();

            // After the pre-anim pass, only the entity that was written to has changed value.
            // Its value is the identity because the animation stream starts with default values.
            for (int i = 1; i < entityTransforms.Count; i++)
            {
                var localToWorld = m_Manager.GetComponentData<LocalToWorld>(entityTransforms[i]);

                if (i == index)
                {
                    Assert.That(localToWorld.Value, Is.EqualTo(float4x4.identity));
                }
                else
                {
                    Assert.That(localToWorld.Value, Is.EqualTo(float4x4.TRS(math.pow(2, i) - 1, quaternion.identity, math.pow(2, i))));
                }
            }

            World.GetOrCreateSystem<EndFrameParentSystem>().Update();
            World.GetOrCreateSystem<EndFrameLocalToParentSystem>().Update();
            World.GetOrCreateSystem<ProcessLateAnimationGraph>().Update();
            m_Manager.CompleteAllJobs();

            // The transform systems have updated the local to world of the children entities of the entity that was written to.
            for (int i = 1; i < entityTransforms.Count; i++)
            {
                var localToWorld = m_Manager.GetComponentData<LocalToWorld>(entityTransforms[i]);

                if (i < index)
                {
                    Assert.That(localToWorld.Value, Is.EqualTo(float4x4.TRS(math.pow(2, i) - 1, quaternion.identity, math.pow(2, i))));
                }
                else
                {
                    // Because we write the identity to the exposed entity, but the value of the local to parent of the
                    // children don't change, we compute the "accumulation" for translation and scale from the index
                    // of the exposed entity.
                    Assert.That(localToWorld.Value, Is.EqualTo(float4x4.TRS(math.pow(2, i - index) - 1, quaternion.identity, math.pow(2, i - index))));
                }
            }
        }

        [TestCase(1, 1)]
        [TestCase(5, 5)]
        [TestCase(9, 9)]
        [TestCase(1, 9)]
        [TestCase(9, 1)]
        [TestCase(1, 5)]
        [TestCase(5, 1)]
        [TestCase(5, 9)]
        [TestCase(9, 5)]
        public void CanReadFromAnimationStreamAndWriteLocalToWorldToEntityInPreAndPostAnim(int preIndex, int postIndex)
        {
            var rig = m_Rig;
            var entityTransforms = CreateRigTransformsWithScale(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);
            SetupEntityTransformComponent(rigEntity);

            RigEntityBuilder.AddWriteTransformHandle<ProcessDefaultAnimationGraph.WriteTransformHandle>(
                m_Manager, rigEntity, entityTransforms[preIndex], preIndex
            );
            RigEntityBuilder.AddWriteTransformHandle<ProcessLateAnimationGraph.WriteTransformHandle>(
                m_Manager, rigEntity, entityTransforms[postIndex], postIndex
            );

            for (int i = 1; i < entityTransforms.Count; i++)
            {
                var localToWorld = m_Manager.GetComponentData<LocalToWorld>(entityTransforms[i]);
                Assert.That(localToWorld.Value, Is.EqualTo(float4x4.TRS(math.pow(2, i) - 1, quaternion.identity, math.pow(2, i))));
            }

            World.GetOrCreateSystem<ProcessDefaultAnimationGraph>().Update();
            m_Manager.CompleteAllJobs();

            // After the pre-anim pass, only the entity that was written to has changed value.
            // Its value is the identity because the animation stream starts with default values.
            for (int i = 1; i < entityTransforms.Count; i++)
            {
                var localToWorld = m_Manager.GetComponentData<LocalToWorld>(entityTransforms[i]);

                if (i == preIndex)
                {
                    Assert.That(localToWorld.Value, Is.EqualTo(float4x4.identity));
                }
                else
                {
                    Assert.That(localToWorld.Value, Is.EqualTo(float4x4.TRS(math.pow(2, i) - 1, quaternion.identity, math.pow(2, i))));
                }
            }

            World.GetOrCreateSystem<EndFrameParentSystem>().Update();
            World.GetOrCreateSystem<EndFrameLocalToParentSystem>().Update();
            m_Manager.CompleteAllJobs();

            // The transform systems have updated the local to world of the children entities of the entity that was written to.
            // Even the transform that has the post anim write handle should be updated.
            for (int i = 1; i < entityTransforms.Count; i++)
            {
                var localToWorld = m_Manager.GetComponentData<LocalToWorld>(entityTransforms[i]);

                if (i == preIndex)
                {
                    Assert.That(localToWorld.Value, Is.EqualTo(float4x4.identity));
                }
                else if (i < preIndex || (i >= postIndex && postIndex > preIndex))
                {
                    // Post write handle prevents the transform to be updated, should have the same value as before.
                    Assert.That(localToWorld.Value, Is.EqualTo(float4x4.TRS(math.pow(2, i) - 1, quaternion.identity, math.pow(2, i))));
                }
                else
                {
                    // Because we write the identity to the exposed entity, but the value of the local to parent of the
                    // children don't change, we compute the "accumulation" for translation and scale from the index
                    // of the exposed entity.
                    Assert.That(localToWorld.Value, Is.EqualTo(float4x4.TRS(math.pow(2, i - preIndex) - 1, quaternion.identity, math.pow(2, i - preIndex))));
                }
            }

            World.GetOrCreateSystem<ProcessLateAnimationGraph>().Update();
            m_Manager.CompleteAllJobs();

            // The transform systems have updated the local to world of the children entities of the entity that was written to.
            for (int i = 1; i < entityTransforms.Count; i++)
            {
                var localToWorld = m_Manager.GetComponentData<LocalToWorld>(entityTransforms[i]);

                if (i == preIndex || i == postIndex)
                {
                    Assert.That(localToWorld.Value, Is.EqualTo(float4x4.identity));
                }
                else if (i < preIndex || (i > postIndex && postIndex > preIndex))
                {
                    Assert.That(localToWorld.Value, Is.EqualTo(float4x4.TRS(math.pow(2, i) - 1, quaternion.identity, math.pow(2, i))));
                }
                else
                {
                    Assert.That(localToWorld.Value, Is.EqualTo(float4x4.TRS(math.pow(2, i - preIndex) - 1, quaternion.identity, math.pow(2, i - preIndex))));
                }
            }
        }

        [Test]
        public void CanDeleteRigEntity()
        {
            var rig = m_Rig;
            var entityTransforms = CreateRigTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);
            SetupEntityTransformComponent(rigEntity);

            for (int i = 1; i < entityTransforms.Count; i++)
            {
                RigEntityBuilder.AddReadTransformHandle<ProcessDefaultAnimationGraph.ReadTransformHandle>(
                    m_Manager, rigEntity, entityTransforms[i], i
                );

                RigEntityBuilder.AddWriteTransformHandle<ProcessLateAnimationGraph.WriteTransformHandle>(
                    m_Manager, rigEntity, entityTransforms[i], i
                );
            }

            World.GetOrCreateSystem<ProcessDefaultAnimationGraph>().Update();
            World.GetOrCreateSystem<ProcessLateAnimationGraph>().Update();

            m_Manager.DestroyEntity(rigEntity);

            World.GetOrCreateSystem<ProcessDefaultAnimationGraph>().Update();
            World.GetOrCreateSystem<ProcessLateAnimationGraph>().Update();

            Assert.Pass();
        }

        [Test]
        public void CanAddReadTransformHandleUnsorted()
        {
            var rig = m_Rig;
            var entityTransforms = CreateRigTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);
            SetupEntityTransformComponent(rigEntity);

            for (int i = entityTransforms.Count - 1; i > 0; i--)
            {
                RigEntityBuilder.AddReadTransformHandle<ProcessDefaultAnimationGraph.ReadTransformHandle>(
                    m_Manager, rigEntity, entityTransforms[i], i
                );
            }

            var entityNode = CreateComponentNode(rigEntity, PreSet);
            var testNode = CreateNode<TestNode>(PreSet);

            PreSet.SendMessage(testNode, TestNode.SimulationPorts.Rig, new Rig { Value = rig });
            PreSet.Connect(entityNode, ComponentNode.Output<AnimatedData>(), testNode, TestNode.KernelPorts.Input);

            var graphBuffer = CreateGraphValue(testNode, TestNode.KernelPorts.Output, PreSet);

            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            // Validate that Read Transform handle are sorted
            var readTransformHandles = m_Manager.GetBuffer<ProcessDefaultAnimationGraph.ReadTransformHandle>(rigEntity);
            Assert.That(readTransformHandles.Length, Is.EqualTo(entityTransforms.Count - 1));
            for (int i = 1; i < readTransformHandles.Length; i++)
            {
                Assert.That(readTransformHandles[i - 1].Index, Is.LessThan(readTransformHandles[i].Index));
            }
        }

        [DisableAutoCreation]
        [UpdateBefore(typeof(DefaultAnimationSystemGroup))]
        internal class MockAddEntityTransform : JobComponentSystem
        {
            internal Entity Rig;
            internal List<Entity> EntityTransforms;
            internal int Iterator;

            protected override JobHandle OnUpdate(JobHandle inputDeps)
            {
                if (Iterator == 0)
                    return inputDeps;

                RigEntityBuilder.AddReadTransformHandle<ProcessDefaultAnimationGraph.ReadTransformHandle>(
                    EntityManager, Rig, EntityTransforms[Iterator], Iterator
                );
                --Iterator;

                return inputDeps;
            }
        }

        [Test]
        public void AddingNewReadTransformHandleShouldSort()
        {
            var rig = m_Rig;
            var entityTransforms = CreateRigTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);
            SetupEntityTransformComponent(rigEntity);

            var entityNode = CreateComponentNode(rigEntity, PreSet);
            var testNode = CreateNode<TestNode>(PreSet);

            PreSet.SendMessage(testNode, TestNode.SimulationPorts.Rig, new Rig { Value = rig });
            PreSet.Connect(entityNode, ComponentNode.Output<AnimatedData>(), testNode, TestNode.KernelPorts.Input);

            var graphBuffer = CreateGraphValue(testNode, TestNode.KernelPorts.Output, PreSet);

            var addTransformSystem = World.GetOrCreateSystem<MockAddEntityTransform>();
            addTransformSystem.Rig = rigEntity;
            addTransformSystem.EntityTransforms = entityTransforms;
            addTransformSystem.Iterator = entityTransforms.Count - 1;

            for (int i = 0; i < entityTransforms.Count; i++)
            {
                addTransformSystem.Update();
                m_PreAnimationGraph.Update();
                m_Manager.CompleteAllJobs();

                // Validate that Read Transform handle are sorted
                var readTransformHandles = m_Manager.GetBuffer<ProcessDefaultAnimationGraph.ReadTransformHandle>(rigEntity);
                for (int j = 1; j < readTransformHandles.Length; j++)
                {
                    Assert.That(readTransformHandles[j - 1].Index, Is.LessThan(readTransformHandles[j].Index), $"On Update '{i}': ReadTransformHandle are not sorted");
                }
            }

            World.DestroySystem(addTransformSystem);
        }

        [Test]
        public void CannotAddMoreThanOneReadTransformHandleThatTargetTheSameRigTransfrom()
        {
            var rig = m_Rig;
            var entityTransforms = CreateRigTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);
            SetupEntityTransformComponent(rigEntity);

            for (int i = 1; i < entityTransforms.Count; i++)
            {
                if (i % 2 == 0)
                {
                    RigEntityBuilder.AddReadTransformHandle<ProcessDefaultAnimationGraph.ReadTransformHandle>(
                        m_Manager, rigEntity, entityTransforms[i], i
                    );
                }
                else
                {
                    // Explicitly create multiple ReadTransformHandle that target the same index
                    RigEntityBuilder.AddReadTransformHandle<ProcessDefaultAnimationGraph.ReadTransformHandle>(
                        m_Manager, rigEntity, entityTransforms[i], i - 1
                    );
                }
            }

            var readTransformHandles = m_Manager.GetBuffer<ProcessDefaultAnimationGraph.ReadTransformHandle>(rigEntity).AsNativeArray();
            readTransformHandles.Sort(new RigEntityBuilder.TransformHandleComparer<ProcessDefaultAnimationGraph.ReadTransformHandle>());
            var end = readTransformHandles.Unique(new RigEntityBuilder.TransformHandleComparer<ProcessDefaultAnimationGraph.ReadTransformHandle>());

            Assert.That(end, Is.LessThan(readTransformHandles.Length));
            Assert.That(end, Is.EqualTo(5));
        }
    }
}
