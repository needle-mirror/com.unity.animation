using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using UnityEngine;

namespace Unity.Animation.Tests
{
    public class Float3AbsoluteEqualityComparer : IEqualityComparer<float3>
    {
        private readonly float m_AllowedError;

        public Float3AbsoluteEqualityComparer(float allowedError)
        {
            m_AllowedError = allowedError;
        }

        public bool Equals(float3 expected, float3 actual)
        {
            return math.abs(expected.x - actual.x) < m_AllowedError &&
                   math.abs(expected.y - actual.y) < m_AllowedError &&
                   math.abs(expected.z - actual.z) < m_AllowedError;
        }

        public int GetHashCode(float3 value)
        {
            return 0;
        }
    }

    public class QuaternionAbsoluteEqualityComparer : IEqualityComparer<quaternion>
    {
        private readonly float m_AllowedError;

        public QuaternionAbsoluteEqualityComparer(float allowedError)
        {
            m_AllowedError = allowedError;
        }

        public bool Equals(quaternion expected, quaternion actual)
        {
            return math.abs(math.dot(expected, actual)) > (1f - m_AllowedError);
        }

        public int GetHashCode(quaternion value)
        {
            return 0;
        }
    }

    public abstract class AnimationTestsFixture
    {
        private const float kTranslationTolerance = 1e-5f;
        private const float kRotationTolerance = 1e-4f;
        private const float kScaleTolerance = 1e-5f;
        protected const float kTimeTolerance = 1e-5f;

        protected readonly Float3AbsoluteEqualityComparer TranslationComparer;
        protected readonly QuaternionAbsoluteEqualityComparer RotationComparer;
        protected readonly Float3AbsoluteEqualityComparer ScaleComparer;

        protected World m_PreviousWorld;
        protected World World;
        protected EntityManager m_Manager;
        protected EntityManager.EntityManagerDebug m_ManagerDebug;

        protected AnimationGraphSystem m_AnimationGraphSystem;
        protected NodeSet Set => m_AnimationGraphSystem.Set;

        List<NodeHandle> m_Nodes;
        List<GraphValue<Buffer<float>>> m_GraphValueBuffers;
        List<GraphValue<float>> m_GraphValues;

        public AnimationTestsFixture()
        {
            TranslationComparer = new Float3AbsoluteEqualityComparer(kTranslationTolerance);
            RotationComparer = new QuaternionAbsoluteEqualityComparer(kRotationTolerance);
            ScaleComparer = new Float3AbsoluteEqualityComparer(kScaleTolerance);
        }

        [OneTimeSetUp]
        protected virtual void OneTimeSetUp()
        {
            m_Nodes = new List<NodeHandle>(200);
            m_GraphValueBuffers = new List<GraphValue<Buffer<float>>>(200);
            m_GraphValues = new List<GraphValue<float>>(200);
        }

        [OneTimeTearDown]
        protected virtual void OneTimeTearDown()
        {
        }

        [SetUp]
        protected virtual void SetUp()
        {
            m_PreviousWorld = World.Active;
            World = World.Active = new World("Test World");

            m_Manager = World.EntityManager;
            m_ManagerDebug = new EntityManager.EntityManagerDebug(m_Manager);

            m_AnimationGraphSystem = World.GetOrCreateSystem<AnimationGraphSystem>();
            m_AnimationGraphSystem.AddRef();
        }

        [TearDown]
        protected virtual void TearDown()
        {
            DestroyNodesAndGraphBuffers();
            m_AnimationGraphSystem.RemoveRef();

            if (m_Manager != null)
            {
                // Clean up systems before calling CheckInternalConsistency because we might have filters etc
                // holding on SharedComponentData making checks fail
                var system = World.GetExistingSystem<ComponentSystemBase>();
                while (system != null)
                {
                    World.DestroySystem(system);
                    system = World.GetExistingSystem<ComponentSystemBase>();
                }

                m_ManagerDebug.CheckInternalConsistency();

                World.Dispose();
                World = null;

                World.Active = m_PreviousWorld;
                m_PreviousWorld = null;
                m_Manager = null;
            }
        }

        protected NodeHandle<T> CreateNode<T>()
            where T : INodeDefinition, new()
        {
            var node = Set.Create<T>();
            m_Nodes.Add(node);
            return node;
        }

        protected GraphValue<Buffer<float>> CreateGraphBuffer<T>(NodeHandle<T> node, DataOutput<T, Buffer<float>> output)
            where T : INodeDefinition
        {
            var graphValue = Set.CreateGraphValue(node, output);
            m_GraphValueBuffers.Add(graphValue);
            return graphValue;
        }

        struct GraphValueBufferGetSizeJob : IJob
        {
            public GraphValue<Buffer<float>> GraphValueBuffer;
            public GraphValueResolver GraphValueResolver;
            public NativeArray<int> Result;

