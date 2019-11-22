using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animation
{
    public static class RigEntityBuilder
    {
        public static ComponentType[] RigComponentTypes = {
            typeof(SharedRigDefinition),
            typeof(AnimatedLocalTranslation),
            typeof(AnimatedLocalRotation),
            typeof(AnimatedLocalScale),
            typeof(AnimatedFloat),
            typeof(AnimatedInt),
            typeof(AnimatedChannelMask),
            typeof(AnimatedLocalToWorld)
        };

        public static ComponentType[] RigPrefabComponentTypes = ConcatComponentTypeArrays(new ComponentType[] { typeof(Prefab) }, RigComponentTypes);

        public struct RigBuffers
        {
            public DynamicBuffer<AnimatedLocalTranslation>     LocalTranslations;
            public DynamicBuffer<AnimatedLocalRotation>     LocalRotations;
            public DynamicBuffer<AnimatedLocalScale>        LocalScales;
            public DynamicBuffer<AnimatedFloat>             Floats;
            public DynamicBuffer<AnimatedInt>               Integers;
            public DynamicBuffer<AnimatedChannelMask>       Masks;

            public DynamicBuffer<AnimatedLocalToWorld>      GlobalMatrices;

            public RigBuffers(EntityManager entityManager, Entity entity)
            {
                LocalRotations = entityManager.GetBuffer<AnimatedLocalRotation>(entity);
                LocalTranslations = entityManager.GetBuffer<AnimatedLocalTranslation>(entity);
                LocalScales = entityManager.GetBuffer<AnimatedLocalScale>(entity);
                Floats = entityManager.GetBuffer<AnimatedFloat>(entity);
                Integers = entityManager.GetBuffer<AnimatedInt>(entity);
                Masks = entityManager.GetBuffer<AnimatedChannelMask>(entity);
                GlobalMatrices = entityManager.GetBuffer<AnimatedLocalToWorld>(entity);
            }

            public void ResizeBuffers(BlobAssetReference<RigDefinition> rigDefinition)
            {
                LocalTranslations.ResizeUninitialized(rigDefinition.Value.Bindings.TranslationBindings.Length);
                LocalRotations.ResizeUninitialized(rigDefinition.Value.Bindings.RotationBindings.Length);
                LocalScales.ResizeUninitialized(rigDefinition.Value.Bindings.ScaleBindings.Length);
                Floats.ResizeUninitialized(rigDefinition.Value.Bindings.FloatBindings.Length);
                Integers.ResizeUninitialized(rigDefinition.Value.Bindings.IntBindings.Length);
                Masks.ResizeUninitialized(rigDefinition.Value.Bindings.BindingCount);

                GlobalMatrices.ResizeUninitialized(rigDefinition.Value.Skeleton.BoneCount);
            }

            unsafe private void InitializeBuffer<T>(DynamicBuffer<T> buffer, T defaultValue) where T : unmanaged
            {
                UnsafeUtility.MemCpyReplicate(buffer.GetUnsafePtr(), &defaultValue, UnsafeUtility.SizeOf<T>(), buffer.Length);
            }

            unsafe private void InitializeBuffer<T>(DynamicBuffer<T> buffer, ref BlobArray<T> defaultValues) where T : struct
            {
                UnsafeUtility.MemCpy(buffer.GetUnsafePtr(), defaultValues.GetUnsafePtr(), UnsafeUtility.SizeOf<T>() * buffer.Length);
            }

            public void InitializeBuffers(BlobAssetReference<RigDefinition> rigDefinition)
            {
                InitializeBuffer(LocalTranslations.Reinterpret<float3>(), ref rigDefinition.Value.DefaultValues.LocalTranslations);
                InitializeBuffer(LocalRotations.Reinterpret<quaternion>(), ref rigDefinition.Value.DefaultValues.LocalRotations);
                InitializeBuffer(LocalScales.Reinterpret<float3>(), ref rigDefinition.Value.DefaultValues.LocalScales);
                InitializeBuffer(Floats.Reinterpret<float>(), ref rigDefinition.Value.DefaultValues.Floats);
                InitializeBuffer(Integers.Reinterpret<int>(), ref rigDefinition.Value.DefaultValues.Integers);
                InitializeBuffer(Masks.Reinterpret<byte>(), new byte());
            }
        }

        private static void InitializeComponentsData(Entity entity, EntityManager entityManager, BlobAssetReference<RigDefinition> rigDefinition)
        {
            entityManager.SetSharedComponentData<SharedRigDefinition>(entity, new SharedRigDefinition { Value = rigDefinition } );

            var rigBuffers = new RigBuffers(entityManager, entity);

            rigBuffers.ResizeBuffers(rigDefinition);
            rigBuffers.InitializeBuffers(rigDefinition);
        }

        public static Entity CreatePrefabEntity(EntityManager entityManager, BlobAssetReference<RigDefinition> rigDefinition)
        {
            var prefab = entityManager.CreateEntity( RigPrefabComponentTypes );

            InitializeComponentsData(prefab, entityManager, rigDefinition);

            return prefab;
        }

        public static void SetupRigEntity(Entity entity, EntityManager entityManager, BlobAssetReference<RigDefinition> rigDefinition)
        {
            var componentTypes = new ComponentTypes(RigComponentTypes);
            entityManager.AddComponents(entity, componentTypes);

            InitializeComponentsData(entity, entityManager, rigDefinition);
        }

        private static ComponentType[] ConcatComponentTypeArrays(ComponentType[] a1, ComponentType[] a2)
        {
            var res = new ComponentType[a1.Length + a2.Length];
            a1.CopyTo(res, 0);
            a2.CopyTo(res, a1.Length);
            return res;
        }
    }
}


