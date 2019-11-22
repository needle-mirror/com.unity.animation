using System;
using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using UnityEngine;
using Unity.Profiling;

namespace Unity.Animation
{
    public class LoopWeightNode
        : NodeDefinition<LoopWeightNode.Data, LoopWeightNode.SimPorts, LoopWeightNode.KernelData, LoopWeightNode.KernelDefs, LoopWeightNode.Kernel>
            , IMsgHandler<BlobAssetReference<RigDefinition>>
            , IMsgHandler<int>
    {
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.LoopWeightNode");
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<LoopWeightNode, BlobAssetReference<RigDefinition>> RigDefinition;
            public MessageInput<LoopWeightNode, int> SkipRoot;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<LoopWeightNode, float> Weight;
            public DataOutput<LoopWeightNode, Buffer<float>> Weights;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public int SkipRoot;
            public ProfilerMarker ProfileMarker;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                if (data.RigDefinition == BlobAssetReference<RigDefinition>.Null)
                    return;

                data.ProfileMarker.Begin();

                var weight = context.Resolve(ports.Weight);
                var weightArray = context.Resolve(ref ports.Weights);

                var count = data.RigDefinition.Value.Bindings.BindingCount;

                for (var iter = 0; iter < count; iter++)
                {
                    var isRoot = iter == data.RigDefinition.Value.Bindings.TranslationBindingIndex;
                    isRoot |= iter == data.RigDefinition.Value.Bindings.RotationBindingIndex;
                    isRoot |= iter == data.RigDefinition.Value.Bindings.ScaleBindingIndex;

                    weightArray[iter] = data.SkipRoot != 0 && isRoot ? 0 : weight;
                }

                data.ProfileMarker.End();
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMarker = k_ProfileMarker;
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigDefinition)
        {
            ref var kernelData = ref GetKernelData(ctx.Handle);

            kernelData.RigDefinition = rigDefinition;
            Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Weights, Buffer<float>.SizeRequest(rigDefinition.Value.Bindings.BindingCount));
        }

        public void HandleMessage(in MessageContext ctx, in int msg)
        {
            ref var kernelData = ref GetKernelData(ctx.Handle);
            kernelData.SkipRoot = msg != 0 ? 1 : 0;
        }
    }

    public class LoopNode
        : NodeDefinition<LoopNode.Data, LoopNode.SimPorts, LoopNode.KernelData, LoopNode.KernelDefs, LoopNode.Kernel>
            , IMsgHandler<BlobAssetReference<RigDefinition>>
            , IMsgHandler<int>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<LoopNode, BlobAssetReference<RigDefinition>> RigDefinition;
            public MessageInput<LoopNode, int> SkipRoot;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<LoopNode, float> NormalizedTime;
            public DataInput<LoopNode, Buffer<float>> Input;
            public DataInput<LoopNode, Buffer<float>> Delta;
            public DataOutput<LoopNode, Buffer<float>> Output;
        }

        public struct Data : INodeData
        {
            public NodeHandle<SimPassThroughNode<BlobAssetReference<RigDefinition>>> RigDefinitionNode;
            public NodeHandle<LoopWeightNode> LoopWeightNode;

            public NodeHandle<AddNode> AddNode;
            public NodeHandle<InverseNode> InverseNode;
            public NodeHandle<WeightNode> WeightNode;
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
            nodeData.LoopWeightNode = Set.Create<LoopWeightNode>();
            nodeData.AddNode = Set.Create<AddNode>();
            nodeData.WeightNode = Set.Create<WeightNode>();
            nodeData.InverseNode = Set.Create<InverseNode>();

            Set.Connect(nodeData.RigDefinitionNode, SimPassThroughNode<BlobAssetReference<RigDefinition>>.SimulationPorts.Output, nodeData.LoopWeightNode, LoopWeightNode.SimulationPorts.RigDefinition);
            Set.Connect(nodeData.RigDefinitionNode, SimPassThroughNode<BlobAssetReference<RigDefinition>>.SimulationPorts.Output, nodeData.AddNode, AddNode.SimulationPorts.RigDefinition);
            Set.Connect(nodeData.RigDefinitionNode, SimPassThroughNode<BlobAssetReference<RigDefinition>>.SimulationPorts.Output, nodeData.InverseNode, InverseNode.SimulationPorts.RigDefinition);
            Set.Connect(nodeData.RigDefinitionNode, SimPassThroughNode<BlobAssetReference<RigDefinition>>.SimulationPorts.Output, nodeData.WeightNode, WeightNode.SimulationPorts.RigDefinition);

            Set.Connect(nodeData.LoopWeightNode,LoopWeightNode.KernelPorts.Weights, nodeData.WeightNode, WeightNode.KernelPorts.Weights);
            Set.Connect(nodeData.WeightNode,WeightNode.KernelPorts.Output, nodeData.InverseNode, InverseNode.KernelPorts.Input);
            Set.Connect(nodeData.InverseNode,InverseNode.KernelPorts.Output, nodeData.AddNode, AddNode.KernelPorts.InputB);

            ctx.ForwardInput(SimulationPorts.RigDefinition, nodeData.RigDefinitionNode, SimPassThroughNode<BlobAssetReference<RigDefinition>>.SimulationPorts.Input);
            ctx.ForwardInput(SimulationPorts.SkipRoot, nodeData.LoopWeightNode, LoopWeightNode.SimulationPorts.SkipRoot);
            ctx.ForwardInput(KernelPorts.NormalizedTime, nodeData.LoopWeightNode, LoopWeightNode.KernelPorts.Weight);
            ctx.ForwardInput(KernelPorts.Input, nodeData.AddNode, AddNode.KernelPorts.InputA);
            ctx.ForwardInput(KernelPorts.Delta, nodeData.WeightNode, WeightNode.KernelPorts.Input);
            ctx.ForwardOutput(KernelPorts.Output, nodeData.AddNode, AddNode.KernelPorts.Output);
        }

        public override void Destroy(NodeHandle handle)
        {
            var nodeData = GetNodeData(handle);

            Set.Destroy(nodeData.RigDefinitionNode);
            Set.Destroy(nodeData.LoopWeightNode);
            Set.Destroy(nodeData.AddNode);
            Set.Destroy(nodeData.InverseNode);
            Set.Destroy(nodeData.WeightNode);
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigDefinition)
        {
        }

        public void HandleMessage(in MessageContext ctx, in int msg)
        {
        }
    }
}
