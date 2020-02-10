using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.TestTools;

namespace Unity.Animation.Tests
{
    public class MixerNodeTests : AnimationTestsFixture, IPrebuildSetup
    {
        private float3 m_ClipRootLocalTranslation1 => new float3(100.0f, 0.0f, 0.0f);
        private quaternion m_ClipRootLocalRotation1 => quaternion.RotateX(math.radians(90.0f));
        private float3 m_ClipRootLocalScale1 => new float3(10.0f, 1.0f, 1.0f);

        private float3 m_ClipChildLocalTranslation1 => new float3(0.0f, 100.0f, 0.0f);
        private quaternion m_ClipChildLocalRotation1 => quaternion.RotateY(math.radians(90.0f));
        private float3 m_ClipChildLocalScale1 => new float3(1.0f, 1.0f, 10.0f);

        private float3 m_ClipRootLocalTranslation2 => new float3(0.0f, 100.0f, 0.0f);
        private quaternion m_ClipRootLocalRotation2 => quaternion.RotateY(math.radians(90.0f));
        private float3 m_ClipRootLocalScale2 => new float3(1.0f, 1.0f, 10.0f);

        private float3 m_ClipChildLocalTranslation2 => new float3(100.0f, 0.0f, 0.0f);
        private quaternion m_ClipChildLocalRotation2 => quaternion.RotateX(math.radians(90.0f));
        private float3 m_ClipChildLocalScale2 => new float3(1.0f, 10.0f, 1.0f);

        private Rig m_Rig;
        private BlobAssetReference<Clip> m_ConstantRootClip1;
        private BlobAssetReference<Clip> m_ConstantRootClip2;
        private BlobAssetReference<Clip> m_ConstantHierarchyClip1;
        private BlobAssetReference<Clip> m_ConstantHierarchyClip2;

