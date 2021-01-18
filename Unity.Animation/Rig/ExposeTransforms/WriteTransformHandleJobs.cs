using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;


namespace Unity.Animation
{
    [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
    internal struct WriteTransformComponentJob<TWriteTransformHandle> : IJobEntityBatch
        where TWriteTransformHandle : struct, IWriteTransformHandle
    {
        static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<Rig>(),
                ComponentType.ReadOnly<RigRootEntity>(),
                ComponentType.ReadOnly<AnimatedData>(),
                ComponentType.ReadOnly<TWriteTransformHandle>(),
                ComponentType.ReadWrite<AnimatedLocalToWorld>()
            }
        };

        [ReadOnly] public ComponentTypeHandle<Rig> Rigs;
        [ReadOnly] public ComponentTypeHandle<RigRootEntity> RigRoots;
        [ReadOnly] public BufferTypeHandle<AnimatedData> AnimatedData;
        [ReadOnly] public BufferTypeHandle<TWriteTransformHandle> WriteTransforms;

        public BufferTypeHandle<AnimatedLocalToWorld> AnimatedLocalToWorlds;

        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<LocalToWorld> EntityLocalToWorld;

        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<LocalToParent> EntityLocalToParent;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<Translation> EntityTranslation;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<Rotation> EntityRotation;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<Scale> EntityScale;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<NonUniformScale> EntityNonUniformScale;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var rigs = batchInChunk.GetNativeArray(Rigs);
            var rigRoots = batchInChunk.GetNativeArray(RigRoots);
            var animatedDataAccessor = batchInChunk.GetBufferAccessor(AnimatedData);
            var writeTransformAccessor = batchInChunk.GetBufferAccessor(WriteTransforms);
            var animatedLocalToWorldAccessor = batchInChunk.GetBufferAccessor(AnimatedLocalToWorlds);

            for (int i = 0; i != batchInChunk.Count; ++i)
            {
                var animatedLocalToWorlds = animatedLocalToWorldAccessor[i].Reinterpret<float4x4>().AsNativeArray();
                var stream = AnimationStream.CreateReadOnly(rigs[i], animatedDataAccessor[i].AsNativeArray());
                var rootLocalToWorld = EntityLocalToWorld[rigRoots[i].Value].Value;

                Core.ComputeLocalToRoot(ref stream, rootLocalToWorld, animatedLocalToWorlds);

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

                    if (EntityLocalToParent.HasComponent(writeTransforms[j].Entity))
                    {
                        EntityLocalToParent[writeTransforms[j].Entity] = new LocalToParent
                        {
                            Value = mathex.float4x4(stream.GetLocalToParentMatrix(writeTransforms[j].Index))
                        };
                    }

                    if (EntityTranslation.HasComponent(writeTransforms[j].Entity))
                    {
                        EntityTranslation[writeTransforms[j].Entity] = new Translation
                        {
                            Value = stream.GetLocalToParentTranslation(writeTransforms[j].Index)
                        };
                    }

                    if (EntityRotation.HasComponent(writeTransforms[j].Entity))
                    {
                        EntityRotation[writeTransforms[j].Entity] = new Rotation
                        {
                            Value = stream.GetLocalToParentRotation(writeTransforms[j].Index)
                        };
                    }

                    if (EntityScale.HasComponent(writeTransforms[j].Entity))
                    {
                        EntityScale[writeTransforms[j].Entity] = new Scale
                        {
                            Value = stream.GetLocalToParentScale(writeTransforms[j].Index).x
                        };
                    }
                    else if (EntityNonUniformScale.HasComponent(writeTransforms[j].Entity))
                    {
                        EntityNonUniformScale[writeTransforms[j].Entity] = new NonUniformScale
                        {
                            Value = stream.GetLocalToParentScale(writeTransforms[j].Index)
                        };
                    }
                }
            }
        }
    }
}
