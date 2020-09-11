using System;

using NUnit.Framework;
using Unity.Entities;

namespace Unity.Animation.Tests
{
    public class RigRemapQueryTests
    {
        BlobAssetReference<RigDefinition> m_SourceRig;
        BlobAssetReference<RigDefinition> m_DestinationRig;
        [OneTimeSetUp]
        protected void OneTimeSetUp()
        {
            var srcSkeletonNodes = new SkeletonNode[]
            {
                new SkeletonNode { Id = "Root", ParentIndex = -1, AxisIndex = -1 },
                new SkeletonNode { Id = "Hips", ParentIndex = 0, AxisIndex = -1 },
                new SkeletonNode { Id = "LeftUpLeg", ParentIndex = 1, AxisIndex = -1 },
                new SkeletonNode { Id = "RightUpLeg", ParentIndex = 1, AxisIndex = -1 },
            };
            var srcChannels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "Velocity"},
                new LocalRotationChannel { Id = "AngularVelocity"},
                new FloatChannel { Id = "Intensity"},
                new FloatChannel { Id = "Radius"},
                new IntChannel { Id = "Index"},
                new IntChannel { Id = "Mode"},
            };

            var dstSkeletonNodes = new SkeletonNode[]
            {
                new SkeletonNode { Id = "AnotherRoot", ParentIndex = -1, AxisIndex = -1 },
                new SkeletonNode { Id = "AnotherHips", ParentIndex = 0, AxisIndex = -1 },
                new SkeletonNode { Id = "AnotherLeftUpLeg", ParentIndex = 1, AxisIndex = -1 },
                new SkeletonNode { Id = "AnotherRightUpLeg", ParentIndex = 1, AxisIndex = -1 },
            };
            var dstChannels = new IAnimationChannel[]
            {
                new LocalTranslationChannel { Id = "AnotherVelocity"},
                new LocalRotationChannel { Id = "AnotherAngularVelocity"},
                new FloatChannel { Id = "AnotherIntensity"},
                new FloatChannel { Id = "AnotherRadius"},
                new IntChannel { Id = "AnotherIndex"},
                new IntChannel { Id = "AnotherMode"},
            };

