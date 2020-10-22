using NUnit.Framework;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation.Tests
{
    public class AnimationStreamNodeTests : AnimationTestsFixture
    {
        private Rig m_Rig;

        private static BlobAssetReference<RigDefinition> CreateTestRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode { ParentIndex = -1, Id = "Root", AxisIndex = -1, LocalTranslationDefaultValue = new float3(100.0f, 0.0f, 0.0f), LocalRotationDefaultValue = quaternion.RotateX(math.radians(90.0f)), LocalScaleDefaultValue = new float3(10.0f, 1.0f, 1.0f) },
                new SkeletonNode { ParentIndex = 0, Id = "Child1", AxisIndex = -1, LocalTranslationDefaultValue = new float3(0.0f, 100.0f, 0.0f), LocalRotationDefaultValue = quaternion.RotateY(math.radians(90.0f)), LocalScaleDefaultValue = new float3(1.0f, 1.0f, 10.0f) }
            };

            var animationChannel = new IAnimationChannel[]
            {
                new FloatChannel {Id = "Root", DefaultValue = 10f },
                new FloatChannel {Id = "Child1", DefaultValue = 10f },
                new IntChannel {Id = "Root", DefaultValue = 20 },
                new IntChannel {Id = "Child1", DefaultValue = 20 }
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannel);
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            m_Rig = new Rig { Value = CreateTestRigDefinition() };
        }

        [Test]
        public void CanGetLocalToParentValuesUsingAnimationStreamNode()
        {
            var defaultValuesNode = CreateNode<DefaultValuesNode>();
            Set.SendMessage(defaultValuesNode, DefaultValuesNode.SimulationPorts.Rig, m_Rig);

            var localToParentRootNode = CreateNode<GetAnimationStreamLocalToParentNode>();
            Set.SendMessage(localToParentRootNode, GetAnimationStreamLocalToParentNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(localToParentRootNode, GetAnimationStreamLocalToParentNode.KernelPorts.Index, 0);

            var localToParentChildNode = CreateNode<GetAnimationStreamLocalToParentNode>();
            Set.SendMessage(localToParentChildNode, GetAnimationStreamLocalToParentNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(localToParentChildNode, GetAnimationStreamLocalToParentNode.KernelPorts.Index, 1);

            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, localToParentRootNode, GetAnimationStreamLocalToParentNode.KernelPorts.Input);
            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, localToParentChildNode, GetAnimationStreamLocalToParentNode.KernelPorts.Input);

            var outRootT = Set.CreateGraphValue(localToParentRootNode, GetAnimationStreamLocalToParentNode.KernelPorts.Translation);
            var outRootR = Set.CreateGraphValue(localToParentRootNode, GetAnimationStreamLocalToParentNode.KernelPorts.Rotation);
            var outRootS = Set.CreateGraphValue(localToParentRootNode, GetAnimationStreamLocalToParentNode.KernelPorts.Scale);
            var outRootTx = Set.CreateGraphValue(localToParentRootNode, GetAnimationStreamLocalToParentNode.KernelPorts.Transform);

            var outChildT = Set.CreateGraphValue(localToParentChildNode, GetAnimationStreamLocalToParentNode.KernelPorts.Translation);
            var outChildR = Set.CreateGraphValue(localToParentChildNode, GetAnimationStreamLocalToParentNode.KernelPorts.Rotation);
            var outChildS = Set.CreateGraphValue(localToParentChildNode, GetAnimationStreamLocalToParentNode.KernelPorts.Scale);
            var outChildTx = Set.CreateGraphValue(localToParentChildNode, GetAnimationStreamLocalToParentNode.KernelPorts.Transform);

            Set.Update(default);

            var resRootT = Set.GetValueBlocking(outRootT);
            var resRootR = Set.GetValueBlocking(outRootR);
            var resRootS = Set.GetValueBlocking(outRootS);
            var resRootTx = Set.GetValueBlocking(outRootTx);

            var resChildT = Set.GetValueBlocking(outChildT);
            var resChildR = Set.GetValueBlocking(outChildR);
            var resChildS = Set.GetValueBlocking(outChildS);
            var resChildTx = Set.GetValueBlocking(outChildTx);

            var defaultStream = AnimationStream.FromDefaultValues(m_Rig);

            Assert.That(resRootT, Is.EqualTo(defaultStream.GetLocalToParentTranslation(0)).Using(TranslationComparer));
            Assert.That(resRootR, Is.EqualTo(defaultStream.GetLocalToParentRotation(0)).Using(RotationComparer));
            Assert.That(resRootS, Is.EqualTo(defaultStream.GetLocalToParentScale(0)).Using(ScaleComparer));
            Assert.AreEqual(resRootTx, mathex.float4x4(defaultStream.GetLocalToParentMatrix(0)));

            Assert.That(resChildT, Is.EqualTo(defaultStream.GetLocalToParentTranslation(1)).Using(TranslationComparer));
            Assert.That(resChildR, Is.EqualTo(defaultStream.GetLocalToParentRotation(1)).Using(RotationComparer));
            Assert.That(resChildS, Is.EqualTo(defaultStream.GetLocalToParentScale(1)).Using(ScaleComparer));
            Assert.AreEqual(resChildTx, mathex.float4x4(defaultStream.GetLocalToParentMatrix(1)));

            Set.ReleaseGraphValue(outRootT);
            Set.ReleaseGraphValue(outRootR);
            Set.ReleaseGraphValue(outRootS);
            Set.ReleaseGraphValue(outRootTx);
            Set.ReleaseGraphValue(outChildT);
            Set.ReleaseGraphValue(outChildR);
            Set.ReleaseGraphValue(outChildS);
            Set.ReleaseGraphValue(outChildTx);
        }

        [Test]
        public void CanGetLocalToRootValuesUsingAnimationStreamNode()
        {
            var defaultValuesNode = CreateNode<DefaultValuesNode>();
            Set.SendMessage(defaultValuesNode, DefaultValuesNode.SimulationPorts.Rig, m_Rig);

            var localToRootRootNode = CreateNode<GetAnimationStreamLocalToRootNode>();
            Set.SendMessage(localToRootRootNode, GetAnimationStreamLocalToRootNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(localToRootRootNode, GetAnimationStreamLocalToRootNode.KernelPorts.Index, 0);

            var localToRootChildNode = CreateNode<GetAnimationStreamLocalToRootNode>();
            Set.SendMessage(localToRootChildNode, GetAnimationStreamLocalToRootNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(localToRootChildNode, GetAnimationStreamLocalToRootNode.KernelPorts.Index, 1);

            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, localToRootRootNode, GetAnimationStreamLocalToRootNode.KernelPorts.Input);
            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, localToRootChildNode, GetAnimationStreamLocalToRootNode.KernelPorts.Input);

            var outRootT = Set.CreateGraphValue(localToRootRootNode, GetAnimationStreamLocalToRootNode.KernelPorts.Translation);
            var outRootR = Set.CreateGraphValue(localToRootRootNode, GetAnimationStreamLocalToRootNode.KernelPorts.Rotation);
            var outRootS = Set.CreateGraphValue(localToRootRootNode, GetAnimationStreamLocalToRootNode.KernelPorts.Scale);
            var outRootTx = Set.CreateGraphValue(localToRootRootNode, GetAnimationStreamLocalToRootNode.KernelPorts.Transform);

            var outChildT = Set.CreateGraphValue(localToRootChildNode, GetAnimationStreamLocalToRootNode.KernelPorts.Translation);
            var outChildR = Set.CreateGraphValue(localToRootChildNode, GetAnimationStreamLocalToRootNode.KernelPorts.Rotation);
            var outChildS = Set.CreateGraphValue(localToRootChildNode, GetAnimationStreamLocalToRootNode.KernelPorts.Scale);
            var outChildTx = Set.CreateGraphValue(localToRootChildNode, GetAnimationStreamLocalToRootNode.KernelPorts.Transform);

            Set.Update(default);

            var resRootT = Set.GetValueBlocking(outRootT);
            var resRootR = Set.GetValueBlocking(outRootR);
            var resRootS = Set.GetValueBlocking(outRootS);
            var resRootTx = Set.GetValueBlocking(outRootTx);

            var resChildT = Set.GetValueBlocking(outChildT);
            var resChildR = Set.GetValueBlocking(outChildR);
            var resChildS = Set.GetValueBlocking(outChildS);
            var resChildTx = Set.GetValueBlocking(outChildTx);

            var defaultStream = AnimationStream.FromDefaultValues(m_Rig);

            defaultStream.GetLocalToRootTRS(0, out float3 testT, out quaternion testR, out float3 testS);
            Assert.That(resRootT, Is.EqualTo(testT).Using(TranslationComparer));
            Assert.That(resRootR, Is.EqualTo(testR).Using(RotationComparer));
            Assert.That(resRootS, Is.EqualTo(testS).Using(ScaleComparer));
            Assert.AreEqual(resRootTx, float4x4.TRS(testT, testR, testS));

            defaultStream.GetLocalToRootTRS(1, out testT, out testR, out testS);
            Assert.That(resChildT, Is.EqualTo(testT).Using(TranslationComparer));
            Assert.That(resChildR, Is.EqualTo(testR).Using(RotationComparer));
            Assert.That(resChildS, Is.EqualTo(testS).Using(ScaleComparer));
            Assert.AreEqual(resChildTx, float4x4.TRS(testT, testR, testS));

            Set.ReleaseGraphValue(outRootT);
            Set.ReleaseGraphValue(outRootR);
            Set.ReleaseGraphValue(outRootS);
            Set.ReleaseGraphValue(outRootTx);
            Set.ReleaseGraphValue(outChildT);
            Set.ReleaseGraphValue(outChildR);
            Set.ReleaseGraphValue(outChildS);
            Set.ReleaseGraphValue(outChildTx);
        }

        [Test]
        public void CanGetIntValuesUsingAnimationStreamNode()
        {
            var defaultValuesNode = CreateNode<DefaultValuesNode>();
            Set.SendMessage(defaultValuesNode, DefaultValuesNode.SimulationPorts.Rig, m_Rig);

            var intRootNode = CreateNode<GetAnimationStreamIntNode>();
            Set.SendMessage(intRootNode, GetAnimationStreamIntNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(intRootNode, GetAnimationStreamIntNode.KernelPorts.Index, 0);

            var intChildNode = CreateNode<GetAnimationStreamIntNode>();
            Set.SendMessage(intChildNode, GetAnimationStreamIntNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(intChildNode, GetAnimationStreamIntNode.KernelPorts.Index, 1);

            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, intRootNode, GetAnimationStreamIntNode.KernelPorts.Input);
            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, intChildNode, GetAnimationStreamIntNode.KernelPorts.Input);

            var outRootInt = Set.CreateGraphValue(intRootNode, GetAnimationStreamIntNode.KernelPorts.Output);
            var outChildInt = Set.CreateGraphValue(intChildNode, GetAnimationStreamIntNode.KernelPorts.Output);

            Set.Update(default);

            var resRootInt = Set.GetValueBlocking(outRootInt);
            var resChildInt = Set.GetValueBlocking(outChildInt);

            var defaultStream = AnimationStream.FromDefaultValues(m_Rig);

            Assert.AreEqual(resRootInt, defaultStream.GetInt(0));
            Assert.AreEqual(resChildInt, defaultStream.GetInt(1));

            Set.ReleaseGraphValue(outRootInt);
            Set.ReleaseGraphValue(outChildInt);
        }

        [Test]
        public void CanGetFloatValuesUsingAnimationStreamNode()
        {
            var defaultValuesNode = CreateNode<DefaultValuesNode>();
            Set.SendMessage(defaultValuesNode, DefaultValuesNode.SimulationPorts.Rig, m_Rig);

            var floatRootNode = CreateNode<GetAnimationStreamFloatNode>();
            Set.SendMessage(floatRootNode, GetAnimationStreamFloatNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(floatRootNode, GetAnimationStreamFloatNode.KernelPorts.Index, 0);

            var floatChildNode = CreateNode<GetAnimationStreamFloatNode>();
            Set.SendMessage(floatChildNode, GetAnimationStreamFloatNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(floatChildNode, GetAnimationStreamFloatNode.KernelPorts.Index, 1);

            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, floatRootNode, GetAnimationStreamFloatNode.KernelPorts.Input);
            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, floatChildNode, GetAnimationStreamFloatNode.KernelPorts.Input);

            var outRootFloat = Set.CreateGraphValue(floatRootNode, GetAnimationStreamFloatNode.KernelPorts.Output);
            var outChildFloat = Set.CreateGraphValue(floatChildNode, GetAnimationStreamFloatNode.KernelPorts.Output);

            Set.Update(default);

            var resRootFloat = Set.GetValueBlocking(outRootFloat);
            var resChildFloat = Set.GetValueBlocking(outChildFloat);

            var defaultStream = AnimationStream.FromDefaultValues(m_Rig);

            Assert.AreEqual(resRootFloat, defaultStream.GetFloat(0));
            Assert.AreEqual(resChildFloat, defaultStream.GetFloat(1));

            Set.ReleaseGraphValue(outRootFloat);
            Set.ReleaseGraphValue(outChildFloat);
        }

        [Test]
        public void CanSetLocalToParentValuesUsingAnimationStreamNode()
        {
            var defaultValuesNode = CreateNode<DefaultValuesNode>();
            Set.SendMessage(defaultValuesNode, DefaultValuesNode.SimulationPorts.Rig, m_Rig);

            var defaultStream = AnimationStream.FromDefaultValues(m_Rig);

            float3 newT = new float3(5f, 0f, 0f);
            quaternion newR = quaternion.RotateZ(math.radians(90f));
            float3 newS = new float3(5f, 6f, 5f);

            // Test T and R modes
            var localToParentRootNode = CreateNode<SetAnimationStreamLocalToParentNode>();
            Set.SendMessage(localToParentRootNode, SetAnimationStreamLocalToParentNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(localToParentRootNode, SetAnimationStreamLocalToParentNode.KernelPorts.Index, 0);
            Set.SetData(localToParentRootNode, SetAnimationStreamLocalToParentNode.KernelPorts.Mode, SetAnimationStreamLocalToParentNode.SetFromMode.Translation);
            Set.SetData(localToParentRootNode, SetAnimationStreamLocalToParentNode.KernelPorts.Translation, newT);
            Set.SetData(localToParentRootNode, SetAnimationStreamLocalToParentNode.KernelPorts.Rotation, newR);
            Set.SetData(localToParentRootNode, SetAnimationStreamLocalToParentNode.KernelPorts.Scale, newS);

            var localToParentChildNode = CreateNode<SetAnimationStreamLocalToParentNode>();
            Set.SendMessage(localToParentChildNode, SetAnimationStreamLocalToParentNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(localToParentChildNode, SetAnimationStreamLocalToParentNode.KernelPorts.Index, 1);
            Set.SetData(localToParentChildNode, SetAnimationStreamLocalToParentNode.KernelPorts.Mode, SetAnimationStreamLocalToParentNode.SetFromMode.Rotation);
            Set.SetData(localToParentChildNode, SetAnimationStreamLocalToParentNode.KernelPorts.Translation, newT);
            Set.SetData(localToParentChildNode, SetAnimationStreamLocalToParentNode.KernelPorts.Rotation, newR);
            Set.SetData(localToParentChildNode, SetAnimationStreamLocalToParentNode.KernelPorts.Scale, newS);

            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, localToParentRootNode, SetAnimationStreamLocalToParentNode.KernelPorts.Input);
            Set.Connect(localToParentRootNode, SetAnimationStreamLocalToParentNode.KernelPorts.Output, localToParentChildNode, SetAnimationStreamLocalToParentNode.KernelPorts.Input);

            var output = Set.CreateGraphValue(localToParentChildNode, SetAnimationStreamLocalToParentNode.KernelPorts.Output);

            var handle = Set.Update(default);
            var outputStream = AnimationStream.CreateReadOnly(m_Rig, DFGUtils.GetGraphValueTempNativeBuffer(Set, output));

            Assert.That(outputStream.GetLocalToParentTranslation(0), Is.EqualTo(newT).Using(TranslationComparer));
            Assert.That(outputStream.GetLocalToParentRotation(0), Is.EqualTo(defaultStream.GetLocalToParentRotation(0)).Using(RotationComparer));
            Assert.That(outputStream.GetLocalToParentScale(0), Is.EqualTo(defaultStream.GetLocalToParentScale(0)).Using(ScaleComparer));

            Assert.That(outputStream.GetLocalToParentTranslation(1), Is.EqualTo(defaultStream.GetLocalToParentTranslation(1)).Using(TranslationComparer));
            Assert.That(outputStream.GetLocalToParentRotation(1), Is.EqualTo(newR).Using(RotationComparer));
            Assert.That(outputStream.GetLocalToParentScale(1), Is.EqualTo(defaultStream.GetLocalToParentScale(1)).Using(ScaleComparer));

            // Test S and TR modes
            Set.SetData(localToParentRootNode, SetAnimationStreamLocalToParentNode.KernelPorts.Mode, SetAnimationStreamLocalToParentNode.SetFromMode.Scale);
            Set.SetData(localToParentChildNode, SetAnimationStreamLocalToParentNode.KernelPorts.Mode, SetAnimationStreamLocalToParentNode.SetFromMode.TranslationRotation);

            handle = Set.Update(handle);
            outputStream = AnimationStream.CreateReadOnly(m_Rig, DFGUtils.GetGraphValueTempNativeBuffer(Set, output));

            Assert.That(outputStream.GetLocalToParentTranslation(0), Is.EqualTo(defaultStream.GetLocalToParentTranslation(0)).Using(TranslationComparer));
            Assert.That(outputStream.GetLocalToParentRotation(0), Is.EqualTo(defaultStream.GetLocalToParentRotation(0)).Using(RotationComparer));
            Assert.That(outputStream.GetLocalToParentScale(0), Is.EqualTo(newS).Using(ScaleComparer));

            Assert.That(outputStream.GetLocalToParentTranslation(1), Is.EqualTo(newT).Using(TranslationComparer));
            Assert.That(outputStream.GetLocalToParentRotation(1), Is.EqualTo(newR).Using(RotationComparer));
            Assert.That(outputStream.GetLocalToParentScale(1), Is.EqualTo(defaultStream.GetLocalToParentScale(1)).Using(ScaleComparer));

            // Test TS and RS modes
            Set.SetData(localToParentRootNode, SetAnimationStreamLocalToParentNode.KernelPorts.Mode, SetAnimationStreamLocalToParentNode.SetFromMode.TranslationScale);
            Set.SetData(localToParentChildNode, SetAnimationStreamLocalToParentNode.KernelPorts.Mode, SetAnimationStreamLocalToParentNode.SetFromMode.RotationScale);

            handle = Set.Update(handle);
            outputStream = AnimationStream.CreateReadOnly(m_Rig, DFGUtils.GetGraphValueTempNativeBuffer(Set, output));

            Assert.That(outputStream.GetLocalToParentTranslation(0), Is.EqualTo(newT).Using(TranslationComparer));
            Assert.That(outputStream.GetLocalToParentRotation(0), Is.EqualTo(defaultStream.GetLocalToParentRotation(0)).Using(RotationComparer));
            Assert.That(outputStream.GetLocalToParentScale(0), Is.EqualTo(newS).Using(ScaleComparer));

            Assert.That(outputStream.GetLocalToParentTranslation(1), Is.EqualTo(defaultStream.GetLocalToParentTranslation(1)).Using(TranslationComparer));
            Assert.That(outputStream.GetLocalToParentRotation(1), Is.EqualTo(newR).Using(RotationComparer));
            Assert.That(outputStream.GetLocalToParentScale(1), Is.EqualTo(newS).Using(ScaleComparer));

            // Test TRS
            Set.SetData(localToParentRootNode, SetAnimationStreamLocalToParentNode.KernelPorts.Mode, SetAnimationStreamLocalToParentNode.SetFromMode.TranslationRotationScale);

            Set.Update(handle);
            outputStream = AnimationStream.CreateReadOnly(m_Rig, DFGUtils.GetGraphValueTempNativeBuffer(Set, output));

            Assert.That(outputStream.GetLocalToParentTranslation(0), Is.EqualTo(newT).Using(TranslationComparer));
            Assert.That(outputStream.GetLocalToParentRotation(0), Is.EqualTo(newR).Using(RotationComparer));
            Assert.That(outputStream.GetLocalToParentScale(0), Is.EqualTo(newS).Using(ScaleComparer));

            Set.ReleaseGraphValue(output);
        }

        [Test]
        public void CanSetLocalToRootValuesUsingAnimationStreamNode()
        {
            var defaultValuesNode = CreateNode<DefaultValuesNode>();
            Set.SendMessage(defaultValuesNode, DefaultValuesNode.SimulationPorts.Rig, m_Rig);

            var defaultStream = AnimationStream.FromDefaultValues(m_Rig);

            float3 newT = new float3(5f, 0f, 0f);
            quaternion newR = quaternion.RotateZ(math.radians(90f));
            float3 newS = new float3(5f, 6f, 5f);

            // Test T, R, S modes
            var localToRootRootNode = CreateNode<SetAnimationStreamLocalToRootNode>();
            Set.SendMessage(localToRootRootNode, SetAnimationStreamLocalToRootNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(localToRootRootNode, SetAnimationStreamLocalToRootNode.KernelPorts.Index, 0);
            Set.SetData(localToRootRootNode, SetAnimationStreamLocalToRootNode.KernelPorts.Mode, SetAnimationStreamLocalToRootNode.SetFromMode.Translation);
            Set.SetData(localToRootRootNode, SetAnimationStreamLocalToRootNode.KernelPorts.Translation, newT);
            Set.SetData(localToRootRootNode, SetAnimationStreamLocalToRootNode.KernelPorts.Rotation, newR);
            Set.SetData(localToRootRootNode, SetAnimationStreamLocalToRootNode.KernelPorts.Scale, newS);

            var localToRootChildNode = CreateNode<SetAnimationStreamLocalToRootNode>();
            Set.SendMessage(localToRootChildNode, SetAnimationStreamLocalToRootNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(localToRootChildNode, SetAnimationStreamLocalToRootNode.KernelPorts.Index, 1);
            Set.SetData(localToRootChildNode, SetAnimationStreamLocalToRootNode.KernelPorts.Mode, SetAnimationStreamLocalToRootNode.SetFromMode.Rotation | SetAnimationStreamLocalToRootNode.SetFromMode.Scale);
            Set.SetData(localToRootChildNode, SetAnimationStreamLocalToRootNode.KernelPorts.Translation, newT);
            Set.SetData(localToRootChildNode, SetAnimationStreamLocalToRootNode.KernelPorts.Rotation, newR);
            Set.SetData(localToRootChildNode, SetAnimationStreamLocalToRootNode.KernelPorts.Scale, newS);

            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, localToRootRootNode, SetAnimationStreamLocalToRootNode.KernelPorts.Input);
            Set.Connect(localToRootRootNode, SetAnimationStreamLocalToRootNode.KernelPorts.Output, localToRootChildNode, SetAnimationStreamLocalToRootNode.KernelPorts.Input);

            var output = Set.CreateGraphValue(localToRootChildNode, SetAnimationStreamLocalToRootNode.KernelPorts.Output);

            var handle = Set.Update(default);
            var outputStream = AnimationStream.CreateReadOnly(m_Rig, DFGUtils.GetGraphValueTempNativeBuffer(Set, output));

            Assert.That(outputStream.GetLocalToRootTranslation(0), Is.EqualTo(newT).Using(TranslationComparer));
            Assert.That(outputStream.GetLocalToRootRotation(0), Is.EqualTo(defaultStream.GetLocalToRootRotation(0)).Using(RotationComparer));
            Assert.That(outputStream.GetLocalToRootScale(0), Is.EqualTo(defaultStream.GetLocalToRootScale(0)).Using(ScaleComparer));

            Assert.That(outputStream.GetLocalToRootTranslation(1), Is.EqualTo(mathex.mul(outputStream.GetLocalToRootMatrix(0), defaultStream.GetLocalToParentTranslation(1))).Using(TranslationComparer));
            Assert.That(outputStream.GetLocalToRootRotation(1), Is.EqualTo(newR).Using(RotationComparer));
            Assert.That(outputStream.GetLocalToRootScale(1), Is.EqualTo(newS).Using(ScaleComparer));

            // Test TR mode
            Set.SetData(localToRootChildNode, SetAnimationStreamLocalToRootNode.KernelPorts.Mode, SetAnimationStreamLocalToRootNode.SetFromMode.TranslationRotation);

            handle = Set.Update(handle);
            outputStream = AnimationStream.CreateReadOnly(m_Rig, DFGUtils.GetGraphValueTempNativeBuffer(Set, output));

            Assert.That(outputStream.GetLocalToRootTranslation(1), Is.EqualTo(newT).Using(TranslationComparer));
            Assert.That(outputStream.GetLocalToRootRotation(1), Is.EqualTo(newR).Using(RotationComparer));

            // Test TRS mode
            Set.SetData(localToRootChildNode, SetAnimationStreamLocalToRootNode.KernelPorts.Mode, SetAnimationStreamLocalToRootNode.SetFromMode.TranslationRotationScale);

            Set.Update(handle);
            outputStream = AnimationStream.CreateReadOnly(m_Rig, DFGUtils.GetGraphValueTempNativeBuffer(Set, output));

            Assert.That(outputStream.GetLocalToRootTranslation(1), Is.EqualTo(newT).Using(TranslationComparer));
            Assert.That(outputStream.GetLocalToRootRotation(1), Is.EqualTo(newR).Using(RotationComparer));
            Assert.That(outputStream.GetLocalToRootScale(1), Is.EqualTo(newS).Using(ScaleComparer));

            Set.ReleaseGraphValue(output);
        }

        [Test]
        public void CanSetIntValuesUsingAnimationStreamNode()
        {
            var defaultValuesNode = CreateNode<DefaultValuesNode>();
            Set.SendMessage(defaultValuesNode, DefaultValuesNode.SimulationPorts.Rig, m_Rig);

            const int kNewRootInt = 4000;
            const int kNewChildInt = 3000;

            var intRootNode = CreateNode<SetAnimationStreamIntNode>();
            Set.SendMessage(intRootNode, SetAnimationStreamIntNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(intRootNode, SetAnimationStreamIntNode.KernelPorts.Index, 0);
            Set.SetData(intRootNode, SetAnimationStreamIntNode.KernelPorts.Value, kNewRootInt);

            var intChildNode = CreateNode<SetAnimationStreamIntNode>();
            Set.SendMessage(intChildNode, SetAnimationStreamIntNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(intChildNode, SetAnimationStreamIntNode.KernelPorts.Index, 1);
            Set.SetData(intChildNode, SetAnimationStreamIntNode.KernelPorts.Value, kNewChildInt);

            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, intRootNode, SetAnimationStreamIntNode.KernelPorts.Input);
            Set.Connect(intRootNode, SetAnimationStreamIntNode.KernelPorts.Output, intChildNode, SetAnimationStreamIntNode.KernelPorts.Input);

            var output = Set.CreateGraphValue(intChildNode, SetAnimationStreamIntNode.KernelPorts.Output);

            Set.Update(default);

            var defaultStream = AnimationStream.FromDefaultValues(m_Rig);

            var outputStream = AnimationStream.CreateReadOnly(m_Rig, DFGUtils.GetGraphValueTempNativeBuffer(Set, output));

            Assert.AreNotEqual(outputStream.GetInt(0), defaultStream.GetInt(0));
            Assert.AreNotEqual(outputStream.GetInt(1), defaultStream.GetInt(1));
            Assert.AreEqual(outputStream.GetInt(0), kNewRootInt);
            Assert.AreEqual(outputStream.GetInt(1), kNewChildInt);

            Set.ReleaseGraphValue(output);
        }

        [Test]
        public void CanSetFloatValuesUsingAnimationStreamNode()
        {
            var defaultValuesNode = CreateNode<DefaultValuesNode>();
            Set.SendMessage(defaultValuesNode, DefaultValuesNode.SimulationPorts.Rig, m_Rig);

            const float kNewRootFloat = 70f;
            const float kNewChildFloat = 60f;

            var floatRootNode = CreateNode<SetAnimationStreamFloatNode>();
            Set.SendMessage(floatRootNode, SetAnimationStreamFloatNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(floatRootNode, SetAnimationStreamFloatNode.KernelPorts.Index, 0);
            Set.SetData(floatRootNode, SetAnimationStreamFloatNode.KernelPorts.Value, kNewRootFloat);

            var floatChildNode = CreateNode<SetAnimationStreamFloatNode>();
            Set.SendMessage(floatChildNode, SetAnimationStreamFloatNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(floatChildNode, SetAnimationStreamFloatNode.KernelPorts.Index, 1);
            Set.SetData(floatChildNode, SetAnimationStreamFloatNode.KernelPorts.Value, kNewChildFloat);

            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, floatRootNode, SetAnimationStreamFloatNode.KernelPorts.Input);
            Set.Connect(floatRootNode, SetAnimationStreamFloatNode.KernelPorts.Output, floatChildNode, SetAnimationStreamFloatNode.KernelPorts.Input);

            var output = Set.CreateGraphValue(floatChildNode, SetAnimationStreamFloatNode.KernelPorts.Output);

            Set.Update(default);

            var defaultStream = AnimationStream.FromDefaultValues(m_Rig);

            var outputStream = AnimationStream.CreateReadOnly(m_Rig, DFGUtils.GetGraphValueTempNativeBuffer(Set, output));

            Assert.AreNotEqual(outputStream.GetFloat(0), defaultStream.GetFloat(0));
            Assert.AreNotEqual(outputStream.GetFloat(1), defaultStream.GetFloat(1));
            Assert.AreEqual(outputStream.GetFloat(0), kNewRootFloat);
            Assert.AreEqual(outputStream.GetFloat(1), kNewChildFloat);

            Set.ReleaseGraphValue(output);
        }

        internal class TestNode
            : SimulationKernelNodeDefinition<TestNode.SimPorts, TestNode.KernelDefs>
            , IRigContextHandler<TestNode.Data>
        {
#pragma warning disable 0649
            public struct SimPorts : ISimulationPortDefinition
            {
                public MessageInput<TestNode, Rig> Rig;
            }

            public struct KernelDefs : IKernelPortDefinition
            {
                public DataOutput<TestNode, Buffer<AnimatedData>> Output;
            }
#pragma warning restore 0649

            struct Data : INodeData, IMsgHandler<Rig>
            {
                public void HandleMessage(in MessageContext ctx, in Rig rig)
                {
                    ctx.UpdateKernelData(new KernelData
                    {
                        RigDefinition = rig
                    });

                    ctx.Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0));
                }
            }

            struct KernelData : IKernelData
            {
                public BlobAssetReference<RigDefinition> RigDefinition;
            }

            struct Kernel : IGraphKernel<KernelData, KernelDefs>
            {
                public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
                {
                    // Test to validate that output is initialized with 0 if nobody write to the buffer
                }
            }

            InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
                (InputPortID)SimulationPorts.Rig;
        }

        [Test]
        public void NodeOutputIsInitializedToZero()
        {
            var node = CreateNode<TestNode>();
            Set.SendMessage(node, TestNode.SimulationPorts.Rig, m_Rig);

            var output = CreateGraphValue(node, TestNode.KernelPorts.Output);

            Set.Update(default);

            var outputStream = AnimationStream.CreateReadOnly(m_Rig, DFGUtils.GetGraphValueTempNativeBuffer(Set, output));

            Assert.That(outputStream.PassMask.HasNone(), Is.True);
            Assert.That(outputStream.FrameMask.HasNone(), Is.True);
        }
    }
}
