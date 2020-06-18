#if !UNITY_ENTITIES_0_12_OR_NEWER

using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnsafeUtility_Collections = Unity.Collections.LowLevel.Unsafe.UnsafeUtility;

namespace Unity.Animation
{
    struct BufferTypeHandle<T> where T : struct, IBufferElementData
    {
        public ArchetypeChunkBufferType<T> Value;
    }

    struct ComponentTypeHandle<T> where T : struct, IComponentData
    {
        public ArchetypeChunkComponentType<T> Value;
    }

    static class ArchetypeChunkExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DidChange<T>(this ArchetypeChunk chunk, BufferTypeHandle<T> chunkBufferType, uint version) where T : struct, IBufferElementData => chunk.DidChange(chunkBufferType.Value, version);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeArray<T> GetNativeArray<T>(this ArchetypeChunk chunk, ComponentTypeHandle<T> chunkComponentType) where T : struct, IComponentData => chunk.GetNativeArray(chunkComponentType.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferAccessor<T> GetBufferAccessor<T>(this ArchetypeChunk chunk, BufferTypeHandle<T> bufferComponentType) where T : struct, IBufferElementData => chunk.GetBufferAccessor(bufferComponentType.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasComponent<T>(this BufferFromEntity<T> buffer, Entity e) where T : struct, IBufferElementData => buffer.Exists(e);
    }

    static class UnsafeUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ref T AsRef<T>(void* ptr) where T : struct => ref UnsafeUtilityEx.AsRef<T>(ptr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void MemClear(void* destination, long size) => UnsafeUtility_Collections.MemClear(destination, size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void MemCpy(void* destination, void* source, long size) => UnsafeUtility_Collections.MemCpy(destination, source, size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int MemCmp(void* ptr1, void* ptr2, long size) => UnsafeUtility_Collections.MemCmp(ptr1, ptr2, size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void MemCpyReplicate(void* destination, void* source, int size, int count) => UnsafeUtility_Collections.MemCpyReplicate(destination, source, size, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>() where T : struct => UnsafeUtility_Collections.SizeOf<T>();
    }
}

#endif
