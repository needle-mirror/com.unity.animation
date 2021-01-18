using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

namespace Unity.Animation.Hybrid
{
    [ConverterVersion("Unity.Animation.Hybrid.RigConversion", 12)]
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    public class RigConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((RigComponent rigComponent) =>
            {
                var rigEntity = TryGetPrimaryEntity(rigComponent);
                var rigDefinition = rigComponent.ToRigDefinition();

                RigEntityBuilder.SetupRigEntity(rigEntity, DstEntityManager, rigDefinition);

                ExposeTransforms(rigComponent, this, DstEntityManager, rigEntity);
            });
        }

        internal static void ExposeTransforms(IRigAuthoring rigAuthoring, GameObjectConversionSystem system, EntityManager entityManager, Entity rigEntity)
        {
            var bones = new List<RigIndexToBone>();
            rigAuthoring.GetBones(bones);

            var boneCount = bones.Count;
            if (boneCount == 0)
            {
                // Don't perform any read/write back of the root transform entity values to the animation stream
                // since we don't have any bones in our definition
                entityManager.AddComponent<DisableRootTransformReadWriteTag>(rigEntity);
                return;
            }

            var rootBoneIndex = bones.FindIndex((bone) => bone.Index == 0);
            if (rootBoneIndex != -1)
            {
                var rootBone = bones[rootBoneIndex].Bone;
                if (rootBone != null)
                {
                    // Warn that any exposed transform on the root bone are ignored. The root bone is always exposed.
                    var exposedTransforms = rootBone.GetComponents(typeof(IExposeTransform));
                    if (exposedTransforms?.Length > 0)
                        Debug.LogWarning($"Root transform [{rootBone.name}] cannot have IExposeTransform components and will be ignored");

                    entityManager.AddComponentData(rigEntity,
                        new RigRootEntity
                        {
                            Value = system.TryGetPrimaryEntity(rootBone),
                            RemapToRootMatrix = AffineTransform.identity
                        });
                }
                else
                {
                    Debug.LogWarning($"No bone associated with root index");
                }

                bones.RemoveAt(rootBoneIndex);
            }

            for (int i = 0; i < bones.Count; ++i)
            {
                var transform = bones[i].Bone;
                if (transform == null)
                    continue;

                Component[] exposeTransformComponents = transform.GetComponents(typeof(IExposeTransform));
                if (exposeTransformComponents?.Length > 0)
                {
                    foreach (var exposeTransformComponent in exposeTransformComponents)
                    {
                        var entity = system.GetPrimaryEntity(transform);
                        if (entity != Entity.Null)
                        {
                            Entity bone = system.TryGetPrimaryEntity(transform);
                            var exposeTransform = exposeTransformComponent as IExposeTransform;
                            if (exposeTransform != null)
                            {
                                exposeTransform.AddExposeTransform(entityManager, rigEntity, bone, bones[i].Index);
                            }
                        }
                    }
                }
            }
        }
    }

#if UNITY_ENABLE_ANIMATION_RIG_CONVERSION_CLEANUP

    [ConverterVersion("Unity.Animation.Hybrid.RigConversionCleanup", 2)]
    [UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
    public class RigConversionCleanup : GameObjectConversionSystem
    {
        static readonly ComponentType[] k_TransformComponentTypes = new[]
        {
            ComponentType.ReadWrite<LocalToWorld>(),
            ComponentType.ReadWrite<LocalToParent>(),
            ComponentType.ReadWrite<Rotation>(),
            ComponentType.ReadWrite<CompositeRotation>(),
            ComponentType.ReadWrite<PostRotation>(),
            ComponentType.ReadWrite<RotationPivot>(),
            ComponentType.ReadWrite<RotationPivotTranslation>(),
            ComponentType.ReadWrite<Translation>(),
            ComponentType.ReadWrite<Scale>(),
            ComponentType.ReadWrite<NonUniformScale>(),
            ComponentType.ReadWrite<CompositeScale>(),
            ComponentType.ReadWrite<ScalePivot>(),
            ComponentType.ReadWrite<ScalePivotTranslation>(),
            ComponentType.ReadWrite<ParentScaleInverse>(),
            ComponentType.ReadWrite<Parent>(),
            ComponentType.ReadWrite<PreviousParent>(),
            ComponentType.ReadWrite<Child>(),
            ComponentType.ReadWrite<PostRotationEulerXYZ>(),
            ComponentType.ReadWrite<PostRotationEulerXZY>(),
            ComponentType.ReadWrite<PostRotationEulerYXZ>(),
            ComponentType.ReadWrite<PostRotationEulerYZX>(),
            ComponentType.ReadWrite<PostRotationEulerZXY>(),
            ComponentType.ReadWrite<PostRotationEulerZYX>(),
            ComponentType.ReadWrite<RotationEulerXYZ>(),
            ComponentType.ReadWrite<RotationEulerXZY>(),
            ComponentType.ReadWrite<RotationEulerYXZ>(),
            ComponentType.ReadWrite<RotationEulerYZX>(),
            ComponentType.ReadWrite<RotationEulerZXY>(),
            ComponentType.ReadWrite<RotationEulerZYX>(),
        };

        protected override void OnUpdate()
        {
            Entities.ForEach((RigComponent rigComponent) =>
            {
                var rigEntity = TryGetPrimaryEntity(rigComponent);

                foreach (var bone in rigComponent.Bones)
                {
                    Component[] exposeTransformComponents = bone.GetComponents(typeof(IExposeTransform));

                    // The first bone is always exposed
                    bool isExposed = bone == rigComponent.Bones[0] || exposeTransformComponents?.Length > 0;

                    var entity = GetPrimaryEntity(bone);
                    if (!isExposed && CanDeleteEntity(bone.gameObject, entity))
                    {
                        DstEntityManager.DestroyEntity(entity);
                    }

                    if (DstEntityManager.Exists(entity) && entity != rigEntity && DstEntityManager.HasComponent<Parent>(entity))
                    {
                        var parent = DstEntityManager.GetComponentData<Parent>(entity);
                        if (parent.Value == Entity.Null)
                        {
                            DstEntityManager.SetComponentData(entity, new Parent { Value = rigEntity });
                        }
                    }
                }
            });
        }

        bool CanDeleteEntity(GameObject gameObject, Entity entity)
        {
            var components = gameObject.GetComponents(typeof(Component));
            if (components.Length > 1)
                return false;
            var canDelete = true;
            for (int i = 0; canDelete && i < components.Length; i++)
            {
                canDelete = components[i] is Transform;
            }
            if (canDelete)
            {
                var componentTypes = DstEntityManager.GetComponentTypes(entity);
                for (int i = 0; canDelete && i < componentTypes.Length; i++)
                {
                    canDelete = IsTransformComponentType(componentTypes[i]);
                }
            }
            return canDelete;
        }

        bool IsTransformComponentType(ComponentType componentType)
        {
            var ret = false;
            for (int i = 0; !ret && i < k_TransformComponentTypes.Length; i++)
            {
                ret = componentType == k_TransformComponentTypes[i];
            }

            return ret;
        }
    }
#endif
}
