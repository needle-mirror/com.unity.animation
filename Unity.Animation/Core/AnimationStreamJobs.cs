using Unity.Entities;
using Unity.Collections;
using Unity.Burst;


namespace Unity.Animation
{
    [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
    internal struct ClearPassMaskJob : IJobEntityBatch
    {
        [ReadOnly] public ComponentTypeHandle<Rig>           RigType;
        public BufferTypeHandle<AnimatedData>                AnimatedDataType;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var rigs = batchInChunk.GetNativeArray(RigType);
            var animatedDataAccessor = batchInChunk.GetBufferAccessor(AnimatedDataType);

            for (int i = 0; i < batchInChunk.Count; ++i)
            {
                var stream = AnimationStream.Create(rigs[i], animatedDataAccessor[i].AsNativeArray());

                stream.PassMask.Clear();
            }
        }
    }

    [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
    internal struct ClearMasksJob : IJobEntityBatch
    {
        [ReadOnly] public ComponentTypeHandle<Rig>           RigType;
        public BufferTypeHandle<AnimatedData>                AnimatedDataType;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var rigs = batchInChunk.GetNativeArray(RigType);
            var animatedDataAccessor = batchInChunk.GetBufferAccessor(AnimatedDataType);

            for (int i = 0; i < batchInChunk.Count; ++i)
            {
                var stream = AnimationStream.Create(rigs[i], animatedDataAccessor[i].AsNativeArray());

                stream.ClearMasks();
            }
        }
    }
}
