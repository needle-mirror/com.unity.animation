using NUnit.Framework;

using Unity.Entities;
using Unity.Animation.Hybrid;

using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.Transforms;
using Unity.Collections;

#if UNITY_ENABLE_ANIMATION_RIG_CONVERSION_CLEANUP
namespace Unity.Animation.Tests
{
    class RigConversionTests : AnimationTestsFixture
    {
        protected Scene m_Scene;

        protected ComponentType[] m_RigEntityComponentType = new[]
        {
            ComponentType.ReadWrite<LocalToWorld>(),
            ComponentType.ReadWrite<Rotation>(),
            ComponentType.ReadWrite<Translation>(),
            ComponentType.ReadWrite<Rig>(),
            ComponentType.ReadWrite<AnimatedData>(),
            ComponentType.ReadWrite<AnimatedLocalToWorld>(),
            ComponentType.ReadWrite<SharedRigHash>(),
            ComponentType.ReadWrite<RigRootEntity>()
        };

        [SetUp]
        protected override void SetUp()
        {
            base.SetUp();

            m_Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            SceneManager.SetActiveScene(m_Scene);
        }

        [Test]
        public void CanConvertRigWithOneTransform()
        {
            var scene = SceneManager.GetActiveScene();
            var go = CreateGameObject();
            go.transform.localPosition = new Vector3(1, 2, 3);

            var rigComponent = go.AddComponent<RigComponent>();
            rigComponent.Bones = go.GetComponentsInChildren<Transform>();

            using (var blobAssetStore = new BlobAssetStore())
            {
                var settings = GameObjectConversionSettings.FromWorld(World, blobAssetStore);
                GameObjectConversionUtility.ConvertScene(scene, settings);

                using (var entities = m_Manager.GetAllEntities(Collections.Allocator.Persistent))
                {
                    Assert.That(entities.Length, Is.EqualTo(1));

                    using (var componentTypeToMatch = new NativeArray<ComponentType>(m_RigEntityComponentType, Allocator.Temp))
                    using (var entityComponentTypes = m_Manager.GetComponentTypes(entities[0]))
                    {
                        Assert.That(entityComponentTypes, Is.EquivalentTo(componentTypeToMatch));
                    }
                }
            }
        }

        [Test]
        public void CanConvertRigTransformHierarchyWithoutExposeTransform()
        {
            var scene = SceneManager.GetActiveScene();
            var root = CreateGameObject();
            root.transform.localPosition = new Vector3(1, 2, 3);

            var child1 = CreateGameObject();
            child1.transform.parent = root.transform;
            child1.transform.localPosition = new Vector3(1, 2, 3);

            var child2 = CreateGameObject();
            child2.transform.parent = root.transform;
            child2.transform.localPosition = new Vector3(1, 2, 3);

            var child3 = CreateGameObject();
            child3.transform.parent = root.transform;
            child3.transform.localPosition = new Vector3(1, 2, 3);

            var rigComponent = root.AddComponent<RigComponent>();
            rigComponent.Bones = root.GetComponentsInChildren<Transform>();

            using (var blobAssetStore = new BlobAssetStore())
            {
                var settings = GameObjectConversionSettings.FromWorld(World, blobAssetStore);
                GameObjectConversionUtility.ConvertScene(scene, settings);

                using (var entities = m_Manager.GetAllEntities(Collections.Allocator.Persistent))
                {
                    Assert.That(entities.Length, Is.EqualTo(1));

                    using (var componentTypeToMatch = new NativeArray<ComponentType>(m_RigEntityComponentType, Allocator.Temp))
                    using (var entityComponentTypes = m_Manager.GetComponentTypes(entities[0]))
                    {
                        Assert.That(entityComponentTypes, Is.EquivalentTo(componentTypeToMatch));
                    }
                }
            }
        }

