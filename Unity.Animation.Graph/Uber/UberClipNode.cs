using System;
using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Mathematics;

namespace Unity.Animation
{
    [NodeDefinition(guid: "859141ab001b4967b5ea5798db0e5879", version: 1, category: "Animation Core", description: "Clip node that can perform different actions based on clip configuration data and supports root motion", isHidden: true)]
    public class UberClipNode
        : SimulationKernelNodeDefinition<UberClipNode.SimPorts, UberClipNode.KernelDefs>
        , IRigContextHandler<UberClipNode.Data>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "3e1fda046ee345d7868bede5248b110b", isHidden: true)]
            public MessageInput<UberClipNode, Rig> Rig;
            [PortDefinition(guid: "1ed1dbc98c954b40aa5b7da1ea927487", description: "Clip to sample")]
            public MessageInput<UberClipNode, BlobAssetReference<Clip>> Clip;
            [PortDefinition(guid: "e9e28a1a30ed4ad3b48bc7e86a30bf30", description: "Clip configuration data")]
            public MessageInput<UberClipNode, ClipConfiguration> Configuration;
            [PortDefinition(guid: "b932e67455d24b0a818f6b04725231db", description: "Is this an additive clip", defaultValue: false)]
            public MessageInput<UberClipNode, bool> Additive;

            internal MessageOutput<UberClipNode, ClipConfiguration> m_OutClipConfig;
            internal MessageOutput<UberClipNode, float> m_OutDuration;
            internal MessageOutput<UberClipNode, Rig> m_OutRig;
            internal MessageOutput<UberClipNode, BlobAssetReference<Clip>> m_OutClip;
            internal MessageOutput<UberClipNode, bool> m_OutIsAdditive;
            internal MessageOutput<UberClipNode, float> m_OutSampleRate;
            internal MessageOutput<UberClipNode, int> m_OutBufferSize;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "9f83a135cc284cd587e0c4c03d98f335", description: "Unbound time")]
            public DataInput<UberClipNode, float> Time;
            [PortDefinition(guid: "c82bccad0b5f4b839e76eb1da59010e9", description: "Delta time")]
            public DataInput<UberClipNode, float> DeltaTime;

            [PortDefinition(guid: "ab8b8a72b06c43079fa00e5667330677", description: "Resulting animation stream")]
            public DataOutput<UberClipNode, Buffer<AnimatedData>> Output;
        }

        internal struct Data : INodeData, IInit, IDestroy
            , IMsgHandler<Rig>
            , IMsgHandler<BlobAssetReference<Clip>>
            , IMsgHandler<ClipConfiguration>
            , IMsgHandler<bool>
        {
            NodeHandle<KernelPassThroughNodeFloat>         m_TimeNode;
            NodeHandle<KernelPassThroughNodeFloat>         m_DeltaTimeNode;
            NodeHandle<KernelPassThroughNodeBufferFloat>   m_OutputNode;

            NodeHandle<NormalizedTimeNode>             m_NormalizedDeltaTimeNode;
            NodeHandle<FloatSubNode>                   m_PrevTimeNode;
            NodeHandle<ConfigurableClipNode>           m_PrevClipNode;
            internal NodeHandle<ConfigurableClipNode>  m_ClipNode;
            NodeHandle<DeltaRootMotionNode>            m_DeltaRootMotionNode;
            NodeHandle<RootMotionFromVelocityNode>     m_RootMotionFromVelocityNode;

            BlobAssetReference<RigDefinition> m_RigDefinition;
            internal BlobAssetReference<Clip> m_Clip;
            bool                              m_IsAdditive;

            ClipConfiguration m_Configuration;

            public void Init(InitContext ctx)
            {
                var thisHandle = ctx.Set.CastHandle<UberClipNode>(ctx.Handle);

                m_TimeNode = ctx.Set.Create<KernelPassThroughNodeFloat>();
                m_DeltaTimeNode = ctx.Set.Create<KernelPassThroughNodeFloat>();
                m_OutputNode = ctx.Set.Create<KernelPassThroughNodeBufferFloat>();

                ctx.ForwardInput(KernelPorts.Time, m_TimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
                ctx.ForwardInput(KernelPorts.DeltaTime, m_DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
                ctx.ForwardOutput(KernelPorts.Output, m_OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Output);

                ctx.Set.Connect(thisHandle, SimulationPorts.m_OutBufferSize, m_OutputNode, KernelPassThroughNodeBufferFloat.SimulationPorts.BufferSize);
            }

            public void Destroy(DestroyContext ctx)
            {
                ctx.Set.Destroy(m_TimeNode);
                ctx.Set.Destroy(m_DeltaTimeNode);
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

            void BuildNodes(MessageContext ctx)
            {
                if (m_Clip == BlobAssetReference<Clip>.Null || m_RigDefinition == BlobAssetReference<RigDefinition>.Null)
                    return;

                var thisHandle = ctx.Set.CastHandle<UberClipNode>(ctx.Handle);

                var mask = m_Configuration.Mask;

                var normalizedTime = (mask & ClipConfigurationMask.NormalizedTime) != 0;
                var deltaRootMotion =  (mask & ClipConfigurationMask.DeltaRootMotion) != 0;
                var rootMotionFromVelocity = (mask & ClipConfigurationMask.RootMotionFromVelocity) != 0;

                if (normalizedTime && rootMotionFromVelocity)
                {
                    m_NormalizedDeltaTimeNode = ctx.Set.Create<NormalizedTimeNode>();
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutDuration, m_NormalizedDeltaTimeNode, NormalizedTimeNode.SimulationPorts.Duration);
                }

                if (deltaRootMotion)
                {
                    m_PrevTimeNode = ctx.Set.Create<FloatSubNode>();
                    m_PrevClipNode = ctx.Set.Create<ConfigurableClipNode>();
                    m_DeltaRootMotionNode = ctx.Set.Create<DeltaRootMotionNode>();

                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutClipConfig, m_PrevClipNode, ConfigurableClipNode.SimulationPorts.Configuration);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig, m_PrevClipNode, ConfigurableClipNode.SimulationPorts.Rig);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutClip, m_PrevClipNode, ConfigurableClipNode.SimulationPorts.Clip);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutIsAdditive, m_PrevClipNode, ConfigurableClipNode.SimulationPorts.Additive);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig, m_DeltaRootMotionNode, DeltaRootMotionNode.SimulationPorts.Rig);
                }
                else if (rootMotionFromVelocity)
                {
                    m_RootMotionFromVelocityNode = ctx.Set.Create<RootMotionFromVelocityNode>();

                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig, m_RootMotionFromVelocityNode, RootMotionFromVelocityNode.SimulationPorts.Rig);
                    ctx.Set.Connect(thisHandle, SimulationPorts.m_OutSampleRate, m_RootMotionFromVelocityNode, RootMotionFromVelocityNode.SimulationPorts.SampleRate);
                }

                m_ClipNode = ctx.Set.Create<ConfigurableClipNode>();
                ctx.Set.Connect(thisHandle, SimulationPorts.m_OutClipConfig, m_ClipNode, ConfigurableClipNode.SimulationPorts.Configuration);
                ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig, m_ClipNode, ConfigurableClipNode.SimulationPorts.Rig);
                ctx.Set.Connect(thisHandle, SimulationPorts.m_OutClip, m_ClipNode, ConfigurableClipNode.SimulationPorts.Clip);
                ctx.Set.Connect(thisHandle, SimulationPorts.m_OutIsAdditive, m_ClipNode, ConfigurableClipNode.SimulationPorts.Additive);

                if (deltaRootMotion)
                {
                    ctx.Set.Connect(m_TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_PrevTimeNode, FloatSubNode.KernelPorts.InputA);
                    ctx.Set.Connect(m_DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_PrevTimeNode, FloatSubNode.KernelPorts.InputB);

                    ctx.Set.Connect(m_PrevTimeNode, FloatSubNode.KernelPorts.Output, m_PrevClipNode, ConfigurableClipNode.KernelPorts.Time);
                    ctx.Set.Connect(m_TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_ClipNode, ConfigurableClipNode.KernelPorts.Time);

                    ctx.Set.Connect(m_PrevClipNode, ConfigurableClipNode.KernelPorts.Output, m_DeltaRootMotionNode, DeltaRootMotionNode.KernelPorts.Previous);
                    ctx.Set.Connect(m_ClipNode, ConfigurableClipNode.KernelPorts.Output, m_DeltaRootMotionNode, DeltaRootMotionNode.KernelPorts.Current);

                    ctx.Set.Connect(m_DeltaRootMotionNode, DeltaRootMotionNode.KernelPorts.Output, m_OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);
                }
                else if (rootMotionFromVelocity)
                {
                    ctx.Set.Connect(m_TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_ClipNode, ConfigurableClipNode.KernelPorts.Time);

                    if (normalizedTime)
                    {
                        ctx.Set.Connect(m_DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_NormalizedDeltaTimeNode, NormalizedTimeNode.KernelPorts.InputTime);
                        ctx.Set.Connect(m_NormalizedDeltaTimeNode, NormalizedTimeNode.KernelPorts.OutputTime, m_RootMotionFromVelocityNode, RootMotionFromVelocityNode.KernelPorts.DeltaTime);
                    }
                    else
                    {
                        ctx.Set.Connect(m_DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_RootMotionFromVelocityNode, RootMotionFromVelocityNode.KernelPorts.DeltaTime);
                    }

                    ctx.Set.Connect(m_ClipNode, ConfigurableClipNode.KernelPorts.Output, m_RootMotionFromVelocityNode, RootMotionFromVelocityNode.KernelPorts.Input);
                    ctx.Set.Connect(m_RootMotionFromVelocityNode, RootMotionFromVelocityNode.KernelPorts.Output, m_OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);
                }
                else
                {
                    ctx.Set.Connect(m_TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_ClipNode, ConfigurableClipNode.KernelPorts.Time);
                    ctx.Set.Connect(m_ClipNode, ConfigurableClipNode.KernelPorts.Output, m_OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);
                }

                EmitAllOutMessages(ctx);
            }

            public void EmitAllOutMessages(MessageContext ctx)
            {
                // We can emit on all the output messages, if there's no connection nothing happens.
                ctx.EmitMessage(SimulationPorts.m_OutRig, new Rig { Value = m_RigDefinition });
                ctx.EmitMessage(SimulationPorts.m_OutClipConfig, m_Configuration);
                ctx.EmitMessage(SimulationPorts.m_OutDuration, m_Clip.Value.Duration);
                ctx.EmitMessage(SimulationPorts.m_OutSampleRate, m_Clip.Value.SampleRate);
                ctx.EmitMessage(SimulationPorts.m_OutClip, m_Clip);
                ctx.EmitMessage(SimulationPorts.m_OutIsAdditive, m_IsAdditive);
                ctx.EmitMessage(SimulationPorts.m_OutBufferSize, m_RigDefinition.Value.Bindings.StreamSize);
            }

            void ClearNodes(NodeSetAPI set)
            {
                if (set.Exists(m_NormalizedDeltaTimeNode))
                    set.Destroy(m_NormalizedDeltaTimeNode);

                if (set.Exists(m_PrevTimeNode))
                    set.Destroy(m_PrevTimeNode);

                if (set.Exists(m_PrevClipNode))
                    set.Destroy(m_PrevClipNode);

                if (set.Exists(m_ClipNode))
                    set.Destroy(m_ClipNode);

                if (set.Exists(m_DeltaRootMotionNode))
                    set.Destroy(m_DeltaRootMotionNode);

                if (set.Exists(m_RootMotionFromVelocityNode))
                    set.Destroy(m_RootMotionFromVelocityNode);
            }
        }

        struct KernelData : IKernelData {}

        [BurstCompile]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports) {}
        }


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

            var defaultStream = AnimationStream.FromDefaultValues(stream.Rig);
            for (int i = 0, count = bindings.RotationBindings.Length, curveIndex = bindings.RotationSamplesOffset; i < count; i++, curveIndex += BindingSet.RotationKeyFloatCount)
            {
                var index = clipInstance.RotationBindingMap[i];
                var r = stream.GetLocalToParentRotation(index);

                float4 prevR;
                if (frameIndex == 0)
                {
                    prevR = defaultStream.GetLocalToParentRotation(index).value;
                }
                else
                {
                    var prevKeyIndex = (frameIndex - 1) * curveCount;
                    prevR = Core.GetDataInSample<float4>(ref samples, curveIndex + prevKeyIndex);
                }

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

            var sourceClipInstance = ClipInstanceBuilder.Create(rig, sourceClip);

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

            var syncTags = blobBuilder.Allocate(ref clip.SynchronizationTags, sourceClip.Value.SynchronizationTags.Length);
            syncTags.CopyFrom(ref sourceClip.Value.SynchronizationTags);

            var clipAsset = blobBuilder.CreateBlobAssetReference<Clip>(Allocator.Persistent);
            blobBuilder.Dispose();

            var clipInstance = ClipInstanceBuilder.Create(rig, clipAsset);

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

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.Rig;
    }
}
