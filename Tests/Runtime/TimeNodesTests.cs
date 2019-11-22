using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Animation.Tests
{
    class TimeNodesTests : AnimationTestsFixture
    {
        [Test]
        public void CanGetDeltaTime()
        {
            var set = Set;
            var deltaTimeNode = CreateNode<DeltaTimeNode>();

            var output = CreateGraphValue(deltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime);

            float deltaTime = Time.deltaTime;
            set.Update();

            float value = set.GetValueBlocking(output);

            Assert.That(value,Is.EqualTo(deltaTime));
        }

        [Test]
        [TestCase(0, 2)]
        [TestCase(0.5f, 3)]
        [TestCase(1, 4.2f)]
        public void CanDenormalizeTime(float inputTime, float duration)
        {
            var set = Set;
            var normalizedTimeNode = CreateNode<NormalizedTimeNode>();
            set.SetData(normalizedTimeNode, NormalizedTimeNode.KernelPorts.InputTime, inputTime);
            set.SendMessage(normalizedTimeNode, NormalizedTimeNode.SimulationPorts.Duration, duration);

            var output = CreateGraphValue(normalizedTimeNode, NormalizedTimeNode.KernelPorts.OutputTime);

            set.Update();

            float value = set.GetValueBlocking(output);

            Assert.That(value,Is.EqualTo(inputTime*duration));
        }

        [Test]
        [TestCase(0, 2)]
        [TestCase(6.2f, 7)]
        [TestCase(-9.3f, 4.2f)]
        public void CanLoopTime(float inputTime, float duration)
        {
            var set = Set;
            var timeLoopNode = CreateNode<TimeLoopNode>();
            set.SetData(timeLoopNode, TimeLoopNode.KernelPorts.InputTime, inputTime);
            set.SendMessage(timeLoopNode, TimeLoopNode.SimulationPorts.Duration, duration);

            var outTimeO = CreateGraphValue(timeLoopNode, TimeLoopNode.KernelPorts.OutputTime);
            var cycleO = CreateGraphValue(timeLoopNode, TimeLoopNode.KernelPorts.Cycle);
            var normalizedO = CreateGraphValue(timeLoopNode, TimeLoopNode.KernelPorts.NormalizedTime);

            set.Update();

            var outTime = set.GetValueBlocking(outTimeO);
            var cycle = set.GetValueBlocking(cycleO);
            var normalized = set.GetValueBlocking(normalizedO);

            var normalizedExpected = inputTime / duration;
            var normalizedTimeInt = (int)normalizedExpected;

            var cycleExp = math.select(normalizedTimeInt, normalizedTimeInt - 1, normalizedExpected < 0);
            normalizedExpected = math.select(normalizedExpected - normalizedTimeInt, normalizedExpected - normalizedTimeInt + 1, normalizedExpected < 0);

            var OutTimeExp = normalizedExpected * duration;

            Assert.That(outTime, Is.EqualTo(OutTimeExp));
            Assert.That(cycle, Is.EqualTo(cycleExp));
            Assert.That(normalized, Is.EqualTo(normalizedExpected));
        }

        [Test]
        [TestCase(.033f, 2)]
        [TestCase(1, 0)]
        [TestCase(.015f, 1)]
        public void CanCountTime(float deltaTime, float speed)
        {
            var set = Set;
            var timeCounterNode = CreateNode<TimeCounterNode>();
            set.SetData(timeCounterNode, TimeCounterNode.KernelPorts.DeltaTime, deltaTime);
            set.SendMessage(timeCounterNode, TimeCounterNode.SimulationPorts.Speed, speed);

            var output = CreateGraphValue(timeCounterNode, TimeCounterNode.KernelPorts.Time);

            var time = 0.0f;

            set.Update();
            time += deltaTime * speed;

            set.Update();
            time += deltaTime * speed;

            set.Update();
            time += deltaTime * speed;

            float value = set.GetValueBlocking(output);

            Assert.That(value,Is.EqualTo(time));
        }

        [Test]
        [TestCase(.033f, 2, 3)]
        [TestCase(1, 0, 2)]
        [TestCase(.015f, 1, 4)]
        public void CanSetTime(float deltaTime, float speed, float time)
        {
            var set = Set;
            var timeCounterNode = CreateNode<TimeCounterNode>();
            set.SetData(timeCounterNode, TimeCounterNode.KernelPorts.DeltaTime, deltaTime);
            set.SendMessage(timeCounterNode, TimeCounterNode.SimulationPorts.Speed, speed);

            var output = CreateGraphValue(timeCounterNode, TimeCounterNode.KernelPorts.Time);

            var expectedTime = 0.0f;

            set.Update();
            expectedTime += deltaTime * speed;

            set.Update();
            expectedTime += deltaTime * speed;

            set.SendMessage(timeCounterNode, TimeCounterNode.SimulationPorts.Time, time);
            expectedTime = time;

            set.Update();
       
            float value = set.GetValueBlocking(output);

            Assert.That(value,Is.EqualTo(expectedTime));
        }
    }
}
