using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
   [NodeDefinition(category:"Animation Core", description:"Evaluates a clip based on the clip configuration mask", isHidden:true)]
   public class ConfigurableClipNode
        : NodeDefinition<ConfigurableClipNode.Data, ConfigurableClipNode.SimPorts, ConfigurableClipNode.KernelData, ConfigurableClipNode.KernelDefs, ConfigurableClipNode.Kernel>
        , IMsgHandler<Rig>
        , IMsgHandler<BlobAssetReference<Clip>>
        , IMsgHandler<ClipConfiguration>
        , IMsgHandler<bool>
        , IRigContextHandler
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<ConfigurableClipNode, Rig> Rig;
            [PortDefinition(description:"Clip to sample")]
            public MessageInput<ConfigurableClipNode, BlobAssetReference<Clip>> Clip;
            [PortDefinition(description:"Clip configuration data")]
            public MessageInput<ConfigurableClipNode, ClipConfiguration> Configuration;
            [PortDefinition(description:"Is this an additive clip", defaultValue:false)]
            public MessageInput<ConfigurableClipNode, bool> Additive;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Unbound time")]
            public DataInput<ConfigurableClipNode, float> Time;

            [PortDefinition(description:"Resulting animation stream")]
            public DataOutput<ConfigurableClipNode, Buffer<AnimatedData>> Output;
        }

        public struct Data : INodeData
        {
            internal NodeHandle<KernelPassThroughNodeFloat> TimeNode;
            internal NodeHandle<KernelPassThroughNodeBufferFloat> OutputNode;

            internal NodeHandle<ClipNode> ClipNode;
            internal NodeHandle<ClipNode> StartClipNode;
            internal NodeHandle<ClipNode> StopClipNode;

            internal NodeHandle<InPlaceMotionNode> InPlaceNode;
            internal NodeHandle<InPlaceMotionNode> StartInPlaceNode;
            internal NodeHandle<InPlaceMotionNode> StopInPlaceNode;

            internal NodeHandle<NormalizedTimeNode> NormalizedTimeNode;
            internal NodeHandle<TimeLoopNode> LoopTimeNode;
            internal NodeHandle<DeltaPoseNode> DeltaNode;
            internal NodeHandle<LoopNode> LoopNode;

            internal NodeHandle<CycleRootMotionNode> CycleRootMotionNode;

            public BlobAssetReference<RigDefinition>    RigDefinition;
            public BlobAssetReference<Clip>             Clip;
            public bool                                 IsAdditive;

            internal ClipConfiguration Configuration;
        }

        public struct KernelData : IKernelData { }

        [BurstCompile]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
            }
        }

        static bool NeedsNormalizedTime(in Data nodeData) =>
            (nodeData.Configuration.Mask & ClipConfigurationMask.NormalizedTime) != 0;

        static bool NeedsLoopTime(in Data nodeData) =>
            (nodeData.Configuration.Mask & (ClipConfigurationMask.LoopTime | ClipConfigurationMask.LoopValues | ClipConfigurationMask.CycleRootMotion)) != 0;

        static bool NeedsLoopTransform(in Data nodeData) =>
            (nodeData.Configuration.Mask & (ClipConfigurationMask.LoopValues)) != 0;

        static bool NeedsStartStopClip(in Data nodeData) =>
            (nodeData.Configuration.Mask & (ClipConfigurationMask.LoopValues | ClipConfigurationMask.CycleRootMotion)) != 0;

        static bool NeedsCycleRootMotion(in Data nodeData) =>
            (nodeData.Configuration.Mask & (ClipConfigurationMask.CycleRootMotion)) != 0;

        static bool NeedsInPlace(in Data nodeData) =>
            nodeData.Configuration.MotionID != 0;

        void BuildNodes(ref Data data)
        {
            if (data.Clip == BlobAssetReference<Clip>.Null || data.RigDefinition == BlobAssetReference<RigDefinition>.Null)
                return;

            var normalizedTime = NeedsNormalizedTime(data);
            var loopTime = NeedsLoopTime(data);
            var loopTransform = NeedsLoopTransform(data);
            var startStopClip = NeedsStartStopClip(data);
            var inPlace = NeedsInPlace(data);
            var cycleRoot = NeedsCycleRootMotion(data);

            // create
            data.ClipNode = Set.Create<ClipNode>();

            if (startStopClip)
            {
                data.StartClipNode = Set.Create<ClipNode>();
                data.StopClipNode = Set.Create<ClipNode>();
            }

            if (normalizedTime)
            {
                data.NormalizedTimeNode = Set.Create<NormalizedTimeNode>();
            }

            if (loopTime)
            {
                data.LoopTimeNode = Set.Create<TimeLoopNode>();
            }

            if (loopTransform)
            {
                data.DeltaNode = Set.Create<DeltaPoseNode>();
                data.LoopNode = Set.Create<LoopNode>();
            }

            if (inPlace)
            {
                data.InPlaceNode = Set.Create<InPlaceMotionNode>();

                if (startStopClip)
                {
                    data.StartInPlaceNode = Set.Create<InPlaceMotionNode>();
                    data.StopInPlaceNode = Set.Create<InPlaceMotionNode>();
                }
            }

            if (cycleRoot)
            {
                data.CycleRootMotionNode = Set.Create<CycleRootMotionNode>();
            }

            // connect kernel ports
            if (normalizedTime)
            {
                Set.Connect(data.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, data.NormalizedTimeNode, NormalizedTimeNode.KernelPorts.InputTime);

                if (loopTime)
                {
                    Set.Connect(data.NormalizedTimeNode, NormalizedTimeNode.KernelPorts.OutputTime, data.LoopTimeNode, TimeLoopNode.KernelPorts.InputTime);
                    Set.Connect(data.LoopTimeNode, TimeLoopNode.KernelPorts.OutputTime, data.ClipNode, ClipNode.KernelPorts.Time);
                }
                else
                {
                    Set.Connect(data.NormalizedTimeNode, NormalizedTimeNode.KernelPorts.OutputTime, data.ClipNode, ClipNode.KernelPorts.Time);
                }
            }
            else
            {
                if (loopTime)
                {
                    Set.Connect(data.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, data.LoopTimeNode, TimeLoopNode.KernelPorts.InputTime);
                    Set.Connect(data.LoopTimeNode, TimeLoopNode.KernelPorts.OutputTime, data.ClipNode, ClipNode.KernelPorts.Time);
                }
                else
                {
                    Set.Connect(data.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, data.ClipNode, ClipNode.KernelPorts.Time);
                }
            }

            if (inPlace)
            {
                if (startStopClip)
                {
                    Set.Connect(data.StartClipNode, ClipNode.KernelPorts.Output, data.StartInPlaceNode, InPlaceMotionNode.KernelPorts.Input);
                    Set.Connect(data.StopClipNode, ClipNode.KernelPorts.Output, data.StopInPlaceNode, InPlaceMotionNode.KernelPorts.Input);
                }

                Set.Connect(data.ClipNode, ClipNode.KernelPorts.Output, data.InPlaceNode, InPlaceMotionNode.KernelPorts.Input);
            }

            if (loopTransform)
            {
                if (inPlace)
                {
                    Set.Connect(data.StartInPlaceNode, InPlaceMotionNode.KernelPorts.Output, data.DeltaNode, DeltaPoseNode.KernelPorts.Subtract);
                    Set.Connect(data.StopInPlaceNode, InPlaceMotionNode.KernelPorts.Output, data.DeltaNode, DeltaPoseNode.KernelPorts.Input);
                    Set.Connect(data.InPlaceNode, InPlaceMotionNode.KernelPorts.Output, data.LoopNode, LoopNode.KernelPorts.Input);
                }
                else
                {
                    Set.Connect(data.StartClipNode, ClipNode.KernelPorts.Output, data.DeltaNode, DeltaPoseNode.KernelPorts.Subtract);
                    Set.Connect(data.StopClipNode, ClipNode.KernelPorts.Output, data.DeltaNode, DeltaPoseNode.KernelPorts.Input);
                    Set.Connect(data.ClipNode, ClipNode.KernelPorts.Output, data.LoopNode, LoopNode.KernelPorts.Input);
                }

                Set.Connect(data.DeltaNode, DeltaPoseNode.KernelPorts.Output, data.LoopNode, LoopNode.KernelPorts.Delta);
                Set.Connect(data.LoopTimeNode, TimeLoopNode.KernelPorts.NormalizedTime, data.LoopNode, LoopNode.KernelPorts.NormalizedTime);

                if (cycleRoot)
                {
                    Set.Connect(data.LoopNode, LoopNode.KernelPorts.Output, data.CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Input);
                }
                else
                {
                    Set.Connect(data.LoopNode, LoopNode.KernelPorts.Output, data.OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);
                }
            }
            else
            {
                if (inPlace)
                {
                    if (cycleRoot)
                    {
                        Set.Connect(data.InPlaceNode, InPlaceMotionNode.KernelPorts.Output, data.CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Input);
                    }
                    else
                    {
                        Set.Connect(data.InPlaceNode, InPlaceMotionNode.KernelPorts.Output, data.OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);
                    }
                }
                else
                {
                    if (cycleRoot)
                    {
                        Set.Connect(data.ClipNode, ClipNode.KernelPorts.Output, data.CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Input);
                    }
                    else
                    {
                        Set.Connect(data.ClipNode, ClipNode.KernelPorts.Output, data.OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);
                    }
                }
            }

            if (cycleRoot)
            {
                Set.Connect(data.LoopTimeNode, TimeLoopNode.KernelPorts.Cycle, data.CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Cycle);
                Set.Connect(data.CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Output, data.OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);

                if (inPlace)
                {
                    Set.Connect(data.StartInPlaceNode, InPlaceMotionNode.KernelPorts.Output, data.CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Start);
                    Set.Connect(data.StopInPlaceNode, InPlaceMotionNode.KernelPorts.Output, data.CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Stop);
                }
                else
                {
                    Set.Connect(data.StartClipNode, ClipNode.KernelPorts.Output, data.CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Start);
                    Set.Connect(data.StopClipNode, ClipNode.KernelPorts.Output, data.CycleRootMotionNode, CycleRootMotionNode.KernelPorts.Stop);
                }
            }

            // connect sim ports
            if (normalizedTime)
            {
                Set.Connect(data.ClipNode, ClipNode.SimulationPorts.Duration, data.NormalizedTimeNode, NormalizedTimeNode.SimulationPorts.Duration);
            }

            if (loopTime)
            {
                Set.Connect(data.ClipNode, ClipNode.SimulationPorts.Duration, data.LoopTimeNode, TimeLoopNode.SimulationPorts.Duration);
            }

            // send messages
            Set.SendMessage(data.ClipNode, ClipNode.SimulationPorts.Rig, new Rig { Value = data.RigDefinition });
            Set.SendMessage(data.ClipNode, ClipNode.SimulationPorts.Clip, data.Clip);
            Set.SendMessage(data.ClipNode, ClipNode.SimulationPorts.Additive, data.IsAdditive);
            Set.SendMessage(data.OutputNode, KernelPassThroughNodeBufferFloat.SimulationPorts.BufferSize,  data.RigDefinition.Value.Bindings.StreamSize);

            if (loopTransform)
            {
                Set.SendMessage(data.DeltaNode, DeltaPoseNode.SimulationPorts.Rig, new Rig { Value = data.RigDefinition });
                Set.SendMessage(data.LoopNode, LoopNode.SimulationPorts.Rig, new Rig { Value = data.RigDefinition });

                Set.SetData(data.LoopNode, LoopNode.KernelPorts.RootWeightMultiplier, inPlace ? 0 : 1);
            }

            if (startStopClip)
            {
                Set.SendMessage(data.StartClipNode, ClipNode.SimulationPorts.Rig, new Rig { Value = data.RigDefinition });
                Set.SendMessage(data.StartClipNode, ClipNode.SimulationPorts.Clip, data.Clip);
                Set.SendMessage(data.StartClipNode, ClipNode.SimulationPorts.Additive, data.IsAdditive);
                Set.SendMessage(data.StopClipNode, ClipNode.SimulationPorts.Rig, new Rig { Value = data.RigDefinition });
                Set.SendMessage(data.StopClipNode, ClipNode.SimulationPorts.Clip, data.Clip);
                Set.SendMessage(data.StopClipNode, ClipNode.SimulationPorts.Additive, data.IsAdditive);

                Set.SetData(data.StartClipNode, ClipNode.KernelPorts.Time, 0);
                Set.SetData(data.StopClipNode, ClipNode.KernelPorts.Time, data.Clip.Value.Duration);
            }

            if (inPlace)
            {
                Set.SendMessage(data.InPlaceNode, InPlaceMotionNode.SimulationPorts.Rig, new Rig { Value = data.RigDefinition });
                Set.SendMessage(data.InPlaceNode, InPlaceMotionNode.SimulationPorts.Configuration,  data.Configuration);

                if (startStopClip)
                {
                    Set.SendMessage(data.StartInPlaceNode, InPlaceMotionNode.SimulationPorts.Rig, new Rig { Value = data.RigDefinition });
                    Set.SendMessage(data.StopInPlaceNode, InPlaceMotionNode.SimulationPorts.Rig, new Rig { Value = data.RigDefinition });

                    Set.SendMessage(data.StartInPlaceNode, InPlaceMotionNode.SimulationPorts.Configuration, data.Configuration);
                    Set.SendMessage(data.StopInPlaceNode, InPlaceMotionNode.SimulationPorts.Configuration, data.Configuration);
                }
            }

            if (cycleRoot)
            {
                Set.SendMessage(data.CycleRootMotionNode, CycleRootMotionNode.SimulationPorts.Rig, new Rig { Value = data.RigDefinition });
            }
        }

        void ClearNodes(Data data)
        {
            if (Set.Exists(data.ClipNode))
                Set.Destroy(data.ClipNode);

            if (Set.Exists(data.StartClipNode))
                Set.Destroy(data.StartClipNode);

            if (Set.Exists(data.StopClipNode))
                Set.Destroy(data.StopClipNode);

            if(Set.Exists(data.NormalizedTimeNode))
                Set.Destroy(data.NormalizedTimeNode);

            if(Set.Exists(data.LoopTimeNode))
                Set.Destroy(data.LoopTimeNode);

            if(Set.Exists(data.DeltaNode))
                Set.Destroy(data.DeltaNode);

            if(Set.Exists(data.LoopNode))
                Set.Destroy(data.LoopNode);

            if(Set.Exists(data.InPlaceNode))
                Set.Destroy(data.InPlaceNode);

            if(Set.Exists(data.StartInPlaceNode))
                Set.Destroy(data.StartInPlaceNode);

            if(Set.Exists(data.StopInPlaceNode))
                Set.Destroy(data.StopInPlaceNode);

            if(Set.Exists(data.CycleRootMotionNode))
                Set.Destroy(data.CycleRootMotionNode);
        }

        protected override void Init(InitContext ctx)
        {
            ref var data = ref GetNodeData(ctx.Handle);

            data.TimeNode = Set.Create<KernelPassThroughNodeFloat>();
            data.OutputNode = Set.Create<KernelPassThroughNodeBufferFloat>();
            data.IsAdditive = false;

            BuildNodes(ref data);

            ctx.ForwardInput(KernelPorts.Time, data.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
            ctx.ForwardOutput(KernelPorts.Output, data.OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Output);
        }

        protected override void Destroy(NodeHandle handle)
        {
            ref var nodeData = ref GetNodeData(handle);

            Set.Destroy(nodeData.TimeNode);
            Set.Destroy(nodeData.OutputNode);

            ClearNodes(nodeData);
        }

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);
            nodeData.RigDefinition = rig;

            ClearNodes(nodeData);
            Set.SetBufferSize(
                ctx.Handle,
                (OutputPortID)KernelPorts.Output,
                Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0)
                );

            BuildNodes(ref nodeData);
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<Clip> clip)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);
            nodeData.Clip = clip;

            ClearNodes(nodeData);
            BuildNodes(ref nodeData);
        }

        public void HandleMessage(in MessageContext ctx, in ClipConfiguration msg)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);

            nodeData.Configuration = msg;

            ClearNodes(nodeData);
            BuildNodes(ref nodeData);
        }

        public void HandleMessage(in MessageContext ctx, in bool msg)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);
            nodeData.IsAdditive = msg;

            ClearNodes(nodeData);
            BuildNodes(ref nodeData);
        }

        internal KernelData ExposeKernelData(NodeHandle handle) => GetKernelData(handle);
        internal Data ExposeNodeData(NodeHandle handle) => GetNodeData(handle);

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
