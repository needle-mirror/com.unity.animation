using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Animation.Authoring;
using Unity.Animation.Authoring.Editor;
using UnityEngine;
using Unity.Mathematics;
using UnityEditor;

namespace Unity.Animation.Tests
{
    class VirtualSkeletonTests
    {
        private Authoring.Skeleton m_Skeleton;
        private VirtualSkeleton m_VirtualSkeleton;

        [SetUp]
        public void SetUp()
        {
            m_Skeleton = ScriptableObject.CreateInstance<Authoring.Skeleton>();
        }

        [TearDown]
        public void TearDown()
        {
            ScriptableObject.DestroyImmediate(m_Skeleton);

            if (m_VirtualSkeleton)
                ScriptableObject.DestroyImmediate(m_VirtualSkeleton);
        }

        static readonly TransformChannel[] s_TransformProperties =
        {
            new TransformChannel { ID = new TransformBindingID { Path = "Root" }, Properties = new TransformChannelProperties { DefaultTranslationValue = float3.zero, DefaultRotationValue = quaternion.identity, DefaultScaleValue = new float3(1f) } },
            new TransformChannel { ID = new TransformBindingID { Path = "Root/A" }, Properties = new TransformChannelProperties { DefaultTranslationValue = new float3(1f, 0f, 0f), DefaultRotationValue = quaternion.Euler(25f, 0f, 0f), DefaultScaleValue = new float3(1f) } },
            new TransformChannel { ID = new TransformBindingID { Path = "Root/B" }, Properties = new TransformChannelProperties { DefaultTranslationValue = new float3(-1f, 0f, 0f), DefaultRotationValue = quaternion.Euler(-25f, 0f, 0f), DefaultScaleValue = new float3(1f) } },
            new TransformChannel { ID = new TransformBindingID { Path = "Root/A/B" }, Properties = new TransformChannelProperties { DefaultTranslationValue = new float3(0f, 1f, 0f), DefaultRotationValue = quaternion.Euler(0f, 0f, 90f), DefaultScaleValue = new float3(1f) } }
        };

        private void CreateSkeleton(TransformChannel[] properties)
        {
            foreach (var property in s_TransformProperties)
            {
                m_Skeleton[property.ID] = property.Properties;
            }
        }

        private void CheckThatVirtualSkeletonMatchesSkeleton(VirtualSkeleton virtualSkeleton)
        {
            var skeleton = virtualSkeleton.Skeleton;
            var transformChannels = skeleton.ActiveTransformChannels.ToList();

            Assert.That(transformChannels, Has.Count.EqualTo(virtualSkeleton.Bones.Length));

            int numberOfBones = 0;

            var bones = new List<VirtualBone>();
            bones.AddRange(virtualSkeleton.Roots);
            while (bones.Count > 0)
            {
                var bone = bones.Last();
                Assert.That(-1 , Is.Not.EqualTo(transformChannels.FindIndex(channel => channel.ID.Equals(bone.ChannelID))));

                bones.RemoveAt(bones.Count - 1);
                bones.AddRange(bone.Children);

                ++numberOfBones;
            }

            Assert.That(transformChannels, Has.Count.EqualTo(numberOfBones));
            //Debug.Log(virtualSkeleton.ToString());
        }

        [Test]
        public void VirtualSkeleton_BuildFrom_Skeleton_Matches_SkeletonHierarchy()
        {
            CreateSkeleton(s_TransformProperties);
            m_VirtualSkeleton = VirtualSkeleton.Create(m_Skeleton);
            CheckThatVirtualSkeletonMatchesSkeleton(m_VirtualSkeleton);
        }

        [Test]
        public void VirtualSkeleton_OnAddBone_Matches_SkeletonHierarchy()
        {
            m_VirtualSkeleton = VirtualSkeleton.Create(m_Skeleton);

            Assume.That(m_VirtualSkeleton.Bones, Is.Empty);
            Assume.That(m_VirtualSkeleton.Roots, Is.Empty);

            foreach (var property in s_TransformProperties)
            {
                m_Skeleton[property.ID] = property.Properties;
                CheckThatVirtualSkeletonMatchesSkeleton(m_VirtualSkeleton);
            }
        }

        [Test]
        public void VirtualSkeleton_OnRemoveBone_Matches_SkeletonHierarchy()
        {
            CreateSkeleton(s_TransformProperties);
            m_VirtualSkeleton = VirtualSkeleton.Create(m_Skeleton);

            for (int i = 0; i < s_TransformProperties.Length; ++i)
            {
                m_Skeleton.RemoveTransformChannelAndDescendants(s_TransformProperties[i].ID);
                CheckThatVirtualSkeletonMatchesSkeleton(m_VirtualSkeleton);
            }

            Assert.That(m_VirtualSkeleton.Bones, Is.Empty);
            Assert.That(m_VirtualSkeleton.Roots, Is.Empty);
        }

