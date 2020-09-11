using System;
using NUnit.Framework;

namespace Unity.Animation.Tests
{
    public class RigBuilderTests
    {
        [Test]
        public void CanCreateRigWithoutAnimationChannel()
        {
            var skeletonNodes = new SkeletonNode[]
            {
                new SkeletonNode {ParentIndex = -1, Id = "Root", AxisIndex = -1}
            };

            var rig = RigBuilder.CreateRigDefinition(skeletonNodes);

            Assert.That(rig.Value.Skeleton.BoneCount, Is.EqualTo(1));
            Assert.That(rig.Value.Bindings.TranslationBindings.Length, Is.EqualTo(1));
            Assert.That(rig.Value.Bindings.RotationBindings.Length, Is.EqualTo(1));
            Assert.That(rig.Value.Bindings.ScaleBindings.Length, Is.EqualTo(1));
        }

        [Test]
        public void CanCreateRigWithoutSkeleton()
        {
            var animationChannel = new IAnimationChannel[]
            {
                new LocalTranslationChannel {Id = "Root"},
                new LocalRotationChannel {Id = "Root"},
                new LocalScaleChannel {Id = "Root"},
            };

            var rig = RigBuilder.CreateRigDefinition(animationChannel);

            Assert.That(rig.Value.Skeleton.BoneCount, Is.EqualTo(0));
            Assert.That(rig.Value.Bindings.TranslationBindings.Length, Is.EqualTo(1));
            Assert.That(rig.Value.Bindings.RotationBindings.Length, Is.EqualTo(1));
            Assert.That(rig.Value.Bindings.ScaleBindings.Length, Is.EqualTo(1));
            Assert.That(rig.Value.Bindings.FloatBindings.Length, Is.EqualTo(0));
            Assert.That(rig.Value.Bindings.IntBindings.Length, Is.EqualTo(0));
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [Test]
        public void CannotCreateRigSkeletonWithoutRoot()
        {
            var animationChannel = new IAnimationChannel[]
            {
                new LocalTranslationChannel {Id = "Root"},
                new LocalRotationChannel {Id = "Root"},
                new LocalScaleChannel {Id = "Root"},
            };

            var skeletonNodes = new SkeletonNode[]
            {
                new SkeletonNode()
            };

            Assert.Throws<ArgumentException>(() => RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannel));
        }

        [Test]
        public void CannotCreateRigSkeletonWithMoreThanOneRoot()
        {
            var animationChannel = new IAnimationChannel[]
            {
                new LocalTranslationChannel {Id = "Root"},
                new LocalRotationChannel {Id = "Root"},
                new LocalScaleChannel {Id = "Root"},
            };

            var skeletonNodes = new SkeletonNode[]
            {
                new SkeletonNode {ParentIndex = -1},
                new SkeletonNode {ParentIndex = 0},
                new SkeletonNode {ParentIndex = -1},
            };

            Assert.Throws<ArgumentException>(() => RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannel));
        }

        [Test]
        public void CannotCreateRigSkeletonWithInvalidParentIndexes()
        {
            var animationChannel = new IAnimationChannel[]
            {
                new LocalTranslationChannel {Id = "Root"},
                new LocalRotationChannel {Id = "Root"},
                new LocalScaleChannel {Id = "Root"},
            };

            var skeletonNodes = new SkeletonNode[]
            {
                new SkeletonNode {ParentIndex = -1},
                new SkeletonNode {ParentIndex = 0},
                new SkeletonNode {ParentIndex = 100},
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannel));


