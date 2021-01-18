using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

namespace Unity.Animation
{
    public static class BoneRendererUtils
    {
        public enum BoneShape
        {
            Line,
            Pyramid,
            Box
        }

        public enum SubMeshType
        {
            BoneFaces,
            BoneWires,
            Count
        }

        static Material s_MaterialWire;
        static Material s_MaterialFace;
        static Mesh s_LineMesh;
        static Mesh s_PyramidMesh;
        static Mesh s_BoxMesh;

        const float k_Epsilon = 1e-5f;
        const float k_BoneBaseSize = 0.05f;

        [BurstCompatible]
        public static float4x4 ComputeBoneMatrix(float3 start, float3 end, float size = 1.0f)
        {
            var lengthsq = math.distancesq(start, end);
            if (lengthsq < k_Epsilon)
                return float4x4.zero;

            var length = math.sqrt(lengthsq);
            var direction = (end - start) / length;

            var tangent = math.cross(direction, math.up());
            if (math.lengthsq(tangent) < 0.1f)
            {
                tangent = math.cross(direction, math.right());
            }
            tangent = math.normalize(tangent);

            var bitangent = math.cross(direction, tangent);

            float scale = length * k_BoneBaseSize * size;

            return new float4x4(
                new float4(tangent * scale, 0f),
                new float4(direction * length, 0f),
                new float4(bitangent * scale, 0f),
                new float4(start, 1f)
            );
        }

        public static Material GetBoneFaceMaterial()
        {
            if (!s_MaterialFace)
            {
                s_MaterialFace = new Material(Shader.Find("Hidden/BoneRenderer"));
                s_MaterialFace.hideFlags = HideFlags.DontSaveInEditor;
                s_MaterialFace.enableInstancing = true;
                s_MaterialFace.DisableKeyword("WIRE_ON");
            }

            return s_MaterialFace;
        }

        public static Material GetBoneWireMaterial()
        {
            if (!s_MaterialWire)
            {
                s_MaterialWire = new Material(Shader.Find("Hidden/BoneRenderer"));
                s_MaterialWire.hideFlags = HideFlags.DontSaveInEditor;
                s_MaterialWire.enableInstancing = true;
                s_MaterialWire.EnableKeyword("WIRE_ON");
            }

            return s_MaterialWire;
        }

        #region Bone static meshes

        public static Mesh GetBoneMesh(BoneShape shape)
        {
            switch (shape)
            {
                case BoneShape.Pyramid:
                    return GetPyramidMesh();

                case BoneShape.Box:
                    return GetBoxMesh();

                case BoneShape.Line:
                default:
                    return GetLineMesh();
            }
        }

        static Mesh GetLineMesh()
        {
            if (s_LineMesh == null)
            {
                s_LineMesh = new Mesh();
                s_LineMesh.name = "BoneRendererLineMesh";
                s_LineMesh.subMeshCount = (int)SubMeshType.Count;
                s_LineMesh.hideFlags = HideFlags.DontSave;

                // Bone vertices
                Vector3[] vertices = new Vector3[]
                {
                    new Vector3(0.0f, 0.0f, 0.0f),
                    new Vector3(0.0f, 1.0f, 0.0f),
                };

                s_LineMesh.vertices = vertices;

                int[] boneWireIndices = new int[]
                {
                    0, 1
                };
                s_LineMesh.SetIndices(boneWireIndices, MeshTopology.Lines, (int)SubMeshType.BoneFaces);
                s_LineMesh.SetIndices(boneWireIndices, MeshTopology.Lines, (int)SubMeshType.BoneWires);
            }

            return s_LineMesh;
        }

        static Mesh GetPyramidMesh()
        {
            if (s_PyramidMesh == null)
            {
                s_PyramidMesh = new Mesh();
                s_PyramidMesh.name = "BoneRendererPyramidMesh";
                s_PyramidMesh.subMeshCount = (int)SubMeshType.Count;
                s_PyramidMesh.hideFlags = HideFlags.DontSave;

                // Bone vertices
                Vector3[] vertices = new Vector3[]
                {
                    new Vector3(0.0f, 1.0f, 0.0f),
                    new Vector3(0.0f, 0.0f, -1.0f),
                    new Vector3(-0.9f, 0.0f, 0.5f),
                    new Vector3(0.9f, 0.0f, 0.5f),
                };

                s_PyramidMesh.vertices = vertices;

                // Build indices for different sub meshes
                int[] boneFaceIndices = new int[]
                {
                    0, 2, 1,
                    0, 1, 3,
                    0, 3, 2,
                    1, 2, 3
                };
                s_PyramidMesh.SetIndices(boneFaceIndices, MeshTopology.Triangles, (int)SubMeshType.BoneFaces);

                int[] boneWireIndices = new int[]
                {
                    0, 1, 0, 2, 0, 3, 1, 2, 2, 3, 3, 1
                };
                s_PyramidMesh.SetIndices(boneWireIndices, MeshTopology.Lines, (int)SubMeshType.BoneWires);
            }

            return s_PyramidMesh;
        }

        static Mesh GetBoxMesh()
        {
            if (s_BoxMesh == null)
            {
                s_BoxMesh = new Mesh();
                s_BoxMesh.name = "BoneRendererBoxMesh";
                s_BoxMesh.subMeshCount = (int)SubMeshType.Count;
                s_BoxMesh.hideFlags = HideFlags.DontSave;

                // Bone vertices
                Vector3[] vertices = new Vector3[]
                {
                    new Vector3(-0.5f, 0.0f, 0.5f),
                    new Vector3(0.5f, 0.0f, 0.5f),
                    new Vector3(0.5f, 0.0f, -0.5f),
                    new Vector3(-0.5f, 0.0f, -0.5f),
                    new Vector3(-0.5f, 1.0f, 0.5f),
                    new Vector3(0.5f, 1.0f, 0.5f),
                    new Vector3(0.5f, 1.0f, -0.5f),
                    new Vector3(-0.5f, 1.0f, -0.5f)
                };

                s_BoxMesh.vertices = vertices;

                // Build indices for different sub meshes
                int[] boneFaceIndices = new int[]
                {
                    0, 2, 1,
                    0, 3, 2,

                    0, 1, 5,
                    0, 5, 4,

                    1, 2, 6,
                    1, 6, 5,

                    2, 3, 7,
                    2, 7, 6,

                    3, 0, 4,
                    3, 4, 7,

                    4, 5, 6,
                    4, 6, 7
                };
                s_BoxMesh.SetIndices(boneFaceIndices, MeshTopology.Triangles, (int)SubMeshType.BoneFaces);

                int[] boneWireIndices = new int[]
                {
                    0, 1, 1, 2, 2, 3, 3, 0,
                    4, 5, 5, 6, 6, 7, 7, 4,
                    0, 4, 1, 5, 2, 6, 3, 7
                };
                s_BoxMesh.SetIndices(boneWireIndices, MeshTopology.Lines, (int)SubMeshType.BoneWires);
            }

            return s_BoxMesh;
        }

        #endregion
    }
}