        private static BlobAssetReference<RigDefinition> CreateTestRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode
                {
                    ParentIndex = -1, Id = "Root", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(0,0,0),
                    LocalRotationDefaultValue = new quaternion(0,0,0,1),
                    LocalScaleDefaultValue = new float3(1,1,1),
                },
                new SkeletonNode
                {
                    ParentIndex = 0, Id = "Child1", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(0,0,0),
                    LocalRotationDefaultValue = new quaternion(0,0,0,1),
                    LocalScaleDefaultValue = new float3(1,1,1),
                },
                new SkeletonNode
                {
                    ParentIndex = 0, Id = "Child2", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(0,0,0),
                    LocalRotationDefaultValue = new quaternion(0,0,0,1),
                    LocalScaleDefaultValue = new float3(1,1,1),
                }
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes);
        }

        public void Setup()
        {
#if UNITY_EDITOR
            PlaymodeTestsEditorSetup.CreateStreamingAssetsDirectory();

            var constantRootClip1 = CreateConstantDenseClip(
                        new[] { ("Root", m_ClipRootLocalTranslation1) },
                        new[] { ("Root", m_ClipRootLocalRotation1) },
                        new[] { ("Root", m_ClipRootLocalScale1) });

            var blobPath = "MixerNodeTestsConstantRootClip1.blob";
            BlobFile.WriteBlobAsset(ref constantRootClip1, blobPath);

            var constantHierarchyClip1 = CreateConstantDenseClip(
                        new[] { ("Root", m_ClipRootLocalTranslation1), ("Child1", m_ClipChildLocalTranslation1) },
                        new[] { ("Root", m_ClipRootLocalRotation1), ("Child1", m_ClipChildLocalRotation1) },
                        new[] { ("Root", m_ClipRootLocalScale1),    ("Child1", m_ClipChildLocalScale1) });

            blobPath = "MixerNodeTestsConstantHierarchyClip1.blob";
            BlobFile.WriteBlobAsset(ref constantHierarchyClip1, blobPath);


            var constantRootClip2 = CreateConstantDenseClip(
                        new[] { ("Root", m_ClipRootLocalTranslation2) },
                        new[] { ("Root", m_ClipRootLocalRotation2) },
                        new[] { ("Root", m_ClipRootLocalScale2) });

            blobPath = "MixerNodeTestsConstantRootClip2.blob";
            BlobFile.WriteBlobAsset(ref constantRootClip2, blobPath);

            var constantHierarchyClip2 = CreateConstantDenseClip(
                        new[] { ("Root", m_ClipRootLocalTranslation2), ("Child1", m_ClipChildLocalTranslation2) },
                        new[] { ("Root", m_ClipRootLocalRotation2), ("Child1", m_ClipChildLocalRotation2) },
                        new[] { ("Root", m_ClipRootLocalScale2),    ("Child1", m_ClipChildLocalScale2) });

            blobPath = "MixerNodeTestsConstantHierarchyClip2.blob";
            BlobFile.WriteBlobAsset(ref constantHierarchyClip2, blobPath);
#endif
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // Create rig
            m_Rig = new Rig { Value = CreateTestRigDefinition() };

            // Clip #1
            {
                // Constant root clip
                {
                    var path = "MixerNodeTestsConstantRootClip1.blob";
                    m_ConstantRootClip1 = BlobFile.ReadBlobAsset<Clip>(path);

                    ClipManager.Instance.GetClipFor(m_Rig, m_ConstantRootClip1);
                }

                // Constant hierarchy clip
                {
                    var path = "MixerNodeTestsConstantHierarchyClip1.blob";
                    m_ConstantHierarchyClip1 = BlobFile.ReadBlobAsset<Clip>(path);

                    ClipManager.Instance.GetClipFor(m_Rig, m_ConstantHierarchyClip1);
                }
            }

            // Clip #2
            {

                // Constant root clip
                {
                    var path = "MixerNodeTestsConstantRootClip2.blob";
                    m_ConstantRootClip2 = BlobFile.ReadBlobAsset<Clip>(path);

                    ClipManager.Instance.GetClipFor(m_Rig, m_ConstantRootClip2);
                }

                // Constant hierarchy clip
                {
                    var path = "MixerNodeTestsConstantHierarchyClip2.blob";
                    m_ConstantHierarchyClip2 = BlobFile.ReadBlobAsset<Clip>(path);

                    ClipManager.Instance.GetClipFor(m_Rig, m_ConstantHierarchyClip2);
                }
            }
        }

        [Test]
        [TestCase(0.0f, Description = "Mix at 0%")]
        [TestCase(0.3f, Description = "Mix at 30%")]
        [TestCase(1.0f, Description = "Mix at 100%")]
        public void CanMixSimpleClips(float weight)
        {
            var expectedLocalTranslation = math.lerp(m_ClipRootLocalTranslation1, m_ClipRootLocalTranslation2, weight);
            var expectedLocalRotation = mathex.lerp(m_ClipRootLocalRotation1, m_ClipRootLocalRotation2, weight);
            var expectedLocalScale = math.lerp(m_ClipRootLocalScale1, m_ClipRootLocalScale2, weight);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipNode1 = CreateNode<ClipNode>();
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Clip, m_ConstantRootClip1);

            var clipNode2 = CreateNode<ClipNode>();
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Clip, m_ConstantRootClip2);

            var mixerNode = CreateNode<MixerNode>();
            set.Connect(clipNode1, ClipNode.KernelPorts.Output, mixerNode, MixerNode.KernelPorts.Input0);
            set.Connect(clipNode2, ClipNode.KernelPorts.Output, mixerNode, MixerNode.KernelPorts.Input1);
            set.SendMessage(mixerNode, MixerNode.SimulationPorts.Rig, m_Rig);
            set.SetData(mixerNode, MixerNode.KernelPorts.Weight, weight);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerNode, MixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
                );

            Assert.That(streamECS.GetLocalToParentTranslation(0), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(0), Is.EqualTo(expectedLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(0), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.0f, Description = "Mix at 0%")]
        [TestCase(0.3f, Description = "Mix at 30%")]
        [TestCase(1.0f, Description = "Mix at 100%")]
        public void CanMixClipsWithHierarchy(float weight)
        {
            var expectedLocalTranslation = math.lerp(m_ClipChildLocalTranslation1, m_ClipChildLocalTranslation2, weight);
            var expectedLocalRotation = mathex.lerp(m_ClipChildLocalRotation1, m_ClipChildLocalRotation2, weight);
            var expectedLocalScale = math.lerp(m_ClipChildLocalScale1, m_ClipChildLocalScale2, weight);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;
            var clipNode1 = CreateNode<ClipNode>();
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Clip, m_ConstantHierarchyClip1);

            var clipNode2 = CreateNode<ClipNode>();
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Clip, m_ConstantHierarchyClip2);

            var mixerNode = CreateNode<MixerNode>();
            set.Connect(clipNode1, ClipNode.KernelPorts.Output, mixerNode, MixerNode.KernelPorts.Input0);
            set.Connect(clipNode2, ClipNode.KernelPorts.Output, mixerNode, MixerNode.KernelPorts.Input1);
            set.SendMessage(mixerNode, MixerNode.SimulationPorts.Rig, m_Rig);
            set.SetData(mixerNode, MixerNode.KernelPorts.Weight, weight);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerNode, MixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
                );
            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
        }

        [Test]
        public void OutputDefaultValuesWhenNoInputConnected()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode
                {
                    ParentIndex = -1, Id = "Root", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(1,2,3),
                    LocalRotationDefaultValue = new quaternion(1,0,0,0),
                    LocalScaleDefaultValue = new float3(2,2,2),
                },
                new SkeletonNode
                {
                    ParentIndex = 0, Id = "Child1", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(4,5,6),
                    LocalRotationDefaultValue = new quaternion(0,1,0,0),
                    LocalScaleDefaultValue = new float3(3,3,3),
                },
                new SkeletonNode
                {
                    ParentIndex = 0, Id = "Child2", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(7,8,9),
                    LocalRotationDefaultValue = new quaternion(0,0,1,0),
                    LocalScaleDefaultValue = new float3(4,4,4),
                }
            };

            var rig = new Rig { Value = RigBuilder.CreateRigDefinition(skeletonNodes) };

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, rig);

            var set = Set;
            var mixer = CreateNode<MixerNode>();
            set.SendMessage(mixer, MixerNode.SimulationPorts.Rig, rig);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixer, MixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
                );

            for(int i=0;i<skeletonNodes.Length;i++)
            {
                Assert.That(streamECS.GetLocalToParentTranslation(i), Is.EqualTo(skeletonNodes[i].LocalTranslationDefaultValue).Using(TranslationComparer));
                Assert.That(streamECS.GetLocalToParentRotation(i), Is.EqualTo(skeletonNodes[i].LocalRotationDefaultValue).Using(RotationComparer));
                Assert.That(streamECS.GetLocalToParentScale(i), Is.EqualTo(skeletonNodes[i].LocalScaleDefaultValue).Using(ScaleComparer));
            }
        }

        [Test]
        [TestCase(0.0f, Description = "Mix at 0%")]
        [TestCase(0.3f, Description = "Mix at 30%")]
        [TestCase(1.0f, Description = "Mix at 100%")]
        public void MixingWithInput0NotConnectedReturnsMixBetweenBindPoseAndClip(float weight)
        {
            var expectedLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation2, weight);
            var expectedLocalRotation = math.slerp(quaternion.identity, m_ClipChildLocalRotation2, weight);
            var expectedLocalScale = math.lerp(new float3(1), m_ClipChildLocalScale2, weight);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var clipNode2 = CreateNode<ClipNode>();
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.Clip, m_ConstantHierarchyClip2);

            var mixerNode = CreateNode<MixerNode>();
            set.Connect(clipNode2, ClipNode.KernelPorts.Output, mixerNode, MixerNode.KernelPorts.Input1);
            set.SendMessage(mixerNode, MixerNode.SimulationPorts.Rig, m_Rig);
            set.SetData(mixerNode, MixerNode.KernelPorts.Weight, weight);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerNode, MixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
                );
            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.0f, Description = "Mix at 0%")]
        [TestCase(0.3f, Description = "Mix at 30%")]
        [TestCase(1.0f, Description = "Mix at 100%")]
        public void MixingWithInput1NotConnectedReturnsMixBetweenBindPoseAndClip(float weight)
        {
            var expectedLocalTranslation = math.lerp(m_ClipChildLocalTranslation1, float3.zero, weight);
            var expectedLocalRotation = math.slerp(m_ClipChildLocalRotation1, quaternion.identity, weight);
            var expectedLocalScale = math.lerp(m_ClipChildLocalScale1, new float3(1), weight);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var clipNode1 = CreateNode<ClipNode>();
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Clip, m_ConstantHierarchyClip1);

            var mixerNode = CreateNode<MixerNode>();
            set.Connect(clipNode1, ClipNode.KernelPorts.Output, mixerNode, MixerNode.KernelPorts.Input0);
            set.SendMessage(mixerNode, MixerNode.SimulationPorts.Rig, m_Rig);
            set.SetData(mixerNode, MixerNode.KernelPorts.Weight, weight);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerNode, MixerNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
                );
            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
        }

        [Test]
        public void MixerBeginNodeFlushStreamValues()
        {
            var expectedLocalTranslation = float3.zero;
            var expectedLocalRotation = new quaternion(0.0F, 0.0F, 0.0F, 0.0F);
            var expectedLocalScale = float3.zero;

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var mixerBeginNode = CreateNode<MixerBeginNode>();
            set.SendMessage(mixerBeginNode, MixerBeginNode.SimulationPorts.Rig, m_Rig);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerBeginNode, MixerBeginNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            var graphValue = CreateGraphValue(mixerBeginNode, MixerBeginNode.KernelPorts.SumWeight);

            m_AnimationGraphSystem.Update();

            var value = set.GetValueBlocking(graphValue);

            Assert.That(value, Is.EqualTo(0.0F));

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
                );

            for(int i=0;i<m_Rig.Value.Value.Skeleton.BoneCount; i++)
            {
                // We expect that the buffer values are all 0
                Assert.That(streamECS.GetLocalToParentTranslation(i), Is.EqualTo(expectedLocalTranslation));
                Assert.That(streamECS.GetLocalToParentRotation(i), Is.EqualTo(expectedLocalRotation));
                Assert.That(streamECS.GetLocalToParentScale(i), Is.EqualTo(expectedLocalScale));
            }
        }

        [Test]
        public void MixerEndNodeReturnsDefaultPoseWhenSumWeightEqualsZero()
        {
            var expectedLocalTranslation = float3.zero;
            var expectedLocalRotation = quaternion.identity;
            var expectedLocalScale = float3.zero;

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var mixerBeginNode = CreateNode<MixerBeginNode>();
            set.SendMessage(mixerBeginNode, MixerBeginNode.SimulationPorts.Rig, m_Rig);

            var mixerEndNode = CreateNode<MixerEndNode>();
            set.SendMessage(mixerEndNode, MixerEndNode.SimulationPorts.Rig, m_Rig);

            set.Connect(mixerBeginNode, MixerBeginNode.KernelPorts.Output, mixerEndNode, MixerEndNode.KernelPorts.Input);
            set.Connect(mixerBeginNode, MixerBeginNode.KernelPorts.SumWeight, mixerEndNode, MixerEndNode.KernelPorts.SumWeight);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerEndNode, MixerEndNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
                );

            var defaultStream = AnimationStream.FromDefaultValues(m_Rig);
            for(int i=0;i<m_Rig.Value.Value.Skeleton.BoneCount; i++)
            {
                Assert.That(streamECS.GetLocalToParentTranslation(i), Is.EqualTo(defaultStream.GetLocalToParentTranslation(i)));
                Assert.That(streamECS.GetLocalToParentRotation(i), Is.EqualTo(defaultStream.GetLocalToParentRotation(i)));
                Assert.That(streamECS.GetLocalToParentScale(i), Is.EqualTo(defaultStream.GetLocalToParentScale(i)));
            }
        }

        [Test]
        public void MixerEndNodeNormalizeRotation()
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var mixerBeginNode = CreateNode<MixerBeginNode>();
            set.SendMessage(mixerBeginNode, MixerBeginNode.SimulationPorts.Rig, m_Rig);

            var mixerEndNode = CreateNode<MixerEndNode>();
            set.SendMessage(mixerEndNode, MixerEndNode.SimulationPorts.Rig, m_Rig);

            set.Connect(mixerBeginNode, MixerBeginNode.KernelPorts.Output, mixerEndNode, MixerEndNode.KernelPorts.Input);
            // Set explicitly sumweight to 1 to force MixerEndNode to normalize rotation
            set.SetData(mixerEndNode, MixerEndNode.KernelPorts.SumWeight, 1.0f);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerEndNode, MixerEndNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
                );

            for(int i=0;i<m_Rig.Value.Value.Skeleton.BoneCount; i++)
            {
                var length = math.length(streamECS.GetLocalToParentRotation(i));

                Assert.That(length, Is.EqualTo(1.0f));
            }
        }


        [Test]
        [TestCase(0.0f, Description = "Mix at 0%")]
        [TestCase(0.33f, Description = "Mix at 33%")]
        [TestCase(1.0f, Description = "Mix at 100%")]
        public void CanMixOneClipWithMixerAdd(float weight)
        {
            var expectedLocalTranslation = math.lerp(float3.zero, m_ClipChildLocalTranslation1, weight);

            var q1 = new quaternion(m_ClipChildLocalRotation1.value * weight);
            var q2 = mathex.select(new quaternion(0,0,0,0), new quaternion(quaternion.identity.value * (1.0f - weight)), weight < 1.0f);
            var expectedLocalRotation = math.normalize(mathex.add(q1, q2));
            var expectedLocalScale = math.lerp(mathex.one(), m_ClipChildLocalScale1, weight);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var mixerBeginNode = CreateNode<MixerBeginNode>();
            set.SendMessage(mixerBeginNode, MixerBeginNode.SimulationPorts.Rig, m_Rig);

            var clipNode1 = CreateNode<ClipNode>();
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Rig, m_Rig);
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.Clip, m_ConstantHierarchyClip1);

            var mixerAddNode = CreateNode<MixerAddNode>();
            set.SendMessage(mixerAddNode, MixerAddNode.SimulationPorts.Rig, m_Rig);
            set.SetData(mixerAddNode, MixerAddNode.KernelPorts.Weight, weight);

            var mixerEndNode = CreateNode<MixerEndNode>();
            set.SendMessage(mixerEndNode, MixerEndNode.SimulationPorts.Rig, m_Rig);

            set.Connect(mixerBeginNode, MixerBeginNode.KernelPorts.Output, mixerAddNode, MixerAddNode.KernelPorts.Input);
            set.Connect(mixerBeginNode, MixerBeginNode.KernelPorts.SumWeight, mixerAddNode, MixerAddNode.KernelPorts.SumWeightInput);
            set.Connect(clipNode1, ClipNode.KernelPorts.Output, mixerAddNode, MixerAddNode.KernelPorts.Add);
            set.Connect(mixerAddNode, MixerAddNode.KernelPorts.SumWeightOutput, mixerEndNode, MixerEndNode.KernelPorts.SumWeight);
            set.Connect(mixerAddNode, MixerAddNode.KernelPorts.Output, mixerEndNode, MixerEndNode.KernelPorts.Input);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerEndNode, MixerEndNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
                );

            Assert.That(streamECS.GetLocalToParentTranslation(1), Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer));
            Assert.That(streamECS.GetLocalToParentRotation(1), Is.EqualTo(expectedLocalRotation).Using(RotationComparer));
            Assert.That(streamECS.GetLocalToParentScale(1), Is.EqualTo(expectedLocalScale).Using(ScaleComparer));
        }

        [Test]
        [TestCase(0.0f, Description = "Mix at 0% shouldn't throw")]
        [TestCase(0.1f, Description = "Mix at 10% shouldn't throw")]
        [TestCase(1.0f, Description = "Mix at 100% shouldn't throw")]
        public void MixerAddNodeShouldntThrowWhenAddPortIsUnconnected(float weight)
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var mixerBeginNode = CreateNode<MixerBeginNode>();
            set.SendMessage(mixerBeginNode, MixerBeginNode.SimulationPorts.Rig, m_Rig);

            var mixerAddNode = CreateNode<MixerAddNode>();
            set.SendMessage(mixerAddNode, MixerAddNode.SimulationPorts.Rig, m_Rig);
            set.SetData(mixerAddNode, MixerAddNode.KernelPorts.Weight, weight);

            var mixerEndNode = CreateNode<MixerEndNode>();
            set.SendMessage(mixerEndNode, MixerEndNode.SimulationPorts.Rig, m_Rig);

            set.Connect(mixerBeginNode, MixerBeginNode.KernelPorts.Output, mixerAddNode, MixerAddNode.KernelPorts.Input);
            set.Connect(mixerBeginNode, MixerBeginNode.KernelPorts.SumWeight, mixerAddNode, MixerAddNode.KernelPorts.SumWeightInput);
            set.Connect(mixerAddNode, MixerAddNode.KernelPorts.SumWeightOutput, mixerEndNode, MixerEndNode.KernelPorts.SumWeight);
            set.Connect(mixerAddNode, MixerAddNode.KernelPorts.Output, mixerEndNode, MixerEndNode.KernelPorts.Input);

            var entityNode = CreateComponentNode(entity);
            set.Connect(mixerEndNode, MixerEndNode.KernelPorts.Output, entityNode);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity);

            m_AnimationGraphSystem.Update();
            Assert.Pass();
        }

        [Test]
        public void MixerAddNodeReturnsPreviousInputWhenWeightIsZero()
        {
            var entity1 = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity1, m_Manager, m_Rig);

            var entity2 = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity2, m_Manager, m_Rig);

            var set = Set;

            var mixerBeginNode = CreateNode<MixerBeginNode>();
            set.SendMessage(mixerBeginNode, MixerBeginNode.SimulationPorts.Rig, m_Rig);

            var mixerAddNode = CreateNode<MixerAddNode>();
            set.SendMessage(mixerAddNode, MixerAddNode.SimulationPorts.Rig, m_Rig);
            set.SetData(mixerAddNode, MixerAddNode.KernelPorts.Weight, 0.0f);

            var mixerEndNode = CreateNode<MixerEndNode>();
            set.SendMessage(mixerEndNode, MixerEndNode.SimulationPorts.Rig, m_Rig);

            set.Connect(mixerBeginNode, MixerBeginNode.KernelPorts.Output, mixerAddNode, MixerAddNode.KernelPorts.Input);
            set.Connect(mixerBeginNode, MixerBeginNode.KernelPorts.SumWeight, mixerAddNode, MixerAddNode.KernelPorts.SumWeightInput);
            set.Connect(mixerAddNode, MixerAddNode.KernelPorts.SumWeightOutput, mixerEndNode, MixerEndNode.KernelPorts.SumWeight);
            set.Connect(mixerAddNode, MixerAddNode.KernelPorts.Output, mixerEndNode, MixerEndNode.KernelPorts.Input);

            var entityNode1 = CreateComponentNode(entity1);
            var entityNode2 = CreateComponentNode(entity2);

            set.Connect(mixerBeginNode, MixerBeginNode.KernelPorts.Output, entityNode1);
            set.Connect(mixerAddNode, MixerAddNode.KernelPorts.Output, entityNode2);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity1);
            m_Manager.AddComponent<PreAnimationGraphTag>(entity2);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity1).AsNativeArray()
                );

            var anotherStreamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity2).AsNativeArray()
                );

            for(int i=0;i<m_Rig.Value.Value.Skeleton.BoneCount; i++)
            {
                Assert.That(anotherStreamECS.GetLocalToParentTranslation(i), Is.EqualTo(streamECS.GetLocalToParentTranslation(i)).Using(TranslationComparer));
                Assert.That(anotherStreamECS.GetLocalToParentRotation(i), Is.EqualTo(streamECS.GetLocalToParentRotation(i)).Using(RotationComparer));
                Assert.That(anotherStreamECS.GetLocalToParentScale(i), Is.EqualTo(streamECS.GetLocalToParentScale(i)).Using(ScaleComparer));
            }
        }

        [Test]
        [TestCase(0.01f)]
        [TestCase(0.5f)]
        [TestCase(1.0f)]
        public void MixerAddNodeReturnsPreviousInputWhenAddPortIsUnconnected(float weight)
        {
            var entity1 = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity1, m_Manager, m_Rig);

            var entity2 = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity2, m_Manager, m_Rig);

            var set = Set;

            var mixerBeginNode = CreateNode<MixerBeginNode>();
            set.SendMessage(mixerBeginNode, MixerBeginNode.SimulationPorts.Rig, m_Rig);

            var mixerAddNode = CreateNode<MixerAddNode>();
            set.SendMessage(mixerAddNode, MixerAddNode.SimulationPorts.Rig, m_Rig);
            set.SetData(mixerAddNode, MixerAddNode.KernelPorts.Weight, weight);

            var mixerEndNode = CreateNode<MixerEndNode>();
            set.SendMessage(mixerEndNode, MixerEndNode.SimulationPorts.Rig, m_Rig);

            set.Connect(mixerBeginNode, MixerBeginNode.KernelPorts.Output, mixerAddNode, MixerAddNode.KernelPorts.Input);
            set.Connect(mixerBeginNode, MixerBeginNode.KernelPorts.SumWeight, mixerAddNode, MixerAddNode.KernelPorts.SumWeightInput);
            set.Connect(mixerAddNode, MixerAddNode.KernelPorts.SumWeightOutput, mixerEndNode, MixerEndNode.KernelPorts.SumWeight);
            set.Connect(mixerAddNode, MixerAddNode.KernelPorts.Output, mixerEndNode, MixerEndNode.KernelPorts.Input);

            var entityNode1 = CreateComponentNode(entity1);
            var entityNode2 = CreateComponentNode(entity2);

            set.Connect(mixerBeginNode, MixerBeginNode.KernelPorts.Output, entityNode1);
            set.Connect(mixerAddNode, MixerAddNode.KernelPorts.Output, entityNode2);

            m_Manager.AddComponent<PreAnimationGraphTag>(entity1);
            m_Manager.AddComponent<PreAnimationGraphTag>(entity2);

            m_AnimationGraphSystem.Update();

            var streamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity1).AsNativeArray()
                );

            var anotherStreamECS = AnimationStream.CreateReadOnly(
                m_Rig,
                m_Manager.GetBuffer<AnimatedData>(entity2).AsNativeArray()
                );

            for(int i=0;i<m_Rig.Value.Value.Skeleton.BoneCount; i++)
            {
                Assert.That(anotherStreamECS.GetLocalToParentTranslation(i), Is.EqualTo(streamECS.GetLocalToParentTranslation(i)).Using(TranslationComparer));
                Assert.That(anotherStreamECS.GetLocalToParentRotation(i), Is.EqualTo(streamECS.GetLocalToParentRotation(i)).Using(RotationComparer));
                Assert.That(anotherStreamECS.GetLocalToParentScale(i), Is.EqualTo(streamECS.GetLocalToParentScale(i)).Using(ScaleComparer));
            }
        }
    }
}
