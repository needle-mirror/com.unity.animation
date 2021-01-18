using System;
using NUnit.Framework;
using UnityEngine;
using Unity.Animation.Hybrid;
using Unity.Animation.Authoring;
using System.Collections.Generic;
using System.Linq;
using Unity.DataFlowGraph;

namespace Unity.Animation.Tests
{
    class RigAuthoring_Skeleton_IntegrationTests : RigHierarchyTestsFixture<RigAuthoring>
    {
        private struct TestHierarchy : IDisposable
        {
            public Authoring.Skeleton Skeleton;

            public Transform Root;
            public Transform A;
            public Transform B;

            public void Dispose()
            {
                ScriptableObject.DestroyImmediate(Skeleton);
            }
        }

        private TestHierarchy SetupTestHierarchy()
        {
            var rootBone = CreateBoneInHierarchy(Component, "Root");
            var a = CreateBoneInHierarchy(rootBone, "A");
            var b = CreateBoneInHierarchy(rootBone, "B");

            var skeleton = ScriptableObject.CreateInstance<Authoring.Skeleton>();
            skeleton.PopulateFromGameObjectHierarchy(Component.gameObject);
            skeleton.Root = RigGenerator.ToTransformBindingID(rootBone, Component.transform);

            Component.Skeleton = skeleton;
            Component.TargetSkeletonRoot = rootBone;

            return new TestHierarchy {Root = rootBone, A = a, B = b, Skeleton = skeleton};
        }

        private void OverrideChildAssignments(TestHierarchy testData)
        {
            Component.OverrideTransformBinding(new TransformBindingID {Path = RigGenerator.ComputeRelativePath(testData.A, Component.transform)}, testData.B);
            Component.OverrideTransformBinding(new TransformBindingID {Path = RigGenerator.ComputeRelativePath(testData.B, Component.transform)}, testData.A);
        }

        [Test]
        public void RigAuthoring_WithoutOverrides_FindsMatches()
        {
            using (var testData = SetupTestHierarchy())
            {
                var bones = new List<RigIndexToBone>();
                ((IRigAuthoring)Component).GetBones(bones);

                Assert.That(bones.Select(m => m.Bone), Is.EqualTo(new[] {testData.Root, testData.A, testData.B}), "Mapping did not match expected result.");
            }
        }

        [Test]
        public void RigAuthoring_OverrideTransformBinding_OverrideUpdated()
        {
            using (var testData = SetupTestHierarchy())
            {
                OverrideChildAssignments(testData);

                var bones = new List<RigIndexToBone>();
                ((IRigAuthoring)Component).GetBones(bones);

                Assert.That(bones.Select(m => m.Bone), Is.EqualTo(new[] {testData.Root, testData.B, testData.A}), "Mapping did not match expected result.");
            }
        }

        [Test]
        public void RigAuthoring_OverrideTransformBinding_AddTransformChannel_OverridePreserved()
        {
            using (var testData = SetupTestHierarchy())
            {
                OverrideChildAssignments(testData);

                var bones = new List<RigIndexToBone>();
                ((IRigAuthoring)Component).GetBones(bones);

                Assume.That(bones.Select(m => m.Bone), Is.EqualTo(new[] {testData.Root, testData.B, testData.A}), "Mapping did not match expected result.");

                testData.Skeleton[new TransformBindingID {Path = "Root/A/C"}] = TransformChannelProperties.Default;

                ((IRigAuthoring)Component).GetBones(bones);

                Assert.That(bones.Select(m => m.Bone), Is.EqualTo(new[] {testData.Root, testData.B, null, testData.A}), "Mapping did not match expected result.");
            }
        }

        [Test]
        public void RigAuthoring_OverrideTransformBinding_AddGameObjectInHierarchy_OverridePreserved()
        {
            using (var testData = SetupTestHierarchy())
            {
                OverrideChildAssignments(testData);

                var bones = new List<RigIndexToBone>();
                ((IRigAuthoring)Component).GetBones(bones);

                Assume.That(bones.Select(m => m.Bone), Is.EqualTo(new[] {testData.Root, testData.B, testData.A}), "Mapping did not match expected result.");

                CreateBoneInHierarchy(testData.A, "C");
                ((IRigAuthoring)Component).GetBones(bones);

                Assert.That(bones.Select(m => m.Bone), Is.EqualTo(new[] {testData.Root, testData.B, testData.A}), "Mapping did not match expected result.");
            }
        }

        [Test]
        public void RigAuthoring_ClearManualOverride_OverrideRemoved()
        {
            using (var testData = SetupTestHierarchy())
            {
                OverrideChildAssignments(testData);

                var bones = new List<RigIndexToBone>();
                ((IRigAuthoring)Component).GetBones(bones);

                Assume.That(bones.Select(m => m.Bone), Is.EqualTo(new[] {testData.Root, testData.B, testData.A}), "Mapping did not match expected result.");

                Component.ClearManualOverride(new TransformBindingID {Path = "Root/B"});

                ((IRigAuthoring)Component).GetBones(bones);

                Assert.That(bones.Select(m => m.Bone), Is.EqualTo(new[] {testData.Root, testData.A, testData.A}), "Mapping did not match expected result.");
            }
        }

        [Test]
        public void RigAuthoring_ClearAllManualOverrides_OverridesRemoved()
        {
            using (var testData = SetupTestHierarchy())
            {
                OverrideChildAssignments(testData);

                var bones = new List<RigIndexToBone>();
                ((IRigAuthoring)Component).GetBones(bones);

                Assume.That(bones.Select(m => m.Bone), Is.EqualTo(new[] {testData.Root, testData.B, testData.A}), "Mapping did not match expected result.");

                Component.ClearAllManualOverrides();

                ((IRigAuthoring)Component).GetBones(bones);
                Assert.That(bones.Select(m => m.Bone), Is.EqualTo(new[] {testData.Root, testData.A, testData.B}), "Mapping did not match expected result.");
            }
        }

        [Test]
        public void RigAuthoring_ClearInvalidManualOverrides_OverridesRemoved()
        {
            using (var testData = SetupTestHierarchy())
            {
                OverrideChildAssignments(testData);

                var bones = new List<RigIndexToBone>();
                ((IRigAuthoring)Component).GetBones(bones);

                var bindingID = new TransformBindingID {Path = "Root/A"};

                Assume.That(Component.HasManualOverride(bindingID), Is.True);

                testData.Skeleton.RemoveTransformChannelAndDescendants(bindingID);
                Component.ClearInvalidManualOverrides();

                Assert.That(Component.HasManualOverride(bindingID), Is.False);
            }
        }
    }
}
