using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Profiling;

namespace Unity.Animation
{
    public abstract class RigComputeMatricesSystemBase : JobComponentSystem
    {
        private EntityQuery m_GlobalSpaceOnlyQuery;
        private EntityQuery m_RigSpaceOnlyQuery;
        private EntityQuery m_GlobalAndRigSpaceQuery;

        private NativeHashMap<int, SharedRigDefinition> m_SharedRigDefinitions;

        static readonly ProfilerMarker k_Marker = new ProfilerMarker("RigComputeMatricesSystemBase");

        protected override void OnCreate()
        {
            m_GlobalSpaceOnlyQuery = GetEntityQuery(
                ComponentType.ReadOnly<SharedRigDefinition>(),
                ComponentType.ReadOnly<AnimatedLocalTranslation>(),
                ComponentType.ReadOnly<AnimatedLocalRotation>(),
                ComponentType.ReadOnly<AnimatedLocalScale>(),
                ComponentType.ReadOnly<AnimatedFloat>(),
                ComponentType.ReadOnly<AnimatedInt>(),

                ComponentType.ReadWrite<AnimatedLocalToWorld>(),
                ComponentType.Exclude<AnimatedLocalToRig>()
                );

            m_RigSpaceOnlyQuery = GetEntityQuery(
                ComponentType.ReadOnly<SharedRigDefinition>(),
                ComponentType.ReadOnly<AnimatedLocalTranslation>(),
                ComponentType.ReadOnly<AnimatedLocalRotation>(),
                ComponentType.ReadOnly<AnimatedLocalScale>(),
                ComponentType.ReadOnly<AnimatedFloat>(),
                ComponentType.ReadOnly<AnimatedInt>(),

                ComponentType.Exclude<AnimatedLocalToWorld>(),
                ComponentType.ReadWrite<AnimatedLocalToRig>()
                );

            m_GlobalAndRigSpaceQuery = GetEntityQuery(
                ComponentType.ReadOnly<SharedRigDefinition>(),
                ComponentType.ReadOnly<AnimatedLocalTranslation>(),
                ComponentType.ReadOnly<AnimatedLocalRotation>(),
                ComponentType.ReadOnly<AnimatedLocalScale>(),
                ComponentType.ReadOnly<AnimatedFloat>(),
                ComponentType.ReadOnly<AnimatedInt>(),

                ComponentType.ReadWrite<AnimatedLocalToWorld>(),
                ComponentType.ReadWrite<AnimatedLocalToRig>()
                );
        }
        protected override void OnDestroy()
        {
            if (m_SharedRigDefinitions.IsCreated)
                m_SharedRigDefinitions.Dispose();

            base.OnDestroy();
        }

