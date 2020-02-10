using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Animation
{
    public struct ChannelWeightMap
    {
        public StringHash Id;
        public float Weight;
    }

    public class ChannelWeightQuery
    {
        public ChannelWeightMap[] Channels = Array.Empty<ChannelWeightMap>();

        void ValidateNoDuplicatedBlendChannel(List<ChannelWeightMap> channels)
        {
            for(int i=0;i!=channels.Count;i++)
            {
                for(int j=0;j!=channels.Count;j++)
                {
                    if (i == j)
                        continue;

                    if (channels[i].Id == channels[j].Id)
                        throw new InvalidOperationException($"ChannelWeightQuery cannot have more than one weight for the same channel. '{channels[i].Id}'");
                }
            }
        }

        List<WeightEntry> GetMatchingChannelWeightEntries(List<ChannelWeightMap> channels, ref BlobArray<StringHash> sourceBindings, int offset)
        {
            var entries = new List<WeightEntry>();
            for (int i = 0; i != channels.Count; i++)
            {
                var index = Core.FindBindingIndex(ref sourceBindings, channels[i].Id);
                if (index != -1)
                {
                    entries.Add(new WeightEntry { Index = index + offset, Weight = channels[i].Weight });
                }
            }

            return entries;
        }

         public BlobAssetReference<ChannelWeightTable> ToChannelWeightTable(BlobAssetReference<RigDefinition> rigDef)
         {
            if (rigDef == default)
                throw new ArgumentNullException("rigDef");

            var channels = new List<ChannelWeightMap>(Channels);
            ValidateNoDuplicatedBlendChannel(channels);

            ref var bindings = ref rigDef.Value.Bindings;
            var entries = new List<WeightEntry>();
            var offset = 0;
            entries.AddRange(GetMatchingChannelWeightEntries(channels, ref bindings.TranslationBindings, offset));
            offset += bindings.TranslationBindings.Length;
            entries.AddRange(GetMatchingChannelWeightEntries(channels, ref bindings.ScaleBindings, offset));
            offset += bindings.ScaleBindings.Length;
            entries.AddRange(GetMatchingChannelWeightEntries(channels, ref bindings.FloatBindings, offset));
            offset += bindings.FloatBindings.Length;
            entries.AddRange(GetMatchingChannelWeightEntries(channels, ref bindings.IntBindings, offset));
            offset += bindings.IntBindings.Length;
            entries.AddRange(GetMatchingChannelWeightEntries(channels, ref bindings.RotationBindings, offset));
            offset += bindings.RotationBindings.Length;

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var table = ref blobBuilder.ConstructRoot<ChannelWeightTable>();
            var builderArray = blobBuilder.Allocate(ref table.Weights, entries.Count);
            for (int i = 0; i < entries.Count; ++i)
                builderArray[i] = entries[i];

            var tableAsset = blobBuilder.CreateBlobAssetReference<ChannelWeightTable>(Allocator.Persistent);

            blobBuilder.Dispose();

            return tableAsset;
         }
    }
}
