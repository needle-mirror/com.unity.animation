using System.Collections.Generic;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animation
{
    public static class RigRemapUtils
    {
        static int CompareRigRemapEntry(int lhsIndex, RigRemapSpace lhsSpace, int rhsIndex, RigRemapSpace rhsSpace)
        {
            var spaceCompare = lhsSpace.CompareTo(rhsSpace);
            return spaceCompare != 0 ? spaceCompare : lhsIndex.CompareTo(rhsIndex);
        }

        struct RigRemapTranslationEntryComparer : IComparer<RigRemapEntry>
        {
            public NativeList<RigTranslationOffset> TranslationOffets;

            public int Compare(RigRemapEntry x, RigRemapEntry y) => CompareRigRemapEntry(
                x.DestinationIndex, x.OffsetIndex > 0 ? TranslationOffets[x.OffsetIndex].Space : RigRemapSpace.LocalToParent,
                y.DestinationIndex, y.OffsetIndex > 0 ? TranslationOffets[y.OffsetIndex].Space : RigRemapSpace.LocalToParent
            );
        }

        struct RigRemapRotationEntryComparer : IComparer<RigRemapEntry>
        {
            public NativeList<RigRotationOffset> RotationOffets;

            public int Compare(RigRemapEntry x, RigRemapEntry y) => CompareRigRemapEntry(
                x.DestinationIndex, x.OffsetIndex > 0 ? RotationOffets[x.OffsetIndex].Space : RigRemapSpace.LocalToParent,
                y.DestinationIndex, y.OffsetIndex > 0 ? RotationOffets[y.OffsetIndex].Space : RigRemapSpace.LocalToParent);
        }

        [System.Flags]
        public enum ChannelFilter : byte
        {
            None        = 0,
            Translation = 1 << 0,
            Rotation    = 1 << 1,
            Scale       = 1 << 2,
            Float       = 1 << 3,
            Int         = 1 << 4,
            All         = Translation | Rotation | Scale | Float | Int
        }

        public struct OffsetOverrides : System.IDisposable
        {
            internal NativeList<StringHash>           m_TranslationIds;
            internal NativeList<StringHash>           m_RotationIds;
            internal NativeList<RigTranslationOffset> m_TranslationOffsets;
            internal NativeList<RigRotationOffset>    m_RotationOffsets;

            internal int LocalToRootTranslationCount { get; private set; }
            internal int LocalToRootRotationCount { get; private set; }

            public OffsetOverrides(int capacity, Allocator allocator)
            {
                m_TranslationIds = new NativeList<StringHash>(capacity, allocator);
                m_RotationIds = new NativeList<StringHash>(capacity, allocator);
                m_TranslationOffsets = new NativeList<RigTranslationOffset>(capacity, allocator);
                m_RotationOffsets = new NativeList<RigRotationOffset>(capacity, allocator);

                m_TranslationOffsets.Add(default);
                m_RotationOffsets.Add(default);
                LocalToRootTranslationCount = 0;
                LocalToRootRotationCount = 0;
            }

            public bool IsCreated => m_TranslationIds.IsCreated || m_RotationIds.IsCreated;
            public bool HasTranslationOffsetOverrides => m_TranslationIds.IsCreated ? m_TranslationIds.Length > 0 : false;
            public bool HasRotationOffsetOverrides => m_RotationIds.IsCreated ? m_RotationIds.Length > 0 : false;

            public void AddTranslationOffsetOverride(StringHash binding, RigTranslationOffset offset)
            {
                m_TranslationIds.Add(binding);
                m_TranslationOffsets.Add(offset);

                if (offset.Space == RigRemapSpace.LocalToRoot)
                    LocalToRootTranslationCount++;
            }

            public void AddRotationOffsetOverride(StringHash binding, RigRotationOffset offset)
            {
                m_RotationIds.Add(binding);
                m_RotationOffsets.Add(offset);

                if (offset.Space == RigRemapSpace.LocalToRoot)
                    LocalToRootRotationCount++;
            }

            public void Dispose()
            {
                m_TranslationIds.Dispose();
                m_RotationIds.Dispose();
                m_TranslationOffsets.Dispose();
                m_RotationOffsets.Dispose();
            }
        }

        /// <summary>
        /// Given source and destination BlobAssetReference<RigDefinition>, this function creates a remap table based on matching bindings.
        /// </summary>
        /// <param name="srcRig">The source BlobAssetReference<RigDefinition> to remap from.</param>
        /// <param name="dstRig">The destination BlobAssetReference<RigDefinition> to remap to.</param>
        /// <param name="filter">Optional parameter to filter matches based on channel type. By default, all types of channels are used for matching.</param>
        /// <param name="offsetOverrides">Optional parameter to specify which translation or rotation channels should be matched with offsets in a given space (LocalToParent vs. LocalToRoot). By default, LocalToParent mapping is performed.</param>
        /// <returns>Returns the BlobAssetReference of the RigRemapTable.</returns>

        public static BlobAssetReference<RigRemapTable> CreateRemapTable(
            BlobAssetReference<RigDefinition> srcRig,
            BlobAssetReference<RigDefinition> dstRig,
            ChannelFilter filter = ChannelFilter.All,
            OffsetOverrides offsetOverrides = default
        )
        {
            Core.ValidateArgumentIsCreated(srcRig);
            Core.ValidateArgumentIsCreated(dstRig);

            ref var srcBindings = ref srcRig.Value.Bindings;
            ref var dstBindings = ref dstRig.Value.Bindings;

            var translationMatches = new NativeList<RigRemapEntry>(Allocator.Temp);
            var rotationMatches = new NativeList<RigRemapEntry>(Allocator.Temp);
            var scaleMatches = new NativeList<RigRemapEntry>(Allocator.Temp);
            var floatMatches = new NativeList<RigRemapEntry>(Allocator.Temp);
            var intMatches = new NativeList<RigRemapEntry>(Allocator.Temp);

            bool hasTranslationChannels = (filter & ChannelFilter.Translation) != ChannelFilter.None;
            bool hasRotationChannels = (filter & ChannelFilter.Rotation) != ChannelFilter.None;

            if (hasTranslationChannels)
                GatherMatchingBindings(translationMatches, ref srcBindings.TranslationBindings, ref dstBindings.TranslationBindings);
            if (hasRotationChannels)
                GatherMatchingBindings(rotationMatches, ref srcBindings.RotationBindings, ref dstBindings.RotationBindings);
            if ((filter & ChannelFilter.Scale) != ChannelFilter.None)
                GatherMatchingBindings(scaleMatches, ref srcBindings.ScaleBindings, ref dstBindings.ScaleBindings);
            if ((filter & ChannelFilter.Float) != ChannelFilter.None)
                GatherMatchingBindings(floatMatches, ref srcBindings.FloatBindings, ref dstBindings.FloatBindings);
            if ((filter & ChannelFilter.Int) != ChannelFilter.None)
                GatherMatchingBindings(intMatches, ref srcBindings.IntBindings, ref dstBindings.IntBindings);

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var rigRemapTable = ref blobBuilder.ConstructRoot<RigRemapTable>();

            int localToRootTranslationCount = 0;
            int localToRootRotationCount = 0;
            if (offsetOverrides.IsCreated)
            {
                if (hasTranslationChannels && offsetOverrides.HasTranslationOffsetOverrides)
                {
                    UpdateBindingOffsets(ref dstBindings.TranslationBindings, offsetOverrides.m_TranslationIds, translationMatches);
                    localToRootTranslationCount = offsetOverrides.LocalToRootTranslationCount;
                }
                else
                    localToRootTranslationCount = 0;

                if (hasRotationChannels && offsetOverrides.HasRotationOffsetOverrides)
                {
                    UpdateBindingOffsets(ref dstBindings.RotationBindings, offsetOverrides.m_RotationIds, rotationMatches);
                    localToRootRotationCount = offsetOverrides.LocalToRootRotationCount;
                }
                else
                    localToRootRotationCount = 0;

                var sortedLocalToRootTREntries = new NativeList<int2>(Allocator.Temp);
                ComputeSortedLocalToRootTREntries(
                    sortedLocalToRootTREntries,
                    translationMatches,
                    rotationMatches,
                    offsetOverrides.m_TranslationOffsets,
                    offsetOverrides.m_RotationOffsets,
                    localToRootTranslationCount,
                    localToRootRotationCount
                );

                if (hasTranslationChannels)
                    FillRigMapperBlobBuffer(ref blobBuilder, offsetOverrides.m_TranslationOffsets, ref rigRemapTable.TranslationOffsets);
                if (hasRotationChannels)
                    FillRigMapperBlobBuffer(ref blobBuilder, offsetOverrides.m_RotationOffsets, ref rigRemapTable.RotationOffsets);

                FillRigMapperBlobBuffer(ref blobBuilder, sortedLocalToRootTREntries, ref rigRemapTable.SortedLocalToRootTREntries);
                sortedLocalToRootTREntries.Dispose();
            }

            FillRigMapperBlobBuffer(ref blobBuilder, translationMatches, ref rigRemapTable.TranslationMappings);
            FillRigMapperBlobBuffer(ref blobBuilder, rotationMatches, ref rigRemapTable.RotationMappings);
            FillRigMapperBlobBuffer(ref blobBuilder, scaleMatches, ref rigRemapTable.ScaleMappings);
            FillRigMapperBlobBuffer(ref blobBuilder, floatMatches, ref rigRemapTable.FloatMappings);
            FillRigMapperBlobBuffer(ref blobBuilder, intMatches, ref rigRemapTable.IntMappings);
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

            return rigRemapTableAsset;
        }

        static void GatherMatchingBindings(NativeList<RigRemapEntry> matches, ref BlobArray<StringHash> srcBindings, ref BlobArray<StringHash> dstBindings)
        {
            if (srcBindings.Length == 0 || dstBindings.Length == 0)
                return;

            matches.Capacity = math.max(srcBindings.Length, dstBindings.Length);
            for (int i = 0; i != srcBindings.Length; i++)
            {
                var dstIdx = Core.FindBindingIndex(ref dstBindings, srcBindings[i]);
                if (dstIdx != -1)
                {
                    matches.AddNoResize(new RigRemapEntry { SourceIndex = i, DestinationIndex = dstIdx, OffsetIndex = -1 });
                }
            }
        }

        static void UpdateBindingOffsets(ref BlobArray<StringHash> dstBindings, NativeList<StringHash> bindingOverride, NativeList<RigRemapEntry> remapEntries)
        {
            for (int i = 0; i < bindingOverride.Length; ++i)
            {
                var dstIdx = Core.FindBindingIndex(ref dstBindings, bindingOverride[i]);
                if (dstIdx != -1)
                {
                    for (int j = 0; j < remapEntries.Length; ++j)
                    {
                        if (dstIdx == remapEntries[j].DestinationIndex)
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

        internal static void ComputeSortedLocalToRootTREntries(
            NativeList<int2> sortedLocalToRootTREntries,
            NativeList<RigRemapEntry> translationMatches,
            NativeList<RigRemapEntry> rotationMatches,
            NativeList<RigTranslationOffset> translationOffsets,
            NativeList<RigRotationOffset> rotationOffsets,
            int localToRootTranslationCount,
            int localToRootRotationCount
        )
        {
            Core.ValidateArgumentIsCreated(translationMatches);
            Core.ValidateArgumentIsCreated(rotationMatches);
            Core.ValidateArgumentIsCreated(translationOffsets);
            Core.ValidateArgumentIsCreated(rotationOffsets);

            int totalCount = localToRootTranslationCount + localToRootRotationCount;
            if (totalCount == 0)
                return;

            sortedLocalToRootTREntries.Capacity = totalCount;

            // Sort both Translation/Rotation matches so that LocalToRoot space mappings are at the end of the lists and by increasing destination index
            if (localToRootTranslationCount > 0)
                translationMatches.Sort(new RigRemapTranslationEntryComparer { TranslationOffets = translationOffsets });
            if (localToRootRotationCount > 0)
                rotationMatches.Sort(new RigRemapRotationEntryComparer { RotationOffets = rotationOffsets });

            // Compute an ordered list of LocalToWorld entries by sorting T/R operations based on destination index.
            // If both T/R operations target the same destination index then one entry is added. Otherwise,
            // the operation with the smallest destination index is added first.

            int tEntryIdx = translationMatches.Length - localToRootTranslationCount;
            int rEntryIdx = rotationMatches.Length - localToRootRotationCount;

            while (tEntryIdx != translationMatches.Length || rEntryIdx != rotationMatches.Length)
            {
                // Get destination index for T/R operation, this is set to -1 if we are at the end of the entries for a list
                int tDstIdx = (tEntryIdx == translationMatches.Length) ? -1 : translationMatches[tEntryIdx].DestinationIndex;
                int rDstIdx = (rEntryIdx == rotationMatches.Length) ? -1 : rotationMatches[rEntryIdx].DestinationIndex;

                if (tDstIdx != -1 && rDstIdx == -1)
                {
                    // We only have a valid translation operation
                    sortedLocalToRootTREntries.Add(math.int2(tEntryIdx, -1));
                }
                else if (tDstIdx == -1 && rDstIdx != -1)
                {
                    // We only have a valid rotation operation
                    sortedLocalToRootTREntries.Add(math.int2(-1, rEntryIdx));
                }
                else
                {
                    // If both T/R are valid, chose which operations should come first when
                    // destination indices don't match.
                    if (tDstIdx == rDstIdx)
                        sortedLocalToRootTREntries.Add(math.int2(tEntryIdx, rEntryIdx));
                    else if (tDstIdx < rDstIdx)
                    {
                        sortedLocalToRootTREntries.Add(math.int2(tEntryIdx, -1));
                        sortedLocalToRootTREntries.Add(math.int2(-1, rEntryIdx));
                    }
                    else
                    {
                        sortedLocalToRootTREntries.Add(math.int2(-1, rEntryIdx));
                        sortedLocalToRootTREntries.Add(math.int2(tEntryIdx, -1));
                    }
                }

                if (tEntryIdx != translationMatches.Length)
                    tEntryIdx++;
                if (rEntryIdx != rotationMatches.Length)
                    rEntryIdx++;
            }
        }

        internal static unsafe void FillRigMapperBlobBuffer<T>(ref BlobBuilder blobBuilder, NativeList<T> data, ref BlobArray<T> blobBuffer)
            where T : struct
        {
            if (!data.IsCreated || data.Length == 0)
                return;

            var builderArray = blobBuilder.Allocate(ref blobBuffer, data.Length);
            UnsafeUtility.MemCpy(
                builderArray.GetUnsafePtr(),
                data.GetUnsafeReadOnlyPtr(),
                UnsafeUtility.SizeOf<T>() * data.Length
            );
        }
    }
}
