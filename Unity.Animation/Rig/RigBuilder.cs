using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using Unity.Animation;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    public static class RigBuilder
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_CreateRigDefMarker = new ProfilerMarker("RigBuilder.CreateRigDefinition");
#endif

        static void CheckHasOnlyOneRootAndValidParentIndices(SkeletonNode[] skeletonNodes)
        {
            Assert.IsTrue(skeletonNodes != null);

            var skeletonNodeCount = skeletonNodes.Length;
            if (skeletonNodeCount > 0 && skeletonNodes[0].ParentIndex != -1)
                throw new System.ArgumentException("First skeleton node is expected to be the root node.");

            for (int i = 1; i < skeletonNodeCount; i++)
            {
                if (skeletonNodes[i].ParentIndex == -1)
                {
                    throw new System.ArgumentException(
                        $"SkeletonNode[{i}] has an invalid ParentIndex: You can have only one root node in your skeleton."
                    );
                }

                if (skeletonNodes[i].ParentIndex == i)
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

            for (int i = 0; i < skeletonNodes.Length; i++)
            {
                if (skeletonNodes[i].AxisIndex == -1)
                    continue;

                if (skeletonNodes[i].AxisIndex != -1 && axes == null)
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

        static void GetAnimationChannels(SkeletonNode[] skeletonNodes, NativeList<LocalTranslationChannel> translationChannels, NativeList<LocalRotationChannel> rotationChannels, NativeList<LocalScaleChannel> scaleChannels)
        {
            Assert.IsTrue(skeletonNodes != null);

            for (int i = 0; i < skeletonNodes.Length; i++)
            {
                var localTranslationChannel = new LocalTranslationChannel { Id = skeletonNodes[i].Id, DefaultValue = skeletonNodes[i].LocalTranslationDefaultValue };
                if (!translationChannels.Contains(localTranslationChannel))
                    translationChannels.Add(localTranslationChannel);

                var localRotationChannel = new LocalRotationChannel { Id = skeletonNodes[i].Id, DefaultValue = skeletonNodes[i].LocalRotationDefaultValue };
                if (!rotationChannels.Contains(localRotationChannel))
                    rotationChannels.Add(localRotationChannel);

                var localScaleChannel = new LocalScaleChannel { Id = skeletonNodes[i].Id, DefaultValue = skeletonNodes[i].LocalScaleDefaultValue };
                if (!scaleChannels.Contains(localScaleChannel))
                    scaleChannels.Add(localScaleChannel);
            }
        }

        static void GetAnimationChannels<T>(IAnimationChannel[] animationChannels, NativeList<T> filteredAnimationChannels) where T : struct, IEquatable<T>
        {
            Assert.IsTrue(animationChannels != null);

            for (var i = 0; i != animationChannels.Length; i++)
            {
                var channel = animationChannels[i];
                if (channel != null && channel.GetType() == typeof(T))
                {
                    if (!filteredAnimationChannels.Contains((T)channel))
                        filteredAnimationChannels.Add((T)channel);
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
            for (int i = 0; i < skeletonNodes.Length; i++)
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

        static void InitializeBindings<T>(ref BlobBuilder blobBuilder, NativeList<T> animationChannels, ref BlobArray<StringHash> bindings)
            where T : struct, IAnimationChannel
        {
            if (animationChannels.Length == 0)
                return;

            var arrayBuilder = blobBuilder.Allocate(ref bindings, animationChannels.Length);
            for (int i = 0; i < animationChannels.Length; i++)
            {
                arrayBuilder[i] = animationChannels[i].Id;
            }
        }

        static unsafe void InitializeDefaultValues<TChannel, TData>(int index, ref BlobBuilderArray<float> arrayBuilder, NativeList<TChannel> animationChannels)
            where TChannel : struct, IAnimationChannel<TData>
            where TData : unmanaged
        {
            if (animationChannels.Length == 0)
                return;

            TData* dataPtr = (TData*)((float*)arrayBuilder.GetUnsafePtr() + index);
            for (int i = 0; i < animationChannels.Length; i++)
            {
                *(dataPtr + i) = animationChannels[i].DefaultValue;
            }

            return;
        }

        static unsafe void InitializeDefaultRotationValues(int index, ref BlobBuilderArray<float> arrayBuilder, NativeList<LocalRotationChannel> rotationChannels)
        {
            if (rotationChannels.Length == 0)
                return;

            // Fill as SOA 4-wide quaternions
            quaternion4* dataPtr = (quaternion4*)((float*)arrayBuilder.GetUnsafePtr() + index);
            int length = rotationChannels.Length >> 2;
            for (int i = 0; i < length; ++i)
            {
                int idx = i << 2;
                *(dataPtr + i) = mathex.quaternion4(
                    rotationChannels[idx + 0].DefaultValue,
                    rotationChannels[idx + 1].DefaultValue,
                    rotationChannels[idx + 2].DefaultValue,
                    rotationChannels[idx + 3].DefaultValue
                );
            }

            // Fill remaining rotations
            for (int i = length << 2; i < rotationChannels.Length; ++i)
            {
                int chunkIdx = i >> 2;
                int subIdx = i & 0x3; // equivalent to % 4;

                quaternion q = rotationChannels[i].DefaultValue;
                ref quaternion4 q4 = ref UnsafeUtilityEx.AsRef<quaternion4>(dataPtr + chunkIdx);

                q4.x[subIdx] = q.value.x;
                q4.y[subIdx] = q.value.y;
                q4.z[subIdx] = q.value.z;
                q4.w[subIdx] = q.value.w;
            }

            return;
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
#if !UNITY_DISABLE_ANIMATION_PROFILING
            k_CreateRigDefMarker.Begin();
#endif

            var translationChannels = new NativeList<LocalTranslationChannel>(Allocator.Temp);
            var rotationChannels = new NativeList<LocalRotationChannel>(Allocator.Temp);
            var scaleChannels = new NativeList<LocalScaleChannel>(Allocator.Temp);
            var floatChannels = new NativeList<FloatChannel>(Allocator.Temp);
            var intChannels = new NativeList<IntChannel>(Allocator.Temp);

            // First extract all animated channels from skeleton nodes, each skeleton nodes will create
            // a dedicated channel for localTranslation, localRotation and localScale
            if (skeletonNodes != null)
            {
                CheckHasOnlyOneRootAndValidParentIndices(skeletonNodes);
                CheckHasValidAxisIndices(skeletonNodes, axes);

                GetAnimationChannels(skeletonNodes, translationChannels, rotationChannels, scaleChannels);
            }

            if (animationChannels != null)
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
            InitializeBindings(ref blobBuilder, scaleChannels, ref rig.Bindings.ScaleBindings);
            InitializeBindings(ref blobBuilder, floatChannels, ref rig.Bindings.FloatBindings);
            InitializeBindings(ref blobBuilder, intChannels, ref rig.Bindings.IntBindings);
            InitializeBindings(ref blobBuilder, rotationChannels, ref rig.Bindings.RotationBindings);

            rig.Bindings = rig.CreateBindingSet(translationChannels.Length, rotationChannels.Length, scaleChannels.Length, floatChannels.Length, intChannels.Length);

            var arrayBuilder = blobBuilder.Allocate(ref rig.DefaultValues, rig.Bindings.StreamSize);

            InitializeDefaultValues<LocalTranslationChannel, float3>(rig.Bindings.TranslationSamplesOffset, ref arrayBuilder, translationChannels);
            InitializeDefaultValues<LocalScaleChannel, float3>(rig.Bindings.ScaleSamplesOffset, ref arrayBuilder, scaleChannels);
            InitializeDefaultValues<FloatChannel, float>(rig.Bindings.FloatSamplesOffset, ref arrayBuilder, floatChannels);
            InitializeDefaultValues<IntChannel, int>(rig.Bindings.IntSamplesOffset, ref arrayBuilder, intChannels);
            InitializeDefaultRotationValues(rig.Bindings.RotationSamplesOffset, ref arrayBuilder, rotationChannels);

            var rigDefinitionAsset = blobBuilder.CreateBlobAssetReference<RigDefinition>(Allocator.Persistent);

            blobBuilder.Dispose();

            rigDefinitionAsset.Value.m_HashCode = (int)HashUtils.ComputeHash(ref rigDefinitionAsset);

#if !UNITY_DISABLE_ANIMATION_PROFILING
            k_CreateRigDefMarker.End();
#endif

            return rigDefinitionAsset;
        }
    }
}
