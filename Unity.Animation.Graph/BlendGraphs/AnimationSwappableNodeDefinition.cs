using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Entities;
using Unity.Profiling;

namespace Unity.Animation
{
    public struct GraphMessage // TODO@sonny Find a better name
    {
        public Hash128                GraphId;
        public GraphManager           GraphManager;
    }

    [NodeDefinition(guid: "ea7ab27ca26645b8b43d276c562e1351", version: 1, isHidden: true)]
    internal class PreAnimationGraphSlot :
        SimulationKernelNodeDefinition<PreAnimationGraphSlot.MessagePorts, PreAnimationGraphSlot.DataPorts>,
        IRigContextHandler<PreAnimationGraphSlot.NodeData>,
        ITimeControlHandler<PreAnimationGraphSlot.NodeData>,
        IEntityManagerHandler<PreAnimationGraphSlot.NodeData>,
        IInputReferenceHandler<PreAnimationGraphSlot.NodeData>
    {
        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Unity.Animation.PreAnimationGraphSlot : Assign Graph");
        public struct MessagePorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "a814eddc87654322a1291bccd98e6d60")]
            public MessageInput<PreAnimationGraphSlot, GraphMessage> GraphID;
            [PortDefinition(guid: "339863f98d0e4116bfd7202ca041ef6b")]
            public MessageInput<PreAnimationGraphSlot, Rig> Context;
            [PortDefinition(guid: "4f7ca5cfee81453bae207a57736a4d12")]
            public MessageInput<PreAnimationGraphSlot, EntityManager> EntityManager;
            [PortDefinition(guid: "97857a255abf408584d6cd2df6a91443")]
            public MessageInput<PreAnimationGraphSlot, TimeControl> TimeControl;
            [PortDefinition(guid: "3149863f98263145a207a57736a4df12")]
            public MessageInput<PreAnimationGraphSlot, NativeArray<InputReference>> InputReferences;

#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
            internal MessageOutput<PreAnimationGraphSlot, EntityContext> ToEntityContextMessagePassThrough;
            internal MessageOutput<PreAnimationGraphSlot, EntityManager> ToEntityManagerMessagePassThrough;
            internal MessageOutput<PreAnimationGraphSlot, NativeArray<InputReference>> ToInputReferenceMessagePassThrough;
            internal MessageOutput<PreAnimationGraphSlot, Rig> ToRigMessagePassThrough;
            internal MessageOutput<PreAnimationGraphSlot, TimeControl> ToTimeControlMessagePassThrough;
            internal MessageOutput<PreAnimationGraphSlot, GraphVariant> DefaultValuesDispatch;
            internal MessageOutput<PreAnimationGraphSlot, BlobAssetReference<Clip>> ClipDispatchSingle;