        [Test]
        public void CanConvertRigTransformHierarchyWithConvertedComponent()
        {
            var scene = SceneManager.GetActiveScene();

            var root = CreateGameObject();
            root.transform.localPosition = new Vector3(1, 2, 3);

            var child1 = CreatePrimitive(PrimitiveType.Cube);
            child1.transform.parent = root.transform;
            child1.transform.localPosition = new Vector3(1, 2, 3);

            var child2 = CreatePrimitive(PrimitiveType.Cube);
            child2.transform.parent = root.transform;
            child2.transform.localPosition = new Vector3(1, 2, 3);

            var child3 = CreatePrimitive(PrimitiveType.Cube);
            child3.transform.parent = root.transform;
            child3.transform.localPosition = new Vector3(1, 2, 3);

            var rigComponent = root.AddComponent<RigComponent>();
            rigComponent.Bones = root.GetComponentsInChildren<Transform>();

            using (var blobAssetStore = new BlobAssetStore())
            {
                var settings = GameObjectConversionSettings.FromWorld(World, blobAssetStore);
                GameObjectConversionUtility.ConvertScene(scene, settings);

                using (var entities = m_Manager.GetAllEntities(Collections.Allocator.Persistent))
                {
                    Assert.That(entities.Length, Is.EqualTo(4));

                    ComponentType[] expectedComponentType = new[]
                    {
                        ComponentType.ReadWrite<Parent>(),
                        ComponentType.ReadWrite<LocalToParent>(),
                        ComponentType.ReadWrite<LocalToWorld>(),
                        ComponentType.ReadWrite<Rotation>(),
                        ComponentType.ReadWrite<Translation>()
                    };

                    using (var rigComponentTypeToMatch = new NativeArray<ComponentType>(m_RigEntityComponentType, Allocator.Temp))
                    using (var componentTypeToMatch = new NativeArray<ComponentType>(expectedComponentType, Allocator.Temp))
                    {
                        for (int i = 0; i < entities.Length; i++)
                        {
                            if (m_Manager.HasComponent<Rig>(entities[i]))
                            {
                                using (var entityComponentTypes = m_Manager.GetComponentTypes(entities[i]))
                                {
                                    Assert.That(entityComponentTypes, Is.EquivalentTo(rigComponentTypeToMatch));
                                }
                            }
                            else
                            {
                                using (var entityComponentTypes = m_Manager.GetComponentTypes(entities[i]))
                                {
                                    Assert.That(entityComponentTypes, Is.SupersetOf(componentTypeToMatch));
                                }
                            }
                        }
                    }
                }
            }
        }

        [Test]
        public void CanConvertRigTransformHierarchyWithRootTransformAndRigComponentOnDifferentGameObject()
        {
            var scene = SceneManager.GetActiveScene();

            var root = CreateGameObject();
            root.transform.localPosition = new Vector3(1, 2, 3);

            var child1 = CreateGameObject();
            child1.transform.parent = root.transform;
            child1.transform.localPosition = new Vector3(1, 2, 3);

            var child2 = CreateGameObject();
            child2.transform.parent = child1.transform;
            child2.transform.localPosition = new Vector3(1, 2, 3);

            var child3 = CreateGameObject();
            child3.transform.parent = child2.transform;
            child3.transform.localPosition = new Vector3(1, 2, 3);

            var child4 = CreatePrimitive(PrimitiveType.Cube);
            child4.transform.parent = child3.transform;
            child4.transform.localPosition = new Vector3(1, 2, 3);

            var rigComponent = root.AddComponent<RigComponent>();
            rigComponent.Bones = child2.GetComponentsInChildren<Transform>();

            using (var blobAssetStore = new BlobAssetStore())
            {
                var settings = GameObjectConversionSettings.FromWorld(World, blobAssetStore);
                GameObjectConversionUtility.ConvertScene(scene, settings);

                using (var entities = m_Manager.GetAllEntities(Collections.Allocator.Persistent))
                {
                    Assert.That(entities.Length, Is.EqualTo(4));

                    ComponentType[] expectedComponentType = new[]
                    {
                        ComponentType.ReadWrite<Parent>(),
                        ComponentType.ReadWrite<LocalToParent>(),
                        ComponentType.ReadWrite<LocalToWorld>(),
                        ComponentType.ReadWrite<Rotation>(),
                        ComponentType.ReadWrite<Translation>()
                    };

                    using (var rigComponentTypeToMatch = new NativeArray<ComponentType>(m_RigEntityComponentType, Allocator.Temp))
                    using (var componentTypeToMatch = new NativeArray<ComponentType>(expectedComponentType, Allocator.Temp))
                    {
                        for (int i = 0; i < entities.Length; i++)
                        {
                            if (m_Manager.HasComponent<Rig>(entities[i]))
                            {
                                using (var entityComponentTypes = m_Manager.GetComponentTypes(entities[i]))
                                {
                                    Assert.That(entityComponentTypes, Is.EquivalentTo(rigComponentTypeToMatch));
                                }
                            }
                            else
                            {
                                using (var entityComponentTypes = m_Manager.GetComponentTypes(entities[i]))
                                {
                                    Assert.That(entityComponentTypes, Is.SupersetOf(componentTypeToMatch));
                                }
                            }
                        }
                    }
                }
            }
        }

