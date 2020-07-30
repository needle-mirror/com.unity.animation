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

            Assert.Throws<System.ArgumentNullException>(() => rigComponent.ExcludeBoneAndDescendants(null));
            Assert.Throws<System.ArgumentNullException>(() => rigComponent.ExcludeBoneAndDescendants(destroyedBone));
            Assert.Throws<System.ArgumentException>(() => rigComponent.ExcludeBoneAndDescendants(boneNotInHierarchy));
        }

        [Test]
        public void RigComponent_ExcludeBoneAndDescendants_DoesNotThrowExceptionsOnValidInput()
        {
            var boneInHierarchy = CreateBoneInHierarchy(rigComponent);

            Assert.DoesNotThrow(() => rigComponent.ExcludeBoneAndDescendants(boneInHierarchy));
        }

        [Test]
        public void RigComponent_IncludeBoneAndAncestors_ThrowsExceptionsOnInvalidInput()
        {
            var boneNotInHierarchy  = CreateBoneNotInHierarchy();
            var destroyedBone       = CreateDestroyedBone();

            Assert.Throws<System.ArgumentNullException>(() => rigComponent.IncludeBoneAndAncestors(null));
            Assert.Throws<System.ArgumentNullException>(() => rigComponent.IncludeBoneAndAncestors(destroyedBone));
            Assert.Throws<System.ArgumentException>(() => rigComponent.IncludeBoneAndAncestors(boneNotInHierarchy));
        }

        [Test]
        public void RigComponent_IncludeBoneAndAncestors_DoesNotThrowExceptionsOnValidInput()
        {
            var boneInHierarchy = CreateBoneInHierarchy(rigComponent);

            Assert.DoesNotThrow(() => rigComponent.IncludeBoneAndAncestors(boneInHierarchy));
        }

        [Test]
        public void RigComponent_IncludeAndDestroyBone_BoneIsNotIncluded()
        {
            var boneInHierarchy = CreateBoneInHierarchy(rigComponent);

            rigComponent.IncludeBoneAndAncestors(boneInHierarchy);
            Destroy(boneInHierarchy);

            Assert.IsFalse(rigComponent.IsBoneIncluded(boneInHierarchy));
        }

        [Test]
        public void RigComponent_IncludeBone_BoneIsIncluded()
        {
            var boneInHierarchy = CreateBoneInHierarchy(rigComponent);

            rigComponent.IncludeBoneAndAncestors(boneInHierarchy);

            Assert.IsTrue(rigComponent.IsBoneIncluded(boneInHierarchy));
        }

        [Test]
        public void RigComponent_AddBoneToHierarchy_NotIncludedByDefault()
        {
            var boneInHierarchy = CreateBoneInHierarchy(rigComponent);

            Assert.IsFalse(rigComponent.IsBoneIncluded(boneInHierarchy));
        }

        // TODO: turn into edit-mode test
        [Test]
        public void RigComponent_PreExistingChild_BoneIsIncludedByDefault()
        {
            rigComponent.Reset();
            Assert.IsTrue(rigComponent.IsBoneIncluded(preExistingChildBone));
        }

        // TODO: turn into edit-mode test
        [Test]
        public void RigComponent_PreExistingChild_WhenExcluded_BoneIsExcluded()
        {
            rigComponent.Reset();
            Assume.That(rigComponent.IsBoneIncluded(preExistingChildBone), Is.True);
            rigComponent.ExcludeBoneAndDescendants(preExistingChildBone);

            Assert.IsFalse(rigComponent.IsBoneIncluded(preExistingChildBone));
        }

        [Test]
        public void RigComponent_IncludeGrandchild_ChildIsAlsoIncluded()
        {
            var boneInHierarchy1 = CreateBoneInHierarchy(rigComponent);
            var boneInHierarchy2 = CreateBoneInHierarchy(boneInHierarchy1);
            var boneInHierarchy3 = CreateBoneInHierarchy(boneInHierarchy2);

            rigComponent.IncludeBoneAndAncestors(boneInHierarchy3);

            Assert.IsTrue(rigComponent.IsBoneIncluded(boneInHierarchy3));
            Assert.IsTrue(rigComponent.IsBoneIncluded(boneInHierarchy2));
            Assert.IsTrue(rigComponent.IsBoneIncluded(boneInHierarchy1));
        }

        [Test]
        public void RigComponent_WhenGrandchildIsIncluded_ExcludeChild_GrandchildIsAlsoExcluded()
        {
            var boneInHierarchy1 = CreateBoneInHierarchy(rigComponent);
            var boneInHierarchy2 = CreateBoneInHierarchy(boneInHierarchy1);
            var boneInHierarchy3 = CreateBoneInHierarchy(boneInHierarchy2);

            rigComponent.IncludeBoneAndAncestors(boneInHierarchy3);
            rigComponent.ExcludeBoneAndDescendants(boneInHierarchy2);

            Assert.IsFalse(rigComponent.IsBoneIncluded(boneInHierarchy3));
            Assert.IsFalse(rigComponent.IsBoneIncluded(boneInHierarchy2));
            Assert.IsTrue(rigComponent.IsBoneIncluded(boneInHierarchy1));
        }

        [Test]
        public void RigComponent_IncludeAndExcludeBone_BoneIsExcluded()
        {
            var boneInHierarchy = CreateBoneInHierarchy(rigComponent);

            rigComponent.IncludeBoneAndAncestors(boneInHierarchy);
            rigComponent.ExcludeBoneAndDescendants(boneInHierarchy);

            Assert.IsFalse(rigComponent.IsBoneIncluded(boneInHierarchy));
        }

        [Test]
        public void RigComponent_ExcludeBone_BoneIsExcluded()
        {
            var boneInHierarchy = CreateBoneInHierarchy(rigComponent);

            rigComponent.ExcludeBoneAndDescendants(boneInHierarchy);

            Assert.IsFalse(rigComponent.IsBoneIncluded(boneInHierarchy));
        }
    }
}
