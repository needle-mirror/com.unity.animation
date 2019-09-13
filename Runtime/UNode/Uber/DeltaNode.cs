using System;
using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using UnityEngine;

namespace Unity.Animation
{
    public class DeltaNode
        : NodeDefinition<DeltaNode.Data, DeltaNode.SimPorts, DeltaNode.KernelData, DeltaNode.KernelDefs, DeltaNode.Kernel>
            , IMsgHandler<BlobAssetReference<RigDefinition>>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<DeltaNode, BlobAssetReference<RigDefinition>> RigDefinition;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<DeltaNode, Buffer<float>> Input;
            public DataInput<DeltaNode, Buffer<float>> Subtract;
            public DataOutput<DeltaNode, Buffer<float>> Output;
        }

        public struct Data : INodeData
        {
            public NodeHandle<SimPassThroughNode<BlobAssetReference<RigDefinition>>> RigDefinitionNode;
            public NodeHandle<AddNode> AddNode;
            public NodeHandle<InverseNode> InverseNode;
        }

        public struct KernelData : IKernelData
        {
        }

        [BurstCompile]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);

            nodeData.RigDefinitionNode = Set.Create<SimPassThroughNode<BlobAssetReference<RigDefinition>>>();
            nodeData.AddNode = Set.Create<AddNode>();
            nodeData.InverseNode = Set.Create<InverseNode>();

            Set.Connect(nodeData.RigDefinitionNode, SimPassThroughNode<BlobAssetReference<RigDefinition>>.SimulationPorts.Output, nodeData.AddNode, AddNode.SimulationPorts.RigDefinition);
            Set.Connect(nodeData.RigDefinitionNode, SimPassThroughNode<BlobAssetReference<RigDefinition>>.SimulationPorts.Output, nodeData.InverseNode, InverseNode.SimulationPorts.RigDefinition);

            Set.Connect(nodeData.InverseNode,InverseNode.KernelPorts.Output, nodeData.AddNode, AddNode.KernelPorts.InputA);

            ctx.ForwardInput(SimulationPorts.RigDefinition, nodeData.RigDefinitionNode, SimPassThroughNode<BlobAssetReference<RigDefinition>>.SimulationPorts.Input);
            ctx.ForwardInput(KernelPorts.Input, nodeData.AddNode, AddNode.KernelPorts.InputB);
            ctx.ForwardInput(KernelPorts.Subtract, nodeData.InverseNode, InverseNode.KernelPorts.Input);
            ctx.ForwardOutput(KernelPorts.Output, nodeData.AddNode, AddNode.KernelPorts.Output);
        }

        public override void Destroy(NodeHandle handle)
        {
            var nodeData = GetNodeData(handle);

            Set.Destroy(nodeData.RigDefinitionNode);
            Set.Destroy(nodeData.AddNode);
            Set.Destroy(nodeData.InverseNode);
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigDefinition)
        {
        }

        public void HandleMessage(in MessageContext ctx, in int msg)
        {
        }
    }
}
