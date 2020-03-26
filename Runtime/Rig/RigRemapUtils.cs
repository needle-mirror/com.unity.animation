using System.Collections.Generic;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

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

        // Given source and destination RigDefinition, this function creates a remap table based on matching bindings
        // Offset overrides can be specified when necessary for certain bones
        public static BlobAssetReference<RigRemapTable> CreateRemapTable(
            BlobAssetReference<RigDefinition> srcRig,
            BlobAssetReference<RigDefinition> dstRig,
            OffsetOverrides offsetOverrides = default
            )
        {
            if (srcRig == default)
                throw new System.ArgumentNullException(nameof(srcRig));
            if (dstRig == default)
                throw new System.ArgumentNullException(nameof(dstRig));

            ref var srcBindings = ref srcRig.Value.Bindings;
            ref var dstBindings = ref dstRig.Value.Bindings;

            var translationMatches = GatherMatchingBindings(ref srcBindings.TranslationBindings, ref dstBindings.TranslationBindings);
            var rotationMatches = GatherMatchingBindings(ref srcBindings.RotationBindings, ref dstBindings.RotationBindings);
            var scaleMatches = GatherMatchingBindings(ref srcBindings.ScaleBindings, ref dstBindings.ScaleBindings);
            var floatMatches = GatherMatchingBindings(ref srcBindings.FloatBindings, ref dstBindings.FloatBindings);
            var intMatches = GatherMatchingBindings(ref srcBindings.IntBindings, ref dstBindings.IntBindings);

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var rigRemapTable = ref blobBuilder.ConstructRoot<RigRemapTable>();

            if (offsetOverrides.IsCreated)
            {
                if (offsetOverrides.HasTranslationOffsetOverrides)
                    UpdateBindingOffsets(ref dstBindings.TranslationBindings, offsetOverrides.m_TranslationIds, translationMatches);

                if (offsetOverrides.HasRotationOffsetOverrides)
                    UpdateBindingOffsets(ref dstBindings.RotationBindings, offsetOverrides.m_RotationIds, rotationMatches);

                var sortedLocalToRootTREntries = ComputeSortedLocalToRootTREntries(translationMatches, rotationMatches, offsetOverrides);
                FillRigMapperBlobBuffer(ref blobBuilder, offsetOverrides.m_TranslationOffsets, ref rigRemapTable.TranslationOffsets);
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

            return rigRemapTableAsset;
        }

        static NativeList<RigRemapEntry> GatherMatchingBindings(ref BlobArray<StringHash> srcBindings, ref BlobArray<StringHash> dstBindings)
        {
            var matches = new NativeList<RigRemapEntry>(Allocator.Persistent);
            if (srcBindings.Length == 0 || dstBindings.Length == 0)
                return matches;

            matches.Capacity = math.max(srcBindings.Length, dstBindings.Length);
            for (int i = 0; i != srcBindings.Length; i++)
            {
                var dstIdx = Core.FindBindingIndex(ref dstBindings, srcBindings[i]);
                if (dstIdx != -1)
                {
                    matches.AddNoResize(new RigRemapEntry { SourceIndex = i, DestinationIndex = dstIdx, OffsetIndex = -1 });
                }
            }

            return matches;
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

        internal static NativeList<int2> ComputeSortedLocalToRootTREntries(
            NativeList<RigRemapEntry> translationMatches,
            NativeList<RigRemapEntry> rotationMatches,
            OffsetOverrides offsetOverrides
            )
        {
            return ComputeSortedLocalToRootTREntries(
                translationMatches,
                rotationMatches,
                offsetOverrides.m_TranslationOffsets,
                offsetOverrides.m_RotationOffsets,
                offsetOverrides.LocalToRootTranslationCount,
                offsetOverrides.LocalToRootRotationCount
                );
        }
 
        internal static NativeList<int2> ComputeSortedLocalToRootTREntries(
            NativeList<RigRemapEntry> translationMatches,
            NativeList<RigRemapEntry> rotationMatches,
            NativeList<RigTranslationOffset> translationOffsets,
            NativeList<RigRotationOffset> rotationOffsets,
            int localToRootTranslationCount,
            int localToRootRotationCount
            )
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            Assert.IsTrue(translationMatches.IsCreated);
            Assert.IsTrue(rotationMatches.IsCreated);
            Assert.IsTrue(translationOffsets.IsCreated);
            Assert.IsTrue(rotationOffsets.IsCreated);
#endif
            NativeList<int2> orderedLocalToWorld = new NativeList<int2>(Allocator.Persistent);
            int totalCount = localToRootTranslationCount + localToRootRotationCount;
            if (totalCount == 0)
                return orderedLocalToWorld;

            orderedLocalToWorld.Capacity = totalCount;

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

            while (tEntryIdx != translationMatches.Length && rEntryIdx != rotationMatches.Length)
            {
                // Get destination index for T/R operation, this is set to -1 if we are at the end of the entries for a list
                int tDstIdx = (tEntryIdx == translationMatches.Length) ? -1 : translationMatches[tEntryIdx].DestinationIndex;
                int rDstIdx = (rEntryIdx == rotationMatches.Length) ? -1 : rotationMatches[rEntryIdx].DestinationIndex;

                if (tDstIdx != -1 && rDstIdx == -1)
                {
                    // We only have a valid translation operation
                    orderedLocalToWorld.Add(math.int2(tEntryIdx, -1));
                }
                else if (tDstIdx == -1 && rDstIdx != -1)
                {
                    // We only have a valid rotation operation
                    orderedLocalToWorld.Add(math.int2(-1, rEntryIdx));
                }
                else
                {
                    // If both T/R are valid, chose which operations should come first when
                    // destination indices don't match.
                    if (tDstIdx == rDstIdx)
                        orderedLocalToWorld.Add(math.int2(tEntryIdx, rEntryIdx));
                    else if (tDstIdx < rDstIdx)
                    {
                        orderedLocalToWorld.Add(math.int2(tEntryIdx, -1));
                        orderedLocalToWorld.Add(math.int2(-1, rEntryIdx));
                    }
                    else
                    {
                        orderedLocalToWorld.Add(math.int2(-1, rEntryIdx));
                        orderedLocalToWorld.Add(math.int2(tEntryIdx, -1));
                    }
                }

                if (tEntryIdx != translationMatches.Length)
                    tEntryIdx++;
                if (rEntryIdx != rotationMatches.Length)
                    rEntryIdx++;
            }

            return orderedLocalToWorld;
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
