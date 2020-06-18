using Unity.Entities;
using Unity.Collections;
using UnityEngine.Assertions;

namespace Unity.Animation
{
    public static class RigUtils
    {
        static void ValidateRigEntity(Entity rigEntity, EntityManager entityManager)
        {
            Assert.IsTrue(entityManager.HasComponent<Rig>(rigEntity));
            Assert.IsTrue(entityManager.HasComponent<RigRootEntity>(rigEntity));
            Assert.IsTrue(entityManager.HasComponent<AnimatedData>(rigEntity));
            Assert.IsTrue(entityManager.HasComponent<AnimatedLocalToWorld>(rigEntity));
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
            entityManager.AddComponentData(debugEntity, new RigRootEntity { Value = debugEntity });
            entityManager.AddComponent<DisableRootTransformReadWriteTag>(debugEntity);

            ValidateRigEntity(debugEntity, entityManager);
            BoneRendererEntityBuilder.CreateBoneRendererEntities(debugEntity, entityManager, rigDefinition, props, ids);

            return debugEntity;
        }
    }
}
