using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    [NodeDefinition(guid: "be1a7512286b41479d93a3e4e1db9534", version: 1, category: "Animation Core/Utils", description: "Gets a value given an index from a buffer")]
    public class GetBufferElementValueNode
        : NodeDefinition<GetBufferElementValueNode.Data, GetBufferElementValueNode.SimPorts, GetBufferElementValueNode.KernelData, GetBufferElementValueNode.KernelDefs, GetBufferElementValueNode.Kernel>
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.GetBufferElementValueNode");
#endif

        public struct SimPorts : ISimulationPortDefinition
        {
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "bfb9c7c192af4e0b9e01a20ae9de6713", description: "Input buffer")]
            public DataInput<GetBufferElementValueNode, Buffer<float>> Input;
            [PortDefinition(guid: "f58169dc597a4d788909b525a723496e", description: "Index in buffer")]
            public DataInput<GetBufferElementValueNode, int> Index;

            [PortDefinition(guid: "28cb77b3c65c49fabe33810d9665efa5", description: "Value")]
            public DataOutput<GetBufferElementValueNode, float> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfileMarker;
#endif
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var buffer = context.Resolve(in ports.Input);
                var index = context.Resolve(in ports.Index);
                if (index < 0 || index >= buffer.Length)
                    throw new System.IndexOutOfRangeException("BufferElementToPortNode: Index's port value is out of range for Input port buffer");

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileMarker.Begin();
#endif
                context.Resolve(ref ports.Output) = buffer[index];

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileMarker.End();
#endif
            }
        }

#if !UNITY_DISABLE_ANIMATION_PROFILING
        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMarker = k_ProfileMarker;
        }

#endif
    }
}
