using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using UnityEngine;

namespace Unity.Animation.Tests
{
    public class FloatAbsoluteEqualityComparer : IEqualityComparer<float>
    {
        private readonly float m_AllowedError;

        public FloatAbsoluteEqualityComparer(float allowedError)
        {
            m_AllowedError = allowedError;
        }

        public bool Equals(float expected, float actual)
        {
            return math.abs(expected - actual) < m_AllowedError;
        }

        public int GetHashCode(float value)
        {
            return 0;
        }
    }

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
            return (math.length(expected) == 0.0f && math.length(actual) == 0.0f) ||
                math.abs(math.dot(expected, actual)) > (1f - m_AllowedError);
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
        protected readonly FloatAbsoluteEqualityComparer FloatComparer;

        protected World World;
        protected EntityManager m_Manager;
        protected EntityManager.EntityManagerDebug m_ManagerDebug;

        // NOTE: Using the PreAnimationGraphSystem for all tests
        // if a two pass setup is eventually needed we'll have to
        // consider upgrading our test fixture to handle this
        protected PreAnimationGraphSystem m_AnimationGraphSystem;
        protected NodeSet Set => m_AnimationGraphSystem.Set;

        private interface IDisposableGraphValue
        {
            void Dispose(NodeSet set);
        }

        private class DisposableGraphValue<T> : IDisposableGraphValue
            where T : struct
        {
            public GraphValue<T> Value;

            public void Dispose(NodeSet set)
            {
                set.ReleaseGraphValue(Value);
            }
        }

        List<NodeHandle> m_Nodes;
        List<IDisposableGraphValue> m_GraphValues;

        public AnimationTestsFixture()
        {
            TranslationComparer = new Float3AbsoluteEqualityComparer(kTranslationTolerance);
            RotationComparer = new QuaternionAbsoluteEqualityComparer(kRotationTolerance);
            ScaleComparer = new Float3AbsoluteEqualityComparer(kScaleTolerance);
            FloatComparer = new FloatAbsoluteEqualityComparer(kTranslationTolerance);
        }

        [OneTimeSetUp]
        protected virtual void OneTimeSetUp()
        {
            m_Nodes = new List<NodeHandle>(200);
            m_GraphValues = new List<IDisposableGraphValue>(200);
        }

        [OneTimeTearDown]
        protected virtual void OneTimeTearDown()
        {
            ClipManager.Instance.Clear();
        }

        [SetUp]
        protected virtual void SetUp()
        {
            World = new World("Test World");

            m_Manager = World.EntityManager;
            m_ManagerDebug = new EntityManager.EntityManagerDebug(m_Manager);

            m_AnimationGraphSystem = World.GetOrCreateSystem<PreAnimationGraphSystem>();
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
                m_Manager = null;
            }
        }

        protected NodeHandle<T> CreateNode<T>()
            where T : NodeDefinition, new()
        {
            var node = Set.Create<T>();
            m_Nodes.Add(node);
            return node;
        }

        protected NodeHandle<ComponentNode> CreateComponentNode(Entity entity)
        {
            var node = Set.CreateComponentNode(entity);
            m_Nodes.Add(node);
            return node;
        }

        protected GraphValue<TData> CreateGraphValue<TData, TDefinition>(NodeHandle<TDefinition> node, DataOutput<TDefinition, TData> output)
            where TDefinition : NodeDefinition
            where TData : struct
        {
            var gv = new DisposableGraphValue<TData> { Value = Set.CreateGraphValue(node, output) };
            m_GraphValues.Add(gv);
            return gv.Value;
        }

        protected void DestroyNodesAndGraphBuffers()
        {
            for (var i = 0; i < m_Nodes.Count; ++i)
                Set.Destroy(m_Nodes[i]);
            m_Nodes.Clear();

            for (var i = 0; i < m_GraphValues.Count; ++i)
                m_GraphValues[i].Dispose(Set);
            m_GraphValues.Clear();
        }

