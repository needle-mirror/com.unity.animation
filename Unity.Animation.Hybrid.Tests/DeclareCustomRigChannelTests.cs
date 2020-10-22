using System;
using NUnit.Framework;
using Unity.Entities;
using Unity.Animation.Hybrid;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.Collections;

namespace Unity.Animation.Tests
{
    public class DeclareCustomRigChannelTests : AnimationTestsFixture
    {
        static readonly string k_CustomTranslationChannelName = "MyCustomTranslation";
        static readonly string k_CustomRotationChannelName    = "MyCustomRotation";
        static readonly string k_CustomScaleChannelName       = "MyCustomScale";
        static readonly string k_CustomFloatChannelName       = "MyCustomFloat";
        static readonly string k_CustomIntChannelName         = "MyCustomInt";

        class TestDeclareCustomRigChannels : MonoBehaviour, IDeclareCustomRigChannels
        {
            public int UniqueId = 0;

            public void DeclareRigChannels(RigChannelCollector collector)
            {
                collector.Add(new TranslationChannel { Id = k_CustomTranslationChannelName + UniqueId, DefaultValue = Vector3.zero });
                collector.Add(new RotationChannel { Id = k_CustomRotationChannelName + UniqueId, DefaultValue = Quaternion.identity });
                collector.Add(new ScaleChannel { Id = k_CustomScaleChannelName + UniqueId, DefaultValue = Vector3.one });
                collector.Add(new Hybrid.FloatChannel { Id = k_CustomFloatChannelName + UniqueId, DefaultValue = 0f });
                collector.Add(new Hybrid.IntChannel { Id = k_CustomIntChannelName + UniqueId, DefaultValue = 0 });
            }
        }

        Scene m_Scene;

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
        }

        [Test]
        public void CanDeclareCustomRigChannels()
        {
            var scene = SceneManager.GetActiveScene();
            var root = CreateGameObject();
            var child = CreateGameObject();
            child.transform.parent = root.transform;

            root.AddComponent<TestDeclareCustomRigChannels>().UniqueId = 0;
            child.AddComponent<TestDeclareCustomRigChannels>().UniqueId = 1;
            child.AddComponent<TestDeclareCustomRigChannels>().UniqueId = 2;

            var rigComponent = root.AddComponent<RigComponent>();
            rigComponent.Bones = root.GetComponentsInChildren<Transform>();

            using (var blobAssetStore = new BlobAssetStore())
            {
                var settings = GameObjectConversionSettings.FromWorld(World, blobAssetStore);
                GameObjectConversionUtility.ConvertScene(scene, settings);

                using (var entities = m_Manager.GetAllEntities(Allocator.Persistent))
                {
                    var entity = GetFirstEntityWithComponent<Rig>(entities);
                    Assert.IsTrue(entity != Entity.Null);

                    ref var bindings = ref m_Manager.GetComponentData<Rig>(entity).Value.Value.Bindings;

                    for (int i = 0; i < 3; ++i)
                    {
                        Assert.IsTrue(Core.FindBindingIndex(ref bindings.TranslationBindings, k_CustomTranslationChannelName + i) != -1);
                        Assert.IsTrue(Core.FindBindingIndex(ref bindings.RotationBindings, k_CustomRotationChannelName + i) != -1);
                        Assert.IsTrue(Core.FindBindingIndex(ref bindings.ScaleBindings, k_CustomScaleChannelName + i) != -1);
                        Assert.IsTrue(Core.FindBindingIndex(ref bindings.FloatBindings, k_CustomFloatChannelName + i) != -1);
                        Assert.IsTrue(Core.FindBindingIndex(ref bindings.IntBindings, k_CustomIntChannelName + i) != -1);
                    }
                }
            }
        }

