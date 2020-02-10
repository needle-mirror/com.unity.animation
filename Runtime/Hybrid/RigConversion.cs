using Unity.Entities;

namespace Unity.Animation.Hybrid
{
    [ConverterVersion("Unity.Animation.Hybrid.RigConversion", 2)]
    public class RigConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((RigComponent rigComponent) =>
            {
                var rigEntity = GetPrimaryEntity(rigComponent);
                var skeletonNodes = RigGenerator.ExtractSkeletonNodesFromRigComponent(rigComponent);
                var channels = RigGenerator.ExtractAnimationChannelFromRigComponent(rigComponent);
                var rigDefinition = RigBuilder.CreateRigDefinition(skeletonNodes, null, channels);
                RigEntityBuilder.SetupRigEntity(rigEntity, DstEntityManager, rigDefinition);
            });
        }
    }
}
