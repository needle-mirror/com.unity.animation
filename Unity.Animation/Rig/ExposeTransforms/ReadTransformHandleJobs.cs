using System;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;

namespace Unity.Animation
{
    [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
    internal struct SortReadTransformComponentJob<TReadTransformHandle> : IJobChunk
        where TReadTransformHandle : struct, IReadTransformHandle
    {
        static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadWrite<TReadTransformHandle>()
            }
        };

        public ArchetypeChunkBufferType<TReadTransformHandle> ReadTransforms;
        public uint LastSystemVersion;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var didChange = chunk.DidChange(ReadTransforms, LastSystemVersion);
            if (!didChange)
                return;

            var readTransformAccessor = chunk.GetBufferAccessor(ReadTransforms);

            for (int i = 0; i < chunk.Count; ++i)
            {
                var readTransforms = readTransformAccessor[i].AsNativeArray();
                readTransforms.Sort(new RigEntityBuilder.TransformHandleComparer<TReadTransformHandle>());
                var end = readTransforms.Unique(new RigEntityBuilder.TransformHandleComparer<TReadTransformHandle>());
                if (end < readTransforms.Length)
                {
                    throw new InvalidOperationException($"Cannot have multiple entity targeting the same transform index. Ignoring Entity '{readTransforms[end].Entity}' targeting transform index '{readTransforms[end].Index}'.");
                }
            }
        }
    }

    [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
    internal struct ReadTransformComponentJob<TReadTransformHandle> : IJobChunk
        where TReadTransformHandle : struct, IReadTransformHandle
    {
        static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<Rig>(),
                ComponentType.ReadOnly<TReadTransformHandle>(),
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadWrite<AnimatedData>()
            }
        };

        [ReadOnly] public ArchetypeChunkComponentType<Rig> Rigs;
        [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> RigLocalToWorlds;
        [ReadOnly] public ArchetypeChunkBufferType<TReadTransformHandle> ReadTransforms;
        [ReadOnly] public ComponentDataFromEntity<LocalToWorld> EntityLocalToWorld;

        public ArchetypeChunkBufferType<AnimatedData> AnimatedData;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var rigs = chunk.GetNativeArray(Rigs);
            var rigLocalToWorlds = chunk.GetNativeArray(RigLocalToWorlds);
            var readTransformAccessor = chunk.GetBufferAccessor(ReadTransforms);
            var animatedDataAccessor = chunk.GetBufferAccessor(AnimatedData);

            for (int i = 0; i < chunk.Count; ++i)
            {
                var readTransformArray = readTransformAccessor[i].AsNativeArray();
                var stream = AnimationStream.Create(rigs[i], animatedDataAccessor[i].AsNativeArray());
                var invRigLocalToWorld = math.inverse(rigLocalToWorlds[i].Value);

                var localToRootCache = new NativeArray<float4x4>(stream.Rig.Value.Skeleton.BoneCount, Allocator.Temp);

                var readTransformIndex = 0;
                var b = 0;

                var readTransform = readTransformArray[readTransformIndex];
                var parentIdx = stream.Rig.Value.Skeleton.ParentIndexes[b];
                var parentLocalToRoot = parentIdx == -1 ? float4x4.identity : localToRootCache[parentIdx];

                // Assumes the ReadtTransformHandles are sorted, so that we need to pass through the bones only once.
                // This does not work if the bones and the read handles are not sorted in the same order.
                while (b < stream.Rig.Value.Skeleton.BoneCount)
                {
                    // Early exit if all read transforms have been processed,
                    // no need to compute the rest of the LocalToRoots.
                    if (readTransformIndex >= readTransformArray.Length)
                        break;

                    readTransform = readTransformArray[readTransformIndex];

                    // If the target index is smaller or equal than the previous one, it means we reached the
                    // end of the buffer, that has the duplicates (for when several read transform target the
                    // same index in the rig).
                    if (readTransformIndex > 0
                        && readTransformArray[readTransformIndex - 1].Index >= readTransform.Index)
                    {
                        break;
                    }

                    parentIdx = stream.Rig.Value.Skeleton.ParentIndexes[b];
                    parentLocalToRoot = parentIdx == -1 ? float4x4.identity : localToRootCache[parentIdx];

                    if (readTransform.Index == b)
                    {
                        if (EntityLocalToWorld.HasComponent(readTransform.Entity))
                        {
                            var localToRoot = math.mul(invRigLocalToWorld, EntityLocalToWorld[readTransform.Entity].Value);

                            if (b > 0)
                            {
                                var tx = math.mul(math.inverse(parentLocalToRoot), localToRoot);

                                stream.SetLocalToParentTranslation(b, tx.c3.xyz);
                                stream.SetLocalToParentRotation(b, new quaternion(tx));
                            }
                            else
                            {
                                stream.SetLocalToParentTranslation(b, localToRoot.c3.xyz);
                                stream.SetLocalToParentRotation(b, new quaternion(localToRoot));
                            }

                            localToRootCache[b] = localToRoot;
                            readTransformIndex++;
                        }
                    }
                    else
                    {
                        localToRootCache[b] = math.mul(parentLocalToRoot, stream.GetLocalToParentMatrix(b));
                    }

                    ++b;
                }
            }
        }
    }
}
