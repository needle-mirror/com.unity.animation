using NUnit.Framework;
using Unity.Mathematics;

namespace Unity.Animation.Tests
{
    public class AffineTransformTests
    {
        [Test]
        public void CanInverseAffineTransform()
        {
            const float tolerance = 1e-6f;
            var floatComparer = new FloatAbsoluteEqualityComparer(tolerance);

            AffineTransform tx = mathex.AffineTransform(math.float3(5, 6, 7), math.normalize(math.float4(1, 2, 3, 4)), math.float3(2, 3, 4));
            AffineTransform invTx = mathex.inverse(tx);
            AffineTransform identity = mathex.mul(tx, invTx);

            Assert.That(identity.rs.c0.x, Is.EqualTo(1f).Using(floatComparer));
            Assert.That(identity.rs.c0.y, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity.rs.c0.z, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity.rs.c1.x, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity.rs.c1.y, Is.EqualTo(1f).Using(floatComparer));
            Assert.That(identity.rs.c1.z, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity.rs.c2.x, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity.rs.c2.y, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity.rs.c2.z, Is.EqualTo(1f).Using(floatComparer));

            Assert.That(identity.t.x, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity.t.y, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity.t.z, Is.EqualTo(0f).Using(floatComparer));
        }

        [Test]
        public void InverseOfZeroRSAffineTransformReturnsZero()
        {
            const float tolerance = 1e-30f;
            var floatComparer = new FloatAbsoluteEqualityComparer(tolerance);

            AffineTransform tx = mathex.AffineTransform(math.float3(5, 6, 7), math.normalize(math.float4(1, 2, 3, 4)), float3.zero);
            AffineTransform invTx = mathex.inverse(tx);

            Assert.That(invTx.rs.c0.x, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(invTx.rs.c0.y, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(invTx.rs.c0.z, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(invTx.rs.c1.x, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(invTx.rs.c1.y, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(invTx.rs.c1.z, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(invTx.rs.c2.x, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(invTx.rs.c2.y, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(invTx.rs.c2.z, Is.EqualTo(0f).Using(floatComparer));

            Assert.That(invTx.t.x, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(invTx.t.y, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(invTx.t.z, Is.EqualTo(0f).Using(floatComparer));
        }

        [Test]
        public void CanInverseAffineTransformWithPicoScale()
        {
            // slightly larger tolerance (vs 1e-6f) for PS4
            const float tolerance = 2e-6f;
            var floatComparer = new FloatAbsoluteEqualityComparer(tolerance);

            AffineTransform tx = mathex.AffineTransform(math.float3(5, 6, 7), math.normalize(math.float4(1, 2, 3, 4)), math.float3(1e-12f, 1e-12f, 1e-12f));
            AffineTransform invTx = mathex.inverse(tx);
            AffineTransform identity = mathex.mul(tx, invTx);

            Assert.That(identity.rs.c0.x, Is.EqualTo(1f).Using(floatComparer));
            Assert.That(identity.rs.c0.y, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity.rs.c0.z, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity.rs.c1.x, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity.rs.c1.y, Is.EqualTo(1f).Using(floatComparer));
            Assert.That(identity.rs.c1.z, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity.rs.c2.x, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity.rs.c2.y, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity.rs.c2.z, Is.EqualTo(1f).Using(floatComparer));

            Assert.That(identity.t.x, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity.t.y, Is.EqualTo(0f).Using(floatComparer));
            Assert.That(identity.t.z, Is.EqualTo(0f).Using(floatComparer));
        }

        // Following test fails because we don't gracefully handle ill-conditioned matrices
        // We can uncomment these once we bring SVD support

        /*
        [Test]
        public void CanInverseSingularAffineTransform()
        {
            // it needs a bit larger tolerance for singular
            // since it uses the svd iterative solver
            const float tolerance = 1e-1f;
            var floatComparer = new FloatAbsoluteEqualityComparer(tolerance);

            AffineTransform tx = mathex.AffineTransform(math.float3(5, 6, 7), math.normalize(math.float4(1, 2, 3, 4)), math.float3(0, 3, 4));
            AffineTransform invTx = mathex.inverse(tx);

            // pseudo inverse penrose #1 test
            AffineTransform testTx = mathex.mul(mathex.mul(tx, invTx), tx);

            Assert.That(testTx.rs.c0.x, Is.EqualTo(tx.rs.c0.x).Using(floatComparer));
            Assert.That(testTx.rs.c0.y, Is.EqualTo(tx.rs.c0.y).Using(floatComparer));
            Assert.That(testTx.rs.c0.z, Is.EqualTo(tx.rs.c0.z).Using(floatComparer));
            Assert.That(testTx.rs.c1.x, Is.EqualTo(tx.rs.c1.x).Using(floatComparer));
            Assert.That(testTx.rs.c1.y, Is.EqualTo(tx.rs.c1.y).Using(floatComparer));
            Assert.That(testTx.rs.c1.z, Is.EqualTo(tx.rs.c1.z).Using(floatComparer));
            Assert.That(testTx.rs.c2.x, Is.EqualTo(tx.rs.c2.x).Using(floatComparer));
            Assert.That(testTx.rs.c2.y, Is.EqualTo(tx.rs.c2.y).Using(floatComparer));
            Assert.That(testTx.rs.c2.z, Is.EqualTo(tx.rs.c2.z).Using(floatComparer));

            Assert.That(testTx.t.x, Is.EqualTo(tx.t.x).Using(floatComparer));
            Assert.That(testTx.t.y, Is.EqualTo(tx.t.y).Using(floatComparer));
            Assert.That(testTx.t.z, Is.EqualTo(tx.t.z).Using(floatComparer));
        }
        */
    }
}
