using Unity.Entities;

namespace Unity.Animation.Hybrid
{
    public class SkeletonConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((Skeleton skeleton) =>
            {
                Entity skeletonEntity = GetPrimaryEntity(skeleton);

                RigEntityBuilder.SetupRigEntity(skeletonEntity, DstEntityManager, skeleton.RigDefinitionPrefab);
            });
        }
    }
}
