using System;
using System.Collections.Generic;

using Unity.Collections;
using Unity.Entities;

namespace Unity.Animation
{
    public struct ChannelMap
    {
        public StringHash SourceId;
        public StringHash DestinationId;
        // index 0 (default) means no or identity offset
        public int OffsetIndex;
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

        List<ChannelMap> GetChannelsMap(ChannelMap[] allChannels, ChannelMap[] typedChannels)
        {
            var channels = new List<ChannelMap>(allChannels);
            channels.AddRange(typedChannels);

            return channels;
        }

        void ValidateNoDuplicatedDestinationChannel(List<ChannelMap> channels)
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

        void ValidateOffsetIndex(List<ChannelMap> channels, int offsetCount)
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

        RigRemapEntry[] GetMatchingRemapEntries(List<ChannelMap> channels, ref BlobArray<StringHash> sourceBindings, ref BlobArray<StringHash> destinationBindings, int offsetCount = 0)
        {
            var rigRemapEntry = new RigRemapEntry[channels.Count];
            int count = 0;
            for (int i = 0; i != channels.Count; i++)
            {
                var sourceIndex = Core.FindBindingIndex(ref sourceBindings, channels[i].SourceId);
                var destinationIndex = Core.FindBindingIndex(ref destinationBindings, channels[i].DestinationId);
                if(sourceIndex != -1 && destinationIndex != -1 && (channels[i].OffsetIndex == 0 || channels[i].OffsetIndex < offsetCount))
                {
                    rigRemapEntry[count++] = new RigRemapEntry { SourceIndex = sourceIndex, DestinationIndex = destinationIndex, OffsetIndex = channels[i].OffsetIndex };
                }
            }

            Array.Resize(ref rigRemapEntry, count);

            return rigRemapEntry;
        }

        public BlobAssetReference<RigRemapTable> ToRigRemapTable(BlobAssetReference<RigDefinition> sourceRigDefinition, BlobAssetReference<RigDefinition> destinationRigDefinition)
        {
            if (sourceRigDefinition == default)
                throw new ArgumentNullException("sourceRigDefinition");

            if (destinationRigDefinition == default)
                throw new ArgumentNullException("destinationRigDefinition");

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

            var blobBuilder = new BlobBuilder(Allocator.Temp);

            ref var rigRemapTable = ref blobBuilder.ConstructRoot<RigRemapTable>();

            FillRigMapperBlobBuffer(ref blobBuilder, translationsRemapEntry, ref rigRemapTable.TranslationMappings);
            FillRigMapperBlobBuffer(ref blobBuilder, rotationsRemapEntry, ref rigRemapTable.RotationMappings);
            FillRigMapperBlobBuffer(ref blobBuilder, scalesRemapEntry, ref rigRemapTable.ScaleMappings);
            FillRigMapperBlobBuffer(ref blobBuilder, floatsRemapEntry, ref rigRemapTable.FloatMappings);
            FillRigMapperBlobBuffer(ref blobBuilder, intsRemapEntry, ref rigRemapTable.IntMappings);
            FillRigMapperBlobBuffer(ref blobBuilder, TranslationOffsets, ref rigRemapTable.TranslationOffsets);
            FillRigMapperBlobBuffer(ref blobBuilder, RotationOffsets, ref rigRemapTable.RotationOffsets);

            var rigRemapTableAsset = blobBuilder.CreateBlobAssetReference<RigRemapTable>(Allocator.Persistent);

            blobBuilder.Dispose();

            return rigRemapTableAsset;
        }

        private static void FillRigMapperBlobBuffer<T>(ref BlobBuilder blobBuilder, T[] data, ref BlobArray<T> blobBuffer)
            where T : struct
        {
            if (data == null || data.Length == 0)
                return;

            var builderArray = blobBuilder.Allocate(ref blobBuffer, data.Length);
            for (int i = 0; i < data.Length; ++i)
                builderArray[i] = data[i];
        }
    }
}
