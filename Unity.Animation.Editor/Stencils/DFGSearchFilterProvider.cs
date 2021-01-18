using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class DFGSearchFilterProvider : ISearcherFilterProvider
    {
        readonly BaseGraphStencil m_Stencil;

        public DFGSearchFilterProvider(BaseGraphStencil stencil)
        {
            m_Stencil = stencil;
        }

        public SearcherFilter GetGraphSearcherFilter()
        {
            return GetGroupSearcherFilter();
        }

        public SearcherFilter GetGroupSearcherFilter()
        {
            return new SearcherFilter()
                .WithDataFlowGraphNodes(m_Stencil)
                .WithCompositorNodes();
        }

        public SearcherFilter GetDataPortSearcherFilter()
        {
            return new SearcherFilter().WithDataPortTypes();
        }

        public SearcherFilter GetMessagePortSearcherFilter()
        {
            return new SearcherFilter().WithMessagePortTypes();
        }

        public SearcherFilter GetOutputToGraphSearcherFilter(IPortModel portModel)
        {
            var compositorPort = portModel as BasePortModel;
            DFGService.PortUsage usage;
            if (compositorPort != null)
                usage = compositorPort.EvaluationType == BasePortModel.PortEvaluationType.Simulation ? DFGService.PortUsage.Message : DFGService.PortUsage.Data;
            else
                usage = portModel.PortType == PortType.Data ? DFGService.PortUsage.Message : DFGService.PortUsage.Data;

            return new SearcherFilter()
                .WithDataFlowGraphNodesWithInputPort(m_Stencil,
                usage,
                portModel.DataTypeHandle.Resolve());
        }

        public SearcherFilter GetInputToGraphSearcherFilter(IPortModel portModel)
        {
            var compositorPort = portModel as BasePortModel;
            DFGService.PortUsage usage;
            if (compositorPort != null)
                usage = compositorPort.EvaluationType == BasePortModel.PortEvaluationType.Simulation ? DFGService.PortUsage.Message : DFGService.PortUsage.Data;
            else
                usage = portModel.PortType == PortType.Data ? DFGService.PortUsage.Message : DFGService.PortUsage.Data;

            return new SearcherFilter()
                .WithDataFlowGraphNodesWithOutputPort(m_Stencil,
                usage,
                portModel.DataTypeHandle.Resolve());
        }

        public SearcherFilter GetEdgeSearcherFilter(IEdgeModel edgeModel) { return SearcherFilter.Empty; }
        public SearcherFilter GetTypeSearcherFilter()
        {
            return SearcherFilter.Empty;
        }
    }
}
