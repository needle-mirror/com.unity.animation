using System.Collections.Generic;

using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animation
{
    internal struct BurstCompatibleTransformHandle : IReadTransformHandle, IWriteTransformHandle
    {
        public Entity Entity { get; set; }
        public int Index { get; set; }
    }

    public static class RigEntityBuilder
    {
        [BurstCompatible(GenericTypeArguments = new[] { typeof(BurstCompatibleTransformHandle) })]
        internal struct TransformHandleComparer<T> : IComparer<T> where T : ITransformHandle
        {
            public int Compare(T x, T y) => x.Index.CompareTo(y.Index);
        }

        public static readonly ComponentTypes RigComponentTypes = new ComponentTypes(
            typeof(Rig),
            typeof(SharedRigHash),
            typeof(AnimatedData),
            typeof(AnimatedLocalToWorld)
        );

        public static readonly ComponentTypes RigPrefabComponentTypes = new ComponentTypes(
            typeof(Rig),
            typeof(SharedRigHash),
            typeof(AnimatedData),
            typeof(AnimatedLocalToWorld),
            typeof(Prefab)
        );

        [BurstCompatible]
        public struct RigBuffers
        {
            public DynamicBuffer<AnimatedData>         Data;
            public DynamicBuffer<AnimatedLocalToWorld> GlobalMatrices;

            public RigBuffers(EntityManager entityManager, Entity entity)
            {
                Data = entityManager.GetBuffer<AnimatedData>(entity);
                GlobalMatrices = entityManager.GetBuffer<AnimatedLocalToWorld>(entity);
            }

            public void ResizeBuffers(BlobAssetReference<RigDefinition> rigDefinition)
            {
                Data.ResizeUninitialized(rigDefinition.Value.Bindings.StreamSize);
                GlobalMatrices.ResizeUninitialized(rigDefinition.Value.Skeleton.BoneCount);
            }

            unsafe public void InitializeBuffers(BlobAssetReference<RigDefinition> rigDefinition)
            {
                ref var defaultValues = ref rigDefinition.Value.DefaultValues;

                Core.ValidateBufferLengthsAreEqual(Data.Length, defaultValues.Length);

                UnsafeUtility.MemCpy(Data.GetUnsafePtr(), defaultValues.GetUnsafePtr(), UnsafeUtility.SizeOf<float>() * defaultValues.Length);
            }
        }

        private static void InitializeComponentsData(Entity entity, EntityManager entityManager, BlobAssetReference<RigDefinition> rigDefinition)
        {
            entityManager.SetComponentData(entity, new Rig { Value = rigDefinition });
            entityManager.SetSharedComponentData(entity, new SharedRigHash { Value = rigDefinition.Value.GetHashCode() });

            var rigBuffers = new RigBuffers(entityManager, entity);

            rigBuffers.ResizeBuffers(rigDefinition);
            rigBuffers.InitializeBuffers(rigDefinition);
        }

        public static Entity CreatePrefabEntity(EntityManager entityManager, BlobAssetReference<RigDefinition> rigDefinition)
        {
            var prefab = entityManager.CreateEntity();
            entityManager.AddComponents(prefab, RigPrefabComponentTypes);

            InitializeComponentsData(prefab, entityManager, rigDefinition);

            if (!entityManager.HasComponent<LocalToWorld>(prefab))
                entityManager.AddComponentData(prefab, new LocalToWorld { Value = float4x4.identity });

            return prefab;
        }

        public static void SetupRigEntity(Entity entity, EntityManager entityManager, BlobAssetReference<RigDefinition> rigDefinition)
        {
            entityManager.AddComponents(entity, RigComponentTypes);

            InitializeComponentsData(entity, entityManager, rigDefinition);

            if (!entityManager.HasComponent<LocalToWorld>(entity))
                entityManager.AddComponentData(entity, new LocalToWorld { Value = float4x4.identity });
        }

        private static void AddTransformHandle<T>(EntityManager entityManager, Entity rig, Entity transform, int index)
            where T : struct, ITransformHandle
        {
            var buffer = entityManager.HasComponent<T>(rig) ? entityManager.GetBuffer<T>(rig) : entityManager.AddBuffer<T>(rig);
            buffer.Add(new T { Entity = transform, Index = index });
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(BurstCompatibleTransformHandle) })]
        public static void AddReadTransformHandle<T>(EntityManager entityManager, Entity rig, Entity transform, int index)
            where T : struct, IReadTransformHandle
        {
            AddTransformHandle<T>(entityManager, rig, transform, index);
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(BurstCompatibleTransformHandle) })]
        public static void AddWriteTransformHandle<T>(EntityManager entityManager, Entity rig, Entity transform, int index)
            where T : struct, IWriteTransformHandle
        {
            AddTransformHandle<T>(entityManager, rig, transform, index);

            entityManager.AddComponent<AnimationLocalToWorldOverride>(transform);
            entityManager.AddComponent<AnimationLocalToParentOverride>(transform);
        }
    }
}
