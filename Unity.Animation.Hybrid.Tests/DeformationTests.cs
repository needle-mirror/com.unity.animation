using System;

using NUnit.Framework;

using Unity.Entities;
using Unity.Deformations;
using Unity.Animation.Hybrid;

using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Unity.Animation.Tests
{
    public class BlendshapeTests : AnimationTestsFixture
    {
        static readonly string k_BlendshapeFBXPath = "Packages/com.unity.animation/Unity.Animation.Hybrid.Tests/Resources/BlendshapeTest.fbx";

        const float m_BlendShapeConstantClip_Value1 = 10f;
        const float m_BlendShapeConstantClip_Value2 = 60f;
        const float m_BlendShapeConstantClip_Value3 = 34f;
        const float m_BlendShapeConstantClip_Value4 = 22f;

        BlobAssetReference<Clip> m_BlendshapeClip;

        Scene m_Scene;
        GameObject m_BlendShapeGO;

        [SetUp]
        protected override void SetUp()
        {
            base.SetUp();

            m_Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            SceneManager.SetActiveScene(m_Scene);

            m_BlendshapeClip = CreateConstantDenseClip(
                Array.Empty<(string, Mathematics.float3)>(),
                Array.Empty<(string, Mathematics.quaternion)>(),
                Array.Empty<(string, Mathematics.float3)>(),
                new[] { ("blendShape.shape1", m_BlendShapeConstantClip_Value1), ("blendShape.shape2", m_BlendShapeConstantClip_Value2), ("blendShape.shape3", m_BlendShapeConstantClip_Value3), ("blendShape.shape4", m_BlendShapeConstantClip_Value4) },
                Array.Empty<(string, int)>()
            );

            m_BlendShapeGO = GameObject.Instantiate(AssetDatabase.LoadAssetAtPath(k_BlendshapeFBXPath, typeof(GameObject)), Vector3.zero, Quaternion.identity) as GameObject;
            m_BlendShapeGO.AddComponent<RigComponent>();
        }

        protected override void TearDown()
        {
            GameObject.DestroyImmediate(m_BlendShapeGO);
            base.TearDown();
        }

        [Test]
        public void CanConvertSkinnedMeshRendererBlendshapes()
        {
            var scene = SceneManager.GetActiveScene();

            using (var blobAssetStore = new BlobAssetStore())
            {
                var settings = GameObjectConversionSettings.FromWorld(World, blobAssetStore);
                GameObjectConversionUtility.ConvertScene(scene, settings);

#if UNITY_ENTITIES_0_12_OR_NEWER && !ENABLE_COMPUTE_DEFORMATIONS
                UnityEngine.TestTools.LogAssert.Expect(
                    LogType.Error,
                    "DOTS SkinnedMeshRenderer blendshapes are only supported via compute shaders in hybrid renderer. Make sure to add 'ENABLE_COMPUTE_DEFORMATIONS' to your scripting defines in Player settings."
                );
#endif

                using (var entities = m_Manager.GetAllEntities(Collections.Allocator.Persistent))
                {
                    Assert.That(entities.Length, Is.EqualTo(1));
                    Assert.IsTrue(m_Manager.HasComponent<BlendShapeWeight>(entities[0]));
                    Assert.IsTrue(m_Manager.HasComponent<BlendShapeChunkMapping>(entities[0]));

                    var bsBuffer = m_Manager.GetBuffer<BlendShapeWeight>(entities[0]);
                    var bsChunkMapping = m_Manager.GetComponentData<BlendShapeChunkMapping>(entities[0]);
                    Assert.That(bsBuffer.Length, Is.EqualTo(4));
                    Assert.That(bsChunkMapping.Size, Is.EqualTo(4));
                }
            }
        }

        [Test]
#if !UNITY_ENTITIES_0_12_OR_NEWER
        [Ignore("DOTS Blendshape only supported in 2020.1 and above")]
#endif
        public void CanAnimateBlendshapeWeights()
        {
            var scene = SceneManager.GetActiveScene();

            using (var blobAssetStore = new BlobAssetStore())
            {
                var settings = GameObjectConversionSettings.FromWorld(World, blobAssetStore);
                GameObjectConversionUtility.ConvertScene(scene, settings);

#if !ENABLE_COMPUTE_DEFORMATIONS
                UnityEngine.TestTools.LogAssert.Expect(
                    LogType.Error,
                    "DOTS SkinnedMeshRenderer blendshapes are only supported via compute shaders in hybrid renderer. Make sure to add 'ENABLE_COMPUTE_DEFORMATIONS' to your scripting defines in Player settings."
                );
#endif

                using (var entities = m_Manager.GetAllEntities(Collections.Allocator.Persistent))
                {
                    Assert.That(entities.Length, Is.EqualTo(1));
                    Assert.IsTrue(m_Manager.HasComponent<BlendShapeWeight>(entities[0]));

                    var bsBuffer = m_Manager.GetBuffer<BlendShapeWeight>(entities[0]);
                    Assert.That(bsBuffer.Length, Is.EqualTo(4));
                    Assert.That(bsBuffer[0].Value, Is.EqualTo(0f));
                    Assert.That(bsBuffer[1].Value, Is.EqualTo(0f));
                    Assert.That(bsBuffer[2].Value, Is.EqualTo(0f));
                    Assert.That(bsBuffer[3].Value, Is.EqualTo(0f));

                    var rig = m_Manager.GetComponentData<Rig>(entities[0]);
                    var animatedDataBuffer = m_Manager.GetBuffer<AnimatedData>(entities[0]).AsNativeArray();
                    var stream = AnimationStream.Create(rig, animatedDataBuffer);

                    Core.EvaluateClip(ClipManager.Instance.GetClipFor(rig, m_BlendshapeClip), 1f, ref stream, 0);

                    m_Manager.World.GetOrCreateSystem<ComputeSkinMatrixSystem>().Update();
                    m_Manager.CompleteAllJobs();

                    bsBuffer = m_Manager.GetBuffer<BlendShapeWeight>(entities[0]);
                    Assert.That(bsBuffer[0].Value, Is.EqualTo(m_BlendShapeConstantClip_Value1));
                    Assert.That(bsBuffer[1].Value, Is.EqualTo(m_BlendShapeConstantClip_Value2));
                    Assert.That(bsBuffer[2].Value, Is.EqualTo(m_BlendShapeConstantClip_Value3));
                    Assert.That(bsBuffer[3].Value, Is.EqualTo(m_BlendShapeConstantClip_Value4));
                }
            }
        }
    }
}
