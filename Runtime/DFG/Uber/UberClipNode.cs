using System;
using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Mathematics;

namespace Unity.Animation
{
    [Serializable, Flags]
    public enum ClipConfigurationMask : int
    {
        NormalizedTime         = 1 << 0,
        LoopTime               = 1 << 1,
        LoopValues             = 1 << 2,
        CycleRootMotion        = 1 << 3,
        DeltaRootMotion        = 1 << 4,
        RootMotionFromVelocity = 1 << 5,
        BankPivot              = 1 << 6
    }

    [Serializable]
    public struct ClipConfiguration
    {
        public ClipConfigurationMask Mask;
        public StringHash MotionID;
    }

    [NodeDefinition(category:"Animation Core", description:"Clip node that can perform different actions based on clip configuration data and supports root motion", isHidden:true)]
    public class UberClipNode
        : NodeDefinition<UberClipNode.Data, UberClipNode.SimPorts, UberClipNode.KernelData, UberClipNode.KernelDefs, UberClipNode.Kernel>
        , IMsgHandler<Rig>
        , IMsgHandler<BlobAssetReference<Clip>>
        , IMsgHandler<ClipConfiguration>
        , IMsgHandler<bool>
        , IRigContextHandler
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<UberClipNode, Rig> Rig;
            [PortDefinition(description:"Clip to sample")]
            public MessageInput<UberClipNode, BlobAssetReference<Clip>> Clip;
            [PortDefinition(description:"Clip configuration data")]
            public MessageInput<UberClipNode, ClipConfiguration> Configuration;
            [PortDefinition(description:"Is this an additive clip", defaultValue:false)]
            public MessageInput<UberClipNode, bool> Additive;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Unbound time")]
            public DataInput<UberClipNode, float> Time;
            [PortDefinition(description:"Delta time")]
            public DataInput<UberClipNode, float> DeltaTime;