#pragma warning restore 649
        }
        public struct DataPorts : IKernelPortDefinition
        {
#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
            [PortDefinition(guid: "339563f98d0e4116bfd7202ca041ef6b")]
            public DataOutput<PreAnimationGraphSlot, Buffer<AnimatedData>> Output;
#pragma warning restore 649
        }

        InputPortID ITaskPort<IEntityManagerHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.EntityManager;
        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.Context;
        InputPortID ITaskPort<ITimeControlHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.TimeControl;
        InputPortID ITaskPort<IInputReferenceHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.InputReferences;

        [Managed]
        private struct NodeData : INodeData, IInit, IDestroy,
                                  IGraphSlot,
                                  IMsgHandler<Rig>,
                                  IMsgHandler<TimeControl>,
                                  IMsgHandler<GraphMessage> //TODO : make this a graph ID instead? That would require supporting graphID in graphvariants somehow
        {
            public BlobAssetReference<Graph> CachedGraphReference { get; set; }
            public Dictionary<NodeID, NodeHandle> Nodes { get; set; }

            public EntityManager CachedManager;
            public Rig CachedRig;
            public NativeArray<InputReference> CachedInputReferences;
            public TimeControl CachedTimeControl;
            public bool ReceivedTimeControl;

            public NodeHandle<KernelPhasePassThroughNodeBufferAnimatedData> m_PassThroughOutput;

            public NodeHandle GetNodeByID(NodeID id) => Nodes[id];

            public void Init(InitContext ctx)
            {
                var thisHandle = ctx.Set.CastHandle<PreAnimationGraphSlot>(ctx.Handle);
                Nodes = new Dictionary<NodeID, NodeHandle>();
                CachedManager = default;
                CachedRig = default;
                ReceivedTimeControl = false;
                m_PassThroughOutput = ctx.Set.Create<KernelPhasePassThroughNodeBufferAnimatedData>();
                ctx.ForwardOutput(KernelPorts.Output, m_PassThroughOutput, KernelPhasePassThroughNodeBufferAnimatedData.KernelPorts.Output);
                ctx.Set.Connect(thisHandle, SimulationPorts.ToRigMessagePassThrough, m_PassThroughOutput, KernelPhasePassThroughNodeBufferAnimatedData.SimulationPorts.Context);
            }

            public void Destroy(DestroyContext ctx)
            {
                foreach (var n in Nodes)
                    ctx.Set.Destroy(n.Value);
                Nodes.Clear();
                ctx.Set.Destroy(m_PassThroughOutput);
                if (CachedInputReferences.IsCreated)
                {
                    CachedInputReferences.Dispose();
                }
            }

            public void AddNode(NodeSetAPI set, CreateNodeCommand create)
            {
                Nodes.Add(create.NodeID, TypeRegistry.Instance.CreateNodeFromHash(set, create.TypeHash));
            }

            public PortDescription.InputPort GetInputPort(NodeSetAPI set, NodeID Node, PortID Port)
            {
                var node = Nodes[Node];
                return set.GetPortDescription(node).Inputs[Port];
            }

            public PortDescription.OutputPort GetOutputPort(NodeSetAPI set, NodeID Node, PortID Port)
            {
                var node = Nodes[Node];
                return set.GetPortDescription(node).Outputs[Port];
            }

            public void DispatchClip(MessageContext ctx, BlobAssetReference<Clip> clip, NodeID nodeID, PortID portID)
            {
                var thisNode = ctx.Set.CastHandle<PreAnimationGraphSlot>(ctx.Handle);
                var sourcePort = (OutputPortID)SimulationPorts.ClipDispatchSingle;

                PortDescription.InputPort targetPort = GetInputPort(ctx.Set, nodeID, portID);

                ctx.Set.Connect(thisNode, sourcePort, Nodes[nodeID], targetPort);
                ctx.EmitMessage(SimulationPorts.ClipDispatchSingle, clip);
                if (targetPort.Category == PortDescription.Category.Data)
                    ctx.Set.DisconnectAndRetainValue(thisNode, sourcePort, Nodes[nodeID], targetPort);
                else
                    ctx.Set.Disconnect(thisNode, sourcePort, Nodes[nodeID], targetPort);
            }

            void UpdateInputs(MessageContext ctx)
            {
                ref var targets = ref CachedGraphReference.Value.InputTargets;
                for (var i = 0; i < targets.Length; ++i)
                {
                    var id = targets[i].TypeHash;
                    var entity = Entity.Null;

                    // This is not great
                    foreach (var r in CachedInputReferences)
                    {
                        if (r.TypeHash == id)
                        {
                            entity = r.Entity;
                            break;
                        }
                    }

                    if (entity != Entity.Null)
                    {
                        var targetNode = Nodes[targets[i].NodeID];

                        PortDescription.InputPort targetPort =
                            GetInputPort(ctx.Set, targets[i].NodeID, targets[i].PortID);

                        ctx.Set.Connect(
                            ctx.Handle,
                            (OutputPortID)SimulationPorts.ToEntityContextMessagePassThrough,
                            targetNode,
                            targetPort);
                        ctx.EmitMessage(
                            SimulationPorts.ToEntityContextMessagePassThrough,
                            new EntityContext() { e = entity, Manager = CachedManager });
                        ctx.Set.Disconnect(
                            ctx.Handle,
                            (OutputPortID)SimulationPorts.ToEntityContextMessagePassThrough, targetNode, targetPort);
                    }
                }
            }

            public void HandleMessage(MessageContext ctx, in GraphMessage msg)
            {
                var graphID = new GraphID
                {
                    Value = msg.GraphId,
                };
                ReleaseInternalNodes(ctx.Set);
                if (!msg.GraphManager.TryGetGraph(graphID, out var graph))
                {
                    return;
                }

                var thisHandle = ctx.Set.CastHandle<PreAnimationGraphSlot>(ctx.Handle);

                CachedGraphReference = graph.Graph;
                k_ProfilerMarker.Begin();
                if (!graph.Graph.IsCreated)
                    throw new System.ArgumentNullException("Invalid Graph");

                GraphUtilityFunctions.CreateInternalTopology(ctx.Set, ref this, in graph.Graph);

                UpdateInputs(ctx);

                ref var parameters = ref graph.Graph.Value.SetValueCommands;
                for (var i = 0; i < parameters.Length; ++i)
                {
                    DispatchDefaultValue(ctx, parameters[i]);
                }

                ref var componentDataInputs = ref CachedGraphReference.Value.GraphInputs;
                for (var i = 0; i < componentDataInputs.Length; ++i)
                {
                    ref var forward = ref componentDataInputs[i];
                    OutputPortID sourcePort;
                    if (forward.Type == InputOutputType.EntityManager)
                    {
                        sourcePort = (OutputPortID)SimulationPorts.ToEntityManagerMessagePassThrough;
                    }
                    else if (forward.Type == InputOutputType.InputReferenceHandler)
                    {
                        sourcePort = (OutputPortID)SimulationPorts.ToInputReferenceMessagePassThrough;
                    }
                    else if (forward.Type == InputOutputType.ContextHandler)
                    {
                        sourcePort = (OutputPortID)SimulationPorts.ToRigMessagePassThrough;
                    }
                    else if (forward.Type == InputOutputType.TimeControlHandler)
                    {
                        sourcePort = (OutputPortID)SimulationPorts.ToTimeControlMessagePassThrough;
                    }
                    else
                    {
                        continue;
                    }
                    ctx.Set.Connect(
                        thisHandle,
                        sourcePort,
                        Nodes[forward.TargetNodeID],
                        GetInputPort(ctx.Set, forward.TargetNodeID, forward.TargetPortID));
                }


                ref var componentDataOutputs = ref graph.Graph.Value.GraphOutputs;
                if (componentDataOutputs.Length > 1)
                {
                    throw new InvalidOperationException($"Assigning an animaton graph with more than one output to {nameof(PreAnimationGraphSlot)}");
                }
                for (var i = 0; i < componentDataOutputs.Length; ++i)
                {
                    ref var forward = ref componentDataOutputs[i];
                    if (forward.Type == InputOutputType.ComponentData)
                    {
                        //Instead of connecting the output to the ComponentNode, we connect them to the passthrough
                        ctx.Set.Connect(
                            Nodes[forward.TargetNodeID],
                            GetOutputPort(ctx.Set, forward.TargetNodeID, forward.TargetPortID),
                            m_PassThroughOutput,
                            (InputPortID)KernelPhasePassThroughNodeBufferAnimatedData.KernelPorts.Input
                        );
                    }
                }

                if (CachedRig.Value.IsCreated)
                {
                    ctx.EmitMessage(SimulationPorts.ToRigMessagePassThrough, CachedRig);
                }

                if (CachedManager != default)
                {
                    ctx.EmitMessage(SimulationPorts.ToEntityManagerMessagePassThrough, CachedManager);
                }

                if (CachedInputReferences.IsCreated)
                {
                    ctx.EmitMessage(SimulationPorts.ToInputReferenceMessagePassThrough, CachedInputReferences);
                }

                if (ReceivedTimeControl)
                {
                    ctx.EmitMessage(SimulationPorts.ToTimeControlMessagePassThrough, CachedTimeControl);
                }

                {
                    ref var assetConnections = ref graph.Graph.Value.ConnectAssetCommands;
                    for (var i = 0; i < assetConnections.Length; ++i)
                    {
                        if (assetConnections[i].AssetType == AnimationGraphAssetHashes.ClipHash)
                        {
                            BlobAssetReference<Clip> asset = GenericAssetManager<Clip, ClipRegister>.Instance.GetAsset(assetConnections[i].AssetID); //TODO cache this
                            if (!asset.IsCreated)
                            {
                                throw new ArgumentException($"Asset {assetConnections[i].AssetID} does not seem to be created");
                            }

                            DispatchClip(ctx, asset, assetConnections[i].DestinationNodeID, assetConnections[i].DestinationPortID);
                        }
                    }
                }
                k_ProfilerMarker.End();
            }

            public void HandleMessage(MessageContext ctx, in TimeControl msg)
            {
                ReceivedTimeControl = true;
                CachedTimeControl = msg;
                ctx.EmitMessage(SimulationPorts.ToTimeControlMessagePassThrough, CachedTimeControl);
            }

            public void DispatchDefaultValue(MessageContext ctx, SetValueCommand valueCommand)
            {
                var thisHandle = ctx.Set.CastHandle<PreAnimationGraphSlot>(ctx.Handle);
                var targetNode = Nodes[valueCommand.Node];
                PortDescription.InputPort tgtPort = GetInputPort(ctx.Set, valueCommand.Node, valueCommand.Port);
                if (valueCommand.Port.IsPortArray())
                {
                    ctx.Set.Connect(thisHandle, (OutputPortID)SimulationPorts.DefaultValuesDispatch, targetNode, tgtPort, valueCommand.Port.Index);
                }
                else
                {
                    ctx.Set.Connect(thisHandle, (OutputPortID)SimulationPorts.DefaultValuesDispatch, targetNode, tgtPort);
                }
                var variant = new GraphVariant()
                {
                    Int4 = valueCommand.Value
                };
                variant.Type = valueCommand.Type;
                ctx.EmitMessage(SimulationPorts.DefaultValuesDispatch, variant);
                if (tgtPort.Category == PortDescription.Category.Message)
                {
                    if (valueCommand.Port.IsPortArray())
                        ctx.Set.Disconnect(thisHandle, (OutputPortID)SimulationPorts.DefaultValuesDispatch, targetNode, tgtPort, valueCommand.Port.Index);
                    else
                        ctx.Set.Disconnect(thisHandle, (OutputPortID)SimulationPorts.DefaultValuesDispatch, targetNode, tgtPort);
                }
                else
                {
                    if (valueCommand.Port.IsPortArray())
                        ctx.Set.DisconnectAndRetainValue(thisHandle, (OutputPortID)SimulationPorts.DefaultValuesDispatch, targetNode, tgtPort, valueCommand.Port.Index);
                    else
                        ctx.Set.DisconnectAndRetainValue(thisHandle, (OutputPortID)SimulationPorts.DefaultValuesDispatch, targetNode, tgtPort);
                }
            }

            private void ReleaseInternalNodes(NodeSetAPI set)
            {
                foreach (var n in Nodes)
                    set.Destroy(n.Value);
                Nodes.Clear();
            }

            public void HandleMessage(MessageContext ctx, in Rig rig)
            {
                CachedRig = rig;
                ctx.EmitMessage(SimulationPorts.ToRigMessagePassThrough, rig);
            }

            public void HandleMessage(MessageContext ctx, in EntityManager msg)
            {
                CachedManager = msg;
                ctx.EmitMessage(SimulationPorts.ToEntityManagerMessagePassThrough, msg);
            }

            public void HandleMessage(MessageContext ctx, in NativeArray<InputReference> msg)
            {
                if (msg.IsCreated)
                {
                    if (CachedInputReferences.IsCreated)
                        CachedInputReferences.Dispose();
                    CachedInputReferences = new NativeArray<InputReference>(msg, Allocator.Persistent);
                    UpdateInputs(ctx);
                }

                ctx.EmitMessage(SimulationPorts.ToInputReferenceMessagePassThrough, msg);
            }
        }

        public struct KernelData : IKernelData
        {
        }

        [BurstCompile]
        public struct Kernel : IGraphKernel<KernelData, DataPorts>
        {
            public void Execute(RenderContext ctx, in KernelData data, ref DataPorts ports)
            {
            }
        }
    }
}
