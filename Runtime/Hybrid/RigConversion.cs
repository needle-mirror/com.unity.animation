using UnityEngine;
using Unity.Entities;

namespace Unity.Animation.Hybrid
{
    [ConverterVersion("Unity.Animation.Hybrid.RigConversion", 5)]
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    public class RigConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((RigComponent rigComponent) =>
            {
                var rigEntity = TryGetPrimaryEntity(rigComponent);
                var skeletonNodes = RigGenerator.ExtractSkeletonNodesFromRigComponent(rigComponent);
                var channels = RigGenerator.ExtractAnimationChannelFromRigComponent(rigComponent);
                var rigDefinition = RigBuilder.CreateRigDefinition(skeletonNodes, null, channels);
                RigEntityBuilder.SetupRigEntity(rigEntity, DstEntityManager, rigDefinition);

                foreach (var transform in rigComponent.Bones)
                {
                    Component[] exposeTransformComponents = transform.GetComponents(typeof(IExposeTransform));
                    foreach(var component in exposeTransformComponents)
                    {
                        var entity = GetPrimaryEntity(transform);
                        if (entity != Entity.Null)
                        {
                            Entity bone = TryGetPrimaryEntity(transform);
                            int boneIndex = RigGenerator.FindTransformIndex(transform, rigComponent.Bones);

                            var exposeTransform = component as IExposeTransform;
                            if(exposeTransform != null)
                            {
                                exposeTransform.AddExposeTransform(DstEntityManager, rigEntity, bone, boneIndex);
                            }
                        }
                    }
                }
            });
        }
    }

    /*
    [ConverterVersion("Unity.Animation.Hybrid.RigConversionCleanup", 1)]
    [UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
    public class RigConversionCleanup : GameObjectConversionSystem
    {
        public Type[] RequiredComponents = new [] { typeof(RigComponent), typeof(SkinnedMesh), typeof(BoneRendererComponent) };

        protected override void OnUpdate()
        {
            Entities.ForEach((RigComponent rigComponent) =>
            {
                var rigEntity = TryGetPrimaryEntity(rigComponent);

                foreach (var bone in rigComponent.Bones)
                {
                    bool isExposed = false;
                    foreach(var exposedBone in rigComponent.RigBones.ExposedBones)
                    {
                        isExposed = bone == exposedBone.Bone;
                        if (isExposed)
                            break;
                    }

                    var entity = GetPrimaryEntity(bone);

                    if(!isExposed && CanDeleteEntity(bone.gameObject) )
                    {
                        // TODO@sonny uncomment when this pr land in dots
                        // https://github.com/Unity-Technologies/dots/pull/4474
                        //DstEntityManager.DestroyEntity(entity);
                    }

                    if(DstEntityManager.Exists(entity) && DstEntityManager.HasComponent<Parent>(entity))
                    {
                        var parent = DstEntityManager.GetComponentData<Parent>(entity);
                        if(parent.Value == Entity.Null)
                        {
                            DstEntityManager.SetComponentData(entity, new Parent { Value = rigEntity } );
                        }
                    }
                }
            });
        }

        bool CanDeleteEntity(GameObject gameObject)
        {
            for(int i = 0; i < RequiredComponents.Length; i++)
            {
                if(gameObject.TryGetComponent(RequiredComponents[i], out _))
                {
                    return false;
                }
            }

            return true;
        }
    }
    */
}
