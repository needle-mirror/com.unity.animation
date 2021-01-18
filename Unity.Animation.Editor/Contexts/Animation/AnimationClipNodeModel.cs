using System;
using Unity.DataFlowGraph;
using Unity.Entities;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using Unity.Animation.Editor;

namespace Unity.Animation.Model
{
    [Serializable]
    [SearcherItem(typeof(AnimationGraphStencil), SearcherContext.Graph, "Animation Clip")]
    [ContextSearch(typeof(AnimationGraphStencil), ContextAvailability.StandAloneGraph, "Animation Clip")]
    [ContextSearch(typeof(AnimationGraphStencil), ContextAvailability.StateMachineGraph, "Animation Clip")]
    internal class AnimationClipNodeModel : BaseNodeModel
    {
        [SerializeField]
        internal bool OverrideLoop = false;
        [SerializeField]
        internal bool Loop = true;

        // [SerializeField]
        // public bool OverrideRootMotion { get; set; }

        internal const string k_PosePortName = "Pose";
        internal const string k_ClipPortName = "Clip";
        internal const string k_RootMotionPortName = "Root Motion Source";

        public override string NodeName => "Animation Clip";

        internal void SetClip(AnimationClip clip)
        {
            InputConstantsById[k_ClipPortName].ObjectValue = clip;
        }

        AnimationClipIRBuilder m_Builder;
        public override INodeIRBuilder Builder
        { get { if (m_Builder == null) m_Builder = new AnimationClipIRBuilder(this); return m_Builder; } }

        class AnimationClipIRBuilder : NodeIRBuilder
        {
            AnimationClipNodeModel ClipNodeModel => Model as AnimationClipNodeModel;
            IRNodeDefinition m_InstanceRef;

            public AnimationClipIRBuilder(AnimationClipNodeModel model)
                : base(model)
            {}

            public override void Build(IR ir, IBuildContext context)
            {
                if (context is StateMachineBuildContext)
                {
                    ir.CompilationResult.AddError($"Animation clip node cannot be used in a State Machine context");
                    return;
                }

                if (context is StandAloneGraphBuildContext standAloneGraphTargetContext)
                {
                    m_InstanceRef = ir.CreateNodeFromModel(ClipNodeModel.Guid, ClipNodeModel.NodeName, typeof(SimpleClipNode));
                    IRBuilder.BuildNodePorts(ir, ClipNodeModel, m_InstanceRef);

                    IRBuilder.BuildPortDefaultValues(ClipNodeModel, m_InstanceRef, ir, context);

                    var dtNode = ir.CreateNode("DeltatimeNode", typeof(GetDeltaTimeNode));
                    IRBuilder.BuildNodePorts(ir, ClipNodeModel, dtNode);

                    bool loop = ClipNodeModel.OverrideLoop ? ClipNodeModel.Loop : standAloneGraphTargetContext.ShouldLoop;
                    ir.AddDefaultValue(new IRPortTarget(m_InstanceRef, nameof(SimpleClipNode.MessagePorts.Timescale)), 1.0f, true);
                    ir.AddDefaultValue(new IRPortTarget(m_InstanceRef, nameof(SimpleClipNode.MessagePorts.Loop)), loop, true);

                    ir.ConnectSimulation(
                        new IRPortTarget(dtNode, nameof(GetDeltaTimeNode.SimulationPorts.Output)),
                        new IRPortTarget(m_InstanceRef, nameof(SimpleClipNode.MessagePorts.DeltaTime)));
                }
                else
                {
                    var graphContext = (StateMachineGraphBuildContext)context;

                    m_InstanceRef = ir.CreateNodeFromModel(ClipNodeModel.Guid, ClipNodeModel.NodeName, typeof(SimpleClipNode));
                    IRBuilder.BuildNodePorts(ir, ClipNodeModel, m_InstanceRef);

                    IRBuilder.BuildPortDefaultValues(ClipNodeModel, m_InstanceRef, ir, context);

                    var getTimeControlNode = ir.CreateNode("TimeControl", typeof(GetTimeControl));
                    IRBuilder.BuildNodePorts(ir, ClipNodeModel, getTimeControlNode);

                    bool loop = ClipNodeModel.OverrideLoop ? ClipNodeModel.Loop : graphContext.ShouldLoop;
                    ir.AddDefaultValue(new IRPortTarget(m_InstanceRef, nameof(SimpleClipNode.MessagePorts.Timescale)), 1.0f, true);
                    ir.AddDefaultValue(new IRPortTarget(m_InstanceRef, nameof(SimpleClipNode.MessagePorts.Loop)), loop, true);

                    ir.ConnectSimulation(
                        new IRPortTarget(getTimeControlNode, nameof(GetTimeControl.SimulationPorts.Output)),
                        new IRPortTarget(m_InstanceRef, nameof(SimpleClipNode.MessagePorts.DeltaTime)));
                }
            }

            public override IRPortTarget GetSourcePortTarget(BasePortModel port, IR ir, IBuildContext context)
            {
                if (port.Title == k_PosePortName)
                    return new IRPortTarget(m_InstanceRef, nameof(SimpleClipNode.DataPorts.Output));

                return base.GetSourcePortTarget(port, ir, context);
            }

            public override IRPortTarget GetDestinationPortTarget(BasePortModel port, IR ir, IBuildContext context)
            {
                if (port.Title == k_ClipPortName)
                    return new IRPortTarget(m_InstanceRef, nameof(SimpleClipNode.MessagePorts.Clip));
                else if (port.Title == k_RootMotionPortName)
                    return new IRPortTarget(m_InstanceRef, nameof(SimpleClipNode.MessagePorts.RootTransform));

                return base.GetDestinationPortTarget(port, ir, context);
            }
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            AddInputPort(
                new NodePortCreation()
                {
                    PortType = PortType.Data,
                    EvalType = BasePortModel.PortEvaluationType.Simulation,
                    DataType = typeof(BlobAssetReference<Clip>).GenerateTypeHandle(),
                    Name = k_ClipPortName,
                    DisplayName = "Animation Clip",
                    PortDescription = "Animation Clip",
                    IsStatic = true
                }, false);

            AddInputPort(
                new NodePortCreation()
                {
                    PortType = PortType.MissingPort,
                    EvalType = BasePortModel.PortEvaluationType.Simulation,
                    DataType = typeof(MotionID).GenerateTypeHandle(),
                    Name = k_RootMotionPortName,
                    DisplayName = "Root Motion Source",
                    PortDescription = "Root Motion Source",
                    IsStatic = true
                }, false);

            AddOutputPort(
                new NodePortCreation()
                {
                    PortType = PortType.Execution,
                    EvalType = BasePortModel.PortEvaluationType.Rendering,
                    DataType = typeof(Buffer<AnimatedData>).GenerateTypeHandle(),
                    Name = k_PosePortName,
                    DisplayName = "Pose",
                    PortDescription = "Pose",
                }, false);
        }
    }
}
