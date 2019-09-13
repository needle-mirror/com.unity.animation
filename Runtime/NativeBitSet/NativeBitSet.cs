using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Collections
{
    [StructLayout (LayoutKind.Sequential)]
    public unsafe struct NativeBitSet : IDisposable, IEquatable<NativeBitSet>
    {
        [NativeDisableUnsafePtrRestriction]
        private long*       m_Memory;
        int                 m_BitsCount;

        public NativeBitSet(int num_bits, Allocator label)
        {
            m_BitsCount = num_bits;
            AllocationLabel = label;
            m_Memory = (long*) UnsafeUtility.Malloc(calc_num_items(m_BitsCount) * sizeof(long), UnsafeUtility.AlignOf<long>(), AllocationLabel);
            UnsafeUtility.MemClear(m_Memory, calc_num_items(m_BitsCount) * sizeof(long));
        }

        public bool IsCreated => m_Memory != null;

        public Allocator AllocationLabel { get; private set; }

        public int Length
        {
            get
            {
                return m_BitsCount;
            }
        }

        public void Dispose()
        {
            UnsafeUtility.Free(m_Memory, AllocationLabel);
        }

        public void Set(int bit)
        {
            CheckIndexOutOfRange(bit);

            m_Memory[word(bit)] |= mask1(bit);
        }

        public void Set()
        {
            var count = calc_num_items(m_BitsCount);
            for(int i=0;i<count;i++)
            {
                m_Memory[i] = ~0L;
            }
            ZeroUnusedBits();
        }

        public void Reset(int bit)
        {
            CheckIndexOutOfRange(bit);

            m_Memory[word(bit)] &= mask0(bit);
        }

        public void Reset()
        {
            var count = calc_num_items(m_BitsCount);
            for(int i=0;i<count;i++)
            {
                m_Memory[i] = 0L;
            }
            ZeroUnusedBits();
        }

        public void Flip(int bit)
        {
            CheckIndexOutOfRange(bit);

            m_Memory[word(bit)] ^= mask1(bit);
        }

        public void Flip()
        {
            var count = calc_num_items(m_BitsCount);
            for(int i=0;i<count;i++)
               m_Memory[i] = ~m_Memory[i];
            ZeroUnusedBits();
        }

        public bool Test(int bit)
        {
            CheckIndexOutOfRange(bit);

            return (m_Memory[word(bit)] & mask1(bit)) != 0L;
        }

        public bool Any()
        {
            var count = calc_num_items(m_BitsCount);
            for(int i=0;i<count;i++)
            {
                if (m_Memory[i] != 0L)
                    return true;
            }
            return false;
        }

        public bool None()
        {
            return !Any();
        }

        public int CountBits()
        {
            int sum = 0;
            var count = calc_num_items(m_BitsCount);
            for(int i=0;i<count;i++)
            {
                sum += math.countbits(m_Memory[i]);
            }
            return sum;
        }

        void ZeroUnusedBits()
        {
            // number of bits used in the last block
            int used_bits = offset(m_BitsCount);
            var count = calc_num_items(m_BitsCount);
            if (used_bits != 0)
                m_Memory[count - 1] &= ~(~0L << used_bits);
        }

        void CheckIndexOutOfRange(int bit)
        {
            if ((uint)bit >= (uint)m_BitsCount)
                throw new System.IndexOutOfRangeException($"bit {bit} is out of range in NativeBitSet of '{m_BitsCount}' Length.");
        }

        public NativeBitSet Copy()
        {
            if (!IsCreated)
                throw new ObjectDisposedException("");

            NativeBitSet ret = new NativeBitSet(m_BitsCount, Allocator.Persistent);

            var count = calc_num_items(m_BitsCount);
            for(int i=0;i<count;i++)
            {
               ret.m_Memory[i] = m_Memory[i];
            }

            return ret;
        }

        public bool Equals(NativeBitSet other)
        {
            if (!IsCreated || !other.IsCreated)
                return false;

            if (m_BitsCount != other.m_BitsCount)
                return false;

            var count = calc_num_items(m_BitsCount);
            for(int i=0;i<count;i++)
            {
                if (other.m_Memory[i] != m_Memory[i])
                    return false;
            }

            return true;
        }

        const int bits_per_item = 8 * sizeof(long);
        static int word(int bit)  { return bit / bits_per_item; }
        static int offset(int bit){ return bit % bits_per_item; }
        static long mask1(int bit) { return 1L << offset(bit); }
        static long mask0(int bit) { return ~(1L << offset(bit)); }
        static int calc_num_items(int num_bits)
        {
           return (num_bits + bits_per_item - 1) / bits_per_item;
        }
    }
}
