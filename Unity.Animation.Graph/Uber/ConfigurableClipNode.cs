using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "792256a71ac546709adf975eed81325f", version: 1, category: "Animation Core", description: "Evaluates a clip based on the clip configuration mask", isHidden: true)]
    public class ConfigurableClipNode
        : SimulationKernelNodeDefinition<ConfigurableClipNode.SimPorts, ConfigurableClipNode.KernelDefs>
        , IRigContextHandler<ConfigurableClipNode.Data>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "5040b6577c8c43458d62bda53ec0cc6d", isHidden: true)]
            public MessageInput<ConfigurableClipNode, Rig> Rig;
            [PortDefinition(guid: "f2efaeb1e1684f218c329edf87ee8517", description: "Clip to sample")]
            public MessageInput<ConfigurableClipNode, BlobAssetReference<Clip>> Clip;
            [PortDefinition(guid: "2f01ade105d14c2ea42217b644bcea25", description: "Clip configuration data")]
            public MessageInput<ConfigurableClipNode, ClipConfiguration> Configuration;
            [PortDefinition(guid: "97417cbc0ad5408bba9a9a1a10603ca0", description: "Is this an additive clip", defaultValue: false)]
            public MessageInput<ConfigurableClipNode, bool> Additive;

            internal MessageOutput<ConfigurableClipNode, Rig> m_OutRig;
            internal MessageOutput<ConfigurableClipNode, BlobAssetReference<Clip>> m_OutClip;
            internal MessageOutput<ConfigurableClipNode, ClipConfiguration> m_OutClipConfig;
            internal MessageOutput<ConfigurableClipNode, bool> m_OutIsAdditive;
            internal MessageOutput<ConfigurableClipNode, int> m_OutBufferSize;
            internal MessageOutput<ConfigurableClipNode, float> m_OutRootWeightMultiplier;
            internal MessageOutput<ConfigurableClipNode, float> m_OutStartClipTime;
            internal MessageOutput<ConfigurableClipNode, float> m_OutStopClipTime;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "6da950f7ac294fc1a867e8a3923d5345", description: "Unbound time")]
            public DataInput<ConfigurableClipNode, float> Time;

            [PortDefinition(guid: "95f138d338bd4db1a0717e28f667df46", description: "Resulting animation stream")]
            public DataOutput<ConfigurableClipNode, Buffer<AnimatedData>> Output;
        }

        internal struct Data : INodeData, IInit, IDestroy
            , IMsgHandler<Rig>
            , IMsgHandler<BlobAssetReference<Clip>>
            , IMsgHandler<ClipConfiguration>
            , IMsgHandler<bool>
        {
            NodeHandle<KernelPassThroughNodeFloat> m_TimeNode;
            NodeHandle<KernelPassThroughNodeBufferFloat> m_OutputNode;

            internal NodeHandle<ClipNode> m_ClipNode;
            NodeHandle<ClipNode>          m_StartClipNode;
            NodeHandle<ClipNode>          m_StopClipNode;

            NodeHandle<InPlaceMotionNode> m_InPlaceNode;
            NodeHandle<InPlaceMotionNode> m_StartInPlaceNode;
            NodeHandle<InPlaceMotionNode> m_StopInPlaceNode;

            NodeHandle<NormalizedTimeNode> m_NormalizedTimeNode;
            NodeHandle<TimeLoopNode> m_LoopTimeNode;
            NodeHandle<DeltaPoseNode> m_DeltaNode;
            NodeHandle<LoopNode> m_LoopNode;

            NodeHandle<CycleRootMotionNode> m_CycleRootMotionNode;

            BlobAssetReference<RigDefinition>    m_RigDefinition;
            BlobAssetReference<Clip>             m_Clip;
            bool                                 m_IsAdditive;

            ClipConfiguration m_Configuration;

            void BuildNodes(MessageContext ctx)
            {
                if (m_Clip == BlobAssetReference<Clip>.Null || m_RigDefinition == BlobAssetReference<RigDefinition>.Null)
                    return;

                var thisHandle = ctx.Set.CastHandle<ConfigurableClipNode>(ctx.Handle);

                var normalizedTime = (m_Configuration.Mask & ClipConfigurationMask.NormalizedTime) != 0;
                var loopTime = (m_Configuration.Mask & (ClipConfigurationMask.LoopTime | ClipConfigurationMask.LoopValues | ClipConfigurationMask.CycleRootMotion)) != 0;
                var loopTransform = (m_Configuration.Mask & (ClipConfigurationMask.LoopValues)) != 0;
                var startStopClip = (m_Configuration.Mask & (ClipConfigurationMask.LoopValues | ClipConfigurationMask.CycleRootMotion)) != 0;
                var inPlace = m_Configuration.MotionID != 0;
                var cycleRoot = (m_Configuration.Mask & (ClipConfigurationMask.CycleRootMotion)) != 0;

                // create
                m_ClipNode = ctx.Set.Create<ClipNode>();

                ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig, m_ClipNode, ClipNode.SimulationPorts.Rig);
                ctx.Set.Connect(thisHandle, SimulationPorts.m_OutClip, m_ClipNode, ClipNode.SimulationPorts.Clip);
                ctx.Set.Connect(thisHandle, SimulationPorts.m_OutIsAdditive, m_ClipNode, ClipNode.SimulationPorts.Additive);

                if (startStopClip)
                {
                    m_StartClipNode = ctx.Set.Create<ClipNode>();
                    m_StopClipNode = ctx.Set.Create<ClipNode>();

                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig, m_StartClipNode, ClipNode.SimulationPorts.Rig);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutClip, m_StartClipNode, ClipNode.SimulationPorts.Clip);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutIsAdditive, m_StartClipNode, ClipNode.SimulationPorts.Additive);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutStartClipTime, m_StartClipNode, ClipNode.KernelPorts.Time);

                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig, m_StopClipNode, ClipNode.SimulationPorts.Rig);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutClip, m_StopClipNode, ClipNode.SimulationPorts.Clip);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutIsAdditive, m_StopClipNode, ClipNode.SimulationPorts.Additive);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutStopClipTime, m_StopClipNode, ClipNode.KernelPorts.Time);
                }

                if (normalizedTime)
                {
                    m_NormalizedTimeNode = ctx.Set.Create<NormalizedTimeNode>();
                }

                if (loopTime)
                {
                    m_LoopTimeNode = ctx.Set.Create<TimeLoopNode>();
                }

                if (loopTransform)
                {
                    m_DeltaNode = ctx.Set.Create<DeltaPoseNode>();
                    m_LoopNode = ctx.Set.Create<LoopNode>();

                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig , m_DeltaNode, DeltaPoseNode.SimulationPorts.Rig);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig, m_LoopNode, LoopNode.SimulationPorts.Rig);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRootWeightMultiplier, m_LoopNode, LoopNode.KernelPorts.RootWeightMultiplier);
                }

                if (inPlace)
                {
                    m_InPlaceNode = ctx.Set.Create<InPlaceMotionNode>();

                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig , m_InPlaceNode, InPlaceMotionNode.SimulationPorts.Rig);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutClipConfig, m_InPlaceNode, InPlaceMotionNode.SimulationPorts.Configuration);

                    if (startStopClip)
                    {
                        m_StartInPlaceNode = ctx.Set.Create<InPlaceMotionNode>();
                        m_StopInPlaceNode = ctx.Set.Create<InPlaceMotionNode>();

                        ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig , m_StartInPlaceNode, InPlaceMotionNode.SimulationPorts.Rig);
                        ctx.Set.Connect(thisHandle, SimulationPorts.m_OutClipConfig, m_StartInPlaceNode, InPlaceMotionNode.SimulationPorts.Configuration);

                        ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig , m_StopInPlaceNode, InPlaceMotionNode.SimulationPorts.Rig);
                        ctx.Set.Connect(thisHandle, SimulationPorts.m_OutClipConfig, m_StopInPlaceNode, InPlaceMotionNode.SimulationPorts.Configuration);
                    }
                }

                if (cycleRoot)
                {
                    m_CycleRootMotionNode = ctx.Set.Create<CycleRootMotionNode>();

                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig , m_CycleRootMotionNode, CycleRootMotionNode.SimulationPorts.Rig);
                }

                // connect kernel ports
                if (normalizedTime)
                {
                    ctx.Set.Connect(m_TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_NormalizedTimeNode, NormalizedTimeNode.KernelPorts.InputTime);

                    if (loopTime)
                    {
                        ctx.Set.Connect(m_NormalizedTimeNode, NormalizedTimeNode.KernelPorts.OutputTime, m_LoopTimeNode, TimeLoopNode.KernelPorts.InputTime);
                        ctx.Set.Connect(m_LoopTimeNode, TimeLoopNode.KernelPorts.OutputTime, m_ClipNode, ClipNode.KernelPorts.Time);
                    }
                    else
                    {
                        ctx.Set.Connect(m_NormalizedTimeNode, NormalizedTimeNode.KernelPorts.OutputTime, m_ClipNode, ClipNode.KernelPorts.Time);
                    }
                }
                else
                {
                    if (loopTime)
                    {
                        ctx.Set.Connect(m_TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_LoopTimeNode, TimeLoopNode.KernelPorts.InputTime);
                        ctx.Set.Connect(m_LoopTimeNode, TimeLoopNode.KernelPorts.OutputTime, m_ClipNode, ClipNode.KernelPorts.Time);
                    }
                    else
                    {
                        ctx.Set.Connect(m_TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_ClipNode, ClipNode.KernelPorts.Time);
                    }
                }

                if (inPlace)
                {
                    if (startStopClip)
                    {
                        ctx.Set.Connect(m_StartClipNode, ClipNode.KernelPorts.Output, m_StartInPlaceNode, InPlaceMotionNode.KernelPorts.Input);
                        ctx.Set.Connect(m_StopClipNode, ClipNode.KernelPorts.Output, m_StopInPlaceNode, InPlaceMotionNode.KernelPorts.Input);
                    }

                    ctx.Set.Connect(m_ClipNode, ClipNode.KernelPorts.Output, m_InPlaceNode, InPlaceMotionNode.KernelPorts.Input);
                }

                if (loopTransform)
                {
                    if (inPlace)
                    {
                        ctx.Set.Connect(m_StartInPlaceNode, InPlaceMotionNode.KernelPorts.Output, m_DeltaNode, DeltaPoseNode.KernelPorts.Subtract);
                        ctx.Set.Connect(m_StopInPlaceNode, InPlaceMotionNode.KernelPorts.Output, m_DeltaNode, DeltaPoseNode.KernelPorts.Input);
                        ctx.Set.Connect(m_InPlaceNode, InPlaceMotionNode.KernelPorts.Output, m_LoopNode, LoopNode.KernelPorts.Input);
                    }
                    else
                    {
                        ctx.Set.Connect(m_StartClipNode, ClipNode.KernelPorts.Output, m_DeltaNode, DeltaPoseNode.KernelPorts.Subtract);
                        ctx.Set.Connect(m_StopClipNode, ClipNode.KernelPorts.Output, m_DeltaNode, DeltaPoseNode.KernelPorts.Input);
                        ctx.Set.Connect(m_ClipNode, ClipNode.KernelPorts.Output, m_LoopNode, LoopNode.KernelPorts.Input);
                    }

                    ctx.Set.Connect(m_DeltaNode, DeltaPoseNode.KernelPorts.Output, m_LoopNode, LoopNode.KernelPorts.Delta);
                    ctx.Set.Connect(m_LoopTimeNode, TimeLoopNode.KernelPorts.NormalizedTime, m_LoopNode, LoopNode.KernelPorts.NormalizedTime);

                    if (cycleRoot)
                    {
                        ctx.Set.Connect(m_LoopNode, LoopNode.KernelPorts.Output, m_CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Input);
                    }
                    else
                    {
                        ctx.Set.Connect(m_LoopNode, LoopNode.KernelPorts.Output, m_OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);
                    }
                }
                else
                {
                    if (inPlace)
                    {
                        if (cycleRoot)
                        {
                            ctx.Set.Connect(m_InPlaceNode, InPlaceMotionNode.KernelPorts.Output, m_CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Input);
                        }
                        else
                        {
                            ctx.Set.Connect(m_InPlaceNode, InPlaceMotionNode.KernelPorts.Output, m_OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);
                        }
                    }
                    else
                    {
                        if (cycleRoot)
                        {
                            ctx.Set.Connect(m_ClipNode, ClipNode.KernelPorts.Output, m_CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Input);
                        }
                        else
                        {
                            ctx.Set.Connect(m_ClipNode, ClipNode.KernelPorts.Output, m_OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);
                        }
                    }
                }

                if (cycleRoot)
                {
                    ctx.Set.Connect(m_LoopTimeNode, TimeLoopNode.KernelPorts.Cycle, m_CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Cycle);
                    ctx.Set.Connect(m_CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Output, m_OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);

                    if (inPlace)
                    {
                        ctx.Set.Connect(m_StartInPlaceNode, InPlaceMotionNode.KernelPorts.Output, m_CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Start);
                        ctx.Set.Connect(m_StopInPlaceNode, InPlaceMotionNode.KernelPorts.Output, m_CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Stop);
                    }
                    else
                    {
                        ctx.Set.Connect(m_StartClipNode, ClipNode.KernelPorts.Output, m_CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Start);
                        ctx.Set.Connect(m_StopClipNode, ClipNode.KernelPorts.Output, m_CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Stop);
                    }
                }

                // connect sim ports
                if (normalizedTime)
                {
                    ctx.Set.Connect(m_ClipNode, ClipNode.SimulationPorts.Duration, m_NormalizedTimeNode, NormalizedTimeNode.SimulationPorts.Duration);
                }

                if (loopTime)
                {
                    ctx.Set.Connect(m_ClipNode, ClipNode.SimulationPorts.Duration, m_LoopTimeNode, TimeLoopNode.SimulationPorts.Duration);
                }

                // send messages
                ctx.EmitMessage(SimulationPorts.m_OutRig, new Rig { Value = m_RigDefinition });
                ctx.EmitMessage(SimulationPorts.m_OutClip, m_Clip);
                ctx.EmitMessage(SimulationPorts.m_OutClipConfig, m_Configuration);
                ctx.EmitMessage(SimulationPorts.m_OutIsAdditive, m_IsAdditive);
                ctx.EmitMessage(SimulationPorts.m_OutBufferSize,  m_RigDefinition.Value.Bindings.StreamSize);
                ctx.EmitMessage(SimulationPorts.m_OutRootWeightMultiplier, inPlace ? 0 : 1);

                ctx.EmitMessage(SimulationPorts.m_OutStartClipTime, 0);
                ctx.EmitMessage(SimulationPorts.m_OutStopClipTime, m_Clip.Value.Duration);
            }

            void ClearNodes(NodeSetAPI set)
            {
                if (set.Exists(m_ClipNode))
                    set.Destroy(m_ClipNode);

                if (set.Exists(m_StartClipNode))
                    set.Destroy(m_StartClipNode);

                if (set.Exists(m_StopClipNode))
                    set.Destroy(m_StopClipNode);

                if (set.Exists(m_NormalizedTimeNode))
                    set.Destroy(m_NormalizedTimeNode);

                if (set.Exists(m_LoopTimeNode))
                    set.Destroy(m_LoopTimeNode);

                if (set.Exists(m_DeltaNode))
                    set.Destroy(m_DeltaNode);

                if (set.Exists(m_LoopNode))
                    set.Destroy(m_LoopNode);

                if (set.Exists(m_InPlaceNode))
                    set.Destroy(m_InPlaceNode);

                if (set.Exists(m_StartInPlaceNode))
                    set.Destroy(m_StartInPlaceNode);

                if (set.Exists(m_StopInPlaceNode))
                    set.Destroy(m_StopInPlaceNode);

                if (set.Exists(m_CycleRootMotionNode))
                    set.Destroy(m_CycleRootMotionNode);
            }

            public void Init(InitContext ctx)
            {
                var thisHandle = ctx.Set.CastHandle<ConfigurableClipNode>(ctx.Handle);

                m_TimeNode = ctx.Set.Create<KernelPassThroughNodeFloat>();
                m_OutputNode = ctx.Set.Create<KernelPassThroughNodeBufferFloat>();
                m_IsAdditive = false;

                ctx.ForwardInput(KernelPorts.Time, m_TimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
                ctx.ForwardOutput(KernelPorts.Output, m_OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Output);

                ctx.Set.Connect(thisHandle, SimulationPorts.m_OutBufferSize, m_OutputNode, KernelPassThroughNodeBufferFloat.SimulationPorts.BufferSize);
            }

            public void Destroy(DestroyContext ctx)
            {
                ctx.Set.Destroy(m_TimeNode);
                ctx.Set.Destroy(m_OutputNode);

                ClearNodes(ctx.Set);
            }

            public void HandleMessage(MessageContext ctx, in Rig rig)
            {
                m_RigDefinition = rig;

                ClearNodes(ctx.Set);
                ctx.Set.SetBufferSize(
                    ctx.Handle,
                    (OutputPortID)KernelPorts.Output,
                    Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0)
                );

                BuildNodes(ctx);
            }

            public void HandleMessage(MessageContext ctx, in BlobAssetReference<Clip> clip)
            {
                m_Clip = clip;

                ClearNodes(ctx.Set);
                BuildNodes(ctx);
            }

            public void HandleMessage(MessageContext ctx, in ClipConfiguration msg)
            {
                m_Configuration = msg;

                ClearNodes(ctx.Set);
                BuildNodes(ctx);
            }

            public void HandleMessage(MessageContext ctx, in bool msg)
            {
                m_IsAdditive = msg;

                ClearNodes(ctx.Set);
                BuildNodes(ctx);
            }
        }

        struct KernelData : IKernelData {}

        [BurstCompile]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
            }
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.Rig;
    }
}
