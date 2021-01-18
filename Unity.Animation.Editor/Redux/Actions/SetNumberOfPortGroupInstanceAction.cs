using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class SetNumberOfPortGroupInstanceAction : BaseAction
    {
        public readonly INodeModel Node;
        public readonly int PortGroup;
        public readonly int OldNbrInstances;
        public readonly int NewNbrInstances;

        public SetNumberOfPortGroupInstanceAction(INodeModel node, int portGroup, int oldNumberInstances, int newNumberInstances)
        {
            Node = node;
            PortGroup = portGroup;
            OldNbrInstances = oldNumberInstances;
            NewNbrInstances = newNumberInstances;
            UndoString = "Set Number of Ports in Group";
        }

        internal static void DefaultReducer(UnityEditor.GraphToolsFoundation.Overdrive.State prevState, SetNumberOfPortGroupInstanceAction action)
        {
            prevState.PushUndo(action);

            var nodeModel = action.Node as IPortGroup;
            if (nodeModel == null)
                return;

            nodeModel.SetPortGroupInstanceSize(action.PortGroup, action.NewNbrInstances);
            prevState.MarkChanged(action.Node);
        }
    }
}