        [Test]
        public void VirtualSkeleton_OnModify_DefaultValues_Updates_Skeleton()
        {
            CreateSkeleton(s_TransformProperties);
            m_VirtualSkeleton = VirtualSkeleton.Create(m_Skeleton);

            var bones = m_VirtualSkeleton.Bones;
            for (int i = 0; i < bones.Length; ++i)
            {
                var currentTranslation = bones[i].DefaultTranslation;
                var newTranslation = currentTranslation + new float3(0f, 0f, 1f);

                var currentRotation = bones[i].DefaultRotation;
                var newRotation = math.mul(currentRotation, quaternion.RotateZ(45f));

                var newScale = new float3(0.5f);

                bones[i].DefaultTranslation = newTranslation;
                bones[i].DefaultRotation = newRotation;
                bones[i].DefaultScale = newScale;

                var channel = m_Skeleton[bones[i].ChannelID];

                Assert.That(bones[i].DefaultTranslation, Is.EqualTo(newTranslation));
                Assert.That(channel.DefaultTranslationValue, Is.EqualTo(newTranslation));

                Assert.That(bones[i].DefaultRotation, Is.EqualTo(newRotation));
                Assert.That(channel.DefaultRotationValue, Is.EqualTo(newRotation));

                Assert.That(bones[i].DefaultScale, Is.EqualTo(newScale));
                Assert.That(channel.DefaultScaleValue, Is.EqualTo(newScale));
            }
        }

        [Test]
        public void VirtualSkeleton_OnUndoRedo_DefaultValues_Updates_Skeleton()
        {
            CreateSkeleton(s_TransformProperties);
            m_VirtualSkeleton = VirtualSkeleton.Create(m_Skeleton);

            var bones = m_VirtualSkeleton.Bones;
            for (int i = 0; i < bones.Length; ++i)
            {
                var currentTranslation = bones[i].DefaultTranslation;
                var newTranslation = currentTranslation + new float3(0f, 0f, 1f);

                Undo.RecordObject(m_Skeleton, "Edit Skeleton");
                bones[i].DefaultTranslation = newTranslation;

                Assert.That(bones[i].DefaultTranslation, Is.EqualTo(newTranslation));

                Undo.PerformUndo();

                Assert.That(bones[i].DefaultTranslation, Is.EqualTo(currentTranslation));
            }
        }

        [Test]
        public void VirtualSkeleton_OnModify_TransformLabels_Updates_Skeleton()
        {
            CreateSkeleton(s_TransformProperties);
            m_VirtualSkeleton = VirtualSkeleton.Create(m_Skeleton);

            TransformLabel[] transformLabels =
            {
                TransformLabel.Create("newLabel1"),
                TransformLabel.Create("newLabel2")
            };

            var bones = m_VirtualSkeleton.Bones;

            // Add new labels to virtual bone.
            var bones0Labels = bones[0].TransformLabels;
            var newLabels = bones0Labels.Concat(transformLabels).ToArray();

            bones[0].TransformLabels = newLabels;

            var updatedLabels = bones[0].TransformLabels;
            Assert.That(newLabels.Length, Is.EqualTo(updatedLabels.Length));
            foreach (var label in newLabels)
            {
                Assert.That(Array.IndexOf(updatedLabels, label), Is.Not.EqualTo(-1));
            }

            // Add the same labels to another bone.
            // This should fail as labels should be unique to a bone.
            var bones1Labels = bones[1].TransformLabels;
            newLabels = bones1Labels.Concat(transformLabels).ToArray();

            bones[1].TransformLabels = newLabels;

            updatedLabels = bones[1].TransformLabels;
            Assert.That(bones1Labels.Length, Is.EqualTo(updatedLabels.Length));
            foreach (var label in bones1Labels)
            {
                Assert.That(Array.IndexOf(updatedLabels, label), Is.Not.EqualTo(-1));
            }

            // Remove all labels from virtual bone.
            bones[0].TransformLabels = Array.Empty<TransformLabel>();

            // Only default label should remain. We cannot remove default label.
            updatedLabels = bones[0].TransformLabels;
            Assert.That(bones0Labels.Length, Is.EqualTo(updatedLabels.Length));
            foreach (var label in bones0Labels)
            {
                Assert.That(Array.IndexOf(updatedLabels, label), Is.Not.EqualTo(-1));
            }
        }

        [Test]
        public void VirtualSkeleton_OnUndoRedo_TransformLabels_Updates_Skeleton()
        {
            CreateSkeleton(s_TransformProperties);
            m_VirtualSkeleton = VirtualSkeleton.Create(m_Skeleton);

            TransformLabel[] transformLabels =
            {
                TransformLabel.Create("newLabel1"),
                TransformLabel.Create("newLabel2")
            };

            var bones = m_VirtualSkeleton.Bones;

            Undo.RecordObject(m_Skeleton, "Edit Skeleton");

            var defaultLabels = bones[0].TransformLabels;
            var newLabels = defaultLabels.Concat(transformLabels).ToArray();

            bones[0].TransformLabels = newLabels;

            var updatedLabels = bones[0].TransformLabels;
            Assert.That(newLabels.Length, Is.EqualTo(updatedLabels.Length));
            foreach (var label in newLabels)
            {
                Assert.That(Array.IndexOf(updatedLabels, label), Is.Not.EqualTo(-1));
            }

            Undo.PerformUndo();

            updatedLabels = bones[0].TransformLabels;
            Assert.That(defaultLabels.Length, Is.EqualTo(updatedLabels.Length));
            foreach (var label in defaultLabels)
            {
                Assert.That(Array.IndexOf(updatedLabels, label), Is.Not.EqualTo(-1));
            }
        }
    }
}
