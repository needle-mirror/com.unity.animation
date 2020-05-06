using NUnit.Framework;
using Unity.Entities;

namespace Unity.Animation.Tests
{
    // We can't use anymore the default AnimationTestsFixture for performance test because
    // com.unity.entity version 0.0.12-preview.28 changed how to bootstrap a new World
    // and we can't figure out how to make it works again.
    public abstract class AnimationPlayModeTestsFixture : AnimationTestsFixture
    {
    }
}