            public void Execute()
            {
                Result[0] = GraphValueResolver.Resolve(GraphValueBuffer).Length;
            }
        }

        struct GraphValueBufferReadbackJob : IJob
        {
            public GraphValue<Buffer<float>> GraphValueBuffer;
            public GraphValueResolver GraphValueResolver;
            public NativeArray<float> Result;

            public void Execute()
            {
                var buffer = GraphValueResolver.Resolve(GraphValueBuffer);
                Result.CopyFrom(buffer);
            }
        }

        protected NativeArray<float> GetGraphValueTempNativeBuffer(GraphValue<Buffer<float>> graphValueBuffer)
        {
            var resolver = Set.GetGraphValueResolver(out var valueResolverDeps);

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
                Set.InjectDependencyFromConsumer(deps);
                deps.Complete();
                readbackSize = result[0];
            }

            using (var result = new NativeArray<float>(readbackSize, Allocator.TempJob))
            {
                var readbackJob = new GraphValueBufferReadbackJob()
                {
                    GraphValueBuffer = graphValueBuffer,
                    GraphValueResolver = resolver,
                    Result = result
                };
                var deps = readbackJob.Schedule(valueResolverDeps);
                Set.InjectDependencyFromConsumer(deps);
                deps.Complete();

                return new NativeArray<float>(result, Allocator.Temp);
            }
        }

        protected GraphValue<float> CreateGraphValue<T>(NodeHandle<T> node, DataOutput<T, float> output)
            where T : INodeDefinition
        {
            var graphValue = Set.CreateGraphValue(node, output);
            m_GraphValues.Add(graphValue);
            return graphValue;
        }

        protected void DestroyNodesAndGraphBuffers()
        {
            var set = Set;

            for (var i = 0; i < m_Nodes.Count; ++i)
                set.Destroy(m_Nodes[i]);
            m_Nodes.Clear();

            for (var i = 0; i < m_GraphValueBuffers.Count; ++i)
                set.ReleaseGraphValue(m_GraphValueBuffers[i]);
            m_GraphValueBuffers.Clear();

            for (var i = 0; i < m_GraphValues.Count; ++i)
                set.ReleaseGraphValue(m_GraphValues[i]);
            m_GraphValues.Clear();
        }
#if UNITY_EDITOR
        protected static AnimationCurve GetConstantCurve(float value)
        {
            return AnimationCurve.Constant(0.0f, 1.0f, value);
        }

        protected static void AddTranslationConstantCurves(AnimationClip clip, string relativePath, float3 tra)
        {
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalPosition.x", GetConstantCurve(tra.x));
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalPosition.y", GetConstantCurve(tra.y));
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalPosition.z", GetConstantCurve(tra.z));
        }

        protected static void AddRotationConstantCurves(AnimationClip clip, string relativePath, quaternion rot)
        {
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.x", GetConstantCurve(rot.value.x));
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.y", GetConstantCurve(rot.value.y));
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.z", GetConstantCurve(rot.value.z));
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.w", GetConstantCurve(rot.value.w));
        }

        protected static void AddScaleConstantCurves(AnimationClip clip, string relativePath, float3 scale)
        {
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalScale.x", GetConstantCurve(scale.x));
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalScale.y", GetConstantCurve(scale.y));
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalScale.z", GetConstantCurve(scale.z));
        }

        protected static void AddFloatConstantCurves(AnimationClip clip, string relativePath, float value)
        {
            clip.SetCurve(relativePath, typeof(Transform), relativePath+".f", GetConstantCurve(value));
        }

        protected static void AddIntegerConstantCurves(AnimationClip clip, string relativePath, int value)
        {
            clip.SetCurve(relativePath, typeof(Transform), relativePath+".i", GetConstantCurve((float)value));
        }

        protected static BlobAssetReference<Clip> CreateConstantDenseClip((string, float3)[] translations, (string, quaternion)[] rotations, (string, float3)[] scales)
        {
            var clip = new AnimationClip();
            for (var i = 0; i < translations.Length; ++i)
            {
                var (relativePath, tra) = translations[i];
                AddTranslationConstantCurves(clip, relativePath, tra);
            }

            for (var i = 0; i < rotations.Length; ++i)
            {
                var (relativePath, rot) = rotations[i];
                AddRotationConstantCurves(clip, relativePath, rot);
            }

            for (var i = 0; i < scales.Length; ++i)
            {
                var (relativePath, scale) = scales[i];
                AddScaleConstantCurves(clip, relativePath, scale);
            }

            return ClipBuilder.AnimationClipToDenseClip(clip);
        }

