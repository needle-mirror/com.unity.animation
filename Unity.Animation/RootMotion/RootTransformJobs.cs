using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animation
{
    /// <summary>
    /// This job writes the root entity transform component values to the animation stream when the
    /// root motion component and the disable root transform R/W tag are not present.
    /// </summary>
    [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
    internal struct ReadRootTransformJob<TAnimatedRootMotion> : IJobEntityBatch
        where TAnimatedRootMotion : struct, IAnimatedRootMotion
    {
        static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<Rig>(),
                ComponentType.ReadOnly<RigRootEntity>(),
                ComponentType.ReadWrite<AnimatedData>()
            },
            None = new ComponentType[]
            {
                typeof(DisableRootTransformReadWriteTag),
                typeof(TAnimatedRootMotion)
            }
        };

        [ReadOnly] public ComponentDataFromEntity<Translation>     EntityTranslation;
        [ReadOnly] public ComponentDataFromEntity<Rotation>        EntityRotation;
        [ReadOnly] public ComponentDataFromEntity<Scale>           EntityScale;
        [ReadOnly] public ComponentDataFromEntity<NonUniformScale> EntityNonUniformScale;

        [ReadOnly] public ComponentTypeHandle<Rig>           RigType;
        [ReadOnly] public ComponentTypeHandle<RigRootEntity> RigRootEntityType;
        public BufferTypeHandle<AnimatedData>                AnimatedDataType;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var animatedDataAccessor = batchInChunk.GetBufferAccessor(AnimatedDataType);
            var rigs = batchInChunk.GetNativeArray(RigType);
            var rigRoots = batchInChunk.GetNativeArray(RigRootEntityType);

            for (int i = 0; i != batchInChunk.Count; ++i)
            {
                var stream = AnimationStream.Create(rigs[i], animatedDataAccessor[i].AsNativeArray());
                var rigRoot = rigRoots[i].Value;

                if (EntityTranslation.HasComponent(rigRoot))
                    stream.SetLocalToParentTranslation(0, EntityTranslation[rigRoot].Value);
                if (EntityRotation.HasComponent(rigRoot))
                    stream.SetLocalToParentRotation(0, EntityRotation[rigRoot].Value);

                if (EntityNonUniformScale.HasComponent(rigRoot))
                    stream.SetLocalToParentScale(0, EntityNonUniformScale[rigRoot].Value);
                else if (EntityScale.HasComponent(rigRoot))
                    stream.SetLocalToParentScale(0, EntityScale[rigRoot].Value);
            }
        }
    }

    /// <summary>
    /// This job writes in the RigRootEntity component the matrix used in the WorldToRootNode to remap a transform from
    /// world space to root space.
    /// </summary>
    [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
    internal struct UpdateRootRemapMatrixJob<TAnimatedRootMotion> : IJobEntityBatch
        where TAnimatedRootMotion : struct, IAnimatedRootMotion
    {
        static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadWrite<RigRootEntity>(),
            }
        };

        [NativeDisableContainerSafetyRestriction]
        [ReadOnly] public ComponentDataFromEntity<LocalToWorld> EntityLocalToWorld;

        [NativeDisableContainerSafetyRestriction]
        [ReadOnly] public ComponentDataFromEntity<Parent> Parent;

        [ReadOnly] public ComponentTypeHandle<DisableRootTransformReadWriteTag> DisableRootTransformType;
        [ReadOnly] public ComponentTypeHandle<TAnimatedRootMotion> AnimatedRootMotionType;

        public ComponentTypeHandle<RigRootEntity> RigRootEntityType;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var rigRoots = batchInChunk.GetNativeArray(RigRootEntityType);

            var isRootDisabled = batchInChunk.Has(DisableRootTransformType);
            var isAnimatedRootMotion = batchInChunk.Has(AnimatedRootMotionType);

            // Those bools are valid for the whole chunk, so check only once before loop.
            if (isRootDisabled || isAnimatedRootMotion)
            {
                /// When root is disabled, we do not manage the value of the index 0 of
                /// the stream. As such, we need to store the LocalToWorld of the rig entity. It will be combined with
                /// what is at the index 0 of the stream at the time of evaluation.
                ///
                /// When root motion is enabled, the index 0 of the stream contains the delta
                /// of the motion. This delta is then accumulated in the LocalToWorld of the rig entity.
                /// We store the LocalToWorld of the rig entity, that will get multiplied with the root motion delta.
                for (int i = 0; i != batchInChunk.Count; ++i)
                {
                    rigRoots[i] = new RigRootEntity
                    {
                        Value = rigRoots[i].Value,
                        RemapToRootMatrix = mathex.AffineTransform(EntityLocalToWorld[rigRoots[i].Value].Value)
                    };
                }
            }
            else
            {
                /// If the root entity has no parent, it means that it's the same as the rig entity
                /// (the root entity must be the rig or a child of the rig). At the beginning of the pass, the rig's
                /// LocalToWorld is copied in the stream at index 0. Then the pass is executed, and at the end of the pass,
                /// the index 0 value is copied back to the rig's LocalToWorld and is reset to the identity. Thus,
                /// during the pass, the index 0 contains the rig's LocalToWorld, that was potentially updated by a clip.
                /// We store the inverse of the rig's LocalToWorld, so that it's not accounted twice for when we multiply
                /// its value with index 0.
                ///
                /// If the root entity has a parent, it's akin to the root motion: the index 0 contains an offset
                /// between the rig and the root bone (that is, it contains the root to rig transform). We store the rig's
                /// LocalToWorld, that will get multiplied with this offset during the remapping.
                for (int i = 0; i != batchInChunk.Count; ++i)
                {
                    var rootEntity = rigRoots[i].Value;
                    var rootTransform = mathex.AffineTransform(EntityLocalToWorld[rootEntity].Value);

                    rigRoots[i] = new RigRootEntity
                    {
                        Value =  rootEntity,
                        RemapToRootMatrix = !Parent.HasComponent(rootEntity) ? mathex.inverse(rootTransform) : rootTransform
                    };
                }
            }
        }
    }


    /// <summary>
    /// This job writes the root transform values from the animation stream back to the
    /// rig entity transform components when the root motion component and the
    /// disable root transform R/W tag are not present.
    /// </summary>
    [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
    internal struct WriteRootTransformJob<TAnimatedRootMotion> : IJobEntityBatch
        where TAnimatedRootMotion : struct, IAnimatedRootMotion
    {
        static readonly Translation k_DefaultTranslation = new Translation { Value = float3.zero };
        static readonly Rotation k_DefaultRotation = new Rotation { Value = quaternion.identity };
        static readonly NonUniformScale k_DefaultNonUniformScale = new NonUniformScale { Value = math.float3(1f) };

        static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<Rig>(),
                ComponentType.ReadOnly<RigRootEntity>(),
                ComponentType.ReadWrite<AnimatedData>()
            },
            None = new ComponentType[]
            {
                typeof(DisableRootTransformReadWriteTag),
                typeof(TAnimatedRootMotion)
            }
        };

        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<LocalToWorld>    EntityLocalToWorld;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<LocalToParent>   EntityLocalToParent;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<Translation>     EntityTranslation;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<Rotation>        EntityRotation;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<Scale>           EntityScale;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<NonUniformScale> EntityNonUniformScale;

        [ReadOnly] public ComponentTypeHandle<Rig>           RigType;
        [ReadOnly] public ComponentTypeHandle<RigRootEntity> RigRootEntityType;
        public BufferTypeHandle<AnimatedData>                AnimatedDataType;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var animatedDataAccessor = batchInChunk.GetBufferAccessor(AnimatedDataType);
            var rigs = batchInChunk.GetNativeArray(RigType);
            var rigRoots = batchInChunk.GetNativeArray(RigRootEntityType);

            for (int i = 0; i != batchInChunk.Count; ++i)
            {
                var stream = AnimationStream.Create(rigs[i], animatedDataAccessor[i].AsNativeArray());

                Translation t;
                Rotation r;
                NonUniformScale s;
                stream.GetLocalToParentTRS(0, out t.Value, out r.Value, out s.Value);
                var rigRoot = rigRoots[i].Value;

                t = EntityTranslation.HasComponent(rigRoot) ?
                    SelectAndSetEntityComponentData(rigRoot, EntityTranslation, t, stream.PassMask.IsTranslationSet(0)) :
                    k_DefaultTranslation;

                r = EntityRotation.HasComponent(rigRoot) ?
                    SelectAndSetEntityComponentData(rigRoot, EntityRotation, r, stream.PassMask.IsRotationSet(0)) :
                    k_DefaultRotation;

                if (EntityNonUniformScale.HasComponent(rigRoot))
                    s = SelectAndSetEntityComponentData(rigRoot, EntityNonUniformScale, s, stream.PassMask.IsScaleSet(0));
                else if (EntityScale.HasComponent(rigRoot))
                    s.Value = SelectAndSetEntityComponentData(rigRoot, EntityScale, new Scale { Value = s.Value.x }, stream.PassMask.IsScaleSet(0)).Value;
                else
                    s = k_DefaultNonUniformScale;

                if (EntityLocalToParent.HasComponent(rigRoot))
                    EntityLocalToParent[rigRoot] = new LocalToParent { Value = float4x4.TRS(t.Value, r.Value, s.Value) };
                else
                    EntityLocalToWorld[rigRoot] = new LocalToWorld { Value = float4x4.TRS(t.Value, r.Value, s.Value) };

                // Since root values will now be on the entity components
                // reset animation buffer root values to identity
                stream.SetLocalToParentTRS(0, float3.zero, quaternion.identity, 1f);
            }
        }

        static TValue SelectAndSetEntityComponentData<TValue>(Entity entity, ComponentDataFromEntity<TValue> EntityData, TValue value, bool channelMask)
            where TValue : struct, IComponentData
        {
            if (channelMask)
                EntityData[entity] = value;
            else
                value = EntityData[entity];

            return value;
        }
    }

    /// <summary>
    /// This job accumulates the root transform values from the animation stream
    /// in the rig entity transform components. Root motion delta values
    /// are stored in the system specific IAnimatedRootMotion components.
    /// </summary>
    [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
    internal struct AccumulateRootTransformJob<TAnimatedRootMotion> : IJobEntityBatch
        where TAnimatedRootMotion : struct, IAnimatedRootMotion
    {
        static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<Rig>(),
                ComponentType.ReadOnly<RigRootEntity>(),
                ComponentType.ReadWrite<AnimatedData>(),
                ComponentType.ReadWrite<TAnimatedRootMotion>()
            },
            None = new ComponentType[]
            {
                typeof(DisableRootTransformReadWriteTag)
            }
        };

        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<LocalToWorld>    EntityLocalToWorld;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<LocalToParent>   EntityLocalToParent;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<Translation>     EntityTranslation;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<Rotation>        EntityRotation;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<Scale>           EntityScale;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<NonUniformScale> EntityNonUniformScale;

        public ComponentTypeHandle<RootMotionOffset> RootMotionOffsetType;

        // TODO: Disabling safety restriction should not be needed for the RootMotionType.
        // For some reason the job safety system complains that it should be marked ReadOnly
        // when that is not the case since this job writes to this component.
        // This started happening as soon as I converted to a generic type with auto-property.
        [NativeDisableContainerSafetyRestriction]
        public ComponentTypeHandle<TAnimatedRootMotion> RootMotionType;

        [ReadOnly] public ComponentTypeHandle<Rig>           RigType;
        [ReadOnly] public ComponentTypeHandle<RigRootEntity> RigRootEntityType;
        public BufferTypeHandle<AnimatedData>                AnimatedDataType;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var rootMotionOffsets = batchInChunk.GetNativeArray(RootMotionOffsetType);
            var rootMotions = batchInChunk.GetNativeArray(RootMotionType).Reinterpret<RigidTransform>();
            var animatedDataAccessor = batchInChunk.GetBufferAccessor(AnimatedDataType);
            var rigs = batchInChunk.GetNativeArray(RigType);
            var rigRoots = batchInChunk.GetNativeArray(RigRootEntityType);

            for (int i = 0; i < batchInChunk.Count; ++i)
            {
                var stream = AnimationStream.Create(rigs[i], animatedDataAccessor[i].AsNativeArray());
                var rigRoot = rigRoots[i].Value;

                var deltaTx = RigidTransform.identity;
                if (stream.PassMask.IsTranslationSet(0))
                    deltaTx.pos = stream.GetLocalToParentTranslation(0);
                if (stream.PassMask.IsRotationSet(0))
                    deltaTx.rot = stream.GetLocalToParentRotation(0);

                RigidTransform absoluteTx = RigidTransform.identity;
                if (EntityTranslation.HasComponent(rigRoot))
                    absoluteTx.pos = EntityTranslation[rigRoot].Value;
                if (EntityRotation.HasComponent(rigRoot))
                    absoluteTx.rot = EntityRotation[rigRoot].Value;

                // Accumulate root motion
                absoluteTx = math.mul(absoluteTx, deltaTx);
                if (rootMotionOffsets.Length > 0)
                {
                    absoluteTx = math.mul(rootMotionOffsets[i].Value, absoluteTx);
                    rootMotionOffsets[i] = new RootMotionOffset { Value = RigidTransform.identity };
                }

                // Update delta value
                rootMotions[i] = deltaTx;

                // Update transform components
                if (EntityTranslation.HasComponent(rigRoot))
                    EntityTranslation[rigRoot] = new Translation { Value = absoluteTx.pos };
                if (EntityRotation.HasComponent(rigRoot))
                    EntityRotation[rigRoot] = new Rotation { Value = absoluteTx.rot };

                // TODO: Support root motion scale
                float3 scale = math.float3(1f);
                if (EntityNonUniformScale.HasComponent(rigRoot))
                    scale *= EntityNonUniformScale[rigRoot].Value;
                else if (EntityScale.HasComponent(rigRoot))
                    scale *= EntityScale[rigRoot].Value;

                if (EntityLocalToParent.HasComponent(rigRoot))
                    EntityLocalToParent[rigRoot] = new LocalToParent { Value = float4x4.TRS(absoluteTx.pos, absoluteTx.rot, scale) };
                else
                    EntityLocalToWorld[rigRoot] = new LocalToWorld { Value = float4x4.TRS(absoluteTx.pos, absoluteTx.rot, scale) };

                // Reset root transform in stream to identity
                stream.SetLocalToParentTRS(0, float3.zero, quaternion.identity, 1f);
            }
        }
    }
}
