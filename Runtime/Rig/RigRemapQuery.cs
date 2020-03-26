using System;
using System.Collections.Generic;

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    public struct ChannelMap
    {
        public StringHash SourceId;
        public StringHash DestinationId;

        public int OffsetIndex; // index 0 (default) means no or identity offset
    }

    ///
    /// var rigRemapQuery = new RigRemapQuery {
    ///        AllChannels = new []
    ///                            { new { SourceId = "Root", DestinationId = "AnotherRoot"}
    ///                            },
    ///        TranslationChannels = new []
    ///                            { new { SourceId = "Hips", DestinationId = "AnotherHips", OffsetIndex = 1 }
    ///                            },
    ///    };
    /// var remapTable = rigRemapQuery.ToRigRemapTable(sourceRig, destinationRig);
    ///
    ///
    public class RigRemapQuery
    {
        /// <summary>
        /// The RigRemapQuery includes all channels of any type with a matching ID in both the source and destination rig.
        /// </summary>
        public ChannelMap[] AllChannels = Array.Empty<ChannelMap>();

        /// <summary>
        /// The RigRemapQuery includes all channels of type 'LocalTranslationChannel' with a matching ID in both the source and destination rig.
        /// </summary>
        public ChannelMap[] TranslationChannels = Array.Empty<ChannelMap>();

        /// <summary>
        /// The RigRemapQuery includes all translation offsets indexed by translation channels OffsetIndex. Index 0 (default) must be allocated as soon as there is at least one other translation offset.
        /// </summary>
        public RigTranslationOffset[] TranslationOffsets = Array.Empty<RigTranslationOffset>();

        /// <summary>
        /// The RigRemapQuery includes all channels of type 'LocalRotationChannel' with a matching ID in both the source and destination rig.
        /// </summary>
        public ChannelMap[] RotationChannels = Array.Empty<ChannelMap>();

        /// <summary>
        /// The RigRemapQuery includes all rotation offsets indexed by rotation channels OffsetIndex. Index 0 (default)  must be allocated as soon as there is at least one other rotation offset.
        /// </summary>
        public RigRotationOffset[] RotationOffsets = Array.Empty<RigRotationOffset>();

        /// <summary>
        /// The RigRemapQuery includes all channels of type 'LocalScaleChannel' with a matching ID in both the source and destination rig.
        /// </summary>
        public ChannelMap[] ScaleChannels = Array.Empty<ChannelMap>();

        /// <summary>
        /// The RigRemapQuery includes all channels of type 'FloatChannel' with a matching ID in both the source and destination rig.
        /// </summary>
        public ChannelMap[] FloatChannels = Array.Empty<ChannelMap>();

        /// <summary>
        /// The RigRemapQuery includes all channels of type 'IntChannel' with a matching ID in both the source and destination rig.
        /// </summary>
        public ChannelMap[] IntChannels = Array.Empty<ChannelMap>();

        static List<ChannelMap> GetChannelsMap(ChannelMap[] allChannels, ChannelMap[] typedChannels)
        {
            var channels = new List<ChannelMap>(allChannels);
            channels.AddRange(typedChannels);

            return channels;
        }

        static void ValidateNoDuplicatedDestinationChannel(List<ChannelMap> channels)
        {
            for(int i=0;i!=channels.Count;i++)
            {
                for(int j=0;j!=channels.Count;j++)
                {
                    if (i == j)
                        continue;

                    if (channels[i].DestinationId == channels[j].DestinationId)
                        throw new InvalidOperationException($"RigRemapQuery cannot have more than one channel mapping that target the same destination. '{channels[i].SourceId}, {channels[i].DestinationId}' and '{channels[j].SourceId}, {channels[j].DestinationId}'");
                }
            }
        }

        static void ValidateOffsetIndex(List<ChannelMap> channels, int offsetCount)
        {
            for(int channelIter = 0; channelIter != channels.Count; channelIter++)
            {
                if (channels[channelIter].OffsetIndex > 0)
                {
                    if (channels[channelIter].OffsetIndex >= offsetCount)
                    {
                        throw new InvalidOperationException($"RigRemapQuery cannot have a channel offset index out of bounds. '{channels[channelIter].DestinationId}' offset index {channels[channelIter].OffsetIndex} >= {offsetCount}");
                    }
                }
            }
        }

        static NativeList<RigRemapEntry> GetMatchingRemapEntries(List<ChannelMap> channels, ref BlobArray<StringHash> sourceBindings, ref BlobArray<StringHash> destinationBindings, int offsetCount = 0)
        {
            var rigRemapEntry = new NativeList<RigRemapEntry>(channels.Count, Allocator.Persistent);
            for (int i = 0; i != channels.Count; i++)
            {
                var sourceIndex = Core.FindBindingIndex(ref sourceBindings, channels[i].SourceId);
                var destinationIndex = Core.FindBindingIndex(ref destinationBindings, channels[i].DestinationId);
                if(sourceIndex != -1 && destinationIndex != -1 && (channels[i].OffsetIndex == 0 || channels[i].OffsetIndex < offsetCount))
                {
                    rigRemapEntry.Add(new RigRemapEntry { SourceIndex = sourceIndex, DestinationIndex = destinationIndex, OffsetIndex = channels[i].OffsetIndex });
                }
            }

            return rigRemapEntry;
        }

        public BlobAssetReference<RigRemapTable> ToRigRemapTable(BlobAssetReference<RigDefinition> sourceRigDefinition, BlobAssetReference<RigDefinition> destinationRigDefinition)
        {
            if (sourceRigDefinition == default)
                throw new ArgumentNullException(nameof(sourceRigDefinition));

            if (destinationRigDefinition == default)
                throw new ArgumentNullException(nameof(destinationRigDefinition));

            var translations = GetChannelsMap(AllChannels, TranslationChannels);
            var rotations = GetChannelsMap(AllChannels, RotationChannels);
            var scales = GetChannelsMap(AllChannels, ScaleChannels);
            var floats = GetChannelsMap(AllChannels, FloatChannels);
            var ints = GetChannelsMap(AllChannels, IntChannels);

            ValidateNoDuplicatedDestinationChannel(translations);
            ValidateNoDuplicatedDestinationChannel(rotations);
            ValidateNoDuplicatedDestinationChannel(scales);
            ValidateNoDuplicatedDestinationChannel(floats);
            ValidateNoDuplicatedDestinationChannel(ints);

            ValidateOffsetIndex(translations, TranslationOffsets.Length);
            ValidateOffsetIndex(rotations, RotationOffsets.Length);
            ValidateOffsetIndex(scales, 0);
            ValidateOffsetIndex(floats, 0);
            ValidateOffsetIndex(ints, 0);

            var translationsRemapEntry = GetMatchingRemapEntries(translations, ref sourceRigDefinition.Value.Bindings.TranslationBindings, ref destinationRigDefinition.Value.Bindings.TranslationBindings, TranslationOffsets.Length);
            var rotationsRemapEntry = GetMatchingRemapEntries(rotations, ref sourceRigDefinition.Value.Bindings.RotationBindings, ref destinationRigDefinition.Value.Bindings.RotationBindings, RotationOffsets.Length);
            var scalesRemapEntry = GetMatchingRemapEntries(scales, ref sourceRigDefinition.Value.Bindings.ScaleBindings, ref destinationRigDefinition.Value.Bindings.ScaleBindings);
            var floatsRemapEntry = GetMatchingRemapEntries(floats, ref sourceRigDefinition.Value.Bindings.FloatBindings, ref destinationRigDefinition.Value.Bindings.FloatBindings);
            var intsRemapEntry = GetMatchingRemapEntries(ints, ref sourceRigDefinition.Value.Bindings.IntBindings, ref destinationRigDefinition.Value.Bindings.IntBindings);

            var translationOffsets = new NativeList<RigTranslationOffset>(Allocator.Temp);
            var rotationOffsets = new NativeList<RigRotationOffset>(Allocator.Temp);
            translationOffsets.CopyFrom(TranslationOffsets);
            rotationOffsets.CopyFrom(RotationOffsets);

            int localToRootTranslationOffsetCount = 0;
            for (int i = 1; i < translationOffsets.Length; ++i)
                if (translationOffsets[i].Space == RigRemapSpace.LocalToRoot)
                    localToRootTranslationOffsetCount++;

            int localToRootRotationOffsetCount = 0;
            for (int i = 1; i < rotationOffsets.Length; ++i)
                if (rotationOffsets[i].Space == RigRemapSpace.LocalToRoot)
                    localToRootRotationOffsetCount++;

            var sortedLocalToRootTREntries = RigRemapUtils.ComputeSortedLocalToRootTREntries(
                translationsRemapEntry,
                rotationsRemapEntry,
                translationOffsets,
                rotationOffsets,
                localToRootTranslationOffsetCount,
                localToRootRotationOffsetCount
                );

            var blobBuilder = new BlobBuilder(Allocator.Temp);

            ref var rigRemapTable = ref blobBuilder.ConstructRoot<RigRemapTable>();

            RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, translationsRemapEntry, ref rigRemapTable.TranslationMappings);
            RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, rotationsRemapEntry, ref rigRemapTable.RotationMappings);
            RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, scalesRemapEntry, ref rigRemapTable.ScaleMappings);
            RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, floatsRemapEntry, ref rigRemapTable.FloatMappings);
            RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, intsRemapEntry, ref rigRemapTable.IntMappings);
            RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, sortedLocalToRootTREntries, ref rigRemapTable.SortedLocalToRootTREntries);
            RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, translationOffsets, ref rigRemapTable.TranslationOffsets);
            RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, rotationOffsets, ref rigRemapTable.RotationOffsets);
            rigRemapTable.LocalToParentTRCount = math.int2(
                translationsRemapEntry.Length - localToRootTranslationOffsetCount,
                rotationsRemapEntry.Length - localToRootRotationOffsetCount
                );

            var rigRemapTableAsset = blobBuilder.CreateBlobAssetReference<RigRemapTable>(Allocator.Persistent);

            blobBuilder.Dispose();

            translationsRemapEntry.Dispose();
            rotationsRemapEntry.Dispose();
            scalesRemapEntry.Dispose();
            floatsRemapEntry.Dispose();
            intsRemapEntry.Dispose();
            sortedLocalToRootTREntries.Dispose();

            return rigRemapTableAsset;
        }
    }
}