        protected static BlobAssetReference<Clip> CreateConstantDenseClip((string, float3)[] translations, (string, quaternion)[] rotations, (string, float3)[] scales, (string, float)[] floats, (string, int)[] integers)
        {
            var clip = new AnimationClip();
            for (var i = 0; i < translations.Length; ++i)
            {
                var (relativePath, tra) = translations[i];
                AddTranslationConstantCurves(clip, relativePath, tra);
            }

            for (var i = 0; i < rotations.Length; ++i)
            {
                var (relativePath, rot) = rotations[i];
                AddRotationConstantCurves(clip, relativePath, rot);
            }

            for (var i = 0; i < scales.Length; ++i)
            {
                var (relativePath, scale) = scales[i];
                AddScaleConstantCurves(clip, relativePath, scale);
            }

            for (var i = 0; i < floats.Length; ++i)
            {
                var (relativePath, floatValue) = floats[i];
                AddFloatConstantCurves(clip, relativePath, floatValue);
            }

            for (var i = 0; i < integers.Length; ++i)
            {
                var (relativePath, intValue) = integers[i];
                AddIntegerConstantCurves(clip, relativePath, intValue);
            }

            return ClipBuilder.AnimationClipToDenseClip(clip);
        }

        protected struct LinearBinding<T>
            where T : struct
        {
            public string Path;
            public T ValueStart;
            public T ValueEnd;
        }

        protected static AnimationCurve GetLinearCurve(float valueStart, float valueEnd, float timeStart = 0, float timeStop = 1.0f)
        {
            return AnimationCurve.Linear(timeStart, valueStart, timeStop, valueEnd);
        }

        protected static void AddTranslationLinearCurves(AnimationClip clip, LinearBinding<float3> binding)
        {
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalPosition.x", GetLinearCurve(binding.ValueStart.x, binding.ValueEnd.x));
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalPosition.y", GetLinearCurve(binding.ValueStart.y, binding.ValueEnd.y));
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalPosition.z", GetLinearCurve(binding.ValueStart.z, binding.ValueEnd.z));
        }

        protected static void AddRotationLinearCurves(AnimationClip clip, LinearBinding<quaternion> binding)
        {
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalRotation.x", GetLinearCurve(binding.ValueStart.value.x, binding.ValueEnd.value.x));
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalRotation.y", GetLinearCurve(binding.ValueStart.value.y, binding.ValueEnd.value.y));
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalRotation.z", GetLinearCurve(binding.ValueStart.value.z, binding.ValueEnd.value.z));
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalRotation.w", GetLinearCurve(binding.ValueStart.value.w, binding.ValueEnd.value.w));
        }

        protected static void AddScaleLinearCurves(AnimationClip clip, LinearBinding<float3> binding)
        {
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalScale.x", GetLinearCurve(binding.ValueStart.x, binding.ValueEnd.x));
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalScale.y", GetLinearCurve(binding.ValueStart.y, binding.ValueEnd.y));
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalScale.z", GetLinearCurve(binding.ValueStart.z, binding.ValueEnd.z));
        }

        protected static void AddFloatLinearCurves(AnimationClip clip, LinearBinding<float> binding)
        {
            clip.SetCurve(binding.Path, typeof(Transform), binding.Path, GetLinearCurve(binding.ValueStart, binding.ValueEnd));
        }

        protected static void AddIntergerLinearCurves(AnimationClip clip, LinearBinding<int> binding)
        {
            clip.SetCurve(binding.Path, typeof(Transform), binding.Path, GetLinearCurve((float)binding.ValueStart, (float)binding.ValueEnd));
        }

        protected static BlobAssetReference<Clip> CreateLinearDenseClip(
            LinearBinding<float3>[] translations,
            LinearBinding<quaternion>[] rotations,
            LinearBinding<float3>[] scales)
        {
            var clip = new AnimationClip();

            for (var i = 0; i < translations.Length; ++i)
                AddTranslationLinearCurves(clip, translations[i]);
            for (var i = 0; i < rotations.Length; ++i)
                AddRotationLinearCurves(clip, rotations[i]);
            for (var i = 0; i < scales.Length; ++i)
                AddScaleLinearCurves(clip, scales[i]);

            return ClipBuilder.AnimationClipToDenseClip(clip);
        }

        protected static BlobAssetReference<Clip> CreateLinearDenseClip(
            LinearBinding<float3>[] translations,
            LinearBinding<quaternion>[] rotations,
            LinearBinding<float3>[] scales,
            LinearBinding<float>[] floats,
            LinearBinding<int>[] integers)
        {
            var clip = new AnimationClip();

            for (var i = 0; i < translations.Length; ++i)
                AddTranslationLinearCurves(clip, translations[i]);
            for (var i = 0; i < rotations.Length; ++i)
                AddRotationLinearCurves(clip, rotations[i]);
            for (var i = 0; i < scales.Length; ++i)
                AddScaleLinearCurves(clip, scales[i]);
            for (var i = 0; i < floats.Length; ++i)
                AddFloatLinearCurves(clip, floats[i]);
            for (var i = 0; i < integers.Length; ++i)
                AddIntergerLinearCurves(clip, integers[i]);

            return ClipBuilder.AnimationClipToDenseClip(clip);
        }
#endif
    }
}
