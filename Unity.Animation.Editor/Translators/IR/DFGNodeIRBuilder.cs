using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class DFGNodeIRBuilder : NodeIRBuilder
    {
        public DFGNodeModel DFGNodeModel => Model as DFGNodeModel;
        public IAuthoringContext Context => (Model.GraphModel.Stencil as BaseGraphStencil).Context;

        internal DFGNodeIRBuilder(DFGNodeModel model)
            : base(model)
        {
        }

        public override void Build(IR ir, IBuildContext context)
        {
            var node = ir.CreateNodeFromModel(Model.Guid, Model.NodeName, DFGNodeModel.NodeType.AssemblyQualifiedName);

            IRBuilder.BuildNodePorts(ir, Model, node);
            if (!DFGNodeModel.IsValid)
                ir.CompilationResult.AddError($"{Model.NodeName} of type {DFGNodeModel.NodeType.FullName} could not be found", Model);

            IRBuilder.BuildPortDefaultValues(DFGNodeModel, node, ir, context);
        }

        public override IRPortTarget GetSourcePortTarget(BasePortModel port, IR ir, IBuildContext context)
        {
            return new IRPortTarget(ir.GetNodeFromModel(DFGNodeModel.Guid), port.OriginalScriptName, port.PortGroupInstance);
        }

        public override IRPortTarget GetDestinationPortTarget(BasePortModel port, IR ir, IBuildContext context)
        {
            return new IRPortTarget(ir.GetNodeFromModel(DFGNodeModel.Guid), port.OriginalScriptName, port.PortGroupInstance);
        }
    }
}
