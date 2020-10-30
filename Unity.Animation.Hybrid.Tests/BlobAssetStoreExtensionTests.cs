#if UNITY_EDITOR

using NUnit.Framework;
using UnityEngine;
using Unity.Entities;
using Unity.Animation.Hybrid;

namespace Unity.Animation.Tests
{
    public class BlobAssetStoreExtensionTests
    {
        BlobAssetStore m_Store;

        RigComponent CreateSimpleRigComponentAndHierarchy(string name)
        {
            var go = new GameObject(name);
            var rigComponent = go.AddComponent<RigComponent>();

            var root = new GameObject("root");
            var child = new GameObject("child");
            root.transform.parent = go.transform;
            child.transform.parent = root.transform;

            rigComponent.Bones = new Transform[]
            {
                root.transform,
                child.transform
            };

            rigComponent.TranslationChannels = new Hybrid.TranslationChannel[]
            {
                new Hybrid.TranslationChannel { DefaultValue = new Vector3(1f, 1f, 1f), Id = "translation" }
            };

            rigComponent.RotationChannels = new Hybrid.RotationChannel[]
            {
                new Hybrid.RotationChannel { DefaultValue = Quaternion.identity, Id = "rotation" }
            };

            rigComponent.ScaleChannels = new Hybrid.ScaleChannel[]
            {
                new Hybrid.ScaleChannel { DefaultValue = new Vector3(1f, 1f, 1f), Id = "scale" }
            };

            rigComponent.FloatChannels = new Hybrid.FloatChannel[]
            {
                new Hybrid.FloatChannel { DefaultValue = 5f, Id = "float" }
            };

            rigComponent.IntChannels = new Hybrid.IntChannel[]
            {
                new Hybrid.IntChannel { DefaultValue = 10, Id = "int" }
            };

            return rigComponent;
        }

        [SetUp]
        public void Setup()
        {
            m_Store = new BlobAssetStore();
        }

        [TearDown]
        public void TearDown()
        {
            m_Store.Dispose();
        }

        [Test]
        public void GetClip_SameAnimationClip_ReturnsSameBlob()
        {
            var clip = new AnimationClip();
            var constantCurve = UnityEngine.AnimationCurve.Constant(0.0f, 1.0f, 1.0f);
            clip.SetCurve("", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("", typeof(Transform), "m_LocalPosition.y", constantCurve);
            clip.SetCurve("", typeof(Transform), "m_LocalPosition.z", constantCurve);

            var blobA = m_Store.GetClip(clip);
            var blobB = m_Store.GetClip(clip);
            Assert.That(blobA == blobB); // ptr check
        }

        [Test]
        public void GetClip_DifferentAnimationClips_ReturnsDifferentBlobs()
        {
            var clipA = new AnimationClip();
            var constantCurveA = UnityEngine.AnimationCurve.Constant(0.0f, 1.0f, 1.0f);
            clipA.SetCurve("", typeof(Transform), "m_LocalPosition.x", constantCurveA);
            clipA.SetCurve("", typeof(Transform), "m_LocalPosition.y", constantCurveA);
            clipA.SetCurve("", typeof(Transform), "m_LocalPosition.z", constantCurveA);

            var clipB = new AnimationClip();
            var constantCurveB = UnityEngine.AnimationCurve.Constant(0.0f, 1.0f, 2.0f);
            clipB.SetCurve("", typeof(Transform), "m_LocalPosition.x", constantCurveB);
            clipB.SetCurve("", typeof(Transform), "m_LocalPosition.y", constantCurveB);
            clipB.SetCurve("", typeof(Transform), "m_LocalPosition.z", constantCurveB);

            var blobA = m_Store.GetClip(clipA);
            var blobB = m_Store.GetClip(clipB);
            Assert.That(blobA != blobB); // ptr check
        }

        [Test]
        public void GetClip_NullAnimationClip_ReturnsNullBlob()
        {
            Assert.That(m_Store.GetClip(null) == BlobAssetReference<Clip>.Null);
        }

        [Test]
        public void GetRigDefinition_SameRigComponent_ReturnsSameBlob()
        {
            var rigComponent = CreateSimpleRigComponentAndHierarchy("rig");
            var blobA = m_Store.GetRigDefinition(rigComponent);
            var blobB = m_Store.GetRigDefinition(rigComponent);

            Assert.That(blobA == blobB); // ptr check
            Object.DestroyImmediate(rigComponent);
        }

        [Test]
        public void GetRigDefinition_DifferentRigComponent_ReturnsDifferentBlobs()
        {
            var rigComponentA = CreateSimpleRigComponentAndHierarchy("rigA");
            var rigComponentB = CreateSimpleRigComponentAndHierarchy("rigB");
            var blobA = m_Store.GetRigDefinition(rigComponentA);
            var blobB = m_Store.GetRigDefinition(rigComponentB);

            Assert.That(blobA != blobB); // ptr check
            Object.DestroyImmediate(rigComponentA);
            Object.DestroyImmediate(rigComponentB);
        }

        [Test]
        public void GetRigDefinition_NullRigComponent_ReturnsNullBlob()
        {
            Assert.That(m_Store.GetRigDefinition(null) == BlobAssetReference<RigDefinition>.Null);
        }
    }
}

#endif
