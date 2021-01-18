using System.Diagnostics;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace Unity.Animation
{
    [BurstCompatible]
    public struct BoneRendererProperties
    {
        public float4 Color;
        public BoneRendererUtils.BoneShape BoneShape;
        public float Size;

        public static BoneRendererProperties Default { get; } = new BoneRendererProperties {
            Color = new float4(0f, 1f, 0f, 0.5f), BoneShape = BoneRendererUtils.BoneShape.Pyramid, Size = 1f
        };
    }

    public static class BoneRendererEntityBuilder
    {
        internal struct Bone
        {
            public int child;
            public int parent;
        }

        static readonly ComponentTypes s_BoneMatrixComponentTypes = new ComponentTypes(
            typeof(RigEntity),
            typeof(BoneRenderer.BoneSize),
            typeof(BoneRenderer.RigIndex),
            typeof(BoneRenderer.RigParentIndex),
            typeof(BoneRenderer.BoneWorldMatrix)
        );

        static readonly ComponentTypes s_BoneRenderingComponentTypes = new ComponentTypes(
            typeof(BoneRenderer.BoneShape),
            typeof(BoneRenderer.BoneColor),
            typeof(BoneRenderer.BoneRendererEntity)
        );

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateTransformIndices(NativeList<int> transformIndices, int max)
        {
            if (!transformIndices.IsCreated || transformIndices.Length == 0)
                throw new System.ArgumentException("transformIndices list is invalid", "transformIndices");

            for (int i = 0; i < transformIndices.Length; ++i)
            {
                if (transformIndices[i] < 0 || transformIndices[i] > max)
                {
                    throw new System.ArgumentOutOfRangeException(
                        $"transformIndices[{i}] has an invalid bone index '{transformIndices[i]}': valid values range [0,  {max}]."
                    );
                }
            }
        }

        static int FindIndex(ref BlobArray<StringHash> ids, StringHash id)
        {
            for (int i = 0; i < ids.Length; ++i)
            {
                if (ids[i] == id)
                    return i;
            }

            return -1;
        }

        static void ExtractValidBones(ref NativeList<Bone> bones, NativeList<int> transformIndices, BlobAssetReference<RigDefinition> rigDefinition)
        {
            ref var parentIndices = ref rigDefinition.Value.Skeleton.ParentIndexes;
            for (int i = 0; i < transformIndices.Length; ++i)
            {
                int parent = parentIndices[transformIndices[i]];
                if (parent == -1 || !transformIndices.Contains(parent))
                    continue;

                bones.Add(new Bone { child = transformIndices[i], parent = parent });
            }
        }

        public static void SetupBoneRendererEntities(
            Entity rigEntity,
            Entity boneDataEntity,
            Entity boneRenderEntity,
            EntityManager entityManager,
            BlobAssetReference<RigDefinition> rigDefinition,
            in BoneRendererProperties properties,
            NativeList<int> transformIndices
        )
        {
            Core.ValidateArgumentIsCreated(rigDefinition);
            ValidateTransformIndices(transformIndices, rigDefinition.Value.Skeleton.BoneCount);

            entityManager.AddComponents(boneDataEntity, s_BoneMatrixComponentTypes);
            entityManager.SetComponentData(boneDataEntity, new RigEntity { Value = rigEntity });
            entityManager.SetComponentData(boneDataEntity, new BoneRenderer.BoneSize { Value = properties.Size });

            var bones = new NativeList<Bone>(transformIndices.Length, Allocator.Temp);
            ExtractValidBones(ref bones, transformIndices, rigDefinition);

            var boneIndicesBuffer = entityManager.AddBuffer<BoneRenderer.RigIndex>(boneDataEntity);
            boneIndicesBuffer.ResizeUninitialized(bones.Length);

            var boneParentIndicesBuffer = entityManager.AddBuffer<BoneRenderer.RigParentIndex>(boneDataEntity);
            boneParentIndicesBuffer.ResizeUninitialized(bones.Length);

            var boneMatricesBuffer = entityManager.AddBuffer<BoneRenderer.BoneWorldMatrix>(boneDataEntity);
            boneMatricesBuffer.ResizeUninitialized(bones.Length);

            for (int i = 0; i < bones.Length; ++i)
            {
                boneMatricesBuffer[i] = new BoneRenderer.BoneWorldMatrix();
                boneIndicesBuffer[i] = new BoneRenderer.RigIndex { Value = bones[i].child };
                boneParentIndicesBuffer[i] = new BoneRenderer.RigParentIndex { Value = bones[i].parent };
            }

            entityManager.AddComponents(boneRenderEntity, s_BoneRenderingComponentTypes);
            entityManager.SetSharedComponentData(boneRenderEntity, new BoneRenderer.BoneShape { Value = properties.BoneShape });
            entityManager.SetComponentData(boneRenderEntity, new BoneRenderer.BoneColor { Value = properties.Color });
            entityManager.SetComponentData(boneRenderEntity, new BoneRenderer.BoneRendererEntity { Value = boneDataEntity });
        }

        public static void CreateBoneRendererEntities(
            Entity rigEntity,
            EntityManager entityManager,
            BlobAssetReference<RigDefinition> rigDefinition,
            in BoneRendererProperties properties,
            NativeList<int> transformIndices
        ) =>
            SetupBoneRendererEntities(rigEntity, entityManager.CreateEntity(), entityManager.CreateEntity(), entityManager, rigDefinition, properties, transformIndices);

        public static void CreateBoneRendererEntities(
            Entity rigEntity,
            EntityManager entityManager,
            BlobAssetReference<RigDefinition> rigDefinition,
            in BoneRendererProperties properties,
            NativeList<StringHash> transformIds = default
        )
        {
            Core.ValidateArgumentIsCreated(rigDefinition);

            NativeList<int> transformIndices;

            if (!transformIds.IsCreated)
            {
                ref var skeleton = ref rigDefinition.Value.Skeleton;
                transformIndices = new NativeList<int>(skeleton.BoneCount, Allocator.Temp);
                for (int i = 0; i < skeleton.BoneCount; ++i)
                    transformIndices.Add(i);
            }
            else
            {
                ref var skeletonIds = ref rigDefinition.Value.Skeleton.Ids;
                transformIndices = new NativeList<int>(skeletonIds.Length, Allocator.Temp);
                for (int i = 0; i < transformIds.Length; ++i)
                {
                    int idx = FindIndex(ref skeletonIds, transformIds[i]);
                    if (idx != -1)
                        transformIndices.Add(idx);
                }
            }

            CreateBoneRendererEntities(rigEntity, entityManager, rigDefinition, properties, transformIndices);
            transformIndices.Dispose();
        }
    }
}
