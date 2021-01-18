using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Animation.Hybrid;
using Unity.Collections;

namespace Unity.Animation.Tests
{
    public class AnimatorExtensionTests
    {
        private GameObject[] CreateTestHirearchy()
        {
            var objects = new GameObject[]
            {
                new GameObject("root"),
                new GameObject("object"),
                new GameObject("child"),
                new GameObject("grandchild"),
                new GameObject("sibling"),
            };

            objects[1].transform.parent = objects[0].transform;
            objects[2].transform.parent = objects[1].transform;
            objects[3].transform.parent = objects[2].transform;
            objects[4].transform.parent = objects[0].transform;

            return objects;
        }

        private (Dictionary<string, GameObject>, string[]) CreateHumanTestHirearchy()
        {
            var objects = new Dictionary<string, GameObject>();
            objects["Root"] = new GameObject("Root");
            objects["Hips"] = new GameObject("Hips");
            objects["LeftUpperLeg"] = new GameObject("LeftUpperLeg");
            objects["LeftLowerLeg"] = new GameObject("LeftLowerLeg");
            objects["LeftFoot"] = new GameObject("LeftFoot");
            objects["LeftToes"] = new GameObject("LeftToes");
            objects["LeftToesEnd"] = new GameObject("LeftToesEnd");
            objects["RightUpperLeg"] = new GameObject("RightUpperLeg");
            objects["RightLowerLeg"] = new GameObject("RightLowerLeg");
            objects["RightFoot"] = new GameObject("RightFoot");
            objects["RightToes"] = new GameObject("RightToes");
            objects["RightToesEnd"] = new GameObject("RightToesEnd");
            objects["Spine"] = new GameObject("Spine");
            objects["Chest"] = new GameObject("Chest");
            objects["UpperChest"] = new GameObject("UpperChest");
            objects["LeftShoulder"] = new GameObject("LeftShoulder");
            objects["LeftUpperArm"] = new GameObject("LeftUpperArm");
            objects["LeftLowerArm"] = new GameObject("LeftLowerArm");
            objects["LeftHand"] = new GameObject("LeftHand");
            objects["RightShoulder"] = new GameObject("RightShoulder");
            objects["RightUpperArm"] = new GameObject("RightUpperArm");
            objects["RightLowerArm"] = new GameObject("RightLowerArm");
            objects["RightHand"] = new GameObject("RightHand");
            objects["Neck"] = new GameObject("Neck");
            objects["Head"] = new GameObject("Head");


            objects["Hips"].transform.parent = objects["Root"].transform;
            objects["LeftUpperLeg"].transform.parent = objects["Hips"].transform;
            objects["LeftLowerLeg"].transform.parent = objects["LeftUpperLeg"].transform;
            objects["LeftFoot"].transform.parent = objects["LeftLowerLeg"].transform;
            objects["LeftToes"].transform.parent = objects["LeftFoot"].transform;
            objects["LeftToesEnd"].transform.parent = objects["LeftToes"].transform;
            objects["RightUpperLeg"].transform.parent = objects["Hips"].transform;
            objects["RightLowerLeg"].transform.parent = objects["RightUpperLeg"].transform;
            objects["RightFoot"].transform.parent = objects["RightLowerLeg"].transform;
            objects["RightToes"].transform.parent = objects["RightFoot"].transform;
            objects["RightToesEnd"].transform.parent = objects["RightToes"].transform;
            objects["Spine"].transform.parent = objects["Hips"].transform;
            objects["Chest"].transform.parent = objects["Spine"].transform;
            objects["UpperChest"].transform.parent = objects["Chest"].transform;
            objects["LeftShoulder"].transform.parent = objects["UpperChest"].transform;
            objects["LeftUpperArm"].transform.parent = objects["LeftShoulder"].transform;
            objects["LeftLowerArm"].transform.parent = objects["LeftUpperArm"].transform;
            objects["LeftHand"].transform.parent = objects["LeftLowerArm"].transform;
            objects["RightShoulder"].transform.parent = objects["UpperChest"].transform;
            objects["RightUpperArm"].transform.parent = objects["RightShoulder"].transform;
            objects["RightLowerArm"].transform.parent = objects["RightUpperArm"].transform;
            objects["RightHand"].transform.parent = objects["RightLowerArm"].transform;
            objects["Neck"].transform.parent = objects["Spine"].transform;
            objects["Head"].transform.parent = objects["Neck"].transform;

            var expectedNames = new string[]
            {
                "Root",
                "Hips",
                "LeftUpperLeg",
                "LeftLowerLeg",
                "LeftFoot",
                "RightUpperLeg",
                "RightLowerLeg",
                "RightFoot",
                "Spine",
                "Chest",
                "UpperChest",
                "LeftShoulder",
                "LeftUpperArm",
                "LeftLowerArm",
                "LeftHand",
                "RightShoulder",
                "RightUpperArm",
                "RightLowerArm",
                "RightHand",
                "Neck",
                "Head",
            };
            var humanDescription = new HumanDescription()
            {
                human = new HumanBone[expectedNames.Length],
                skeleton = new SkeletonBone[expectedNames.Length]
            };
            for (int i = 0; i < expectedNames.Length; i++)
            {
                humanDescription.human[i] = new HumanBone() { boneName = expectedNames[i], humanName = expectedNames[i] };
                humanDescription.skeleton[i] = new SkeletonBone() {
                    name = expectedNames[i],
                    position = new Vector3(0, 1, 2),
                    rotation = Quaternion.identity,
                    scale = new Vector3(1, 1, 1),
                };
            }

            objects["Root"].AddComponent<Animator>();
            objects["Root"].GetComponent<Animator>().avatar = AvatarBuilder.BuildHumanAvatar(objects["Root"], humanDescription);


            return (objects, expectedNames);
        }

