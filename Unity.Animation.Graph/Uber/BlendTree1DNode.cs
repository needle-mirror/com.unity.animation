using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "a88bd40bda0f4f379831b0c7f023ce6e", version: 1, category: "Animation Core/Blend Trees", description: "Evaluates a 1D BlendTree based on a blend parameter")]
    public class BlendTree1DNode
        : SimulationKernelNodeDefinition<BlendTree1DNode.SimPorts, BlendTree1DNode.KernelDefs>
        , IRigContextHandler<BlendTree1DNode.Data>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "390da22df44642a9b5ad2adaaf83c1a6", description: "BlendTree data")]
            public MessageInput<BlendTree1DNode, BlobAssetReference<BlendTree1D>> BlendTree;
            [PortDefinition(guid: "aa9894ceaa80410296466cda31f6c744", isHidden: true)]
            public MessageInput<BlendTree1DNode, Rig> Rig;

            [PortDefinition(guid: "9c16a594bbf44b8cb0d4757a37037bb5", isHidden: true)]
            public MessageOutput<BlendTree1DNode, Rig> RigOut;

            // For internal messages in node data.
            internal MessageOutput<BlendTree1DNode, BlobAssetReference<BlendTree1D>> m_OutBlendTree;
            internal PortArray<MessageOutput<BlendTree1DNode, ClipConfiguration>> m_OutClipConfigs;
            internal PortArray<MessageOutput<BlendTree1DNode, BlobAssetReference<Clip>>> m_OutClips;
            internal PortArray<MessageOutput<BlendTree1DNode, float>> m_OutMotionDurations;
            internal PortArray<MessageOutput<BlendTree1DNode, int>> m_OutIndices;
        }

        [Managed]
        internal struct Data : INodeData, IInit, IDestroy
            , IMsgHandler<Rig>, IMsgHandler<BlobAssetReference<BlendTree1D>>
        {
            // Assets.
            internal BlobAssetReference<RigDefinition>  m_RigDefinition;
            BlobAssetReference<BlendTree1D>             m_BlendTree;

            NodeHandle<ComputeBlendTree1DWeightsNode> m_ComputeBlendTree1DWeightsNode;
            NodeHandle<KernelPassThroughNodeFloat>    m_NormalizedTimeNode;
            NodeHandle<NMixerNode>                    m_NMixerNode;

            internal List<NodeHandle<UberClipNode>>     m_Motions;
            List<NodeHandle<GetBufferElementValueNode>> m_MotionDurationNodes;


            public void Init(InitContext ctx)
            {
                var thisHandle = ctx.Set.CastHandle<BlendTree1DNode>(ctx.Handle);

                m_NormalizedTimeNode = ctx.Set.Create<KernelPassThroughNodeFloat>();
                m_NMixerNode = ctx.Set.Create<NMixerNode>();
                m_ComputeBlendTree1DWeightsNode = ctx.Set.Create<ComputeBlendTree1DWeightsNode>();

                ctx.Set.Connect(thisHandle, SimulationPorts.RigOut, m_NMixerNode, NMixerNode.SimulationPorts.Rig);
                ctx.Set.Connect(thisHandle, SimulationPorts.m_OutBlendTree, m_ComputeBlendTree1DWeightsNode, ComputeBlendTree1DWeightsNode.SimulationPorts.BlendTree);

                ctx.ForwardInput(KernelPorts.NormalizedTime, m_NormalizedTimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
                ctx.ForwardInput(KernelPorts.BlendParameter, m_ComputeBlendTree1DWeightsNode, ComputeBlendTree1DWeightsNode.KernelPorts.BlendParameter);
                ctx.ForwardOutput(KernelPorts.Output, m_NMixerNode, NMixerNode.KernelPorts.Output);
                ctx.ForwardOutput(KernelPorts.Duration, m_ComputeBlendTree1DWeightsNode, ComputeBlendTree1DWeightsNode.KernelPorts.Duration);

                m_Motions = new List<NodeHandle<UberClipNode>>();
                m_MotionDurationNodes = new List<NodeHandle<GetBufferElementValueNode>>();
            }

            public void Destroy(DestroyContext ctx)
            {
                ctx.Set.Destroy(m_NormalizedTimeNode);
                ctx.Set.Destroy(m_NMixerNode);
                ctx.Set.Destroy(m_ComputeBlendTree1DWeightsNode);

                for (int i = 0; i < m_Motions.Count; i++)
                    ctx.Set.Destroy(m_Motions[i]);

                for (int i = 0; i < m_MotionDurationNodes.Count; i++)
                    ctx.Set.Destroy(m_MotionDurationNodes[i]);
            }

            public void HandleMessage(MessageContext ctx, in BlobAssetReference<BlendTree1D> blendTree)
            {
                var thisHandle = ctx.Set.CastHandle<BlendTree1DNode>(ctx.Handle);

                m_BlendTree = blendTree;

                ctx.EmitMessage(SimulationPorts.m_OutBlendTree, blendTree);

                for (int i = 0; i < m_Motions.Count; i++)
                    ctx.Set.Destroy(m_Motions[i]);

                for (int i = 0; i < m_MotionDurationNodes.Count; i++)
                    ctx.Set.Destroy(m_MotionDurationNodes[i]);

                m_Motions.Clear();
                m_MotionDurationNodes.Clear();

                var length = m_BlendTree.Value.Motions.Length;

                ctx.Set.SetPortArraySize(m_NMixerNode, NMixerNode.KernelPorts.Inputs, (ushort)length);
                ctx.Set.SetPortArraySize(m_NMixerNode, NMixerNode.KernelPorts.Weights, (ushort)length);
                ctx.Set.SetPortArraySize(m_ComputeBlendTree1DWeightsNode, ComputeBlendTree1DWeightsNode.KernelPorts.MotionDurations, (ushort)length);

                ctx.Set.SetPortArraySize(thisHandle, SimulationPorts.m_OutClipConfigs, (ushort)length);
                ctx.Set.SetPortArraySize(thisHandle, SimulationPorts.m_OutClips, (ushort)length);
                ctx.Set.SetPortArraySize(thisHandle, SimulationPorts.m_OutMotionDurations, (ushort)length);
                ctx.Set.SetPortArraySize(thisHandle, SimulationPorts.m_OutIndices, (ushort)length);

                for (int i = 0; i < length; i++)
                {
                    var clip = m_BlendTree.Value.Motions[i];
                    Core.ValidateIsCreated((BlobAssetReference<Clip>)clip);

                    var motionNode = ctx.Set.Create<UberClipNode>();

                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutClipConfigs, i, motionNode, UberClipNode.SimulationPorts.Configuration);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutClips, i, motionNode, UberClipNode.SimulationPorts.Clip);
                    ctx.EmitMessage(SimulationPorts.m_OutClipConfigs, i, new ClipConfiguration { Mask = ClipConfigurationMask.NormalizedTime });
                    ctx.EmitMessage(SimulationPorts.m_OutClips, i, clip);

                    var bufferElementToPortNode = ctx.Set.Create<GetBufferElementValueNode>();

                    m_Motions.Add(motionNode);
                    m_MotionDurationNodes.Add(bufferElementToPortNode);

                    ctx.Set.Connect(thisHandle, SimulationPorts.RigOut, motionNode, UberClipNode.SimulationPorts.Rig);
                    ctx.Set.Connect(m_NormalizedTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, motionNode, UberClipNode.KernelPorts.Time);
                    ctx.Set.Connect(motionNode, UberClipNode.KernelPorts.Output, m_NMixerNode, NMixerNode.KernelPorts.Inputs, i);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutMotionDurations, i, m_ComputeBlendTree1DWeightsNode, ComputeBlendTree1DWeightsNode.KernelPorts.MotionDurations, i);
                    ctx.EmitMessage(SimulationPorts.m_OutMotionDurations,  i, clip.Value.Duration);

                    ctx.Set.Connect(m_ComputeBlendTree1DWeightsNode, ComputeBlendTree1DWeightsNode.KernelPorts.Weights, bufferElementToPortNode, GetBufferElementValueNode.KernelPorts.Input);
                    ctx.Set.Connect(bufferElementToPortNode, GetBufferElementValueNode.KernelPorts.Output, m_NMixerNode, NMixerNode.KernelPorts.Weights, i);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutIndices, i, bufferElementToPortNode, GetBufferElementValueNode.KernelPorts.Index);
                    ctx.EmitMessage(SimulationPorts.m_OutIndices,  i, i);
                }

                // Forward rig definition to all children.
                ctx.EmitMessage(SimulationPorts.RigOut, new Rig { Value = m_RigDefinition });
            }

            public void HandleMessage(MessageContext ctx, in Rig rig)
            {
                m_RigDefinition = rig;
                ctx.EmitMessage(SimulationPorts.RigOut, rig);
            }
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "e1d595a425104c32ab36d5a6f51bb53b", description: "Normalized time")]
            public DataInput<BlendTree1DNode, float> NormalizedTime;
            [PortDefinition(guid: "ec8393afd55d415e9d5768173435fbe3", displayName: "Blend", description: "Blend parameter value")]
            public DataInput<BlendTree1DNode, float> BlendParameter;

            [PortDefinition(guid: "75e048a572a44ff6a813a711b9560812", description: "Resulting animation stream")]
            public DataOutput<BlendTree1DNode, Buffer<AnimatedData>> Output;
            [PortDefinition(guid: "32674ad7efba46ca99072747923cad00", description: "Current motion duration, used to compute normalized time")]
            public DataOutput<BlendTree1DNode, float> Duration;
        }

        struct KernelData : IKernelData {}

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
            }
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.Rig;
    }
}
