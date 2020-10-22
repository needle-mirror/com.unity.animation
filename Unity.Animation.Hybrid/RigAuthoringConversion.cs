using Unity.Animation.Authoring;
using Unity.Entities;

namespace Unity.Animation.Hybrid
{
    [ConverterVersion("simonbz", 2)]
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    public class RigAuthoringConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((RigAuthoring rigAuthoring) =>
            {
                if (rigAuthoring.Skeleton == null)
                    return;

                var rigEntity = TryGetPrimaryEntity(rigAuthoring);

                var rigDefinition = rigAuthoring.Skeleton.ToRigDefinition();
                RigEntityBuilder.SetupRigEntity(rigEntity, DstEntityManager, rigDefinition);

                // TODO. Create attachments from Skeleton data.
                // Q. How do we define attachments in the skeleton?
                // - By retrieving transforms child of the RigAuthoring component and verifying that they
                //   have a IExposeTransform to process them.
                // - By identifying the exposed transforms directly in the skeleton asset.
                RigConversion.ExposeTransforms(rigAuthoring, ((IRigAuthoring)rigAuthoring).Bones, this, DstEntityManager, rigEntity);

                // TODO. Create sockets from Skeleton data.
            });
        }
    }
}
