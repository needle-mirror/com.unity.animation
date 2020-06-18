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
    internal struct ReadRootTransformJob<TTag, TAnimatedRootMotion> : IJobChunk
        where TTag : struct, IAnimationSystemTag
        where TAnimatedRootMotion : struct, IAnimatedRootMotion
    {
        static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<TTag>(),
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

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var animatedDataAccessor = chunk.GetBufferAccessor(AnimatedDataType);
            var rigs = chunk.GetNativeArray(RigType);
            var rigRoots = chunk.GetNativeArray(RigRootEntityType);

            for (int i = 0; i != chunk.Count; ++i)
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
    /// This job writes the root transform values from the animation stream back to the
    /// rig entity transform components when the root motion component and the
    /// disable root transform R/W tag are not present.
    /// </summary>
    [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
    internal struct WriteRootTransformJob<TTag, TAnimatedRootMotion> : IJobChunk
        where TTag : struct, IAnimationSystemTag
        where TAnimatedRootMotion : struct, IAnimatedRootMotion
    {
        static readonly Translation k_DefaultTranslation = new Translation { Value = float3.zero };
        static readonly Rotation k_DefaultRotation = new Rotation { Value = quaternion.identity };
        static readonly NonUniformScale k_DefaultNonUniformScale = new NonUniformScale { Value = math.float3(1f) };

        static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<TTag>(),
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

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var animatedDataAccessor = chunk.GetBufferAccessor(AnimatedDataType);
            var rigs = chunk.GetNativeArray(RigType);
            var rigRoots = chunk.GetNativeArray(RigRootEntityType);

            for (int i = 0; i != chunk.Count; ++i)
            {
                var stream = AnimationStream.Create(rigs[i], animatedDataAccessor[i].AsNativeArray());

                Translation t;
                Rotation r;
                NonUniformScale s;
                stream.GetLocalToParentTRS(0, out t.Value, out r.Value, out s.Value);
                var rigRoot = rigRoots[i].Value;

                t = EntityTranslation.HasComponent(rigRoot) ?
                    SelectAndSetEntityComponentData(rigRoot, EntityTranslation, t, stream.GetTranslationChannelMask(0)) :
                    k_DefaultTranslation;

                r = EntityRotation.HasComponent(rigRoot) ?
                    SelectAndSetEntityComponentData(rigRoot, EntityRotation, r, stream.GetRotationChannelMask(0)) :
                    k_DefaultRotation;

                if (EntityNonUniformScale.HasComponent(rigRoot))
                    s = SelectAndSetEntityComponentData(rigRoot, EntityNonUniformScale, s, stream.GetScaleChannelMask(0));
                else if (EntityScale.HasComponent(rigRoot))
                    s.Value = SelectAndSetEntityComponentData(rigRoot, EntityScale, new Scale { Value = s.Value.x }, stream.GetScaleChannelMask(0)).Value;
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
    internal struct AccumulateRootTransformJob<TTag, TAnimatedRootMotion> : IJobChunk
        where TTag : struct, IAnimationSystemTag
        where TAnimatedRootMotion : struct, IAnimatedRootMotion
    {
        static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<TTag>(),
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
#if UNITY_ENTITIES_0_12_OR_NEWER
        [NativeDisableContainerSafetyRestriction]
        public ComponentTypeHandle<TAnimatedRootMotion> RootMotionType;
#else
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly] public ComponentTypeHandle<TAnimatedRootMotion> RootMotionType;
#endif

        [ReadOnly] public ComponentTypeHandle<Rig>           RigType;
        [ReadOnly] public ComponentTypeHandle<RigRootEntity> RigRootEntityType;
        public BufferTypeHandle<AnimatedData>                AnimatedDataType;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var rootMotionOffsets = chunk.GetNativeArray(RootMotionOffsetType);
            var rootMotions = chunk.GetNativeArray(RootMotionType).Reinterpret<RigidTransform>();
            var animatedDataAccessor = chunk.GetBufferAccessor(AnimatedDataType);
            var rigs = chunk.GetNativeArray(RigType);
            var rigRoots = chunk.GetNativeArray(RigRootEntityType);

            for (int i = 0; i < chunk.Count; ++i)
            {
                var stream = AnimationStream.Create(rigs[i], animatedDataAccessor[i].AsNativeArray());
                var rigRoot = rigRoots[i].Value;

                var deltaTx = RigidTransform.identity;
                if (stream.GetTranslationChannelMask(0))
                    deltaTx.pos = stream.GetLocalToParentTranslation(0);
                if (stream.GetRotationChannelMask(0))
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

#if UNITY_ENTITIES_0_12_OR_NEWER
                // Update delta value
                rootMotions[i] = deltaTx;
#else
                // HACK: For entities 0.11, hack the safety system by using a ReadOnlyPtr to make it
                // think we are only reading and never writing.
                unsafe { *((RigidTransform*)rootMotions.GetUnsafeReadOnlyPtr() + i) = deltaTx; }
#endif

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
