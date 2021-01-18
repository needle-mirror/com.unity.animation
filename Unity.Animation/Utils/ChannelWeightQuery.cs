using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Animation
{
    [BurstCompatible]
    public struct ChannelWeightMap
    {
        public StringHash Id;
        public float Weight;
    }

    public class ChannelWeightQuery
    {
        public ChannelWeightMap[] Channels = Array.Empty<ChannelWeightMap>();

        /// <summary>
        /// Checks that the list does not contain more than one entry that sets the weight of a channel.
        /// </summary>
        /// <param name="channels">The list to validate.</param>
        /// <exception cref="InvalidOperationException">A ChannelWeightQuery cannot have more than one weight for the same channel.</exception>
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateNoDuplicatedBlendChannel(NativeList<ChannelWeightMap> channels)
        {
            for (int i = 0; i != channels.Length; i++)
            {
                for (int j = 0; j != channels.Length; j++)
                {
                    if (i == j)
                        continue;

                    if (channels[i].Id == channels[j].Id)
                        throw new InvalidOperationException($"ChannelWeightQuery cannot have more than one weight for the same channel. '{channels[i].Id}'");
                }
            }
        }

        /// <summary>
        /// Finds which channels correspond to source bindings and add them to the channel weight entries.
        /// </summary>
        /// <param name="channels">A list of weights associated with channel IDs.</param>
        /// <param name="sourceBindings">The bindings of the source.</param>
        /// <param name="offset">Offset of the bindings to add to the channel index.</param>
        /// <param name="entries">This is an in-out parameter. For each channel matching a source binding, a WeightEntry with the index of the channel and its associated weight is appended to this list.</param>
        void AddMatchingChannelWeightsToEntries(NativeList<ChannelWeightMap> channels, ref BlobArray<StringHash> sourceBindings, int offset,
            NativeList<WeightEntry> entries)
        {
            Core.ValidateArgumentIsCreated(channels);
            Core.ValidateArgumentIsCreated(entries);

            for (int i = 0; i != channels.Length; i++)
            {
                var index = Core.FindBindingIndex(ref sourceBindings, channels[i].Id);
                if (index != -1)
                {
                    entries.Add(new WeightEntry { Index = index + offset, Weight = channels[i].Weight });
                }
            }
        }

        /// <summary>
        /// Creates the ChannelWeightTable to map blending weights to a rig.
        /// </summary>
        /// <param name="rigDef">The rig that will be blended.</param>
        /// <returns>A BlobAssetReference of a ChannelWeightTable that contains weights associated with their rig channels' indices.</returns>
        /// <exception cref="ArgumentNullException">The rig should be defined.</exception>
        public BlobAssetReference<ChannelWeightTable> ToChannelWeightTable(BlobAssetReference<RigDefinition> rigDef)
        {
            Core.ValidateArgumentIsCreated(rigDef);

            var channels = new NativeList<ChannelWeightMap>(Allocator.Temp);
            channels.CopyFrom(Channels);
            ValidateNoDuplicatedBlendChannel(channels);

            ref var bindings = ref rigDef.Value.Bindings;
            var entries = new NativeList<WeightEntry>(Allocator.Temp);
            var offset = 0;
            AddMatchingChannelWeightsToEntries(channels, ref bindings.TranslationBindings, offset, entries);
            offset += bindings.TranslationBindings.Length;
            AddMatchingChannelWeightsToEntries(channels, ref bindings.ScaleBindings, offset, entries);
            offset += bindings.ScaleBindings.Length;
            AddMatchingChannelWeightsToEntries(channels, ref bindings.FloatBindings, offset, entries);
            offset += bindings.FloatBindings.Length;
            AddMatchingChannelWeightsToEntries(channels, ref bindings.IntBindings, offset, entries);
            offset += bindings.IntBindings.Length;
            AddMatchingChannelWeightsToEntries(channels, ref bindings.RotationBindings, offset, entries);
            offset += bindings.RotationBindings.Length;

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var table = ref blobBuilder.ConstructRoot<ChannelWeightTable>();
            var builderArray = blobBuilder.Allocate(ref table.Weights, entries.Length);
            for (int i = 0; i < entries.Length; ++i)
                builderArray[i] = entries[i];

            var tableAsset = blobBuilder.CreateBlobAssetReference<ChannelWeightTable>(Allocator.Persistent);

            blobBuilder.Dispose();
            entries.Dispose();
            channels.Dispose();

            return tableAsset;
        }
    }
}
