using System;
using System.Diagnostics;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;


namespace Unity.Animation
{
    [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
    internal struct SortReadTransformComponentJob<TReadTransformHandle> : IJobEntityBatch
        where TReadTransformHandle : struct, IReadTransformHandle
    {
        static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadWrite<TReadTransformHandle>()
            }
        };

        public BufferTypeHandle<TReadTransformHandle> ReadTransforms;
        public uint LastSystemVersion;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateHasOnlyOneEntityPerTransform(NativeArray<TReadTransformHandle> readTransforms)
        {
            var end = readTransforms.Unique(new RigEntityBuilder.TransformHandleComparer<TReadTransformHandle>());
            if (end < readTransforms.Length)
            {
                throw new InvalidOperationException($"Cannot have multiple entity targeting the same transform index. Ignoring Entity '{readTransforms[end].Entity}' targeting transform index '{readTransforms[end].Index}'.");
            }
        }

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var didChange = batchInChunk.DidChange(ReadTransforms, LastSystemVersion);
            if (!didChange)
                return;

            var readTransformAccessor = batchInChunk.GetBufferAccessor(ReadTransforms);

            for (int i = 0; i < batchInChunk.Count; ++i)
            {
                var readTransforms = readTransformAccessor[i].AsNativeArray();
                readTransforms.Sort(new RigEntityBuilder.TransformHandleComparer<TReadTransformHandle>());

                ValidateHasOnlyOneEntityPerTransform(readTransforms);
            }
        }
    }

    [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
    internal struct ReadTransformComponentJob<TReadTransformHandle> : IJobEntityBatch
        where TReadTransformHandle : struct, IReadTransformHandle
    {
        static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<Rig>(),
                ComponentType.ReadOnly<TReadTransformHandle>(),
                ComponentType.ReadOnly<RigRootEntity>(),
                ComponentType.ReadWrite<AnimatedData>()
            }
        };

        [ReadOnly] public ComponentTypeHandle<Rig> Rigs;
        [ReadOnly] public ComponentTypeHandle<RigRootEntity> RigRoots;
        [ReadOnly] public BufferTypeHandle<TReadTransformHandle> ReadTransforms;
        [ReadOnly] public ComponentDataFromEntity<LocalToWorld> EntityLocalToWorld;

        public BufferTypeHandle<AnimatedData> AnimatedData;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var rigs = batchInChunk.GetNativeArray(Rigs);
            var rigRoots = batchInChunk.GetNativeArray(RigRoots);
            var readTransformAccessor = batchInChunk.GetBufferAccessor(ReadTransforms);
            var animatedDataAccessor = batchInChunk.GetBufferAccessor(AnimatedData);

            for (int i = 0; i < batchInChunk.Count; ++i)
            {
                var readTransformArray = readTransformAccessor[i].AsNativeArray();
                var stream = AnimationStream.Create(rigs[i], animatedDataAccessor[i].AsNativeArray());
                var invRootLocalToWorld = math.inverse(EntityLocalToWorld[rigRoots[i].Value].Value);

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
                            var localToRoot = math.mul(invRootLocalToWorld, EntityLocalToWorld[readTransform.Entity].Value);

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
                        localToRootCache[b] = math.mul(parentLocalToRoot, mathex.float4x4(stream.GetLocalToParentMatrix(b)));
                    }

                    ++b;
                }
            }
        }
    }
}
