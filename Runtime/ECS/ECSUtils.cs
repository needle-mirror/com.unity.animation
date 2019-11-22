using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;

namespace Unity.Animation
{
    public static class ECSUtils
    {
        public static void UpdateSharedComponentDataHashMap<T>(ref NativeHashMap<int, T> map, List<T> sharedValues, List<int> sharedIndex, EntityManager manager, Allocator allocator)
            where T : struct, ISharedComponentData
        {
            sharedValues.Clear();
            sharedIndex.Clear();
            manager.GetAllUniqueSharedComponentData(sharedValues, sharedIndex);

            if (map.IsCreated && map.Capacity < sharedValues.Count)
                map.Capacity = sharedValues.Count;

            map.Clear();
            for (int i = 0; i != sharedValues.Count; ++i)
                map.TryAdd(sharedIndex[i], sharedValues[i]);
        }
    }
}
