using System.Diagnostics;

using Unity.Entities;
using Unity.Collections;

namespace Unity.Animation
{
    public static class RigUtils
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateRigEntity(Entity rigEntity, EntityManager entityManager)
        {
            if (!entityManager.HasComponent<Rig>(rigEntity))
                throw new System.InvalidOperationException($"A component with type:Rig has not been added to the entity.");

            if (!entityManager.HasComponent<RigRootEntity>(rigEntity))
                throw new System.InvalidOperationException($"A component with type:RigRootEntity has not been added to the entity.");

            if (!entityManager.HasComponent<AnimatedData>(rigEntity))
                throw new System.InvalidOperationException($"A component with type:AnimatedData has not been added to the entity.");

            if (!entityManager.HasComponent<AnimatedLocalToWorld>(rigEntity))
                throw new System.InvalidOperationException($"A component with type:AnimatedLocalToWorld has not been added to the entity.");
        }

        public static Entity InstantiateDebugRigEntity(Entity rigEntity, EntityManager entityManager, in BoneRendererProperties props, NativeList<StringHash> ids = default)
        {
            ValidateRigEntity(rigEntity, entityManager);

            var debugEntity = entityManager.Instantiate(rigEntity);
            var rigDefinition = entityManager.GetComponentData<Rig>(debugEntity).Value;
            BoneRendererEntityBuilder.CreateBoneRendererEntities(debugEntity, entityManager, rigDefinition, props, ids);

            return debugEntity;
        }

        public static Entity InstantiateDebugRigEntity(BlobAssetReference<RigDefinition> rigDefinition, EntityManager entityManager, in BoneRendererProperties props, NativeList<StringHash> ids = default)
        {
            var debugEntity = entityManager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(debugEntity, entityManager, rigDefinition);
            entityManager.AddComponentData(debugEntity,
                new RigRootEntity
                {
                    Value = debugEntity,
                    RemapToRootMatrix = AffineTransform.identity
                });
            entityManager.AddComponent<DisableRootTransformReadWriteTag>(debugEntity);

            ValidateRigEntity(debugEntity, entityManager);
            BoneRendererEntityBuilder.CreateBoneRendererEntities(debugEntity, entityManager, rigDefinition, props, ids);

            return debugEntity;
        }
    }
}
