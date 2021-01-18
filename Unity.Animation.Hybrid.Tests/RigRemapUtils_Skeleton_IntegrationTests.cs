using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Animation.Authoring;
using Unity.Animation.Hybrid;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Animation.Tests
{
    class RigRemapUtils_Skeleton_IntegrationTests
    {
        private Authoring.Skeleton m_SourceSkeleton;
        private Authoring.Skeleton m_TargetSkeleton;

        [SetUp]
        public void SetUp()
        {
            m_SourceSkeleton = ScriptableObject.CreateInstance<Authoring.Skeleton>();
            m_TargetSkeleton = ScriptableObject.CreateInstance<Authoring.Skeleton>();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_SourceSkeleton)
                ScriptableObject.DestroyImmediate(m_SourceSkeleton);
            if (m_TargetSkeleton)
                ScriptableObject.DestroyImmediate(m_TargetSkeleton);
        }

        public void BuildSkeleton(Authoring.Skeleton skeleton, TransformBindingID[] bindingIDs, TransformBindingID rootID = default)
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

        const string kNoPrefix = "";
        const string kBonePrefix1 = "PREFIX1_";
        const string kBonePrefix2 = "PREFIX2_";

        [TestCase(kNoPrefix, kNoPrefix, TestName = "No prefix -> No prefix")]
        [TestCase(kBonePrefix1, kNoPrefix, TestName = "Prefix 1 -> No prefix")]
        [TestCase(kNoPrefix, kBonePrefix1, TestName = "No prefix -> Prefix 1")]
        [TestCase(kBonePrefix1, kBonePrefix2, TestName = "Prefix 1 -> Prefix 2")]
        public void RigRemapUtils_CreateRemapTable_Skeleton_To_Skeleton_WithSameTopology_FindsMatches(string sourcePrefix, string targetPrefix)
        {
            BuildSkeleton(m_SourceSkeleton, new[] { new TransformBindingID { Path = String.Format("{0}A/{0}B/{0}C/{0}D", sourcePrefix) } });
            BuildSkeleton(m_TargetSkeleton, new[] { new TransformBindingID { Path = String.Format("{0}A/{0}B/{0}C/{0}D", targetPrefix) } });

            Assume.That(m_SourceSkeleton.ActiveTransformChannelCount, Is.EqualTo(5));
            Assume.That(m_TargetSkeleton.ActiveTransformChannelCount, Is.EqualTo(5));

            var sourceNamespace = m_SourceSkeleton.ExtractNamespace();
            var targetNamespace = m_TargetSkeleton.ExtractNamespace();

            Assert.That(sourceNamespace, Is.EqualTo(sourcePrefix));
            Assert.That(targetNamespace, Is.EqualTo(targetPrefix));

            var sourceHasher = new MapByPathHashGenerator {BonePrefix = sourceNamespace};
            var targetHasher = new MapByPathHashGenerator {BonePrefix = targetNamespace};

            using (var boneMappingTable = Hybrid.RigRemapUtils.CreateRemapTable(
                m_SourceSkeleton,
                m_TargetSkeleton,
                RigRemapUtils.ChannelFilter.All,
                default,
                sourceHasher,
                targetHasher))
            {
                Assume.That(boneMappingTable.IsCreated, Is.True);

                ref var mappings = ref boneMappingTable.Value.TranslationMappings;
                var mappingCount = mappings.Length;

                Assert.That(mappingCount, Is.EqualTo(5));

                var srcIndices = mappings.ToArray().Select(m => m.SourceIndex).ToArray();
                var dstIndices = mappings.ToArray().Select(m => m.DestinationIndex).ToArray();
                Assert.That(srcIndices, Is.EqualTo(dstIndices), "Indices did not correspond perfectly.");
            }
        }

        static readonly TransformBindingID[] k_SkeletonLOD0 =
        {
            new TransformBindingID {Path = "A/B/C/D/E"},
            new TransformBindingID {Path = "A/BB"},
            new TransformBindingID {Path = "A/B/CC"}
        };
        static readonly TransformBindingID[] k_SkeletonLOD1 =
        {
            new TransformBindingID {Path = "A/B/C/D/E"},
        };
        static readonly TransformBindingID[] k_SkeletonLOD2 =
        {
            new TransformBindingID {Path = "A/B/C"},
        };

        static readonly TestCaseData[] s_SkeletonsWithLODs =
        {
            new TestCaseData(k_SkeletonLOD0, k_SkeletonLOD1).SetName("LOD 0 -> LOD 1"),
            new TestCaseData(k_SkeletonLOD0, k_SkeletonLOD2).SetName("LOD 0 -> LOD 2"),
            new TestCaseData(k_SkeletonLOD2, k_SkeletonLOD0).SetName("LOD 2 -> LOD 0")
        };

        [TestCaseSource(nameof(s_SkeletonsWithLODs))]
        public void RigRemapUtils_CreateRemapTable_Skeleton_To_Skeleton_WithLODs_FindMatches(TransformBindingID[] sourceBindings, TransformBindingID[] targetBindings)
        {
            BuildSkeleton(m_SourceSkeleton, sourceBindings);
            BuildSkeleton(m_TargetSkeleton, targetBindings);

            var hasher = new MapByPathHashGenerator();

            using (var boneMappingTable = Hybrid.RigRemapUtils.CreateRemapTable(
                m_SourceSkeleton,
                m_TargetSkeleton,
                RigRemapUtils.ChannelFilter.All,
                default,
                hasher,
                hasher))
            {
                Assume.That(boneMappingTable.IsCreated, Is.True);

                var srcChannels = new List<TransformChannel>();
                var dstChannels = new List<TransformChannel>();

                m_SourceSkeleton.GetAllTransforms(srcChannels);
                m_TargetSkeleton.GetAllTransforms(dstChannels);

                ref var mappings = ref boneMappingTable.Value.TranslationMappings;
                var mappingCount = mappings.Length;

                var expectedCount = math.min(srcChannels.Count, dstChannels.Count);

                Assert.That(mappingCount, Is.GreaterThan(0));
                Assert.That(mappingCount, Is.EqualTo(expectedCount));

                var srcChannelNames = mappings.ToArray().Select(m => srcChannels[m.SourceIndex].ID.Name).ToArray();
                var dstChannelNames = mappings.ToArray().Select(m => dstChannels[m.DestinationIndex].ID.Name).ToArray();
                Assert.That(srcChannelNames, Is.EqualTo(dstChannelNames), "Names did not correspond perfectly.");
            }
        }

        static readonly (TransformBindingID[] skeleton, TransformBindingID root)k_SkeletonWithoutRoot =
            (new[] { new TransformBindingID { Path = "C/D" } }, TransformBindingID.Root);
        static readonly (TransformBindingID[] skeleton, TransformBindingID root)k_SkeletonWithRoot1 =
            (new[] { new TransformBindingID { Path = "A/B/C/D" } }, new TransformBindingID { Path = "A/B" });
        static readonly (TransformBindingID[] skeleton, TransformBindingID root)k_SkeletonWithRoot2 =
            (new[] { new TransformBindingID { Path = "AA/BB/C/D" } }, new TransformBindingID { Path = "AA/BB" });

        static readonly TestCaseData[] s_SkeletonWithRoots =
        {
            new TestCaseData(k_SkeletonWithoutRoot, k_SkeletonWithRoot1).SetName("No root -> Root 1"),
            new TestCaseData(k_SkeletonWithRoot1, k_SkeletonWithoutRoot).SetName("Root 1 -> No root"),
            new TestCaseData(k_SkeletonWithRoot1, k_SkeletonWithRoot2).SetName("Root 1 -> Root 2")
        };

        [TestCaseSource(nameof(s_SkeletonWithRoots))]
        public void RigRemapUtils_CreateRemapTable_Skeleton_To_Skeleton_WithRoots_FindsMatches(
            (TransformBindingID[] skeleton, TransformBindingID root) source,
            (TransformBindingID[] skeleton, TransformBindingID root) target
        )
        {
            BuildSkeleton(m_SourceSkeleton, source.skeleton, source.root);
            BuildSkeleton(m_TargetSkeleton, target.skeleton, target.root);

            Assume.That(m_SourceSkeleton.ActiveTransformChannelCount, Is.EqualTo(m_TargetSkeleton.ActiveTransformChannelCount));

            var sourceHasher = new MapByPathHashGenerator {RootID = source.root};
            var targetHasher = new MapByPathHashGenerator {RootID = target.root};

            using (var boneMappingTable = Hybrid.RigRemapUtils.CreateRemapTable(
                m_SourceSkeleton,
                m_TargetSkeleton,
                RigRemapUtils.ChannelFilter.All,
                default,
                sourceHasher,
                targetHasher))
            {
                Assume.That(boneMappingTable.IsCreated, Is.True);

                ref var mappings = ref boneMappingTable.Value.TranslationMappings;
                var mappingCount = mappings.Length;

                Assert.That(mappingCount, Is.EqualTo(m_SourceSkeleton.ActiveTransformChannelCount));

                var srcIndices = mappings.ToArray().Select(m => m.SourceIndex).ToArray();
                var dstIndices = mappings.ToArray().Select(m => m.DestinationIndex).ToArray();
                Assert.That(srcIndices, Is.EqualTo(dstIndices), "Indices did not correspond perfectly.");
            }
        }
    }
}
