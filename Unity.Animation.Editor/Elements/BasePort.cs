using System.Linq;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class BasePort : Port
    {
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList("compositorPort");
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.TemplatePath + "CompositorPort.uss"));
        }

        protected override void UpdateSelfFromModel()
        {
            base.UpdateSelfFromModel();

            if (((BasePortModel)PortModel).IsStatic)
                AddToClassList("static");

            if (PortModel.PortType == PortType.Execution)
                AddToClassList("execution");
            else if (PortModel.Direction == Direction.Output &&
                     ((IInOutPortsNode)PortModel.NodeModel).OutputsByDisplayOrder.Count(x => x is BasePortModel model && !model.IsHidden) == 1)
                AddToClassList("portConnectorOnly");
        }
    }
}
