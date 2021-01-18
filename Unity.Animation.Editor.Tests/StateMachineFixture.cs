using Unity.Animation.Model;

namespace Unity.Animation.Editor.Tests
{
    internal class DummyStateMachineStencil : AnimationStateMachineStencil
    {
        public override IAuthoringContext Context => new DummyContext();
    }

    internal class BaseStateMachineFixture : BaseFixture<StateMachineAsset, StateMachineModel, DummyStateMachineStencil>
    {
        protected override void RegisterReducers()
        {
            base.RegisterReducers();
            var stateMachineRegisterReducer = new StateMachineRegisterReducer();
            stateMachineRegisterReducer.RegisterReducers(m_Store);
        }
    }
}
