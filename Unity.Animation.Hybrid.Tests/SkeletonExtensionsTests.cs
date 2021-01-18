using System;
using NUnit.Framework;
using Unity.Animation.Authoring;
using Unity.Animation.Hybrid;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Animation.Tests
{
    class SkeletonExtensionsTests
    {
        private Authoring.Skeleton m_Skeleton;

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

        private void BuildSkeleton(Authoring.Skeleton skeleton, TransformBindingID[] bindingIDs, TransformBindingID rootID = default)
        {
            foreach (var id in bindingIDs)
            {
                skeleton[id] = new TransformChannelProperties
                {
                    DefaultTranslationValue = float3.zero,
                    DefaultRotationValue = quaternion.identity,
                    DefaultScaleValue = new float3(1f)
                };
            }

            skeleton.Root = rootID;
        }

        const string kBonePrefix = "PREFIX_";

        static readonly TransformBindingID[] k_SkeletonWithoutPrefix =
        {
            new TransformBindingID {Path = "A/B/C/D/E/F"},
        };

        static readonly TransformBindingID[] k_SkeletonWithPrefix =
        {
            new TransformBindingID {Path = String.Format("{0}A/{0}B/{0}C/{0}D/{0}E/{0}F", kBonePrefix)}
        };

        static readonly TransformBindingID[] k_SkeletonWithPartialPrefix =
        {
            new TransformBindingID {Path = String.Format("A/B/C/{0}D/{0}E/{0}F", kBonePrefix)}
        };

        static readonly TestCaseData[] s_SkeletonsWithPrefixes =
        {
            new TestCaseData(k_SkeletonWithoutPrefix, TransformBindingID.Invalid).SetName("Skeleton without prefix").Returns(string.Empty),
            new TestCaseData(k_SkeletonWithPrefix, TransformBindingID.Invalid).SetName("Skeleton with prefix").Returns(kBonePrefix),
            new TestCaseData(k_SkeletonWithPartialPrefix, TransformBindingID.Invalid).SetName("Skeleton with partial prefix").Returns(string.Empty),
            new TestCaseData(k_SkeletonWithPartialPrefix, new TransformBindingID {Path = "A/B/C"}).SetName("Skeleton with partial prefix and root node").Returns(kBonePrefix)
        };

        [TestCaseSource(nameof(s_SkeletonsWithPrefixes))]
        public string Skeleton_ExtractNamespace_ReturnsPrefix(TransformBindingID[] skeleton, TransformBindingID root)
        {
            BuildSkeleton(m_Skeleton, skeleton, root);

            return m_Skeleton.ExtractNamespace();
        }
    }
}
