using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(category: "Animation Core/Utils", description: "Convert LocalToWorld component data to float4x4")]
    public class ConvertLocalToWorldComponentToFloat4x4Node : ConvertToBase<
        ConvertLocalToWorldComponentToFloat4x4Node,
        LocalToWorld,
        float4x4,
        ConvertLocalToWorldComponentToFloat4x4Node.Kernel
    >
    {
        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports) =>
                ctx.Resolve(ref ports.Output) = ctx.Resolve(ports.Input).Value;
        }
    }

    [NodeDefinition(category: "Animation Core/Utils", description: "Convert float4x4 to LocalToWorld component data")]
    public class ConvertFloat4x4ToLocalToWorldComponentNode : ConvertToBase<
        ConvertFloat4x4ToLocalToWorldComponentNode,
        float4x4,
        LocalToWorld,
        ConvertFloat4x4ToLocalToWorldComponentNode.Kernel
    >
    {
        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports) =>
                ctx.Resolve(ref ports.Output).Value = ctx.Resolve(ports.Input);
        }
    }

    [NodeDefinition(category: "Animation Core/Utils", description: "Convert LocalToParent component data to float4x4")]
    public class ConvertLocalToParentComponentToFloat4x4Node : ConvertToBase<
        ConvertLocalToParentComponentToFloat4x4Node,
        LocalToParent,
        float4x4,
        ConvertLocalToParentComponentToFloat4x4Node.Kernel
    >
    {
        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports) =>
                ctx.Resolve(ref ports.Output) = ctx.Resolve(ports.Input).Value;
        }
    }

    [NodeDefinition(category: "Animation Core/Utils", description: "Convert float4x4 to LocalToParent component data")]
    public class ConvertFloat4x4ToLocalToParentComponentNode : ConvertToBase<
        ConvertFloat4x4ToLocalToParentComponentNode,
        float4x4,
        LocalToParent,
        ConvertFloat4x4ToLocalToParentComponentNode.Kernel
    >
    {
        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports) =>
                ctx.Resolve(ref ports.Output).Value = ctx.Resolve(ports.Input);
        }
    }
}
