using NUnit.Framework;
using System.Linq;

namespace Unity.Animation.Editor.Tests
{
    // Accepted
    internal class NoContextAttribute : DummyGraphStencil
    {
    }

    [Context("")]
    internal class EmptyName : DummyGraphStencil
    {
    }

    [Context("private")]
    class PrivateStencil : BaseGraphStencil
    {
        public override IAuthoringContext Context => new DummyContext();
    }

    [Context("internal")]
    internal class InternalStencil : DummyGraphStencil
    {
    }

    // Rejected
    [Context("public abstract")]
    internal abstract class AbstractStencil : DummyGraphStencil
    {
    }

    [Context("generic")]
    internal class GenericStencil<T> : DummyGraphStencil
    {
    }

    [Context("Not Stencil")]
    public class NotAStencil
    {
    }

    class ContextServiceTests : BaseGraphFixture
    {
        protected override string[] TestAssemblies => new[] { "Unity.Animation.Editor.Tests" };

        [Test]
        public void DetectAvailableContexts()
        {
            // DummyGraphStencil, NoContextAttribute, EmptyName, PrivateStencil, InternalStencil, State Machine
            Assert.That(ContextService.AvailableContexts.Count, Is.EqualTo(6));
        }
    }
}
