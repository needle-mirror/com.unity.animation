using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class InOutPortContainerPart : UnityEditor.GraphToolsFoundation.Overdrive.InOutPortContainerPart
    {
        protected InOutPortContainerPart(string name, IGraphElementModel model, IGraphElement ownerElement,
                                                   string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
        }

        public new static InOutPortContainerPart Create(string name, IGraphElementModel model, IGraphElement graphElement, string parentClassName)
        {
            if (model is IPortNode)
            {
                return new InOutPortContainerPart(name, model, graphElement, parentClassName);
            }

            return null;
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is IInOutPortsNode portHolder)
            {
                var listVisibleInputPorts = portHolder.GetInputPorts()
                    .Where(x => !(x as BasePortModel).IsHidden)
                    .Select(x => x).ToList();
                var listVisibleOutputPorts = portHolder.GetOutputPorts()
                    .Where(x => !(x as BasePortModel).IsHidden)
                    .Select(x => x).ToList();

                m_InputPortContainer?.UpdatePorts(listVisibleInputPorts, m_OwnerElement.GraphView,
                    m_OwnerElement.Store);
                m_OutputPortContainer?.UpdatePorts(listVisibleOutputPorts, m_OwnerElement.GraphView,
                    m_OwnerElement.Store);
            }
            else if (m_Model is ISingleInputPortNode inputPortHolder)
            {
                var inputPort = (inputPortHolder.InputPort as BasePortModel).IsHidden
                    ? new IPortModel[] {}
                : new[] {inputPortHolder.InputPort};

                m_InputPortContainer?.UpdatePorts(inputPort, m_OwnerElement.GraphView,
                    m_OwnerElement.Store);
            }
            else if (m_Model is ISingleOutputPortNode outputPortHolder)
            {
                var outputPort = (outputPortHolder.OutputPort as BasePortModel).IsHidden
                    ? new IPortModel[] {}
                : new[] {outputPortHolder.OutputPort};

                m_OutputPortContainer?.UpdatePorts(outputPort, m_OwnerElement.GraphView,
                    m_OwnerElement.Store);
            }
        }
    }
}
