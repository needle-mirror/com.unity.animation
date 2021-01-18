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
            Component.Version = 0;

            Component.UpgradeWhenNecessary();

            Assert.AreEqual(RigComponent.LatestVersion, Component.Version);
        }

        [Test]
        public void RigComponent_WhenInvalidBonesSerialized_UpgradeWhenNecessary_RemovesInvalidBones()
        {
            var boneNotInHierarchy  = CreateBoneNotInHierarchy();
            var boneInHierarchy     = CreateBoneInHierarchy(Component);
            var destroyedBone       = CreateDestroyedBone();
            Component.Version  = 0;
            Component.Bones = new[]
            {
                null,               // not a bone
                boneInHierarchy,    // part of the hierarchy, valid
                boneNotInHierarchy, // not part of the hierarchy, invalid
                destroyedBone       // destroyed, invalid
            };

            Component.UpgradeWhenNecessary();

            // No bone got added to the excludeBones list
            Assert.That(Component.ExcludeBones, Is.Empty);
            // Check if all bones, except null and boneInHierarchy, have been added to invalidBones
            // Note that a destroyed transform is still added b/c it might become valid
            // again if a change is reverted. This allows the user to still find the bone
            // in the RigComponent invalid bones list in the inspector.
            Assert.IsTrue(Component.InvalidBones.Contains(boneNotInHierarchy));
            Assert.IsTrue(Component.InvalidBones.Contains(destroyedBone));
            Assert.IsFalse(Component.InvalidBones.Contains(boneInHierarchy));
            Assert.IsFalse(Component.InvalidBones.Contains(null));
            // Check if all bones, except boneInHierarchy, have been removed from Bones
            Assert.That(Component.Bones, Is.EquivalentTo(new[] { boneInHierarchy }));
        }

        [Test]
        public void RigComponent_WithChildBoneNotInChildListAndUpgrade_IsPartOfExcludeAndNotBones()
        {
            var boneInHierarchy = CreateBoneInHierarchy(Component);
            var boneInHierarchyButNotPartOfBonesList = CreateBoneInHierarchy(Component);
            boneInHierarchyButNotPartOfBonesList.transform.parent = boneInHierarchy.transform;
            Component.Version = 0;
            Component.Bones = new[] { boneInHierarchy };

            Component.UpgradeWhenNecessary();

            // neither bone is part of invalid bones
            Assume.That(Component.InvalidBones, Is.Empty);
            // only boneInHierarchyButNotPartOfBonesList is part of excludedBones
            Assert.That(Component.ExcludeBones, Is.EquivalentTo(new[] { boneInHierarchyButNotPartOfBonesList }));
            // only boneInHierarchy is part of bones
            Assert.That(Component.Bones, Is.EquivalentTo(new[] { boneInHierarchy }));
        }
    }
}
