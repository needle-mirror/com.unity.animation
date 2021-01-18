using System.Collections.Generic;
using Unity.Collections;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Profiling;

namespace Unity.Animation
{
    public interface IGraphSlot : IMsgHandler<EntityManager>, IMsgHandler<NativeArray<InputReference>>
    {
        BlobAssetReference<Graph> CachedGraphReference { get; set; }
        Dictionary<NodeID, NodeHandle> Nodes { get; set; }
        void AddNode(NodeSetAPI set, CreateNodeCommand create);
        NodeHandle GetNodeByID(NodeID id);
        PortDescription.InputPort GetInputPort(NodeSetAPI set, NodeID Node, PortID Port);
        PortDescription.OutputPort GetOutputPort(NodeSetAPI set, NodeID Node, PortID Port);
    }

    public static class GraphUtilityFunctions
    {
        private static readonly ProfilerMarker k_ProfilerNodesMarker = new ProfilerMarker("Unity.Animation.CompositorDefaultImplementations : Setup Graph : Nodes");
        private static readonly ProfilerMarker k_ProfilerConnectionsMarker = new ProfilerMarker("Unity.Animation.CompositorDefaultImplementations : Setup Graph : Connections");
        private static readonly ProfilerMarker k_ProfilerPortArraysMarker = new ProfilerMarker("Unity.Animation.CompositorDefaultImplementations : Setup Graph : Port Arrays");
        private static readonly ProfilerMarker k_ProfilerParametersMarker = new ProfilerMarker("Unity.Animation.CompositorDefaultImplementations : Setup Graph : Parameters");

        public static void Connect<TNodeData>(NodeSetAPI nodeset, ref TNodeData data, ConnectNodesCommand connection)
            where TNodeData : INodeData, IGraphSlot
        {
            // TODO : Maybe create a method to throw when one of the nodes is invalid
            if (connection.DestinationNodeID != NodeID.Invalid &&
                connection.SourceNodeID != NodeID.Invalid)
            {
                if (connection.DestinationPortID.IsPortArray())
                    nodeset.Connect(
                        data.Nodes[connection.SourceNodeID],
                        data.GetOutputPort(nodeset, connection.SourceNodeID, connection.SourcePortID),
                        data.Nodes[connection.DestinationNodeID],
                        data.GetInputPort(nodeset, connection.DestinationNodeID, connection.DestinationPortID),
                        connection.DestinationPortID.Index);
                else
                    nodeset.Connect(
                        data.Nodes[connection.SourceNodeID],
                        data.GetOutputPort(nodeset, connection.SourceNodeID, connection.SourcePortID),
                        data.Nodes[connection.DestinationNodeID],
                        data.GetInputPort(nodeset, connection.DestinationNodeID, connection.DestinationPortID));
            }
        }

        public static void CreateInternalTopology<TNodeData>(NodeSetAPI nodeset, ref TNodeData data, in BlobAssetReference<Graph> graph)
            where TNodeData : INodeData, IGraphSlot
        {
            if (!graph.IsCreated)
                throw new System.ArgumentNullException("Invalid Graph");

            // Create nodes
            {
                k_ProfilerNodesMarker.Begin();
                ref var nodes = ref graph.Value.CreateCommands;
                for (var i = 0; i < nodes.Length; ++i)
                    data.AddNode(nodeset, nodes[i]);
                k_ProfilerNodesMarker.End();
            }

            // Set port array sizes
            {
                k_ProfilerConnectionsMarker.Begin();
                ref var ports = ref graph.Value.ResizeCommands;
                for (var i = 0; i < ports.Length; ++i)
                    nodeset.SetPortArraySize(
                        data.Nodes[ports[i].Node],
                        data.GetInputPort(nodeset, ports[i].Node, ports[i].Port),
                        ports[i].ArraySize);
                k_ProfilerConnectionsMarker.End();
            }

            // Create connections between internal nodes
            {
                k_ProfilerPortArraysMarker.Begin();
                ref var connections = ref graph.Value.ConnectCommands;
                for (var i = 0; i < connections.Length; ++i)
                    GraphUtilityFunctions.Connect(nodeset, ref data, connections[i]);
                k_ProfilerPortArraysMarker.End();
            }
        }
    }
}
