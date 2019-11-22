using Unity.Entities;
using Unity.Collections;
using UnityEngine.Assertions;

namespace Unity.Animation
{
    public static class RigUtils
    {
        static void ValidateRigEntity(Entity rigEntity, EntityManager entityManager)
        {
            Assert.IsTrue(entityManager.HasComponent<SharedRigDefinition>(rigEntity));
            Assert.IsTrue(entityManager.HasComponent<AnimatedLocalTranslation>(rigEntity));
            Assert.IsTrue(entityManager.HasComponent<AnimatedLocalRotation>(rigEntity));
            Assert.IsTrue(entityManager.HasComponent<AnimatedLocalScale>(rigEntity));
            Assert.IsTrue(entityManager.HasComponent<AnimatedLocalToWorld>(rigEntity));
        }

        public static Entity InstantiateDebugRigEntity(Entity rigEntity, EntityManager entityManager, in BoneRendererProperties props, NativeList<StringHash> ids = default)
        {
            ValidateRigEntity(rigEntity, entityManager);

            var debugEntity = entityManager.Instantiate(rigEntity);
            var rigDefinition = entityManager.GetSharedComponentData<SharedRigDefinition>(debugEntity).Value;
            BoneRendererEntityBuilder.CreateBoneRendererEntities(debugEntity, entityManager, rigDefinition, props, ids);

            return debugEntity;
        }

        public static Entity InstantiateDebugRigEntity(BlobAssetReference<RigDefinition> rigDefinition, EntityManager entityManager, in BoneRendererProperties props, NativeList<StringHash> ids = default)
        {
            var debugEntity = entityManager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(debugEntity, entityManager, rigDefinition);
            ValidateRigEntity(debugEntity, entityManager);

            BoneRendererEntityBuilder.CreateBoneRendererEntities(debugEntity, entityManager, rigDefinition, props, ids);

            return debugEntity;
        }
    }
}
