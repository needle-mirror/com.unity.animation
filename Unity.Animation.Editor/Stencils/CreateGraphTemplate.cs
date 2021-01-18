using System;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace Unity.Animation.Editor
{
    internal class CreateGraphTemplate : BaseCreateTemplate
    {
        public CreateGraphTemplate(Type stencilType)
            : base(stencilType)
        {
        }

        public override void InitBasicGraph(IGraphModel graphModel)
        {
            base.InitBasicGraph(graphModel);
            var stencil = graphModel.Stencil as BaseGraphStencil;
            graphModel.CreateNode(stencil.Context.OutputNodeType, "Output", new UnityEngine.Vector2(0, 0));
        }
    }
}