        protected override JobHandle OnUpdate(JobHandle dep)
        {
            k_Marker.Begin();

            ECSUtils.UpdateSharedComponentDataHashMap(ref m_SharedRigDefinitions, EntityManager, Allocator.Persistent);

            var globalSpaceOnlyJob = new ComputeGlobalSpaceJob
            {
                Translations = GetArchetypeChunkComponentType<Translation>(true),
                Rotations = GetArchetypeChunkComponentType<Rotation>(true),
                Scales = GetArchetypeChunkComponentType<Scale>(true),
                SharedRigDefinitions = m_SharedRigDefinitions,
                SharedRigDefinitionIndex = GetArchetypeChunkSharedComponentType<SharedRigDefinition>(),
                LocalTranslations = GetArchetypeChunkBufferType<AnimatedLocalTranslation>(true),
                LocalRotations  = GetArchetypeChunkBufferType<AnimatedLocalRotation>(true),
                LocalScales  = GetArchetypeChunkBufferType<AnimatedLocalScale>(true),
                Floats  = GetArchetypeChunkBufferType<AnimatedFloat>(true),
                Ints  = GetArchetypeChunkBufferType<AnimatedInt>(true),

                LocalToWorld = GetArchetypeChunkBufferType<AnimatedLocalToWorld>()
            };
            var globalSpaceOnlyHandle = globalSpaceOnlyJob.Schedule(m_GlobalSpaceOnlyQuery, dep);

            var rigSpaceOnlyJob = new ComputeRigSpaceJob
            {
                SharedRigDefinitions = m_SharedRigDefinitions,
                SharedRigDefinitionIndex = GetArchetypeChunkSharedComponentType<SharedRigDefinition>(),
                LocalTranslations = GetArchetypeChunkBufferType<AnimatedLocalTranslation>(true),
                LocalRotations = GetArchetypeChunkBufferType<AnimatedLocalRotation>(true),
                LocalScales = GetArchetypeChunkBufferType<AnimatedLocalScale>(true),
                Floats = GetArchetypeChunkBufferType<AnimatedFloat>(true),
                Ints = GetArchetypeChunkBufferType<AnimatedInt>(true),

                LocalToRig = GetArchetypeChunkBufferType<AnimatedLocalToRig>()
            };
            var rigSpaceOnlyHandle = rigSpaceOnlyJob.Schedule(m_RigSpaceOnlyQuery, dep);

            var globalAndRigSpaceJob = new ComputeGlobalAndRigSpaceJob
            {
                Translations = GetArchetypeChunkComponentType<Translation>(true),
                Rotations = GetArchetypeChunkComponentType<Rotation>(true),
                Scales = GetArchetypeChunkComponentType<Scale>(true),
                SharedRigDefinitions = m_SharedRigDefinitions,
                SharedRigDefinitionIndex = GetArchetypeChunkSharedComponentType<SharedRigDefinition>(),
                LocalTranslations = GetArchetypeChunkBufferType<AnimatedLocalTranslation>(true),
                LocalRotations = GetArchetypeChunkBufferType<AnimatedLocalRotation>(true),
                LocalScales = GetArchetypeChunkBufferType<AnimatedLocalScale>(true),
                Floats = GetArchetypeChunkBufferType<AnimatedFloat>(true),
                Ints = GetArchetypeChunkBufferType<AnimatedInt>(true),

                LocalToWorld = GetArchetypeChunkBufferType<AnimatedLocalToWorld>(),
                LocalToRig = GetArchetypeChunkBufferType<AnimatedLocalToRig>()
            };

            // TODO : These jobs should ideally all run in parallel since the queries are mutually exclusive.
            //        For now, in order to prevent the safety system from throwing errors schedule
            //        globalAndRigSpaceJob with a dependency on the two others. 
            var inputDep = globalAndRigSpaceJob.Schedule(
                m_GlobalAndRigSpaceQuery,
                JobHandle.CombineDependencies(globalSpaceOnlyHandle, rigSpaceOnlyHandle)
                );
            
            k_Marker.End();

            return inputDep;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        struct ComputeGlobalSpaceJob : IJobChunk
        {
            [ReadOnly] public NativeHashMap<int, SharedRigDefinition> SharedRigDefinitions;
            [ReadOnly] public ArchetypeChunkSharedComponentType<SharedRigDefinition> SharedRigDefinitionIndex;

            [ReadOnly] public ArchetypeChunkComponentType<Translation> Translations;
            [ReadOnly] public ArchetypeChunkComponentType<Rotation> Rotations;
            [ReadOnly] public ArchetypeChunkComponentType<Scale> Scales;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedLocalTranslation> LocalTranslations;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedLocalRotation> LocalRotations;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedLocalScale> LocalScales;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedFloat> Floats;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedInt> Ints;

            public ArchetypeChunkBufferType<AnimatedLocalToWorld> LocalToWorld;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var eTranslations = chunk.GetNativeArray(Translations);
                var eRotations = chunk.GetNativeArray(Rotations);
                var eScales = chunk.GetNativeArray(Scales);
                var lTranslations = chunk.GetBufferAccessor(LocalTranslations);
                var lRotations = chunk.GetBufferAccessor(LocalRotations);
                var lScales = chunk.GetBufferAccessor(LocalScales);
                var lFloats = chunk.GetBufferAccessor(Floats);
                var lInts = chunk.GetBufferAccessor(Ints);
                var localToWorlds = chunk.GetBufferAccessor(LocalToWorld);
                var sharedRigDefinitionIndex = chunk.GetSharedComponentIndex(SharedRigDefinitionIndex);

                var rig = SharedRigDefinitions[sharedRigDefinitionIndex].Value;

                var entityCount = chunk.Count;
                var chunkRotationsExist = eRotations.Length > 0;
                var chunkTranslationsExist = eTranslations.Length > 0;
                var chunkScalesExist = eScales.Length > 0;

                float4x4 entityMatrix;

                // 000
                if ((!chunkTranslationsExist) && (!chunkRotationsExist) && (!chunkScalesExist))
                {
                    entityMatrix = float4x4.identity;
                    for (int i = 0; i != entityCount; ++i)
                    {
                        var stream = AnimationStreamProvider.CreateReadOnly(rig, lTranslations[i], lRotations[i], lScales[i], lFloats[i], lInts[i]);
                        ComputeLocalToWorld(ref entityMatrix, ref stream, localToWorlds[i].Reinterpret<float4x4>());
                    }
                }
                // 001
                else if ((!chunkTranslationsExist) && (!chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i != entityCount; ++i)
                    {
                        entityMatrix = float4x4.TRS(float3.zero, quaternion.identity, eScales[i].Value);
                        var stream = AnimationStreamProvider.CreateReadOnly(rig, lTranslations[i], lRotations[i], lScales[i], lFloats[i], lInts[i]);
                        ComputeLocalToWorld(ref entityMatrix, ref stream, localToWorlds[i].Reinterpret<float4x4>());
                    }
                }
                // 010
                else if ((!chunkTranslationsExist) && (chunkRotationsExist) && (!chunkScalesExist))
                {
                    for (int i = 0; i != entityCount; ++i)
                    {
                        entityMatrix = float4x4.TRS(float3.zero, eRotations[i].Value, mathex.one());
                        var stream = AnimationStreamProvider.CreateReadOnly(rig, lTranslations[i], lRotations[i], lScales[i], lFloats[i], lInts[i]);
                        ComputeLocalToWorld(ref entityMatrix, ref stream, localToWorlds[i].Reinterpret<float4x4>());
                    }
                }
                // 011
                else if ((!chunkTranslationsExist) && (chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i != entityCount; ++i)
                    {
                        entityMatrix = float4x4.TRS(float3.zero, eRotations[i].Value, eScales[i].Value);
                        var stream = AnimationStreamProvider.CreateReadOnly(rig, lTranslations[i], lRotations[i], lScales[i], lFloats[i], lInts[i]);
                        ComputeLocalToWorld(ref entityMatrix, ref stream, localToWorlds[i].Reinterpret<float4x4>());
                    }
                }
                // 100
                else if ((chunkTranslationsExist) && (!chunkRotationsExist) && (!chunkScalesExist))
                {
                    for (int i = 0; i != entityCount; ++i)
                    {
                        entityMatrix = float4x4.TRS(eTranslations[i].Value, quaternion.identity, mathex.one());
                        var stream = AnimationStreamProvider.CreateReadOnly(rig, lTranslations[i], lRotations[i], lScales[i], lFloats[i], lInts[i]);
                        ComputeLocalToWorld(ref entityMatrix, ref stream, localToWorlds[i].Reinterpret<float4x4>());
                    }
                }
                // 101
                else if ((chunkTranslationsExist) && (!chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i != entityCount; ++i)
                    {
                        entityMatrix = float4x4.TRS(eTranslations[i].Value, quaternion.identity, eScales[i].Value);
                        var stream = AnimationStreamProvider.CreateReadOnly(rig, lTranslations[i], lRotations[i], lScales[i], lFloats[i], lInts[i]);
                        ComputeLocalToWorld(ref entityMatrix, ref stream, localToWorlds[i].Reinterpret<float4x4>());
                    }
                }
                // 110
                else if ((chunkTranslationsExist) && (chunkRotationsExist) && (!chunkScalesExist))
                {
                    for (int i = 0; i != entityCount; ++i)
                    {
                        entityMatrix = float4x4.TRS(eTranslations[i].Value, eRotations[i].Value, mathex.one());
                        var stream = AnimationStreamProvider.CreateReadOnly(rig, lTranslations[i], lRotations[i], lScales[i], lFloats[i], lInts[i]);
                        ComputeLocalToWorld(ref entityMatrix, ref stream, localToWorlds[i].Reinterpret<float4x4>());
                    }
                }
                // 111
                else if ((chunkTranslationsExist) && (chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i != entityCount; ++i)
                    {
                        entityMatrix = float4x4.TRS(eTranslations[i].Value, eRotations[i].Value, eScales[i].Value);
                        var stream = AnimationStreamProvider.CreateReadOnly(rig, lTranslations[i], lRotations[i], lScales[i], lFloats[i], lInts[i]);
                        ComputeLocalToWorld(ref entityMatrix, ref stream, localToWorlds[i].Reinterpret<float4x4>());
                    }
                }
            }

            static void ComputeLocalToWorld(
                ref float4x4 localToWorld,
                ref AnimationStream<AnimationStreamOffsetPtrDescriptor> stream,
                DynamicBuffer<float4x4> outLocalToWorlds
                )
            {
                if (stream.IsNull)
                    return;

                // Compute root in world space.
                outLocalToWorlds[0] = math.mul(localToWorld, stream.GetLocalToParentMatrix(0));

                // Compute world coordinates of all other joints.
                for (int i = 1, count = stream.Rig.Value.Skeleton.ParentIndexes.Length; i != count; ++i)
                {
                    var pIdx = stream.Rig.Value.Skeleton.ParentIndexes[i];
                    outLocalToWorlds[i] = math.mul(outLocalToWorlds[pIdx], stream.GetLocalToParentMatrix(i));
                }
            }
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        struct ComputeRigSpaceJob : IJobChunk
        {
            [ReadOnly] public NativeHashMap<int, SharedRigDefinition> SharedRigDefinitions;
            [ReadOnly] public ArchetypeChunkSharedComponentType<SharedRigDefinition> SharedRigDefinitionIndex;

            [ReadOnly] public ArchetypeChunkBufferType<AnimatedLocalTranslation> LocalTranslations;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedLocalRotation> LocalRotations;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedLocalScale> LocalScales;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedFloat> Floats;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedInt> Ints;

            public ArchetypeChunkBufferType<AnimatedLocalToRig> LocalToRig;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var lTranslations = chunk.GetBufferAccessor(LocalTranslations);
                var lRotations = chunk.GetBufferAccessor(LocalRotations);
                var lScales = chunk.GetBufferAccessor(LocalScales);
                var lFloats = chunk.GetBufferAccessor(Floats);
                var lInts = chunk.GetBufferAccessor(Ints);
                var localToRigs = chunk.GetBufferAccessor(LocalToRig);
                var sharedRigDefinitionIndex = chunk.GetSharedComponentIndex(SharedRigDefinitionIndex);

                var rig = SharedRigDefinitions[sharedRigDefinitionIndex].Value;

                 for (int i = 0; i != chunk.Count; ++i)
                 {
                    var stream = AnimationStreamProvider.CreateReadOnly(rig, lTranslations[i], lRotations[i], lScales[i], lFloats[i], lInts[i]);
                    ComputeLocalToRig(ref stream, localToRigs[i].Reinterpret<float4x4>());
                 }
            }

            static void ComputeLocalToRig(
                ref AnimationStream<AnimationStreamOffsetPtrDescriptor> stream,
                DynamicBuffer<float4x4> outLocalToRigs
                )
            {
                if (stream.IsNull)
                    return;

                // Compute object space transforms
                outLocalToRigs[0] = stream.GetLocalToParentMatrix(0);
                for (int i = 1, count = stream.Rig.Value.Skeleton.ParentIndexes.Length; i != count; ++i)
                {
                    var pIdx = stream.Rig.Value.Skeleton.ParentIndexes[i];
                    outLocalToRigs[i] = math.mul(outLocalToRigs[pIdx], stream.GetLocalToParentMatrix(i));
                }
            }
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        struct ComputeGlobalAndRigSpaceJob : IJobChunk
        {
            [ReadOnly] public NativeHashMap<int, SharedRigDefinition> SharedRigDefinitions;
            [ReadOnly] public ArchetypeChunkSharedComponentType<SharedRigDefinition> SharedRigDefinitionIndex;

            [ReadOnly] public ArchetypeChunkComponentType<Translation> Translations;
            [ReadOnly] public ArchetypeChunkComponentType<Rotation> Rotations;
            [ReadOnly] public ArchetypeChunkComponentType<Scale> Scales;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedLocalTranslation> LocalTranslations;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedLocalRotation> LocalRotations;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedLocalScale> LocalScales;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedFloat> Floats;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedInt> Ints;

            public ArchetypeChunkBufferType<AnimatedLocalToWorld> LocalToWorld;
            public ArchetypeChunkBufferType<AnimatedLocalToRig> LocalToRig;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var eTranslations = chunk.GetNativeArray(Translations);
                var eRotations = chunk.GetNativeArray(Rotations);
                var eScales = chunk.GetNativeArray(Scales);
                var lTranslations = chunk.GetBufferAccessor(LocalTranslations);
                var lRotations = chunk.GetBufferAccessor(LocalRotations);
                var lScales = chunk.GetBufferAccessor(LocalScales);
                var lFloats = chunk.GetBufferAccessor(Floats);
                var lInts = chunk.GetBufferAccessor(Ints);
                var localToWorlds = chunk.GetBufferAccessor(LocalToWorld);
                var localToRigs = chunk.GetBufferAccessor(LocalToRig);
                var sharedRigDefinitionIndex = chunk.GetSharedComponentIndex(SharedRigDefinitionIndex);

                var rig = SharedRigDefinitions[sharedRigDefinitionIndex].Value;

                var entityCount = chunk.Count;
                var chunkRotationsExist = eRotations.Length > 0;
                var chunkTranslationsExist = eTranslations.Length > 0;
                var chunkScalesExist = eScales.Length > 0;

                float4x4 entityMatrix;

                // 000
                if ((!chunkTranslationsExist) && (!chunkRotationsExist) && (!chunkScalesExist))
                {
                    entityMatrix = float4x4.identity;
                    for (int i = 0; i != entityCount; ++i)
                    {
                        var stream = AnimationStreamProvider.CreateReadOnly(rig, lTranslations[i], lRotations[i], lScales[i], lFloats[i], lInts[i]);
                        ComputeLocalToWorldAndRig(ref entityMatrix, ref stream, localToWorlds[i].Reinterpret<float4x4>(), localToRigs[i].Reinterpret<float4x4>());
                    }
                }
                // 001
                else if ((!chunkTranslationsExist) && (!chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i != entityCount; ++i)
                    {
                        entityMatrix = float4x4.TRS(float3.zero, quaternion.identity, eScales[i].Value);
                        var stream = AnimationStreamProvider.CreateReadOnly(rig, lTranslations[i], lRotations[i], lScales[i], lFloats[i], lInts[i]);
                        ComputeLocalToWorldAndRig(ref entityMatrix, ref stream, localToWorlds[i].Reinterpret<float4x4>(), localToRigs[i].Reinterpret<float4x4>());
                    }
                }
                // 010
                else if ((!chunkTranslationsExist) && (chunkRotationsExist) && (!chunkScalesExist))
                {
                    for (int i = 0; i != entityCount; ++i)
                    {
                        entityMatrix = float4x4.TRS(float3.zero, eRotations[i].Value, mathex.one());
                        var stream = AnimationStreamProvider.CreateReadOnly(rig, lTranslations[i], lRotations[i], lScales[i], lFloats[i], lInts[i]);
                        ComputeLocalToWorldAndRig(ref entityMatrix, ref stream, localToWorlds[i].Reinterpret<float4x4>(), localToRigs[i].Reinterpret<float4x4>());
                    }
                }
                // 011
                else if ((!chunkTranslationsExist) && (chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i != entityCount; ++i)
                    {
                        entityMatrix = float4x4.TRS(float3.zero, eRotations[i].Value, eScales[i].Value);
                        var stream = AnimationStreamProvider.CreateReadOnly(rig, lTranslations[i], lRotations[i], lScales[i], lFloats[i], lInts[i]);
                        ComputeLocalToWorldAndRig(ref entityMatrix, ref stream, localToWorlds[i].Reinterpret<float4x4>(), localToRigs[i].Reinterpret<float4x4>());
                    }
                }
                // 100
                else if ((chunkTranslationsExist) && (!chunkRotationsExist) && (!chunkScalesExist))
                {
                    for (int i = 0; i != entityCount; ++i)
                    {
                        entityMatrix = float4x4.TRS(eTranslations[i].Value, quaternion.identity, mathex.one());
                        var stream = AnimationStreamProvider.CreateReadOnly(rig, lTranslations[i], lRotations[i], lScales[i], lFloats[i], lInts[i]);
                        ComputeLocalToWorldAndRig(ref entityMatrix, ref stream, localToWorlds[i].Reinterpret<float4x4>(), localToRigs[i].Reinterpret<float4x4>());
                    }
                }
                // 101
                else if ((chunkTranslationsExist) && (!chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i != entityCount; ++i)
                    {
                        entityMatrix = float4x4.TRS(eTranslations[i].Value, quaternion.identity, eScales[i].Value);
                        var stream = AnimationStreamProvider.CreateReadOnly(rig, lTranslations[i], lRotations[i], lScales[i], lFloats[i], lInts[i]);
                        ComputeLocalToWorldAndRig(ref entityMatrix, ref stream, localToWorlds[i].Reinterpret<float4x4>(), localToRigs[i].Reinterpret<float4x4>());
                    }
                }
                // 110
                else if ((chunkTranslationsExist) && (chunkRotationsExist) && (!chunkScalesExist))
                {
                    for (int i = 0; i != entityCount; ++i)
                    {
                        entityMatrix = float4x4.TRS(eTranslations[i].Value, eRotations[i].Value, mathex.one());
                        var stream = AnimationStreamProvider.CreateReadOnly(rig, lTranslations[i], lRotations[i], lScales[i], lFloats[i], lInts[i]);
                        ComputeLocalToWorldAndRig(ref entityMatrix, ref stream, localToWorlds[i].Reinterpret<float4x4>(), localToRigs[i].Reinterpret<float4x4>());
                    }
                }
                // 111
                else if ((chunkTranslationsExist) && (chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i != entityCount; ++i)
                    {
                        entityMatrix = float4x4.TRS(eTranslations[i].Value, eRotations[i].Value, eScales[i].Value);
                        var stream = AnimationStreamProvider.CreateReadOnly(rig, lTranslations[i], lRotations[i], lScales[i], lFloats[i], lInts[i]);
                        ComputeLocalToWorldAndRig(ref entityMatrix, ref stream, localToWorlds[i].Reinterpret<float4x4>(), localToRigs[i].Reinterpret<float4x4>());
                    }
                }
            }

            static void ComputeLocalToWorldAndRig(
                ref float4x4 localToWorld,
                ref AnimationStream<AnimationStreamOffsetPtrDescriptor> stream,
                DynamicBuffer<float4x4> outLocalToWorlds,
                DynamicBuffer<float4x4> outLocalToRigs
                )
            {
                if (stream.IsNull)
                    return;

                var mat = stream.GetLocalToParentMatrix(0);
                outLocalToWorlds[0] = math.mul(localToWorld, mat);
                outLocalToRigs[0] = mat;

                // Compute object and world transforms
                for (int i = 1, count = stream.Rig.Value.Skeleton.ParentIndexes.Length; i != count; ++i)
                {
                    var pIdx = stream.Rig.Value.Skeleton.ParentIndexes[i];
                    mat = stream.GetLocalToParentMatrix(i);

                    outLocalToWorlds[i] = math.mul(outLocalToWorlds[pIdx], mat);
                    outLocalToRigs[i] = math.mul(outLocalToRigs[pIdx], mat);
                }
            }
        }
    }
}
