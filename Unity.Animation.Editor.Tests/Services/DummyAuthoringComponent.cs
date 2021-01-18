using Unity.Entities;

namespace Unity.Animation.Editor.Tests
{
    [GenerateAuthoringComponent]
    struct DummyAuthoringComponent : IComponentData
    {
#pragma warning disable 649
        public float Field1; // Valid
        float Field2;
        internal int Field3;
        bool Field4;
        public DummyType Field5; // Valid
#pragma warning restore 649
    }

    struct DummyType
    {
        float f;
        int i;
    }
}
