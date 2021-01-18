using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    internal static class NativeArrayUniqueExtension
    {
        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        internal struct DefaultComparer<T> : IComparer<T> where T : IComparable<T>
        {
            public int Compare(T x, T y) => x.CompareTo(y);
        }

        // Default Comparer
        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        public unsafe static int Unique<T>(T* array, int length) where T : unmanaged, IComparable<T>
        {
            return Unique(array, length, new DefaultComparer<T>());
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        public unsafe static int Unique<T>(this NativeArray<T> array) where T : struct, IComparable<T>
        {
            return Unique<T, DefaultComparer<T>>(array.GetUnsafePtr(), array.Length, new DefaultComparer<T>());
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(DefaultComparer<int>) })]
        public unsafe static int Unique<T, U>(T* array, int length, U comp)
            where T : unmanaged
            where U : IComparer<T>
        {
            return Unique<T, U>((void*)array, length, comp);
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(DefaultComparer<int>) })]
        public unsafe static int Unique<T, U>(this NativeArray<T> array, U comp)
            where T : struct
            where U : IComparer<T>
        {
            return Unique<T, U>(array.GetUnsafePtr(), array.Length, comp);
        }

        unsafe static int Unique<T, U>(void* array, int length, U comp)
            where T : struct
            where U : IComparer<T>
        {
            if (length == 0)
                return 0;

            int result = 0;
            for (int i = 1; i < length; i++)
            {
                var resultElement = UnsafeUtility.ReadArrayElement<T>(array, result);
                var element = UnsafeUtility.ReadArrayElement<T>(array, i);
                if (comp.Compare(resultElement, element) != 0 && ++result != i)
                {
                    resultElement = Swap<T>(array, result, i);
                }
            }
            return ++result;
        }

        unsafe static T Swap<T>(void* array, int lhs, int rhs) where T : struct
        {
            T val = UnsafeUtility.ReadArrayElement<T>(array, lhs);
            UnsafeUtility.WriteArrayElement<T>(array, lhs, UnsafeUtility.ReadArrayElement<T>(array, rhs));
            UnsafeUtility.WriteArrayElement<T>(array, rhs, val);
            return val;
        }
    }
}
