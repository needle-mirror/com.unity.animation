using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Profiling;
using Unity.Mathematics;

namespace Unity.Animation
{
    public class BlendTree2DNode
        : NodeDefinition<BlendTree2DNode.Data, BlendTree2DNode.SimPorts, BlendTree2DNode.KernelData, BlendTree2DNode.KernelDefs, BlendTree2DNode.Kernel>
            , IParametrizable
            , IMsgHandler<BlobAssetReference<BlendTree2DSimpleDirectionnal>>
            , IMsgHandler<BlobAssetReference<RigDefinition>>
            , IBlendTree
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<BlendTree2DNode, BlobAssetReference<BlendTree2DSimpleDirectionnal>>     BlendTree;
            public MessageInput<BlendTree2DNode, BlobAssetReference<RigDefinition>>                     RigDefinition;
            public MessageInput<BlendTree2DNode, Parameter>                                             Parameter;

            public MessageOutput<BlendTree2DNode, BlobAssetReference<RigDefinition>>  RigDefinitionOut;
            public MessageOutput<BlendTree2DNode, Parameter>                          ParameterOut;

            public MessageOutput<BlendTree2DNode, float> Duration;
        }

        static readonly ProfilerMarker k_ProfileComputeBlendTree2D = new ProfilerMarker("Animation.ComputeBlendTree2DWeights");

        [Managed]
        public struct Data : INodeData
        {
            public Parameter            BlendParameterX;
            public Parameter            BlendParameterY;

            // Assets.
            public BlobAssetReference<RigDefinition>                    RigDefinition;
            public BlobAssetReference<BlendTree2DSimpleDirectionnal>    BlendTree;

            internal NodeHandle<BlendTree2DNode>                 BlendTree2DNode;
            internal NodeHandle<KernelPassThroughNodeFloat>    NormalizedTimeNode;

            internal NodeHandle<MixerBeginNode>         MixerBegin;
            internal List< NodeHandle<MixerAddNode> >   MixerAdd;
            internal NodeHandle<MixerEndNode>           MixerEnd;

            internal List< NodeHandle > Motions;

            public float Duration;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<BlendTree2DNode, float> NormalizedTime;
            public DataOutput<BlendTree2DNode, Buffer<float>>  Output;
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
            data.BlendTree2DNode = Set.CastHandle<BlendTree2DNode>(ctx.Handle);

            data.NormalizedTimeNode = Set.Create<KernelPassThroughNodeFloat>();
            data.MixerBegin = Set.Create<MixerBeginNode>();
            data.MixerEnd = Set.Create<MixerEndNode>();

            Set.Connect(data.BlendTree2DNode, SimulationPorts.RigDefinitionOut, data.MixerBegin, MixerBeginNode.SimulationPorts.RigDefinition);
            Set.Connect(data.BlendTree2DNode, SimulationPorts.RigDefinitionOut, data.MixerEnd, MixerEndNode.SimulationPorts.RigDefinition);

            ctx.ForwardInput(KernelPorts.NormalizedTime, data.NormalizedTimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
            ctx.ForwardOutput(KernelPorts.Output, data.MixerEnd, MixerEndNode.KernelPorts.Output);

            data.Motions = new List<NodeHandle>();
            data.MixerAdd = new List<NodeHandle<MixerAddNode>>();
            data.Duration = 1.0f;
        }

        public override void Destroy(NodeHandle handle)
        {
            var data = GetNodeData(handle);

            Set.Destroy(data.NormalizedTimeNode);
            Set.Destroy(data.MixerBegin);
            Set.Destroy(data.MixerEnd);

            for (int i=0;i<data.MixerAdd.Count;i++)
                Set.Destroy(data.MixerAdd[i]);

            for (int i=0;i<data.Motions.Count;i++)
                Set.Destroy(data.Motions[i]);
        }

        private void ApplyWeights(ref Data data)
        {
            k_ProfileComputeBlendTree2D.Begin();

            var duration = 0.0f;

            var length = data.BlendTree.Value.Motions.Length;
            if(length > 0)
            {
                var weights = new NativeArray<float>(length, Allocator.Temp);

                var blendParameter = new float2(data.BlendParameterX.Value, data.BlendParameterY.Value);

                Core.ComputeBlendTree2DSimpleDirectionnalWeights(data.BlendTree, blendParameter, ref weights);

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
                    duration += weights[i] * childDuration;

                    Set.SetData(data.MixerAdd[i], MixerAddNode.KernelPorts.Weight, weights[i]);
                }
            }

            data.Duration = duration;
            EmitMessage(data.BlendTree2DNode, SimulationPorts.Duration, data.Duration);

            k_ProfileComputeBlendTree2D.End();
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<BlendTree2DSimpleDirectionnal> blendTree)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);

            var thisHandle = Set.CastHandle<BlendTree2DNode>(ctx.Handle);

            if(nodeData.RigDefinition == default)
                throw new InvalidOperationException($"BlendTree2DNode: Please set RigDefinition before setting blendTree.");

            nodeData.BlendTree = blendTree;
            nodeData.BlendParameterX.Id = blendTree.Value.BlendParameterX.Id;
            nodeData.BlendParameterY.Id = blendTree.Value.BlendParameterY.Id;

            for(int i=0;i<nodeData.MixerAdd.Count;i++)
                Set.Destroy(nodeData.MixerAdd[i]);

            for(int i=0;i<nodeData.Motions.Count;i++)
                Set.Destroy(nodeData.Motions[i]);

            nodeData.MixerAdd.Clear();
            nodeData.Motions.Clear();

            NodeHandle<MixerAddNode> previousMixerAdd = new NodeHandle<MixerAddNode>();

            var length = nodeData.BlendTree.Value.Motions.Length;
            for(int i=0;i<length;i++)
            {
                NodeHandle motionNode = new NodeHandle();

                var mixerAddNode = Set.Create<MixerAddNode>();
                nodeData.MixerAdd.Add(mixerAddNode);
                Set.Connect(thisHandle, SimulationPorts.RigDefinitionOut, mixerAddNode, MixerAddNode.SimulationPorts.RigDefinition);

                if(previousMixerAdd == default)
                {
                    Set.Connect(nodeData.MixerBegin, MixerBeginNode.KernelPorts.Output, mixerAddNode, MixerAddNode.KernelPorts.Input);
                    Set.Connect(nodeData.MixerBegin, MixerBeginNode.KernelPorts.SumWeight, mixerAddNode, MixerAddNode.KernelPorts.SumWeightInput);
                }
                else
                {
                    Set.Connect(previousMixerAdd, MixerAddNode.KernelPorts.Output, mixerAddNode, MixerAddNode.KernelPorts.Input);
                    Set.Connect(previousMixerAdd, MixerAddNode.KernelPorts.SumWeightOutput, mixerAddNode, MixerAddNode.KernelPorts.SumWeightInput);
                }

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

                    // Rig definition need to be set before blend tree
                    Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.RigDefinition, nodeData.RigDefinition);
                    Set.SendMessage(blendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, childBlendTree);

                    motionNode = blendTreeNode;
                    nodeData.Motions.Add(blendTreeNode);
                }
                else if(nodeData.BlendTree.Value.MotionTypes[i] == MotionType.BlendTree2DSimpleDirectionnal)
                {
                    var childBlendTree = WeakAssetReferenceUtils.LoadAsset<BlendTree2DSimpleDirectionnal>(nodeData.BlendTree.Value.Motions[i]);

                    var blendTreeNode = Set.Create<BlendTree2DNode>();

                    // Rig definition need to be set before blend tree
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
                        Set.Connect(motionNode, motion.AnimationStreamOutputPort, mixerAddNode, (InputPortID) MixerAddNode.KernelPorts.Add);
                    }

                    if (f is IBlendTree blendtree)
                    {
                        Set.Connect(thisHandle, (OutputPortID) SimulationPorts.RigDefinitionOut, motionNode, blendtree.RigDefinitionInputPort);
                        Set.Connect(thisHandle, (OutputPortID) SimulationPorts.ParameterOut, motionNode, blendtree.ParameterInputPort);
                    }
                }

                previousMixerAdd = mixerAddNode;
            }

            if(previousMixerAdd != default)
            {
                Set.Connect(previousMixerAdd, MixerAddNode.KernelPorts.SumWeightOutput, nodeData.MixerEnd, MixerEndNode.KernelPorts.SumWeight);
                Set.Connect(previousMixerAdd, MixerAddNode.KernelPorts.Output, nodeData.MixerEnd, MixerEndNode.KernelPorts.Input);
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
            if(nodeData.BlendParameterX.Id == msg.Id && nodeData.BlendParameterX.Value != msg.Value)
            {
                nodeData.BlendParameterX.Value = msg.Value;
                ApplyWeights(ref nodeData);
            }
            else if(nodeData.BlendParameterY.Id == msg.Id && nodeData.BlendParameterY.Value != msg.Value)
            {
                nodeData.BlendParameterY.Value = msg.Value;
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
            (OutputPortID)SimulationPorts.RigDefinitionOut;

        public OutputPortID ParameterOutputPort =>
            (OutputPortID) SimulationPorts.RigDefinitionOut;

        // Needed for test purpose to inspect internal node
        internal Data ExposeNodeData(NodeHandle handle) => GetNodeData(handle);
    }
}
