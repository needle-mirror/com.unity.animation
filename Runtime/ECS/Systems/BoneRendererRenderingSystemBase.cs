using System;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling;

using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Animation
{
    public abstract class BoneRendererRenderingSystemBase : ComponentSystem
    {
        EntityQuery m_Query;

        const int kMaxDrawMeshInstanceCount = 1023;
        Matrix4x4[] m_Matrices;
        Vector4[] m_Colors;

        static readonly ProfilerMarker k_Marker = new ProfilerMarker("BoneRendererRenderingSystemBase");

        protected override unsafe void OnCreate()
        {
            m_Query = GetEntityQuery(
                ComponentType.ReadOnly<BoneRenderer.BoneShape>(),
                ComponentType.ReadOnly<BoneRenderer.BoneColor>(),
                ComponentType.ReadOnly<BoneRenderer.BoneRendererEntity>()
            );

            Assert.AreEqual(UnsafeUtility.SizeOf<float4>(), sizeof(Vector4), "Size mismatch between float4 and Vector4.");
            Assert.AreEqual(UnsafeUtility.SizeOf<float4x4>(), sizeof(Matrix4x4), "Size mismatch between float4x4 and Matrix4x4.");

            m_Matrices = new Matrix4x4[kMaxDrawMeshInstanceCount];
            m_Colors = new Vector4[kMaxDrawMeshInstanceCount];
        }

        protected override unsafe void OnUpdate()
        {
            k_Marker.Begin();

            var boneShapes = (BoneRendererUtils.BoneShape[])Enum.GetValues(typeof(BoneRendererUtils.BoneShape));
            var propertyBlock = new MaterialPropertyBlock();
            var boneWireMaterial = BoneRendererUtils.GetBoneWireMaterial();
            var boneFaceMaterial = BoneRendererUtils.GetBoneFaceMaterial();

            // Pack the rendering for each mesh to benefit from DrawInstanced.
            foreach (var boneShape in boneShapes)
            {
                m_Query.SetSharedComponentFilter(new BoneRenderer.BoneShape { Value = boneShape });
                var chunks = m_Query.CreateArchetypeChunkArray(Allocator.TempJob);

                var boneColorType = GetArchetypeChunkComponentType<BoneRenderer.BoneColor>(true);
                var entityType = GetArchetypeChunkComponentType<BoneRenderer.BoneRendererEntity>(true);

                var srcOffset = 0;
                var destOffset = 0;

                foreach (var chunk in chunks)
                {
                    var boneColors = chunk.GetNativeArray(boneColorType);
                    var boneRendererEntities = chunk.GetNativeArray(entityType); 

                    for (int i = 0; i != chunk.Count; i++)
                    {
                        var matricesBuffer = EntityManager.GetBuffer<BoneRenderer.BoneWorldMatrix>(boneRendererEntities[i].Value);
                        var bufLen = matricesBuffer.Length;

                        fixed (Matrix4x4* matricesPtr = &m_Matrices[0])
                        {
                            fixed (Vector4* colorsPtr = &m_Colors[0])
                            {
                                while (destOffset + bufLen > kMaxDrawMeshInstanceCount)
                                {
                                    var copyCount = kMaxDrawMeshInstanceCount - destOffset;

                                    UnsafeUtility.MemCpy(matricesPtr + destOffset,
                                        (float4x4*)matricesBuffer.GetUnsafePtr() + srcOffset,
                                        copyCount * UnsafeUtility.SizeOf<float4x4>()
                                        );

                                    UnsafeUtility.MemCpyReplicate(colorsPtr + destOffset,
                                        (float4*)boneColors.GetUnsafeReadOnlyPtr() + i,
                                        UnsafeUtility.SizeOf<float4>(),
                                        copyCount
                                        );

                                    propertyBlock.SetVectorArray("_Color", m_Colors);
                                    Graphics.DrawMeshInstanced(BoneRendererUtils.GetBoneMesh(boneShape), (int)BoneRendererUtils.SubMeshType.BoneFaces, boneFaceMaterial, m_Matrices, kMaxDrawMeshInstanceCount, propertyBlock);
                                    Graphics.DrawMeshInstanced(BoneRendererUtils.GetBoneMesh(boneShape), (int)BoneRendererUtils.SubMeshType.BoneWires, boneWireMaterial, m_Matrices, kMaxDrawMeshInstanceCount, propertyBlock);

                                    bufLen -= copyCount;
                                    destOffset = 0;
                                    srcOffset += copyCount;
                                }

                                UnsafeUtility.MemCpy(matricesPtr + destOffset,
                                    (float4x4*)matricesBuffer.GetUnsafePtr() + srcOffset,
                                    bufLen * UnsafeUtility.SizeOf<float4x4>()
                                    );
                        
                                UnsafeUtility.MemCpyReplicate(colorsPtr + destOffset,
                                    (float4*)boneColors.GetUnsafeReadOnlyPtr() + i,
                                    UnsafeUtility.SizeOf<float4>(),
                                    bufLen
                                    );

                                srcOffset = 0;
                                destOffset += bufLen;
                            }
                        }
                    }
                }

                // Only do a draw call when there is something to draw.
                if (destOffset > 0)
                {
                    propertyBlock.SetVectorArray("_Color", m_Colors);
                    Graphics.DrawMeshInstanced(BoneRendererUtils.GetBoneMesh(boneShape), (int)BoneRendererUtils.SubMeshType.BoneFaces, boneFaceMaterial, m_Matrices, destOffset, propertyBlock);
                    Graphics.DrawMeshInstanced(BoneRendererUtils.GetBoneMesh(boneShape), (int)BoneRendererUtils.SubMeshType.BoneWires, boneWireMaterial, m_Matrices, destOffset, propertyBlock);
                }

                m_Query.ResetFilter();
                chunks.Dispose();
            }

            k_Marker.End();
        }
    }
}
