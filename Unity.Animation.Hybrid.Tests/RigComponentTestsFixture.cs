using NUnit.Framework;
using UnityEngine;
using Unity.Animation.Hybrid;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Unity.Animation.Tests
{
    abstract class RigComponentTestsFixture
    {
        protected RigComponent  rigComponent;
        protected Transform     preExistingChildBone;

        protected List<GameObject> createdGameObjects = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            createdGameObjects.Clear();

            var gameObject = new GameObject("RigComponent");
            preExistingChildBone = new GameObject("pre-existing child").transform;
            preExistingChildBone.parent = gameObject.transform;
            rigComponent = gameObject.AddComponent<RigComponent>();

            createdGameObjects.Add(gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in createdGameObjects)
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
            createdGameObjects.Add(go);
            return go.transform;
        }

        protected Transform CreateBoneNotInHierarchy()
        {
            return CreateTransformWithName("bone NOT in hierarchy");
        }

        protected Transform CreateBoneInHierarchy(RigComponent rigComponent)
        {
            var transform = CreateTransformWithName("bone in hierarchy");
            transform.parent = rigComponent.transform;
            return transform;
        }

        protected Transform CreateBoneInHierarchy(Transform parent)
        {
            var transform = CreateTransformWithName("bone in hierarchy");
            transform.parent = parent;
            return transform;
        }

        protected Transform CreateDestroyedBone()
        {
            var destroyedGameObject = new GameObject("destroyed bone");
            var destroyedTransform = destroyedGameObject.transform;
            Destroy(destroyedGameObject);
            return destroyedTransform;
        }
    }
}
