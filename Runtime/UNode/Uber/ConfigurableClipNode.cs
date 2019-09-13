using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;

namespace Unity.Animation
{
   public class ConfigurableClipNode
        : NodeDefinition<ConfigurableClipNode.Data, ConfigurableClipNode.SimPorts, ConfigurableClipNode.KernelData, ConfigurableClipNode.KernelDefs, ConfigurableClipNode.Kernel>
            , IMsgHandler<BlobAssetReference<ClipInstance>>
            , IMsgHandler<ClipConfiguration>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<ConfigurableClipNode, BlobAssetReference<ClipInstance>> ClipInstance;
            public MessageInput<ConfigurableClipNode, ClipConfiguration> Configuration;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<ConfigurableClipNode, float> Time;
            public DataOutput<ConfigurableClipNode, Buffer<float>> Output;
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
            internal NodeHandle<DeltaNode> DeltaNode;
            internal NodeHandle<LoopNode> LoopNode;

            internal NodeHandle<CycleRootMotionNode> CycleRootMotionNode;

            public BlobAssetReference<ClipInstance> ClipInstance;
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

        static bool NeedsNormalizedTime(Data nodeData)
        {
            return (nodeData.Configuration.Mask & (int)ClipConfigurationMask.NormalizedTime) != 0;
        }

        static bool NeedsLoopTime(Data nodeData)
        {
            return (nodeData.Configuration.Mask & (int)(ClipConfigurationMask.LoopTime | ClipConfigurationMask.LoopTransform | ClipConfigurationMask.CycleRootMotion)) != 0;
        }

        static bool NeedsLoopTransform(Data nodeData)
        {
            return (nodeData.Configuration.Mask & (int)(ClipConfigurationMask.LoopTransform)) != 0;
        }

        static bool NeedsStartStopClip(Data nodeData)
        {
            return (nodeData.Configuration.Mask & (int)(ClipConfigurationMask.LoopTransform | ClipConfigurationMask.CycleRootMotion)) != 0;
        }

        static bool NeedsCycleRootMotion(Data nodeData)
        {
            return (nodeData.Configuration.Mask & (int)(ClipConfigurationMask.CycleRootMotion)) != 0;
        }

        static bool NeedsInPlace(Data nodeData)
        {
            return nodeData.Configuration.MotionID != 0;
        }

        void BuildNodes(ref Data data)
        {
            if (data.ClipInstance == BlobAssetReference<ClipInstance>.Null)
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
                data.DeltaNode = Set.Create<DeltaNode>();
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
                    Set.Connect(data.StartInPlaceNode, InPlaceMotionNode.KernelPorts.Output, data.DeltaNode, DeltaNode.KernelPorts.Subtract);
                    Set.Connect(data.StopInPlaceNode, InPlaceMotionNode.KernelPorts.Output, data.DeltaNode, DeltaNode.KernelPorts.Input);
                    Set.Connect(data.InPlaceNode, InPlaceMotionNode.KernelPorts.Output, data.LoopNode, LoopNode.KernelPorts.Input);
                }
                else
                {
                    Set.Connect(data.StartClipNode, ClipNode.KernelPorts.Output, data.DeltaNode, DeltaNode.KernelPorts.Subtract);
                    Set.Connect(data.StopClipNode, ClipNode.KernelPorts.Output, data.DeltaNode, DeltaNode.KernelPorts.Input);
                    Set.Connect(data.ClipNode, ClipNode.KernelPorts.Output, data.LoopNode, LoopNode.KernelPorts.Input);
                }

                Set.Connect(data.DeltaNode, DeltaNode.KernelPorts.Output, data.LoopNode, LoopNode.KernelPorts.Delta);
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
            Set.SendMessage(data.ClipNode, ClipNode.SimulationPorts.ClipInstance, data.ClipInstance);
            Set.SendMessage(data.OutputNode, KernelPassThroughNodeBufferFloat.SimulationPorts.BufferSize,  data.ClipInstance.Value.RigDefinition.Value.Bindings.CurveCount);

            if (loopTransform)
            {
                Set.SendMessage(data.DeltaNode, DeltaNode.SimulationPorts.RigDefinition,  data.ClipInstance.Value.RigDefinition);
                Set.SendMessage(data.LoopNode, LoopNode.SimulationPorts.RigDefinition,  data.ClipInstance.Value.RigDefinition);
                Set.SendMessage(data.LoopNode, LoopNode.SimulationPorts.SkipRoot, inPlace ? 1 : 0);
            }

            if (startStopClip)
            {
                Set.SendMessage(data.StartClipNode, ClipNode.SimulationPorts.ClipInstance,  data.ClipInstance);
                Set.SendMessage(data.StopClipNode, ClipNode.SimulationPorts.ClipInstance,  data.ClipInstance);

                Set.SetData(data.StartClipNode, ClipNode.KernelPorts.Time, 0);
                Set.SetData(data.StopClipNode, ClipNode.KernelPorts.Time,  data.ClipInstance.Value.Clip.Duration);
            }

            if (inPlace)
            {
                Set.SendMessage(data.InPlaceNode, InPlaceMotionNode.SimulationPorts.RigDefinition,  data.ClipInstance.Value.RigDefinition);
                Set.SendMessage(data.InPlaceNode, InPlaceMotionNode.SimulationPorts.Configuration,  data.Configuration);

                if (startStopClip)
                {
                    Set.SendMessage(data.StartInPlaceNode, InPlaceMotionNode.SimulationPorts.RigDefinition,  data.ClipInstance.Value.RigDefinition);
                    Set.SendMessage(data.StopInPlaceNode, InPlaceMotionNode.SimulationPorts.RigDefinition,  data.ClipInstance.Value.RigDefinition);

                    Set.SendMessage(data.StartInPlaceNode, InPlaceMotionNode.SimulationPorts.Configuration,  data.Configuration);
                    Set.SendMessage(data.StopInPlaceNode, InPlaceMotionNode.SimulationPorts.Configuration,  data.Configuration);
                }
            }

            if (cycleRoot)
            {
                Set.SendMessage(data.CycleRootMotionNode, CycleRootMotionNode.SimulationPorts.RigDefinition,  data.ClipInstance.Value.RigDefinition);
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

       public override void Init(InitContext ctx)
       {
           ref var data = ref GetNodeData(ctx.Handle);

           data.TimeNode = Set.Create<KernelPassThroughNodeFloat>();
           data.OutputNode = Set.Create<KernelPassThroughNodeBufferFloat>();

           BuildNodes(ref data);

           ctx.ForwardInput(KernelPorts.Time, data.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
           ctx.ForwardOutput(KernelPorts.Output, data.OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Output);
       }

       public override void Destroy(NodeHandle handle)
       {
           ref var nodeData = ref GetNodeData(handle);

           Set.Destroy(nodeData.TimeNode);
           Set.Destroy(nodeData.OutputNode);

           ClearNodes(nodeData);
       }

       public void HandleMessage(in MessageContext ctx, in BlobAssetReference<ClipInstance> clipInstance)
       {
           ref var nodeData = ref GetNodeData(ctx.Handle);
           nodeData.ClipInstance = clipInstance;

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

       internal KernelData ExposeKernelData(NodeHandle handle) => GetKernelData(handle);
       internal Data ExposeNodeData(NodeHandle handle) => GetNodeData(handle);
    }
}
