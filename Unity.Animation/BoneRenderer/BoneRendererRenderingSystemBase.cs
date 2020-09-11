using System;
using System.Diagnostics;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

using UnityEngine;

namespace Unity.Animation
{
    public abstract class BoneRendererRenderingSystemBase : SystemBase
    {
        EntityQuery m_Query;

        const int kMaxDrawMeshInstanceCount = 1023;
        Matrix4x4[] m_Matrices;
        Vector4[] m_Colors;
        int m_ColorPropertyID;
        BoneRendererUtils.BoneShape[] m_BoneShapes;
        MaterialPropertyBlock m_PropertyBlock;

        protected override unsafe void OnCreate()
        {
            m_Query = GetEntityQuery(
                ComponentType.ReadOnly<BoneRenderer.BoneShape>(),
                ComponentType.ReadOnly<BoneRenderer.BoneColor>(),
                ComponentType.ReadOnly<BoneRenderer.BoneRendererEntity>()
            );

            ValidateMathTypeSizesAreEqual();

            m_Matrices = new Matrix4x4[kMaxDrawMeshInstanceCount];
            m_Colors = new Vector4[kMaxDrawMeshInstanceCount];
            m_ColorPropertyID = Shader.PropertyToID("_Color");
            m_BoneShapes = (BoneRendererUtils.BoneShape[])Enum.GetValues(typeof(BoneRendererUtils.BoneShape));
            m_PropertyBlock = new MaterialPropertyBlock();
        }

        protected override unsafe void OnUpdate()
        {
            CompleteDependency();

            var boneWireMaterial = BoneRendererUtils.GetBoneWireMaterial();
            var boneFaceMaterial = BoneRendererUtils.GetBoneFaceMaterial();

            // Pack the rendering for each mesh to benefit from DrawInstanced.
            foreach (var boneShape in m_BoneShapes)
            {
                m_Query.SetSharedComponentFilter(new BoneRenderer.BoneShape { Value = boneShape });
                var chunks = m_Query.CreateArchetypeChunkArray(Allocator.TempJob);

                var boneColorType = GetComponentTypeHandle<BoneRenderer.BoneColor>(true);
                var entityType = GetComponentTypeHandle<BoneRenderer.BoneRendererEntity>(true);

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

                        fixed(Matrix4x4* matricesPtr = &m_Matrices[0])
                        {
                            fixed(Vector4* colorsPtr = &m_Colors[0])
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

                                    m_PropertyBlock.SetVectorArray(m_ColorPropertyID, m_Colors);
                                    Graphics.DrawMeshInstanced(BoneRendererUtils.GetBoneMesh(boneShape), (int)BoneRendererUtils.SubMeshType.BoneFaces, boneFaceMaterial, m_Matrices, kMaxDrawMeshInstanceCount, m_PropertyBlock);
                                    Graphics.DrawMeshInstanced(BoneRendererUtils.GetBoneMesh(boneShape), (int)BoneRendererUtils.SubMeshType.BoneWires, boneWireMaterial, m_Matrices, kMaxDrawMeshInstanceCount, m_PropertyBlock);

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
                    m_PropertyBlock.SetVectorArray(m_ColorPropertyID, m_Colors);
                    Graphics.DrawMeshInstanced(BoneRendererUtils.GetBoneMesh(boneShape), (int)BoneRendererUtils.SubMeshType.BoneFaces, boneFaceMaterial, m_Matrices, destOffset, m_PropertyBlock);
                    Graphics.DrawMeshInstanced(BoneRendererUtils.GetBoneMesh(boneShape), (int)BoneRendererUtils.SubMeshType.BoneWires, boneWireMaterial, m_Matrices, destOffset, m_PropertyBlock);
                }

                m_Query.ResetFilter();
                chunks.Dispose();
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateMathTypeSizesAreEqual()
        {
            if (UnsafeUtility.SizeOf<float4>() != UnsafeUtility.SizeOf<Vector4>())
                throw new System.InvalidOperationException("Size mismatch between float4 and Vector4.");
            if (UnsafeUtility.SizeOf<float4x4>() != UnsafeUtility.SizeOf<Matrix4x4>())
                throw new System.InvalidOperationException("Size mismatch between float4x4 and Matrix4x4.");
        }
    }
}
