using System.Linq;
using NUnit.Framework;

namespace Unity.Animation.Editor.Tests
{
    public class StateMachineGraphTargetContext
    {
        [Test]
        public void StateMachineGraphTargetContext_CanSetLoop_ValidateFunctionality()
        {
            var targetContext = new StateMachineGraphBuildContext();
            Assert.That(targetContext.ShouldLoop, Is.False);
            targetContext.ShouldLoop = true;
            Assert.That(targetContext.ShouldLoop, Is.True);
        }

        [Test]
        public void PreBuild_CreatesInputs()
        {
            var ir = new IR("testIR", false);
            var targetContext = new StateMachineGraphBuildContext();
            var ctx = new DummyContext();
            targetContext.PreBuild(ir, ctx);
            Assert.That(ir.Inputs.Where(x => x.Source.Node.Name == "ContextInput").Count(), Is.EqualTo(1));
            Assert.That(ir.Inputs.Where(x => x.Source.Node.Name == "EntityManagerInput").Count(), Is.EqualTo(1));
            Assert.That(ir.Inputs.Where(x => x.Source.Node.Name == "TimeControl").Count(), Is.EqualTo(1));
            Assert.That(ir.Inputs.Where(x => x.Source.Node.Name == "InputReferences").Count(), Is.EqualTo(1));
            Assert.That(ir.Inputs.Where(x => x.Source.Node.Name == "NotAThing").Count(), Is.EqualTo(0));
        }
    }
}
