using System.Collections.Generic;

using NUnit.Framework;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
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

            skeleton[0] = new SkeletonNode { Id = "Root", ParentIndex = -1, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1) };
            for(int i=1;i<boneCount;i++)
            {
                skeleton[i] = new SkeletonNode { Id = $"{i}", ParentIndex = i - 1, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1) };
            };

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

            for(int i=0;i < rig.Value.Skeleton.BoneCount; i++)
            {
                list.Add(m_Manager.CreateEntity());

                m_Manager.AddComponentData(list[i], new LocalToWorld { Value = i == 0 ? float4x4.identity : float4x4.TRS(math.float3(i), quaternion.identity, math.float3(1)) });
                m_Manager.AddComponentData(list[i], new Translation { Value = i == 0 ? float3.zero : new float3(i) });
                m_Manager.AddComponentData(list[i], new Rotation { Value = quaternion.identity });
                m_Manager.AddComponentData(list[i], new NonUniformScale { Value = new float3(1) });

                if(rig.Value.Skeleton.ParentIndexes[i] != -1)
                {
                    var parent = list[rig.Value.Skeleton.ParentIndexes[i]];
                    m_Manager.AddComponentData(list[i], new Parent { Value = parent });
                    m_Manager.AddComponentData(list[i], new LocalToParent());
                }
            }
            return list;
        }

        List<Entity> CreateRigTransformsWithScale(BlobAssetReference<RigDefinition> rig)
        {
            var list = new List<Entity>(rig.Value.Skeleton.BoneCount);

            for(int i=0;i < rig.Value.Skeleton.BoneCount; i++)
            {
                list.Add(m_Manager.CreateEntity());

                m_Manager.AddComponentData(list[i], new LocalToWorld { Value = i == 0 ? float4x4.identity : float4x4.TRS(math.float3(i), quaternion.identity, math.float3(i)) });
                m_Manager.AddComponentData(list[i], new Translation { Value = i == 0 ? float3.zero : new float3(i) });
                m_Manager.AddComponentData(list[i], new Rotation { Value = quaternion.identity });
                m_Manager.AddComponentData(list[i], new NonUniformScale { Value = i == 0 ? float3.zero : new float3(i) });

                if(rig.Value.Skeleton.ParentIndexes[i] != -1)
                {
                    var parent = list[rig.Value.Skeleton.ParentIndexes[i]];
                    m_Manager.AddComponentData(list[i], new Parent { Value = parent });
                    m_Manager.AddComponentData(list[i], new LocalToParent());
                }
            }
            return list;
        }

        void ValidateAnimationStream(ref AnimationStream stream, ref AnimationStream expectedValue)
        {
            Assert.That(stream.Rig.Value.Skeleton.BoneCount, Is.EqualTo(expectedValue.Rig.Value.Skeleton.BoneCount));

            for(int i=1;i<stream.Rig.Value.Skeleton.BoneCount; i++)
            {
                // No need to use a specialized comparator here
                // All value are integer which are always exact
                Assert.That(stream.GetLocalToParentTranslation(i), Is.EqualTo(expectedValue.GetLocalToParentTranslation(i)).Using(TranslationComparer), $"Translation mismatch for transform '{i}'");
                Assert.That(stream.GetLocalToParentRotation(i), Is.EqualTo(expectedValue.GetLocalToParentRotation(i)).Using(RotationComparer),  $"Rotation mismatch for transform '{i}'");
                Assert.That(stream.GetLocalToParentScale(i), Is.EqualTo(expectedValue.GetLocalToParentScale(i)).Using(ScaleComparer), $"Scale mismatch for transform '{i}'");
            }
        }

        [NodeDefinition(category:"Tests", description:"ExposeTransform tests node", isHidden:true)]
        public class TestNode : NodeDefinition<TestNode.Data, TestNode.SimPorts, TestNode.KernelData, TestNode.KernelDefs, TestNode.Kernel>, IRigContextHandler
        {
            public struct SimPorts : ISimulationPortDefinition
            {
                [PortDefinition(isHidden:true)] public MessageInput<TestNode, Rig> Rig;
            }

            public struct KernelDefs : IKernelPortDefinition
            {
                [PortDefinition(description:"Input stream")]  public DataInput<TestNode, Buffer<AnimatedData>>  Input;
                [PortDefinition(description:"Output stream")] public DataOutput<TestNode, Buffer<AnimatedData>> Output;
            }

            public struct Data : INodeData { }

            public struct KernelData : IKernelData
            {
                public BlobAssetReference<RigDefinition> RigDefinition;
            }

            [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
            public struct Kernel : IGraphKernel<KernelData, KernelDefs>
            {
                public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
                {
                    var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                    if (outputStream.IsNull)
                        throw new System.InvalidOperationException($"TestNode Output is invalid.");

                    var inputStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));

                    AnimationStreamUtils.MemCpy(ref outputStream, ref inputStream);
                }
            }

            public void HandleMessage(in MessageContext ctx, in Rig rig)
            {
                GetKernelData(ctx.Handle).RigDefinition = rig.Value;
                Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0) );
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
            AnimationStreamUtils.SetDefaultValues(ref stream);
            for(int i=1;i<stream.Rig.Value.Skeleton.BoneCount; i++)
            {
                var tmp = float4x4.TRS(new float3(i), quaternion.identity, new float3(1));
                stream.SetLocalToRootTR(i, tmp.c3.xyz, math.quaternion(tmp));
            }

            stream = AnimationStream.Create(m_Rig, m_ExpectedStreamBufferHalf);
            AnimationStreamUtils.SetDefaultValues(ref stream);
            for(int i=1;i<stream.Rig.Value.Skeleton.BoneCount; i++)
            {
                if(i%2==0)
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
            AnimationStreamUtils.SetDefaultValues(ref stream);

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

            SetupEntityTransformComponent(rigEntity);
            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(rigEntity);
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, rig);

            for(int i=0;i<entityTransforms.Count; i++)
            {
                RigEntityBuilder.AddReadTransformHandle<PreAnimationGraphSystem.ReadTransformHandle>(
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

            // Validate that the PreAnimationGraphSystem copy the transform value into the stream
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
            AnimationStreamUtils.SetDefaultValues(ref ecsStream);
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamDefault);

            // Validate that the PostAnimationGraphSystem doesn't copy the transform value into the stream since we are targeting only PreAnimationGraphSystem.Tag
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

            SetupEntityTransformComponent(rigEntity);
            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(rigEntity);
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, rig);

            for(int i=1;i<entityTransforms.Count; i++)
            {
                if(i%2==0)
                {
                    RigEntityBuilder.AddReadTransformHandle<PreAnimationGraphSystem.ReadTransformHandle>(
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

             // Validate that the PreAnimationGraphSystem copy the transform value into the stream
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
            AnimationStreamUtils.SetDefaultValues(ref ecsStream);
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamDefault);

            // Validate that the PostAnimationGraphSystem doesn't copy the transform value into the stream since we are targeting only PreAnimationGraphSystem.Tag
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

            SetupEntityTransformComponent(rigEntity);
            m_Manager.AddComponent<PostAnimationGraphSystem.Tag>(rigEntity);
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, rig);

            for(int i=0;i<entityTransforms.Count; i++)
            {
                RigEntityBuilder.AddReadTransformHandle<PostAnimationGraphSystem.ReadTransformHandle>(
                    m_Manager, rigEntity, entityTransforms[i], i
                    );
            }

            var entityNode = CreateComponentNode(rigEntity, PostSet);
            var testNode = CreateNode<TestNode>(PostSet);

            PostSet.SendMessage(testNode, TestNode.SimulationPorts.Rig, new Rig { Value = rig });
            PostSet.Connect(entityNode, testNode, TestNode.KernelPorts.Input);

            var graphBuffer = CreateGraphValue(testNode, TestNode.KernelPorts.Output, PostSet);

            // Validate that the PreAnimationGraphSystem doesn't copy the transform value into the stream
            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            var ecsStream = AnimationStream.Create(
                rig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
                );
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamDefault);

            // Validate that the PostAnimationGraphSystem copy the transform value into the stream since we are targeting only PostAnimationGraphTag
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

            SetupEntityTransformComponent(rigEntity);
            m_Manager.AddComponent<PostAnimationGraphSystem.Tag>(rigEntity);
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, rig);

            for(int i=1;i<entityTransforms.Count; i++)
            {
                if(i%2==0)
                {
                    RigEntityBuilder.AddReadTransformHandle<PostAnimationGraphSystem.ReadTransformHandle>(
                        m_Manager, rigEntity, entityTransforms[i], i
                        );
                }
            }

            var entityNode = CreateComponentNode(rigEntity, PostSet);
            var testNode = CreateNode<TestNode>(PostSet);

            PostSet.SendMessage(testNode, TestNode.SimulationPorts.Rig, new Rig { Value = rig });
            PostSet.Connect(entityNode, ComponentNode.Output<AnimatedData>(), testNode, TestNode.KernelPorts.Input);

            var graphBuffer = CreateGraphValue(testNode, TestNode.KernelPorts.Output, PostSet);

            // Validate that the PreAnimationGraphSystem doesn't copy the transform value into the stream
            m_PreAnimationGraph.Update();
            m_Manager.CompleteAllJobs();

            var ecsStream = AnimationStream.Create(
                rig,
                m_Manager.GetBuffer<AnimatedData>(rigEntity).AsNativeArray()
                );
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamDefault);

            // Validate that the PostAnimationGraphSystem copy the transform value into the stream since we are targeting only PreAnimationGraphSystem.Tag
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

            SetupEntityTransformComponent(rigEntity);
            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(rigEntity);
            m_Manager.AddComponent<PostAnimationGraphSystem.Tag>(rigEntity);
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, rig);

            for(int i=0;i<entityTransforms.Count; i++)
            {
                RigEntityBuilder.AddReadTransformHandle<PreAnimationGraphSystem.ReadTransformHandle>(
                    m_Manager, rigEntity, entityTransforms[i], i
                    );
                RigEntityBuilder.AddReadTransformHandle<PostAnimationGraphSystem.ReadTransformHandle>(
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

            // Validate that the PreAnimationGraphSystem copy the transform value into the stream
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
            AnimationStreamUtils.SetDefaultValues(ref ecsStream);
            ValidateAnimationStream(ref ecsStream, ref m_ExpectedStreamDefault);

            // Validate that the PostAnimationGraphSystem copy the transform value into the stream
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
        public void CanReadFromAnimationStreamAndWriteLocalToWorldToAllEntities()
        {
            var rig = m_Rig;
            var entityTransforms = CreateRigTransformsWithScale(rig);

            var rigEntity = m_Manager.CreateEntity();

            SetupEntityTransformComponent(rigEntity);
            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(rigEntity);
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, rig);

            for(int i=1;i<entityTransforms.Count; i++)
            {
                RigEntityBuilder.AddWriteTransformHandle<PostAnimationGraphSystem.WriteTransformHandle>(
                    m_Manager, rigEntity, entityTransforms[i], i
                    );
            }

            for(int i=1;i<entityTransforms.Count; i++)
            {
                var localToWorld = m_Manager.GetComponentData<LocalToWorld>(entityTransforms[i]);
                Assert.That(localToWorld.Value, Is.EqualTo(float4x4.TRS(math.float3(i), quaternion.identity, math.float3(i))));
            }

            World.GetOrCreateSystem<PreAnimationGraphSystem>().Update();
            World.GetOrCreateSystem<PostAnimationGraphSystem>().Update();
            m_Manager.CompleteAllJobs();

            // The animation stream start with default values, so after writing into entities they all should have the identity matrix
            for(int i=1;i<entityTransforms.Count; i++)
            {
                var localToWorld = m_Manager.GetComponentData<LocalToWorld>(entityTransforms[i]);

                Assert.That(localToWorld.Value, Is.EqualTo(float4x4.identity));
            }
        }

        [Test]
        public void CanReadFromAnimationStreamAndWriteLocalToWorldToHalfEntities()
        {
            var rig = m_Rig;
            var entityTransforms = CreateRigTransformsWithScale(rig);

            var rigEntity = m_Manager.CreateEntity();

            SetupEntityTransformComponent(rigEntity);
            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(rigEntity);
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, rig);

            for(int i=1;i<entityTransforms.Count; i++)
            {
                if(i%2==0)
                {
                    RigEntityBuilder.AddWriteTransformHandle<PostAnimationGraphSystem.WriteTransformHandle>(
                        m_Manager, rigEntity, entityTransforms[i], i
                        );
                }
            }

            for(int i=1;i<entityTransforms.Count; i++)
            {
                var localToWorld = m_Manager.GetComponentData<LocalToWorld>(entityTransforms[i]);

                Assert.That(localToWorld.Value, Is.EqualTo(float4x4.TRS(math.float3(i), quaternion.identity, math.float3(i))));
            }

            World.GetOrCreateSystem<PreAnimationGraphSystem>().Update();
            World.GetOrCreateSystem<PostAnimationGraphSystem>().Update();
            m_Manager.CompleteAllJobs();

            // The animation stream start with default values, so after writing into entities they all should have the identity matrix
            for(int i=1;i<entityTransforms.Count; i++)
            {
                var localToWorld = m_Manager.GetComponentData<LocalToWorld>(entityTransforms[i]);
                if(i%2==0)
                {
                    Assert.That(localToWorld.Value, Is.EqualTo(float4x4.identity));
                }
                else
                {
                    Assert.That(localToWorld.Value, Is.EqualTo(float4x4.TRS(math.float3(i), quaternion.identity, math.float3(i))));
                }
            }
        }

        [Test]
        public void CanDeleteRigEntity()
        {
            var rig = m_Rig;
            var entityTransforms = CreateRigTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();

            SetupEntityTransformComponent(rigEntity);
            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(rigEntity);
            m_Manager.AddComponent<PostAnimationGraphSystem.Tag>(rigEntity);
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, rig);

            for(int i=1;i<entityTransforms.Count; i++)
            {
                RigEntityBuilder.AddReadTransformHandle<PreAnimationGraphSystem.ReadTransformHandle>(
                    m_Manager, rigEntity, entityTransforms[i], i
                    );

                RigEntityBuilder.AddWriteTransformHandle<PostAnimationGraphSystem.WriteTransformHandle>(
                    m_Manager, rigEntity, entityTransforms[i], i
                    );
            }

            World.GetOrCreateSystem<PreAnimationGraphSystem>().Update();
            World.GetOrCreateSystem<PostAnimationGraphSystem>().Update();

            m_Manager.DestroyEntity(rigEntity);

            World.GetOrCreateSystem<PreAnimationGraphSystem>().Update();
            World.GetOrCreateSystem<PostAnimationGraphSystem>().Update();

            Assert.Pass();
        }

        [Test]
        public void CanAddReadTransformHandleUnsorted()
        {
            var rig = m_Rig;
            var entityTransforms = CreateRigTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();

            SetupEntityTransformComponent(rigEntity);
            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(rigEntity);
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, rig);

            for(int i=entityTransforms.Count-1; i>0; i--)
            {
                RigEntityBuilder.AddReadTransformHandle<PreAnimationGraphSystem.ReadTransformHandle>(
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
            var readTransformHandles = m_Manager.GetBuffer<PreAnimationGraphSystem.ReadTransformHandle>(rigEntity);
            Assert.That(readTransformHandles.Length, Is.EqualTo(entityTransforms.Count-1));
            for(int i=1; i<readTransformHandles.Length; i++)
            {
                Assert.That(readTransformHandles[i-1].Index, Is.LessThan(readTransformHandles[i].Index));
            }
        }

        [DisableAutoCreation]
        [UpdateBefore(typeof(PreAnimationSystemGroup))]
        internal class TestAddEntityTransformSystem : JobComponentSystem
        {
            internal Entity Rig;
            internal List<Entity> EntityTransforms;
            internal int Iterator;

            protected override JobHandle OnUpdate(JobHandle inputDeps)
            {
                if (Iterator == 0)
                    return inputDeps;

                RigEntityBuilder.AddReadTransformHandle<PreAnimationGraphSystem.ReadTransformHandle>(
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

            SetupEntityTransformComponent(rigEntity);
            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(rigEntity);
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, rig);

            var entityNode = CreateComponentNode(rigEntity, PreSet);
            var testNode = CreateNode<TestNode>(PreSet);

            PreSet.SendMessage(testNode, TestNode.SimulationPorts.Rig, new Rig { Value = rig });
            PreSet.Connect(entityNode, ComponentNode.Output<AnimatedData>(), testNode, TestNode.KernelPorts.Input);

            var graphBuffer = CreateGraphValue(testNode, TestNode.KernelPorts.Output, PreSet);

            var addTransformSystem = World.GetOrCreateSystem<TestAddEntityTransformSystem>();
            addTransformSystem.Rig = rigEntity;
            addTransformSystem.EntityTransforms = entityTransforms;
            addTransformSystem.Iterator = entityTransforms.Count - 1;

            for(int i=0;i<entityTransforms.Count;i++)
            {
                addTransformSystem.Update();
                m_PreAnimationGraph.Update();
                m_Manager.CompleteAllJobs();

                // Validate that Read Transform handle are sorted
                var readTransformHandles = m_Manager.GetBuffer<PreAnimationGraphSystem.ReadTransformHandle>(rigEntity);
                for(int j=1; j<readTransformHandles.Length; j++)
                {
                    Assert.That(readTransformHandles[j-1].Index, Is.LessThan(readTransformHandles[j].Index), $"On Update '{i}': ReadTransformHandle are not sorted");
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

            SetupEntityTransformComponent(rigEntity);
            m_Manager.AddComponent<PreAnimationGraphSystem.Tag>(rigEntity);
            RigEntityBuilder.SetupRigEntity(rigEntity, m_Manager, rig);

            for(int i=1; i<entityTransforms.Count; i++)
            {
                if(i%2==0)
                {
                    RigEntityBuilder.AddReadTransformHandle<PreAnimationGraphSystem.ReadTransformHandle>(
                    m_Manager, rigEntity, entityTransforms[i], i
                    );
                }
                else
                {
                    // Explicitly create multiple ReadTransformHandle that target the same index
                    RigEntityBuilder.AddReadTransformHandle<PreAnimationGraphSystem.ReadTransformHandle>(
                        m_Manager, rigEntity, entityTransforms[i], i-1
                        );
                }
            }

            var readTransformHandles = m_Manager.GetBuffer<PreAnimationGraphSystem.ReadTransformHandle>(rigEntity).AsNativeArray();
            readTransformHandles.Sort(new RigEntityBuilder.TransformHandleComparer<PreAnimationGraphSystem.ReadTransformHandle>());
            var end = readTransformHandles.Unique(new RigEntityBuilder.TransformHandleComparer<PreAnimationGraphSystem.ReadTransformHandle>());

            Assert.That(end, Is.LessThan(readTransformHandles.Length));
            Assert.That(end, Is.EqualTo(5));
        }
    }
}