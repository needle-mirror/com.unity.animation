using System.Linq;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using Unity.Animation.Editor;

namespace Unity.Animation.Model
{
    internal class BlackboardGraphModel : UnityEditor.GraphToolsFoundation.Overdrive.BasicModel.BlackboardGraphModel
    {
        const string k_GraphTitle = "Graph";
        const string k_StateMachineTitle = "State Machine";

        public override string GetBlackboardSubTitle()
        {
            if (GraphModel == null)
                return "";

            return (GraphModel.Stencil is BaseGraphStencil) ? k_GraphTitle : k_StateMachineTitle;
        }

        public override void PopulateCreateMenu(string sectionName, GenericMenu menu, Store store)
        {
            var stencil = store.State.GraphModel.Stencil as BaseStencil;
            foreach (var componentInfo in Hybrid.AuthoringComponentService.GetComponentInfos())
            {
                var authoringTypeHandle = componentInfo.AuthoringType.GenerateTypeHandle();
                var runtimeTypeHandle = componentInfo.RuntimeType.GenerateTypeHandle();
                var binding =
                    (store.State.GraphModel as BaseModel).GetComponentBinding(componentInfo.RuntimeType);

                if (binding == null)
                {
                    menu.AddItem(new GUIContent(authoringTypeHandle.GetMetadata(stencil).FriendlyName), false, () =>
                    {
                        var finalName = runtimeTypeHandle.GetMetadata(stencil).FriendlyName;
                        var i = 0;
                        while (store.State.GraphModel.VariableDeclarations.Any(v => v.Title == finalName))
                            finalName = runtimeTypeHandle.GetMetadata(stencil).FriendlyName + i++;

                        store.Dispatch(new CreateInputComponentAction(
                            new[] { new CreateInputComponentAction.InputData(finalName, componentInfo.RuntimeType)}));
                    });
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent(authoringTypeHandle.GetMetadata(stencil).FriendlyName));
                }
            }
        }
    }
}
