using System;
using System.Diagnostics;

using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;


#if UNITY_ENABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    /// <summary>
    /// An intermediate data representing the rig.
    /// </summary>
    /// <remarks>
    /// If you don't use the RigComponent but your own custom rig representation,
    /// as long as you hav a method to convert this representation to a RigBuilderData,
    /// we can build a RigDefinition from it.
    /// </remarks>
    public struct RigBuilderData : IDisposable
    {
        public NativeList<SkeletonNode> SkeletonNodes;
        public NativeList<LocalTranslationChannel> TranslationChannels;
        public NativeList<LocalRotationChannel> RotationChannels;
        public NativeList<LocalScaleChannel> ScaleChannels;
        public NativeList<FloatChannel> FloatChannels;
        public NativeList<IntChannel> IntChannels;
        public NativeList<Axis> Axes;

        private bool m_IsDisposed;

        /// <summary>
        /// Constructs all the member lists using the specified type of memory allocation.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.
        /// </param>
        public RigBuilderData(Allocator allocator)
        {
            SkeletonNodes = new NativeList<SkeletonNode>(allocator);
            TranslationChannels = new NativeList<LocalTranslationChannel>(allocator);
            RotationChannels = new NativeList<LocalRotationChannel>(allocator);
            ScaleChannels = new NativeList<LocalScaleChannel>(allocator);
            FloatChannels = new NativeList<FloatChannel>(allocator);
            IntChannels = new NativeList<IntChannel>(allocator);
            Axes = new NativeList<Axis>(allocator);

            m_IsDisposed = false;
        }

        /// <summary>
        /// Reports whether memory for all the member lists is allocated.
        /// </summary>
        /// <value>True if this all the member lists have been allocated.</value>
        /// <remarks>Note that the lists are not allocated if you use the default constructor. You must specify
        /// at least an allocation type to construct a usable RigBuilderData.</remarks>
        public bool IsCreated
        {
            get
            {
                return SkeletonNodes.IsCreated
                    && TranslationChannels.IsCreated
                    && RotationChannels.IsCreated
                    && ScaleChannels.IsCreated
                    && FloatChannels.IsCreated
                    && IntChannels.IsCreated
                    && Axes.IsCreated;
            }
        }

        /// <summary>
        /// Disposes and deallocates all the member lists.
        /// </summary>
        public void Dispose()
        {
            if (m_IsDisposed) return;

            SkeletonNodes.Dispose();
            TranslationChannels.Dispose();
            RotationChannels.Dispose();
            ScaleChannels.Dispose();
            FloatChannels.Dispose();
            IntChannels.Dispose();
            Axes.Dispose();

            m_IsDisposed = true;
        }
    }

    /// <summary>
    /// This is a helper class that creates the RigDefinition used in the animation systems.
    /// </summary>
    public static class RigBuilder
    {
#if UNITY_ENABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_CreateRigDefMarker = new ProfilerMarker("RigBuilder.CreateRigDefinition");
#endif

        // TODO: this should be removed once the obsolete CreateRigDefinition is removed.
        static unsafe RigBuilderData BuildData(SkeletonNode[] skeletonNodes, Axis[] axes, IAnimationChannel[] animationChannels, Allocator allocator = Allocator.Temp)
        {
            var buildData = new RigBuilderData(allocator);

            if (skeletonNodes != null)
            {
                fixed(void* ptr = &skeletonNodes[0])
                {
                    buildData.SkeletonNodes.AddRange(ptr, skeletonNodes.Length);
                }
            }

            if (axes != null)
            {
                fixed(void* ptr = &axes[0])
                {
                    buildData.Axes.AddRange(ptr, axes.Length);
                }
            }

            if (animationChannels != null)
            {
                GetAnimationChannels(animationChannels, buildData.TranslationChannels);
                GetAnimationChannels(animationChannels, buildData.RotationChannels);
                GetAnimationChannels(animationChannels, buildData.ScaleChannels);
                GetAnimationChannels(animationChannels, buildData.FloatChannels);
                GetAnimationChannels(animationChannels, buildData.IntChannels);
            }

            return buildData;
        }

        [BurstCompile]
        internal struct CreateRigDefinitionJob : IJob
        {
            public BlobBuilder BlobBuilder;
            public RigBuilderData RigBuilderData;

            public void Execute()
            {
                CreateRigDefinition(RigBuilderData, BlobBuilder);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckHasOnlyOneRootAndValidParentIndices(NativeList<SkeletonNode> skeletonNodes)
        {
            if (!skeletonNodes.IsCreated)
                throw new System.ArgumentNullException("SkeletonNodes are not created.");

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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckHasValidAxisIndices(NativeList<SkeletonNode> skeletonNodes, NativeList<Axis> axes)
        {
            if (!skeletonNodes.IsCreated)
                throw new System.ArgumentNullException("SkeletonNodes NativeList is not created.");

            if (!axes.IsCreated)
                throw new System.ArgumentNullException("Axes NativeList is not created.");

            for (int i = 0; i < skeletonNodes.Length; i++)
            {
                if (skeletonNodes[i].AxisIndex == -1)
                    continue;

                if (skeletonNodes[i].AxisIndex != -1 && axes.Length == 0)
                {
                    throw new ArgumentNullException("axis",
                        $"SkeletonNode[{i}] has an AxisIndex '{skeletonNodes[i].AxisIndex}' but axis is empty."
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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckIsRigBuilderDataCreated(RigBuilderData data)
        {
            if (!data.IsCreated)
                throw new ArgumentNullException("RigBuilderData is not created.");
        }

        static void GetAnimationChannels(NativeList<SkeletonNode> skeletonNodes, NativeList<LocalTranslationChannel> translationChannels, NativeList<LocalRotationChannel> rotationChannels, NativeList<LocalScaleChannel> scaleChannels)
        {
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

        // TODO: this should be removed once the obsolete CreateRigDefinition is removed.
        static void GetAnimationChannels<T>(IAnimationChannel[] animationChannels, NativeList<T> filteredAnimationChannels) where T : struct, IEquatable<T>
        {
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

        static void GetAnimationChannels<T>(NativeList<T> animationChannels, NativeList<T> filteredAnimationChannels) where T : struct, IEquatable<T>
        {
            for (var i = 0; i != animationChannels.Length; i++)
            {
                var channel = animationChannels[i];
                if (!filteredAnimationChannels.Contains(channel))
                    filteredAnimationChannels.Add(channel);
            }
        }

        static void InitializeSkeletonNodes(ref BlobBuilder blobBuilder, NativeList<SkeletonNode> skeletonNodes, ref BlobArray<int> parentIndices, ref BlobArray<StringHash> skeletonIds, ref BlobArray<int> axisIndices)
        {
            if (!skeletonNodes.IsCreated || skeletonNodes.Length == 0)
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

        static void InitializeAxes(ref BlobBuilder blobBuilder, NativeList<Axis> axes, ref BlobArray<Axis> rigAxes)
        {
            if (!axes.IsCreated || axes.Length == 0)
                return;

            var arrayBuilder = blobBuilder.Allocate(ref rigAxes, axes.Length);
            for (int i = 0; i < axes.Length; ++i)
            {
                arrayBuilder[i] = axes[i];
            }
        }

        static void InitializeBindings<T>(ref BlobBuilder blobBuilder, NativeList<T> animationChannels, ref BlobArray<StringHash> bindings)
            where T : struct, IAnimationChannel
        {
            if (!animationChannels.IsCreated || animationChannels.Length == 0)
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
            if (!animationChannels.IsCreated || animationChannels.Length == 0)
                return;

            TData* data = (TData*)((float*)arrayBuilder.GetUnsafePtr() + index);
            for (int i = 0; i < animationChannels.Length; i++)
            {
                data[i] = animationChannels[i].DefaultValue;
            }
        }

        static unsafe void InitializeDefaultRotationValues(int index, ref BlobBuilderArray<float> arrayBuilder, NativeList<LocalRotationChannel> rotationChannels)
        {
            if (!rotationChannels.IsCreated || rotationChannels.Length == 0)
                return;

            // Fill as SOA 4-wide quaternions
            quaternion4* data = (quaternion4*)((float*)arrayBuilder.GetUnsafePtr() + index);
            int length = rotationChannels.Length >> 2;
            for (int i = 0; i < length; ++i)
            {
                int idx = i << 2;
                data[i] = mathex.quaternion4(
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
                ref quaternion4 q4 = ref UnsafeUtility.AsRef<quaternion4>(data + chunkIdx);

                q4.x[subIdx] = q.value.x;
                q4.y[subIdx] = q.value.y;
                q4.z[subIdx] = q.value.z;
                q4.w[subIdx] = q.value.w;
            }
        }

        // TODO: this should be deprecated to favour the method that takes a RigBuilderData as an argument.
        // But this implies to update almost all the tests...
        public static BlobAssetReference<RigDefinition> CreateRigDefinition(IAnimationChannel[] animationChannels)
        {
            return CreateRigDefinition(null, null, animationChannels);
        }

        // TODO: this should be deprecated to favour the method that takes a RigBuilderData as an argument.
        // But this implies to update almost all the tests...
        public static BlobAssetReference<RigDefinition> CreateRigDefinition(SkeletonNode[] skeletonNodes, Axis[] axis = null)
        {
            return CreateRigDefinition(skeletonNodes, axis, null);
        }

        // TODO: this should be deprecated to favour the method that takes a RigBuilderData as an argument.
        // But this implies to update almost all the tests...
        public static BlobAssetReference<RigDefinition> CreateRigDefinition(SkeletonNode[] skeletonNodes, Axis[] axes,
            IAnimationChannel[] animationChannels)
        {
            var rigBuilderData = BuildData(skeletonNodes, axes, animationChannels);
            return CreateRigDefinition(rigBuilderData);
        }

        /// <summary>
        /// Creates a BlobAssetReference of the RigDefinition from a RigBuilderData.
        /// </summary>
        /// <remarks>
        /// The <see cref="RigBuilderData"/> structure can be built from a RigComponent using ExtractRigBuilderData,
        /// or can be filled manually.
        /// While creating the rig definition, we ensure that there is no duplicates in the skeleton nodes and the custom
        /// animation channels.
        /// The TRS bindings in the RigDefinition BindingSet are ordered as: first the skeleton's, then the custom channels'.
        /// For example, the translations of the skeleton nodes are added to the translation bindings before the custom
        /// translation channels.
        /// </remarks>
        /// <param name="rigBuilderData">The RigBuilderData containing the skeleton, the axes and the custom animation channels.</param>
        /// <returns>The BlobAssetReference of the RigDefinition.</returns>
        public static BlobAssetReference<RigDefinition> CreateRigDefinition(RigBuilderData rigBuilderData)
        {
#if UNITY_ENABLE_ANIMATION_PROFILING
            k_CreateRigDefMarker.Begin();
#endif
            BlobAssetReference<RigDefinition> rigDefinitionAsset;
            using (var blobBuilder = new BlobBuilder(Allocator.Temp))
            {
                CreateRigDefinition(rigBuilderData, blobBuilder);

                rigDefinitionAsset = blobBuilder.CreateBlobAssetReference<RigDefinition>(Allocator.Persistent);
                rigDefinitionAsset.Value.m_HashCode = (int)HashUtils.ComputeHash(ref rigDefinitionAsset);
            }

#if UNITY_ENABLE_ANIMATION_PROFILING
            k_CreateRigDefMarker.End();
#endif

            return rigDefinitionAsset;
        }

        /// <summary>
        /// Creates a BlobAssetReference of the RigDefinition from a RigBuilderData.
        /// This function creates runs a job internally and can't be called from another job.
        /// </summary>
        /// <param name="rigBuilderData">The RigBuilderData containing the skeleton, the axes and the custom animation channels.</param>
        /// <returns>The BlobAssetReference of the RigDefinition.</returns>
        [BurstCompile]
        public static BlobAssetReference<RigDefinition> RunCreateRigDefinitionJob(RigBuilderData rigBuilderData)
        {
#if UNITY_ENABLE_ANIMATION_PROFILING
            k_CreateRigDefMarker.Begin();
#endif

            BlobAssetReference<RigDefinition> rigDefinitionAsset;
            using (var blobBuilder = new BlobBuilder(Allocator.TempJob))
            {
                var job = new CreateRigDefinitionJob
                {
                    BlobBuilder = blobBuilder,
                    RigBuilderData = rigBuilderData
                };
                job.Run();

                rigDefinitionAsset = blobBuilder.CreateBlobAssetReference<RigDefinition>(Allocator.Persistent);
                rigDefinitionAsset.Value.m_HashCode = (int)HashUtils.ComputeHash(ref rigDefinitionAsset);
            }

#if UNITY_ENABLE_ANIMATION_PROFILING
            k_CreateRigDefMarker.End();
#endif

            return rigDefinitionAsset;
        }

        static void CreateRigDefinition(RigBuilderData rigBuilderData, BlobBuilder blobBuilder)
        {
            CheckIsRigBuilderDataCreated(rigBuilderData);

            var translations = new NativeList<LocalTranslationChannel>(Allocator.Temp);
            var rotations = new NativeList<LocalRotationChannel>(Allocator.Temp);
            var scales = new NativeList<LocalScaleChannel>(Allocator.Temp);

            CheckHasOnlyOneRootAndValidParentIndices(rigBuilderData.SkeletonNodes);
            CheckHasValidAxisIndices(rigBuilderData.SkeletonNodes, rigBuilderData.Axes);

            GetAnimationChannels(rigBuilderData.SkeletonNodes, translations, rotations, scales);
            GetAnimationChannels(rigBuilderData.TranslationChannels, translations);
            GetAnimationChannels(rigBuilderData.RotationChannels, rotations);
            GetAnimationChannels(rigBuilderData.ScaleChannels, scales);

            ref var rig = ref blobBuilder.ConstructRoot<RigDefinition>();

            InitializeSkeletonNodes(ref blobBuilder, rigBuilderData.SkeletonNodes, ref rig.Skeleton.ParentIndexes, ref rig.Skeleton.Ids, ref rig.Skeleton.AxisIndexes);
            InitializeAxes(ref blobBuilder, rigBuilderData.Axes, ref rig.Skeleton.Axis);

            InitializeBindings(ref blobBuilder, translations, ref rig.Bindings.TranslationBindings);
            InitializeBindings(ref blobBuilder, scales, ref rig.Bindings.ScaleBindings);
            InitializeBindings(ref blobBuilder, rigBuilderData.FloatChannels, ref rig.Bindings.FloatBindings);
            InitializeBindings(ref blobBuilder, rigBuilderData.IntChannels, ref rig.Bindings.IntBindings);
            InitializeBindings(ref blobBuilder, rotations, ref rig.Bindings.RotationBindings);

            rig.Bindings = rig.CreateBindingSet(
                translations.Length,
                rotations.Length,
                scales.Length,
                rigBuilderData.FloatChannels.Length,
                rigBuilderData.IntChannels.Length);

            var arrayBuilder = blobBuilder.Allocate(ref rig.DefaultValues, rig.Bindings.StreamSize);

            InitializeDefaultValues<LocalTranslationChannel, float3>(rig.Bindings.TranslationSamplesOffset, ref arrayBuilder, translations);
            InitializeDefaultValues<LocalScaleChannel, float3>(rig.Bindings.ScaleSamplesOffset, ref arrayBuilder, scales);
            InitializeDefaultValues<FloatChannel, float>(rig.Bindings.FloatSamplesOffset, ref arrayBuilder, rigBuilderData.FloatChannels);
            InitializeDefaultValues<IntChannel, int>(rig.Bindings.IntSamplesOffset, ref arrayBuilder, rigBuilderData.IntChannels);
            InitializeDefaultRotationValues(rig.Bindings.RotationSamplesOffset, ref arrayBuilder, rotations);
        }
    }
}
