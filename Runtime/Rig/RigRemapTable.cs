using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
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
    }

    public struct RigRotationOffset
    {
        public quaternion PreRotation;
        public quaternion PostRotation;
    }

    public struct RigRemapTable
    {
        public BlobArray<RigRemapEntry>           TranslationMappings;
        public BlobArray<RigRemapEntry>           RotationMappings;
        public BlobArray<RigRemapEntry>           ScaleMappings;
        public BlobArray<RigRemapEntry>           FloatMappings;
        public BlobArray<RigRemapEntry>           IntMappings;

        public BlobArray<RigTranslationOffset> TranslationOffsets;
        public BlobArray<RigRotationOffset> RotationOffsets;
    }
}
