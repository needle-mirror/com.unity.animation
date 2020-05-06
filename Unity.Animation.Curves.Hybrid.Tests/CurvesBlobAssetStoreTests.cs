using Unity.Entities;
using NUnit.Framework;
using Unity.Animation.Hybrid;

namespace Unity.Animation.Tests
{
    public class CurvesBlobAssetStoreTests
    {
        BlobAssetStore m_Store;

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
        public void GetAnimationCurve_SameCurves_ReturnsSameBlob()
        {
            var curveA = UnityEngine.AnimationCurve.Linear(0, 0, 1, 1);
            var blobA = m_Store.GetAnimationCurve(curveA);
            var blobB = m_Store.GetAnimationCurve(curveA);
            Assert.That(blobA == blobB); // ptr check
        }

        [Test]
        public void GetAnimationCurve_SameCurveData_ReturnsSameBlob()
        {
            var curveA = UnityEngine.AnimationCurve.Linear(0, 0, 1, 1);
            var curveB = UnityEngine.AnimationCurve.Linear(0, 0, 1, 1);
            var blobA = m_Store.GetAnimationCurve(curveA);
            var blobB = m_Store.GetAnimationCurve(curveB);
            Assert.That(blobA == blobB); // ptr check
        }

        [Test]
        public void GetAnimationCurve_DifferentCurves_ReturnsDifferentBlobs()
        {
            var curveA = UnityEngine.AnimationCurve.Linear(0, 0, 1, 1);
            var curveB = UnityEngine.AnimationCurve.EaseInOut(0, 0, 1, 1);

            var blobA = m_Store.GetAnimationCurve(curveA);
            var blobB = m_Store.GetAnimationCurve(curveB);
            Assert.That(blobA != blobB); // ptr check
        }

        [Test]
        public void GetAnimationCurve_NullCurve_ReturnsNullBlob()
        {
            Assert.That(m_Store.GetAnimationCurve(null) == BlobAssetReference<AnimationCurveBlob>.Null);
        }
    }
}
