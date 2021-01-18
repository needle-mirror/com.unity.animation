using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "999bc0582bad4a30a151256ac76555d5", version: 1, "Animation High-Level")]
    public class SimpleClipNode : SimulationKernelNodeDefinition<SimpleClipNode.MessagePorts, SimpleClipNode.DataPorts>,
        IRigContextHandler<SimpleClipNode.NodeData>
    {
        public struct MessagePorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "37389135eaca43c5ad9156d247802a23", isHidden: true)] public MessageInput<SimpleClipNode, Rig> Context;
            [PortDefinition(guid: "d0049832955b45e5a00485a3fec4844a", "Clip")] public MessageInput<SimpleClipNode, BlobAssetReference<Clip>> Clip;
            [PortDefinition(guid: "641db30afc65474586d512c9af02be83", "Delta Time")] public MessageInput<SimpleClipNode, float> DeltaTime;
            [PortDefinition(guid: "26e268729b5b4c7bb5203266e1467b11", "Timescale", defaultValue: 1.0F)] public MessageInput<SimpleClipNode, float> Timescale;
            [PortDefinition(guid: "8d3a831e399641cb9307aa05d1d79723", "Loop", defaultValue: true)] public MessageInput<SimpleClipNode, bool> Loop;
            [PortDefinition(guid: "a85ff9727dcd46688c9b9db62a46d7f7", "Root Motion Transform")] public MessageInput<SimpleClipNode, MotionID> RootTransform;

#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
            internal MessageOutput<SimpleClipNode, float> OutInternalDeltaTimeValue;
            internal MessageOutput<SimpleClipNode, float> OutInternalTimescaleValue;
            internal MessageOutput<SimpleClipNode, ClipConfiguration> OutInternalCreateClipConfiguration;
#pragma warning restore 649
        }

        public struct DataPorts : IKernelPortDefinition
        {
            [PortDefinition(guid: "1c2cf0d7b572400b85de31dfa9def7e9")]
            public DataOutput<SimpleClipNode, Buffer<AnimatedData>> Output;
        }

        private struct NodeData : INodeData, IInit, IDestroy,
                                  IMsgHandler<BlobAssetReference<Clip>>,
                                  IMsgHandler<float>,
                                  IMsgHandler<bool>,
                                  IMsgHandler<MotionID>,
                                  IMsgHandler<Rig>
        {
            public NodeHandle<Unity.Animation.ClipPlayerNode> ClipPlayerNode;

            public bool Loop;
            public uint MotionID;

            public void Init(InitContext ctx)
            {
                var thisHandle = ctx.Set.CastHandle<SimpleClipNode>(ctx.Handle);
                ClipPlayerNode = ctx.Set.Create<Unity.Animation.ClipPlayerNode>();

                ctx.Set.Connect(thisHandle, SimulationPorts.OutInternalDeltaTimeValue, ClipPlayerNode, Unity.Animation.ClipPlayerNode.KernelPorts.DeltaTime);
                ctx.Set.Connect(thisHandle, SimulationPorts.OutInternalTimescaleValue, ClipPlayerNode, Unity.Animation.ClipPlayerNode.KernelPorts.Speed);
                ctx.Set.Connect(thisHandle, SimulationPorts.OutInternalCreateClipConfiguration, ClipPlayerNode, Unity.Animation.ClipPlayerNode.SimulationPorts.Configuration);
                ctx.ForwardInput(SimulationPorts.Context, ClipPlayerNode, Unity.Animation.ClipPlayerNode.SimulationPorts.Rig);
                ctx.ForwardInput(SimulationPorts.Clip, ClipPlayerNode, Unity.Animation.ClipPlayerNode.SimulationPorts.Clip);
                ctx.ForwardOutput(KernelPorts.Output, ClipPlayerNode, Unity.Animation.ClipPlayerNode.KernelPorts.Output);
            }

            public void Destroy(DestroyContext ctx)
            {
                ctx.Set.Destroy(ClipPlayerNode);
            }

            public void HandleMessage(MessageContext ctx, in BlobAssetReference<Clip> msg)
            {
            }

            public void HandleMessage(MessageContext ctx, in float msg)
            {
                if (ctx.Port == SimpleClipNode.SimulationPorts.DeltaTime)
                {
                    ctx.EmitMessage(SimulationPorts.OutInternalDeltaTimeValue, msg);
                }
                else if (ctx.Port == SimpleClipNode.SimulationPorts.Timescale)
                {
                    ctx.EmitMessage(SimulationPorts.OutInternalTimescaleValue, msg);
                }
            }

            public void HandleMessage(MessageContext ctx, in bool msg)
            {
                if (ctx.Port == SimpleClipNode.SimulationPorts.Loop && Loop != msg)
                {
                    Loop = msg;
                    ctx.EmitMessage(SimulationPorts.OutInternalCreateClipConfiguration,  ComputeClipConfiguration());
                }
            }

            private ClipConfiguration ComputeClipConfiguration()
            {
                var clipConfig = new ClipConfiguration();
                clipConfig.Mask = 0;

                if (Loop)
                    clipConfig.Mask |= ClipConfigurationMask.LoopTime;
                if (MotionID != 0)
                    clipConfig.Mask |= ClipConfigurationMask.RootMotionFromVelocity | ClipConfigurationMask.CycleRootMotion | ClipConfigurationMask.DeltaRootMotion;

                StringHash id;
                id.Id = MotionID;
                clipConfig.MotionID = id;

                return clipConfig;
            }

            public void HandleMessage(MessageContext ctx, in Rig msg)
            {
            }

            public void HandleMessage(MessageContext ctx, in MotionID msg)
            {
                MotionID = msg.Value;
                ctx.EmitMessage(SimulationPorts.OutInternalCreateClipConfiguration,  ComputeClipConfiguration());
            }
        }

        public struct KernelData : IKernelData
        {
        }

        public struct Kernel : IGraphKernel<KernelData, DataPorts>
        {
            public void Execute(RenderContext ctx, in KernelData data, ref DataPorts ports)
            {
            }
        }

        public InputPortID GetPort(NodeHandle handle)
        {
            return (InputPortID)SimulationPorts.Context;
        }
    }
}
