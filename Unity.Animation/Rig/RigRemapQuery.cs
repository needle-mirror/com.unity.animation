using System;
using System.Diagnostics;

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    [BurstCompatible]
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

        /// <summary>
        /// Fill the channels list with channels that have a specific ID and typed channels.
        /// </summary>
        /// <param name="allChannels">Channels of any type with a matching ID in both the source and destination rig</param>
        /// <param name="typedChannels">Typed channels (LocalTranslation, LocalRotation, LocalScale, Float, Int) with a matching ID in both the source and destination rig</param>
        /// <param name="channelsList">A list that contains allChannels + typedChannels.</param>
        unsafe static void GetChannelsMap(ChannelMap[] allChannels, ChannelMap[] typedChannels, NativeList<ChannelMap> channelsList)
        {
            Core.ValidateArgumentIsCreated(channelsList);

            int totalSize = allChannels.Length + typedChannels.Length;
            if (channelsList.Capacity < totalSize)
                channelsList.Capacity = totalSize;

            if (allChannels.Length > 0)
            {
                fixed(void* ptr = &allChannels[0])
                {
                    channelsList.AddRangeNoResize(ptr, allChannels.Length);
                }
            }
            if (typedChannels.Length > 0)
            {
                fixed(void* ptr = &typedChannels[0])
                {
                    channelsList.AddRangeNoResize(ptr, typedChannels.Length);
                }
            }
        }

        /// <summary>
        /// Checks that the retarget does not map twice to the same destination.
        /// </summary>
        /// <param name="channels">The list of ChannelMap to validate.</param>
        /// <exception cref="InvalidOperationException">RigRemapQuery cannot have more than one channel mapping that target the same destination.</exception>
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateNoDuplicatedDestinationChannel(NativeList<ChannelMap> channels)
        {
            for (int i = 0; i != channels.Length; i++)
            {
                for (int j = 0; j != channels.Length; j++)
                {
                    if (i == j)
                        continue;

                    if (channels[i].DestinationId == channels[j].DestinationId)
                        throw new InvalidOperationException($"RigRemapQuery cannot have more than one channel mapping that target the same destination. '{channels[i].SourceId}, {channels[i].DestinationId}' and '{channels[j].SourceId}, {channels[j].DestinationId}'");
                }
            }
        }

        /// <summary>
        /// Validates that channels with an offset index reference a valid offset.
        /// </summary>
        /// <param name="channels">Mappings between a source rig and destination rig.</param>
        /// <param name="offsetCount"> The number of offsets (translation or rotation).</param>
        /// <exception cref="InvalidOperationException">RigRemapQuery cannot have a channel offset index out of bounds.</exception>
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateOffsetIndex(NativeList<ChannelMap> channels, int offsetCount)
        {
            for (int channelIter = 0; channelIter != channels.Length; channelIter++)
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

        /// <summary>
        /// For each channel, finds the channel index in the source rig and the channel index in the destination rig.
        /// </summary>
        /// <param name="channels">The channels to map between the two rigs.</param>
        /// <param name="sourceBindings">The bindings of the source rig.</param>
        /// <param name="destinationBindings">The bindings of the destination rig.</param>
        /// <param name="offsetCount">The number of offsets (translation or rotation).</param>
        /// <returns>A list that maps two channels between a source and a destination rig, with an optional offset.</returns>
        static NativeList<RigRemapEntry> GetMatchingRemapEntries(NativeList<ChannelMap> channels, ref BlobArray<StringHash> sourceBindings, ref BlobArray<StringHash> destinationBindings, int offsetCount = 0)
        {
            var rigRemapEntry = new NativeList<RigRemapEntry>(channels.Length, Allocator.Persistent);

            for (int i = 0; i != channels.Length; i++)
            {
                var sourceIndex = Core.FindBindingIndex(ref sourceBindings, channels[i].SourceId);
                var destinationIndex = Core.FindBindingIndex(ref destinationBindings, channels[i].DestinationId);
                if (sourceIndex != -1 && destinationIndex != -1 &&
                    (channels[i].OffsetIndex == 0 || channels[i].OffsetIndex < offsetCount))
                {
                    rigRemapEntry.Add(new RigRemapEntry
                    {
                        SourceIndex = sourceIndex, DestinationIndex = destinationIndex,
                        OffsetIndex = channels[i].OffsetIndex
                    });
                }
            }

            return rigRemapEntry;
        }

        /// <summary>
        /// Creates a rig remap table between a source rig and a target rig.
        /// </summary>
        /// <param name="sourceRigDefinition">The rig to remap from.</param>
        /// <param name="destinationRigDefinition">The rig to remap to.</param>
        /// <returns>A table of the mappings between the channels of the source and the destination rigs.</returns>
        /// <exception cref="ArgumentNullException">Both source and destination rigs must be defined.</exception>
        public BlobAssetReference<RigRemapTable> ToRigRemapTable(BlobAssetReference<RigDefinition> sourceRigDefinition, BlobAssetReference<RigDefinition> destinationRigDefinition)
        {
            Core.ValidateArgumentIsCreated(sourceRigDefinition);
            Core.ValidateArgumentIsCreated(destinationRigDefinition);

            var translations = new NativeList<ChannelMap>(AllChannels.Length + TranslationChannels.Length, Allocator.Temp);
            var rotations = new NativeList<ChannelMap>(AllChannels.Length + RotationChannels.Length, Allocator.Temp);
            var scales = new NativeList<ChannelMap>(AllChannels.Length + ScaleChannels.Length, Allocator.Temp);
            var floats = new NativeList<ChannelMap>(AllChannels.Length + FloatChannels.Length, Allocator.Temp);
            var ints = new NativeList<ChannelMap>(AllChannels.Length + IntChannels.Length, Allocator.Temp);

            GetChannelsMap(AllChannels, TranslationChannels, translations);
            GetChannelsMap(AllChannels, RotationChannels, rotations);
            GetChannelsMap(AllChannels, ScaleChannels, scales);
            GetChannelsMap(AllChannels, FloatChannels, floats);
            GetChannelsMap(AllChannels, IntChannels, ints);

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

            var translationsRemapEntry = GetMatchingRemapEntries(translations,
                ref sourceRigDefinition.Value.Bindings.TranslationBindings,
                ref destinationRigDefinition.Value.Bindings.TranslationBindings, TranslationOffsets.Length);
            var rotationsRemapEntry = GetMatchingRemapEntries(rotations,
                ref sourceRigDefinition.Value.Bindings.RotationBindings,
                ref destinationRigDefinition.Value.Bindings.RotationBindings, RotationOffsets.Length);
            var scalesRemapEntry = GetMatchingRemapEntries(scales,
                ref sourceRigDefinition.Value.Bindings.ScaleBindings,
                ref destinationRigDefinition.Value.Bindings.ScaleBindings);
            var floatsRemapEntry = GetMatchingRemapEntries(floats,
                ref sourceRigDefinition.Value.Bindings.FloatBindings,
                ref destinationRigDefinition.Value.Bindings.FloatBindings);
            var intsRemapEntry = GetMatchingRemapEntries(ints, ref sourceRigDefinition.Value.Bindings.IntBindings,
                ref destinationRigDefinition.Value.Bindings.IntBindings);

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

            var sortedLocalToRootTREntries = new NativeList<int2>(Allocator.Temp);
            RigRemapUtils.ComputeSortedLocalToRootTREntries(
                sortedLocalToRootTREntries,
                translationsRemapEntry,
                rotationsRemapEntry,
                translationOffsets,
                rotationOffsets,
                localToRootTranslationOffsetCount,
                localToRootRotationOffsetCount
            );

            var blobBuilder = new BlobBuilder(Allocator.Temp);

            ref var rigRemapTable = ref blobBuilder.ConstructRoot<RigRemapTable>();

            RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, translationsRemapEntry,
                ref rigRemapTable.TranslationMappings);
            RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, rotationsRemapEntry,
                ref rigRemapTable.RotationMappings);
            RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, scalesRemapEntry,
                ref rigRemapTable.ScaleMappings);
            RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, floatsRemapEntry,
                ref rigRemapTable.FloatMappings);
            RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, intsRemapEntry, ref rigRemapTable.IntMappings);
            RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, sortedLocalToRootTREntries,
                ref rigRemapTable.SortedLocalToRootTREntries);
            RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, translationOffsets,
                ref rigRemapTable.TranslationOffsets);
            RigRemapUtils.FillRigMapperBlobBuffer(ref blobBuilder, rotationOffsets,
                ref rigRemapTable.RotationOffsets);
            rigRemapTable.LocalToParentTRCount = math.int2(
                translationsRemapEntry.Length - localToRootTranslationOffsetCount,
                rotationsRemapEntry.Length - localToRootRotationOffsetCount
            );

            var rigRemapTableAsset = blobBuilder.CreateBlobAssetReference<RigRemapTable>(Allocator.Persistent);

            blobBuilder.Dispose();

            translations.Dispose();
            rotations.Dispose();
            scales.Dispose();
            floats.Dispose();
            ints.Dispose();

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