            m_SourceRig = RigBuilder.CreateRigDefinition(srcSkeletonNodes, null, srcChannels);
            m_DestinationRig = RigBuilder.CreateRigDefinition(dstSkeletonNodes, null, dstChannels);
        }

        [OneTimeTearDown]
        protected void OneTimeTearDown()
        {
            m_SourceRig.Dispose();
            m_DestinationRig.Dispose();
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [Test]
        public void CannotCreateRemapTableWhenSourceRigIsInvalid()
        {
            var rigRemapQuery = new RigRemapQuery {};

            Assert.Throws<ArgumentNullException>(() => rigRemapQuery.ToRigRemapTable(default, m_DestinationRig));
        }

        [Test]
        public void CannotCreateRemapTableWhenDestinationRigIsInvalid()
        {
            var rigRemapQuery = new RigRemapQuery {};

            Assert.Throws<ArgumentNullException>(() => rigRemapQuery.ToRigRemapTable(m_SourceRig, default));
        }

        [Test]
        public void CannotCreateRemapTableWithOutOfBoundsTranslationOffsetIndex()
        {
            var rigRemapQuery = new RigRemapQuery
            {
                TranslationChannels = new[]
                {
                    new ChannelMap { SourceId = "Hips", DestinationId = "AnotherHips", OffsetIndex = 1 },
                }
            };

            Assert.Throws<InvalidOperationException>(() => rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig));
        }

        [Test]
        public void CannotCreateRemapTableWithOutOfBoundsRotationOffsetIndex()
        {
            var rigRemapQuery = new RigRemapQuery
            {
                RotationChannels = new[]
                {
                    new ChannelMap { SourceId = "Hips", DestinationId = "AnotherHips", OffsetIndex = 1 },
                }
            };

            Assert.Throws<InvalidOperationException>(() => rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig));
        }

        [Test]
        public void CannotCreateRemapTableWhenDuplicatedDestinationChannelAreFound()
        {
            var rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherHips" },
                    new ChannelMap { SourceId = "Hips", DestinationId = "AnotherHips" },
                }
            };

            Assert.Throws<InvalidOperationException>(() => rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig));

            rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherHips" },
                },
                TranslationChannels = new[]
                {
                    new ChannelMap { SourceId = "Hips", DestinationId = "AnotherHips" },
                }
            };

            Assert.Throws<InvalidOperationException>(() => rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig));

            rigRemapQuery = new RigRemapQuery
            {
                TranslationChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherHips" },
                    new ChannelMap { SourceId = "Hips", DestinationId = "AnotherHips" },
                }
            };

            Assert.Throws<InvalidOperationException>(() => rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig));

            rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherHips" },
                },
                RotationChannels = new[]
                {
                    new ChannelMap { SourceId = "Hips", DestinationId = "AnotherHips" },
                }
            };

            Assert.Throws<InvalidOperationException>(() => rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig));

            rigRemapQuery = new RigRemapQuery
            {
                RotationChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherHips" },
                    new ChannelMap { SourceId = "Hips", DestinationId = "AnotherHips" },
                }
            };

            Assert.Throws<InvalidOperationException>(() => rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig));

            rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherHips" },
                },
                ScaleChannels = new[]
                {
                    new ChannelMap { SourceId = "Hips", DestinationId = "AnotherHips" },
                }
            };

            Assert.Throws<InvalidOperationException>(() => rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig));

            rigRemapQuery = new RigRemapQuery
            {
                ScaleChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherHips" },
                    new ChannelMap { SourceId = "Hips", DestinationId = "AnotherHips" },
                }
            };

            Assert.Throws<InvalidOperationException>(() => rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig));

            rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new[]
                {
                    new ChannelMap { SourceId = "Intensity", DestinationId = "AnotherIntensity" },
                    new ChannelMap { SourceId = "Radius", DestinationId = "AnotherIntensity" },
                }
            };

            Assert.Throws<InvalidOperationException>(() => rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig));

            rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new[]
                {
                    new ChannelMap { SourceId = "Intensity", DestinationId = "AnotherIntensity" },
                },
                FloatChannels = new[]
                {
                    new ChannelMap { SourceId = "Radius", DestinationId = "AnotherIntensity" },
                }
            };

            Assert.Throws<InvalidOperationException>(() => rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig));

            rigRemapQuery = new RigRemapQuery
            {
                FloatChannels = new[]
                {
                    new ChannelMap { SourceId = "Intensity", DestinationId = "AnotherIntensity" },
                    new ChannelMap { SourceId = "Radius", DestinationId = "AnotherIntensity" },
                }
            };

            Assert.Throws<InvalidOperationException>(() => rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig));

            rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new[]
                {
                    new ChannelMap { SourceId = "Index", DestinationId = "AnotherIndex" },
                    new ChannelMap { SourceId = "Mode", DestinationId = "AnotherIndex" },
                }
            };

            Assert.Throws<InvalidOperationException>(() => rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig));

            rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new[]
                {
                    new ChannelMap { SourceId = "Index", DestinationId = "AnotherIndex" },
                },
                IntChannels = new[]
                {
                    new ChannelMap { SourceId = "Mode", DestinationId = "AnotherIndex" },
                }
            };

            Assert.Throws<InvalidOperationException>(() => rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig));

            rigRemapQuery = new RigRemapQuery
            {
                IntChannels = new[]
                {
                    new ChannelMap { SourceId = "Index", DestinationId = "AnotherMode" },
                    new ChannelMap { SourceId = "Mode", DestinationId = "AnotherMode" },
                }
            };

            Assert.Throws<InvalidOperationException>(() => rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig));
        }

