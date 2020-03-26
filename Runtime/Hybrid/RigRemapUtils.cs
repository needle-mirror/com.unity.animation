using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Animation.Hybrid
{
    public static class RigRemapUtils
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

        // Given source and destination RigComponents create a remap table based on matching ids. A BindingHashDelegate can be specified in order to
        // match using either the transform path (BindingHashUtils.HashFullPath), the transform name (BindingHashUtils.HashName) or a custom delegate.
        // If no binding hash deletegate is specified, the system wide BindingHashUtils.DefaultBindingHash is used.
        // By default, LocalToParent mapping is performed however offset overrides can be specified to remap in LocalToRoot space and/or add translation/rotation offsets.
        public static BlobAssetReference<RigRemapTable> CreateRemapTable(
            RigComponent srcRig,
            RigComponent dstRig,
            Animation.RigRemapUtils.OffsetOverrides offsetOverrides = default,
            BindingHashDelegate bindingHash = null
            )
        {
            if (srcRig == null)
                throw new System.ArgumentNullException(nameof(srcRig));
            if (dstRig == null)
                throw new System.ArgumentNullException(nameof(dstRig));

            var hasher = bindingHash ?? BindingHashUtils.DefaultBindingHash;

            var srcRigHashMap = RigComponentHashMap(srcRig, hasher);
            FindMatches(
                srcRigHashMap,
                dstRig,
                out NativeList<RigRemapEntry> translationMatches,
                out NativeList<RigRemapEntry> rotationMatches,
                out NativeList<RigRemapEntry> scaleMatches,
                out NativeList<RigRemapEntry> floatMatches,
                out NativeList<RigRemapEntry> intMatches,
                hasher
                );

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var rigRemapTable = ref blobBuilder.ConstructRoot<RigRemapTable>();

            if (offsetOverrides.IsCreated)
            {
                if (offsetOverrides.HasTranslationOffsetOverrides)
                    OverrideBindingOffset(srcRigHashMap, offsetOverrides.m_TranslationIds, translationMatches);

                if (offsetOverrides.HasRotationOffsetOverrides)
                    OverrideBindingOffset(srcRigHashMap, offsetOverrides.m_RotationIds, rotationMatches);

                var sortedLocalToRootTREntries = Animation.RigRemapUtils.ComputeSortedLocalToRootTREntries(translationMatches, rotationMatches, offsetOverrides);
                Animation.RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, offsetOverrides.m_TranslationOffsets, ref rigRemapTable.TranslationOffsets);
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
                translationMatches.Length - offsetOverrides.LocalToRootTranslationCount,
                rotationMatches.Length - offsetOverrides.LocalToRootRotationCount
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

        static NativeHashMap<StringHash, RigElementValue> RigComponentHashMap(RigComponent rig, BindingHashDelegate hasher)
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
                map.TryAdd(hasher(RigGenerator.ComputeRelativePath(rig.Bones[i], rig.transform)), new RigElementValue { Index = i, Type = RigElementType.Bone });

            for (int i = 0; i < translationCount; ++i)
                map.TryAdd(hasher(rig.TranslationChannels[i].Id), new RigElementValue { Index = boneCount + i, Type = RigElementType.Translation });

            for (int i = 0; i < rotationCount; ++i)
                map.TryAdd(hasher(rig.RotationChannels[i].Id), new RigElementValue { Index = boneCount + i, Type = RigElementType.Rotation });

            for (int i = 0; i < scaleCount; ++i)
                map.TryAdd(hasher(rig.ScaleChannels[i].Id), new RigElementValue { Index = boneCount + i, Type = RigElementType.Scale });

            for (int i = 0; i < floatCount; ++i)
                map.TryAdd(hasher(rig.FloatChannels[i].Id), new RigElementValue { Index = i, Type = RigElementType.Float });

            for (int i = 0; i < intCount; ++i)
                map.TryAdd(hasher(rig.IntChannels[i].Id), new RigElementValue { Index = i, Type = RigElementType.Int });

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
            BindingHashDelegate hasher
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

            for (int i = 0; i < boneCount; ++i)
            {
                if (srcRigHashMap.TryGetValue(hasher(RigGenerator.ComputeRelativePath(dstRig.Bones[i], dstRig.transform)), out RigElementValue value))
                {
                    translations.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
                    rotations.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
                    scales.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
                }
            }

            for (int i = 0; i < translationCount; ++i)
            {
                if (srcRigHashMap.TryGetValue(hasher(dstRig.TranslationChannels[i].Id), out RigElementValue value))
                    translations.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
            }

            for (int i = 0; i < rotationCount; ++i)
            {
                if (srcRigHashMap.TryGetValue(hasher(dstRig.RotationChannels[i].Id), out RigElementValue value))
                    rotations.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
            }

            for (int i = 0; i < scaleCount; ++i)
            {
                if (srcRigHashMap.TryGetValue(hasher(dstRig.ScaleChannels[i].Id), out RigElementValue value))
                    scales.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
            }

            for (int i = 0; i < floatCount; ++i)
            {
                if (srcRigHashMap.TryGetValue(hasher(dstRig.FloatChannels[i].Id), out RigElementValue value))
                    floats.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
            }

            for (int i = 0; i < intCount; ++i)
            {
                if (srcRigHashMap.TryGetValue(hasher(dstRig.IntChannels[i].Id), out RigElementValue value))
                    ints.Add(new RigRemapEntry { SourceIndex = value.Index, DestinationIndex = i, OffsetIndex = -1 });
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
