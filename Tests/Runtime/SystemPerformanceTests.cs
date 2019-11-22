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

        protected static int[] s_CharacterCount =
        {
            1, 20, 50, 100, 200, 500, 1000
        };

        public void Setup()
        {
#if UNITY_EDITOR
            PlaymodeTestsEditorSetup.CreateStreamingAssetsDirectory();

            PlaymodeTestsEditorSetup.BuildRigDefinitionBlobAsset("Packages/com.unity.animation/Tests/Runtime/Ninja.prefab");
#endif
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            var path = "Ninja.blob";
            m_RigDefinition = BlobFile.ReadBlobAsset<RigDefinition>(path);
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
    }
}
