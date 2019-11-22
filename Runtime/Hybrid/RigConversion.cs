using Unity.Mathematics;
using Unity.Entities;

namespace Unity.Animation.Hybrid
{
    [ConverterVersion("Unity.Animation.Hybrid.RigConversion", 1)]
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

                // BlobAssetReference on SharedComponent is not supported by subscene
                // For now we need to use a proxy RigDefinitionComponent on conversion and change it at runtime
                // for a SharedRigDefinition
                //RigEntityBuilder.SetupRigEntity(rigEntity, DstEntityManager, rigDefinition);
                DstEntityManager.AddComponentData(rigEntity, new RigDefinitionSetup {
                    Value = rigDefinition
                });
            });
        }
    }
}