#if UNITY_EDITOR
        protected static UnityEngine.AnimationCurve GetConstantCurve(float value)
        {
            return UnityEngine.AnimationCurve.Constant(0.0f, 1.0f, value);
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
            clip.SetCurve(relativePath, typeof(Animator), relativePath, GetConstantCurve(value));
        }

        protected static void AddIntegerConstantCurves(AnimationClip clip, string relativePath, int value)
        {
            clip.SetCurve(relativePath, typeof(UnityEngine.Animation), relativePath, GetConstantCurve((float)value));
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

        protected static UnityEngine.AnimationCurve GetLinearCurve(float valueStart, float valueEnd, float timeStart = 0, float timeStop = 1.0f)
        {
            return UnityEngine.AnimationCurve.Linear(timeStart, valueStart, timeStop, valueEnd);
        }

        protected static void AddTranslationLinearCurves(AnimationClip clip, LinearBinding<float3> binding, float timeStart = 0, float timeStop = 1.0f)
        {
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalPosition.x", GetLinearCurve(binding.ValueStart.x, binding.ValueEnd.x, timeStart, timeStop));
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalPosition.y", GetLinearCurve(binding.ValueStart.y, binding.ValueEnd.y, timeStart, timeStop));
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalPosition.z", GetLinearCurve(binding.ValueStart.z, binding.ValueEnd.z, timeStart, timeStop));
        }

        protected static void AddRotationLinearCurves(AnimationClip clip, LinearBinding<quaternion> binding, float timeStart = 0, float timeStop = 1.0f)
        {
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalRotation.x", GetLinearCurve(binding.ValueStart.value.x, binding.ValueEnd.value.x, timeStart, timeStop));
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalRotation.y", GetLinearCurve(binding.ValueStart.value.y, binding.ValueEnd.value.y, timeStart, timeStop));
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalRotation.z", GetLinearCurve(binding.ValueStart.value.z, binding.ValueEnd.value.z, timeStart, timeStop));
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalRotation.w", GetLinearCurve(binding.ValueStart.value.w, binding.ValueEnd.value.w, timeStart, timeStop));
        }

        protected static void AddScaleLinearCurves(AnimationClip clip, LinearBinding<float3> binding, float timeStart = 0, float timeStop = 1.0f)
        {
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalScale.x", GetLinearCurve(binding.ValueStart.x, binding.ValueEnd.x, timeStart, timeStop));
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalScale.y", GetLinearCurve(binding.ValueStart.y, binding.ValueEnd.y, timeStart, timeStop));
            clip.SetCurve(binding.Path, typeof(Transform), "m_LocalScale.z", GetLinearCurve(binding.ValueStart.z, binding.ValueEnd.z, timeStart, timeStop));
        }

        protected static void AddFloatLinearCurves(AnimationClip clip, LinearBinding<float> binding, float timeStart = 0, float timeStop = 1.0f)
        {
            clip.SetCurve(binding.Path, typeof(Animator), binding.Path, GetLinearCurve(binding.ValueStart, binding.ValueEnd, timeStart, timeStop));
        }

        protected static void AddIntegerLinearCurves(AnimationClip clip, LinearBinding<int> binding, float timeStart = 0, float timeStop = 1.0f)
        {
            clip.SetCurve(binding.Path, typeof(UnityEngine.Animation), binding.Path, GetLinearCurve((float)binding.ValueStart, (float)binding.ValueEnd, timeStart, timeStop));
        }

        protected static BlobAssetReference<Clip> CreateLinearDenseClip(
            LinearBinding<float3>[] translations,
            LinearBinding<quaternion>[] rotations,
            LinearBinding<float3>[] scales,
            float timeStart = 0, float timeStop = 1.0f)
        {
            var clip = new AnimationClip();

            for (var i = 0; i < translations.Length; ++i)
                AddTranslationLinearCurves(clip, translations[i],timeStart,timeStop);
            for (var i = 0; i < rotations.Length; ++i)
                AddRotationLinearCurves(clip, rotations[i],timeStart,timeStop);
            for (var i = 0; i < scales.Length; ++i)
                AddScaleLinearCurves(clip, scales[i],timeStart,timeStop);

            return ClipBuilder.AnimationClipToDenseClip(clip);
        }

        protected static BlobAssetReference<Clip> CreateLinearDenseClip(
            LinearBinding<float3>[] translations,
            LinearBinding<quaternion>[] rotations,
            LinearBinding<float3>[] scales,
            LinearBinding<float>[] floats,
            LinearBinding<int>[] integers,
            float timeStart = 0, float timeStop = 1.0f)
        {
            var clip = new AnimationClip();

            for (var i = 0; i < translations.Length; ++i)
                AddTranslationLinearCurves(clip, translations[i],timeStart,timeStop);
            for (var i = 0; i < rotations.Length; ++i)
                AddRotationLinearCurves(clip, rotations[i],timeStart,timeStop);
            for (var i = 0; i < scales.Length; ++i)
                AddScaleLinearCurves(clip, scales[i],timeStart,timeStop);
            for (var i = 0; i < floats.Length; ++i)
                AddFloatLinearCurves(clip, floats[i],timeStart,timeStop);
            for (var i = 0; i < integers.Length; ++i)
                AddIntegerLinearCurves(clip, integers[i],timeStart,timeStop);

            return ClipBuilder.AnimationClipToDenseClip(clip);
        }
#endif
    }
}
