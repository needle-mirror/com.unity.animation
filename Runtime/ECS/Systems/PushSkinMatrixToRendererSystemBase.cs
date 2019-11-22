using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Animation
{
    public abstract class PushSkinMatrixToRendererSystemBase : ComponentSystem
    {
        EntityQuery m_Query;
        ComputeBuffer m_Buffer;

        protected override void OnCreate()
        {
            m_Query = GetEntityQuery(
                ComponentType.ReadWrite<SkinMatrix>(),
                ComponentType.ReadWrite<BoneIndexOffset>()
                );
            m_Buffer = new ComputeBuffer(2048, UnsafeUtility.SizeOf<float3x4>(), ComputeBufferType.Default);
        }

        protected override unsafe void OnUpdate()
        {
            var skinMatrix = GetArchetypeChunkBufferType<SkinMatrix>();
            var chunks = m_Query.CreateArchetypeChunkArray(Allocator.TempJob);
            var matrixOffsetType = GetArchetypeChunkComponentType<BoneIndexOffset>();

            var numElements = 0;
            foreach (var chunk in chunks)
            {
                for (int i = 0; i != chunk.Count; i++)
                {
                    var skinMatricesBuf = chunk.GetBufferAccessor(skinMatrix);
                    var skinMatrixArray = skinMatricesBuf[i].Reinterpret<float3x4>();
                    numElements += skinMatrixArray.Length;
                }
            }

            if (m_Buffer.count < numElements)
            {
                m_Buffer.Dispose();
                m_Buffer = new ComputeBuffer(numElements, UnsafeUtility.SizeOf<float3x4>(), ComputeBufferType.Default);
            }

            var allSkinMatrices = new NativeArray<float3x4>(m_Buffer.count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            int offset = 0;
            int matrixCount = 0;
            foreach (var chunk in chunks)
            {
                var skinMatricesBuf = chunk.GetBufferAccessor(skinMatrix);
                var matrixOffset = chunk.GetNativeArray(matrixOffsetType);

                for (int i = 0; i != chunk.Count; i++)
                {
                    var skinMatrixArray = skinMatricesBuf[i].Reinterpret<float3x4>();
                    if (matrixCount + skinMatrixArray.Length >= m_Buffer.count)
                        continue;

                    matrixCount += skinMatrixArray.Length;

                    UnsafeUtility.MemCpy(
                        (float3x4*)allSkinMatrices.GetUnsafePtr() + offset,
                        skinMatrixArray.GetUnsafePtr(),
                        skinMatrixArray.Length * UnsafeUtility.SizeOf<float3x4>()
                        );

                    matrixOffset[i] = new BoneIndexOffset
                    {
                        Value = offset
                    };
                    offset += skinMatrixArray.Length;
                }
            }

            m_Buffer.SetData(allSkinMatrices, 0, 0, offset);
            Shader.SetGlobalBuffer("_SkinMatrices", m_Buffer);
            allSkinMatrices.Dispose();
            chunks.Dispose();
        }

        protected override void OnDestroy()
        {
            m_Buffer.Dispose();
        }
    }
}
