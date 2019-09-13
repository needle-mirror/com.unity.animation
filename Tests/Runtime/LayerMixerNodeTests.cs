using NUnit.Framework;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.TestTools;

namespace Unity.Animation.Tests
{
    public class LayerMixerNodeTests : AnimationTestsFixture, IPrebuildSetup
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

        private BlobAssetReference<RigDefinition> m_Rig;
        private BlobAssetReference<ClipInstance> m_ConstantClip1;
        private BlobAssetReference<ClipInstance> m_ConstantClip2;

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
                    LocalTranslationDefaultValue = new float3(0,0.95f,0),
                    LocalRotationDefaultValue = new quaternion(0,0,0,1),
                    LocalScaleDefaultValue = new float3(2,2,2),
                },
                new SkeletonNode
                {
                    ParentIndex = 0, Id = "Child2", AxisIndex = -1,
                    LocalTranslationDefaultValue = new float3(0,0.10f,0),
                    LocalRotationDefaultValue = new quaternion(0,0,0,1),
                    LocalScaleDefaultValue = new float3(3,3,3),
                },
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes);
        }


        public void Setup()
        {
#if UNITY_EDITOR
            var denseClip1 = CreateConstantDenseClip(
                        new[] { ("Root", m_ClipRootLocalTranslation1), ("Child1", m_ClipChildLocalTranslation1) },
                        new[] { ("Root", m_ClipRootLocalRotation1), ("Child1", m_ClipChildLocalRotation1) },
                        new[] { ("Root", m_ClipRootLocalScale1),    ("Child1", m_ClipChildLocalScale1) });

            var blobPath = "LayerMixerNodeTestsDenseClip1.blob";
            BlobFile.WriteBlobAsset(ref denseClip1, blobPath);

            var denseClip2 = CreateConstantDenseClip(
                        new[] { ("Root", m_ClipRootLocalTranslation2), ("Child1", m_ClipChildLocalTranslation2) },
                        new[] { ("Root", m_ClipRootLocalRotation2), ("Child1", m_ClipChildLocalRotation2) },
                        new[] { ("Root", m_ClipRootLocalScale2),    ("Child1", m_ClipChildLocalScale2) });

            blobPath = "LayerMixerNodeTestsDenseClip2.blob";
            BlobFile.WriteBlobAsset(ref denseClip2, blobPath);
#endif
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // Create rig
            m_Rig = CreateTestRigDefinition();

            // Clip #1
            {
                var path = "LayerMixerNodeTestsDenseClip1.blob";
                var denseClip = BlobFile.ReadBlobAsset<Clip>(path);
                m_ConstantClip1 = ClipManager.Instance.GetClipFor(m_Rig, denseClip);
            }

            // Clip #2
            {
                var path = "LayerMixerNodeTestsDenseClip2.blob";
                var denseClip = BlobFile.ReadBlobAsset<Clip>(path);
                m_ConstantClip2 = ClipManager.Instance.GetClipFor(m_Rig, denseClip);
            }
        }

        [Test]
        public void CanSetRigDefinition()
        {
            var set = Set;
            var layerMixer = CreateNode<LayerMixerNode>();

            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.RigDefinition, m_Rig);

            var otherRig = set.GetFunctionality(layerMixer).ExposeKernelData(layerMixer).RigDefinition;

            Assert.That(otherRig, Is.EqualTo(m_Rig));
        }

        [Test]
        public void CanSetBlendingMode()
        {
            var set = Set;
            var layerMixer = CreateNode<LayerMixerNode>();

            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.RigDefinition, m_Rig);

            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendModeInput0, BlendingMode.Override);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendModeInput2, BlendingMode.Override);

            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendModeInput1, BlendingMode.Additive);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendModeInput3, BlendingMode.Additive);

            var blendModeInput0 = set.GetFunctionality(layerMixer).ExposeKernelData(layerMixer).BlendingModeInput0;
            var blendModeInput1 = set.GetFunctionality(layerMixer).ExposeKernelData(layerMixer).BlendingModeInput1;
            var blendModeInput2 = set.GetFunctionality(layerMixer).ExposeKernelData(layerMixer).BlendingModeInput2;
            var blendModeInput3 = set.GetFunctionality(layerMixer).ExposeKernelData(layerMixer).BlendingModeInput3;

            Assert.AreEqual(BlendingMode.Override, blendModeInput0);
            Assert.AreEqual(BlendingMode.Override, blendModeInput2);

            Assert.AreEqual(BlendingMode.Additive, blendModeInput1);
            Assert.AreEqual(BlendingMode.Additive, blendModeInput3);
        }

        [Test]
        public void CanSetWeight()
        {
            var set = Set;
            var layerMixer = CreateNode<LayerMixerNode>();

            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.RigDefinition, m_Rig);

            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.WeightInput0, 1.0f);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.WeightInput1, 0.5f);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.WeightInput2, 0.33f);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.WeightInput3, 0.0f);

            var weightInput1 = set.GetFunctionality(layerMixer).ExposeKernelData(layerMixer).WeightInput0;
            var weightInput2 = set.GetFunctionality(layerMixer).ExposeKernelData(layerMixer).WeightInput1;
            var weightInput3 = set.GetFunctionality(layerMixer).ExposeKernelData(layerMixer).WeightInput2;
            var weightInput4 = set.GetFunctionality(layerMixer).ExposeKernelData(layerMixer).WeightInput3;

            Assert.AreEqual(1.0f, weightInput1);
            Assert.AreEqual(0.5f, weightInput2);
            Assert.AreEqual(0.33f, weightInput3);
            Assert.AreEqual(0.0f, weightInput4);
        }

        [Test]
        public void CanSetMask()
        {
            var set = Set;
            var layerMixer = CreateNode<LayerMixerNode>();

            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.RigDefinition, m_Rig);

            var mask0 = new NativeBitSet(m_Rig.Value.Bindings.BindingCount, Allocator.Persistent);
            var mask1 = new NativeBitSet(m_Rig.Value.Bindings.BindingCount, Allocator.Persistent);
            var mask2 = new NativeBitSet(m_Rig.Value.Bindings.BindingCount, Allocator.Persistent);
            var mask3 = new NativeBitSet(m_Rig.Value.Bindings.BindingCount, Allocator.Persistent);

            mask1.Flip();

            for(int i=0;i<mask3.Length;i += 2)
                mask3.Flip(i);

            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.MaskInput0, mask0);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.MaskInput1, mask1);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.MaskInput2, mask2);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.MaskInput3, mask3);

            var maskInput0 = set.GetFunctionality(layerMixer).ExposeKernelData(layerMixer).MaskInput0;
            var maskInput1 = set.GetFunctionality(layerMixer).ExposeKernelData(layerMixer).MaskInput1;
            var maskInput2 = set.GetFunctionality(layerMixer).ExposeKernelData(layerMixer).MaskInput2;
            var maskInput3 = set.GetFunctionality(layerMixer).ExposeKernelData(layerMixer).MaskInput3;

            Assert.AreEqual(mask0, maskInput0);
            Assert.AreEqual(mask1, maskInput1);
            Assert.AreEqual(mask2, maskInput2);
            Assert.AreEqual(mask3, maskInput3);

            mask0.Dispose();
            mask1.Dispose();
            mask2.Dispose();
            mask3.Dispose();
        }

        [Test]
        public void CannotSetMaskWithoutSettingRig()
        {
            var set = Set;
            var layerMixer = CreateNode<LayerMixerNode>();

            var mask1 = new NativeBitSet(10, Allocator.Persistent);

            Assert.Throws<System.NullReferenceException>(() => set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.MaskInput1, mask1));

            mask1.Dispose();
        }

        [Test]
        public void CannotSetMaskIfLengthDoesntMatchRig()
        {
            var set = Set;
            var layerMixer = CreateNode<LayerMixerNode>();

            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.RigDefinition, m_Rig);

            var mask0 = new NativeBitSet(m_Rig.Value.Bindings.BindingCount+10, Allocator.Persistent);

            Assert.Throws<System.ArgumentException>(() => set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.MaskInput0, mask0));

            mask0.Dispose();
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

            var rig = RigBuilder.CreateRigDefinition(skeletonNodes);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, rig);

            var set = Set;
            var layerMixer = CreateNode<LayerMixerNode>();
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.RigDefinition, rig);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(layerMixer, LayerMixerNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var localTranslationBuffer = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity).Reinterpret<float3>();
            var localRotationBuffer = m_Manager.GetBuffer<AnimatedLocalRotation>(entity).Reinterpret<quaternion>();
            var localScaleBuffer = m_Manager.GetBuffer<AnimatedLocalScale>(entity).Reinterpret<float3>();

            Assert.AreEqual(skeletonNodes.Length, localTranslationBuffer.Length);
            Assert.AreEqual(skeletonNodes.Length, localRotationBuffer.Length);
            Assert.AreEqual(skeletonNodes.Length, localScaleBuffer.Length);

            for(int i=0;i<skeletonNodes.Length;i++)
            {
                var localTranslation = localTranslationBuffer[i];
                var localRotation = localRotationBuffer[i];
                var localScale = localScaleBuffer[i];

                Assert.That(localTranslation, Is.EqualTo(skeletonNodes[i].LocalTranslationDefaultValue).Using(TranslationComparer));
                Assert.That(localRotation, Is.EqualTo(skeletonNodes[i].LocalRotationDefaultValue).Using(RotationComparer));
                Assert.That(localScale, Is.EqualTo(skeletonNodes[i].LocalScaleDefaultValue).Using(ScaleComparer));
            }
        }

        [Test]
        public void CanMixOneOverrideLayer()
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.ClipInstance, m_ConstantClip1);

            var layerMixer = CreateNode<LayerMixerNode>();
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.RigDefinition, m_Rig);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.WeightInput0, 1.0f);

            set.Connect(clipNode, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Input0);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(layerMixer, LayerMixerNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var localTranslationBuffer = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity).Reinterpret<float3>();
            var localRotationBuffer = m_Manager.GetBuffer<AnimatedLocalRotation>(entity).Reinterpret<quaternion>();
            var localScaleBuffer = m_Manager.GetBuffer<AnimatedLocalScale>(entity).Reinterpret<float3>();

            var localTranslation = localTranslationBuffer[0];
            var localRotation = localRotationBuffer[0];
            var localScale = localScaleBuffer[0];

            Assert.That(localTranslation, Is.EqualTo(m_ClipRootLocalTranslation1).Using(TranslationComparer), "Root localTranslation doesn't match clip localTranslation");
            Assert.That(localRotation, Is.EqualTo(m_ClipRootLocalRotation1).Using(RotationComparer), "Root localRotation doesn't match clip localRotation");
            Assert.That(localScale, Is.EqualTo(m_ClipRootLocalScale1).Using(ScaleComparer), "Root localScale doesn't match clip localScale");

            localTranslation = localTranslationBuffer[1];
            localRotation = localRotationBuffer[1];
            localScale = localScaleBuffer[1];

            Assert.That(localTranslation, Is.EqualTo(m_ClipChildLocalTranslation1).Using(TranslationComparer), "Child1 localTranslation doesn't match clip localTranslation");
            Assert.That(localRotation, Is.EqualTo(m_ClipChildLocalRotation1).Using(RotationComparer), "Child1 localRotation doesn't match clip localRotation");
            Assert.That(localScale, Is.EqualTo(m_ClipChildLocalScale1).Using(ScaleComparer), "Child1 localScale doesn't match clip localScale");
        }

        [Test]
        public void CanMixMultipleOverrideLayer()
        {
            const float k_BlendValue = 0.3f;
            var expectedLocalTranslation = math.lerp(m_ClipChildLocalTranslation1, m_ClipChildLocalTranslation2, k_BlendValue);
            var expectedLocalRotation = math.slerp(m_ClipChildLocalRotation1, m_ClipChildLocalRotation2, k_BlendValue);
            var expectedLocalScale = math.lerp(m_ClipChildLocalScale1, m_ClipChildLocalScale2, k_BlendValue);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var clipNode1 = CreateNode<ClipNode>();
            set.SendMessage(clipNode1, ClipNode.SimulationPorts.ClipInstance, m_ConstantClip1);

            var clipNode2 = CreateNode<ClipNode>();
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.ClipInstance, m_ConstantClip2);

            var layerMixer = CreateNode<LayerMixerNode>();
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.RigDefinition, m_Rig);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.WeightInput0, 1.0f);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.WeightInput1, k_BlendValue);

            set.Connect(clipNode1, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Input0);
            set.Connect(clipNode2, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Input1);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(layerMixer, LayerMixerNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var localTranslationBuffer = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity).Reinterpret<float3>();
            var localRotationBuffer = m_Manager.GetBuffer<AnimatedLocalRotation>(entity).Reinterpret<quaternion>();
            var localScaleBuffer = m_Manager.GetBuffer<AnimatedLocalScale>(entity).Reinterpret<float3>();

            var localTranslation = localTranslationBuffer[1];
            var localRotation = localRotationBuffer[1];
            var localScale = localScaleBuffer[1];

            Assert.That(localTranslation, Is.EqualTo(expectedLocalTranslation).Using(TranslationComparer), "Child1 localTranslation doesn't match blended value");
            Assert.That(localRotation, Is.EqualTo(expectedLocalRotation).Using(RotationComparer), "Child1 localRotation doesn't match blended value");
            Assert.That(localScale, Is.EqualTo(expectedLocalScale).Using(ScaleComparer), "Child1 localScale doesn't match blended value");
        }

        [Test]
        [TestCase(0.0f)]
        [TestCase(0.5f)]
        [TestCase(1.0f)]
        public void CanMixOneAdditiveLayer(float layerWeight)
        {
            var expectedRootLocalTranslation1 = m_Rig.Value.DefaultValues.LocalTranslations[0] + (m_ClipRootLocalTranslation1 * layerWeight);
            var expectedRootLocalRotation1 = math.mul(m_Rig.Value.DefaultValues.LocalRotations[0], mathex.quatWeight(m_ClipRootLocalRotation1, layerWeight));
            var expectedRootLocalScale1 = m_Rig.Value.DefaultValues.LocalScales[0] + (m_ClipRootLocalScale1 * layerWeight);

            var expectedChildLocalTranslation1 = m_Rig.Value.DefaultValues.LocalTranslations[1] + (m_ClipChildLocalTranslation1 * layerWeight);
            var expectedChildLocalRotation1 = math.mul(m_Rig.Value.DefaultValues.LocalRotations[1], mathex.quatWeight(m_ClipChildLocalRotation1, layerWeight));
            var expectedChildLocalScale1 = m_Rig.Value.DefaultValues.LocalScales[1] + (m_ClipChildLocalScale1 * layerWeight);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.ClipInstance, m_ConstantClip1);

            var layerMixer = CreateNode<LayerMixerNode>();
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.RigDefinition, m_Rig);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.WeightInput0, layerWeight);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendModeInput0, BlendingMode.Additive);

            set.Connect(clipNode, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Input0);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(layerMixer, LayerMixerNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var localTranslationBuffer = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity).Reinterpret<float3>();
            var localRotationBuffer = m_Manager.GetBuffer<AnimatedLocalRotation>(entity).Reinterpret<quaternion>();
            var localScaleBuffer = m_Manager.GetBuffer<AnimatedLocalScale>(entity).Reinterpret<float3>();

            var localTranslation = localTranslationBuffer[0];
            var localRotation = localRotationBuffer[0];
            var localScale = localScaleBuffer[0];

            Assert.That(localTranslation, Is.EqualTo(expectedRootLocalTranslation1).Using(TranslationComparer), "Root localTranslation doesn't match expected value");
            Assert.That(localRotation, Is.EqualTo(expectedRootLocalRotation1).Using(RotationComparer), "Root localRotation doesn't match expected value");
            Assert.That(localScale, Is.EqualTo(expectedRootLocalScale1).Using(ScaleComparer), "Root localScale doesn't match expected value");

            localTranslation = localTranslationBuffer[1];
            localRotation = localRotationBuffer[1];
            localScale = localScaleBuffer[1];

            Assert.That(localTranslation, Is.EqualTo(expectedChildLocalTranslation1).Using(TranslationComparer), "Child1 localTranslation doesn't match expected value");
            Assert.That(localRotation, Is.EqualTo(expectedChildLocalRotation1).Using(RotationComparer), "Child1 localRotation doesn't match expected value");
            Assert.That(localScale, Is.EqualTo(expectedChildLocalScale1).Using(ScaleComparer), "Child1 localScale doesn't match expected value");
        }

        [Test]
        [TestCase(0.0f, 0.0f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(1.0f, 1.0f)]
        [TestCase(0.0f, 0.5f)]
        [TestCase(0.0f, 1.0f)]
        public void CanMixMultipleAdditiveLayer(float layer1Weight, float layer2Weight)
        {
            var expectedRootLocalTranslation1 = m_Rig.Value.DefaultValues.LocalTranslations[0] + (m_ClipRootLocalTranslation1 * layer1Weight) + (m_ClipRootLocalTranslation2 * layer2Weight);
            var expectedRootLocalRotation1 = math.mul(math.mul(m_Rig.Value.DefaultValues.LocalRotations[0],  mathex.quatWeight(m_ClipRootLocalRotation1, layer1Weight)), mathex.quatWeight(m_ClipRootLocalRotation2, layer2Weight));
            var expectedRootLocalScale1 = m_Rig.Value.DefaultValues.LocalScales[0] + (m_ClipRootLocalScale1 * layer1Weight) + (m_ClipRootLocalScale2 * layer2Weight);

            var expectedChildLocalTranslation1 = m_Rig.Value.DefaultValues.LocalTranslations[1] + (m_ClipChildLocalTranslation1 * layer1Weight) + (m_ClipChildLocalTranslation2 * layer2Weight);
            var expectedChildLocalRotation1 = math.mul(math.mul(m_Rig.Value.DefaultValues.LocalRotations[1],  mathex.quatWeight(m_ClipChildLocalRotation1, layer1Weight)), mathex.quatWeight(m_ClipChildLocalRotation2, layer2Weight));
            var expectedChildLocalScale1 = m_Rig.Value.DefaultValues.LocalScales[1] + (m_ClipChildLocalScale1 * layer1Weight) + (m_ClipChildLocalScale2 * layer2Weight);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.ClipInstance, m_ConstantClip1);

            var clipNode2 = CreateNode<ClipNode>();
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.ClipInstance, m_ConstantClip2);

            var layerMixer = CreateNode<LayerMixerNode>();
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.RigDefinition, m_Rig);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.WeightInput0, layer1Weight);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendModeInput0, BlendingMode.Additive);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.WeightInput1, layer2Weight);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendModeInput1, BlendingMode.Additive);

            set.Connect(clipNode, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Input0);
            set.Connect(clipNode2, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Input1);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(layerMixer, LayerMixerNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var localTranslationBuffer = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity).Reinterpret<float3>();
            var localRotationBuffer = m_Manager.GetBuffer<AnimatedLocalRotation>(entity).Reinterpret<quaternion>();
            var localScaleBuffer = m_Manager.GetBuffer<AnimatedLocalScale>(entity).Reinterpret<float3>();

            var localTranslation = localTranslationBuffer[0];
            var localRotation = localRotationBuffer[0];
            var localScale = localScaleBuffer[0];

            Assert.That(localTranslation, Is.EqualTo(expectedRootLocalTranslation1).Using(TranslationComparer), "Root localTranslation doesn't match expected value");
            Assert.That(localRotation, Is.EqualTo(expectedRootLocalRotation1).Using(RotationComparer), "Root localRotation doesn't match expected value");
            Assert.That(localScale, Is.EqualTo(expectedRootLocalScale1).Using(ScaleComparer), "Root localScale doesn't match expected value");

            localTranslation = localTranslationBuffer[1];
            localRotation = localRotationBuffer[1];
            localScale = localScaleBuffer[1];

            Assert.That(localTranslation, Is.EqualTo(expectedChildLocalTranslation1).Using(TranslationComparer), "Child1 localTranslation doesn't match expected value");
            Assert.That(localRotation, Is.EqualTo(expectedChildLocalRotation1).Using(RotationComparer), "Child1 localRotation doesn't match expected value");
            Assert.That(localScale, Is.EqualTo(expectedChildLocalScale1).Using(ScaleComparer), "Child1 localScale doesn't match expected value");
        }

        [Test]
        [TestCase(0.0f, 0.0f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(1.0f, 1.0f)]
        [TestCase(0.0f, 0.5f)]
        [TestCase(0.0f, 1.0f)]
        public void CanMixMultipleAdditiveLayerAndMaskChannel (float layer0Weight, float layer1Weight)
        {
            // Root is masked on layer 2
            var expectedRootLocalTranslation1 = m_Rig.Value.DefaultValues.LocalTranslations[0] + (m_ClipRootLocalTranslation1 * layer0Weight);
            var expectedRootLocalRotation1 = math.mul(m_Rig.Value.DefaultValues.LocalRotations[0], mathex.quatWeight(m_ClipRootLocalRotation1, layer0Weight));
            var expectedRootLocalScale1 = m_Rig.Value.DefaultValues.LocalScales[0] + (m_ClipRootLocalScale1 * layer0Weight);

            var expectedChildLocalTranslation1 = m_Rig.Value.DefaultValues.LocalTranslations[1] + (m_ClipChildLocalTranslation1 * layer0Weight) + (m_ClipChildLocalTranslation2 * layer1Weight);
            var expectedChildLocalRotation1 = math.mul(math.mul(m_Rig.Value.DefaultValues.LocalRotations[1], mathex.quatWeight(m_ClipChildLocalRotation1, layer0Weight)), mathex.quatWeight(m_ClipChildLocalRotation2, layer1Weight));
            var expectedChildLocalScale1 = m_Rig.Value.DefaultValues.LocalScales[1] + (m_ClipChildLocalScale1 * layer0Weight) + (m_ClipChildLocalScale2 * layer1Weight);

            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.ClipInstance, m_ConstantClip1);

            var clipNode2 = CreateNode<ClipNode>();
            set.SendMessage(clipNode2, ClipNode.SimulationPorts.ClipInstance, m_ConstantClip2);

            var mask = new NativeBitSet(m_Rig.Value.Bindings.BindingCount, Allocator.Temp);

            // layer 2 only affect Child1 translation, rotation, and scale
            mask.Flip(m_Rig.Value.Bindings.TranslationBindingIndex + 1);
            mask.Flip(m_Rig.Value.Bindings.RotationBindingIndex + 1);
            mask.Flip(m_Rig.Value.Bindings.ScaleBindingIndex + 1);

            var layerMixer = CreateNode<LayerMixerNode>();
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.RigDefinition, m_Rig);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.WeightInput0, layer0Weight);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendModeInput0, BlendingMode.Additive);

            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.WeightInput1, layer1Weight);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.BlendModeInput1, BlendingMode.Additive);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.MaskInput1, mask);

            set.Connect(clipNode, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Input0);
            set.Connect(clipNode2, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Input1);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(layerMixer, LayerMixerNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var localTranslationBuffer = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity).Reinterpret<float3>();
            var localRotationBuffer = m_Manager.GetBuffer<AnimatedLocalRotation>(entity).Reinterpret<quaternion>();
            var localScaleBuffer = m_Manager.GetBuffer<AnimatedLocalScale>(entity).Reinterpret<float3>();

            var localTranslation = localTranslationBuffer[0];
            var localRotation = localRotationBuffer[0];
            var localScale = localScaleBuffer[0];

            Assert.That(localTranslation, Is.EqualTo(expectedRootLocalTranslation1).Using(TranslationComparer), "Root localTranslation doesn't match expected value");
            Assert.That(localRotation, Is.EqualTo(expectedRootLocalRotation1).Using(RotationComparer), "Root localRotation doesn't match expected value");
            Assert.That(localScale, Is.EqualTo(expectedRootLocalScale1).Using(ScaleComparer), "Root localScale doesn't match expected value");

            localTranslation = localTranslationBuffer[1];
            localRotation = localRotationBuffer[1];
            localScale = localScaleBuffer[1];

            Assert.That(localTranslation, Is.EqualTo(expectedChildLocalTranslation1).Using(TranslationComparer), "Child1 localTranslation doesn't match expected value");
            Assert.That(localRotation, Is.EqualTo(expectedChildLocalRotation1).Using(RotationComparer), "Child1 localRotation doesn't match expected value");
            Assert.That(localScale, Is.EqualTo(expectedChildLocalScale1).Using(ScaleComparer), "Child1 localScale doesn't match expected value");
        }

        [Test]
        public void BlendingInAnUnconnectedLayerDoesntCrash()
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var layerMixer = CreateNode<LayerMixerNode>();
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.RigDefinition, m_Rig);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.WeightInput0, 1.0f);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.WeightInput1, 1.0f);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.WeightInput2, 1.0f);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.WeightInput3, 1.0f);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(layerMixer, LayerMixerNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            Assert.DoesNotThrow(() => m_AnimationGraphSystem.Update());
        }

        [Test]
        public void CanMaskChannel()
        {
            var entity = m_Manager.CreateEntity();
            RigEntityBuilder.SetupRigEntity(entity, m_Manager, m_Rig);

            var set = Set;

            var clipNode = CreateNode<ClipNode>();
            set.SendMessage(clipNode, ClipNode.SimulationPorts.ClipInstance, m_ConstantClip1);

            var mask = new NativeBitSet(m_Rig.Value.Bindings.BindingCount, Allocator.Temp);

            // layer 1 only affect Child1 translation, rotation, and scale
            mask.Flip(m_Rig.Value.Bindings.TranslationBindingIndex + 1);
            mask.Flip(m_Rig.Value.Bindings.RotationBindingIndex + 1);
            mask.Flip(m_Rig.Value.Bindings.ScaleBindingIndex + 1);

            var layerMixer = CreateNode<LayerMixerNode>();
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.RigDefinition, m_Rig);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.WeightInput0, 1.0f);
            set.SendMessage(layerMixer, LayerMixerNode.SimulationPorts.MaskInput0, mask);

            mask.Dispose();

            set.Connect(clipNode, ClipNode.KernelPorts.Output, layerMixer, LayerMixerNode.KernelPorts.Input0);

            var output = new GraphOutput { Buffer = CreateGraphBuffer(layerMixer, LayerMixerNode.KernelPorts.Output) };
            m_Manager.AddComponentData(entity, output);

            m_AnimationGraphSystem.Update();

            // Get local translation, rotation, scale.
            var localTranslationBuffer = m_Manager.GetBuffer<AnimatedLocalTranslation>(entity).Reinterpret<float3>();
            var localRotationBuffer = m_Manager.GetBuffer<AnimatedLocalRotation>(entity).Reinterpret<quaternion>();
            var localScaleBuffer = m_Manager.GetBuffer<AnimatedLocalScale>(entity).Reinterpret<float3>();

            var localTranslation = localTranslationBuffer[0];
            var localRotation = localRotationBuffer[0];
            var localScale = localScaleBuffer[0];

            Assert.That(localTranslation, Is.EqualTo(m_Rig.Value.DefaultValues.LocalTranslations[0]).Using(TranslationComparer), "Root localTranslation doesn't match DefaultValues localTranslation");
            Assert.That(localRotation, Is.EqualTo(m_Rig.Value.DefaultValues.LocalRotations[0]).Using(RotationComparer), "Root localRotation doesn't match DefaultValues localRotation");
            Assert.That(localScale, Is.EqualTo(m_Rig.Value.DefaultValues.LocalScales[0]).Using(ScaleComparer), "Root localScale doesn't match DefaultValues localScale");

            localTranslation = localTranslationBuffer[1];
            localRotation = localRotationBuffer[1];
            localScale = localScaleBuffer[1];

            Assert.That(localTranslation, Is.EqualTo(m_ClipChildLocalTranslation1).Using(TranslationComparer), "Child1 localTranslation doesn't match clip localTranslation");
            Assert.That(localRotation, Is.EqualTo(m_ClipChildLocalRotation1).Using(RotationComparer), "Child1 localRotation doesn't match clip localRotation");
            Assert.That(localScale, Is.EqualTo(m_ClipChildLocalScale1).Using(ScaleComparer), "Child1 localScale doesn't match clip localScale");
        }
    }
}
