using System;
using System.Collections;

using NUnit.Framework;

using Unity.Animation.Tests;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;


namespace Unity.Animation.PerformanceTests
{
    [Category("performance"), Category("animation")]
    public class SystemPerformanceTests : AnimationPlayModeTestsFixture, IPrebuildSetup
    {
        protected RigComputeMatricesSystem              m_RigComputeMatricesSystem;

        protected BlobAssetReference<RigDefinition>     m_RigDefinition;
        protected Entity                                m_RigPrefab;

        protected BlobAssetReference<Clip>              m_ClipWalk;

        protected BlobAssetReference<Clip>              m_ClipJog;

        protected BlobAssetReference<ClipInstance>      m_ClipInstanceWalk;
        protected BlobAssetReference<ClipInstance>      m_ClipInstanceJog;


        protected static int[] s_CharacterCount =
        {
            1, 20, 50, 100, 200, 500, 1000
        };

        public void Setup()
        {
#if UNITY_EDITOR
            PlaymodeTestsEditorSetup.CreateStreamingAssetsDirectory();

            PlaymodeTestsEditorSetup.BuildRigDefinitionBlobAsset("Packages/com.unity.animation/Tests/Runtime/Ninja.prefab");
            PlaymodeTestsEditorSetup.BuildClipBlobAsset("Packages/com.unity.animation/Tests/Runtime/Ninja1_Combat_Jog_Forward_InPlace.anim");
            PlaymodeTestsEditorSetup.BuildClipBlobAsset("Packages/com.unity.animation/Tests/Runtime/Ninja1_Combat_Walk_Forward_InPlace.anim");
#endif
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            var path = "Ninja.blob";
            m_RigDefinition = BlobFile.ReadBlobAsset<RigDefinition>(path);

            path = "Ninja1_Combat_Walk_Forward_InPlace.blob";
            m_ClipWalk = BlobFile.ReadBlobAsset<Clip>(path);

            path = "Ninja1_Combat_Jog_Forward_InPlace.blob";
            m_ClipJog = BlobFile.ReadBlobAsset<Clip>(path);
        }

        [SetUp]
        protected override void SetUp()
        {
            base.SetUp();

            m_RigComputeMatricesSystem = World.GetOrCreateSystem<RigComputeMatricesSystem>();
            // Force and update to kick burst compilation
            m_RigComputeMatricesSystem.Update();

            Set.RendererModel = DataFlowGraph.RenderExecutionModel.Islands;

            // Force and update to kick burst compilation
            m_AnimationGraphSystem.Update();

            m_RigPrefab = RigEntityBuilder.CreatePrefabEntity(m_Manager, m_RigDefinition);

            m_Manager.AddComponentData(m_RigPrefab, new Translation { Value = float3.zero });
            m_Manager.AddComponentData(m_RigPrefab, new Rotation { Value = quaternion.identity });

            m_ClipInstanceWalk = ClipManager.Instance.GetClipFor(m_RigDefinition, m_ClipWalk);
            m_ClipInstanceJog = ClipManager.Instance.GetClipFor(m_RigDefinition, m_ClipJog);
        }

        private void CreateSimpleGraph(Entity entity)
        {
            var set = Set;

            var clipNode1 = CreateNode<ClipNode>();
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.ClipInstance, m_ClipInstanceWalk);

            var clipNode2 = CreateNode<ClipNode>();
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.ClipInstance, m_ClipInstanceJog);

            var mixerNode = CreateNode<MixerNode>();
            set.SendMessage(mixerNode, MixerNode.SimulationPorts.RigDefinition, m_RigDefinition);
            set.SendMessage(mixerNode, MixerNode.SimulationPorts.Blend, 0.5f);
            set.Connect(clipNode1, ClipNode.KernelPorts.Output, mixerNode, MixerNode.KernelPorts.Input0);
            set.Connect(clipNode2, ClipNode.KernelPorts.Output, mixerNode, MixerNode.KernelPorts.Input1);

            var graphBuffer = CreateGraphBuffer(mixerNode, MixerNode.KernelPorts.Output);
            m_Manager.AddComponentData(entity, new GraphOutput { Buffer = graphBuffer });
        }

        [UnityTest, Performance, Version("1")]
        public IEnumerator RigComputeMatricesSystem([ValueSource("s_CharacterCount")] int instanceCount)
        {
            var entities = new NativeArray<Entity>(instanceCount, Allocator.Persistent);
            m_Manager.Instantiate(m_RigPrefab, entities);

            SampleGroupDefinition[] markers =
            {
                new SampleGroupDefinition("RigComputeMatricesSystemBase"),
                new SampleGroupDefinition("RigComputeMatricesSystemBase:ComputeGlobalSpaceJob (Burst)"),
                new SampleGroupDefinition("RigComputeMatricesSystemBase:ComputeRigSpaceJob (Burst)"),
                new SampleGroupDefinition("RigComputeMatricesSystemBase:ComputeGlobalAndRigSpaceJob (Burst)")
            };

            yield return Measure.Frames()
                .WarmupCount(10)
                .MeasurementCount(10)
                .ProfilerMarkers(markers)
                .Run();

            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        [UnityTest, Performance, Version("1")]
        public IEnumerator AnimationGraphSystem([ValueSource("s_CharacterCount")] int instanceCount)
        {
            var entities = new NativeArray<Entity>(instanceCount, Allocator.Persistent);
            m_Manager.Instantiate(m_RigPrefab, entities);

            foreach (var entity in entities)
                CreateSimpleGraph(entity);

            SampleGroupDefinition[] markers =
            {
                new SampleGroupDefinition("AnimationGraphSystemBase"),
                new SampleGroupDefinition("RenderGraph:ParallelRenderer (Burst)"),
                new SampleGroupDefinition("Animation.SampleClip"),
                new SampleGroupDefinition("Animation.MixPose")
            };

            yield return Measure.Frames()
                .WarmupCount(10)
                .MeasurementCount(10)
                .ProfilerMarkers(markers)
                .Run();

            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }
    }
}
