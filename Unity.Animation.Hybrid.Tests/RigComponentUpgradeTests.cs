using NUnit.Framework;
using UnityEngine;
using Unity.Animation.Hybrid;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Unity.Animation.Tests
{
    class RigComponentUpgradeTests : RigComponentTestsFixture
    {
        [Test]
        public void RigComponent_Upgrade_VersionIsUpdated()
        {
            rigComponent.Version = 0;

            rigComponent.UpgradeWhenNecessary();

            Assert.AreEqual(RigComponent.LatestVersion, rigComponent.Version);
        }

        [Test]
        public void RigComponent_WhenInvalidBonesSerialized_UpgradeWhenNecessary_RemovesInvalidBones()
        {
            var boneNotInHierarchy  = CreateBoneNotInHierarchy();
            var boneInHierarchy     = CreateBoneInHierarchy(rigComponent);
            var destroyedBone       = CreateDestroyedBone();
            rigComponent.Version  = 0;
            rigComponent.Bones = new[]
            {
                null,               // not a bone
                boneInHierarchy,    // part of the hierarchy, valid
                boneNotInHierarchy, // not part of the hierarchy, invalid
                destroyedBone       // destroyed, invalid
            };

            rigComponent.UpgradeWhenNecessary();

            // No bone got added to the excludeBones list
            Assert.That(rigComponent.ExcludeBones, Is.Empty);
            // Check if all bones, except null and boneInHierarchy, have been added to invalidBones
            // Note that a destroyed transform is still added b/c it might become valid
            // again if a change is reverted. This allows the user to still find the bone
            // in the RigComponent invalid bones list in the inspector.
            Assert.IsTrue(rigComponent.InvalidBones.Contains(boneNotInHierarchy));
            Assert.IsTrue(rigComponent.InvalidBones.Contains(destroyedBone));
            Assert.IsFalse(rigComponent.InvalidBones.Contains(boneInHierarchy));
            Assert.IsFalse(rigComponent.InvalidBones.Contains(null));
            // Check if all bones, except boneInHierarchy, have been removed from Bones
            Assert.That(rigComponent.Bones, Is.EquivalentTo(new[] { boneInHierarchy }));
        }

        [Test]
        public void RigComponent_WithChildBoneNotInChildListAndUpgrade_IsPartOfExcludeAndNotBones()
        {
            var boneInHierarchy = CreateBoneInHierarchy(rigComponent);
            var boneInHierarchyButNotPartOfBonesList = CreateBoneInHierarchy(rigComponent);
            boneInHierarchyButNotPartOfBonesList.transform.parent = boneInHierarchy.transform;
            rigComponent.Version = 0;
            rigComponent.Bones = new[] { boneInHierarchy };

            rigComponent.UpgradeWhenNecessary();

            // neither bone is part of invalid bones
            Assume.That(rigComponent.InvalidBones, Is.Empty);
            // only boneInHierarchyButNotPartOfBonesList is part of excludedBones
            Assert.That(rigComponent.ExcludeBones, Is.EquivalentTo(new[] { boneInHierarchyButNotPartOfBonesList }));
            // only boneInHierarchy is part of bones
            Assert.That(rigComponent.Bones, Is.EquivalentTo(new[] { boneInHierarchy }));
        }
    }
}
