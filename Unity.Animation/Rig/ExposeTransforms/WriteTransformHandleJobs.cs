using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;


namespace Unity.Animation
{
    [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
    internal struct WriteTransformComponentJob<TWriteTransformHandle> : IJobChunk
        where TWriteTransformHandle : struct, IWriteTransformHandle
    {
        static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<Rig>(),
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<AnimatedData>(),
                ComponentType.ReadOnly<TWriteTransformHandle>(),
                ComponentType.ReadWrite<AnimatedLocalToWorld>()
            }
        };

        [ReadOnly] public ArchetypeChunkComponentType<Rig> Rigs;
        [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> RigLocalToWorlds;
        [ReadOnly] public ArchetypeChunkBufferType<AnimatedData> AnimatedData;
        [ReadOnly] public ArchetypeChunkBufferType<TWriteTransformHandle> WriteTransforms;

        public ArchetypeChunkBufferType<AnimatedLocalToWorld> AnimatedLocalToWorlds;

        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<LocalToWorld> EntityLocalToWorld;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var rigs = chunk.GetNativeArray(Rigs);
            var rigLocalToWorlds = chunk.GetNativeArray(RigLocalToWorlds);
            var animatedDataAccessor = chunk.GetBufferAccessor(AnimatedData);
            var writeTransformAccessor = chunk.GetBufferAccessor(WriteTransforms);
            var animatedLocalToWorldAccessor = chunk.GetBufferAccessor(AnimatedLocalToWorlds);

            for (int i = 0; i != chunk.Count; ++i)
            {
                var animatedLocalToWorlds = animatedLocalToWorldAccessor[i].Reinterpret<float4x4>().AsNativeArray();
                var stream = AnimationStream.CreateReadOnly(rigs[i], animatedDataAccessor[i].AsNativeArray());
                Core.ComputeLocalToWorld(rigLocalToWorlds[i].Value, ref stream, animatedLocalToWorlds);

                var writeTransforms = writeTransformAccessor[i].AsNativeArray();
                for (int j = 0; j < writeTransforms.Length; ++j)
                {
                    if (EntityLocalToWorld.HasComponent(writeTransforms[j].Entity))
                    {
                        EntityLocalToWorld[writeTransforms[j].Entity] = new LocalToWorld
                        {
                            Value = animatedLocalToWorlds[writeTransforms[j].Index]
                        };
                    }
                }
            }
        }
    }
}
