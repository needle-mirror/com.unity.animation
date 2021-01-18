using System;

using NUnit.Framework;

using Unity.Entities;
using Unity.Deformations;
using Unity.Animation.Hybrid;

using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.Collections;

namespace Unity.Animation.Tests
{
    public class BlendshapeTests : AnimationTestsFixture
    {
        static readonly string k_BlendshapePrefabPath = "Packages/com.unity.animation/Unity.Animation.Hybrid.Tests/Resources/BlendshapeTest.prefab";

        const float m_BlendShapeConstantClip_Value1 = 10f;
        const float m_BlendShapeConstantClip_Value2 = 60f;
        const float m_BlendShapeConstantClip_Value3 = 34f;
        const float m_BlendShapeConstantClip_Value4 = 22f;

        BlobAssetReference<Clip> m_BlendshapeClip;

        Scene m_Scene;
        GameObject m_BlendShapeGO;

        Entity GetFirstEntityWithComponent<T>(NativeArray<Entity> entities)
        {
            for (int i = 0; i < entities.Length; ++i)
                if (m_Manager.HasComponent<T>(entities[i]))
                    return entities[i];

            return Entity.Null;
        }

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

            var clip = new AnimationClip();
            clip.SetCurve("", typeof(SkinnedMeshRenderer), "blendShape.shape1", GetConstantCurve(m_BlendShapeConstantClip_Value1));
            clip.SetCurve("", typeof(SkinnedMeshRenderer), "blendShape.shape2", GetConstantCurve(m_BlendShapeConstantClip_Value2));
            clip.SetCurve("", typeof(SkinnedMeshRenderer), "blendShape.shape3", GetConstantCurve(m_BlendShapeConstantClip_Value3));
            clip.SetCurve("", typeof(SkinnedMeshRenderer), "blendShape.shape4", GetConstantCurve(m_BlendShapeConstantClip_Value4));
            m_BlendshapeClip = clip.ToDenseClip();

            m_BlendShapeGO = GameObject.Instantiate(AssetDatabase.LoadAssetAtPath(k_BlendshapePrefabPath, typeof(GameObject)), Vector3.zero, Quaternion.identity) as GameObject;
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

#if !ENABLE_COMPUTE_DEFORMATIONS
                UnityEngine.TestTools.LogAssert.Expect(
                    LogType.Error,
                    "DOTS SkinnedMeshRenderer blendshapes are only supported via compute shaders in hybrid renderer. Make sure to add 'ENABLE_COMPUTE_DEFORMATIONS' to your scripting defines in Player settings."
                );
#endif

                using (var entities = m_Manager.GetAllEntities(Collections.Allocator.Persistent))
                {
                    Assert.NotZero(entities.Length); // rig entity and rendering deformation entity

                    var entity = GetFirstEntityWithComponent<BlendShapeWeight>(entities);
                    Assert.IsTrue(entity != Entity.Null);
                    Assert.IsTrue(m_Manager.HasComponent<BlendShapeChunkMapping>(entity));

                    var bsBuffer = m_Manager.GetBuffer<BlendShapeWeight>(entity);
                    var bsChunkMapping = m_Manager.GetComponentData<BlendShapeChunkMapping>(entity);
                    Assert.That(bsBuffer.Length, Is.EqualTo(4));
                    Assert.That(bsChunkMapping.Size, Is.EqualTo(4));
                }
            }
        }

        [Test]
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
                    Assert.NotZero(entities.Length); // rig entity and rendering deformation entity

                    var entity = GetFirstEntityWithComponent<BlendShapeWeight>(entities);
                    Assert.IsTrue(entity != Entity.Null);

                    var bsBuffer = m_Manager.GetBuffer<BlendShapeWeight>(entity);
                    Assert.That(bsBuffer.Length, Is.EqualTo(4));
                    Assert.That(bsBuffer[0].Value, Is.EqualTo(0f));
                    Assert.That(bsBuffer[1].Value, Is.EqualTo(0f));
                    Assert.That(bsBuffer[2].Value, Is.EqualTo(0f));
                    Assert.That(bsBuffer[3].Value, Is.EqualTo(0f));

                    var rig = m_Manager.GetComponentData<Rig>(entity);
                    var animatedDataBuffer = m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray();
                    var stream = AnimationStream.Create(rig, animatedDataBuffer);

                    Core.EvaluateClip(ClipManager.Instance.GetClipFor(rig, m_BlendshapeClip), 1f, ref stream, 0);

                    m_Manager.World.GetOrCreateSystem<ComputeDeformationData>().Update();
                    m_Manager.CompleteAllJobs();

                    bsBuffer = m_Manager.GetBuffer<BlendShapeWeight>(entity);
                    Assert.That(bsBuffer[0].Value, Is.EqualTo(m_BlendShapeConstantClip_Value1));
                    Assert.That(bsBuffer[1].Value, Is.EqualTo(m_BlendShapeConstantClip_Value2));
                    Assert.That(bsBuffer[2].Value, Is.EqualTo(m_BlendShapeConstantClip_Value3));
                    Assert.That(bsBuffer[3].Value, Is.EqualTo(m_BlendShapeConstantClip_Value4));
                }
            }
        }
    }
}
