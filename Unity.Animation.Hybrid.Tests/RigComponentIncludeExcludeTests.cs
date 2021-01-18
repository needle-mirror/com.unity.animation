using NUnit.Framework;
using UnityEngine;
using Unity.Entities;
using Unity.Animation.Hybrid;

namespace Unity.Animation.Tests
{
    class RigComponentIncludeExcludeTests : RigComponentTestsFixture
    {
        [Test]
        public void RigComponent_ExcludeBoneAndDescendants_ThrowsExceptionsOnInvalidInput()
        {
            var boneNotInHierarchy  = CreateBoneNotInHierarchy();
            var destroyedBone       = CreateDestroyedBone();

            Assert.Throws<System.ArgumentNullException>(() => Component.ExcludeBoneAndDescendants(null));
            Assert.Throws<System.ArgumentNullException>(() => Component.ExcludeBoneAndDescendants(destroyedBone));
            Assert.Throws<System.ArgumentException>(() => Component.ExcludeBoneAndDescendants(boneNotInHierarchy));
        }

        [Test]
        public void RigComponent_ExcludeBoneAndDescendants_DoesNotThrowExceptionsOnValidInput()
        {
            var boneInHierarchy = CreateBoneInHierarchy(Component);

            Assert.DoesNotThrow(() => Component.ExcludeBoneAndDescendants(boneInHierarchy));
        }

        [Test]
        public void RigComponent_IncludeBoneAndAncestors_ThrowsExceptionsOnInvalidInput()
        {
            var boneNotInHierarchy  = CreateBoneNotInHierarchy();
            var destroyedBone       = CreateDestroyedBone();

            Assert.Throws<System.ArgumentNullException>(() => Component.IncludeBoneAndAncestors(null));
            Assert.Throws<System.ArgumentNullException>(() => Component.IncludeBoneAndAncestors(destroyedBone));
            Assert.Throws<System.ArgumentException>(() => Component.IncludeBoneAndAncestors(boneNotInHierarchy));
        }

        [Test]
        public void RigComponent_IncludeBoneAndAncestors_DoesNotThrowExceptionsOnValidInput()
        {
            var boneInHierarchy = CreateBoneInHierarchy(Component);

            Assert.DoesNotThrow(() => Component.IncludeBoneAndAncestors(boneInHierarchy));
        }

        [Test]
        public void RigComponent_IncludeAndDestroyBone_BoneIsNotIncluded()
        {
            var boneInHierarchy = CreateBoneInHierarchy(Component);

            Component.IncludeBoneAndAncestors(boneInHierarchy);
            Destroy(boneInHierarchy);

            Assert.IsFalse(Component.IsBoneIncluded(boneInHierarchy));
        }

        [Test]
        public void RigComponent_IncludeBone_BoneIsIncluded()
        {
            var boneInHierarchy = CreateBoneInHierarchy(Component);

            Component.IncludeBoneAndAncestors(boneInHierarchy);

            Assert.IsTrue(Component.IsBoneIncluded(boneInHierarchy));
        }

        [Test]
        public void RigComponent_AddBoneToHierarchy_NotIncludedByDefault()
        {
            var boneInHierarchy = CreateBoneInHierarchy(Component);

            Assert.IsFalse(Component.IsBoneIncluded(boneInHierarchy));
        }

        // TODO: turn into edit-mode test
        [Test]
        public void RigComponent_PreExistingChild_BoneIsIncludedByDefault()
        {
            Component.Reset();
            Assert.IsTrue(Component.IsBoneIncluded(PreExistingChildBone));
        }

        // TODO: turn into edit-mode test
        [Test]
        public void RigComponent_PreExistingChild_WhenExcluded_BoneIsExcluded()
        {
            Component.Reset();
            Assume.That(Component.IsBoneIncluded(PreExistingChildBone), Is.True);
            Component.ExcludeBoneAndDescendants(PreExistingChildBone);

            Assert.IsFalse(Component.IsBoneIncluded(PreExistingChildBone));
        }

        [Test]
        public void RigComponent_IncludeGrandchild_ChildIsAlsoIncluded()
        {
            var boneInHierarchy1 = CreateBoneInHierarchy(Component);
            var boneInHierarchy2 = CreateBoneInHierarchy(boneInHierarchy1);
            var boneInHierarchy3 = CreateBoneInHierarchy(boneInHierarchy2);

            Component.IncludeBoneAndAncestors(boneInHierarchy3);

            Assert.IsTrue(Component.IsBoneIncluded(boneInHierarchy3));
            Assert.IsTrue(Component.IsBoneIncluded(boneInHierarchy2));
            Assert.IsTrue(Component.IsBoneIncluded(boneInHierarchy1));
        }

        [Test]
        public void RigComponent_ExcludeAllChildrenSetChildAsRootAndInclude_ChildIsIncluded()
        {
            var boneInHierarchy1 = CreateBoneInHierarchy(Component);
            var boneInHierarchy2 = CreateBoneInHierarchy(boneInHierarchy1);
            var boneInHierarchy3 = CreateBoneInHierarchy(boneInHierarchy2);

            Component.ExcludeBoneAndDescendants(boneInHierarchy1);
            Component.SkeletonRootBone = boneInHierarchy2;
            Component.IncludeBoneAndDescendants(boneInHierarchy2);

            Assert.IsTrue(Component.IsBoneIncluded(boneInHierarchy3));
            Assert.IsTrue(Component.IsBoneIncluded(boneInHierarchy2));
            Assert.IsFalse(Component.IsBoneIncluded(boneInHierarchy1));
        }

        [Test]
        public void RigComponent_WhenGrandchildIsIncluded_ExcludeChild_GrandchildIsAlsoExcluded()
        {
            var boneInHierarchy1 = CreateBoneInHierarchy(Component);
            var boneInHierarchy2 = CreateBoneInHierarchy(boneInHierarchy1);
            var boneInHierarchy3 = CreateBoneInHierarchy(boneInHierarchy2);

            Component.IncludeBoneAndAncestors(boneInHierarchy3);
            Component.ExcludeBoneAndDescendants(boneInHierarchy2);

            Assert.IsFalse(Component.IsBoneIncluded(boneInHierarchy3));
            Assert.IsFalse(Component.IsBoneIncluded(boneInHierarchy2));
            Assert.IsTrue(Component.IsBoneIncluded(boneInHierarchy1));
        }

        [Test]
        public void RigComponent_IncludeAndExcludeBone_BoneIsExcluded()
        {
            var boneInHierarchy = CreateBoneInHierarchy(Component);

            Component.IncludeBoneAndAncestors(boneInHierarchy);
            Component.ExcludeBoneAndDescendants(boneInHierarchy);

            Assert.IsFalse(Component.IsBoneIncluded(boneInHierarchy));
        }

        [Test]
        public void RigComponent_ExcludeBone_BoneIsExcluded()
        {
            var boneInHierarchy = CreateBoneInHierarchy(Component);

            Component.ExcludeBoneAndDescendants(boneInHierarchy);

            Assert.IsFalse(Component.IsBoneIncluded(boneInHierarchy));
        }
    }
}
