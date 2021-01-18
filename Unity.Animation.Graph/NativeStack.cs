using System;
using System.Runtime.InteropServices;

namespace Unity.Collections
{
    /// <summary>
    /// An unmanaged, resizable stack.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the container.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct NativeStack<T>
        where T : struct
    {
        NativeList<T> m_List;

        /// <summary>
        /// Constructs a new stack with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the stack. If the stack grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        public NativeStack(int initialCapacity, Allocator allocator)
        {
            m_List = new NativeList<T>(initialCapacity, allocator);
        }

        /// <summary>
        /// The current number of items in the stack.
        /// </summary>
        /// <value>The item count.</value>
        public int Length
        {
            get
            {
                return m_List.Length;
            }
        }

        /// <summary>
        /// The number of items that can fit in the stack.
        /// </summary>
        /// <value>The number of items that the stack can hold before it resizes its internal storage.</value>
        /// <remarks>Capacity specifies the number of items the stack can currently hold. You can change Capacity
        /// to fit more or fewer items. Changing Capacity creates a new array of the specified size, copies the
        /// old array to the new one, and then deallocates the original array memory. You cannot change the Capacity
        /// to a size smaller than <see cref="Length"/> (remove unwanted elements from the list first).</remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if Capacity is set smaller than Length.</exception>
        public int Capacity
        {
            get
            {
                return m_List.Capacity;
            }

            set
            {
                m_List.Capacity = value;
            }
        }

        /// <summary>
        /// Reports whether container is empty.
        /// </summary>
        /// <value>True if this container empty.</value>
        public bool IsEmpty => !m_List.IsCreated || Length == 0;


        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// </summary>
        /// <value>True if this container object's internal storage has been allocated.</value>
        /// <remarks>Note that the container storage is not created if you use the default constructor. You must specify
        /// at least an allocation type to construct a usable container.</remarks>
        public bool IsCreated => m_List.IsCreated;

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
            m_List.Dispose();
        }

        /// <summary>
        ///  Pushes the given element value to the top of the stack.
        /// </summary>
        /// <param name="value">The struct to be added at the top of the stack.</param>
        /// <remarks>If the stack has reached its current capacity, it copies the original, internal array to
        /// a new, larger array, and then deallocates the original.
        /// </remarks>
        public void Push(in T value)
        {
            m_List.Add(value);
        }

        /// <summary>
        ///  Removes the top element from the stack.
        /// </summary>
        public void Pop()
        {
            m_List.RemoveAt(m_List.Length - 1);
        }

        /// <summary>
        /// Returns the top element in the stack. This is the most recently pushed element. This element will be removed on a call to pop().
        /// </summary>
        public T Top()
        {
            return m_List[m_List.Length - 1];
        }

        /// <summary>
        /// Clears the stack.
        /// </summary>
        /// <remarks>Stack <see cref="Capacity"/> remains unchanged.</remarks>
        public void Clear()
        {
            m_List.Clear();
        }
    }
}
