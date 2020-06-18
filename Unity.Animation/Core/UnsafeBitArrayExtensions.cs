using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animation
{
    static internal class UnsafeBitArrayExtensions
    {
        const int k_SizeMultiple = 64;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void AssertSameLength(ref UnsafeBitArray source, ref UnsafeBitArray destination)
        {
            if (source.Length != destination.Length)
                throw new ArgumentException($"UnsafeBitArray must be of the same length: {source.Length} vs {destination.Length}.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void AssertLengthIsMultipleOf64(ref UnsafeBitArray bits)
        {
            if ((bits.Length & (k_SizeMultiple - 1)) != 0)
                throw new ArgumentException($"UnsafeBitArray length must be a multiple of 64: {bits.Length}.");
        }

        /// <summary>
        /// Set all bits to desired boolean value.
        /// </summary>
        /// <param name="value">Value of bits to set.</param>
        internal static void SetBits(this ref UnsafeBitArray bits, bool value)
        {
            bits.SetBits(0, value, bits.Length);
        }

        /// <summary>
        /// Copy all bits from source.
        /// </summary>
        /// <param name="src">Bits to copy from.</param>
        /// <remarks>
        /// Source bit array should be of same length.
        /// </remarks>
        internal static void CopyFrom(this ref UnsafeBitArray bits, ref UnsafeBitArray src)
        {
            AssertSameLength(ref bits, ref src);
#if UNITY_ENTITIES_0_12_OR_NEWER
            bits.Copy(0, ref src, 0, bits.Length);
#else
            for (int i = 0; i < bits.Length; i += k_SizeMultiple)
            {
                bits.SetBits(i, src.GetBits(i, k_SizeMultiple), k_SizeMultiple);
            }
#endif
        }

        /// <summary>
        /// OR with all bits from source.
        /// </summary>
        /// <param name="src">Input bits.</param>
        /// <remarks>
        /// This method only works with UnsafeBitArray of 64 bit multiples.
        /// Source bit array should be of same length.
        /// </remarks>
        internal static void OrBits64(this ref UnsafeBitArray bits, ref UnsafeBitArray src)
        {
            AssertSameLength(ref bits, ref src);
            AssertLengthIsMultipleOf64(ref src);

            var length = src.Length;
            for (int i = 0; i < length; i += k_SizeMultiple)
            {
                var value = bits.GetBits(i, k_SizeMultiple) | src.GetBits(i, k_SizeMultiple);
                bits.SetBits(i, value, k_SizeMultiple);
            }
        }

        /// <summary>
        /// OR all bits from lhs with rhs and store result. Both lhs and rhs parameters are not modified.
        /// </summary>
        /// <param name="lhs">Input bits.</param>
        /// <param name="rhs">Input bits.</param>
        /// <remarks>
        /// This method only works with UnsafeBitArray of 64 bit multiples and does not modify lhs or rhs.
        /// All UnsafeBitArray must be of same size.
        /// </remarks>
        internal static void OrBits64(this ref UnsafeBitArray bits, ref UnsafeBitArray lhs, ref UnsafeBitArray rhs)
        {
            AssertSameLength(ref bits, ref lhs);
            AssertSameLength(ref lhs, ref rhs);
            AssertLengthIsMultipleOf64(ref lhs);

            var length = lhs.Length;
            for (int i = 0; i < length; i += k_SizeMultiple)
            {
                var value = lhs.GetBits(i, k_SizeMultiple) | rhs.GetBits(i, k_SizeMultiple);
                bits.SetBits(i, value, k_SizeMultiple);
            }
        }

        /// <summary>
        /// AND with all bits from source.
        /// </summary>
        /// <param name="src">Input bits.</param>
        /// <remarks>
        /// This method only works with UnsafeBitArray of 64 bit multiples.
        /// Source bit array should be of same length.
        /// </remarks>
        internal static void AndBits64(this ref UnsafeBitArray bits, ref UnsafeBitArray src)
        {
            AssertSameLength(ref bits, ref src);
            AssertLengthIsMultipleOf64(ref src);

            var length = src.Length;
            for (int i = 0; i < length; i += k_SizeMultiple)
            {
                var value = bits.GetBits(i, k_SizeMultiple) & src.GetBits(i, k_SizeMultiple);
                bits.SetBits(i, value, k_SizeMultiple);
            }
        }

        /// <summary>
        /// OR all bits from lhs with rhs and store result. Both lhs and rhs parameters are not modified.
        /// </summary>
        /// <param name="lhs">Input bits.</param>
        /// <param name="rhs">Input bits.</param>
        /// <remarks>
        /// This method only works with UnsafeBitArray of 64 bit multiples and does not modify lhs or rhs.
        /// All UnsafeBitArray must be of same size.
        /// </remarks>
        internal static void AndBits64(this ref UnsafeBitArray bits, ref UnsafeBitArray lhs, ref UnsafeBitArray rhs)
        {
            AssertSameLength(ref bits, ref lhs);
            AssertSameLength(ref lhs, ref rhs);
            AssertLengthIsMultipleOf64(ref lhs);

            var length = lhs.Length;
            for (int i = 0; i < length; i += k_SizeMultiple)
            {
                var value = lhs.GetBits(i, k_SizeMultiple) & rhs.GetBits(i, k_SizeMultiple);
                bits.SetBits(i, value, k_SizeMultiple);
            }
        }

        /// <summary>
        /// Invert all bits.
        /// </summary>
        /// <remarks>
        /// This method only works with UnsafeBitArray of 64 bit multiples.
        /// </remarks>
        internal static void InvertBits64(this ref UnsafeBitArray bits)
        {
            AssertLengthIsMultipleOf64(ref bits);

            var length = bits.Length;
            for (int i = 0; i < length; i += k_SizeMultiple)
            {
                bits.SetBits(i, ~bits.GetBits(i, k_SizeMultiple), k_SizeMultiple);
            }
        }
    }
}
