using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace Unity.Animation.Editor
{
    internal class Blackboard : UnityEditor.GraphToolsFoundation.Overdrive.Blackboard
    {
        public static readonly string blackboardContextPartName = "context";
        public static readonly string blackboardInputsPartName = "inputs";
        protected override void BuildPartList()
        {
            PartList.AppendPart(BlackboardHeaderPart.Create(blackboardHeaderPartName, Model, this, ussClassName));
            PartList.AppendPart(BlackboardContextDefinitionPart.Create(blackboardContextPartName, Model, this, ussClassName));
            PartList.AppendPart(BlackboardInputsPart.Create(blackboardInputsPartName, Model, this, ussClassName));
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
        }
    }
}
