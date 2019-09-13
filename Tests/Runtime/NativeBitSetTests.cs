using NUnit.Framework;
using Unity.Collections;

namespace Unity.Animation.Tests
{
    class NativeBitSetTests
    {
        [Test]
        [TestCase(0)]
        [TestCase(10)]
        [TestCase(120)]
        [TestCase(256)]
        [TestCase(64)]
        [TestCase(128)]
        public void CanAllocateNativeBitSet(int numBits)
        {
            var bitset = new NativeBitSet(numBits, Allocator.Persistent);

            Assert.AreEqual(numBits, bitset.Length);
            Assert.IsTrue(bitset.None());
            bitset.Dispose();
        }

        [Test]
        [TestCase(10, 1)]
        [TestCase(120, 78)]
        [TestCase(256, 140)]
        [TestCase(64, 63)]
        [TestCase(128, 127)]
        [TestCase(256, 64)]
        [TestCase(256, 192)]
        public void CanSetBit(int numBits, int bit)
        {
            var bitset = new NativeBitSet(numBits, Allocator.Persistent);

            bitset.Set(bit);

            Assert.AreEqual(numBits, bitset.Length);
            Assert.AreEqual(1, bitset.CountBits());
            Assert.IsTrue(bitset.Test(bit));
            Assert.IsTrue(bitset.Any());
            
            bitset.Dispose();
        }

        [Test]
        [TestCase(10, 10)]
        [TestCase(120, 256)]
        [TestCase(256, -1)]
        [TestCase(64, 64)]
        [TestCase(64, 65)]
        [TestCase(128, 128)]
        [TestCase(128, 129)]
        public void CannotSetBitOutOfRange(int numBits, int bit)
        {
            var bitset = new NativeBitSet(numBits, Allocator.Persistent);

            Assert.Throws<System.IndexOutOfRangeException>(() => bitset.Set(bit));
            
            bitset.Dispose();
        }

        [Test]
        [TestCase(10)]
        [TestCase(120)]
        [TestCase(256)]
        [TestCase(64)]
        [TestCase(128)]
        public void CanSetCompleteBitSet(int numBits)
        {
            var bitset = new NativeBitSet(numBits, Allocator.Persistent);

            bitset.Set();

            Assert.AreEqual(numBits, bitset.Length);
            Assert.AreEqual(numBits, bitset.CountBits());
            Assert.IsTrue(bitset.Any());
            
            bitset.Dispose();
        }


        [Test]
        [TestCase(10, 1)]
        [TestCase(120, 78)]
        [TestCase(256, 140)]
        [TestCase(64, 63)]
        [TestCase(128, 127)]
        [TestCase(256, 0)]
        [TestCase(256, 64)]
        [TestCase(256, 128)]
        [TestCase(256, 192)]
        public void CanResetBit(int numBits, int bit)
        {
            var bitset = new NativeBitSet(numBits, Allocator.Persistent);

            bitset.Set(bit);

            Assert.IsTrue(bitset.Test(bit));

            bitset.Reset(bit);

            Assert.IsFalse(bitset.Test(bit));
            
            bitset.Dispose();
        }

        [Test]
        [TestCase(10, 10)]
        [TestCase(120, 256)]
        [TestCase(256, -1)]
        [TestCase(64, 64)]
        [TestCase(128, 128)]
        public void CannotResetBitOutOfRange(int numBits, int bit)
        {
            var bitset = new NativeBitSet(numBits, Allocator.Persistent);

            Assert.Throws<System.IndexOutOfRangeException>(() => bitset.Reset(bit));
            
            bitset.Dispose();
        }

        [Test]
        [TestCase(10)]
        [TestCase(120)]
        [TestCase(256)]
        [TestCase(64)]
        [TestCase(128)]
        public void CanResetCompleteBitSet(int numBits)
        {
            var bitset = new NativeBitSet(numBits, Allocator.Persistent);

            bitset.Set();

            Assert.AreEqual(numBits, bitset.Length);
            Assert.AreEqual(numBits, bitset.CountBits());

            bitset.Reset();

            Assert.AreEqual(0, bitset.CountBits());
            
            bitset.Dispose();
        }



        [Test]
        [TestCase(10, 1)]
        [TestCase(120, 78)]
        [TestCase(256, 140)]
        [TestCase(64, 63)]
        [TestCase(128, 127)]
        [TestCase(256, 0)]
        [TestCase(256, 64)]
        [TestCase(256, 128)]
        [TestCase(256, 192)]
        public void CanFlipBit(int numBits, int bit)
        {
            var bitset = new NativeBitSet(numBits, Allocator.Persistent);

            Assert.IsFalse(bitset.Test(bit));

            bitset.Flip(bit);

            Assert.IsTrue(bitset.Test(bit));

            bitset.Dispose();
        }

        [Test]
        [TestCase(10, 10)]
        [TestCase(120, 256)]
        [TestCase(256, -1)]
        [TestCase(64, 64)]
        [TestCase(128, 128)]
        public void CannotFlipBitOutOfRange(int numBits, int bit)
        {
            var bitset = new NativeBitSet(numBits, Allocator.Persistent);

            Assert.Throws<System.IndexOutOfRangeException>(() => bitset.Flip(bit));
            
            bitset.Dispose();
        }

        [Test]
        [TestCase(10)]
        [TestCase(120)]
        [TestCase(256)]
        [TestCase(64)]
        [TestCase(128)]
        public void CanFlipCompleteBitSet(int numBits)
        {
            var bitset = new NativeBitSet(numBits, Allocator.Persistent);

            Assert.AreEqual(0, bitset.CountBits());

            bitset.Flip();

            Assert.AreEqual(numBits, bitset.CountBits());

            bitset.Dispose();
        }

        [Test]
        [TestCase(10, 10)]
        [TestCase(120, 256)]
        [TestCase(256, -1)]
        [TestCase(64, 64)]
        [TestCase(128, 128)]
        public void CannotTestBitOutOfRange(int numBits, int bit)
        {
            var bitset = new NativeBitSet(numBits, Allocator.Persistent);

            Assert.Throws<System.IndexOutOfRangeException>(() => bitset.Test(bit));
            
            bitset.Dispose();
        }
    }
}
