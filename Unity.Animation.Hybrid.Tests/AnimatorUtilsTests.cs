using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Animation.Hybrid;
using UnityEngine;

namespace Unity.Animation.Tests
{
    public class AnimatorUtilsTests
    {
        private GameObject[] CreateTestHirearchy()
        {
            var objects = new GameObject[]
            {
                new GameObject("root"),
                new GameObject("object"),
                new GameObject("child"),
                new GameObject("sibling"),
                new GameObject("grandchild"),
            };

            objects[1].transform.parent = objects[0].transform;
            objects[2].transform.parent = objects[1].transform;
            objects[3].transform.parent = objects[0].transform;
            objects[4].transform.parent = objects[2].transform;

            return objects;
        }

        [Test]
        public void FindDescendant()
        {
            var objects = CreateTestHirearchy();

            var root = objects[0].transform;
            Assert.IsNull(AnimatorUtils.FindDescendant(root, "root"));
            Assert.IsNull(AnimatorUtils.FindDescendant(root, "a non exsisting name"));
            for (int i = 1; i < objects.Length; ++i)
            {
                Assert.AreEqual(objects[i].transform, AnimatorUtils.FindDescendant(root, objects[i].name));
            }
        }

#if !ENABLE_IL2CPP
        [TestCase(new string[] { "root", "object", "child", "sibling", "grandchild" }, new string[] { "root", "object", "child", "sibling", "grandchild" }, Description = "Valid skeleton")]
        [TestCase(new string[] { "root", "invalid name" }, new string[] { "root" }, Description = "Non exsistant bone")]
        [TestCase(new string[] { "root (clone)", "object" }, new string[] { "root (clone)", "object" }, Description = "Renamed root")]
        [TestCase(new string[] {}, new string[] {}, Description = "Empty skeleton")]
        [TestCase(new string[] { "root", "sibling", "invalid object", "child", "grandchild" }, new string[] { "root", "sibling" }, Description = "Drop children of invalid bones")]
        public void FilterNonExsistantBones(string[] boneNames, string[] expectedNames)
        {
            var objects = CreateTestHirearchy();

            var bones = new List<string>(boneNames).Select((name) => new SkeletonBone { name = name }).ToArray();
            var expectedBones = new List<string>(expectedNames).Select((name) => new SkeletonBone { name = name }).ToArray();
            var actualBones = AnimatorUtils.FilterNonExsistantBones(objects[0].transform, bones);
            Assert.AreEqual(expectedBones, actualBones);
        }

        [TestCase(new string[] { "root", "sibling", "object", "child", "grandchild" }, new int[] { -1, 0, 0, 2, 3 }, Description = "Full hirearchy")]
        [TestCase(new string[] { "root", "object", "child" }, new int[] { -1, 0, 1 }, Description = "Partial hirearchy")]
        [TestCase(new string[] { "root" }, new int[] { -1 }, Description = "Only root")]
        [TestCase(new string[] {}, new int[] {}, Description = "No bones")]
        [TestCase(new string[] { "renamed root", "sibling", "object", "child", "grandchild" }, new int[] { -1, 0, 0, 2, 3 }, Description = "Renamed root")]
        [TestCase(new string[] { "root", "invalid name" }, new int[] { -1, -1 }, Description = "Invalid root")]
        public void GetBoneParentIndicies(string[] boneNames, int[] expected)
        {
            var objects = CreateTestHirearchy();

            var bones = new List<string>(boneNames).Select((name) => new SkeletonBone { name = name }).ToArray();

            var actual = AnimatorUtils.GetBoneParentIndicies(objects[0].transform, bones);
            Assert.AreEqual(expected, actual);
        }

#endif
    }
}
