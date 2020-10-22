using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Animation.Authoring;
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
            var transformChannels = skeletonAsset.ActiveTransformChannels.ToList();

            var channelIndex = transformChannels.FindIndex(channel => channel.ID.Equals(id));

            Assume.That(skeletonAsset.GetTransformChannelState(id) == TransformChannelState.Active);
            Assume.That(channelIndex, Is.Not.EqualTo(-1));
            Assume.That(transformChannels[channelIndex].Properties, Is.EqualTo(expectedProperties), "Transform properties did not match.");
        }

        private void AssertThatTransformChannelExistsAndIsActive(Authoring.Skeleton skeletonAsset, TransformBindingID id, TransformChannelProperties expectedProperties)
        {
            var transformChannels = skeletonAsset.ActiveTransformChannels.ToList();

            var channelIndex = transformChannels.FindIndex(channel => channel.ID.Equals(id));

            Assert.AreEqual(TransformChannelState.Active, skeletonAsset.GetTransformChannelState(id));
            Assert.That(channelIndex, Is.Not.EqualTo(-1));
            Assert.That(transformChannels[channelIndex].Properties, Is.EqualTo(expectedProperties), "Transform properties did not match.");
        }

        private void AssertThatTransformChannelExistsAndIsInactive(Authoring.Skeleton skeletonAsset, TransformBindingID id, TransformChannelProperties expectedProperties)
        {
            var transformChannels = skeletonAsset.InactiveTransformChannels.ToList();

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
                case GenericPropertyType.Float:
                    var floatChannels = m_Skeleton.FloatChannels;
                    return floatChannels.Cast<IGenericChannel>().ToArray();
                case GenericPropertyType.Int:
                    var intChannels = m_Skeleton.IntChannels;
                    return intChannels.Cast<IGenericChannel>().ToArray();
                case GenericPropertyType.Quaternion:
                    var quaternionChannels = m_Skeleton.QuaternionChannels;
                    return quaternionChannels.Cast<IGenericChannel>().ToArray();
            }

            return Array.Empty<IGenericChannel>();
        }

        [SetUp]
        public void SetUp()
        {
            m_Skeleton = ScriptableObject.CreateInstance<Authoring.Skeleton>();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Skeleton)
                ScriptableObject.DestroyImmediate(m_Skeleton);
        }

        private static TransformBindingID[] s_TransformBindings =
        {
            new TransformBindingID {Path = "Root"},
            new TransformBindingID {Path = "Root/A"},
            new TransformBindingID {Path = "Root/A/B"},
            new TransformBindingID {Path = "Root/B"},
        };

        private static TransformChannel[] s_TransformProperties =
        {
            new TransformChannel { ID = s_TransformBindings[0], Properties = new TransformChannelProperties { DefaultTranslationValue = float3.zero, DefaultRotationValue = quaternion.identity, DefaultScaleValue = new float3(1f) } },
            new TransformChannel { ID = s_TransformBindings[1], Properties = new TransformChannelProperties { DefaultTranslationValue = new float3(1f, 0f, 0f), DefaultRotationValue = quaternion.Euler(25f, 0f, 0f), DefaultScaleValue = new float3(1f) } },
            new TransformChannel { ID = s_TransformBindings[2], Properties = new TransformChannelProperties { DefaultTranslationValue = new float3(-1f, 0f, 0f), DefaultRotationValue = quaternion.Euler(-25f, 0f, 0f), DefaultScaleValue = new float3(1f) } },
            new TransformChannel { ID = s_TransformBindings[3], Properties = new TransformChannelProperties { DefaultTranslationValue = new float3(0f, 1f, 0f), DefaultRotationValue = quaternion.Euler(0f, 0f, 90f), DefaultScaleValue = new float3(1f) } }
        };

        // Skeleton management.
        [Test]
        public void Skeleton_AddBone_AppendsTo_TransformChannels()
        {
            foreach (var property in s_TransformProperties)
            {
                m_Skeleton[property.ID] = property.Properties;
                AssumeThatTransformChannelExistsAndIsActive(m_Skeleton, property.ID, property.Properties);
            }

            var transformChannels = m_Skeleton.ActiveTransformChannels;

            Assert.That(transformChannels, Has.Count.EqualTo(s_TransformProperties.Length));
        }

        [Test]
        public void Skeleton_RemoveBone_RemovesFrom_TransformChannels(
            [ValueSource(nameof(s_TransformProperties))] TransformChannel channel
        )
        {
            m_Skeleton[channel.ID] = channel.Properties;
            Assume.That(m_Skeleton.Contains(channel.ID), Is.True, "Skeleton did not contain bone to be removed.");

            var retValue = m_Skeleton.RemoveTransformChannelAndDescendants(channel.ID);
            Assume.That(retValue, Is.True, "Removing bone returned failure.");

            Assert.That(m_Skeleton.Contains(channel.ID), Is.False, "Bone still defined on skeleton.");
        }

        [Test]
        public void Skeleton_RemoveBone_WhenDescendentsAreInactive_DescendantsAreRemoved()
        {
            var a = new TransformBindingID { Path = "A" };
            var b = new TransformBindingID { Path = "A/B" };
            var c = new TransformBindingID { Path = "A/B/C" };
            m_Skeleton[c] = default;
            m_Skeleton.SetTransformChannelDescendantsToInactive(b, includeSelf: true);
            Assume.That(m_Skeleton.ActiveTransformChannels.Select(ch => ch.ID), Is.EqualTo(new[] { a }), "Active transforms did not initialize correctly.");
            Assume.That(m_Skeleton.InactiveTransformChannels.Select(ch => ch.ID), Is.EqualTo(new[] { b, c }), "Inactive transforms did not initialize correctly.");

            m_Skeleton.RemoveTransformChannelAndDescendants(a);

            Assert.That(m_Skeleton.ActiveTransformChannels, Is.Empty, "One or more active bones still defined on skeleton.");
            Assert.That(m_Skeleton.InactiveTransformChannels, Is.Empty, "One or more inactive bones still defined on skeleton.");
        }

        [Test]
        public void Skeleton_RemoveTransformChannelAndDescendants_DescendantsRemoved()
        {
            var a = new TransformBindingID { Path = "A" };
            var b = new TransformBindingID { Path = "A/B" };
            var c = new TransformBindingID { Path = "A/B/C" };
            m_Skeleton[c] = default;
            Assume.That(m_Skeleton.ActiveTransformChannels.Select(ch => ch.ID), Is.EqualTo(new[] { a, b, c }), "Skeleton did not contain expected hierarchy.");

            var retValue = m_Skeleton.RemoveTransformChannelAndDescendants(a);
            Assume.That(retValue, Is.True, "Removing bone returned failure.");

            Assert.That(m_Skeleton.ActiveTransformChannels, Is.Empty, "One or more bones still defined on skeleton.");
        }

        [Test]
        public void Skeleton_Contains_WhenInvalid_ThrowsArgumentException() =>
            Assert.Throws<ArgumentException>(() => m_Skeleton.Contains(new TransformBindingID { Path = null }));

        [Test]
        public void Skeleton_Contains_WhenDoesNotExist_ReturnsFalse([Values("", "A", "A/B", "/")] string path)
        {
            Assume.That(m_Skeleton.ActiveTransformChannels, Is.Empty, "One or more active bones defined on skeleton by default.");
            Assume.That(m_Skeleton.InactiveTransformChannels, Is.Empty, "One or more inactive bones defined on skeleton by default.");
            var id = new TransformBindingID { Path = path };

            var actual = m_Skeleton.Contains(id);
            Assert.That(actual, Is.False, "Undefined bone located on skeleton definition.");
        }

        [Test]
        public void Skeleton_Contains_WhenExistsActive_ReturnsTrue([Values("", "A", "A/B", "/")] string path)
        {
            Assume.That(m_Skeleton.ActiveTransformChannels, Is.Empty, "One or more active bones defined on skeleton by default.");
            Assume.That(m_Skeleton.InactiveTransformChannels, Is.Empty, "One or more inactive bones defined on skeleton by default.");
            var id = new TransformBindingID { Path = path };

            m_Skeleton[id] = default;
            Assume.That(m_Skeleton.ActiveTransformChannels.Select(c => c.ID), Contains.Item(id), "Newly added bone absent from skeleton definition.");

            var actual = m_Skeleton.Contains(id);
            Assert.That(actual, Is.True, "Failed to locate newly added (active) bone.");
        }

        [Test]
        public void Skeleton_Contains_WhenExistsInactive_ReturnsTrue([Values("", "A", "A/B", "/")] string path)
        {
            var id = new TransformBindingID { Path = path };
            Assume.That(m_Skeleton.Contains(id), Is.False, "Bone defined on skeleton by default.");
            m_Skeleton[id] = default;
            m_Skeleton.SetTransformChannelDescendantsToInactive(id);
            Assume.That(m_Skeleton.InactiveTransformChannels.Select(c => c.ID), Contains.Item(id), "Inactive bone not defined.");

            var actual = m_Skeleton.Contains(id);
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
            var rootID = new TransformBindingID { Path = "Root" };
            var rootID_A = new TransformBindingID { Path = "Root/A" };
            var rootID_B = new TransformBindingID { Path = "Root/B" };
            var rootID_B_C = new TransformBindingID { Path = "Root/B/C" };
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
            var rootID = new TransformBindingID { Path = "Root" };
            var rootID_A = new TransformBindingID { Path = "Root/A" };
            var rootID_B = new TransformBindingID { Path = "Root/B" };
            var rootID_B_C = new TransformBindingID { Path = "Root/B/C" };
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
            var rootID = new TransformBindingID { Path = "Root" };
            var rootID_A = new TransformBindingID { Path = "Root/A" };
            var rootID_B = new TransformBindingID { Path = "Root/B" };
            var rootID_B_C = new TransformBindingID { Path = "Root/B/C" };
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
            var rootID = new TransformBindingID { Path = "Root" };
            var rootID_A = new TransformBindingID { Path = "Root/A" };
            var rootID_B = new TransformBindingID { Path = "Root/B" };
            var rootID_B_C = new TransformBindingID { Path = "Root/B/C" };
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
            var rootID = new TransformBindingID { Path = "Root" };
            var rootID_A = new TransformBindingID { Path = "Root/A" };
            var rootID_B = new TransformBindingID { Path = "Root/B" };
            var rootID_B_C = new TransformBindingID { Path = "Root/B/C" };
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
            var rootID = new TransformBindingID { Path = "Root" };
            var rootID_A = new TransformBindingID { Path = "Root/A" };
            var rootID_B = new TransformBindingID { Path = "Root/B" };
            var rootID_B_C = new TransformBindingID { Path = "Root/B/C" };
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
            var rootID = new TransformBindingID { Path = "Root" };
            var rootID_A = new TransformBindingID { Path = "Root/A" };
            var rootID_B = new TransformBindingID { Path = "Root/B" };
            var rootID_B_C = new TransformBindingID { Path = "Root/B/C" };
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
            var rootID = new TransformBindingID { Path = "Root" };
            var rootID_A = new TransformBindingID { Path = "Root/A" };
            var rootID_B = new TransformBindingID { Path = "Root/B" };
            var rootID_B_C = new TransformBindingID { Path = "Root/B/C" };
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
            var rootID = new TransformBindingID { Path = "Root" };
            var rootID_A = new TransformBindingID { Path = "Root/A" };
            var rootID_B = new TransformBindingID { Path = "Root/B" };
            var rootID_B_C = new TransformBindingID { Path = "Root/B/C" };
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
            var rootID = new TransformBindingID { Path = "Root" };
            var rootID_A = new TransformBindingID { Path = "Root/A" };
            var rootID_B = new TransformBindingID { Path = "Root/B" };
            var rootID_B_C = new TransformBindingID { Path = "Root/B/C" };
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
            var rootID = new TransformBindingID { Path = "Root" };
            var rootID_A = new TransformBindingID { Path = "Root/A" };
            var rootID_B = new TransformBindingID { Path = "Root/B" };
            var rootID_B_C = new TransformBindingID { Path = "Root/B/C" };

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

            m_Skeleton.Root = rootID_B;

            Assert.That(m_Skeleton.QueryTransformIndex(rootID), Is.EqualTo(-1));
            Assert.That(m_Skeleton.QueryTransformIndex(rootID_A), Is.EqualTo(-1));
            Assert.That(m_Skeleton.QueryTransformIndex(rootID_B), Is.EqualTo(0));
            Assert.That(m_Skeleton.QueryTransformIndex(rootID_B_C), Is.EqualTo(1));
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

            var transformChannels = m_Skeleton.ActiveTransformChannels;

            Assert.That(transformChannels, Has.Count.EqualTo(s_TransformProperties.Length));
        }

        [Test]
        public void Skeleton_RemoveBone_WhenChannelDoesntExist_Returns_False(
            [ValueSource(nameof(s_TransformProperties))] TransformChannel channel
        )
        {
            Assume.That(m_Skeleton.Contains(channel.ID), Is.False, "Bone defined on skeleton by default.");

            var actual = m_Skeleton.RemoveTransformChannelAndDescendants(channel.ID);

            Assert.That(actual, Is.False, "Removing nonexistent bone reported success.");
        }

        [Test]
        public void Skeleton_AddBone_WhenNoAncestorsExist_AddsAncestorsWithDefaultValues()
        {
            Assume.That(m_Skeleton.ActiveTransformChannels, Is.Empty, "One or more active bones defined on skeleton by default.");
            Assume.That(m_Skeleton.InactiveTransformChannels, Is.Empty, "One or more inactive bones defined on skeleton by default.");

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

            Assert.That(m_Skeleton.ActiveTransformChannels, Is.EqualTo(new[] { a, b, c }), "One or more bones initialized incorrectly.");
        }

        [Test]
        public void Skeleton_AddBone_WhenAncestorsExistAndAreInactive_NewBoneAutomaticallyInactive()
        {
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
            m_Skeleton[a.ID] = a.Properties;
            m_Skeleton[b.ID] = b.Properties;
            m_Skeleton.SetTransformChannelDescendantsToInactive(a.ID, includeSelf: true);
            Assume.That(m_Skeleton.ActiveTransformChannels, Is.Empty, "One or more active bones defined on skeleton.");
            Assume.That(m_Skeleton.InactiveTransformChannels, Is.EqualTo(new[] { a, b }), "Inactive ancestors not present on skeleton.");

            var c = new TransformChannel
            {
                ID = new TransformBindingID { Path = "A/B/C" },
                Properties = TransformChannelProperties.Default
            };
            m_Skeleton[c.ID] = c.Properties;

            Assert.That(m_Skeleton.ActiveTransformChannels, Is.Empty, "One or more active bones defined on skeleton.");
            Assert.That(m_Skeleton.InactiveTransformChannels, Is.EqualTo(new[] { a, b, c }), "Newly added bone or one of its ancestors not present as inactive.");
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

        [Test]
        public void Skeleton_RemoveBone_RemovesTransformLabel()
        {
            foreach (var property in s_TransformProperties)
            {
                m_Skeleton[property.ID] = property.Properties;
                var retValue = m_Skeleton.AddTransformLabel(property.ID, TransformLabel.Create(property.ID.Name));
                Assume.That(retValue, Is.True);
            }

            foreach (var property in s_TransformProperties)
            {
                m_Skeleton.RemoveTransformChannelAndDescendants(property.ID);
                Assume.That(m_Skeleton.Contains(property.ID), Is.False);

                var labels = new List<TransformLabel>();
                m_Skeleton.QueryTransformLabels(property.ID, labels);
                Assert.That(labels, Is.Empty);
            }

            var labelReferences = m_Skeleton.TransformLabels;

            Assert.That(labelReferences, Is.Empty);
        }

        [Test]
        [TestCaseSource(nameof(s_GenericProperties))]
        [TestCaseSource(nameof(s_TupleGenericProperties))]
        public void Skeleton_RemoveGenericProperty_RemovesGenericPropertyLabel(Tuple<GenericBindingID, GenericPropertyVariant> property)
        {
            m_Skeleton.AddOrSetGenericProperty(property.Item1, property.Item2);

            var newLabel = GenericPropertyLabel.Create(property.Item1.AttributeName);
            newLabel.ValueType = property.Item1.ValueType;

            var retValue = m_Skeleton.AddGenericPropertyLabel(property.Item1, newLabel);
            Assume.That(retValue, Is.True);

            retValue = m_Skeleton.RemoveGenericProperty(property.Item1);
            Assume.That(retValue, Is.True);

            var labels = new List<GenericPropertyLabel>();
            m_Skeleton.QueryGenericPropertyLabels(property.Item1, labels);
            Assert.That(labels, Is.Empty);

            var allLabels = m_Skeleton.GenericPropertyLabels;

            Assert.That(allLabels, Is.Empty);
        }

        // SkeletonLabelSet labels management.
        // Cannot add duplicate labels
        // Cannot assign the same label to multiple bindings.
        [Test]
        public void Skeleton_AddTransformLabel_LabelApplied()
        {
            var property = s_TransformProperties[0];

            m_Skeleton[property.ID] = property.Properties;

            var newLabel = ScriptableObject.CreateInstance<TransformLabel>();
            try
            {
                var retValue = m_Skeleton.AddTransformLabel(property.ID, newLabel);
                Assume.That(retValue, Is.True);

                var labels = new List<TransformLabel>();
                m_Skeleton.QueryTransformLabels(property.ID, labels);
                Assert.That(labels, Contains.Item(newLabel));
            }
            finally
            {
                ScriptableObject.DestroyImmediate(newLabel);
            }
        }

        [Test]
        public void Skeleton_RemoveTransformLabel_LabelRemoved()
        {
            var property = s_TransformProperties[0];

            m_Skeleton[property.ID] = property.Properties;

            var newLabel = ScriptableObject.CreateInstance<TransformLabel>();
            try
            {
                var retValue = m_Skeleton.AddTransformLabel(property.ID, newLabel);
                Assume.That(retValue, Is.True);

                retValue = m_Skeleton.RemoveTransformLabel(newLabel);
                Assume.That(retValue, Is.True);

                var labels = new List<TransformLabel>();
                m_Skeleton.QueryTransformLabels(property.ID, labels);
                Assert.That(labels, Does.Not.Contains(newLabel));
            }
            finally
            {
                ScriptableObject.DestroyImmediate(newLabel);
            }
        }

        [Test]
        public void Skeleton_AddTransformLabel_IsDuplicate_Returns_False()
        {
            var property = s_TransformProperties[0];

            m_Skeleton[property.ID] = property.Properties;

            var newLabel = ScriptableObject.CreateInstance<TransformLabel>();
            try
            {
                var retValue = m_Skeleton.AddTransformLabel(property.ID, newLabel);
                Assume.That(retValue, Is.True);

                retValue = m_Skeleton.AddTransformLabel(property.ID, newLabel);
                Assert.That(retValue, Is.False);
            }
            finally
            {
                ScriptableObject.DestroyImmediate(newLabel);
            }
        }

        [Test]
        public void Skeleton_AddTransformLabel_IsAssociatedToAnotherChannel_Returns_False()
        {
            var property1 = s_TransformProperties[0];
            var property2 = s_TransformProperties[1];

            m_Skeleton[property1.ID] = property1.Properties;

            m_Skeleton[property2.ID] = property2.Properties;

            var newLabel = ScriptableObject.CreateInstance<TransformLabel>();
            try
            {
                var retValue = m_Skeleton.AddTransformLabel(property1.ID, newLabel);
                Assume.That(retValue, Is.True);

                retValue = m_Skeleton.AddTransformLabel(property2.ID, newLabel);
                Assert.That(retValue, Is.False);
            }
            finally
            {
                ScriptableObject.DestroyImmediate(newLabel);
            }
        }

        [Test]
        public void Skeleton_AddTransformLabel_TransformChannelDoesntExist_Returns_False()
        {
            var property = s_TransformProperties[0];

            var newLabel = ScriptableObject.CreateInstance<TransformLabel>();
            try
            {
                var retValue = m_Skeleton.AddTransformLabel(property.ID, newLabel);
                Assert.That(retValue, Is.False);
            }
            finally
            {
                ScriptableObject.DestroyImmediate(newLabel);
            }
        }

        [Test]
        [TestCaseSource(nameof(s_GenericProperties))]
        [TestCaseSource(nameof(s_TupleGenericProperties))]
        public void Skeleton_AddGenericPropertyLabel(Tuple<GenericBindingID, GenericPropertyVariant> property)
        {
            m_Skeleton.AddOrSetGenericProperty(property.Item1, property.Item2);

            var newLabel = ScriptableObject.CreateInstance<GenericPropertyLabel>();
            newLabel.ValueType = property.Item1.ValueType;

            try
            {
                var retValue = m_Skeleton.AddGenericPropertyLabel(property.Item1, newLabel);
                Assume.That(retValue, Is.True);

                var labels = new List<GenericPropertyLabel>();
                m_Skeleton.QueryGenericPropertyLabels(property.Item1, labels);
                Assert.That(labels, Contains.Item(newLabel));
            }
            finally
            {
                ScriptableObject.DestroyImmediate(newLabel);
            }
        }

        [Test]
        [TestCaseSource(nameof(s_GenericProperties))]
        [TestCaseSource(nameof(s_TupleGenericProperties))]
        public void Skeleton_RemoveGenericPropertyLabel(Tuple<GenericBindingID, GenericPropertyVariant> property)
        {
            m_Skeleton.AddOrSetGenericProperty(property.Item1, property.Item2);

            var newLabel = ScriptableObject.CreateInstance<GenericPropertyLabel>();
            newLabel.ValueType = property.Item1.ValueType;

            try
            {
                var retValue = m_Skeleton.AddGenericPropertyLabel(property.Item1, newLabel);
                Assume.That(retValue, Is.True);

                retValue = m_Skeleton.RemoveGenericPropertyLabel(newLabel);
                Assume.That(retValue, Is.True);

                var labels = new List<GenericPropertyLabel>();
                m_Skeleton.QueryGenericPropertyLabels(property.Item1, labels);
                Assert.That(labels, Does.Not.Contains(newLabel));
            }
            finally
            {
                ScriptableObject.DestroyImmediate(newLabel);
            }
        }

        [Test]
        [TestCaseSource(nameof(s_GenericProperties))]
        [TestCaseSource(nameof(s_TupleGenericProperties))]
        public void Skeleton_AddGenericPropertyLabel_IsDuplicate_Returns_False(Tuple<GenericBindingID, GenericPropertyVariant> property)
        {
            m_Skeleton.AddOrSetGenericProperty(property.Item1, property.Item2);

            var newLabel = ScriptableObject.CreateInstance<GenericPropertyLabel>();
            newLabel.ValueType = property.Item1.ValueType;

            try
            {
                var retValue = m_Skeleton.AddGenericPropertyLabel(property.Item1, newLabel);
                Assume.That(retValue, Is.True);

                retValue = m_Skeleton.AddGenericPropertyLabel(property.Item1, newLabel);
                Assert.That(retValue, Is.False);
            }
            finally
            {
                ScriptableObject.DestroyImmediate(newLabel);
            }
        }

        [Test]
        [TestCaseSource(nameof(s_GenericProperties))]
        [TestCaseSource(nameof(s_TupleGenericProperties))]
        public void Skeleton_AddGenericPropertyLabel_TransformChannelDoesntExist_Returns_False(Tuple<GenericBindingID, GenericPropertyVariant> property)
        {
            var newLabel = ScriptableObject.CreateInstance<GenericPropertyLabel>();
            try
            {
                var retValue = m_Skeleton.AddGenericPropertyLabel(property.Item1, newLabel);
                Assert.That(retValue, Is.False);
            }
            finally
            {
                ScriptableObject.DestroyImmediate(newLabel);
            }
        }

        [Test]
        [TestCaseSource(nameof(s_GenericProperties))]
        [TestCaseSource(nameof(s_TupleGenericProperties))]
        public void Skeleton_AddGenericPropertyLabel_TypeIsWrong_Returns_False(Tuple<GenericBindingID, GenericPropertyVariant> property)
        {
            m_Skeleton.AddOrSetGenericProperty(property.Item1, property.Item2);

            var newLabel = ScriptableObject.CreateInstance<GenericPropertyLabel>();
            newLabel.ValueType = property.Item1.ValueType + 1;

            try
            {
                var retValue = m_Skeleton.AddGenericPropertyLabel(property.Item1, newLabel);
                Assert.That(retValue, Is.False);
            }
            finally
            {
                ScriptableObject.DestroyImmediate(newLabel);
            }
        }

        // SkeletonLabelSet queries
        // Query indices for Transform bindings
        // Query indices for float/int/quaternion bindings
        // Query indices for tuple bindings
        [Test]
        public void Skeleton_QueryTransformIndex_Returns_ValidChannelIndex()
        {
            foreach (var property in s_TransformProperties)
            {
                m_Skeleton[property.ID] = property.Properties;
            }

            var newLabel = ScriptableObject.CreateInstance<TransformLabel>();

            try
            {
                var queriedProperty = s_TransformProperties[3];

                var retValue = m_Skeleton.AddTransformLabel(queriedProperty.ID, newLabel);
                Assume.That(retValue, Is.True);

                var index = m_Skeleton.QueryTransformIndex(newLabel);
                Assert.That(index, Is.GreaterThanOrEqualTo(0));

                var transformChannels = m_Skeleton.ActiveTransformChannels;

                Assert.That(queriedProperty.ID.Equals(transformChannels[index].ID));
            }
            finally
            {
                ScriptableObject.DestroyImmediate(newLabel);
            }
        }

        [Test]
        [TestCaseSource(nameof(s_GenericProperties))]
        [TestCaseSource(nameof(s_TupleGenericProperties))]
        public void Skeleton_QueryPropertyIndex_Returns_ValidChannelIndex(Tuple<GenericBindingID, GenericPropertyVariant> property)
        {
            m_Skeleton.AddOrSetGenericProperty(property.Item1, property.Item2);

            var newLabel = ScriptableObject.CreateInstance<GenericPropertyLabel>();
            newLabel.ValueType = property.Item1.ValueType;

            try
            {
                var retValue = m_Skeleton.AddGenericPropertyLabel(property.Item1, newLabel);
                Assume.That(retValue, Is.True);

                var channelIndex = m_Skeleton.QueryGenericPropertyIndex(newLabel);
                Assert.That(channelIndex, Is.GreaterThanOrEqualTo(0));

                AssertThatGenericChannelAtIndexMatchesGenericBindingID(m_Skeleton, property.Item1, channelIndex);
            }
            finally
            {
                ScriptableObject.DestroyImmediate(newLabel);
            }
        }

        [Test]
        [TestCaseSource(nameof(s_TupleGenericProperties))]
        public void Skeleton_QueryPropertyIndex_OutOfOrderTuples_Returns_ValidChannelIndex(Tuple<GenericBindingID, GenericPropertyVariant> property)
        {
            // Add all tuple properties in reverse order.
            for (int i = (int)property.Item1.ValueType.GetNumberOfChannels() - 1; i >= 0; --i)
            {
                m_Skeleton.AddOrSetGenericProperty(property.Item1[i], property.Item2[i]);
            }

            var newLabel = ScriptableObject.CreateInstance<GenericPropertyLabel>();
            newLabel.ValueType = property.Item1.ValueType;

            try
            {
                var retValue = m_Skeleton.AddGenericPropertyLabel(property.Item1, newLabel);
                Assume.That(retValue, Is.True);

                var channelIndex = m_Skeleton.QueryGenericPropertyIndex(newLabel);
                Assert.That(channelIndex, Is.GreaterThanOrEqualTo(0));

                AssertThatGenericChannelAtIndexMatchesGenericBindingID(m_Skeleton, property.Item1, channelIndex);
            }
            finally
            {
                ScriptableObject.DestroyImmediate(newLabel);
            }
        }

        [Test]
        public void Skeleton_QueryPropertyIndex_OfPropertyBuffers_Returns_ValidChannelIndex()
        {
            var genericBindings = new GenericBindingID[]
            {
                new GenericBindingID {AttributeName = "MyStruct.a", Path = "", ComponentType = typeof(DummyGenericPropertyComponent), ValueType = GenericPropertyType.Float},
                new GenericBindingID {AttributeName = "MyStruct.b", Path = "", ComponentType = typeof(DummyGenericPropertyComponent), ValueType = GenericPropertyType.Float},
                new GenericBindingID {AttributeName = "MyStruct.c", Path = "", ComponentType = typeof(DummyGenericPropertyComponent), ValueType = GenericPropertyType.Float},
                new GenericBindingID {AttributeName = "MyStruct.d", Path = "", ComponentType = typeof(DummyGenericPropertyComponent), ValueType = GenericPropertyType.Float},
                new GenericBindingID {AttributeName = "MyStruct.e", Path = "", ComponentType = typeof(DummyGenericPropertyComponent), ValueType = GenericPropertyType.Float},
                new GenericBindingID {AttributeName = "MyStruct.f", Path = "", ComponentType = typeof(DummyGenericPropertyComponent), ValueType = GenericPropertyType.Float},
                new GenericBindingID {AttributeName = "MyStruct.g", Path = "", ComponentType = typeof(DummyGenericPropertyComponent), ValueType = GenericPropertyType.Float},
                new GenericBindingID {AttributeName = "MyStruct.h", Path = "", ComponentType = typeof(DummyGenericPropertyComponent), ValueType = GenericPropertyType.Float}
            };

            // Add all properties.
            for (int i = 0; i < genericBindings.Length; ++i)
            {
                m_Skeleton.AddOrSetGenericProperty(genericBindings[i], new GenericPropertyVariant {Float = 0f});
            }

            // Query on a float with a custom size.
            var newLabel = ScriptableObject.CreateInstance<GenericPropertyLabel>();
            newLabel.ValueType = GenericPropertyType.Float;

            try
            {
                var retValue = m_Skeleton.AddGenericPropertyLabel(genericBindings[0], newLabel);
                Assume.That(retValue, Is.True);

                var channelIndex = m_Skeleton.QueryGenericPropertyIndex(newLabel, (uint)genericBindings.Length);
                Assert.That(channelIndex, Is.GreaterThanOrEqualTo(0));

                for (int i = 0; i < genericBindings.Length; ++i)
                {
                    AssertThatGenericChannelAtIndexMatchesGenericBindingID(m_Skeleton, genericBindings[i], channelIndex + i);
                }
            }
            finally
            {
                ScriptableObject.DestroyImmediate(newLabel);
            }
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

            var hasher = new BindingHashGenerator();
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

            var hasher = new BindingHashGenerator();
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