        internal struct ReadTransformHandle : IReadTransformHandle
        {
            public Entity Entity { get; set; }
            public int Index { get; set; }
        }

        internal struct WriteTransformHandle : IWriteTransformHandle
        {
            public Entity Entity { get; set; }
            public int Index { get; set; }
        }

        internal class TestReadTransformHandle : ReadExposeTransform<ReadTransformHandle>
        {
        }

        internal class TestWriteTransformHandle : WriteExposeTransform<WriteTransformHandle>
        {
        }

        [Test]
        public void CanConvertRigTransformHierarchyWithExposeTransforms()
        {
            var scene = SceneManager.GetActiveScene();

            var root = CreateGameObject();
            root.transform.localPosition = new Vector3(1, 2, 3);

            var child1 = CreateGameObject();
            child1.transform.parent = root.transform;
            child1.transform.localPosition = new Vector3(1, 2, 3);
            child1.AddComponent<TestWriteTransformHandle>();

            var child2 = CreateGameObject();
            child2.transform.parent = root.transform;
            child2.transform.localPosition = new Vector3(1, 2, 3);
            child2.AddComponent<TestReadTransformHandle>();

            var child3 = CreateGameObject();
            child3.transform.parent = root.transform;
            child3.transform.localPosition = new Vector3(1, 2, 3);
            child3.AddComponent<TestWriteTransformHandle>();

            var rigComponent = root.AddComponent<RigComponent>();
            rigComponent.Bones = root.GetComponentsInChildren<Transform>();

            using (var blobAssetStore = new BlobAssetStore())
            {
                var settings = GameObjectConversionSettings.FromWorld(World, blobAssetStore);
                GameObjectConversionUtility.ConvertScene(scene, settings);

                using (var entities = m_Manager.GetAllEntities(Collections.Allocator.Persistent))
                {
                    Assert.That(entities.Length, Is.EqualTo(4));

                    ComponentType[] expectedComponentType = new[]
                    {
                        ComponentType.ReadWrite<Parent>(),
                        ComponentType.ReadWrite<LocalToParent>(),
                        ComponentType.ReadWrite<LocalToWorld>(),
                        ComponentType.ReadWrite<Rotation>(),
                        ComponentType.ReadWrite<Translation>()
                    };


                    using (var rigComponentTypeToMatch = new NativeArray<ComponentType>(m_RigEntityComponentType, Allocator.Temp))
                    using (var componentTypeToMatch = new NativeArray<ComponentType>(expectedComponentType, Allocator.Temp))
                    {
                        for (int i = 0; i < entities.Length; i++)
                        {
                            if (m_Manager.HasComponent<Rig>(entities[i]))
                            {
                                using (var entityComponentTypes = m_Manager.GetComponentTypes(entities[i]))
                                {
                                    // Using SupersetOf since expose transform also add IBufferElement on the rig
                                    Assert.That(entityComponentTypes, Is.SupersetOf(rigComponentTypeToMatch));
                                }
                            }
                            else
                            {
                                using (var entityComponentTypes = m_Manager.GetComponentTypes(entities[i]))
                                {
                                    // Using SupersetOf since write expose transform also have the WriteGroup override components
                                    Assert.That(entityComponentTypes, Is.SupersetOf(componentTypeToMatch));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
#endif
