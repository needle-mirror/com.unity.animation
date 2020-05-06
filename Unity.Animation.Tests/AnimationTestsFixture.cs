using System.Collections.Generic;
using Unity.Animation.Hybrid;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using UnityEngine;
using System;

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

        protected PreAnimationGraphSystem m_PreAnimationGraph;
        protected PostAnimationGraphSystem m_PostAnimationGraph;

        protected NodeSet PreSet => m_PreAnimationGraph.Set;
        protected NodeSet PostSet => m_PostAnimationGraph.Set;

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

        List<Tuple<NodeHandle, NodeSet>> m_Nodes;
        List<Tuple<IDisposableGraphValue, NodeSet>> m_GraphValues;
        List<GameObject> m_GameObjects;
        List<UnityEngine.Object> m_Objects;

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
            m_Nodes = new List<Tuple<NodeHandle, NodeSet>>(200);
            m_GraphValues = new List<Tuple<IDisposableGraphValue, NodeSet>>(200);
            m_GameObjects = new List<GameObject>();
            m_Objects = new List<UnityEngine.Object>();
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

            m_PreAnimationGraph = World.GetOrCreateSystem<PreAnimationGraphSystem>();
            m_PreAnimationGraph.AddRef();

            m_PostAnimationGraph = World.GetOrCreateSystem<PostAnimationGraphSystem>();
            m_PostAnimationGraph.AddRef();
        }

        [TearDown]
        protected virtual void TearDown()
        {
            DestroyNodesAndGraphBuffers();
            DestroyObjects();
            m_AnimationGraphSystem.RemoveRef();
            m_PreAnimationGraph.RemoveRef();
            m_PostAnimationGraph.RemoveRef();

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

        protected GameObject CreateGameObject()
        {
            var go = new GameObject();
            m_GameObjects.Add(go);
            return go;
        }

        protected AnimationClip CreateAnimationClip()
        {
            var animationClip = new AnimationClip();
            m_Objects.Add(animationClip);
            return animationClip;
        }

        protected NodeHandle<T> CreateNode<T>()
            where T : NodeDefinition, new()
        {
            return CreateNode<T>(Set);
        }

        protected NodeHandle<T> CreateNode<T>(NodeSet set)
            where T : NodeDefinition, new()
        {
            var node = set.Create<T>();
            m_Nodes.Add(Tuple.Create((NodeHandle)node, set));
            return node;
        }

        protected NodeHandle<ComponentNode> CreateComponentNode(Entity entity)
        {
            return CreateComponentNode(entity, Set);
        }

        protected NodeHandle<ComponentNode> CreateComponentNode(Entity entity, NodeSet set)
        {
            var node = set.CreateComponentNode(entity);
            m_Nodes.Add(Tuple.Create((NodeHandle)node, set));
            return node;
        }

        protected GraphValue<TData> CreateGraphValue<TData, TDefinition>(NodeHandle<TDefinition> node, DataOutput<TDefinition, TData> output)
            where TDefinition : NodeDefinition
            where TData : struct
        {
            return CreateGraphValue(node, output, Set);
        }

        protected GraphValue<TData> CreateGraphValue<TData, TDefinition>(NodeHandle<TDefinition> node, DataOutput<TDefinition, TData> output, NodeSet set)
            where TDefinition : NodeDefinition
            where TData : struct
        {
            var gv = new DisposableGraphValue<TData> { Value = set.CreateGraphValue(node, output) };
            m_GraphValues.Add(Tuple.Create((IDisposableGraphValue)gv, set));
            return gv.Value;
        }

        protected void DestroyNodesAndGraphBuffers()
        {
            for (var i = 0; i < m_Nodes.Count; ++i)
                m_Nodes[i].Item2.Destroy(m_Nodes[i].Item1);
            m_Nodes.Clear();

            for (var i = 0; i < m_GraphValues.Count; ++i)
                m_GraphValues[i].Item1.Dispose(m_GraphValues[i].Item2);
            m_GraphValues.Clear();
        }

        protected void DestroyObjects()
        {
            for (var i = 0; i < m_GameObjects.Count; ++i)
                GameObject.DestroyImmediate(m_GameObjects[i]);

            for (var i = 0; i < m_Objects.Count; ++i)
                UnityEngine.Object.DestroyImmediate(m_Objects[i]);
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
                var(relativePath, tra) = translations[i];
                AddTranslationConstantCurves(clip, relativePath, tra);
            }

            for (var i = 0; i < rotations.Length; ++i)
            {
                var(relativePath, rot) = rotations[i];
                AddRotationConstantCurves(clip, relativePath, rot);
            }

            for (var i = 0; i < scales.Length; ++i)
            {
                var(relativePath, scale) = scales[i];
                AddScaleConstantCurves(clip, relativePath, scale);
            }

            return clip.ToDenseClip();
        }

        protected static BlobAssetReference<Clip> CreateConstantDenseClip((string, float3)[] translations, (string, quaternion)[] rotations, (string, float3)[] scales, (string, float)[] floats, (string, int)[] integers)
        {
            var clip = new AnimationClip();
            for (var i = 0; i < translations.Length; ++i)
            {
                var(relativePath, tra) = translations[i];
                AddTranslationConstantCurves(clip, relativePath, tra);
            }

            for (var i = 0; i < rotations.Length; ++i)
            {
                var(relativePath, rot) = rotations[i];
                AddRotationConstantCurves(clip, relativePath, rot);
            }

            for (var i = 0; i < scales.Length; ++i)
            {
                var(relativePath, scale) = scales[i];
                AddScaleConstantCurves(clip, relativePath, scale);
            }

            for (var i = 0; i < floats.Length; ++i)
            {
                var(relativePath, floatValue) = floats[i];
                AddFloatConstantCurves(clip, relativePath, floatValue);
            }

            for (var i = 0; i < integers.Length; ++i)
            {
                var(relativePath, intValue) = integers[i];
                AddIntegerConstantCurves(clip, relativePath, intValue);
            }

            return clip.ToDenseClip();
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
                AddTranslationLinearCurves(clip, translations[i], timeStart, timeStop);
            for (var i = 0; i < rotations.Length; ++i)
                AddRotationLinearCurves(clip, rotations[i], timeStart, timeStop);
            for (var i = 0; i < scales.Length; ++i)
                AddScaleLinearCurves(clip, scales[i], timeStart, timeStop);

            return clip.ToDenseClip();
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
                AddTranslationLinearCurves(clip, translations[i], timeStart, timeStop);
            for (var i = 0; i < rotations.Length; ++i)
                AddRotationLinearCurves(clip, rotations[i], timeStart, timeStop);
            for (var i = 0; i < scales.Length; ++i)
                AddScaleLinearCurves(clip, scales[i], timeStart, timeStop);
            for (var i = 0; i < floats.Length; ++i)
                AddFloatLinearCurves(clip, floats[i], timeStart, timeStop);
            for (var i = 0; i < integers.Length; ++i)
                AddIntegerLinearCurves(clip, integers[i], timeStart, timeStop);

            return clip.ToDenseClip();
        }

#endif
    }
}