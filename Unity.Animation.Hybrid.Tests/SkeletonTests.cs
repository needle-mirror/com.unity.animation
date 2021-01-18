using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Animation.Authoring;
using Unity.Animation.Hybrid;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Animation.Tests
{
    class SkeletonTests
    {
        private Authoring.Skeleton m_Skeleton;

        private void AssumeThatTransformChannelExistsAndIsActive(Authoring.Skeleton skeletonAsset, TransformBindingID id, TransformChannelProperties expectedProperties)
        {
            var transformChannels = new List<TransformChannel>();
            m_Skeleton.GetAllTransforms(transformChannels, TransformChannelSearchMode.ActiveRootDescendants);

            var channelIndex = transformChannels.FindIndex(channel => channel.ID.Equals(id));

            Assume.That(skeletonAsset.GetTransformChannelState(id) == TransformChannelState.Active);
            Assume.That(channelIndex, Is.Not.EqualTo(-1));
            Assume.That(transformChannels[channelIndex].Properties, Is.EqualTo(expectedProperties), "Transform properties did not match.");
        }

        private void AssertThatTransformChannelExistsAndIsActive(Authoring.Skeleton skeletonAsset, TransformBindingID id, TransformChannelProperties expectedProperties)
        {
            var transformChannels = new List<TransformChannel>();
            m_Skeleton.GetAllTransforms(transformChannels, TransformChannelSearchMode.ActiveRootDescendants);

            var channelIndex = transformChannels.FindIndex(channel => channel.ID.Equals(id));

            Assert.AreEqual(TransformChannelState.Active, skeletonAsset.GetTransformChannelState(id));
            Assert.That(channelIndex, Is.Not.EqualTo(-1));
            Assert.That(transformChannels[channelIndex].Properties, Is.EqualTo(expectedProperties), "Transform properties did not match.");
        }

        private void AssertThatTransformChannelExistsAndIsInactive(Authoring.Skeleton skeletonAsset, TransformBindingID id, TransformChannelProperties expectedProperties)
        {
            var transformChannels = new List<TransformChannel>();
            m_Skeleton.GetAllTransforms(transformChannels, TransformChannelSearchMode.InactiveAll);

            var channelIndex = transformChannels.FindIndex(channel => channel.ID.Equals(id));

            Assert.AreEqual(TransformChannelState.Inactive, skeletonAsset.GetTransformChannelState(id));
            Assert.That(channelIndex, Is.Not.EqualTo(-1));
            Assert.That(transformChannels[channelIndex].Properties, Is.EqualTo(expectedProperties), "Transform properties did not match.");
        }

        private void AssertThatGenericChannelExists(Authoring.Skeleton skeletonAsset, GenericBindingID id, GenericPropertyVariant defaultValue)
        {
            var genericChannels = new List<IGenericChannel>();
            skeletonAsset.GetGenericChannels(genericChannels);

            var numberOfChannels = id.ValueType.GetNumberOfChannels();
            if (numberOfChannels > 1)
            {
                for (int i = 0; i < numberOfChannels; ++i)
                {
                    var channelIndex = genericChannels.FindIndex(channel => channel.ID.Equals(id[i]));

                    Assert.That(channelIndex, Is.Not.EqualTo(-1));
                    Assert.That(genericChannels[channelIndex].DefaultValue, Is.EqualTo(defaultValue[i]));
                }
            }
            else
            {
                var channelIndex = genericChannels.FindIndex(channel => channel.ID.Equals(id));

                Assert.That(channelIndex, Is.Not.EqualTo(-1));
                Assert.That(genericChannels[channelIndex].DefaultValue, Is.EqualTo(defaultValue));
            }
        }

        private void AssertThatGenericChannelDoesntExist(Authoring.Skeleton skeletonAsset, GenericBindingID id)
        {
            var genericChannels = new List<IGenericChannel>();
            skeletonAsset.GetGenericChannels(genericChannels);

            var numberOfChannels = id.ValueType.GetNumberOfChannels();
            if (numberOfChannels > 1)
            {
                for (int i = 0; i < numberOfChannels; ++i)
                {
                    var channelIndex = genericChannels.FindIndex(channel => channel.ID.Equals(id[i]));
                    Assert.That(channelIndex, Is.EqualTo(-1));
                }
            }
            else
            {
                var channelIndex = genericChannels.FindIndex(channel => channel.ID.Equals(id));
                Assert.That(channelIndex, Is.EqualTo(-1));
            }
        }

        private void AssertThatGenericChannelAtIndexMatchesGenericBindingID(Authoring.Skeleton skeletonAsset, GenericBindingID id, int channelIndex)
        {
            var numberOfChannels = id.ValueType.GetNumberOfChannels();
            var concreteChannels = GetConcreteChannels(skeletonAsset, id.ValueType);

            if (numberOfChannels > 1)
            {
                for (int i = 0; i < numberOfChannels; ++i)
                {
                    Assert.That(id[i].Equals(concreteChannels[channelIndex + i].ID));
                }
            }
            else
            {
                Assert.That(id.Equals(concreteChannels[channelIndex].ID));
            }
        }

        private void AssertThatRigBuilderSkeletonNodeMatchesTransformChannel(RigBuilderData rigBuilderData, uint id, float3 defaultTranslation, quaternion defaultRotation, float3 defaultScale)
        {
            var skeletonNodes = rigBuilderData.SkeletonNodes;

            int index = -1;
            for (int i = 0; i < skeletonNodes.Length; ++i)
            {
                if (skeletonNodes[i].Id == id)
                {
                    index = i;
                    break;
                }
            }

            Assert.That(index, Is.Not.EqualTo(-1));
            Assert.That(skeletonNodes[index].LocalTranslationDefaultValue, Is.EqualTo(defaultTranslation), "Translation did not match.");
            Assert.That(skeletonNodes[index].LocalRotationDefaultValue, Is.EqualTo(defaultRotation), "Rotation did not match.");
            Assert.That(skeletonNodes[index].LocalScaleDefaultValue, Is.EqualTo(defaultScale), "Scale did not match.");
        }

        private IGenericChannel[] GetConcreteChannels(Authoring.Skeleton skeletonAsset, GenericPropertyType valueType)
        {
            switch (valueType.GetGenericChannelType())
            {
                case GenericChannelType.Float:
                    var floatChannels = m_Skeleton.FloatChannels;
                    return floatChannels.Cast<IGenericChannel>().ToArray();
                case GenericChannelType.Int:
                    var intChannels = m_Skeleton.IntChannels;
                    return intChannels.Cast<IGenericChannel>().ToArray();
                case GenericChannelType.Quaternion:
                    var quaternionChannels = m_Skeleton.QuaternionChannels;
                    return quaternionChannels.Cast<IGenericChannel>().ToArray();
            }

            return Array.Empty<IGenericChannel>();
        }

        [SetUp]
        public void SetUp()
        {
            m_Skeleton = ScriptableObject.CreateInstance<Authoring.Skeleton>();
            Assume.That(m_Skeleton.ActiveTransformChannelCount, Is.EqualTo(0), "Skeleton was not empty.");
            Assume.That(m_Skeleton.InactiveTransformChannelCount, Is.EqualTo(0), "Skeleton was not empty");
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Skeleton)
                ScriptableObject.DestroyImmediate(m_Skeleton);
        }

        [Test]
        public void Skeleton_ActiveTransformChannelCount_AfterSettingPathToGrandchild_IncludesAllAncestors()
        {
            m_Skeleton[new TransformBindingID { Path = "A/B/C" }] = default;

            var expectedTransformChannelCount = new[] { "", "A", "A/B", "A/B/C" }.Length;
            Assert.That(
                m_Skeleton.ActiveTransformChannelCount, Is.EqualTo(expectedTransformChannelCount),
                "Active transform channel count did not include only descendants of root."
            );
        }

        [Test]
        public void Skeleton_ActiveTransformChannelCount_ExcludesRootAncestors()
        {
            m_Skeleton[new TransformBindingID { Path = "A/B/C" }] = default;

            m_Skeleton.Root = new TransformBindingID { Path = "A/B" };

            var expectedTransformChannelCount = new[] { "A/B", "A/B/C" }.Length;
            Assert.That(
                m_Skeleton.ActiveTransformChannelCount, Is.EqualTo(expectedTransformChannelCount),
                "Active transform channel count did not include only descendants of root."
            );
        }

        [Test]
        public void Skeleton_InactiveTransformChannelCount_IncludesRootAncestors()
        {
            m_Skeleton[new TransformBindingID { Path = "A/B/C" }] = default;

            m_Skeleton.Root = new TransformBindingID { Path = "A/B" };

            var expectedTransformChannelCount = new[] { "", "A" }.Length;
            Assert.That(
                m_Skeleton.InactiveTransformChannelCount, Is.EqualTo(expectedTransformChannelCount),
                "Active transform channel count did not include only ancestors of root."
            );
        }

        static readonly TransformBindingID[] s_TransformBindings =
        {
            TransformBindingID.Root,
            new TransformBindingID {Path = "A"},
            new TransformBindingID {Path = "A/B"},
            new TransformBindingID {Path = "B"},
        };

        static readonly TransformChannel[] s_TransformProperties =
        {
            new TransformChannel { ID = s_TransformBindings[0], Properties = new TransformChannelProperties { DefaultTranslationValue = float3.zero, DefaultRotationValue = quaternion.identity, DefaultScaleValue = new float3(1f) } },
            new TransformChannel { ID = s_TransformBindings[1], Properties = new TransformChannelProperties { DefaultTranslationValue = new float3(1f, 0f, 0f), DefaultRotationValue = quaternion.Euler(25f, 0f, 0f), DefaultScaleValue = new float3(1f) } },
            new TransformChannel { ID = s_TransformBindings[2], Properties = new TransformChannelProperties { DefaultTranslationValue = new float3(-1f, 0f, 0f), DefaultRotationValue = quaternion.Euler(-25f, 0f, 0f), DefaultScaleValue = new float3(1f) } },
            new TransformChannel { ID = s_TransformBindings[3], Properties = new TransformChannelProperties { DefaultTranslationValue = new float3(0f, 1f, 0f), DefaultRotationValue = quaternion.Euler(0f, 0f, 90f), DefaultScaleValue = new float3(1f) } }
        };

        // Skeleton management.
        [Test]
        public void Skeleton_SetTransformChannelsOutOfOrder_AppendsToActiveTransformsInOrder()
        {
            m_Skeleton[new TransformBindingID { Path = "B" }] = default;
            m_Skeleton[new TransformBindingID { Path = "A/B" }] = default;
            m_Skeleton[new TransformBindingID { Path = "A" }] = default;
            m_Skeleton[new TransformBindingID { Path = "" }] = default;

            var transformChannels = new List<TransformChannel>();
            m_Skeleton.GetAllTransforms(transformChannels, TransformChannelSearchMode.ActiveRootDescendants);

            var expectedOrder = new[] { "", "A", "A/B", "B" };
            Assert.That(transformChannels.Select(c => c.ID.Path), Is.EqualTo(expectedOrder));
        }

        [Test]
        public void Skeleton_RemoveBone_RemovesFrom_TransformChannels(
            [ValueSource(nameof(s_TransformProperties))] TransformChannel channel
        )
        {
            m_Skeleton[channel.ID] = channel.Properties;
            Assume.That(m_Skeleton.Contains(channel.ID, TransformChannelSearchMode.ActiveAndInactiveAll), Is.True, "Skeleton did not contain bone to be removed.");

            var retValue = m_Skeleton.RemoveTransformChannelAndDescendants(channel.ID);
            Assume.That(retValue, Is.True, "Removing bone returned failure.");

            Assert.That(m_Skeleton.Contains(channel.ID, TransformChannelSearchMode.ActiveAndInactiveAll), Is.False, "Bone still defined on skeleton.");
        }

        [Test]
        public void Skeleton_RemoveBone_WhenDescendentsAreInactive_DescendantsAreRemoved()
        {
            var root = TransformBindingID.Root;
            var a = new TransformBindingID { Path = "A" };
            var b = new TransformBindingID { Path = "A/B" };
            var c = new TransformBindingID { Path = "A/B/C" };
            m_Skeleton[c] = default;
            m_Skeleton.SetTransformChannelDescendantsToInactive(b, includeSelf: true);
            Assume.That(m_Skeleton.GetTransformChannelState(root), Is.EqualTo(TransformChannelState.Active), "Active transforms did not initialize correctly.");
            Assume.That(m_Skeleton.GetTransformChannelState(a), Is.EqualTo(TransformChannelState.Active), "Active transforms did not initialize correctly.");
            Assume.That(m_Skeleton.GetTransformChannelState(b), Is.EqualTo(TransformChannelState.Inactive), "Active transforms did not initialize correctly.");
            Assume.That(m_Skeleton.GetTransformChannelState(c), Is.EqualTo(TransformChannelState.Inactive), "Active transforms did not initialize correctly.");

            m_Skeleton.RemoveTransformChannelAndDescendants(root);

            Assert.That(m_Skeleton.ActiveTransformChannelCount, Is.EqualTo(0), "One or more active bones still defined on skeleton.");
            Assert.That(m_Skeleton.InactiveTransformChannelCount, Is.EqualTo(0), "One or more inactive bones still defined on skeleton.");
        }

        [Test]
        public void Skeleton_RemoveTransformChannelAndDescendants_DescendantsRemoved()
        {
            var root = TransformBindingID.Root;
            var a = new TransformBindingID { Path = "A" };
            var b = new TransformBindingID { Path = "A/B" };
            var c = new TransformBindingID { Path = "A/B/C" };
            m_Skeleton[c] = default;
            var activeTransformChannels = new List<TransformChannel>();
            m_Skeleton.GetAllTransforms(activeTransformChannels, TransformChannelSearchMode.ActiveRootDescendants);
            Assume.That(activeTransformChannels.Select(ch => ch.ID), Is.EqualTo(new[] { root, a, b, c }), "Skeleton did not contain expected hierarchy.");

            var retValue = m_Skeleton.RemoveTransformChannelAndDescendants(root);
            Assume.That(retValue, Is.True, "Removing bone returned failure.");

            Assert.That(m_Skeleton.ActiveTransformChannelCount, Is.EqualTo(0), "One or more bones still defined on skeleton.");
        }

        [Test]
        public void Skeleton_Contains_WhenInvalid_ThrowsArgumentException() =>
            Assert.Throws<ArgumentException>(() => m_Skeleton.Contains(new TransformBindingID { Path = null }, TransformChannelSearchMode.ActiveAndInactiveAll));

        [Test]
        public void Skeleton_Contains_WhenDoesNotExist_ReturnsFalse([Values("", "A", "A/B", "/")] string path)
        {
            Assume.That(m_Skeleton.ActiveTransformChannelCount, Is.EqualTo(0), "One or more active bones defined on skeleton by default.");
            Assume.That(m_Skeleton.InactiveTransformChannelCount, Is.EqualTo(0), "One or more inactive bones defined on skeleton by default.");
            var id = new TransformBindingID { Path = path };

            var actual = m_Skeleton.Contains(id, TransformChannelSearchMode.ActiveAndInactiveAll);
            Assert.That(actual, Is.False, "Undefined bone located on skeleton definition.");
        }

        [Test]
        public void Skeleton_Contains_WhenExistsActive_ReturnsTrue([Values("", "A", "A/B", "/")] string path)
        {
            Assume.That(m_Skeleton.ActiveTransformChannelCount, Is.EqualTo(0), "One or more active bones defined on skeleton by default.");
            Assume.That(m_Skeleton.InactiveTransformChannelCount, Is.EqualTo(0), "One or more inactive bones defined on skeleton by default.");
            var id = new TransformBindingID { Path = path };

            m_Skeleton[id] = default;
            var activeTransformChannels = new List<TransformChannel>();
            m_Skeleton.GetAllTransforms(activeTransformChannels, TransformChannelSearchMode.ActiveRootDescendants);
            Assume.That(activeTransformChannels.Select(c => c.ID), Contains.Item(id), "Newly added bone absent from skeleton definition.");

            var actual = m_Skeleton.Contains(id, TransformChannelSearchMode.ActiveAndInactiveAll);
            Assert.That(actual, Is.True, "Failed to locate newly added (active) bone.");
        }

        [Test]
        public void Skeleton_Contains_WhenExistsInactive_ReturnsTrue([Values("", "A", "A/B", "/")] string path)
        {
            var id = new TransformBindingID { Path = path };
            Assume.That(m_Skeleton.Contains(id, TransformChannelSearchMode.ActiveAndInactiveAll), Is.False, "Bone defined on skeleton by default.");
            m_Skeleton[id] = default;
            m_Skeleton.SetTransformChannelDescendantsToInactive(id);
            Assert.That(m_Skeleton.GetTransformChannelState(id), Is.EqualTo(TransformChannelState.Inactive), "Inactive bone not defined.");

            var actual = m_Skeleton.Contains(id, TransformChannelSearchMode.ActiveAndInactiveAll);
            Assert.That(actual, Is.True, "Failed to locate newly added (inactive) bone.");
        }

        [Test]
        public void Skeleton_SetInactiveTransformChannelsToAllBones_AllBonesAreInactive()
        {
            foreach (var property in s_TransformProperties)
            {
                m_Skeleton[property.ID] = property.Properties;
                AssumeThatTransformChannelExistsAndIsActive(m_Skeleton, property.ID, property.Properties);
            }

            m_Skeleton.SetInactiveTransformChannels(s_TransformBindings);

            foreach (var property in s_TransformProperties)
            {
                AssertThatTransformChannelExistsAndIsInactive(m_Skeleton, property.ID, property.Properties);
            }
        }

        [Test]
        public void Skeleton_SetTransformChannelAncestorsToActiveIncludingSelf_AllDescendantsAreActive()
        {
            var rootID = TransformBindingID.Root;
            var rootID_A = new TransformBindingID { Path = "A" };
            var rootID_B = new TransformBindingID { Path = "B" };
            var rootID_B_C = new TransformBindingID { Path = "B/C" };
            m_Skeleton[rootID] = TransformChannelProperties.Default;
            m_Skeleton[rootID_A] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B_C] = TransformChannelProperties.Default;
            m_Skeleton.SetInactiveTransformChannels(new[] { rootID, rootID_A, rootID_B, rootID_B_C });
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_A));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B_C));

            m_Skeleton.SetTransformChannelAncestorsToActive(rootID_B_C, includeSelf: true);

            // part of chain to root including rootID_B_C, needs to be active
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B_C));
            // not part of chain to root or rootID_B_C, needs to remain inactive
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_A));
        }

        [Test]
        public void Skeleton_SetTransformChannelAncestorsToActiveExcludingSelf_AllDescendantsAreActive()
        {
            var rootID = TransformBindingID.Root;
            var rootID_A = new TransformBindingID { Path = "A" };
            var rootID_B = new TransformBindingID { Path = "B" };
            var rootID_B_C = new TransformBindingID { Path = "B/C" };
            m_Skeleton[rootID] = TransformChannelProperties.Default;
            m_Skeleton[rootID_A] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B_C] = TransformChannelProperties.Default;
            m_Skeleton.SetInactiveTransformChannels(new[] { rootID, rootID_A, rootID_B, rootID_B_C });
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_A));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B_C));

            m_Skeleton.SetTransformChannelAncestorsToActive(rootID_B_C, includeSelf: false);

            // part of chain to root, needs to be active
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B));
            // not part of chain to root, needs to remain inactive
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_A));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B_C));
        }

        [Test]
        public void Skeleton_SetTransformChannelDescendantsAndAncestorsToActive_AllDescendantsAndAncestorsAreActive()
        {
            var rootID = TransformBindingID.Root;
            var rootID_A = new TransformBindingID { Path = "A" };
            var rootID_B = new TransformBindingID { Path = "B" };
            var rootID_B_C = new TransformBindingID { Path = "B/C" };
            m_Skeleton[rootID] = TransformChannelProperties.Default;
            m_Skeleton[rootID_A] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B_C] = TransformChannelProperties.Default;
            m_Skeleton.SetInactiveTransformChannels(new[] { rootID, rootID_A, rootID_B, rootID_B_C });
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_A));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B_C));

            m_Skeleton.SetTransformChannelDescendantsAndAncestorsToActive(rootID_B);

            // part of chain to from Root/B root, needs to be active
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B_C));
            // not part of chain from Root/B to root, needs to be inactive
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_A));
        }

        [Test]
        public void Skeleton_SetTransformChannelDescendantsAndAncestorsToActiveEnumerable_AllDescendantsAndAncestorsAreActive()
        {
            var rootID = TransformBindingID.Root;
            var rootID_A = new TransformBindingID { Path = "A" };
            var rootID_B = new TransformBindingID { Path = "B" };
            var rootID_B_C = new TransformBindingID { Path = "B/C" };
            m_Skeleton[rootID] = TransformChannelProperties.Default;
            m_Skeleton[rootID_A] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B_C] = TransformChannelProperties.Default;
            m_Skeleton.SetInactiveTransformChannels(new[] { rootID, rootID_A, rootID_B, rootID_B_C });
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_A));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B_C));

            m_Skeleton.SetTransformChannelDescendantsAndAncestorsToActive(new[] { rootID_B });

            // part of chain to from Root/B root, needs to be active
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B_C));
            // not part of chain from Root/B to root, needs to be inactive
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_A));
        }

        [Test]
        public void Skeleton_SetTransformChannelDescendantsToInactiveIncludingSelfEnumerable_AllDescendantsAreInactive()
        {
            var rootID = TransformBindingID.Root;
            var rootID_A = new TransformBindingID { Path = "A" };
            var rootID_B = new TransformBindingID { Path = "B" };
            var rootID_B_C = new TransformBindingID { Path = "B/C" };
            m_Skeleton[rootID] = TransformChannelProperties.Default;
            m_Skeleton[rootID_A] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B_C] = TransformChannelProperties.Default;
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_A));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B_C));

            m_Skeleton.SetTransformChannelDescendantsToInactive(new[] { rootID_B }, includeSelf: true);

            // descendants of rootID_B or rootID_B itself, needs to be inactive
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B_C));
            // not a descendant of rootID_B or rootID_B itself, so needs to remain active
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_A));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID));
        }

        [Test]
        public void Skeleton_SetTransformChannelDescendantsToInactiveExcludingSelfEnumerable_AllDescendantsAreInactive()
        {
            var rootID = TransformBindingID.Root;
            var rootID_A = new TransformBindingID { Path = "A" };
            var rootID_B = new TransformBindingID { Path = "B" };
            var rootID_B_C = new TransformBindingID { Path = "B/C" };
            m_Skeleton[rootID] = TransformChannelProperties.Default;
            m_Skeleton[rootID_A] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B_C] = TransformChannelProperties.Default;
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_A));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B_C));

            m_Skeleton.SetTransformChannelDescendantsToInactive(new[] { rootID_B }, includeSelf: false);

            // descendants of rootID_B, needs to be inactive
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B_C));
            // not a descendant of rootID_B, so needs to remain active;
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_A));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B));
        }

        [Test]
        public void Skeleton_SetTransformChannelAncestorsToActiveIncludingSelfEnumerable_AllDescendantsAreActive()
        {
            var rootID = TransformBindingID.Root;
            var rootID_A = new TransformBindingID { Path = "A" };
            var rootID_B = new TransformBindingID { Path = "B" };
            var rootID_B_C = new TransformBindingID { Path = "B/C" };
            m_Skeleton[rootID] = TransformChannelProperties.Default;
            m_Skeleton[rootID_A] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B_C] = TransformChannelProperties.Default;
            m_Skeleton.SetInactiveTransformChannels(new[] { rootID, rootID_A, rootID_B, rootID_B_C });
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_A));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B_C));

            m_Skeleton.SetTransformChannelAncestorsToActive(new[] { rootID_B_C }, includeSelf: true);

            // ancestors of rootID_B or rootID_B itself, needs to be active
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B_C));
            // not a ancestors of rootID_B or rootID_B itself, so needs to remain inactive
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_A));
        }

        [Test]
        public void Skeleton_SetTransformChannelAncestorsToActiveExcludingSelfEnumerable_AllDescendantsAreActive()
        {
            var rootID = TransformBindingID.Root;
            var rootID_A = new TransformBindingID { Path = "A" };
            var rootID_B = new TransformBindingID { Path = "B" };
            var rootID_B_C = new TransformBindingID { Path = "B/C" };
            m_Skeleton[rootID] = TransformChannelProperties.Default;
            m_Skeleton[rootID_A] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B_C] = TransformChannelProperties.Default;
            m_Skeleton.SetInactiveTransformChannels(new[] { rootID, rootID_A, rootID_B, rootID_B_C });
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_A));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B_C));

            m_Skeleton.SetTransformChannelAncestorsToActive(new[] { rootID_B_C }, includeSelf: false);

            // ancestors of rootID_B, needs to be active
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B));
            // not a ancestors of rootID_B, so needs to remain inactive
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_A));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B_C));
        }

        [Test]
        public void Skeleton_SetTransformChannelDescendantsToInactiveIncludingSelf_AllDescendantsAreInactive()
        {
            var rootID = TransformBindingID.Root;
            var rootID_A = new TransformBindingID { Path = "A" };
            var rootID_B = new TransformBindingID { Path = "B" };
            var rootID_B_C = new TransformBindingID { Path = "B/C" };
            m_Skeleton[rootID] = TransformChannelProperties.Default;
            m_Skeleton[rootID_A] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B_C] = TransformChannelProperties.Default;
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B_C));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_A));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID));

            m_Skeleton.SetTransformChannelDescendantsToInactive(rootID_B, includeSelf: true);

            // descendants of rootID_B or rootID_B itself, needs to be inactive
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B));
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B_C));
            // not a descendant of rootID_B or rootID_B itself, so needs to remain active
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_A));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID));
        }

        [Test]
        public void Skeleton_SetTransformChannelDescendantsToInactiveExcludingSelf_AllDescendantsAreInactive()
        {
            var rootID = TransformBindingID.Root;
            var rootID_A = new TransformBindingID { Path = "A" };
            var rootID_B = new TransformBindingID { Path = "B" };
            var rootID_B_C = new TransformBindingID { Path = "B/C" };
            m_Skeleton[rootID] = TransformChannelProperties.Default;
            m_Skeleton[rootID_A] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B_C] = TransformChannelProperties.Default;
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B_C));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_A));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID));

            m_Skeleton.SetTransformChannelDescendantsToInactive(rootID_B, includeSelf: false);

            // descendants of rootID_B, needs to be inactive
            Assert.AreEqual(TransformChannelState.Inactive, m_Skeleton.GetTransformChannelState(rootID_B_C));
            // not a descendant of rootID_B, so needs to remain active
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_B));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID_A));
            Assert.AreEqual(TransformChannelState.Active, m_Skeleton.GetTransformChannelState(rootID));
        }

        [Test]
        public void Skeleton_SetRootBone_UpdatesChannelIndices()
        {
            var rootID = TransformBindingID.Root;
            var rootID_A = new TransformBindingID { Path = "A" };
            var rootID_B = new TransformBindingID { Path = "B" };
            var rootID_B_C = new TransformBindingID { Path = "B/C" };

            m_Skeleton[rootID] = TransformChannelProperties.Default;
            m_Skeleton[rootID_A] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B_C] = TransformChannelProperties.Default;

            Assume.That(m_Skeleton.QueryTransformIndex(rootID), Is.EqualTo(0));
            Assume.That(m_Skeleton.QueryTransformIndex(rootID_A), Is.EqualTo(1));
            Assume.That(m_Skeleton.QueryTransformIndex(rootID_B), Is.EqualTo(2));
            Assume.That(m_Skeleton.QueryTransformIndex(rootID_B_C), Is.EqualTo(3));

            m_Skeleton.Root = rootID_A;

            Assert.That(m_Skeleton.QueryTransformIndex(rootID), Is.EqualTo(-1));
            Assert.That(m_Skeleton.QueryTransformIndex(rootID_A), Is.EqualTo(0));
            Assert.That(m_Skeleton.QueryTransformIndex(rootID_B), Is.EqualTo(-1));
            Assert.That(m_Skeleton.QueryTransformIndex(rootID_B_C), Is.EqualTo(-1));

            Assert.That(m_Skeleton.ActiveTransformChannelCount, Is.EqualTo(1));

            m_Skeleton.Root = rootID_B;

            Assert.That(m_Skeleton.QueryTransformIndex(rootID), Is.EqualTo(-1));
            Assert.That(m_Skeleton.QueryTransformIndex(rootID_A), Is.EqualTo(-1));
            Assert.That(m_Skeleton.QueryTransformIndex(rootID_B), Is.EqualTo(0));
            Assert.That(m_Skeleton.QueryTransformIndex(rootID_B_C), Is.EqualTo(1));

            Assert.That(m_Skeleton.ActiveTransformChannelCount, Is.EqualTo(2));
        }

        [Test]
        public void Skeleton_WithRootBone_AddBone_Updates_ActiveTransformChannelCount()
        {
            var rootID = TransformBindingID.Root;
            var rootID_A = new TransformBindingID { Path = "A" };
            var rootID_B = new TransformBindingID { Path = "A/B" };

            m_Skeleton[rootID] = TransformChannelProperties.Default;
            m_Skeleton[rootID_A] = TransformChannelProperties.Default;
            m_Skeleton.Root = rootID_A;

            Assert.That(m_Skeleton.ActiveTransformChannelCount, Is.EqualTo(1));

            m_Skeleton[rootID_B] = TransformChannelProperties.Default;

            Assert.That(m_Skeleton.ActiveTransformChannelCount, Is.EqualTo(2));
        }

        [Test]
        public void Skeleton_WithRootBone_RemoveBone_Updates_ActiveTransformChannelCount()
        {
            var rootID = TransformBindingID.Root;
            var rootID_A = new TransformBindingID { Path = "A" };
            var rootID_B = new TransformBindingID { Path = "A/B" };

            m_Skeleton[rootID] = TransformChannelProperties.Default;
            m_Skeleton[rootID_A] = TransformChannelProperties.Default;
            m_Skeleton[rootID_B] = TransformChannelProperties.Default;
            m_Skeleton.Root = rootID_A;

            Assert.That(m_Skeleton.ActiveTransformChannelCount, Is.EqualTo(2));

            m_Skeleton.RemoveTransformChannelAndDescendants(rootID_B);

            Assert.That(m_Skeleton.ActiveTransformChannelCount, Is.EqualTo(1));
        }

        [Test]
        public void Skeleton_AddBone_WhenChannelAlreadyExists_UpdatesChannel()
        {
            foreach (var property in s_TransformProperties)
            {
                m_Skeleton[property.ID] = property.Properties;

                var newProperties = new TransformChannelProperties
                {
                    DefaultTranslationValue = property.Properties.DefaultTranslationValue + new float3(0f, 0f, 1f),
                    DefaultRotationValue = math.mul(property.Properties.DefaultRotationValue, quaternion.Euler(0f, 0f, 45f)),
                    DefaultScaleValue = new float3(0.5f)
                };

                m_Skeleton[property.ID] = newProperties;

                var properties = m_Skeleton[property.ID];

                Assert.That(properties, Is.EqualTo(newProperties));
            }

            var transformChannels = new List<TransformChannel>();
            m_Skeleton.GetAllTransforms(transformChannels, TransformChannelSearchMode.ActiveRootDescendants);

            Assert.That(transformChannels, Has.Count.EqualTo(s_TransformProperties.Length));
        }

        [Test]
        public void Skeleton_RemoveBone_WhenChannelDoesntExist_Returns_False(
            [ValueSource(nameof(s_TransformProperties))] TransformChannel channel
        )
        {
            Assume.That(m_Skeleton.Contains(channel.ID, TransformChannelSearchMode.ActiveAndInactiveAll), Is.False, "Bone defined on skeleton by default.");

            var actual = m_Skeleton.RemoveTransformChannelAndDescendants(channel.ID);

            Assert.That(actual, Is.False, "Removing nonexistent bone reported success.");
        }

        [Test]
        public void Skeleton_AddBone_WhenNoAncestorsExist_AddsAncestorsWithDefaultValues()
        {
            Assume.That(m_Skeleton.ActiveTransformChannelCount, Is.EqualTo(0), "One or more active bones defined on skeleton by default.");
            Assume.That(m_Skeleton.InactiveTransformChannelCount, Is.EqualTo(0), "One or more inactive bones defined on skeleton by default.");

            var root = new TransformChannel
            {
                ID = TransformBindingID.Root,
                Properties = TransformChannelProperties.Default
            };
            var a = new TransformChannel
            {
                ID = new TransformBindingID { Path = "A" },
                Properties = TransformChannelProperties.Default
            };
            var b = new TransformChannel
            {
                ID = new TransformBindingID { Path = "A/B" },
                Properties = TransformChannelProperties.Default
            };
            var c = new TransformChannel
            {
                ID = new TransformBindingID { Path = "A/B/C" },
                Properties = new TransformChannelProperties
                {
                    DefaultTranslationValue = new float3(1f, 2f, 3f),
                    DefaultRotationValue = quaternion.Euler(4f, 5f, 6f),
                    DefaultScaleValue = new float3(7f, 8f, 9f)
                }
            };
            m_Skeleton[c.ID] = c.Properties;
            var activeTransformChannels = new List<TransformChannel>();
            m_Skeleton.GetAllTransforms(activeTransformChannels, TransformChannelSearchMode.ActiveRootDescendants);

            Assert.That(activeTransformChannels, Is.EqualTo(new[] { root, a, b, c }), "One or more bones initialized incorrectly.");
        }

        [Test]
        public void Skeleton_AddBone_WhenAncestorsExistAndAreInactive_NewBoneAutomaticallyInactive()
        {
            var root = new TransformChannel
            {
                ID = TransformBindingID.Root,
                Properties = TransformChannelProperties.Default
            };
            var a = new TransformChannel
            {
                ID = new TransformBindingID { Path = "A" },
                Properties = TransformChannelProperties.Default
            };
            var b = new TransformChannel
            {
                ID = new TransformBindingID { Path = "A/B" },
                Properties = TransformChannelProperties.Default
            };
            m_Skeleton[root.ID] = root.Properties;
            m_Skeleton[a.ID] = a.Properties;
            m_Skeleton[b.ID] = b.Properties;
            m_Skeleton.SetTransformChannelDescendantsToInactive(TransformBindingID.Root, includeSelf: true);
            Assume.That(m_Skeleton.ActiveTransformChannelCount, Is.EqualTo(0), "One or more active bones defined on skeleton.");
            Assert.That(m_Skeleton.GetTransformChannelState(root.ID), Is.EqualTo(TransformChannelState.Inactive), "Newly added bone or one of its ancestors not present as inactive.");
            Assert.That(m_Skeleton.GetTransformChannelState(a.ID), Is.EqualTo(TransformChannelState.Inactive), "Newly added bone or one of its ancestors not present as inactive.");
            Assert.That(m_Skeleton.GetTransformChannelState(b.ID), Is.EqualTo(TransformChannelState.Inactive), "Newly added bone or one of its ancestors not present as inactive.");

            var c = new TransformChannel
            {
                ID = new TransformBindingID { Path = "A/B/C" },
                Properties = TransformChannelProperties.Default
            };
            m_Skeleton[c.ID] = c.Properties;

            Assert.That(m_Skeleton.ActiveTransformChannelCount, Is.EqualTo(0), "One or more active bones defined on skeleton.");
            Assert.That(m_Skeleton.GetTransformChannelState(root.ID), Is.EqualTo(TransformChannelState.Inactive), "Newly added bone or one of its ancestors not present as inactive.");
            Assert.That(m_Skeleton.GetTransformChannelState(a.ID), Is.EqualTo(TransformChannelState.Inactive), "Newly added bone or one of its ancestors not present as inactive.");
            Assert.That(m_Skeleton.GetTransformChannelState(b.ID), Is.EqualTo(TransformChannelState.Inactive), "Newly added bone or one of its ancestors not present as inactive.");
            Assert.That(m_Skeleton.GetTransformChannelState(c.ID), Is.EqualTo(TransformChannelState.Inactive), "Newly added bone or one of its ancestors not present as inactive.");
        }

        static Tuple<GenericBindingID, GenericPropertyVariant>[] s_GenericProperties =
        {
            new Tuple<GenericBindingID, GenericPropertyVariant>(
                new GenericBindingID {AttributeName = nameof(DummyGenericPropertyComponent.MyFloat), ComponentType = typeof(DummyGenericPropertyComponent), Path = "", ValueType = GenericPropertyType.Float},
                new GenericPropertyVariant {Float = 3.1416f}),
            new Tuple<GenericBindingID, GenericPropertyVariant>(
                new GenericBindingID {AttributeName = nameof(DummyGenericPropertyComponent.MyInt), ComponentType = typeof(DummyGenericPropertyComponent), Path = "", ValueType = GenericPropertyType.Int},
                new GenericPropertyVariant {Int = 42}),
            new Tuple<GenericBindingID, GenericPropertyVariant>(
                new GenericBindingID {AttributeName = nameof(DummyGenericPropertyComponent.MyQuat), ComponentType = typeof(DummyGenericPropertyComponent), Path = "", ValueType = GenericPropertyType.Quaternion},
                new GenericPropertyVariant {Quaternion = quaternion.identity}),
        };

        static Tuple<GenericBindingID, GenericPropertyVariant>[] s_TupleGenericProperties =
        {
            new Tuple<GenericBindingID, GenericPropertyVariant>(
                new GenericBindingID {AttributeName = nameof(DummyGenericPropertyComponent.MyFloat2), ComponentType = typeof(DummyGenericPropertyComponent), Path = "", ValueType = GenericPropertyType.Float2},
                new GenericPropertyVariant {Float2 = new float2(5f, 10f)}),
            new Tuple<GenericBindingID, GenericPropertyVariant>(
                new GenericBindingID {AttributeName = nameof(DummyGenericPropertyComponent.MyFloat3), ComponentType = typeof(DummyGenericPropertyComponent), Path = "", ValueType = GenericPropertyType.Float3},
                new GenericPropertyVariant {Float3 = new float3(15f, 20f, 25f)})
        };

        [Test]
        [TestCaseSource(nameof(s_GenericProperties))]
        [TestCaseSource(nameof(s_TupleGenericProperties))]
        public void Skeleton_AddGenericProperty_AppendsTo_GenericChannels(Tuple<GenericBindingID, GenericPropertyVariant> property)
        {
            m_Skeleton.AddOrSetGenericProperty(property.Item1, property.Item2);

            AssertThatGenericChannelExists(m_Skeleton, property.Item1, property.Item2);

            var genericChannels = new List<IGenericChannel>();
            m_Skeleton.GetGenericChannels(genericChannels);
            Assert.That(genericChannels, Has.Count.EqualTo(property.Item2.Type.GetNumberOfChannels()));
        }

        [Test]
        [TestCaseSource(nameof(s_GenericProperties))]
        [TestCaseSource(nameof(s_TupleGenericProperties))]
        public void Skeleton_RemoveGenericProperty_RemovesFrom_GenericChannels(Tuple<GenericBindingID, GenericPropertyVariant> property)
        {
            m_Skeleton.AddOrSetGenericProperty(property.Item1, property.Item2);
            var retValue = m_Skeleton.RemoveGenericProperty(property.Item1);
            Assume.That(retValue, Is.True);

            AssertThatGenericChannelDoesntExist(m_Skeleton, property.Item1);

            var genericChannels = new List<IGenericChannel>();
            m_Skeleton.GetGenericChannels(genericChannels);
            Assert.That(genericChannels, Is.Empty);
        }

        [Test]
        [TestCaseSource(nameof(s_GenericProperties))]
        public void Skeleton_AddGenericProperty_WhenChannelAlreadyExists_UpdatesChannel(Tuple<GenericBindingID, GenericPropertyVariant> property)
        {
            m_Skeleton.AddOrSetGenericProperty(property.Item1, property.Item2);

            var newValueBuffer = new float4(3f, 2f, 1f, 0f);

            var newValue = new GenericPropertyVariant {Float4 = newValueBuffer};
            newValue.Type = property.Item2.Type;

            m_Skeleton.AddOrSetGenericProperty(property.Item1, newValue);

            var channels = new List<IGenericChannel>();
            m_Skeleton.QueryGenericPropertyChannels(property.Item1, channels);
            Assert.That(channels, Is.Not.Null);
            Assert.That(channels, Has.Count.EqualTo(property.Item2.Type.GetNumberOfChannels()));

            Assert.That(newValue, Is.EqualTo(channels[0].DefaultValue));

            var genericChannels = new List<IGenericChannel>();
            m_Skeleton.GetGenericChannels(genericChannels);

            Assert.That(genericChannels, Has.Count.EqualTo(property.Item2.Type.GetNumberOfChannels()));
        }

        [TestCaseSource(nameof(s_TupleGenericProperties))]
        public void Skeleton_AddTupleGenericProperty_WhenChannelAlreadyExists_UpdatesChannel(Tuple<GenericBindingID, GenericPropertyVariant> property)
        {
            m_Skeleton.AddOrSetGenericProperty(property.Item1, property.Item2);

            var newValueBuffer = new float4(3f, 2f, 1f, 0f);

            var newValue = new GenericPropertyVariant {Float4 = newValueBuffer};
            newValue.Type = property.Item2.Type;

            m_Skeleton.AddOrSetGenericProperty(property.Item1, newValue);

            var channels = new List<IGenericChannel>();
            m_Skeleton.QueryGenericPropertyChannels(property.Item1, channels);
            Assert.That(channels, Is.Not.Null);
            Assert.That(channels, Has.Count.EqualTo(property.Item2.Type.GetNumberOfChannels()));

            for (int i = 0; i < channels.Count; ++i)
            {
                Assert.That(newValueBuffer[i], Is.EqualTo(channels[i].DefaultValue.Float));
            }

            var genericChannels = new List<IGenericChannel>();
            m_Skeleton.GetGenericChannels(genericChannels);

            Assert.That(genericChannels, Has.Count.EqualTo(property.Item2.Type.GetNumberOfChannels()));
        }

        [Test]
        [TestCaseSource(nameof(s_GenericProperties))]
        [TestCaseSource(nameof(s_TupleGenericProperties))]
        public void Skeleton_RemoveGenericProperty_WhenChannelDoesntExist_Returns_False(Tuple<GenericBindingID, GenericPropertyVariant> property)
        {
            var genericChannels = new List<IGenericChannel>();
            m_Skeleton.GetGenericChannels(genericChannels);

            Assume.That(genericChannels, Is.Empty);

            var retValue = m_Skeleton.RemoveGenericProperty(property.Item1);
            Assert.That(retValue, Is.False);
        }

        [Test]
        [TestCaseSource(nameof(s_TupleGenericProperties))]
        public void Skeleton_AddSingleChannel_FromTupleProperty_AppendsTo_GenericChannels(Tuple<GenericBindingID, GenericPropertyVariant> property)
        {
            // Adding a single channel in a tuple property should only add that channel.
            m_Skeleton.AddOrSetGenericProperty(property.Item1[0], property.Item2[0]);

            var genericChannels = new List<IGenericChannel>();
            m_Skeleton.GetGenericChannels(genericChannels);

            Assume.That(genericChannels, Has.Count.EqualTo(1));

            // Adding the full property afterwards shouldn't create duplicate channels.
            m_Skeleton.AddOrSetGenericProperty(property.Item1, property.Item2);

            genericChannels.Clear();
            m_Skeleton.GetGenericChannels(genericChannels);

            Assert.That(genericChannels, Has.Count.EqualTo(property.Item2.Type.GetNumberOfChannels()));
        }

        [Test]
        [TestCaseSource(nameof(s_TupleGenericProperties))]
        public void Skeleton_RemoveSingleChannel_FromTupleProperty_RemovesFrom_GenericChannels(Tuple<GenericBindingID, GenericPropertyVariant> property)
        {
            m_Skeleton.AddOrSetGenericProperty(property.Item1, property.Item2);

            var genericChannels = new List<IGenericChannel>();
            m_Skeleton.GetGenericChannels(genericChannels);

            Assume.That(genericChannels, Has.Count.EqualTo(property.Item2.Type.GetNumberOfChannels()));

            // Remove a single channel from the tuple property.
            var retValue = m_Skeleton.RemoveGenericProperty(property.Item1[0]);
            Assume.That(retValue, Is.True);

            genericChannels.Clear();
            m_Skeleton.GetGenericChannels(genericChannels);

            Assert.That(genericChannels, Has.Count.EqualTo(property.Item2.Type.GetNumberOfChannels() - 1));
        }

        // Skeleton Conversion
        [Test]
        public void Skeleton_ToRigBuilderData_MatchTransformChannels()
        {
            foreach (var property in s_TransformProperties)
            {
                m_Skeleton[property.ID] = property.Properties;
                AssumeThatTransformChannelExistsAndIsActive(m_Skeleton, property.ID, property.Properties);
            }

            var hasher = Hybrid.BindingHashGlobals.DefaultHashGenerator;
            using (var rigBuilderData = m_Skeleton.ToRigBuilderData(hasher, Allocator.Temp))
            {
                Assert.That(rigBuilderData.SkeletonNodes, Has.Length.EqualTo(s_TransformProperties.Length));
                foreach (var property in s_TransformProperties)
                {
                    AssertThatRigBuilderSkeletonNodeMatchesTransformChannel(rigBuilderData, hasher.ToHash(property.ID), property.Properties.DefaultTranslationValue, property.Properties.DefaultRotationValue, property.Properties.DefaultScaleValue);
                }
            }
        }

        [Test]
        public void Skeleton_ToRigBuilderData_WithSkeletonRoot_MatchTransformChannels()
        {
            foreach (var property in s_TransformProperties)
            {
                m_Skeleton[property.ID] = property.Properties;
                AssumeThatTransformChannelExistsAndIsActive(m_Skeleton, property.ID, property.Properties);
            }

            m_Skeleton.Root = s_TransformBindings[1];

            var transformPropertiesSubSet = new[]
            {
                s_TransformProperties[1], // Root/A
                s_TransformProperties[2]  // Root/A/B
            };

            var hasher = Hybrid.BindingHashGlobals.DefaultHashGenerator;
            using (var rigBuilderData = m_Skeleton.ToRigBuilderData(hasher, Allocator.Temp))
            {
                Assert.That(rigBuilderData.SkeletonNodes, Has.Length.EqualTo(transformPropertiesSubSet.Length));
                foreach (var property in transformPropertiesSubSet)
                {
                    AssertThatRigBuilderSkeletonNodeMatchesTransformChannel(rigBuilderData, hasher.ToHash(property.ID), property.Properties.DefaultTranslationValue, property.Properties.DefaultRotationValue, property.Properties.DefaultScaleValue);
                }
            }
        }
    }
}
