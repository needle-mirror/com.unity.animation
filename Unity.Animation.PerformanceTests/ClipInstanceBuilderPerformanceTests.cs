using NUnit.Framework;

using Unity.Mathematics;
using Unity.Animation.Hybrid;
using UnityEngine;

using Unity.Animation.Tests;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;
using Unity.Entities;

namespace Unity.Animation.PerformanceTests
{
    [Category("Performance"), Category("Animation")]
    public class ClipInstanceBuilderPerformanceTests : AnimationTestsFixture, IPrebuildSetup
    {
        protected static int[] s_BoneCount =
        {
            100, 200, 400, 800, 1600
        };

        readonly static string[] blobPath = new string[]
        {
            "ClipInstanceBuilderClipWith100Bones.blob",
            "ClipInstanceBuilderClipWith200Bones.blob",
            "ClipInstanceBuilderClipWith400Bones.blob",
            "ClipInstanceBuilderClipWith800Bones.blob",
            "ClipInstanceBuilderClipWith1600Bones.blob"
        };

#if UNITY_EDITOR
        BlobAssetReference<Clip> CreateClip(int boneCount)
        {
            var clip = new AnimationClip();
            for (int i = 1; i < boneCount; ++i)
            {
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalPosition.x", GetConstantCurve(1f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalPosition.y", GetConstantCurve(2f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalPosition.z", GetConstantCurve(3f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalRotation.x", GetConstantCurve(0f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalRotation.y", GetConstantCurve(0.5f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalRotation.z", GetConstantCurve(0f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalRotation.w", GetConstantCurve(0.5f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalScale.x", GetConstantCurve(1f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalScale.y", GetConstantCurve(1f));
                clip.SetCurve($"{i}", typeof(Transform), "m_LocalScale.z", GetConstantCurve(1f));
            }

            return clip.ToDenseClip();
        }

#endif
        public void Setup()
        {
#if UNITY_EDITOR
            PlaymodeTestsEditorSetup.CreateStreamingAssetsDirectory();

            for (int i = 0; i < s_BoneCount.Length; i++)
            {
                var denseClip = CreateClip(s_BoneCount[i]);
                BlobFile.WriteBlobAsset(ref denseClip, blobPath[i]);
                denseClip.Dispose();
            }
#endif
        }

        int GetFileIndex(int boneCount)
        {
            for (int i = 0; i < s_BoneCount.Length; i++)
            {
                if (s_BoneCount[i] == boneCount)
                    return i;
            }
            return 0;
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(800)]
        [TestCase(1600)]
        public void CreateClipInstance(int boneCount)
        {
            var skeleton = new SkeletonNode[boneCount];
            skeleton[0] = new SkeletonNode { Id = "Root", ParentIndex = -1, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1) };

            for (int i = 1; i < boneCount; ++i)
            {
                skeleton[i] = new SkeletonNode { Id = $"{i}", ParentIndex = i - 1, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1) };
            }

            var rigDefintion = RigBuilder.CreateRigDefinition(skeleton);
            var denseClip = BlobFile.ReadBlobAsset<Clip>(blobPath[GetFileIndex(boneCount)]);
            var clipInstance = default(BlobAssetReference<ClipInstance>);
            Measure.Method(
                () => clipInstance = ClipInstanceBuilder.Create(rigDefintion, denseClip)
            )
                .SampleGroup("ClipInstanceBuilder.Create")
                .WarmupCount(100)
                .MeasurementCount(500)
                .CleanUp(() =>
                {
                    clipInstance.Dispose();
                })
                .Run();

            denseClip.Dispose();
            rigDefintion.Dispose();
        }
    }
}
