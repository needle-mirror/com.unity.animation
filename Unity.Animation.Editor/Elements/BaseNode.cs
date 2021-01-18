using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace Unity.Animation.Editor
{
    internal abstract class BaseNode : CollapsibleInOutNode
    {
        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.ReplacePart(portContainerPartName, InOutPortContainerPart.Create(portContainerPartName, Model, this, ussClassName));
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.TemplatePath + "CompositorNode.uss"));
        }
    }
}
