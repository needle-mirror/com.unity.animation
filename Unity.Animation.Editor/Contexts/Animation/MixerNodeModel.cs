using System;
using Unity.DataFlowGraph;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Editor;

namespace Unity.Animation.Model
{
    [Serializable]
    [SearcherItem(typeof(AnimationGraphStencil), SearcherContext.Graph, "Mixer")]
    [ContextSearch(typeof(AnimationGraphStencil), ContextAvailability.StandAloneGraph, "Mixer")]
    internal class MixerNodeModel : BaseNodeModel
    {
        internal const string k_PosePortName = "Pose";
        internal const string k_Input1PortName = "Input 1";
        internal const string k_Input2PortName = "Input 2";
        internal const string k_WeightPortName = "Weight";

        public override string NodeName => "Mixer";

        MixerIRBuilder m_Builder;
        public override INodeIRBuilder Builder
        { get { if (m_Builder == null) m_Builder = new MixerIRBuilder(this); return m_Builder; } }

        class MixerIRBuilder : NodeIRBuilder
        {
            MixerNodeModel MixerModel => Model as MixerNodeModel;
            IRNodeDefinition m_InstanceRef;

            public MixerIRBuilder(MixerNodeModel model)
                : base(model)
            {}

            public override void Build(IR ir, IBuildContext context)
            {
                if (context is StateMachineBuildContext)
                {
                    ir.CompilationResult.AddError($"Mixer node cannot be used in a State Machine context");
                    return;
                }

                m_InstanceRef = ir.CreateNodeFromModel(MixerModel.Guid, MixerModel.NodeName, typeof(MixerNode));
                IRBuilder.BuildNodePorts(ir, MixerModel, m_InstanceRef);
                IRBuilder.BuildPortDefaultValues(MixerModel, m_InstanceRef, ir, context);
            }

            public override IRPortTarget GetSourcePortTarget(BasePortModel port, IR ir, IBuildContext context)
            {
                if (port.Title == k_PosePortName)
                    return new IRPortTarget(m_InstanceRef, nameof(MixerNode.KernelDefs.Output));

                return base.GetSourcePortTarget(port, ir, context);
            }

            public override IRPortTarget GetDestinationPortTarget(BasePortModel port, IR ir, IBuildContext context)
            {
                if (port.Title == k_Input1PortName)
                    return new IRPortTarget(m_InstanceRef, nameof(MixerNode.KernelDefs.Input0));
                else if (port.Title == k_Input2PortName)
                    return new IRPortTarget(m_InstanceRef, nameof(MixerNode.KernelDefs.Input1));
                else if (port.Title == k_WeightPortName)
                    return new IRPortTarget(m_InstanceRef, nameof(MixerNode.KernelDefs.Weight));

                return base.GetDestinationPortTarget(port, ir, context);
            }
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            AddInputPort(
                new NodePortCreation()
                {
                    PortType = PortType.Execution,
                    EvalType = BasePortModel.PortEvaluationType.Rendering,
                    DataType = typeof(Buffer<AnimatedData>).GenerateTypeHandle(),
                    Name = k_Input1PortName,
                    DisplayName = k_Input1PortName,
                    PortDescription = k_Input1PortName,
                }, false);

            AddInputPort(
                new NodePortCreation()
                {
                    PortType = PortType.Execution,
                    EvalType = BasePortModel.PortEvaluationType.Rendering,
                    DataType = typeof(Buffer<AnimatedData>).GenerateTypeHandle(),
                    Name = k_Input2PortName,
                    DisplayName = k_Input2PortName,
                    PortDescription = k_Input2PortName,
                }, false);

            AddInputPort(
                new NodePortCreation()
                {
                    PortType = PortType.Data,
                    EvalType = BasePortModel.PortEvaluationType.Rendering,
                    DataType = typeof(float).GenerateTypeHandle(),
                    Name = k_WeightPortName,
                    DisplayName = k_WeightPortName,
                    PortDescription = k_WeightPortName,
                }, false);

            AddOutputPort(
                new NodePortCreation()
                {
                    PortType = PortType.Execution,
                    EvalType = BasePortModel.PortEvaluationType.Rendering,
                    DataType = typeof(Buffer<AnimatedData>).GenerateTypeHandle(),
                    Name = k_PosePortName,
                    DisplayName = k_PosePortName,
                    PortDescription = k_PosePortName,
                }, false);
        }
    }
}