#endif

        [Test]
        public void CanCreateEmptyRemapTableFromEmptyQuery()
        {
            var rigRemapQuery = new RigRemapQuery {};

            var remapTable = rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig);

            Assert.That(remapTable.Value.TranslationMappings.Length, Is.EqualTo(0));
            Assert.That(remapTable.Value.RotationMappings.Length, Is.EqualTo(0));
            Assert.That(remapTable.Value.ScaleMappings.Length, Is.EqualTo(0));
            Assert.That(remapTable.Value.FloatMappings.Length, Is.EqualTo(0));
            Assert.That(remapTable.Value.IntMappings.Length, Is.EqualTo(0));
        }

        [Test]
        public void CanCreateRemapTableWithTranslationOffsets()
        {
            var rigRemapQuery = new RigRemapQuery
            {
                TranslationChannels = new[]
                {
                    new ChannelMap { SourceId = "Hips", DestinationId = "AnotherHips", OffsetIndex = 1 },
                },

                TranslationOffsets = new[]
                {
                    new RigTranslationOffset(),
                    new RigTranslationOffset()
                }
            };

            var remapTable = rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig);

            Assert.That(remapTable.Value.TranslationOffsets.Length, Is.EqualTo(2));
        }

        [Test]
        public void CanCreateRemapTableWithRotationOffsets()
        {
            var rigRemapQuery = new RigRemapQuery
            {
                RotationChannels = new[]
                {
                    new ChannelMap { SourceId = "Hips", DestinationId = "AnotherHips", OffsetIndex = 1 },
                },

                RotationOffsets = new[]
                {
                    new RigRotationOffset(),
                    new RigRotationOffset()
                }
            };

            var remapTable = rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig);

            Assert.That(remapTable.Value.RotationOffsets.Length, Is.EqualTo(2));
        }

        [Test]
        public void UnmatchingMappingAreRemoved()
        {
            var rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Hips", DestinationId = "AnotherHips" },
                    new ChannelMap { SourceId = "LeftUpLeg", DestinationId = "DONOTEXIST" },
                    new ChannelMap { SourceId = "DONOTEXIST", DestinationId = "AnotherRightUpLeg" },
                }
            };

            var remapTable = rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig);

            Assert.That(remapTable.Value.TranslationMappings.Length, Is.EqualTo(2));
            Assert.That(remapTable.Value.RotationMappings.Length, Is.EqualTo(2));
            Assert.That(remapTable.Value.ScaleMappings.Length, Is.EqualTo(2));
            Assert.That(remapTable.Value.FloatMappings.Length, Is.EqualTo(0));
            Assert.That(remapTable.Value.IntMappings.Length, Is.EqualTo(0));
        }

        [Test]
        public void RemapTableIndicesMatchRigDefinitionChannelIndices()
        {
            var rigRemapQuery = new RigRemapQuery
            {
                AllChannels = new[]
                {
                    new ChannelMap { SourceId = "Root", DestinationId = "AnotherRoot" },
                    new ChannelMap { SourceId = "Hips", DestinationId = "AnotherHips" },
                },
                RotationChannels = new[]
                {
                    new ChannelMap { SourceId = "LeftUpLeg", DestinationId = "AnotherLeftUpLeg" },
                    new ChannelMap { SourceId = "RightUpLeg", DestinationId = "AnotherRightUpLeg" },
                },
                FloatChannels = new[]
                {
                    new ChannelMap { SourceId = "Intensity", DestinationId = "AnotherIntensity" },
                },
                IntChannels = new[]
                {
                    new ChannelMap { SourceId = "Index", DestinationId = "AnotherIndex" },
                }
            };

            var remapTable = rigRemapQuery.ToRigRemapTable(m_SourceRig, m_DestinationRig);

            Assert.That(remapTable.Value.TranslationMappings.Length, Is.EqualTo(2));
            Assert.That(remapTable.Value.RotationMappings.Length, Is.EqualTo(4));
            Assert.That(remapTable.Value.ScaleMappings.Length, Is.EqualTo(2));
            Assert.That(remapTable.Value.FloatMappings.Length, Is.EqualTo(1));
            Assert.That(remapTable.Value.IntMappings.Length, Is.EqualTo(1));


            for (int i = 0; i != remapTable.Value.TranslationMappings.Length; i++)
            {
                var srcIndex = remapTable.Value.TranslationMappings[i].SourceIndex;
                var dstIndex = remapTable.Value.TranslationMappings[i].DestinationIndex;

                Assert.That(m_SourceRig.Value.Bindings.TranslationBindings[srcIndex], Is.EqualTo(rigRemapQuery.AllChannels[i].SourceId));
                Assert.That(m_DestinationRig.Value.Bindings.TranslationBindings[srcIndex], Is.EqualTo(rigRemapQuery.AllChannels[i].DestinationId));
            }

            for (int i = 0; i != remapTable.Value.RotationMappings.Length; i++)
            {
                var srcIndex = remapTable.Value.RotationMappings[i].SourceIndex;
                var dstIndex = remapTable.Value.RotationMappings[i].DestinationIndex;

                if (i < 2)
                {
                    Assert.That(m_SourceRig.Value.Bindings.RotationBindings[srcIndex], Is.EqualTo(rigRemapQuery.AllChannels[i].SourceId));
                    Assert.That(m_DestinationRig.Value.Bindings.RotationBindings[srcIndex], Is.EqualTo(rigRemapQuery.AllChannels[i].DestinationId));
                }
                else
                {
                    Assert.That(m_SourceRig.Value.Bindings.RotationBindings[srcIndex], Is.EqualTo(rigRemapQuery.RotationChannels[i - 2].SourceId));
                    Assert.That(m_DestinationRig.Value.Bindings.RotationBindings[srcIndex], Is.EqualTo(rigRemapQuery.RotationChannels[i - 2].DestinationId));
                }
            }

            for (int i = 0; i != remapTable.Value.ScaleMappings.Length; i++)
            {
                var srcIndex = remapTable.Value.ScaleMappings[i].SourceIndex;
                var dstIndex = remapTable.Value.ScaleMappings[i].DestinationIndex;

                Assert.That(m_SourceRig.Value.Bindings.ScaleBindings[srcIndex], Is.EqualTo(rigRemapQuery.AllChannels[i].SourceId));
                Assert.That(m_DestinationRig.Value.Bindings.ScaleBindings[srcIndex], Is.EqualTo(rigRemapQuery.AllChannels[i].DestinationId));
            }

            for (int i = 0; i != remapTable.Value.FloatMappings.Length; i++)
            {
                var srcIndex = remapTable.Value.FloatMappings[i].SourceIndex;
                var dstIndex = remapTable.Value.FloatMappings[i].DestinationIndex;

                Assert.That(m_SourceRig.Value.Bindings.FloatBindings[srcIndex], Is.EqualTo(rigRemapQuery.FloatChannels[i].SourceId));
                Assert.That(m_DestinationRig.Value.Bindings.FloatBindings[srcIndex], Is.EqualTo(rigRemapQuery.FloatChannels[i].DestinationId));
            }

            for (int i = 0; i != remapTable.Value.IntMappings.Length; i++)
            {
                var srcIndex = remapTable.Value.IntMappings[i].SourceIndex;
                var dstIndex = remapTable.Value.IntMappings[i].DestinationIndex;

                Assert.That(m_SourceRig.Value.Bindings.IntBindings[srcIndex], Is.EqualTo(rigRemapQuery.IntChannels[i].SourceId));
                Assert.That(m_DestinationRig.Value.Bindings.IntBindings[srcIndex], Is.EqualTo(rigRemapQuery.IntChannels[i].DestinationId));
            }
        }
    }
}