            skeletonNodes = new SkeletonNode[]
            {
                new SkeletonNode {ParentIndex = -1},
                new SkeletonNode {ParentIndex = 0},
                new SkeletonNode {ParentIndex = -100},
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannel));
        }

        [Test]
        public void CannotCreateRigSkeletonWhenSkeletonNodeIsItsOwnParent()
        {
            var animationChannel = new IAnimationChannel[]
            {
                new LocalTranslationChannel {Id = "Root"},
                new LocalRotationChannel {Id = "Root"},
                new LocalScaleChannel {Id = "Root"},
            };

            var skeletonNodes = new SkeletonNode[]
            {
                new SkeletonNode {ParentIndex = -1},
                new SkeletonNode {ParentIndex = 1},
            };

            Assert.Throws<ArgumentException>(() => RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannel));
        }

        [Test]
        public void CannotCreateRigSkeletonWhenAxisIndexIsValidAndAxisIsNull()
        {
            var animationChannel = new IAnimationChannel[]
            {
                new LocalTranslationChannel {Id = "Root"},
                new LocalRotationChannel {Id = "Root"},
                new LocalScaleChannel {Id = "Root"},
            };

            var skeletonNodes = new SkeletonNode[]
            {
                new SkeletonNode {ParentIndex = -1, AxisIndex = 0},
                new SkeletonNode {ParentIndex = 0, AxisIndex = 1},
                new SkeletonNode {ParentIndex = 1, AxisIndex = 2},
            };

            Assert.Throws<ArgumentNullException>(() => RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannel));
        }

        [Test]
        public void CannotCreateRigSkeletonWhenAxisIndexIsInvalid()
        {
            var animationChannel = new IAnimationChannel[]
            {
                new LocalTranslationChannel {Id = "Root"},
                new LocalRotationChannel {Id = "Root"},
                new LocalScaleChannel {Id = "Root"},
            };

            var skeletonNodes = new SkeletonNode[]
            {
                new SkeletonNode {ParentIndex = -1, AxisIndex = 0},
                new SkeletonNode {ParentIndex = 0, AxisIndex = 1},
                new SkeletonNode {ParentIndex = 1, AxisIndex = 100},
            };

            var axis = new Axis[]
            {
                new Axis(),
                new Axis()
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => RigBuilder.CreateRigDefinition(skeletonNodes, axis, animationChannel));

            skeletonNodes = new SkeletonNode[]
            {
                new SkeletonNode {ParentIndex = -1, AxisIndex = 0},
                new SkeletonNode {ParentIndex = 0, AxisIndex = 1},
                new SkeletonNode {ParentIndex = 1, AxisIndex = -100},
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => RigBuilder.CreateRigDefinition(skeletonNodes, axis, animationChannel));
        }

#endif
        [Test]
        public void CanRemoveDuplicateChannelFromRigDefinition()
        {
            var animationChannel = new IAnimationChannel[]
            {
                new LocalTranslationChannel {Id = "Root"},
                new LocalTranslationChannel {Id = "Root"},
                new LocalTranslationChannel {Id = "Root"},
                new LocalRotationChannel {Id = "Root"},
                new LocalScaleChannel {Id = "Root"},
                new LocalRotationChannel {Id = "Root"},
                new LocalTranslationChannel {Id = "Root"}
            };

            var skeletonNodes = new SkeletonNode[]
            {
                new SkeletonNode {ParentIndex = -1, AxisIndex = -1, Id = "Root"}
            };

            var rig = RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannel);

            Assert.That(rig.Value.Bindings.TranslationBindings.Length, Is.EqualTo(1));
            Assert.That(rig.Value.Bindings.RotationBindings.Length, Is.EqualTo(1));
            Assert.That(rig.Value.Bindings.ScaleBindings.Length, Is.EqualTo(1));
        }

        [Test]
        public void RigDefinitionWithSameDataHaveSameHashCode()
        {
            var animationChannel = new IAnimationChannel[]
            {
                new LocalTranslationChannel {Id = "Root"},
                new LocalTranslationChannel {Id = "Root"},
                new LocalTranslationChannel {Id = "Root"},
                new LocalRotationChannel {Id = "Root"},
                new LocalScaleChannel {Id = "Root"},
                new LocalRotationChannel {Id = "Root"},
                new LocalTranslationChannel {Id = "Root"}
            };

            var skeletonNodes = new SkeletonNode[]
            {
                new SkeletonNode {ParentIndex = -1, AxisIndex = -1, Id = "Root"}
            };

            var rig1 = RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannel);

            var rig2 = RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannel);

            Assert.That(rig1.Value.GetHashCode(), Is.EqualTo(rig2.Value.GetHashCode()));
        }

        [Test]
        public void RigDefinitionWithDifferentDataHaveDifferentHashCode()
        {
            var animationChannel = new IAnimationChannel[]
            {
                new LocalTranslationChannel {Id = "Root"},
                new LocalTranslationChannel {Id = "Root"},
                new LocalTranslationChannel {Id = "Root"},
                new LocalRotationChannel {Id = "Root"},
                new LocalScaleChannel {Id = "Root"},
                new LocalRotationChannel {Id = "Root"},
                new LocalTranslationChannel {Id = "Root"}
            };

            var skeletonNodes1 = new SkeletonNode[]
            {
                new SkeletonNode {ParentIndex = -1, AxisIndex = -1, Id = "Root"}
            };

            var rig1 = RigBuilder.CreateRigDefinition(skeletonNodes1, null, animationChannel);

            var skeletonNodes2 = new SkeletonNode[]
            {
                new SkeletonNode {ParentIndex = -1, AxisIndex = -1, Id = "Root1"}
            };
            var rig2 = RigBuilder.CreateRigDefinition(skeletonNodes2, null, animationChannel);

            Assert.That(rig1.Value.GetHashCode(), Is.Not.EqualTo(rig2.Value.GetHashCode()));
        }
    }
}