        [Test]
        public void ExtractSkeletonNodesFromAnimator()
        {
            (var objects, var expectedNames) = CreateHumanTestHirearchy();
            var expected = new SkeletonNode[expectedNames.Length];
            var animator = objects["Root"].GetComponent<Animator>();
            var parentIndicies = AnimatorUtils.GetBoneParentIndicies(objects["Root"].transform, animator.avatar.humanDescription.skeleton);
            for (int i = 0; i < expectedNames.Length; ++i)
            {
                var bone = animator.avatar.humanDescription.skeleton[i];
                expected[i] = new SkeletonNode() {
                    Id = new StringHash(RigGenerator.ComputeRelativePath(objects[expectedNames[i]].transform, objects["Root"].transform)),
                    LocalTranslationDefaultValue = bone.position,
                    LocalRotationDefaultValue = bone.rotation,
                    LocalScaleDefaultValue = bone.scale,
                    AxisIndex = -1,
                    ParentIndex = parentIndicies[i],
                };
            }
            using (var actual = new NativeList<SkeletonNode>(Allocator.Temp))
            {
                animator.ExtractSkeletonNodes(actual);
                Assert.AreEqual(expected, actual.ToArray());
            }
        }

        [Test]
        public void ExtractSkeletonNodesFromAnimatorModifiedTransforms()
        {
            (var objects, var expectedNames) = CreateHumanTestHirearchy();

            objects["Spine"].name = "Spine (Renamed)";
            objects["RightUpperLeg"].name = "RightUpperLeg (Renamed)";
            objects["LeftLowerLeg"].name = "LeftLowerLeg (Renamed)";

            var bones = objects["Root"].GetComponent<Animator>().avatar.humanDescription.skeleton;
            var expected = new SkeletonNode[]
            {
                new SkeletonNode() {
                    Id = new StringHash(""),
                    LocalTranslationDefaultValue = bones[0].position,
                    LocalRotationDefaultValue = bones[0].rotation,
                    LocalScaleDefaultValue = bones[0].scale,
                    AxisIndex = -1,
                    ParentIndex = -1,
                },
                new SkeletonNode() {
                    Id = new StringHash("Hips"),
                    LocalTranslationDefaultValue = bones[1].position,
                    LocalRotationDefaultValue = bones[1].rotation,
                    LocalScaleDefaultValue = bones[1].scale,
                    AxisIndex = -1,
                    ParentIndex = 0,
                },
                new SkeletonNode() {
                    Id = new StringHash("Hips/LeftUpperLeg"),
                    LocalTranslationDefaultValue = bones[2].position,
                    LocalRotationDefaultValue = bones[2].rotation,
                    LocalScaleDefaultValue = bones[2].scale,
                    AxisIndex = -1,
                    ParentIndex = 1,
                }
            };
            using (var actual = new NativeList<SkeletonNode>(Allocator.Temp))
            {
                objects["Root"].GetComponent<Animator>().ExtractSkeletonNodes(actual);
                Assert.AreEqual(expected, actual.ToArray());
            }
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, "Animator (Root (UnityEngine.Animator)) is missing 18 bones.");
        }

        [Test]
        public void ExtractBonesFromAnimatorNullAvatar()
        {
            var objects = CreateTestHirearchy();
            objects[0].AddComponent<Animator>();

            var expected = new List<RigIndexToBone>(new[]
            {
                new RigIndexToBone {Index = 0, Bone = objects[0].transform},
                new RigIndexToBone {Index = 1, Bone = objects[1].transform},
                new RigIndexToBone {Index = 2, Bone = objects[2].transform},
                new RigIndexToBone {Index = 3, Bone = objects[3].transform},
                new RigIndexToBone {Index = 4, Bone = objects[4].transform}
            });

            var actual = new List<RigIndexToBone>();
            objects[0].GetComponent<Animator>().ExtractBoneTransforms(actual);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ExtractBonesFromAnimatorGenericAvatar()
        {
            var objects = CreateTestHirearchy();
            objects[0].AddComponent<Animator>();
            objects[0].GetComponent<Animator>().avatar = AvatarBuilder.BuildGenericAvatar(objects[0], "root");

            var expected = new List<RigIndexToBone>(new[]
            {
                new RigIndexToBone {Index = 0, Bone = objects[0].transform},
                new RigIndexToBone {Index = 1, Bone = objects[1].transform},
                new RigIndexToBone {Index = 2, Bone = objects[2].transform},
                new RigIndexToBone {Index = 3, Bone = objects[3].transform},
                new RigIndexToBone {Index = 4, Bone = objects[4].transform}
            });

            var actual = new List<RigIndexToBone>();
            objects[0].GetComponent<Animator>().ExtractBoneTransforms(actual);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ExtractBonesFromAnimatorHumanoidAvatar()
        {
            (var objects, var expectedNames) = CreateHumanTestHirearchy();

            var expected = new List<RigIndexToBone>(new List<string>(expectedNames).Select((n, i) => new RigIndexToBone {Index = i, Bone = objects[n].transform}));

            var actual = new List<RigIndexToBone>();
            objects["Root"].GetComponent<Animator>().ExtractBoneTransforms(actual);
            Assert.AreEqual(expected, actual);
        }
    }
}
