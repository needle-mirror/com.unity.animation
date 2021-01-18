using Unity.DataFlowGraph;
using Unity.Entities;

namespace Unity.Animation.StateMachine
{
    sealed class StateMachineInstanceDebugView
    {
        StateMachineInstance m_StateMachineInstance;

        public StateMachineInstanceDebugView(StateMachineInstance stateMachineInstance)
        {
            m_StateMachineInstance = stateMachineInstance;
        }

        public Hash128[] States
        {
            get
            {
                return m_StateMachineInstance.Definition.Value.States.ToArray();
            }
        }

        public EnterSelectorTransition[] OnEnterSelectors
        {
            get
            {
                return m_StateMachineInstance.Definition.Value.OnEnterSelectors.ToArray();
            }
        }

        public TransitionDefinition[] GlobalTransitions
        {
            get
            {
                return m_StateMachineInstance.Definition.Value.GlobalTransitions.ToArray();
            }
        }

        public TransitionDefinition[] OutgoingTransitions
        {
            get
            {
                return m_StateMachineInstance.Definition.Value.OutgoingTransitions.ToArray();
            }
        }

        public TransitionConditionFragment[] TransitionFragments
        {
            get
            {
                return m_StateMachineInstance.Definition.Value.ConditionFragments.ToArray();
            }
        }
        public int ID
        {
            get
            {
                return m_StateMachineInstance.ID;
            }
        }
        public float AccumulatedTime
        {
            get
            {
                return m_StateMachineInstance.AccumulatedTime;
            }
        }
        public int CurrentState
        {
            get
            {
                return m_StateMachineInstance.CurrentState;
            }
        }
        public NodeHandle CurrentStateNode
        {
            get
            {
                return m_StateMachineInstance.CurrentStateNode;
            }
        }

        public GraphHandle GraphHandle
        {
            get
            {
                return m_StateMachineInstance.GraphHandle;
            }
        }
        public DataFlowGraph.NodeHandle OutputNode
        {
            get
            {
                return m_StateMachineInstance.OutputNode;
            }
        }
        public OutputPortID OutputPortID
        {
            get
            {
                return m_StateMachineInstance.OutputPortID;
            }
        }
        public Unity.DataFlowGraph.NodeHandle ConnectToNode
        {
            get
            {
                return m_StateMachineInstance.ConnectToNode;
            }
        }
        public OutputPortID ConnectToPortID
        {
            get
            {
                return m_StateMachineInstance.ConnectToPortID;
            }
        }
    }
}
