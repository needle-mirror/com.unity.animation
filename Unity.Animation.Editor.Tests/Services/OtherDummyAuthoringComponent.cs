using Unity.Entities;

namespace Unity.Animation.Editor.Tests
{
    [GenerateAuthoringComponent]
    struct OtherDummyAuthoringComponent : IComponentData
    {
#pragma warning disable 649
        public float Field;
#pragma warning restore 649
    }
}
