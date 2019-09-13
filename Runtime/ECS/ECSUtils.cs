using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;

namespace Unity.Animation
{
    public static class ECSUtils
    {
        public static void UpdateSharedComponentDataHashMap<T>(ref NativeHashMap<int, T> map, EntityManager manager, Allocator allocator)
            where T : struct, ISharedComponentData
        {
            var sharedValues = new List<T>();
            var sharedIndex = new List<int>();
            manager.GetAllUniqueSharedComponentData(sharedValues, sharedIndex);

            if (map.IsCreated && map.Capacity < sharedValues.Count)
                map.Dispose();

            if (!map.IsCreated)
                map = new NativeHashMap<int, T>(sharedValues.Count, allocator);
            else
                map.Clear();

            for (int i = 0; i != sharedValues.Count; ++i)
                map.TryAdd(sharedIndex[i], sharedValues[i]);
        }
    }
}
