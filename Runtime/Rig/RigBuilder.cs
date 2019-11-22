using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Unity.Animation
{
    public static class RigBuilder
    {
        static void CheckHasOnlyOneRootAndValidParentIndices(SkeletonNode[] skeletonNodes)
        {
            Assert.IsTrue(skeletonNodes != null);

            var skeletonNodeCount = skeletonNodes.Length;
            if (skeletonNodeCount > 0 && skeletonNodes[0].ParentIndex != -1)
                throw new System.ArgumentException("First skeleton node is expected to be the root node.");

            for(int i = 1; i < skeletonNodeCount; i++)
            {
                if(skeletonNodes[i].ParentIndex == -1)
                {
                    throw new System.ArgumentException(
                        $"SkeletonNode[{i}] has an invalid ParentIndex: You can have only one root node in your skeleton."
                        );
                }

                if(skeletonNodes[i].ParentIndex == i)
                {
                    throw new System.ArgumentException(
                        $"SkeletonNode[{i}] has an invalid ParentIndex '{skeletonNodes[i].ParentIndex}': The SkeletonNode parent cannot be itself."
                        );
                }

                if (skeletonNodes[i].ParentIndex < 0 || skeletonNodes[i].ParentIndex >= skeletonNodeCount)
                {
                    throw new System.ArgumentOutOfRangeException(
                        $"SkeletonNode[{i}] has an invalid ParentIndex '{skeletonNodes[i].ParentIndex}': value must be between 0 and {skeletonNodeCount-1}."
                        );
                }
            }
        }

        static void CheckHasValidAxisIndices(SkeletonNode[] skeletonNodes, Axis[] axes)
        {
            Assert.IsTrue(skeletonNodes != null);

            for(int i = 0; i < skeletonNodes.Length; i++)
            {
                if (skeletonNodes[i].AxisIndex == -1)
                    continue;

                if(skeletonNodes[i].AxisIndex != -1 && axes == null)
                {
                    throw new ArgumentNullException("axis",
                        $"SkeletonNode[{i}] has an AxisIndex '{skeletonNodes[i].AxisIndex}' but axis is null."
                        );
                }

                if (skeletonNodes[i].AxisIndex < -1 || skeletonNodes[i].AxisIndex >= axes.Length)
                {
                    throw new System.ArgumentOutOfRangeException(
                        $"SkeletonNode[{i}] has an invalid AxisIndex '{skeletonNodes[i].AxisIndex}': value must be between -1 and {axes.Length-1}."
                        );
                }
            }
        }

        static void GetAnimationChannels(SkeletonNode[] skeletonNodes, List<LocalTranslationChannel> translationChannels, List<LocalRotationChannel> rotationChannels, List<LocalScaleChannel> scaleChannels)
        {
            Assert.IsTrue(skeletonNodes != null);

            for(int i = 0; i < skeletonNodes.Length; i++)
            {
                var localTranslationChannel = new LocalTranslationChannel { Id = skeletonNodes[i].Id, DefaultValue = skeletonNodes[i].LocalTranslationDefaultValue };
                if(!translationChannels.Contains(localTranslationChannel))
                    translationChannels.Add(localTranslationChannel);

                var localRotationChannel = new LocalRotationChannel { Id = skeletonNodes[i].Id, DefaultValue = skeletonNodes[i].LocalRotationDefaultValue };
                if(!rotationChannels.Contains(localRotationChannel))
                    rotationChannels.Add(localRotationChannel);

                var localScaleChannel = new LocalScaleChannel { Id = skeletonNodes[i].Id, DefaultValue = skeletonNodes[i].LocalScaleDefaultValue };
                if(!scaleChannels.Contains(localScaleChannel))
                    scaleChannels.Add(localScaleChannel);
            }
        }

        static void GetAnimationChannels<T>(IAnimationChannel[] animationChannels, List<T> filteredAnimationChannels) where T : struct, IAnimationChannel
        {
            Assert.IsTrue(animationChannels != null);

            for (var i = 0; i != animationChannels.Length; i++)
            {
                var channel = animationChannels[i];
                if (channel != null && channel.GetType() == typeof(T))
                {
                    if(!filteredAnimationChannels.Contains((T) channel))
                        filteredAnimationChannels.Add((T) channel);
                }
            }
        }

        static void InitializeSkeletonNodes(ref BlobBuilder blobBuilder, SkeletonNode[] skeletonNodes, ref BlobArray<int> parentIndices, ref BlobArray<StringHash> skeletonIds, ref BlobArray<int> axisIndices)
        {
            if (skeletonNodes == null || skeletonNodes.Length == 0)
                return;

            var parentIndicesBuilder = blobBuilder.Allocate(ref parentIndices, skeletonNodes.Length);
            var skeletonIdsBuilder = blobBuilder.Allocate(ref skeletonIds, skeletonNodes.Length);
            var skeletonAxisIndicesBuilder = blobBuilder.Allocate(ref axisIndices, skeletonNodes.Length);
            for(int i = 0; i < skeletonNodes.Length; i++)
            {
                parentIndicesBuilder[i] = skeletonNodes[i].ParentIndex;
                skeletonIdsBuilder[i] = skeletonNodes[i].Id;
                skeletonAxisIndicesBuilder[i] = skeletonNodes[i].AxisIndex;
            }
        }

        static void InitializeAxes(ref BlobBuilder blobBuilder, Axis[] axes, ref BlobArray<Axis> rigAxes)
        {
            if (axes == null || axes.Length == 0)
                return;

            var arrayBuilder = blobBuilder.Allocate(ref rigAxes, axes.Length);
            for (int i = 0; i < axes.Length; ++i)
            {
                rigAxes[i] = axes[i];
            }
        }

        static void InitializeBindings<T>(ref BlobBuilder blobBuilder, List<T> animationChannels, ref BlobArray<StringHash> bindings)
            where T : struct, IAnimationChannel
        {
            if (animationChannels.Count == 0)
                return;

            var arrayBuilder = blobBuilder.Allocate(ref bindings, animationChannels.Count);
            for(int i = 0; i < animationChannels.Count; i++)
            {
                arrayBuilder[i] = animationChannels[i].Id;
            }
        }

        static void InitializeDefaultValues<TChannel, TData>(ref BlobBuilder blobBuilder, List<TChannel> animationChannels, ref BlobArray<TData> defaultValues)
            where TChannel : struct, IAnimationChannel<TData>
            where TData : struct
        {
            if (animationChannels.Count == 0)
                return;

            var arrayBuilder = blobBuilder.Allocate(ref defaultValues, animationChannels.Count);
            for(int i = 0; i < animationChannels.Count; i++)
            {
                arrayBuilder[i] = animationChannels[i].DefaultValue;
            }
        }

        static unsafe uint ComputeHashCode<T>(ref BlobArray<T> array, uint seed = 0)
            where T : struct
        {
            return math.hash(array.GetUnsafePtr(), array.Length * UnsafeUtility.SizeOf<T>(), seed);
        }

        public static BlobAssetReference<RigDefinition> CreateRigDefinition(IAnimationChannel[] animationChannels)
        {
            return CreateRigDefinition(null, null, animationChannels);
        }

        public static BlobAssetReference<RigDefinition> CreateRigDefinition(SkeletonNode[] skeletonNodes, Axis[] axis = null)
        {
            return CreateRigDefinition(skeletonNodes, axis, null);
        }

        public static BlobAssetReference<RigDefinition> CreateRigDefinition(SkeletonNode[] skeletonNodes, Axis[] axes,
            IAnimationChannel[] animationChannels)
        {
            var translationChannels = new List<LocalTranslationChannel>();
            var rotationChannels = new List<LocalRotationChannel>();
            var scaleChannels = new List<LocalScaleChannel>();
            var floatChannels = new List<FloatChannel>();
            var intChannels = new List<IntChannel>();

            // First extract all animated channels from skeleton nodes, each skeleton nodes will create
            // a dedicated channel for localTranslation, localRotation and localScale
            if(skeletonNodes != null)
            {
                CheckHasOnlyOneRootAndValidParentIndices(skeletonNodes);
                CheckHasValidAxisIndices(skeletonNodes, axes);

                GetAnimationChannels(skeletonNodes, translationChannels, rotationChannels, scaleChannels);
            }

            if(animationChannels != null)
            {
                GetAnimationChannels(animationChannels, translationChannels);
                GetAnimationChannels(animationChannels, rotationChannels);
                GetAnimationChannels(animationChannels, scaleChannels);
                GetAnimationChannels(animationChannels, floatChannels);
                GetAnimationChannels(animationChannels, intChannels);
            }

            var blobBuilder = new BlobBuilder(Allocator.Temp);

            ref var rig = ref blobBuilder.ConstructRoot<RigDefinition>();

            InitializeSkeletonNodes(ref blobBuilder, skeletonNodes, ref rig.Skeleton.ParentIndexes, ref rig.Skeleton.Ids, ref rig.Skeleton.AxisIndexes);
            InitializeAxes(ref blobBuilder, axes, ref rig.Skeleton.Axis);

            InitializeBindings(ref blobBuilder, translationChannels, ref rig.Bindings.TranslationBindings);
            InitializeBindings(ref blobBuilder, rotationChannels, ref rig.Bindings.RotationBindings);
            InitializeBindings(ref blobBuilder, scaleChannels, ref rig.Bindings.ScaleBindings);
            InitializeBindings(ref blobBuilder, floatChannels, ref rig.Bindings.FloatBindings);
            InitializeBindings(ref blobBuilder, intChannels, ref rig.Bindings.IntBindings);

            InitializeDefaultValues(ref blobBuilder, translationChannels, ref rig.DefaultValues.LocalTranslations);
            InitializeDefaultValues(ref blobBuilder, rotationChannels, ref rig.DefaultValues.LocalRotations);
            InitializeDefaultValues(ref blobBuilder, scaleChannels, ref rig.DefaultValues.LocalScales);
            InitializeDefaultValues(ref blobBuilder, floatChannels, ref rig.DefaultValues.Floats);
            InitializeDefaultValues(ref blobBuilder, intChannels, ref rig.DefaultValues.Integers);

            var rigDefinitionAsset = blobBuilder.CreateBlobAssetReference<RigDefinition>(Allocator.Persistent);

            blobBuilder.Dispose();

            uint hashCode = ComputeHashCode(ref rigDefinitionAsset.Value.Skeleton.Ids);
            hashCode = ComputeHashCode(ref rigDefinitionAsset.Value.Skeleton.ParentIndexes, hashCode);
            hashCode = ComputeHashCode(ref rigDefinitionAsset.Value.Skeleton.AxisIndexes, hashCode);
            hashCode = ComputeHashCode(ref rigDefinitionAsset.Value.Skeleton.Axis, hashCode);

            hashCode = ComputeHashCode(ref rigDefinitionAsset.Value.Bindings.TranslationBindings, hashCode);
            hashCode = ComputeHashCode(ref rigDefinitionAsset.Value.Bindings.RotationBindings, hashCode);
            hashCode = ComputeHashCode(ref rigDefinitionAsset.Value.Bindings.ScaleBindings, hashCode);
            hashCode = ComputeHashCode(ref rigDefinitionAsset.Value.Bindings.FloatBindings, hashCode);
            hashCode = ComputeHashCode(ref rigDefinitionAsset.Value.Bindings.IntBindings, hashCode);

            hashCode = ComputeHashCode(ref rigDefinitionAsset.Value.DefaultValues.LocalTranslations, hashCode);
            hashCode = ComputeHashCode(ref rigDefinitionAsset.Value.DefaultValues.LocalRotations, hashCode);
            hashCode = ComputeHashCode(ref rigDefinitionAsset.Value.DefaultValues.LocalScales, hashCode);
            hashCode = ComputeHashCode(ref rigDefinitionAsset.Value.DefaultValues.Floats, hashCode);
            hashCode = ComputeHashCode(ref rigDefinitionAsset.Value.DefaultValues.Integers, hashCode);

            rigDefinitionAsset.Value.m_HashCode = (int)hashCode;

            return rigDefinitionAsset;
        }
    }
}