        [Test]
        public void DuplicateRigChannelReturnExistingIndices()
        {
            var scene = SceneManager.GetActiveScene();
            var go = CreateGameObject();

            var rigComponent = go.AddComponent<RigComponent>();
            rigComponent.Bones = go.GetComponentsInChildren<Transform>();

            var rigBuilderData = rigComponent.ExtractRigBuilderData();

            var tChannel = new TranslationChannel { Id = k_CustomTranslationChannelName };
            var rChannel = new RotationChannel { Id = k_CustomRotationChannelName };
            var sChannel = new ScaleChannel { Id = k_CustomScaleChannelName };
            var fChannel = new Hybrid.FloatChannel { Id = k_CustomFloatChannelName };
            var iChannel = new Hybrid.IntChannel { Id = k_CustomIntChannelName };

            var collector = new RigChannelCollector(rigComponent.transform, ref rigBuilderData);
            var tIdx = collector.Add(tChannel);
            var rIdx = collector.Add(rChannel);
            var sIdx = collector.Add(sChannel);
            var fIdx = collector.Add(fChannel);
            var iIdx = collector.Add(iChannel);

            // Adding duplicate channels to collector should simply return index of existing channel.
            Assert.AreEqual(tIdx, collector.Add(tChannel));
            Assert.AreEqual(rIdx, collector.Add(rChannel));
            Assert.AreEqual(sIdx, collector.Add(sChannel));
            Assert.AreEqual(fIdx, collector.Add(fChannel));
            Assert.AreEqual(iIdx, collector.Add(iChannel));

            rigBuilderData.Dispose();
        }

        [Test]
        public void AddingInvalidRigChannelsThrowsExceptions()
        {
            var scene = SceneManager.GetActiveScene();
            var root = CreateGameObject("root");
            var child1 = CreateGameObject("child1");
            var child2 = CreateGameObject("child2");
            child1.transform.parent = root.transform;
            child2.transform.parent = child1.transform;

            var rigComponent = root.AddComponent<RigComponent>();
            rigComponent.Bones = root.GetComponentsInChildren<Transform>();

            var rigBuilderData = rigComponent.ExtractRigBuilderData();

            var collector = new RigChannelCollector(rigComponent.transform, ref rigBuilderData);

            TranslationChannel tChannel = null;
            RotationChannel rChannel = null;
            ScaleChannel sChannel = null;
            Hybrid.FloatChannel fChannel = null;
            Hybrid.IntChannel iChannel = null;

            // Adding null channels throws
            Assert.Throws<NullReferenceException>(() => collector.Add(tChannel));
            Assert.Throws<NullReferenceException>(() => collector.Add(rChannel));
            Assert.Throws<NullReferenceException>(() => collector.Add(sChannel));
            Assert.Throws<NullReferenceException>(() => collector.Add(fChannel));
            Assert.Throws<NullReferenceException>(() => collector.Add(iChannel));

            tChannel = new TranslationChannel();
            rChannel = new RotationChannel();
            sChannel = new ScaleChannel();
            fChannel = new Hybrid.FloatChannel();
            iChannel = new Hybrid.IntChannel();

            // Adding channels with invalid ids throws
            Assert.Throws<InvalidOperationException>(() => collector.Add(tChannel));
            Assert.Throws<InvalidOperationException>(() => collector.Add(rChannel));
            Assert.Throws<InvalidOperationException>(() => collector.Add(sChannel));
            Assert.Throws<InvalidOperationException>(() => collector.Add(fChannel));
            Assert.Throws<InvalidOperationException>(() => collector.Add(iChannel));

            tChannel.Id = "";              // For root path expection - custom channel can't be null/empty
            rChannel.Id = "child1";        // child1 clashes with skeleton node
            sChannel.Id = "child1/child2"; // child1/child2 also clashes with skeleton node

            // Adding custom T,R,S channels that clash with skeleton nodes throws
            Assert.Throws<InvalidOperationException>(() => collector.Add(tChannel));
            Assert.Throws<InvalidOperationException>(() => collector.Add(rChannel));
            Assert.Throws<InvalidOperationException>(() => collector.Add(sChannel));

            rigBuilderData.Dispose();
        }
    }
}
