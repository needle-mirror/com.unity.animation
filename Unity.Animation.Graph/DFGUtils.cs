using Unity.DataFlowGraph;
using Unity.Jobs;
using Unity.Collections;

namespace Unity.Animation
{
    public static class DFGUtils
    {
        internal struct GraphValueBufferGetSizeJob : IJob
        {
            public GraphValue<Buffer<AnimatedData>> GraphValueBuffer;
            public GraphValueResolver GraphValueResolver;
            public NativeArray<int> Result;

            public void Execute()
            {
                Result[0] = GraphValueResolver.Resolve(GraphValueBuffer).Length;
            }
        }

        internal struct GraphValueBufferReadbackJob : IJob
        {
            public GraphValue<Buffer<AnimatedData>> GraphValueBuffer;
            public GraphValueResolver GraphValueResolver;
            public NativeArray<AnimatedData> Result;

            public void Execute()
            {
                var buffer = GraphValueResolver.Resolve(GraphValueBuffer);
                Result.CopyFrom(buffer);
            }
        }

        public static NativeArray<AnimatedData> GetGraphValueTempNativeBuffer(NodeSet set, GraphValue<Buffer<AnimatedData>> graphValueBuffer)
        {
            var resolver = set.GetGraphValueResolver(out var valueResolverDeps);

            int readbackSize;
            using (var result = new NativeArray<int>(1, Allocator.TempJob))
            {
                var readbackSizeJob = new GraphValueBufferGetSizeJob()
                {
                    GraphValueBuffer = graphValueBuffer,
                    GraphValueResolver = resolver,
                    Result = result
                };
                var deps = readbackSizeJob.Schedule(valueResolverDeps);
                set.InjectDependencyFromConsumer(deps);
                deps.Complete();
                readbackSize = result[0];
            }

            using (var result = new NativeArray<AnimatedData>(readbackSize, Allocator.TempJob))
            {
                var readbackJob = new GraphValueBufferReadbackJob()
                {
                    GraphValueBuffer = graphValueBuffer,
                    GraphValueResolver = resolver,
                    Result = result
                };
                var deps = readbackJob.Schedule(valueResolverDeps);
                set.InjectDependencyFromConsumer(deps);
                deps.Complete();

                return new NativeArray<AnimatedData>(result, Allocator.Temp);
            }
        }
    }
}
