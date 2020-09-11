using NUnit.Framework;
using Unity.Animation.Hybrid;
using UnityEngine;

namespace Unity.Animation.Tests
{
    public class EvaluateCurveNodeTests : AnimationTestsFixture
    {
        [TestCase(-1)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.51f)]
        public void TestStepAnimationCurveNode(float time)
        {
            var animCurve = new UnityEngine.AnimationCurve();
            animCurve.AddKey(new UnityEngine.Keyframe(0, 0, Mathf.Infinity, Mathf.Infinity));
            animCurve.AddKey(new UnityEngine.Keyframe(0.5f, 1, Mathf.Infinity, Mathf.Infinity));
            animCurve.AddKey(new UnityEngine.Keyframe(1, 1, Mathf.Infinity, Mathf.Infinity));
            var expected = animCurve.Evaluate(time);

            var dotsCurve = animCurve.ToDotsAnimationCurve();

            var curveNode = CreateNode<EvaluateCurveNode>();
            Set.SendMessage(curveNode, EvaluateCurveNode.SimulationPorts.AnimationCurve, dotsCurve);
            Set.SetData(curveNode, EvaluateCurveNode.KernelPorts.Time, time);

            var output = CreateGraphValue(curveNode, EvaluateCurveNode.KernelPorts.Output);
            Set.Update(default);
            var value = Set.GetValueBlocking(output);

            Assert.That(value, Is.EqualTo(expected));

            dotsCurve.Dispose();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestLinearAnimationCurveNode(float time)
        {
            var animCurve = UnityEngine.AnimationCurve.Linear(0, 0, 1, 1);
            var expected = animCurve.Evaluate(time);

            var dotsCurve = animCurve.ToDotsAnimationCurve();

            var curveNode = CreateNode<EvaluateCurveNode>();
            Set.SendMessage(curveNode, EvaluateCurveNode.SimulationPorts.AnimationCurve, dotsCurve);
            Set.SetData(curveNode, EvaluateCurveNode.KernelPorts.Time, time);

            var output = CreateGraphValue(curveNode, EvaluateCurveNode.KernelPorts.Output);
            Set.Update(default);
            var value = Set.GetValueBlocking(output);

            Assert.That(value, Is.EqualTo(expected));

            dotsCurve.Dispose();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestEaseInOutAnimationCurveNode(float time)
        {
            var animCurve = UnityEngine.AnimationCurve.EaseInOut(0, 0, 1, 1);
            var expected = animCurve.Evaluate(time);

            var dotsCurve = animCurve.ToDotsAnimationCurve();

            var curveNode = CreateNode<EvaluateCurveNode>();
            Set.SendMessage(curveNode, EvaluateCurveNode.SimulationPorts.AnimationCurve, dotsCurve);
            Set.SetData(curveNode, EvaluateCurveNode.KernelPorts.Time, time);

            var output = CreateGraphValue(curveNode, EvaluateCurveNode.KernelPorts.Output);
            Set.Update(default);
            var value = Set.GetValueBlocking(output);

            Assert.That(value, Is.EqualTo(expected));

            dotsCurve.Dispose();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestConstantAnimationCurveNode(float time)
        {
            var animCurve = UnityEngine.AnimationCurve.Constant(0, 1, 1);
            var expected = animCurve.Evaluate(time);

            var dotsCurve = animCurve.ToDotsAnimationCurve();

            var curveNode = CreateNode<EvaluateCurveNode>();
            Set.SendMessage(curveNode, EvaluateCurveNode.SimulationPorts.AnimationCurve, dotsCurve);
            Set.SetData(curveNode, EvaluateCurveNode.KernelPorts.Time, time);

            var output = CreateGraphValue(curveNode, EvaluateCurveNode.KernelPorts.Output);
            Set.Update(default);
            var value = Set.GetValueBlocking(output);

            Assert.That(value, Is.EqualTo(expected));

            dotsCurve.Dispose();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestComplexAnimationCurveNode(float time)
        {
            var animCurve = UnityEngine.AnimationCurve.Linear(10, -10, 100, -10);

            // Only supports clamp
            animCurve.preWrapMode = WrapMode.Clamp;
            animCurve.postWrapMode = WrapMode.Clamp;

            animCurve.AddKey(new UnityEngine.Keyframe() { time = 50, value = 100 });
            animCurve.AddKey(new UnityEngine.Keyframe() { time = 75, value = -30 });
            animCurve.AddKey(new UnityEngine.Keyframe() { time = 25, value = -30 });

            var expected = animCurve.Evaluate(time);

            var dotsCurve = animCurve.ToDotsAnimationCurve();

            var curveNode = CreateNode<EvaluateCurveNode>();
            Set.SendMessage(curveNode, EvaluateCurveNode.SimulationPorts.AnimationCurve, dotsCurve);
            Set.SetData(curveNode, EvaluateCurveNode.KernelPorts.Time, time);

            var output = CreateGraphValue(curveNode, EvaluateCurveNode.KernelPorts.Output);
            Set.Update(default);
            var value = Set.GetValueBlocking(output);

            Assert.That(value, Is.EqualTo(expected));

            dotsCurve.Dispose();
        }

        [TestCase(0f)]
        [TestCase(0.25f)]
        [TestCase(0.5f)]
        [TestCase(0.75f)]
        [TestCase(2f)]
        [TestCase(5f)]
        [TestCase(10f)]
        public void TestBezierAnimationCurveNode(float time)
        {
            var animCurve = new UnityEngine.AnimationCurve(new Keyframe[]
            {
                new Keyframe(0, 0, 0, -0.5f, 0, 2.0f),
                new Keyframe { time = 2f, value = 1f, inTangent = 0f, outTangent = 10f, inWeight = 1.75f, outWeight = 0f, weightedMode = WeightedMode.In},
                new Keyframe { time = 10f, value = 0f, inTangent = -10f, outTangent = 0f, inWeight = 0f, outWeight = 0f, weightedMode = WeightedMode.None},
            });

            // Only supports clamp
            animCurve.preWrapMode = WrapMode.Clamp;
            animCurve.postWrapMode = WrapMode.Clamp;

            var expected = animCurve.Evaluate(time);

            var dotsCurve = animCurve.ToDotsAnimationCurve();

            var curveNode = CreateNode<EvaluateCurveNode>();
            Set.SendMessage(curveNode, EvaluateCurveNode.SimulationPorts.AnimationCurve, dotsCurve);
            Set.SetData(curveNode, EvaluateCurveNode.KernelPorts.Time, time);

            var output = CreateGraphValue(curveNode, EvaluateCurveNode.KernelPorts.Output);
            Set.Update(default);
            var value = Set.GetValueBlocking(output);

            Assert.That(value, Is.EqualTo(expected).Using(FloatComparer));

            dotsCurve.Dispose();
        }

        /// <summary>
        /// If no time is set, should give the curve evaluated at 0.
        /// </summary>
        [Test]
        public void TestNoTimeSetInCurveNode()
        {
            var animCurve = UnityEngine.AnimationCurve.Linear(10, -10, 100, -10);

            // Only supports clamp
            animCurve.preWrapMode = WrapMode.Clamp;
            animCurve.postWrapMode = WrapMode.Clamp;

            animCurve.AddKey(new UnityEngine.Keyframe() { time = 50, value = 100 });
            animCurve.AddKey(new UnityEngine.Keyframe() { time = 75, value = -30 });
            animCurve.AddKey(new UnityEngine.Keyframe() { time = 25, value = -30 });

            var expected = animCurve.Evaluate(0);

            var dotsCurve = animCurve.ToDotsAnimationCurve();

            var curveNode = CreateNode<EvaluateCurveNode>();
            Set.SendMessage(curveNode, EvaluateCurveNode.SimulationPorts.AnimationCurve, dotsCurve);

            var output = CreateGraphValue(curveNode, EvaluateCurveNode.KernelPorts.Output);
            Set.Update(default);
            var value = Set.GetValueBlocking(output);

            Assert.That(value, Is.EqualTo(expected));

            dotsCurve.Dispose();
        }
    }
}
