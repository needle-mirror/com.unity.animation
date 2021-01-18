using System.Collections.Generic;
using Unity.Animation.Authoring;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Animation.Hybrid
{
    public static partial class RigRemapUtils
    {
        internal enum RigElementType : byte
        {
            Bone,
            Translation,
            Rotation,
            Scale,
            Float,
            Int
        }

        internal struct RigElementValue
        {
            public int Index;
            public RigElementType Type;
        }

        /// <summary>
        /// Given source and destination RigComponents this function creates a remap table based on matching ids.
        /// </summary>
        /// <param name="srcRig">The source RigComponent to remap from.</param>
        /// <param name="dstRig">The destination RigComponent to remap to.</param>
        /// <param name="filter">Optional parameter to filter matches based on channel type. By default, all types of channels are used for matching.</param>
        /// <param name="offsetOverrides">Optional parameter to specify which translation or rotation channels should be matched with offsets in a given space (LocalToParent vs. LocalToRoot). By default, LocalToParent mapping is performed.</param>
        /// <param name="hasher">Optional parameter to specify a BindingHashGenerator in order to match bindings using a custom strategy. If not specified the system wide BindingHashGlobals.DefaultHashGenerator is used.</param>
        /// <returns>Returns the BlobAssetReference of the RigRemapTable.</returns>
        /// <exception cref="ArgumentNullException">srcRig and dstRig must be not null.</exception>
        public static BlobAssetReference<RigRemapTable> CreateRemapTable(
            RigComponent srcRig,
            RigComponent dstRig,
            Animation.RigRemapUtils.ChannelFilter filter = Animation.RigRemapUtils.ChannelFilter.All,
            Animation.RigRemapUtils.OffsetOverrides offsetOverrides = default,
            BindingHashGenerator hasher = default
        )
        {
            if (srcRig == null)
                throw new System.ArgumentNullException(nameof(srcRig));
            if (dstRig == null)
                throw new System.ArgumentNullException(nameof(dstRig));

            if (!hasher.IsValid)
                hasher = BindingHashGlobals.DefaultHashGenerator;

            var srcRigHashMap = RigComponentHashMap(srcRig, hasher);
            FindMatches(
                srcRigHashMap,
                dstRig,
                out NativeList<RigRemapEntry> translationMatches,
                out NativeList<RigRemapEntry> rotationMatches,
                out NativeList<RigRemapEntry> scaleMatches,
                out NativeList<RigRemapEntry> floatMatches,
                out NativeList<RigRemapEntry> intMatches,
                filter,
                hasher
            );

            return CreateRemapTable(
                srcRigHashMap,
                translationMatches,
                rotationMatches,
                scaleMatches,
                floatMatches,
                intMatches,
                filter,
                offsetOverrides
            );
        }

        /// <summary>
        /// Given source and destination Skeletons this function creates a remap table based on matching ids.
        /// </summary>
        /// <param name="srcSkeleton">The source RigComponent to remap from.</param>
        /// <param name="dstSkeleton">The destination RigComponent to remap to.</param>
        /// <param name="filter">Optional parameter to filter matches based on channel type. By default, all types of channels are used for matching.</param>
        /// <param name="offsetOverrides">Optional parameter to specify which translation or rotation channels should be matched with offsets in a given space (LocalToParent vs. LocalToRoot). By default, LocalToParent mapping is performed.</param>
        /// <param name="srcHasher">Optional parameter to specify a BindingHashGenerator in order to match srcSkeleton bindings using a custom strategy. If not specified the system wide BindingHashGlobals.DefaultHashGenerator is used.</param>
        /// <param name="dstHasher">Optional parameter to specify a BindingHashGenerator in order to match dstSkeleton bindings using a custom strategy. If not specified srcHasher is used instead.</param>
        /// <returns>Returns the BlobAssetReference of the RigRemapTable.</returns>
        /// <exception cref="ArgumentNullException">srcSkeleton and dstSkeleton must be not null.</exception>
        public static BlobAssetReference<RigRemapTable> CreateRemapTable(
            Authoring.Skeleton srcSkeleton,
            Authoring.Skeleton dstSkeleton,
            Animation.RigRemapUtils.ChannelFilter filter = Animation.RigRemapUtils.ChannelFilter.All,
            Animation.RigRemapUtils.OffsetOverrides offsetOverrides = default,
            BindingHashGenerator srcHasher = default,
            BindingHashGenerator dstHasher = default
        )
        {
            if (srcSkeleton == null)
                throw new System.ArgumentNullException(nameof(srcSkeleton));
            if (dstSkeleton == null)
                throw new System.ArgumentNullException(nameof(dstSkeleton));

            if (!srcHasher.IsValid)
                srcHasher = BindingHashGlobals.DefaultHashGenerator;
            if (!dstHasher.IsValid)
                dstHasher = srcHasher;

            var srcRigHashMap = RigComponentHashMap(srcSkeleton, srcHasher);
            FindMatches(
                srcRigHashMap,
                dstSkeleton,
                out NativeList<RigRemapEntry> translationMatches,
                out NativeList<RigRemapEntry> rotationMatches,
                out NativeList<RigRemapEntry> scaleMatches,
                out NativeList<RigRemapEntry> floatMatches,
                out NativeList<RigRemapEntry> intMatches,
                filter,
                dstHasher
            );

            return CreateRemapTable(
                srcRigHashMap,
                translationMatches,
                rotationMatches,
                scaleMatches,
                floatMatches,
                intMatches,
                filter,
                offsetOverrides
            );
        }

        private static BlobAssetReference<RigRemapTable> CreateRemapTable(
            NativeHashMap<StringHash, RigElementValue> srcRigHashMap,
            NativeList<RigRemapEntry> translationMatches,
            NativeList<RigRemapEntry> rotationMatches,
            NativeList<RigRemapEntry> scaleMatches,
            NativeList<RigRemapEntry> floatMatches,
            NativeList<RigRemapEntry> intMatches,
            Animation.RigRemapUtils.ChannelFilter filter = Animation.RigRemapUtils.ChannelFilter.All,
            Animation.RigRemapUtils.OffsetOverrides offsetOverrides = default
        )
        {
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var rigRemapTable = ref blobBuilder.ConstructRoot<RigRemapTable>();

            int localToRootTranslationCount = 0;
            int localToRootRotationCount = 0;
            if (offsetOverrides.IsCreated)
            {
                bool hasTranslationChannels = (filter & Animation.RigRemapUtils.ChannelFilter.Translation) != Animation.RigRemapUtils.ChannelFilter.None;
                bool hasRotationChannels = (filter & Animation.RigRemapUtils.ChannelFilter.Rotation) != Animation.RigRemapUtils.ChannelFilter.None;

                if (hasTranslationChannels && offsetOverrides.HasTranslationOffsetOverrides)
                {
                    OverrideBindingOffset(srcRigHashMap, offsetOverrides.m_TranslationIds, translationMatches);
                    localToRootTranslationCount = offsetOverrides.LocalToRootTranslationCount;
                }
                else
                    localToRootTranslationCount = 0;

                if (hasRotationChannels && offsetOverrides.HasRotationOffsetOverrides)
                {
                    OverrideBindingOffset(srcRigHashMap, offsetOverrides.m_RotationIds, rotationMatches);
                    localToRootRotationCount = offsetOverrides.LocalToRootRotationCount;
                }
                else
                    localToRootRotationCount = 0;

                var sortedLocalToRootTREntries = new NativeList<int2>(Allocator.Temp);
                Animation.RigRemapUtils.ComputeSortedLocalToRootTREntries(
                    sortedLocalToRootTREntries,
                    translationMatches,
                    rotationMatches,
                    offsetOverrides.m_TranslationOffsets,
                    offsetOverrides.m_RotationOffsets,
                    localToRootTranslationCount,
                    localToRootRotationCount
                );

                if (hasTranslationChannels)
                    Animation.RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, offsetOverrides.m_TranslationOffsets, ref rigRemapTable.TranslationOffsets);
                if (hasRotationChannels)
                    Animation.RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, offsetOverrides.m_RotationOffsets, ref rigRemapTable.RotationOffsets);

                Animation.RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, sortedLocalToRootTREntries, ref rigRemapTable.SortedLocalToRootTREntries);
                sortedLocalToRootTREntries.Dispose();
            }

            Animation.RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, translationMatches, ref rigRemapTable.TranslationMappings);
            Animation.RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, rotationMatches, ref rigRemapTable.RotationMappings);
            Animation.RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, scaleMatches, ref rigRemapTable.ScaleMappings);
            Animation.RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, floatMatches, ref rigRemapTable.FloatMappings);
            Animation.RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, intMatches, ref rigRemapTable.IntMappings);
            rigRemapTable.LocalToParentTRCount = math.int2(
                translationMatches.Length - localToRootTranslationCount,
                rotationMatches.Length - localToRootRotationCount
            );

            var rigRemapTableAsset = blobBuilder.CreateBlobAssetReference<RigRemapTable>(Allocator.Persistent);

            blobBuilder.Dispose();

            translationMatches.Dispose();
            rotationMatches.Dispose();
            scaleMatches.Dispose();
            floatMatches.Dispose();
            intMatches.Dispose();
            srcRigHashMap.Dispose();

            return rigRemapTableAsset;
        }

        static NativeHashMap<StringHash, RigElementValue> RigComponentHashMap(RigComponent rig, BindingHashGenerator hasher)
        {
            int boneCount = rig.Bones?.Length ?? 0;
            int translationCount = rig.TranslationChannels?.Length ?? 0;
            int rotationCount = rig.RotationChannels?.Length ?? 0;
            int scaleCount = rig.ScaleChannels?.Length ?? 0;
            int floatCount = rig.FloatChannels?.Length ?? 0;
            int intCount = rig.IntChannels?.Length ?? 0;

            var map = new NativeHashMap<StringHash, RigElementValue>(
                boneCount + translationCount + rotationCount + scaleCount + floatCount + intCount,
                Allocator.Persistent
            );

            for (int i = 0; i < boneCount; ++i)
                map.TryAdd(hasher.ToHash(RigGenerator.ToTransformBindingID(rig.Bones[i], rig.transform)), new RigElementValue { Index = i, Type = RigElementType.Bone });

            for (int i = 0; i < translationCount; ++i)
                map.TryAdd(hasher.ToHash(RigGenerator.ToGenericBindingID(rig.TranslationChannels[i].Id)), new RigElementValue { Index = boneCount + i, Type = RigElementType.Translation });

            for (int i = 0; i < rotationCount; ++i)
                map.TryAdd(hasher.ToHash(RigGenerator.ToGenericBindingID(rig.RotationChannels[i].Id)), new RigElementValue { Index = boneCount + i, Type = RigElementType.Rotation });

            for (int i = 0; i < scaleCount; ++i)
                map.TryAdd(hasher.ToHash(RigGenerator.ToGenericBindingID(rig.ScaleChannels[i].Id)), new RigElementValue { Index = boneCount + i, Type = RigElementType.Scale });

            for (int i = 0; i < floatCount; ++i)
                map.TryAdd(hasher.ToHash(RigGenerator.ToGenericBindingID(rig.FloatChannels[i].Id)), new RigElementValue { Index = i, Type = RigElementType.Float });

            for (int i = 0; i < intCount; ++i)
                map.TryAdd(hasher.ToHash(RigGenerator.ToGenericBindingID(rig.IntChannels[i].Id)), new RigElementValue { Index = i, Type = RigElementType.Int });

            return map;
        }

        static NativeHashMap<StringHash, RigElementValue> RigComponentHashMap(Authoring.Skeleton skeleton, BindingHashGenerator hasher)
        {
            var transformChannels = new List<TransformChannel>();
            skeleton.GetAllTransforms(transformChannels);

            var quaternionChannels = skeleton.QuaternionChannels;
            var floatChannels = skeleton.FloatChannels;
            var intChannels = skeleton.IntChannels;

            int boneCount = transformChannels.Count;
            int quaternionCount = quaternionChannels.Count;
            int floatCount = floatChannels.Count;
            int intCount = intChannels.Count;

            var map = new NativeHashMap<StringHash, RigElementValue>(
                boneCount + quaternionCount + floatCount + intCount,
                Allocator.Persistent
            );

            for (int i = 0; i < boneCount; ++i)
                map.TryAdd(hasher.ToHash(transformChannels[i].ID), new RigElementValue { Index = i, Type = RigElementType.Bone });

            for (int i = 0; i < quaternionCount; ++i)
                map.TryAdd(hasher.ToHash(quaternionChannels[i].ID), new RigElementValue { Index = boneCount + i, Type = RigElementType.Rotation });

            for (int i = 0; i < floatCount; ++i)
                map.TryAdd(hasher.ToHash(floatChannels[i].ID), new RigElementValue { Index = i, Type = RigElementType.Float });

            for (int i = 0; i < intCount; ++i)
                map.TryAdd(hasher.ToHash(intChannels[i].ID), new RigElementValue { Index = i, Type = RigElementType.Int });

            return map;
        }

        static void FindMatches(
            NativeHashMap<StringHash, RigElementValue> srcRigHashMap,
            RigComponent dstRig,
            out NativeList<RigRemapEntry> translations,
            out NativeList<RigRemapEntry> rotations,
            out NativeList<RigRemapEntry> scales,
            out NativeList<RigRemapEntry> floats,
            out NativeList<RigRemapEntry> ints,
            Animation.RigRemapUtils.ChannelFilter filter,
            BindingHashGenerator hasher
        )
        {
            int boneCount = dstRig.Bones?.Length ?? 0;
            int translationCount = dstRig.TranslationChannels?.Length ?? 0;
            int rotationCount = dstRig.RotationChannels?.Length ?? 0;
            int scaleCount = dstRig.ScaleChannels?.Length ?? 0;
            int floatCount = dstRig.FloatChannels?.Length ?? 0;
            int intCount = dstRig.IntChannels?.Length ?? 0;

            translations = new NativeList<RigRemapEntry>(boneCount + translationCount, Allocator.Persistent);
            rotations = new NativeList<RigRemapEntry>(boneCount + rotationCount, Allocator.Persistent);
            scales = new NativeList<RigRemapEntry>(boneCount + scaleCount, Allocator.Persistent);
            floats = new NativeList<RigRemapEntry>(floatCount, Allocator.Persistent);
            ints = new NativeList<RigRemapEntry>(intCount, Allocator.Persistent);

            bool hasTranslation = (filter & Animation.RigRemapUtils.ChannelFilter.Translation) != Animation.RigRemapUtils.ChannelFilter.None;
            bool hasRotation = (filter & Animation.RigRemapUtils.ChannelFilter.Rotation) != Animation.RigRemapUtils.ChannelFilter.None;
            bool hasScale = (filter & Animation.RigRemapUtils.ChannelFilter.Scale) != Animation.RigRemapUtils.ChannelFilter.None;

            for (int i = 0; i < boneCount; ++i)
            {
                if (srcRigHashMap.TryGetValue(hasher.ToHash(RigGenerator.ToTransformBindingID(dstRig.Bones[i], dstRig.transform)), out RigElementValue value))
                {
                    if (hasTranslation)
                        translations.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
                    if (hasRotation)
                        rotations.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
                    if (hasScale)
                        scales.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
                }
            }

            if (hasTranslation)
            {
                for (int i = 0; i < translationCount; ++i)
                {
                    if (srcRigHashMap.TryGetValue(hasher.ToHash(RigGenerator.ToGenericBindingID(dstRig.TranslationChannels[i].Id)), out RigElementValue value))
                        translations.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
                }
            }

            if (hasRotation)
            {
                for (int i = 0; i < rotationCount; ++i)
                {
                    if (srcRigHashMap.TryGetValue(hasher.ToHash(RigGenerator.ToGenericBindingID(dstRig.RotationChannels[i].Id)), out RigElementValue value))
                        rotations.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
                }
            }

            if (hasScale)
            {
                for (int i = 0; i < scaleCount; ++i)
                {
                    if (srcRigHashMap.TryGetValue(hasher.ToHash(RigGenerator.ToGenericBindingID(dstRig.ScaleChannels[i].Id)), out RigElementValue value))
                        scales.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
                }
            }

            if ((filter & Animation.RigRemapUtils.ChannelFilter.Float) != Animation.RigRemapUtils.ChannelFilter.None)
            {
                for (int i = 0; i < floatCount; ++i)
                {
                    if (srcRigHashMap.TryGetValue(hasher.ToHash(RigGenerator.ToGenericBindingID(dstRig.FloatChannels[i].Id)), out RigElementValue value))
                        floats.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
                }
            }

            if ((filter & Animation.RigRemapUtils.ChannelFilter.Int) != Animation.RigRemapUtils.ChannelFilter.None)
            {
                for (int i = 0; i < intCount; ++i)
                {
                    if (srcRigHashMap.TryGetValue(hasher.ToHash(RigGenerator.ToGenericBindingID(dstRig.IntChannels[i].Id)), out RigElementValue value))
                        ints.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
                }
            }
        }

        static void FindMatches(
            NativeHashMap<StringHash, RigElementValue> srcRigHashMap,
            Authoring.Skeleton dstSkeleton,
            out NativeList<RigRemapEntry> translations,
            out NativeList<RigRemapEntry> rotations,
            out NativeList<RigRemapEntry> scales,
            out NativeList<RigRemapEntry> floats,
            out NativeList<RigRemapEntry> ints,
            Animation.RigRemapUtils.ChannelFilter filter,
            BindingHashGenerator hasher
        )
        {
            var transformChannels = new List<TransformChannel>();
            dstSkeleton.GetAllTransforms(transformChannels);

            var quaternionChannels = dstSkeleton.QuaternionChannels;
            var floatChannels = dstSkeleton.FloatChannels;
            var intChannels = dstSkeleton.IntChannels;

            int boneCount = transformChannels.Count;
            int quaternionCount = quaternionChannels.Count;
            int floatCount = floatChannels.Count;
            int intCount = intChannels.Count;

            translations = new NativeList<RigRemapEntry>(boneCount, Allocator.Persistent);
            rotations = new NativeList<RigRemapEntry>(boneCount + quaternionCount, Allocator.Persistent);
            scales = new NativeList<RigRemapEntry>(boneCount, Allocator.Persistent);
            floats = new NativeList<RigRemapEntry>(floatCount, Allocator.Persistent);
            ints = new NativeList<RigRemapEntry>(intCount, Allocator.Persistent);

            bool hasTranslation = (filter & Animation.RigRemapUtils.ChannelFilter.Translation) != Animation.RigRemapUtils.ChannelFilter.None;
            bool hasRotation = (filter & Animation.RigRemapUtils.ChannelFilter.Rotation) != Animation.RigRemapUtils.ChannelFilter.None;
            bool hasScale = (filter & Animation.RigRemapUtils.ChannelFilter.Scale) != Animation.RigRemapUtils.ChannelFilter.None;

            for (int i = 0; i < boneCount; ++i)
            {
                if (srcRigHashMap.TryGetValue(hasher.ToHash(transformChannels[i].ID), out RigElementValue value))
                {
                    if (hasTranslation)
                        translations.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
                    if (hasRotation)
                        rotations.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
                    if (hasScale)
                        scales.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
                }
            }

            if (hasRotation)
            {
                for (int i = 0; i < quaternionCount; ++i)
                {
                    if (srcRigHashMap.TryGetValue(hasher.ToHash(quaternionChannels[i].ID), out RigElementValue value))
                        rotations.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
                }
            }

            if ((filter & Animation.RigRemapUtils.ChannelFilter.Float) != Animation.RigRemapUtils.ChannelFilter.None)
            {
                for (int i = 0; i < floatCount; ++i)
                {
                    if (srcRigHashMap.TryGetValue(hasher.ToHash(floatChannels[i].ID), out RigElementValue value))
                        floats.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
                }
            }

            if ((filter & Animation.RigRemapUtils.ChannelFilter.Int) != Animation.RigRemapUtils.ChannelFilter.None)
            {
                for (int i = 0; i < intCount; ++i)
                {
                    if (srcRigHashMap.TryGetValue(hasher.ToHash(intChannels[i].ID), out RigElementValue value))
                        ints.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
                }
            }
        }

        static void OverrideBindingOffset(NativeHashMap<StringHash, RigElementValue> srcRigHashMap, NativeList<StringHash> bindingOverride, NativeList<RigRemapEntry> remapEntries)
        {
            for (int i = 0; i < bindingOverride.Length; ++i)
            {
                if (srcRigHashMap.TryGetValue(bindingOverride[i], out RigElementValue value))
                {
                    if (value.Type == RigElementType.Bone)
                    {
                        for (int j = 0; j < remapEntries.Length; ++j)
                        {
                            if (value.Index == remapEntries[j].SourceIndex)
                            {
                                var entry = remapEntries[j];
                                entry.OffsetIndex = i + 1;
                                remapEntries[j] = entry;
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
