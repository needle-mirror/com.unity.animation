using NUnit.Framework;
using Unity.Entities;

namespace Unity.Animation.Tests
{
    // We can't use anymore the default AnimationTestsFixture for performance test because
    // com.unity.entity version 0.0.12-preview.28 changed how to bootstrap a new World
    // and we can't figure out how to make it works again.
    public abstract class AnimationPlayModeTestsFixture : AnimationTestsFixture
    {
        [SetUp]
        protected override void SetUp()
        {
            World = World.Active;

            m_Manager = World.EntityManager;
            m_ManagerDebug = new EntityManager.EntityManagerDebug(m_Manager);

            m_AnimationGraphSystem = World.GetOrCreateSystem<AnimationGraphSystem>();
            m_AnimationGraphSystem.AddRef();
        }

        [TearDown]
        protected override void TearDown()
        {
            DestroyNodesAndGraphBuffers();
            m_AnimationGraphSystem.RemoveRef();
        }
    }
}
