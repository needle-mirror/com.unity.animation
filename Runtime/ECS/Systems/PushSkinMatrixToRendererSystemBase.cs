using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

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
                ComponentType.ReadWrite<SkinMatrix>()
                );
            m_Buffer = new ComputeBuffer(1024, UnsafeUtility.SizeOf<float3x4>(), ComputeBufferType.Default);
        }

        protected override unsafe void OnUpdate()
        {
            var skinRenderer = GetArchetypeChunkSharedComponentType<SkinRenderer>();
            var skinMatrix = GetArchetypeChunkBufferType<SkinMatrix>();
            var chunks = m_Query.CreateArchetypeChunkArray(Allocator.TempJob);

            //@TODO: Handle when m_Buffer is too small...
            var allSkinMatrices = new NativeArray<float3x4>(m_Buffer.count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            int offset = 0;
            int matrixCount = 0;
            foreach (var chunk in chunks)
            {
                var renderer = EntityManager.GetSharedComponentData<SkinRenderer>(chunk.GetSharedComponentIndex(skinRenderer));
                var skinMatricesBuf = chunk.GetBufferAccessor(skinMatrix);

                if (renderer.Mesh == null)
                    continue;

                for (int i = 0; i != chunk.Count; i++)
                {
                    var skinMatrixArray = skinMatricesBuf[i].Reinterpret<float3x4>();
                    if (matrixCount + skinMatrixArray.Length >= 1024)
                        continue;

                    matrixCount += skinMatrixArray.Length;

                    UnsafeUtility.MemCpy(
                        (float3x4*)allSkinMatrices.GetUnsafePtr() + offset,
                        skinMatrixArray.GetUnsafePtr(),
                        skinMatrixArray.Length * UnsafeUtility.SizeOf<float3x4>()
                        );

                    var properties = new MaterialPropertyBlock();
                    properties.SetInt("_SkinMatricesOffset", offset);
                    properties.SetBuffer("_SkinMatrices", m_Buffer);

                    if (renderer.Material0)
                        Graphics.DrawMesh(renderer.Mesh, Matrix4x4.identity, renderer.Material0, renderer.Layer, null, 0, properties, renderer.CastShadows, renderer.ReceiveShadows);
                    if (renderer.Material1)
                        Graphics.DrawMesh(renderer.Mesh, Matrix4x4.identity, renderer.Material1, renderer.Layer, null, 1, properties, renderer.CastShadows, renderer.ReceiveShadows);

                    offset += skinMatrixArray.Length;
                }
            }

            m_Buffer.SetData(allSkinMatrices, 0, 0, offset);
            allSkinMatrices.Dispose();
            chunks.Dispose();
        }

        protected override void OnDestroy()
        {
            m_Buffer.Dispose();
        }
    }
}
