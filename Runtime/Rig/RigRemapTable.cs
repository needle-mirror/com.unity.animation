using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    // Remapping in LocalToRoot space has a performance cost,
    // use it with caution
    public enum RigRemapSpace : byte
    {
        LocalToParent = 0,
        LocalToRoot   = 1
    }

    public struct RigRemapEntry
    {
        public int SourceIndex;
        public int DestinationIndex;
        public int OffsetIndex;
    }

    public struct RigTranslationOffset
    {
        public float Scale;
        public quaternion Rotation;
        public RigRemapSpace Space;
    }

    public struct RigRotationOffset
    {
        public quaternion PreRotation;
        public quaternion PostRotation;
        public RigRemapSpace Space;
    }

    public struct RigRemapTable
    {
        public BlobArray<RigRemapEntry>        TranslationMappings;
        public BlobArray<RigRemapEntry>        RotationMappings;
        public BlobArray<RigRemapEntry>        ScaleMappings;
        public BlobArray<RigRemapEntry>        FloatMappings;
        public BlobArray<RigRemapEntry>        IntMappings;

        public BlobArray<int2>                 SortedLocalToRootTREntries; // x = TranslationMappingIndex, y = RotationMappingIndex
        public int2                            LocalToParentTRCount;       // x = TranslationCount, y = RotationCount

        public BlobArray<RigTranslationOffset> TranslationOffsets;
        public BlobArray<RigRotationOffset>    RotationOffsets;
    }
}
