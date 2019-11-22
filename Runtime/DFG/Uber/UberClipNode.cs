using System;
using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Mathematics;

namespace Unity.Animation
{
    public enum ClipConfigurationMask
    {
        NormalizedTime = 1 << 0,
        LoopTime = 1 << 1,
        LoopValues = 1 << 2,
        CycleRootMotion = 1 << 3,
        DeltaRootMotion = 1 << 4,
        RootMotionFromVelocity = 1 << 5,
        BankPivot = 1 << 6
    }

    public struct ClipConfiguration
    {
        public int Mask;
        public StringHash MotionID;
    }

    public class UberClipNode
        : NodeDefinition<UberClipNode.Data, UberClipNode.SimPorts, UberClipNode.KernelData, UberClipNode.KernelDefs, UberClipNode.Kernel>
            , IMsgHandler<BlobAssetReference<ClipInstance>>
            , IMsgHandler<ClipConfiguration>
            , INormalizedTimeMotion
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<UberClipNode, BlobAssetReference<ClipInstance>> ClipInstance;
            public MessageInput<UberClipNode, ClipConfiguration> Configuration;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<UberClipNode, float> Time;
            public DataInput<UberClipNode, float> DeltaTime;

            public DataOutput<UberClipNode, Buffer<float>> Output;
        }

        public struct Data : INodeData
        {
            internal NodeHandle<KernelPassThroughNodeFloat> TimeNode;
            internal NodeHandle<KernelPassThroughNodeFloat> DeltaTimeNode;
            internal NodeHandle<KernelPassThroughNodeBufferFloat> OutputNode;

            internal NodeHandle<NormalizedTimeNode> NormalizedDeltaTimeNode;
            internal NodeHandle<FloatSubNode> PrevTimeNode;
            internal NodeHandle<ConfigurableClipNode> PrevClipNode;
            internal NodeHandle<ConfigurableClipNode> ClipNode;
            internal NodeHandle<DeltaRootMotionNode> DeltaRootMotionNode;
            internal NodeHandle<RootMotionFromVelocityNode> RootMotionFromVelocityNode;

            internal BlobAssetReference<ClipInstance> ClipInstance;
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
            var mask = nodeData.Configuration.Mask;

            var normalizedTime = (mask & (int)ClipConfigurationMask.NormalizedTime) != 0;
            var deltaRootMotion =  (mask & (int)ClipConfigurationMask.DeltaRootMotion) != 0;
            var rootMotionFromVelocity = (mask & (int)ClipConfigurationMask.RootMotionFromVelocity) != 0;

            if (nodeData.ClipInstance == BlobAssetReference<ClipInstance>.Null)
            {
                return;
            }

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

                Set.Connect(nodeData.PrevClipNode, ConfigurableClipNode.KernelPorts.Output, nodeData.DeltaRootMotionNode, DeltaRootMotionNode.KernelPorts.Prev);
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
                 Set.SendMessage(nodeData.NormalizedDeltaTimeNode, NormalizedTimeNode.SimulationPorts.Duration, nodeData.ClipInstance.Value.Clip.Duration);
            }

            if (deltaRootMotion)
            {
                Set.SendMessage(nodeData.PrevClipNode, ConfigurableClipNode.SimulationPorts.Configuration, nodeData.Configuration);
                Set.SendMessage(nodeData.PrevClipNode, ConfigurableClipNode.SimulationPorts.ClipInstance, nodeData.ClipInstance);
                Set.SendMessage(nodeData.DeltaRootMotionNode, DeltaRootMotionNode.SimulationPorts.RigDefinition, nodeData.ClipInstance.Value.RigDefinition);
            }
            else if (rootMotionFromVelocity)
            {
                Set.SendMessage(nodeData.RootMotionFromVelocityNode, RootMotionFromVelocityNode.SimulationPorts.RigDefinition, nodeData.ClipInstance.Value.RigDefinition);
                Set.SendMessage(nodeData.RootMotionFromVelocityNode, RootMotionFromVelocityNode.SimulationPorts.SampleRate, nodeData.ClipInstance.Value.Clip.SampleRate);
            }

            Set.SendMessage(nodeData.ClipNode, ConfigurableClipNode.SimulationPorts.ClipInstance, nodeData.ClipInstance);
            Set.SendMessage(nodeData.OutputNode, KernelPassThroughNodeBufferFloat.SimulationPorts.BufferSize, nodeData.ClipInstance.Value.RigDefinition.Value.Bindings.CurveCount);
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

        public override void Init(InitContext ctx)
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

        public override void Destroy(NodeHandle handle)
        {
            var nodeData = GetNodeData(handle);

            Set.Destroy(nodeData.TimeNode);
            Set.Destroy(nodeData.DeltaTimeNode);
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

        public OutputPortID AnimationStreamOutputPort =>
            (OutputPortID)KernelPorts.Output;

        public float GetDuration(NodeHandle handle)
        {
            var nodeData = GetNodeData(handle);
            return nodeData.ClipInstance != BlobAssetReference<ClipInstance>.Null ? nodeData.ClipInstance.Value.Clip.Duration : 0;
        }

        public InputPortID NormalizedTimeInputPort =>
            (InputPortID)KernelPorts.Time;

        internal Data ExposeNodeData(NodeHandle handle) => GetNodeData(handle);
        internal KernelData ExposeKernelData(NodeHandle handle) => GetKernelData(handle);

        static void UpdateFrame<T>(
            ref ClipInstance clipInstance,
            ref BlobArray<float> samples,
            ref AnimationStream<T> stream, int frameIndex)
            where T : struct, IAnimationStreamDescriptor
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
        public static BlobAssetReference<Clip> Bake(BlobAssetReference<ClipInstance> sourceClipInstance, ClipConfiguration clipConfiguration, float sampleRate = 60.0f)
        {
            using (var world = new World("BakeUberClipNodeWorld"))
            {
                // create the UberClipNode to resample
                var animationSystem = world.GetOrCreateSystem<AnimationGraphSystem>();
                animationSystem.AddRef();

                var set = animationSystem.Set;
                var entityManager = world.EntityManager;

                var entity = entityManager.CreateEntity();
                RigEntityBuilder.SetupRigEntity(entity, entityManager, sourceClipInstance.Value.RigDefinition);

                var clipNode = set.Create<UberClipNode>();
                set.SendMessage(clipNode, UberClipNode.SimulationPorts.Configuration, clipConfiguration);
                set.SendMessage(clipNode, UberClipNode.SimulationPorts.ClipInstance, sourceClipInstance);

                var output = new GraphOutput { Buffer = set.CreateGraphValue(clipNode, UberClipNode.KernelPorts.Output) };
                entityManager.AddComponentData(entity, output);

                // create the destination clip instance
                var blobBuilder = new BlobBuilder(Allocator.Temp);
                ref var clip = ref blobBuilder.ConstructRoot<Clip>();

                clip.Duration = sourceClipInstance.Value.Clip.Duration;
                clip.SampleRate = sampleRate;

                var needsRoot = clipConfiguration.MotionID != 0;
                var needsRootT = needsRoot && sourceClipInstance.Value.TranslationBindingMap[0] != 0;
                var needsRootR = needsRoot && sourceClipInstance.Value.RotationBindingMap[0] != 0;

                var curveCount = 0;

                var translationBindingsCount = sourceClipInstance.Value.Clip.Bindings.TranslationBindings.Length + (needsRootT ? 1 : 0);

                if (translationBindingsCount > 0)
                {
                    var translationBindings = blobBuilder.Allocate(ref clip.Bindings.TranslationBindings, translationBindingsCount);

                    if (needsRootT)
                    {
                        translationBindings[0] = sourceClipInstance.Value.RigDefinition.Value.Bindings.TranslationBindings[0];
                    }

                    for (var iter = 0; iter < translationBindingsCount - (needsRootT ? 1 : 0); iter++)
                    {
                        translationBindings[iter + (needsRootT ? 1 : 0)] = sourceClipInstance.Value.Clip.Bindings.TranslationBindings[iter];
                    }

                    curveCount += translationBindingsCount * BindingSet.TranslationKeyFloatCount;
                }

                var rotationBindingsCount = sourceClipInstance.Value.Clip.Bindings.RotationBindings.Length + (needsRootR ? 1 : 0);

                if (rotationBindingsCount > 0)
                {
                    var rotationBindings = blobBuilder.Allocate(ref clip.Bindings.RotationBindings, rotationBindingsCount);

                    if (needsRootR)
                    {
                        rotationBindings[0] = sourceClipInstance.Value.RigDefinition.Value.Bindings.RotationBindings[0];
                    }

                    for (var iter = 0; iter < rotationBindingsCount - (needsRootR ? 1 : 0); iter++)
                    {
                        rotationBindings[iter + (needsRootR ? 1 : 0)] = sourceClipInstance.Value.Clip.Bindings.RotationBindings[iter];
                    }


                    curveCount += rotationBindingsCount * BindingSet.RotationKeyFloatCount;
                }

                var scaleBindingsCount = sourceClipInstance.Value.Clip.Bindings.ScaleBindings.Length;

                if (scaleBindingsCount > 0)
                {
                    var scaleBindings = blobBuilder.Allocate(ref clip.Bindings.ScaleBindings, scaleBindingsCount);
                    scaleBindings.CopyFrom(ref sourceClipInstance.Value.Clip.Bindings.ScaleBindings);

                    curveCount += scaleBindingsCount * BindingSet.ScaleKeyFloatCount;
                }

                var floatBindingsCount = sourceClipInstance.Value.Clip.Bindings.FloatBindings.Length;

                if (floatBindingsCount > 0)
                {
                    var floatBindings = blobBuilder.Allocate(ref clip.Bindings.FloatBindings, floatBindingsCount);
                    floatBindings.CopyFrom(ref sourceClipInstance.Value.Clip.Bindings.FloatBindings);

                    curveCount += floatBindingsCount * BindingSet.FloatKeyFloatCount;
                }

                var intBindingsCount = sourceClipInstance.Value.Clip.Bindings.IntBindings.Length;

                if (intBindingsCount > 0)
                {
                    var intBindings = blobBuilder.Allocate(ref clip.Bindings.IntBindings, intBindingsCount);
                    intBindings.CopyFrom(ref sourceClipInstance.Value.Clip.Bindings.IntBindings);

                    curveCount += intBindingsCount * BindingSet.IntKeyFloatCount;
                }

                blobBuilder.Allocate(ref clip.Samples, curveCount * (clip.FrameCount + 1));

                var clipRef = blobBuilder.CreateBlobAssetReference<Clip>(Allocator.Persistent);
                blobBuilder.Dispose();

                var clipInstance = ClipInstance.Create(sourceClipInstance.Value.RigDefinition, clipRef);

                var deltaTime = 1.0f / clipInstance.Value.Clip.SampleRate;
                set.SetData(clipNode, UberClipNode.KernelPorts.DeltaTime, deltaTime);

                var frameCount = clipInstance.Value.Clip.FrameCount;

                for (var frameIter = 0; frameIter <= frameCount; frameIter++)
                {
                    var time = frameIter < frameCount ? (float)frameIter / clipInstance.Value.Clip.SampleRate : clipInstance.Value.Clip.Duration;
                    set.SetData(clipNode, UberClipNode.KernelPorts.Time, time);

                    animationSystem.Update();

                    var stream = AnimationStreamProvider.Create(
                        sourceClipInstance.Value.RigDefinition,
                        entityManager.GetBuffer<AnimatedLocalTranslation>(entity),
                        entityManager.GetBuffer<AnimatedLocalRotation>(entity),
                        entityManager.GetBuffer<AnimatedLocalScale>(entity),
                        entityManager.GetBuffer<AnimatedFloat>(entity),
                        entityManager.GetBuffer<AnimatedInt>(entity)
                    );

                    UpdateFrame(ref clipInstance.Value, ref clipRef.Value.Samples, ref stream, frameIter);
                }

                clipInstance.Dispose();

                entityManager.RemoveComponent<GraphOutput>(entity);

                set.ReleaseGraphValue(output.Buffer);
                set.Destroy(clipNode);

                animationSystem.RemoveRef();

                return clipRef;
            }
        }
    }
}
