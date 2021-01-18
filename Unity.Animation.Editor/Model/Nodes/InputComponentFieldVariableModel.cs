using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace Unity.Animation.Model
{
    [Serializable]
    internal class InputComponentFieldVariableModel : BaseVariableModel
    {
        internal string ComponentName;

        public override string Title => $"{ComponentName}.{(DeclarationModel as InputComponentFieldVariableDeclarationModel).DisplayTitle}";

        protected override void OnDefineNode()
        {
            if (DeclarationModel is InputComponentFieldVariableDeclarationModel model)
            {
                DefineNode(true, model.DataType, BasePortModel.PortEvaluationType.Properties);
                UpdateNameFromDeclaration();
            }

            m_Capabilities.Remove(UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.Renamable);
        }

        public void UpdateNameFromDeclaration()
        {
            if (DeclarationModel != null)
            {
                if ((GraphModel as BaseGraphModel).TryGetComponentBinding(
                    (DeclarationModel as InputComponentFieldVariableDeclarationModel).Identifier, out var input))
                    ComponentName = input.Name;
                else
                    ComponentName = "unknown";
            }
        }

        public override void OnDuplicateNode(INodeModel sourceNode)
        {
            base.OnDuplicateNode(sourceNode);

            if (sourceNode.GraphModel != GraphModel)
            {
                var sourceVariableDeclaration = (sourceNode as VariableNodeModel).DeclarationModel as InputComponentFieldVariableDeclarationModel;
                foreach (var variableDeclaration in GraphModel.VariableDeclarations.OfType<InputComponentFieldVariableDeclarationModel>())
                {
                    if (variableDeclaration.Identifier == sourceVariableDeclaration.Identifier &&
                        variableDeclaration.FieldHandle == sourceVariableDeclaration.FieldHandle)
                    {
                        DeclarationModel = variableDeclaration;
                        break;
                    }
                }
            }
        }
    }
}
