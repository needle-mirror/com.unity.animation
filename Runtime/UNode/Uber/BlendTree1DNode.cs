using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public class BlendTree1DNode
        : NodeDefinition<BlendTree1DNode.Data, BlendTree1DNode.SimPorts, BlendTree1DNode.KernelData, BlendTree1DNode.KernelDefs, BlendTree1DNode.Kernel>
            , IParametrizable
            , IMsgHandler<BlobAssetReference<BlendTree1D>>
            , IMsgHandler<BlobAssetReference<RigDefinition>>
            , IBlendTree
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<BlendTree1DNode, BlobAssetReference<BlendTree1D>>       BlendTree;
            public MessageInput<BlendTree1DNode, BlobAssetReference<RigDefinition>>     RigDefinition;
            public MessageInput<BlendTree1DNode, Parameter>                             Parameter;

            public MessageOutput<BlendTree1DNode, BlobAssetReference<RigDefinition>>  RigDefinitionOut;
            public MessageOutput<BlendTree1DNode, Parameter>                          ParameterOut;

            public MessageOutput<BlendTree1DNode, float> Duration;
        }

        static readonly ProfilerMarker k_ProfileComputeBlendTree1D = new ProfilerMarker("Animation.ComputeBlendTree1DWeights");

        [Managed]
        public struct Data : INodeData
        {
            public Parameter            BlendParameter;

            // Assets.
            public BlobAssetReference<RigDefinition>    RigDefinition;
            public BlobAssetReference<BlendTree1D>      BlendTree;

            internal NodeHandle<BlendTree1DNode> BlendTree1DNode;
            internal NodeHandle<KernelPassThroughNodeFloat> NormalizedTimeNode;
            internal NodeHandle<MixerNode>        Mixer;

            internal List< NodeHandle > Motions;
            internal List< NodeHandle > MixerInputs;

            public float Duration;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<BlendTree1DNode, float> NormalizedTime;
            public DataOutput<BlendTree1DNode, Buffer<float>>  Output;
        }
        public struct KernelData : IKernelData
        {

        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var data = ref GetNodeData(ctx.Handle);
            data.BlendTree1DNode = Set.CastHandle<BlendTree1DNode>(ctx.Handle);

            data.NormalizedTimeNode = Set.Create<KernelPassThroughNodeFloat>();
            data.Mixer = Set.Create<MixerNode>();

            Set.Connect(data.BlendTree1DNode, SimulationPorts.RigDefinitionOut, data.Mixer, MixerNode.SimulationPorts.RigDefinition);

            ctx.ForwardInput(KernelPorts.NormalizedTime, data.NormalizedTimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
            ctx.ForwardOutput(KernelPorts.Output, data.Mixer, MixerNode.KernelPorts.Output);

            data.Motions = new List<NodeHandle>();
            data.MixerInputs = new List<NodeHandle>();
            data.Duration = 1.0f;
        }

        public override void Destroy(NodeHandle handle)
        {
            var data = GetNodeData(handle);

            Set.Destroy(data.NormalizedTimeNode);
            Set.Destroy(data.Mixer);

            for(int i=0;i<data.Motions.Count;i++)
                Set.Destroy(data.Motions[i]);
        }

        private void ReplaceMixerInput(ref Data data, int index, NodeHandle input)
        {
            while (index >= data.MixerInputs.Count)
            {
                data.MixerInputs.Add(new NodeHandle());
            }

            if (index > 1 || data.MixerInputs[index] == input)
                return;

            var mixerInputPort = (InputPortID) (index == 0 ? MixerNode.KernelPorts.Input0 : MixerNode.KernelPorts.Input1);

            // First let disconnect any previously connected input on mixerInputPort
            if (data.MixerInputs[index] != default)
            {
                var f = Set.GetFunctionality(data.MixerInputs[index]);
                if (f is INormalizedTimeMotion motion)
                {
                    Set.Disconnect(data.MixerInputs[index], motion.AnimationStreamOutputPort, data.Mixer, mixerInputPort);
                }
            }

            {
                var f = Set.GetFunctionality(input);
                if (f is INormalizedTimeMotion motion)
                {
                    Set.Connect(input, motion.AnimationStreamOutputPort, data.Mixer, mixerInputPort);
                }
            }

            data.MixerInputs[index] = input;
        }

        private void ApplyWeights(ref Data data)
        {
            k_ProfileComputeBlendTree1D.Begin();

            var duration = 0.0f;

            var length = data.BlendTree.Value.Motions.Length;
            if(length > 0)
            {
                var weights = new NativeArray<float>(length, Allocator.Temp);

                Core.ComputeBlendTree1DWeights(data.BlendTree, data.BlendParameter.Value, ref weights);

                var connectionCount = 0;
                for(int i=0;i<length;i++)
                {
                    float childDuration = 0.0f;
                    var f = Set.GetFunctionality(data.Motions[i]);
                    if (f is IMotion motion)
                    {
                        childDuration = motion.GetDuration(data.Motions[i]);
                    }
                    else
                    {
                        throw new InvalidOperationException( $"Cannot get duration from source node. Source is not of type {typeof(IMotion).Name}");
                    }

                    childDuration /= data.BlendTree.Value.MotionSpeeds[i];

                    if(weights[i] == 1.0f)
                    {
                        ReplaceMixerInput(ref data, 0, data.Motions[i]);
                        ReplaceMixerInput(ref data, 1, data.Motions[i]);
                        Set.SendMessage(data.Mixer, MixerNode.SimulationPorts.Blend, 1.0f);

                        duration = childDuration;
                        break;
                    }
                    else if( weights[i] > 0.0f)
                    {
                        duration += weights[i] * childDuration;

                        if(connectionCount == 0)
                        {
                            ReplaceMixerInput(ref data, 0, data.Motions[i]);
                            connectionCount++;
                        }
                        else if(connectionCount == 1)
                        {
                            ReplaceMixerInput(ref data, 1, data.Motions[i]);
                            Set.SendMessage(data.Mixer, MixerNode.SimulationPorts.Blend, weights[i]);
                            connectionCount++;
                        }
                    }
                }
            }

            data.Duration = duration;
            EmitMessage(data.BlendTree1DNode, SimulationPorts.Duration, data.Duration);

            k_ProfileComputeBlendTree1D.End();
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<BlendTree1D> blendTree)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);

            var thisHandle = Set.CastHandle<BlendTree1DNode>(ctx.Handle);

            if(nodeData.RigDefinition == default)
                throw new InvalidOperationException($"BlendTree1DNode: Please set RigDefinition before setting blendTree.");

            nodeData.BlendTree = blendTree;
            nodeData.BlendParameter.Id = blendTree.Value.BlendParameter.Id;

            for(int i=0;i<nodeData.Motions.Count;i++)
                Set.Destroy(nodeData.Motions[i]);

            nodeData.Motions.Clear();

            var length = nodeData.BlendTree.Value.Motions.Length;
            for(int i=0;i<length;i++)
            {
                NodeHandle motionNode = new NodeHandle();

                if(nodeData.BlendTree.Value.MotionTypes[i] == MotionType.Clip)
                {
                    var clip = WeakAssetReferenceUtils.LoadAsset<Clip>(nodeData.BlendTree.Value.Motions[i]);
                    var clipInstance = ClipManager.Instance.GetClipFor(nodeData.RigDefinition, clip);

                    var clipNode = Set.Create<UberClipNode>();
                    Set.SendMessage(clipNode,UberClipNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = (int)ClipConfigurationMask.NormalizedTime });
                    Set.SendMessage(clipNode, UberClipNode.SimulationPorts.ClipInstance, clipInstance);

                    motionNode = clipNode;
                    nodeData.Motions.Add(clipNode);
                }
                else if(nodeData.BlendTree.Value.MotionTypes[i] == MotionType.BlendTree1D)
                {
                    var childBlendTree = WeakAssetReferenceUtils.LoadAsset<BlendTree1D>(nodeData.BlendTree.Value.Motions[i]);

                    var blendTreeNode = Set.Create<BlendTree1DNode>();
                    Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.RigDefinition, nodeData.RigDefinition);
                    Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, childBlendTree);

                    motionNode = blendTreeNode;
                    nodeData.Motions.Add(blendTreeNode);
                }
                else if(nodeData.BlendTree.Value.MotionTypes[i] == MotionType.BlendTree2DSimpleDirectionnal)
                {
                    var childBlendTree = WeakAssetReferenceUtils.LoadAsset<BlendTree2DSimpleDirectionnal>(nodeData.BlendTree.Value.Motions[i]);

                    var blendTreeNode = Set.Create<BlendTree2DNode>();
                    Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.RigDefinition, nodeData.RigDefinition);
                    Set.SendMessage(blendTreeNode, BlendTree2DNode.SimulationPorts.BlendTree, childBlendTree);

                    motionNode = blendTreeNode;
                    nodeData.Motions.Add(blendTreeNode);
                }

                if(motionNode != default)
                {
                    var f = Set.GetFunctionality(motionNode);
                    if (f is INormalizedTimeMotion motion)
                    {
                        Set.Connect(nodeData.NormalizedTimeNode, (OutputPortID)KernelPassThroughNodeFloat.KernelPorts.Output, motionNode, motion.NormalizedTimeInputPort);
                    }

                    if (f is IBlendTree blendtree)
                    {
                        Set.Connect(thisHandle, (OutputPortID) SimulationPorts.RigDefinitionOut, motionNode, blendtree.RigDefinitionInputPort);
                        Set.Connect(thisHandle, (OutputPortID) SimulationPorts.ParameterOut, motionNode, blendtree.ParameterInputPort);
                    }
                }
            }

            // Forward rig definition to all children.
            EmitMessage(ctx.Handle, SimulationPorts.RigDefinitionOut, nodeData.RigDefinition);

            ApplyWeights(ref nodeData);
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigDefinition)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);
            nodeData.RigDefinition = rigDefinition;
            // Forward rig definition to all children
            EmitMessage(ctx.Handle, SimulationPorts.RigDefinitionOut, rigDefinition);
        }

        public void HandleMessage(in MessageContext ctx, in Parameter msg)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);
            if(nodeData.BlendParameter.Id == msg.Id && nodeData.BlendParameter.Value != msg.Value)
            {
                nodeData.BlendParameter.Value = msg.Value;

                ApplyWeights(ref nodeData);
            }

            // Forward parameter msg to all children. Child of type blend tree need to get this msg
            EmitMessage(ctx.Handle, SimulationPorts.ParameterOut, msg);
        }

        public OutputPortID AnimationStreamOutputPort =>
            (OutputPortID) KernelPorts.Output;

        public float GetDuration(NodeHandle handle) =>
            GetNodeData(handle).Duration;

        public InputPortID NormalizedTimeInputPort =>
            (InputPortID) KernelPorts.NormalizedTime;

        public InputPortID RigDefinitionInputPort =>
            (InputPortID) SimulationPorts.RigDefinition;

        public InputPortID ParameterInputPort =>
            (InputPortID) SimulationPorts.Parameter;

        public OutputPortID RigDefinitionOutputPort =>
            (OutputPortID) SimulationPorts.RigDefinitionOut;

        public OutputPortID ParameterOutputPort =>
            (OutputPortID) SimulationPorts.RigDefinitionOut;

        // Needed for test purpose to inspect internal node
        internal Data ExposeNodeData(NodeHandle handle) => GetNodeData(handle);
    }
}
