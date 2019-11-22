using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Animation.Tests
{
    public class RigEntityBuilderTests : AnimationTestsFixture
    {
        void CheckEntityHasRigComponentTypeAndBufferResized(Entity entity, ComponentType[] rigComponentTypes, int bufferLength)
        {
            foreach (var componentType in rigComponentTypes)
            {
                Assert.IsTrue(m_Manager.HasComponent(entity, componentType));
            }

            var rigBuffer = new RigEntityBuilder.RigBuffers(m_Manager, entity);
            Assert.That(rigBuffer.LocalTranslations.Length, Is.EqualTo(bufferLength));
            Assert.That(rigBuffer.LocalRotations.Length, Is.EqualTo(bufferLength));
            Assert.That(rigBuffer.LocalScales.Length, Is.EqualTo(bufferLength));
            Assert.That(rigBuffer.Masks.Length, Is.EqualTo(bufferLength * 3));  // translation + rotation + scale bindings
            Assert.That(rigBuffer.GlobalMatrices.Length, Is.EqualTo(bufferLength));
        }

        [Test]
        public void CanCreatePrefabEntity()
        {
            var skeletonNodes = new SkeletonNode[] {
                new SkeletonNode {ParentIndex = -1, Id = "Root", AxisIndex = -1}
            };

            var rigDefinition = RigBuilder.CreateRigDefinition(skeletonNodes);

            var prefab = RigEntityBuilder.CreatePrefabEntity(m_Manager, rigDefinition);

            CheckEntityHasRigComponentTypeAndBufferResized(prefab, RigEntityBuilder.RigPrefabComponentTypes, skeletonNodes.Length);
        }

        [Test]
        public void CanInstantiatePrefabEntity()
        {
            var instanceCount = 100;
            var skeletonNodes = new SkeletonNode[] {
                new SkeletonNode {ParentIndex = -1, Id = "Root", AxisIndex = -1},
                new SkeletonNode {ParentIndex = 0, Id = "Hips", AxisIndex = -1},
                new SkeletonNode {ParentIndex = 1, Id = "LeftUpLeg", AxisIndex = -1},
                new SkeletonNode {ParentIndex = 1, Id = "RightUpLeg", AxisIndex = -1}
            };

            var rigDefinition = RigBuilder.CreateRigDefinition(skeletonNodes);

            var prefab = RigEntityBuilder.CreatePrefabEntity(m_Manager, rigDefinition);

            var entities = new NativeArray<Entity>(instanceCount, Allocator.Temp);
            m_Manager.Instantiate(prefab, entities);

            foreach (var entity in entities)
                CheckEntityHasRigComponentTypeAndBufferResized(entity, RigEntityBuilder.RigComponentTypes, skeletonNodes.Length);
        }
    }
}
