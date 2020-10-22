using NUnit.Framework;
using Unity.DataFlowGraph;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Animation.Tests
{
    // These tests only validate that the output of a constraint graph node matches the result computed from the
    // direct solver calls.
    // Constraint solver validation tests are done in Unity.Animation.Tests/ConstraintTests.cs

    // TODO : Add graph node tests for Postion, Rotation, Parent, Aim and TwoBoneIK.

    public class ConstraintNodeTests : AnimationTestsFixture
    {
        const int k_Twist0Index = 1;
        const int k_Twist1Index = 2;
        const int k_HandIndex   = 3;

        private Rig m_Rig;

        Rig CreateTestRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode { ParentIndex = -1, Id = "Root", AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = mathex.one() },
                new SkeletonNode { ParentIndex = 0, Id = "UpperArm", AxisIndex = -1, LocalTranslationDefaultValue = new float3(1f, 0f, 0f), LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = mathex.one()},
                new SkeletonNode { ParentIndex = 1, Id = "LowerArm", AxisIndex = -1, LocalTranslationDefaultValue = new float3(1f, 0f, 0f), LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = mathex.one()},
                new SkeletonNode { ParentIndex = 2, Id = "Hand", AxisIndex = -1, LocalTranslationDefaultValue = new float3(1f, 0.02f, 0f), LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = mathex.one()},
            };

            return new Rig { Value = RigBuilder.CreateRigDefinition(skeletonNodes) };
        }

        void UpdateTwistCorrectionNodeVariableInputs(NodeSet set, NodeHandle<TwistCorrectionNode> node, in Core.TwistCorrectionData data)
        {
            for (int i = 0; i < data.TwistIndices.Length; ++i)
                Set.SetData(node, TwistCorrectionNode.KernelPorts.TwistIndices, i, data.TwistIndices[i]);
            for (int i = 0; i < data.TwistWeights.Length; ++i)
                Set.SetData(node, TwistCorrectionNode.KernelPorts.TwistWeights, i, data.TwistWeights[i]);
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            m_Rig = CreateTestRigDefinition();
        }

        [Test]
        public void TwistCorrectionNodeComputesExpectedResult()
        {
            var buffer = new NativeArray<AnimatedData>(m_Rig.Value.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var debugStream = AnimationStream.Create(m_Rig, buffer);
            debugStream.ResetToDefaultValues();

            var defaultTwistSourceRot = debugStream.GetLocalToParentRotation(k_HandIndex);

            var twistCorrectionData = Core.TwistCorrectionData.Default();
            twistCorrectionData.SourceRotation = defaultTwistSourceRot;
            twistCorrectionData.SourceInverseDefaultRotation = math.conjugate(twistCorrectionData.SourceRotation);
            twistCorrectionData.LocalTwistAxis = math.float3(1f, 0f, 0f);
            twistCorrectionData.TwistIndices = new NativeArray<int>(new int[] { k_Twist0Index, k_Twist1Index }, Allocator.Temp);
            twistCorrectionData.TwistWeights = new NativeArray<float>(twistCorrectionData.TwistIndices.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);

            // Create node setup
            var defaultValuesNode = CreateNode<DefaultValuesNode>();
            Set.SendMessage(defaultValuesNode, DefaultValuesNode.SimulationPorts.Rig, m_Rig);

            var setAnimationStreamL2PNode = CreateNode<SetAnimationStreamLocalToParentNode>();
            Set.SendMessage(setAnimationStreamL2PNode, SetAnimationStreamLocalToParentNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(setAnimationStreamL2PNode, SetAnimationStreamLocalToParentNode.KernelPorts.Mode, SetAnimationStreamLocalToParentNode.SetFromMode.Rotation);
            Set.SetData(setAnimationStreamL2PNode, SetAnimationStreamLocalToParentNode.KernelPorts.Index, k_HandIndex);

            var getAnimationStreamL2PNode = CreateNode<GetAnimationStreamLocalToParentNode>();
            Set.SendMessage(getAnimationStreamL2PNode, GetAnimationStreamLocalToParentNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(getAnimationStreamL2PNode, GetAnimationStreamLocalToParentNode.KernelPorts.Index, k_HandIndex);

            var twistCorrectionNode = CreateNode<TwistCorrectionNode>();
            Set.SendMessage(twistCorrectionNode, TwistCorrectionNode.SimulationPorts.Rig, m_Rig);
            Set.SetData(twistCorrectionNode, TwistCorrectionNode.KernelPorts.LocalTwistAxis, TwistCorrectionNode.TwistAxis.X);
            Set.SetData(twistCorrectionNode, TwistCorrectionNode.KernelPorts.SourceDefaultRotation, defaultTwistSourceRot);
            Set.SetPortArraySize(twistCorrectionNode, TwistCorrectionNode.KernelPorts.TwistIndices, 2);
            Set.SetPortArraySize(twistCorrectionNode, TwistCorrectionNode.KernelPorts.TwistWeights, 2);

            Set.Connect(defaultValuesNode, DefaultValuesNode.KernelPorts.Output, setAnimationStreamL2PNode, SetAnimationStreamLocalToParentNode.KernelPorts.Input);
            Set.Connect(setAnimationStreamL2PNode, SetAnimationStreamLocalToParentNode.KernelPorts.Output, getAnimationStreamL2PNode, GetAnimationStreamLocalToParentNode.KernelPorts.Input);
            Set.Connect(getAnimationStreamL2PNode, GetAnimationStreamLocalToParentNode.KernelPorts.Rotation, twistCorrectionNode, TwistCorrectionNode.KernelPorts.SourceRotation);
            Set.Connect(setAnimationStreamL2PNode, SetAnimationStreamLocalToParentNode.KernelPorts.Output, twistCorrectionNode, TwistCorrectionNode.KernelPorts.Input);
            var nodeOuput = Set.CreateGraphValue(twistCorrectionNode, TwistCorrectionNode.KernelPorts.Output);

            // Apply rotation to source, twist constraint w0 = 0f, w1 = 0f
            var newRotation = math.mul(twistCorrectionData.SourceRotation, quaternion.AxisAngle(math.float3(1f, 0f, 0f), math.radians(90)));
            twistCorrectionData.SourceRotation = newRotation;
            Set.SetData(setAnimationStreamL2PNode, SetAnimationStreamLocalToParentNode.KernelPorts.Rotation, newRotation);
            UpdateTwistCorrectionNodeVariableInputs(Set, twistCorrectionNode, twistCorrectionData);

            Core.SolveTwistCorrection(ref debugStream, twistCorrectionData, 1f);
            var handle = Set.Update(default);
            var outputStream = AnimationStream.CreateReadOnly(m_Rig, DFGUtils.GetGraphValueTempNativeBuffer(Set, nodeOuput));
            Assert.That(outputStream.GetLocalToParentRotation(k_Twist0Index), Is.EqualTo(debugStream.GetLocalToParentRotation(k_Twist0Index)).Using(RotationComparer));
            Assert.That(outputStream.GetLocalToParentRotation(k_Twist1Index), Is.EqualTo(debugStream.GetLocalToParentRotation(k_Twist1Index)).Using(RotationComparer));

            // twist_w0 = 1, twist_w1 = 1
            twistCorrectionData.TwistWeights[0] = 1f;
            twistCorrectionData.TwistWeights[1] = 1f;
            UpdateTwistCorrectionNodeVariableInputs(Set, twistCorrectionNode, twistCorrectionData);

            Core.SolveTwistCorrection(ref debugStream, twistCorrectionData, 1f);
            handle = Set.Update(handle);
            outputStream = AnimationStream.CreateReadOnly(m_Rig, DFGUtils.GetGraphValueTempNativeBuffer(Set, nodeOuput));
            Assert.That(outputStream.GetLocalToParentRotation(k_Twist0Index), Is.EqualTo(debugStream.GetLocalToParentRotation(k_Twist0Index)).Using(RotationComparer));
            Assert.That(outputStream.GetLocalToParentRotation(k_Twist1Index), Is.EqualTo(debugStream.GetLocalToParentRotation(k_Twist1Index)).Using(RotationComparer));

            // twistNode0.w = -1f, twistNode1.w = -1f [twist nodes should be inverse to source]
            twistCorrectionData.TwistWeights[0] = -1f;
            twistCorrectionData.TwistWeights[1] = -1f;
            UpdateTwistCorrectionNodeVariableInputs(Set, twistCorrectionNode, twistCorrectionData);

            Core.SolveTwistCorrection(ref debugStream, twistCorrectionData, 1f);
            handle = Set.Update(handle);
            outputStream = AnimationStream.CreateReadOnly(m_Rig, DFGUtils.GetGraphValueTempNativeBuffer(Set, nodeOuput));
            Assert.That(outputStream.GetLocalToParentRotation(k_Twist0Index), Is.EqualTo(debugStream.GetLocalToParentRotation(k_Twist0Index)).Using(RotationComparer));
            Assert.That(outputStream.GetLocalToParentRotation(k_Twist1Index), Is.EqualTo(debugStream.GetLocalToParentRotation(k_Twist1Index)).Using(RotationComparer));

            handle.Complete();
            Set.ReleaseGraphValue(nodeOuput);
        }
    }
}
