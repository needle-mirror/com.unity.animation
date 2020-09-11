using Unity.Entities;
using Unity.Collections;
using Unity.Burst;


namespace Unity.Animation
{
    [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
    internal struct ClearPassMaskJob : IJobChunk
    {
        [ReadOnly] public ComponentTypeHandle<Rig>           RigType;
        public BufferTypeHandle<AnimatedData>                AnimatedDataType;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var rigs = chunk.GetNativeArray(RigType);
            var animatedDataAccessor = chunk.GetBufferAccessor(AnimatedDataType);

            for (int i = 0; i < chunk.Count; ++i)
            {
                var stream = AnimationStream.Create(rigs[i], animatedDataAccessor[i].AsNativeArray());

                stream.PassMask.Clear();
            }
        }
    }

    [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
    internal struct ClearMasksJob : IJobChunk
    {
        [ReadOnly] public ComponentTypeHandle<Rig>           RigType;
        public BufferTypeHandle<AnimatedData>                AnimatedDataType;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var rigs = chunk.GetNativeArray(RigType);
            var animatedDataAccessor = chunk.GetBufferAccessor(AnimatedDataType);

            for (int i = 0; i < chunk.Count; ++i)
            {
                var stream = AnimationStream.Create(rigs[i], animatedDataAccessor[i].AsNativeArray());

                stream.ClearMasks();
            }
        }
    }
}
