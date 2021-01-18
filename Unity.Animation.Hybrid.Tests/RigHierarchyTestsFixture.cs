using NUnit.Framework;
using UnityEngine;
using Unity.Animation.Hybrid;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Unity.Animation.Tests
{
    abstract class RigHierarchyTestsFixture<T> where T : Component, IRigAuthoring
    {
        protected T Component;
        private List<GameObject> m_CreatedGameObjects = new List<GameObject>();

        [SetUp]
        public virtual void SetUp()
        {
            m_CreatedGameObjects.Clear();

            var rigHierarchy = CreateTransformWithName(typeof(T).Name);
            Component = rigHierarchy.gameObject.AddComponent<T>();
        }

        [TearDown]
        public virtual void TearDown()
        {
            foreach (var go in m_CreatedGameObjects)
                Destroy(go);
        }

        protected void Destroy(Component component)
        {
            if (!component)
                return;
            Destroy(component.gameObject);
        }

        protected void Destroy(UnityEngine.Object obj)
        {
            if (!obj)
                return;
            UnityEngine.Object.DestroyImmediate(obj);
        }

        protected Transform CreateTransformWithName(string name)
        {
            var go = new GameObject(name);
            m_CreatedGameObjects.Add(go);
            return go.transform;
        }

        protected Transform CreateBoneNotInHierarchy(string name = "bone NOT in hierarchy")
        {
            return CreateTransformWithName(name);
        }

        protected Transform CreateBoneInHierarchy(T component, string name = "bone in hierarchy")
        {
            var transform = CreateTransformWithName(name);
            transform.parent = component.transform;
            return transform;
        }

        protected Transform CreateBoneInHierarchy(Transform parent, string name = "bone in hierarchy")
        {
            var transform = CreateTransformWithName(name);
            transform.parent = parent;
            return transform;
        }

        protected Transform CreateDestroyedBone(string name = "destroyed bone")
        {
            var destroyedGameObject = new GameObject(name);
            var destroyedTransform = destroyedGameObject.transform;
            Destroy(destroyedGameObject);
            return destroyedTransform;
        }
    }

    abstract class RigComponentTestsFixture : RigHierarchyTestsFixture<RigComponent>
    {
        protected Transform    PreExistingChildBone;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            PreExistingChildBone = CreateBoneInHierarchy(Component, "pre-existing child");
        }
    }
}
