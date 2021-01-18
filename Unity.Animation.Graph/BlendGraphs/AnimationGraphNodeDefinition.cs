using System;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.DataFlowGraph.Attributes;
using System.Collections.Generic;
using Unity.Collections;

namespace Unity.Animation
{
    [NodeDefinition(guid: "b443f0f88b334dfa8f262f05df04217d", version: 1, isHidden: true)]
    public class AnimationGraphNodeDefinition : SimulationNodeDefinition<AnimationGraphNodeDefinition.MessagePorts>,
        IRigContextHandler<AnimationGraphNodeDefinition.NodeData>,
        IComponentNodeHandler<AnimationGraphNodeDefinition.NodeData>,
        IGraphHandler<AnimationGraphNodeDefinition.NodeData>,
        IEntityManagerHandler<AnimationGraphNodeDefinition.NodeData>,
        IGraphInstanceHandler<AnimationGraphNodeDefinition.NodeData>,
        IInputReferenceHandler<AnimationGraphNodeDefinition.NodeData>
    {
        public struct MessagePorts : ISimulationPortDefinition
        {
            public MessageInput<AnimationGraphNodeDefinition, BlendTree1DAsset> BlendTree1DAsset;
            public MessageInput<AnimationGraphNodeDefinition, DynamicBuffer<BlendTree1DAsset>> BlendTree1DAssets;
            public MessageInput<AnimationGraphNodeDefinition, BlendTree2DAsset> BlendTree2DAsset;
            public MessageInput<AnimationGraphNodeDefinition, DynamicBuffer<BlendTree2DAsset>> BlendTree2DAssets;
            public MessageInput<AnimationGraphNodeDefinition, Rig> Context;
            public MessageInput<AnimationGraphNodeDefinition, EntityManager> EntityManager;
            public MessageInput<AnimationGraphNodeDefinition, NativeArray<InputReference>> InputReferences;

            public MessageInput<AnimationGraphNodeDefinition, BlobAssetReference<Graph>> Graph;
            public MessageInput<AnimationGraphNodeDefinition, BlobAssetReference<GraphInstanceParameters>> GraphInstance;
            public MessageInput<AnimationGraphNodeDefinition, NodeHandle<ComponentNode>> ComponentNodePort;

#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
            internal MessageOutput<AnimationGraphNodeDefinition, EntityContext> ToEntityContextMessagePassThrough;
            internal MessageOutput<AnimationGraphNodeDefinition, TimeControl> ToTimeControlMessagePassThrough;
            internal MessageOutput<AnimationGraphNodeDefinition, EntityManager> ToEntityManagerMessagePassThrough;
            internal MessageOutput<AnimationGraphNodeDefinition, Rig> ToRigMessagePassThrough;
            internal MessageOutput<AnimationGraphNodeDefinition, NativeArray<InputReference>> ToInputReferencesMessagePassThrough;

            internal MessageOutput<AnimationGraphNodeDefinition, GraphVariant> DefaultValuesDispatch;
            internal MessageOutput<AnimationGraphNodeDefinition, BlobAssetReference<Clip>> ClipDispatchSingle;
            internal MessageOutput<AnimationGraphNodeDefinition, BlobAssetReference<BlendTree1D>> BlendTree1DDispatchSingle;
            internal MessageOutput<AnimationGraphNodeDefinition, DynamicBuffer<BlobAssetReference<BlendTree1D>>> BlendTree1DDispatchBuffer;
            internal MessageOutput<AnimationGraphNodeDefinition, BlobAssetReference<BlendTree2DSimpleDirectional>> BlendTree2DDispatchSingle;
            internal MessageOutput<AnimationGraphNodeDefinition, DynamicBuffer<BlobAssetReference<BlendTree2DSimpleDirectional>>> BlendTree2DDispatchBuffer;

#pragma warning restore 649
        }

        InputPortID ITaskPort<IGraphInstanceHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.GraphInstance;
        InputPortID ITaskPort<IGraphHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.Graph;
        InputPortID ITaskPort<IComponentNodeHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.ComponentNodePort;
        InputPortID ITaskPort<IEntityManagerHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.EntityManager;
        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.Context;
        InputPortID ITaskPort<IInputReferenceHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.InputReferences;

        [Managed]
        private struct NodeData : INodeData, IInit, IDestroy,
                                  IRootGraphSimulationData,
                                  IMsgHandler<Rig>,
                                  IAnimationAssetsMsgHandler
        {
            public BlobAssetReference<Graph> CachedGraphReference { get; set; }
            public Dictionary<NodeID, NodeHandle> Nodes { get; set; }
            public Dictionary<ulong, InputTarget> InputTargets { get; set; }

            public EntityManager Manager { get; set; }

            public NodeHandle GetNodeByID(NodeID id) => Nodes[id];

            public void Init(InitContext ctx)
            {
                Nodes = new Dictionary<NodeID, NodeHandle>();
            }

            public void Destroy(DestroyContext ctx)
            {
                foreach (var n in Nodes)
                    ctx.Set.Destroy(n.Value);
            }

            public void AddNode(NodeSetAPI set, CreateNodeCommand create)
            {
                Nodes.Add(create.NodeID, TypeRegistry.Instance.CreateNodeFromHash(set, create.TypeHash));
            }

            public void HandleMessage(MessageContext ctx, in NodeHandle<ComponentNode> msg)
            {
                if (msg == default)
                {
                    throw new ArgumentNullException("ComponentNode is invalid");
                }

                ref var componentDataInputs = ref CachedGraphReference.Value.GraphInputs;
                for (var i = 0; i < componentDataInputs.Length; ++i)
                {
                    ref var forward = ref componentDataInputs[i];
                    if (forward.Type != InputOutputType.ComponentData)
                        continue;

                    if (forward.TargetPortID.IsPortArray())
                    {
                        ctx.Set.Connect(
                            msg,
                            Unity.DataFlowGraph.ComponentNode.Output(ComponentType.ReadOnly(TypeManager.GetTypeIndexFromStableTypeHash(forward.TypeHash))),
                            Nodes[forward.TargetNodeID],
                            GetInputPort(ctx.Set, forward.TargetNodeID, forward.TargetPortID),
                            forward.TargetPortID.Index,
                            NodeSetAPI.ConnectionType.Feedback
                        );
                    }
                    else
                    {
                        ctx.Set.Connect(
                            msg,
                            Unity.DataFlowGraph.ComponentNode.Output(ComponentType.ReadOnly(TypeManager.GetTypeIndexFromStableTypeHash(forward.TypeHash))),
                            Nodes[forward.TargetNodeID],
                            GetInputPort(ctx.Set, forward.TargetNodeID, forward.TargetPortID),
                            NodeSetAPI.ConnectionType.Feedback);
                    }
                }

                ref var componentDataOutputs = ref CachedGraphReference.Value.GraphOutputs;
                for (var i = 0; i < componentDataOutputs.Length; ++i)
                {
                    ref var forward = ref componentDataOutputs[i];
                    if (forward.Type != InputOutputType.ComponentData)
                        continue;
                    ctx.Set.Connect(
                        Nodes[forward.TargetNodeID],
                        GetOutputPort(ctx.Set, forward.TargetNodeID, forward.TargetPortID),
                        msg,
                        Unity.DataFlowGraph.ComponentNode.Input(ComponentType.FromTypeIndex(TypeManager.GetTypeIndexFromStableTypeHash(forward.TypeHash))));
                }
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
                var thisNode = ctx.Set.CastHandle<AnimationGraphNodeDefinition>(ctx.Handle);
                var sourcePort = (OutputPortID)SimulationPorts.ClipDispatchSingle;

                PortDescription.InputPort targetPort = GetInputPort(ctx.Set, nodeID, portID);

                ctx.Set.Connect(thisNode, sourcePort, Nodes[nodeID], targetPort);
                ctx.EmitMessage(SimulationPorts.ClipDispatchSingle, clip);
                if (targetPort.Category == PortDescription.Category.Data)
                    ctx.Set.DisconnectAndRetainValue(thisNode, sourcePort, Nodes[nodeID], targetPort);
                else
                    ctx.Set.Disconnect(thisNode, sourcePort, Nodes[nodeID], targetPort);
            }

            // Here when the AnimationGraphNodeDefinition receives a Graph, we recreate the topology
            // indicated in the Graph
            public void HandleMessage(MessageContext ctx, in BlobAssetReference<Graph> msg)
            {
                if (!msg.IsCreated)
                    throw new ArgumentNullException(nameof(msg));
                CachedGraphReference = msg;

                var thisHandle = ctx.Set.CastHandle<AnimationGraphNodeDefinition>(ctx.Handle);

                GraphUtilityFunctions.CreateInternalTopology(ctx.Set, ref this, in msg);

                // Store Input Targets
                {
                    InputTargets = new Dictionary<ulong, InputTarget>();
                    ref var targets = ref msg.Value.InputTargets;
                    for (var i = 0; i < targets.Length; ++i)
                    {
                        InputTargets.Add(targets[i].TypeHash, targets[i]);
                    }
                }

                // Dispatch all default values for ports
                {
                    ref var parameters = ref msg.Value.SetValueCommands;
                    for (var i = 0; i < parameters.Length; ++i)
                    {
                        DispatchValue(ctx, parameters[i]);
                    }
                }

                // Connect clips
                {
                    ref var assetConnections = ref msg.Value.ConnectAssetCommands;
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

                // Connect outputs to context passthroughs
                {
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
                            sourcePort = (OutputPortID)SimulationPorts.ToInputReferencesMessagePassThrough;
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
                        ctx.Set.Connect(thisHandle,
                            sourcePort,
                            Nodes[forward.TargetNodeID],
                            GetInputPort(ctx.Set, forward.TargetNodeID, forward.TargetPortID));
                    }
                }
            }

            public void HandleMessage(MessageContext ctx, in BlobAssetReference<GraphInstanceParameters> msg)
            {
                ref var parameters = ref msg.Value.Values;
                for (var i = 0; i < parameters.Length; ++i)
                {
                    DispatchValue(ctx, parameters[i]);
                }
            }

            public void DispatchValue(MessageContext ctx, SetValueCommand parameter)
            {
                var thisHandle = ctx.Set.CastHandle<AnimationGraphNodeDefinition>(ctx.Handle);
                var targetNode = Nodes[parameter.Node];
                PortDescription.InputPort tgtPort = GetInputPort(ctx.Set, parameter.Node, parameter.Port);
                if (parameter.Port.IsPortArray())
                {
                    ctx.Set.Connect(thisHandle, (OutputPortID)SimulationPorts.DefaultValuesDispatch, targetNode, tgtPort, parameter.Port.Index);
                }
                else
                {
                    ctx.Set.Connect(thisHandle, (OutputPortID)SimulationPorts.DefaultValuesDispatch, targetNode, tgtPort);
                }
                var variant = new GraphVariant()
                {
                    Int4 = parameter.Value
                };

                variant.Type = parameter.Type;
                ctx.EmitMessage(SimulationPorts.DefaultValuesDispatch, variant);
                if (tgtPort.Category == PortDescription.Category.Message)
                {
                    if (parameter.Port.IsPortArray())
                        ctx.Set.Disconnect(thisHandle, (OutputPortID)SimulationPorts.DefaultValuesDispatch, targetNode, tgtPort, parameter.Port.Index);
                    else
                        ctx.Set.Disconnect(thisHandle, (OutputPortID)SimulationPorts.DefaultValuesDispatch, targetNode, tgtPort);
                }
                else
                {
                    if (parameter.Port.IsPortArray())
                        ctx.Set.DisconnectAndRetainValue(thisHandle, (OutputPortID)SimulationPorts.DefaultValuesDispatch, targetNode, tgtPort, parameter.Port.Index);
                    else
                        ctx.Set.DisconnectAndRetainValue(thisHandle, (OutputPortID)SimulationPorts.DefaultValuesDispatch, targetNode, tgtPort);
                }
            }

            public void HandleMessage(MessageContext ctx, in EntityManager msg)
            {
                Manager = msg;

                ctx.EmitMessage(SimulationPorts.ToEntityManagerMessagePassThrough, msg);
            }

            public void HandleMessage(MessageContext ctx, in NativeArray<InputReference> msg)
            {
                foreach (var i in msg)
                {
                    if (InputTargets.TryGetValue(i.TypeHash, out var target))
                    {
                        var targetNode = Nodes[target.NodeID];

                        PortDescription.InputPort targetPort =
                            GetInputPort(ctx.Set, target.NodeID, target.PortID);

                        ctx.Set.Connect(
                            ctx.Handle,
                            (OutputPortID)SimulationPorts.ToEntityContextMessagePassThrough,
                            targetNode,
                            targetPort);
                        ctx.EmitMessage(
                            SimulationPorts.ToEntityContextMessagePassThrough,
                            new EntityContext() { e = i.Entity, Manager = Manager });
                        ctx.Set.Disconnect(
                            ctx.Handle,
                            (OutputPortID)SimulationPorts.ToEntityContextMessagePassThrough, targetNode, targetPort);
                    }
                }

                ctx.EmitMessage(SimulationPorts.ToInputReferencesMessagePassThrough, msg);
            }

            public void HandleMessage(MessageContext ctx, in Rig msg)
            {
                ctx.EmitMessage(SimulationPorts.ToRigMessagePassThrough, msg);
            }

            public void HandleMessage(MessageContext ctx, in BlendTree1DAsset msg)
            {
                var thisNode = ctx.Set.CastHandle<AnimationGraphNodeDefinition>(ctx.Handle);
                var sourcePort = (OutputPortID)SimulationPorts.BlendTree1DDispatchSingle;

                PortDescription.InputPort targetPort = GetInputPort(ctx.Set, msg.Node, msg.Port);

                ctx.Set.Connect(thisNode, sourcePort, Nodes[msg.Node], targetPort);
                ctx.EmitMessage(SimulationPorts.BlendTree1DDispatchSingle, msg.Value);
                if (targetPort.Category == PortDescription.Category.Data)
                    ctx.Set.DisconnectAndRetainValue(thisNode, sourcePort, Nodes[msg.Node], targetPort);
                else
                    ctx.Set.Disconnect(thisNode, sourcePort, Nodes[msg.Node], targetPort);
            }

            public void HandleMessage(MessageContext ctx, in DynamicBuffer<BlendTree1DAsset> msg)
            {
                var thisNode = ctx.Set.CastHandle<AnimationGraphNodeDefinition>(ctx.Handle);
                var sourcePort = (OutputPortID)SimulationPorts.BlendTree1DDispatchSingle;
                foreach (var asset in msg)
                {
                    PortDescription.InputPort targetPort = GetInputPort(ctx.Set, asset.Node, asset.Port);

                    ctx.Set.Connect(thisNode, sourcePort, Nodes[asset.Node], targetPort);
                    ctx.EmitMessage(SimulationPorts.BlendTree1DDispatchSingle, asset.Value);
                    if (targetPort.Category == PortDescription.Category.Data)
                        ctx.Set.DisconnectAndRetainValue(thisNode, sourcePort, Nodes[asset.Node], targetPort);
                    else
                        ctx.Set.Disconnect(thisNode, sourcePort, Nodes[asset.Node], targetPort);
                }
            }

            public void HandleMessage(MessageContext ctx, in BlendTree2DAsset msg)
            {
                var thisNode = ctx.Set.CastHandle<AnimationGraphNodeDefinition>(ctx.Handle);
                var sourcePort = (OutputPortID)SimulationPorts.BlendTree2DDispatchSingle;

                PortDescription.InputPort targetPort = GetInputPort(ctx.Set, msg.Node, msg.Port);

                ctx.Set.Connect(thisNode, sourcePort, Nodes[msg.Node], targetPort);
                ctx.EmitMessage(SimulationPorts.BlendTree2DDispatchSingle, msg.Value);
                if (targetPort.Category == PortDescription.Category.Data)
                    ctx.Set.DisconnectAndRetainValue(thisNode, sourcePort, Nodes[msg.Node], targetPort);
                else
                    ctx.Set.Disconnect(thisNode, sourcePort, Nodes[msg.Node], targetPort);
            }

            public void HandleMessage(MessageContext ctx, in DynamicBuffer<BlendTree2DAsset> msg)
            {
                var thisNode = ctx.Set.CastHandle<AnimationGraphNodeDefinition>(ctx.Handle);
                var sourcePort = (OutputPortID)SimulationPorts.BlendTree2DDispatchSingle;
                foreach (var asset in msg)
                {
                    PortDescription.InputPort targetPort = GetInputPort(ctx.Set, asset.Node, asset.Port);

                    ctx.Set.Connect(thisNode, sourcePort, Nodes[asset.Node], targetPort);
                    ctx.EmitMessage(SimulationPorts.BlendTree2DDispatchSingle, asset.Value);
                    if (targetPort.Category == PortDescription.Category.Data)
                        ctx.Set.DisconnectAndRetainValue(thisNode, sourcePort, Nodes[asset.Node], targetPort);
                    else
                        ctx.Set.Disconnect(thisNode, sourcePort, Nodes[asset.Node], targetPort);
                }
            }
        }
    }
}