            [PortDefinition(description:"Resulting animation stream")]
            public DataOutput<UberClipNode, Buffer<AnimatedData>> Output;
        }

        public struct Data : INodeData
        {
            internal NodeHandle<KernelPassThroughNodeFloat>         TimeNode;
            internal NodeHandle<KernelPassThroughNodeFloat>         DeltaTimeNode;
            internal NodeHandle<KernelPassThroughNodeBufferFloat>   OutputNode;

            internal NodeHandle<NormalizedTimeNode>             NormalizedDeltaTimeNode;
            internal NodeHandle<FloatSubNode>                   PrevTimeNode;
            internal NodeHandle<ConfigurableClipNode>           PrevClipNode;
            internal NodeHandle<ConfigurableClipNode>           ClipNode;
            internal NodeHandle<DeltaRootMotionNode>            DeltaRootMotionNode;
            internal NodeHandle<RootMotionFromVelocityNode>     RootMotionFromVelocityNode;

            public BlobAssetReference<RigDefinition> RigDefinition;
            public BlobAssetReference<Clip>          Clip;
            public bool                              IsAdditive;

            internal ClipConfiguration Configuration;
        }

        public struct KernelData : IKernelData { }

        [BurstCompile]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports) { }
        }

        void BuildNodes(ref Data nodeData)
        {
            if (nodeData.Clip == BlobAssetReference<Clip>.Null || nodeData.RigDefinition == BlobAssetReference<RigDefinition>.Null)
                return;

            var mask = nodeData.Configuration.Mask;

            var normalizedTime = (mask & ClipConfigurationMask.NormalizedTime) != 0;
            var deltaRootMotion =  (mask & ClipConfigurationMask.DeltaRootMotion) != 0;
            var rootMotionFromVelocity = (mask & ClipConfigurationMask.RootMotionFromVelocity) != 0;

            if (normalizedTime && rootMotionFromVelocity)
            {
                nodeData.NormalizedDeltaTimeNode = Set.Create<NormalizedTimeNode>();
            }

            if (deltaRootMotion)
            {
                nodeData.PrevTimeNode = Set.Create<FloatSubNode>();
                nodeData.PrevClipNode = Set.Create<ConfigurableClipNode>();
                nodeData.DeltaRootMotionNode = Set.Create<DeltaRootMotionNode>();
            }
            else if (rootMotionFromVelocity)
            {
                nodeData.RootMotionFromVelocityNode = Set.Create<RootMotionFromVelocityNode>();
            }

            nodeData.ClipNode = Set.Create<ConfigurableClipNode>();

            if (deltaRootMotion)
            {
                Set.Connect(nodeData.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.PrevTimeNode, FloatSubNode.KernelPorts.InputA);
                Set.Connect(nodeData.DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.PrevTimeNode, FloatSubNode.KernelPorts.InputB);

                Set.Connect(nodeData.PrevTimeNode, FloatSubNode.KernelPorts.Output, nodeData.PrevClipNode, ConfigurableClipNode.KernelPorts.Time);
                Set.Connect(nodeData.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.ClipNode, ConfigurableClipNode.KernelPorts.Time);

                Set.Connect(nodeData.PrevClipNode, ConfigurableClipNode.KernelPorts.Output, nodeData.DeltaRootMotionNode, DeltaRootMotionNode.KernelPorts.Previous);
                Set.Connect(nodeData.ClipNode, ConfigurableClipNode.KernelPorts.Output, nodeData.DeltaRootMotionNode, DeltaRootMotionNode.KernelPorts.Current);

                Set.Connect(nodeData.DeltaRootMotionNode, DeltaRootMotionNode.KernelPorts.Output, nodeData.OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);
            }
            else if (rootMotionFromVelocity)
            {
                Set.Connect(nodeData.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.ClipNode, ConfigurableClipNode.KernelPorts.Time);

                if (normalizedTime)
                {
                    Set.Connect(nodeData.DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.NormalizedDeltaTimeNode, NormalizedTimeNode.KernelPorts.InputTime);
                    Set.Connect(nodeData.NormalizedDeltaTimeNode, NormalizedTimeNode.KernelPorts.OutputTime, nodeData.RootMotionFromVelocityNode, RootMotionFromVelocityNode.KernelPorts.DeltaTime);
                }
                else
                {
                    Set.Connect(nodeData.DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.RootMotionFromVelocityNode, RootMotionFromVelocityNode.KernelPorts.DeltaTime);
                }

                Set.Connect(nodeData.ClipNode, ConfigurableClipNode.KernelPorts.Output, nodeData.RootMotionFromVelocityNode, RootMotionFromVelocityNode.KernelPorts.Input);
                Set.Connect(nodeData.RootMotionFromVelocityNode, RootMotionFromVelocityNode.KernelPorts.Output, nodeData.OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);
            }
            else
            {
                Set.Connect(nodeData.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.ClipNode, ConfigurableClipNode.KernelPorts.Time);
                Set.Connect(nodeData.ClipNode, ConfigurableClipNode.KernelPorts.Output, nodeData.OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);
            }

            Set.SendMessage(nodeData.ClipNode, ConfigurableClipNode.SimulationPorts.Configuration, nodeData.Configuration);

            if (normalizedTime && rootMotionFromVelocity)
            {
                 Set.SendMessage(nodeData.NormalizedDeltaTimeNode, NormalizedTimeNode.SimulationPorts.Duration, nodeData.Clip.Value.Duration);
            }

            if (deltaRootMotion)
            {
                Set.SendMessage(nodeData.PrevClipNode, ConfigurableClipNode.SimulationPorts.Configuration, nodeData.Configuration);
                Set.SendMessage(nodeData.PrevClipNode, ConfigurableClipNode.SimulationPorts.Rig, new Rig { Value = nodeData.RigDefinition });
                Set.SendMessage(nodeData.PrevClipNode, ConfigurableClipNode.SimulationPorts.Clip, nodeData.Clip);
                Set.SendMessage(nodeData.PrevClipNode, ConfigurableClipNode.SimulationPorts.Additive, nodeData.IsAdditive);
                Set.SendMessage(nodeData.DeltaRootMotionNode, DeltaRootMotionNode.SimulationPorts.Rig, new Rig { Value = nodeData.RigDefinition });
            }
            else if (rootMotionFromVelocity)
            {
                Set.SendMessage(nodeData.RootMotionFromVelocityNode, RootMotionFromVelocityNode.SimulationPorts.Rig, new Rig { Value = nodeData.RigDefinition });
                Set.SendMessage(nodeData.RootMotionFromVelocityNode, RootMotionFromVelocityNode.SimulationPorts.SampleRate, nodeData.Clip.Value.SampleRate);
            }

            Set.SendMessage(nodeData.ClipNode, ConfigurableClipNode.SimulationPorts.Rig, new Rig { Value = nodeData.RigDefinition });
            Set.SendMessage(nodeData.ClipNode, ConfigurableClipNode.SimulationPorts.Clip, nodeData.Clip);
            Set.SendMessage(nodeData.ClipNode, ConfigurableClipNode.SimulationPorts.Additive, nodeData.IsAdditive);
            Set.SendMessage(nodeData.OutputNode, KernelPassThroughNodeBufferFloat.SimulationPorts.BufferSize, nodeData.RigDefinition.Value.Bindings.StreamSize);
        }

        void ClearNodes(Data nodeData)
        {
            if (Set.Exists(nodeData.NormalizedDeltaTimeNode))
                Set.Destroy(nodeData.NormalizedDeltaTimeNode);

            if (Set.Exists(nodeData.PrevTimeNode))
                Set.Destroy(nodeData.PrevTimeNode);

            if (Set.Exists(nodeData.PrevClipNode))
                Set.Destroy(nodeData.PrevClipNode);

            if (Set.Exists(nodeData.ClipNode))
                Set.Destroy(nodeData.ClipNode);

            if (Set.Exists(nodeData.DeltaRootMotionNode))
                Set.Destroy(nodeData.DeltaRootMotionNode);

            if (Set.Exists(nodeData.RootMotionFromVelocityNode))
                Set.Destroy(nodeData.RootMotionFromVelocityNode);
        }

        protected override void Init(InitContext ctx)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);

            nodeData.TimeNode = Set.Create<KernelPassThroughNodeFloat>();
            nodeData.DeltaTimeNode = Set.Create<KernelPassThroughNodeFloat>();
            nodeData.OutputNode = Set.Create<KernelPassThroughNodeBufferFloat>();

            BuildNodes(ref nodeData);

            ctx.ForwardInput(KernelPorts.Time, nodeData.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
            ctx.ForwardInput(KernelPorts.DeltaTime, nodeData.DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
            ctx.ForwardOutput(KernelPorts.Output, nodeData.OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Output);
        }

        protected override void Destroy(NodeHandle handle)
        {
            var nodeData = GetNodeData(handle);

            Set.Destroy(nodeData.TimeNode);
            Set.Destroy(nodeData.DeltaTimeNode);
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

        internal Data ExposeNodeData(NodeHandle handle) => GetNodeData(handle);
        internal KernelData ExposeKernelData(NodeHandle handle) => GetKernelData(handle);

        static void UpdateFrame(
            ref ClipInstance clipInstance,
            ref BlobArray<float> samples,
            ref AnimationStream stream,
            int frameIndex
            )
        {
            ref var clip = ref clipInstance.Clip;
            ref var bindings = ref clip.Bindings;
            var curveCount = clip.Bindings.CurveCount;

            var keyIndex = frameIndex * curveCount;

            for (int i = 0, count = bindings.TranslationBindings.Length, curveIndex = bindings.TranslationSamplesOffset; i < count; i++, curveIndex += BindingSet.TranslationKeyFloatCount)
            {
                var index = clipInstance.TranslationBindingMap[i];

                var t = stream.GetLocalToParentTranslation(index);

                if (frameIndex < clipInstance.Clip.FrameCount)
                {
                    Core.SetDataInSample<float3>(ref samples, curveIndex + keyIndex, t);
                }
                else
                {
                    var prevKeyIndex = (frameIndex - 1) * curveCount;
                    var prevT = Core.GetDataInSample<float3>(ref samples, curveIndex + prevKeyIndex);
                    Core.SetDataInSample<float3>(ref samples, curveIndex + keyIndex, Core.AdjustLastFrameValue(prevT, t, clipInstance.Clip.LastFrameError));
                }
            }

            for (int i = 0, count = bindings.RotationBindings.Length, curveIndex = bindings.RotationSamplesOffset; i < count; i++, curveIndex += BindingSet.RotationKeyFloatCount)
            {
                var index = clipInstance.RotationBindingMap[i];

                var r = stream.GetLocalToParentRotation(index);
                var prevKeyIndex = (frameIndex - 1) * curveCount;
                var prevR = Core.GetDataInSample<float4>(ref samples, curveIndex + prevKeyIndex);
                r.value = math.dot(r.value, prevR) < 0 ? r.value * -1.0f : r.value;

                if (frameIndex < clipInstance.Clip.FrameCount)
                {
                    Core.SetDataInSample<float4>(ref samples, curveIndex + keyIndex, r.value);
                }
                else
                {
                    r.value = Core.AdjustLastFrameValue(prevR, r.value, clipInstance.Clip.LastFrameError);
                    r = math.normalizesafe(r);
                    Core.SetDataInSample<float4>(ref samples, curveIndex + keyIndex, r.value);
                }
            }

            for (int i = 0, count = bindings.ScaleBindings.Length, curveIndex = bindings.ScaleSamplesOffset; i < count; i++, curveIndex += BindingSet.ScaleKeyFloatCount)
            {
                var index = clipInstance.ScaleBindingMap[i];

                var s = stream.GetLocalToParentScale(index);

                if (frameIndex < clipInstance.Clip.FrameCount)
                {
                    Core.SetDataInSample<float3>(ref samples, curveIndex + keyIndex, s);
                }
                else
                {
                    var prevKeyIndex = (frameIndex - 1) * curveCount;
                    var prevS = Core.GetDataInSample<float3>(ref samples, curveIndex + prevKeyIndex);
                    Core.SetDataInSample<float3>(ref samples, curveIndex + keyIndex, Core.AdjustLastFrameValue(prevS, s, clipInstance.Clip.LastFrameError));
                }
            }

            for (int i = 0, count = bindings.FloatBindings.Length, curveIndex = bindings.FloatSamplesOffset; i < count; i++, curveIndex += BindingSet.FloatKeyFloatCount)
            {
                var index = clipInstance.FloatBindingMap[i];

                var f = stream.GetFloat(index);

                if (frameIndex < clipInstance.Clip.FrameCount)
                {
                    Core.SetDataInSample<float>(ref samples, curveIndex + keyIndex, f);
                }
                else
                {
                    var prevKeyIndex = (frameIndex - 1) * curveCount;
                    var prevF = Core.GetDataInSample<float>(ref samples, curveIndex + prevKeyIndex);
                    Core.SetDataInSample<float>(ref samples, curveIndex + keyIndex, Core.AdjustLastFrameValue(prevF, f, clipInstance.Clip.LastFrameError));
                }
            }

            for (int i = 0, count = bindings.IntBindings.Length, curveIndex = bindings.IntSamplesOffset; i < count; i++, curveIndex += BindingSet.IntKeyFloatCount)
            {
                var index = clipInstance.FloatBindingMap[i];

                var v = stream.GetInt(index);
                Core.SetDataInSample<float>(ref samples, curveIndex + keyIndex, v);
            }
        }

        public static BlobAssetReference<Clip> Bake(BlobAssetReference<RigDefinition> rig, BlobAssetReference<Clip> sourceClip, ClipConfiguration clipConfiguration, float sampleRate = 60.0f)
        {
            using (var set = new NodeSet())
                return Bake(set, rig, sourceClip, clipConfiguration, sampleRate);
        }

        public static BlobAssetReference<Clip> Bake(NodeSet set, BlobAssetReference<RigDefinition> rig, BlobAssetReference<Clip> sourceClip, ClipConfiguration clipConfiguration, float sampleRate = 60.0f)
        {
            if (set == null)
                throw new ArgumentNullException(nameof(set));

            var clipNode = set.Create<UberClipNode>();
            set.SendMessage(clipNode, UberClipNode.SimulationPorts.Configuration, clipConfiguration);
            set.SendMessage(clipNode, UberClipNode.SimulationPorts.Rig, new Rig { Value = rig });
            set.SendMessage(clipNode, UberClipNode.SimulationPorts.Clip, sourceClip);

            var graphValue = set.CreateGraphValue(clipNode, KernelPorts.Output);

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var clip = ref blobBuilder.ConstructRoot<Clip>();

            clip.Duration = sourceClip.Value.Duration;
            clip.SampleRate = sampleRate;

            var sourceClipInstance = ClipInstance.Create(rig, sourceClip);

            var needsRoot  = clipConfiguration.MotionID != 0;
            var needsRootT = needsRoot && sourceClipInstance.Value.TranslationBindingMap.Length > 0 && sourceClipInstance.Value.TranslationBindingMap[0] != 0;
            var needsRootR = needsRoot && sourceClipInstance.Value.RotationBindingMap.Length > 0 && sourceClipInstance.Value.RotationBindingMap[0] != 0;

            var translationBindingsCount = sourceClipInstance.Value.Clip.Bindings.TranslationBindings.Length + (needsRootT ? 1 : 0);
            if (translationBindingsCount > 0)
            {
                var translationBindings = blobBuilder.Allocate(ref clip.Bindings.TranslationBindings, translationBindingsCount);

                if (needsRootT)
                {
                    translationBindings[0] = rig.Value.Bindings.TranslationBindings[0];
                }

                for (var iter = 0; iter < translationBindingsCount - (needsRootT ? 1 : 0); iter++)
                {
                    translationBindings[iter + (needsRootT ? 1 : 0)] = sourceClipInstance.Value.Clip.Bindings.TranslationBindings[iter];
                }
            }

            var rotationBindingsCount = sourceClipInstance.Value.Clip.Bindings.RotationBindings.Length + (needsRootR ? 1 : 0);
            if (rotationBindingsCount > 0)
            {
                var rotationBindings = blobBuilder.Allocate(ref clip.Bindings.RotationBindings, rotationBindingsCount);

                if (needsRootR)
                {
                    rotationBindings[0] = rig.Value.Bindings.RotationBindings[0];
                }

                for (var iter = 0; iter < rotationBindingsCount - (needsRootR ? 1 : 0); iter++)
                {
                    rotationBindings[iter + (needsRootR ? 1 : 0)] = sourceClipInstance.Value.Clip.Bindings.RotationBindings[iter];
                }
            }

            var scaleBindingsCount = sourceClipInstance.Value.Clip.Bindings.ScaleBindings.Length;
            if (scaleBindingsCount > 0)
            {
                var scaleBindings = blobBuilder.Allocate(ref clip.Bindings.ScaleBindings, scaleBindingsCount);
                scaleBindings.CopyFrom(ref sourceClipInstance.Value.Clip.Bindings.ScaleBindings);
            }

            var floatBindingsCount = sourceClipInstance.Value.Clip.Bindings.FloatBindings.Length;
            if (floatBindingsCount > 0)
            {
                var floatBindings = blobBuilder.Allocate(ref clip.Bindings.FloatBindings, floatBindingsCount);
                floatBindings.CopyFrom(ref sourceClipInstance.Value.Clip.Bindings.FloatBindings);
            }

            var intBindingsCount = sourceClipInstance.Value.Clip.Bindings.IntBindings.Length;
            if (intBindingsCount > 0)
            {
                var intBindings = blobBuilder.Allocate(ref clip.Bindings.IntBindings, intBindingsCount);
                intBindings.CopyFrom(ref sourceClipInstance.Value.Clip.Bindings.IntBindings);
            }

            clip.Bindings = clip.CreateBindingSet(translationBindingsCount, rotationBindingsCount, scaleBindingsCount, floatBindingsCount, intBindingsCount);

            blobBuilder.Allocate(ref clip.Samples, clip.Bindings.CurveCount * (clip.FrameCount + 1));

            var clipAsset = blobBuilder.CreateBlobAssetReference<Clip>(Allocator.Persistent);
            blobBuilder.Dispose();

            var clipInstance = ClipInstance.Create(rig, clipAsset);

            var deltaTime = 1.0f / clipInstance.Value.Clip.SampleRate;
            set.SetData(clipNode, KernelPorts.DeltaTime, deltaTime);

            var frameCount = clipInstance.Value.Clip.FrameCount;

            for (var frameIter = 0; frameIter <= frameCount; frameIter++)
            {
                var time = frameIter < frameCount ? (float)frameIter / clipInstance.Value.Clip.SampleRate : clipInstance.Value.Clip.Duration;
                set.SetData(clipNode, KernelPorts.Time, time);

                set.Update();
                var buffer = DFGUtils.GetGraphValueTempNativeBuffer(set, graphValue);
                var stream = AnimationStream.CreateReadOnly(rig, buffer);

                UpdateFrame(ref clipInstance.Value, ref clipAsset.Value.Samples, ref stream, frameIter);
            }

            sourceClipInstance.Dispose();
            clipInstance.Dispose();

            set.ReleaseGraphValue(graphValue);
            set.Destroy(clipNode);

            clipAsset.Value.m_HashCode = (int)HashUtils.ComputeHash(ref clipAsset);

            return clipAsset;
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
