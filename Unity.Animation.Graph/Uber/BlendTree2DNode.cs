using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "dd965a2ebf994f77ad87f02fce11ce9a", version: 1, category: "Animation Core/Blend Trees", description: "Evaluates a 2D BlendTree based on X and Y blend parameters")]
    public class BlendTree2DNode
        : SimulationKernelNodeDefinition<BlendTree2DNode.SimPorts, BlendTree2DNode.KernelDefs>
        , IRigContextHandler<BlendTree2DNode.Data>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "28cb77b3c65c49fabe33810d9665efa5", description: "BlendTree data")]
            public MessageInput<BlendTree2DNode, BlobAssetReference<BlendTree2DSimpleDirectional>> BlendTree;
            [PortDefinition(guid: "2efe136265d74ee6bf17610df0d2fcbe", isHidden: true)]
            public MessageInput<BlendTree2DNode, Rig> Rig;

            [PortDefinition(guid: "e0f25365515b4009bcf8f99e8208bc68", isHidden: true)]
            public MessageOutput<BlendTree2DNode, Rig> RigOut;

            // For internal messages in node data.
            internal MessageOutput<BlendTree2DNode, BlobAssetReference<BlendTree2DSimpleDirectional>> m_OutBlendTree;
            internal PortArray<MessageOutput<BlendTree2DNode, ClipConfiguration>> m_OutClipConfigs;
            internal PortArray<MessageOutput<BlendTree2DNode, BlobAssetReference<Clip>>> m_OutClips;
            internal PortArray<MessageOutput<BlendTree2DNode, float>> m_OutMotionDurations;
            internal PortArray<MessageOutput<BlendTree2DNode, int>> m_OutIndices;
        }

        [Managed]
        internal struct Data : INodeData, IInit, IDestroy
            , IMsgHandler<Rig>, IMsgHandler<BlobAssetReference<BlendTree2DSimpleDirectional>>
        {
            // Assets.
            internal BlobAssetReference<RigDefinition>       m_RigDefinition;
            BlobAssetReference<BlendTree2DSimpleDirectional> m_BlendTree;

            NodeHandle<KernelPassThroughNodeFloat> m_NormalizedTimeNode;
            NodeHandle<ComputeBlendTree2DWeightsNode> m_ComputeBlendTree2DWeightsNode;

            NodeHandle<NMixerNode> m_NMixerNode;

            internal List<NodeHandle<UberClipNode>> m_Motions;
            List<NodeHandle<GetBufferElementValueNode>> m_MotionDurationNodes;

            public void Init(InitContext ctx)
            {
                var thisHandle = ctx.Set.CastHandle<BlendTree2DNode>(ctx.Handle);

                m_NormalizedTimeNode = ctx.Set.Create<KernelPassThroughNodeFloat>();
                m_NMixerNode = ctx.Set.Create<NMixerNode>();
                m_ComputeBlendTree2DWeightsNode = ctx.Set.Create<ComputeBlendTree2DWeightsNode>();

                ctx.Set.Connect(thisHandle, SimulationPorts.RigOut, m_NMixerNode,
                    NMixerNode.SimulationPorts.Rig);
                ctx.Set.Connect(thisHandle, SimulationPorts.m_OutBlendTree, m_ComputeBlendTree2DWeightsNode,
                    ComputeBlendTree2DWeightsNode.SimulationPorts.BlendTree);

                ctx.ForwardInput(KernelPorts.NormalizedTime, m_NormalizedTimeNode,
                    KernelPassThroughNodeFloat.KernelPorts.Input);
                ctx.ForwardInput(KernelPorts.BlendParameterX, m_ComputeBlendTree2DWeightsNode,
                    ComputeBlendTree2DWeightsNode.KernelPorts.BlendParameterX);
                ctx.ForwardInput(KernelPorts.BlendParameterY, m_ComputeBlendTree2DWeightsNode,
                    ComputeBlendTree2DWeightsNode.KernelPorts.BlendParameterY);
                ctx.ForwardOutput(KernelPorts.Output, m_NMixerNode, NMixerNode.KernelPorts.Output);
                ctx.ForwardOutput(KernelPorts.Duration, m_ComputeBlendTree2DWeightsNode,
                    ComputeBlendTree2DWeightsNode.KernelPorts.Duration);

                m_Motions = new List<NodeHandle<UberClipNode>>();
                m_MotionDurationNodes = new List<NodeHandle<GetBufferElementValueNode>>();
            }

            public void Destroy(DestroyContext ctx)
            {
                ctx.Set.Destroy(m_NormalizedTimeNode);
                ctx.Set.Destroy(m_NMixerNode);
                ctx.Set.Destroy(m_ComputeBlendTree2DWeightsNode);

                for (int i = 0; i < m_Motions.Count; i++)
                    ctx.Set.Destroy(m_Motions[i]);

                for (int i = 0; i < m_MotionDurationNodes.Count; i++)
                    ctx.Set.Destroy(m_MotionDurationNodes[i]);
            }

            public void HandleMessage(MessageContext ctx,
                in BlobAssetReference<BlendTree2DSimpleDirectional> blendTree)
            {
                m_BlendTree = blendTree;

                var thisHandle = ctx.Set.CastHandle<BlendTree2DNode>(ctx.Handle);

                for (int i = 0; i < m_Motions.Count; i++)
                    ctx.Set.Destroy(m_Motions[i]);

                for (int i = 0; i < m_MotionDurationNodes.Count; i++)
                    ctx.Set.Destroy(m_MotionDurationNodes[i]);

                m_Motions.Clear();
                m_MotionDurationNodes.Clear();

                ctx.EmitMessage(SimulationPorts.m_OutBlendTree, blendTree);

                var length = m_BlendTree.Value.Motions.Length;

                ctx.Set.SetPortArraySize(thisHandle, SimulationPorts.m_OutClipConfigs, (ushort)length);
                ctx.Set.SetPortArraySize(thisHandle, SimulationPorts.m_OutClips, (ushort)length);
                ctx.Set.SetPortArraySize(thisHandle, SimulationPorts.m_OutMotionDurations, (ushort)length);
                ctx.Set.SetPortArraySize(thisHandle, SimulationPorts.m_OutIndices, (ushort)length);

                ctx.Set.SetPortArraySize(m_NMixerNode, NMixerNode.KernelPorts.Inputs, (ushort)length);
                ctx.Set.SetPortArraySize(m_NMixerNode, NMixerNode.KernelPorts.Weights, (ushort)length);
                ctx.Set.SetPortArraySize(m_ComputeBlendTree2DWeightsNode,
                    ComputeBlendTree2DWeightsNode.KernelPorts.MotionDurations, (ushort)length);

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
                    ctx.Set.Connect(m_NormalizedTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output,
                        motionNode, UberClipNode.KernelPorts.Time);
                    ctx.Set.Connect(motionNode, UberClipNode.KernelPorts.Output, m_NMixerNode,
                        NMixerNode.KernelPorts.Inputs, i);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutMotionDurations, i, m_ComputeBlendTree2DWeightsNode, ComputeBlendTree2DWeightsNode.KernelPorts.MotionDurations, i);
                    ctx.EmitMessage(SimulationPorts.m_OutMotionDurations,  i, clip.Value.Duration);

                    ctx.Set.Connect(m_ComputeBlendTree2DWeightsNode,
                        ComputeBlendTree2DWeightsNode.KernelPorts.Weights, bufferElementToPortNode,
                        GetBufferElementValueNode.KernelPorts.Input);
                    ctx.Set.Connect(bufferElementToPortNode, GetBufferElementValueNode.KernelPorts.Output,
                        m_NMixerNode, NMixerNode.KernelPorts.Weights, i);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutIndices, i, bufferElementToPortNode, GetBufferElementValueNode.KernelPorts.Index);
                    ctx.EmitMessage(SimulationPorts.m_OutIndices,  i, i);
                }

                // Forward rig definition to all children.
                ctx.EmitMessage(SimulationPorts.RigOut, new Rig {Value = m_RigDefinition});
            }

            public void HandleMessage(MessageContext ctx, in Rig rig)
            {
                m_RigDefinition = rig;
                ctx.EmitMessage(SimulationPorts.RigOut, new Rig {Value = m_RigDefinition});
            }
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "6d64e4d8dd1a4574ab065ce67c239a15", description: "Normalized time")]
            public DataInput<BlendTree2DNode, float> NormalizedTime;
            [PortDefinition(guid: "20a3ff3a4a99432783ec25b41830a83b", displayName: "Blend X", description: "Blend parameter X value")]
            public DataInput<BlendTree2DNode, float> BlendParameterX;
            [PortDefinition(guid: "ed4275b3425a431ea5e05c3a9e7dc65f", displayName: "Blend Y", description: "Blend parameter Y value")]
            public DataInput<BlendTree2DNode, float> BlendParameterY;

            [PortDefinition(guid: "8279a2f700f14b81bb2f4db467d2fb7a", description: "Resulting animation stream")]
            public DataOutput<BlendTree2DNode, Buffer<AnimatedData>> Output;
            [PortDefinition(guid: "8a3c6c2b60ee493ca8eed9f5488fb39e", description: "Current motion duration, used to compute normalized time")]
            public DataOutput<BlendTree2DNode, float> Duration;
        }

        struct KernelData : IKernelData
        {
        }

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
