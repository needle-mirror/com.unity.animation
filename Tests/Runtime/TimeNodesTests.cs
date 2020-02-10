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
            var deltaTimeNode = CreateNode<DeltaTimeNode>();

            var output = CreateGraphValue(deltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime);

            float deltaTime = Time.deltaTime;
            Set.Update(default);

            float value = Set.GetValueBlocking(output);

            Assert.That(value,Is.EqualTo(deltaTime));
        }

        [TestCase(0, 2)]
        [TestCase(0.5f, 3)]
        [TestCase(1, 4.2f)]
        public void CanDenormalizeTime(float inputTime, float duration)
        {
            var normalizedTimeNode = CreateNode<NormalizedTimeNode>();
            Set.SetData(normalizedTimeNode, NormalizedTimeNode.KernelPorts.InputTime, inputTime);
            Set.SendMessage(normalizedTimeNode, NormalizedTimeNode.SimulationPorts.Duration, duration);

            var output = CreateGraphValue(normalizedTimeNode, NormalizedTimeNode.KernelPorts.OutputTime);

            Set.Update(default);

            float value = Set.GetValueBlocking(output);

            Assert.That(value,Is.EqualTo(inputTime*duration));
        }

        [TestCase(0, 2)]
        [TestCase(6.2f, 7)]
        [TestCase(-9.3f, 4.2f)]
        public void CanLoopTime(float inputTime, float duration)
        {
            var timeLoopNode = CreateNode<TimeLoopNode>();
            Set.SetData(timeLoopNode, TimeLoopNode.KernelPorts.InputTime, inputTime);
            Set.SendMessage(timeLoopNode, TimeLoopNode.SimulationPorts.Duration, duration);

            var outTimeO = CreateGraphValue(timeLoopNode, TimeLoopNode.KernelPorts.OutputTime);
            var cycleO = CreateGraphValue(timeLoopNode, TimeLoopNode.KernelPorts.Cycle);
            var normalizedO = CreateGraphValue(timeLoopNode, TimeLoopNode.KernelPorts.NormalizedTime);

            Set.Update(default);

            var outTime = Set.GetValueBlocking(outTimeO);
            var cycle = Set.GetValueBlocking(cycleO);
            var normalized = Set.GetValueBlocking(normalizedO);

            var normalizedExpected = inputTime / duration;
            var normalizedTimeInt = (int)normalizedExpected;

            var cycleExp = math.select(normalizedTimeInt, normalizedTimeInt - 1, normalizedExpected < 0);
            normalizedExpected = math.select(normalizedExpected - normalizedTimeInt, normalizedExpected - normalizedTimeInt + 1, normalizedExpected < 0);

            var OutTimeExp = normalizedExpected * duration;

            Assert.That(outTime, Is.EqualTo(OutTimeExp));
            Assert.That(cycle, Is.EqualTo(cycleExp));
            Assert.That(normalized, Is.EqualTo(normalizedExpected));
        }

        [TestCase(.033f, 2)]
        [TestCase(1, 0)]
        [TestCase(.015f, 1)]
        public void CanCountTime(float deltaTime, float speed)
        {
            var timeCounterNode = CreateNode<TimeCounterNode>();
            Set.SetData(timeCounterNode, TimeCounterNode.KernelPorts.DeltaTime, deltaTime);
            Set.SetData(timeCounterNode, TimeCounterNode.KernelPorts.Speed, speed);

            var output = CreateGraphValue(timeCounterNode, TimeCounterNode.KernelPorts.Time);

            var time = 0.0f;

            var handle = Set.Update(default);
            time += deltaTime * speed;

            handle = Set.Update(handle);
            time += deltaTime * speed;

            Set.Update(handle);
            time += deltaTime * speed;

            float value = Set.GetValueBlocking(output);

            Assert.That(value,Is.EqualTo(time));
        }

        [TestCase(.033f, 2, 3)]
        [TestCase(1, 0, 2)]
        [TestCase(.015f, 1, 4)]
        public void CanSetTime(float deltaTime, float speed, float time)
        {
            var timeCounterNode = CreateNode<TimeCounterNode>();
            Set.SetData(timeCounterNode, TimeCounterNode.KernelPorts.DeltaTime, deltaTime);
            Set.SetData(timeCounterNode, TimeCounterNode.KernelPorts.Speed, speed);

            var output = CreateGraphValue(timeCounterNode, TimeCounterNode.KernelPorts.Time);

            var expectedTime = 0.0f;

            var handle = Set.Update(default);
            expectedTime += deltaTime * speed;

            handle = Set.Update(handle);
            expectedTime += deltaTime * speed;

            Set.SendMessage(timeCounterNode, TimeCounterNode.SimulationPorts.Time, time);
            expectedTime = time;

            Set.Update(handle);
       
            float value = Set.GetValueBlocking(output);

            Assert.That(value,Is.EqualTo(expectedTime));
        }
    }
}
