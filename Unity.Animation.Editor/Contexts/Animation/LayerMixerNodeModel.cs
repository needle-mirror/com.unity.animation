using System;
using Unity.DataFlowGraph;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Editor;

namespace Unity.Animation.Model
{
    [Serializable]
    [SearcherItem(typeof(AnimationGraphStencil), SearcherContext.Graph, "High-Level Nodes / Mixers / Layer Mixer")]
    internal class LayerMixerNodeModel : BaseNodeModel
    {
        class LayerMixerIRBuilder : NodeIRBuilder
        {
            LayerMixerNodeModel MixerModel => Model as LayerMixerNodeModel;

            internal string m_InstanceNodeName;

            public LayerMixerIRBuilder(LayerMixerNodeModel model)
                : base(model)
            {
            }

            public override void Build(IR ir, IBuildContext context)
            {
                var mixerNode = ir.CreateNodeFromModel(Model.Guid, Model.NodeName, typeof(LayerMixerNode));
                m_InstanceNodeName = mixerNode.Name;
                IRBuilder.BuildNodePorts(ir, Model, mixerNode);

                IRBuilder.BuildPortDefaultValues(Model, mixerNode, ir, context);
            }

            public override IRPortTarget GetSourcePortTarget(BasePortModel port, IR ir, IBuildContext context)
            {
                if (port.Title == "Output")
                    return new IRPortTarget(ir.GetNodeFromName(m_InstanceNodeName), "Output");

                return base.GetSourcePortTarget(port, ir, context);
            }

            public override IRPortTarget GetDestinationPortTarget(BasePortModel port, IR ir, IBuildContext context)
            {
                if (port.Title == "Layer Count")
                    return new IRPortTarget(ir.GetNodeFromName(m_InstanceNodeName), "LayerCount");
                else if (port.OriginalScriptName == "Inputs" ||
                         port.OriginalScriptName == "Weights" ||
                         port.OriginalScriptName == "BlendingModes")
                    return new IRPortTarget(ir.GetNodeFromName(m_InstanceNodeName), port.OriginalScriptName, port.PortGroupInstance);
                return base.GetDestinationPortTarget(port, ir, context);
            }
        }

        public override string NodeName => "Layer Mixer";

        LayerMixerIRBuilder m_Builder;
        public override INodeIRBuilder Builder
        { get { if (m_Builder == null) m_Builder = new LayerMixerIRBuilder(this); return m_Builder; } }


        void InitPortGroups()
        {
            var layerGroup = PortGroupDefinitions.GetOrCreateGroupInstance(1);
            layerGroup.MinInstance = 2;
            layerGroup.MaxInstance = -1;
            layerGroup.SimulationPortToDrive = "LayerCount";
            layerGroup.PortGroupSizeDescription = "Number of Layers";
            layerGroup.IsDefaultGroup = false;

            layerGroup.DataInputs.Add(new PortDefinition()
            {
                Type = typeof(Buffer<AnimatedData>),
                FieldName = "Inputs",
                DisplayName = "Input",
            });
            layerGroup.DataInputs.Add(new PortDefinition()
            {
                Type = typeof(float),
                FieldName = "Weights",
                DisplayName = "Weight",
            });
            layerGroup.DataInputs.Add(new PortDefinition()
            {
                Type = typeof(BlendingMode),
                FieldName = "BlendingModes",
                DisplayName = "Blending Mode",
                IsStatic = true,
            });
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            InitPortGroups();
            AddPortGroup(1);

            AddOutputPort(
                new NodePortCreation()
                {
                    PortType = PortType.Execution,
                    EvalType = BasePortModel.PortEvaluationType.Rendering,
                    DataType = typeof(Buffer<AnimatedData>).GenerateTypeHandle(),
                    Name = "Output",
                    DisplayName = "Output",
                    PortDescription = "Output"
                }, false);
        }
    }
}
