#if UNITY_ENABLE_ANIMATION_ANIMATOR_CONVERSION
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace Unity.Animation.Hybrid
{
    [ConverterVersion("Unity.Animation.Hybrid.AnimatorConversion", 2)]
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    internal class AnimatorConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities
                .WithNone<RigComponent>() // If a RigComponent is present, we prefer the RigConversion system
                .ForEach((Animator animatorComponent) =>
                {
                    var rigEntity = TryGetPrimaryEntity(animatorComponent);
                    using (var skeletonNodes = new NativeList<SkeletonNode>(Allocator.Temp))
                    {
                        animatorComponent.ExtractSkeletonNodes(skeletonNodes);
                        var rigDefinition = RigBuilder.CreateRigDefinition(skeletonNodes.ToArray(), null, null);
                        RigEntityBuilder.SetupRigEntity(rigEntity, DstEntityManager, rigDefinition);
                    }

                    var boneTransforms = animatorComponent.ExtractBoneTransforms();
                    RigConversion.ExposeTransforms(animatorComponent, boneTransforms, this, DstEntityManager, rigEntity);
                });
        }
    }
}
#endif
