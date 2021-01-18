using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class SubGraphNodeIRBuilder : NodeIRBuilder
    {
        public SubGraphNodeModel SubGraphNodeModel => Model as SubGraphNodeModel;
        public IAuthoringContext Context => (Model.GraphModel.Stencil as BaseGraphStencil).Context;

        internal SubGraphNodeIRBuilder(SubGraphNodeModel model)
            : base(model)
        {
        }

        public override void Build(IR ir, IBuildContext context)
        {
            if (SubGraphNodeModel.GraphAsset == null)
                return;
            var nestedGraph = (BaseGraphModel)SubGraphNodeModel.GraphAsset.GraphModel;

            var node = ir.CreateNodeFromModel(SubGraphNodeModel.Guid, nestedGraph.Name, nestedGraph.AssetModel.FriendlyScriptName + "Node");

            IRBuilder.BuildNodePorts(ir, SubGraphNodeModel, node);
            IRBuilder.BuildPortDefaultValues(SubGraphNodeModel, node, ir, new StateMachineGraphBuildContext());
            var referencedIR = BuildReferencedIR(nestedGraph, node, ir); //do smth with IR?
            foreach (var nestedIRRef in referencedIR.ReferencedIRs)
            {
                if (!ir.ReferencedIRs.ContainsKey(nestedIRRef.Key))
                    ir.ReferencedIRs.Add(nestedIRRef.Key, nestedIRRef.Value);
            }
            if (!ir.ReferencedIRs.ContainsKey(nestedGraph.AssetModel.FriendlyScriptName + "Node"))
                ir.ReferencedIRs.Add(nestedGraph.AssetModel.FriendlyScriptName + "Node", referencedIR);
        }

        internal static IR BuildReferencedIR(BaseGraphModel nestedGraph, IRNodeDefinition node, IR ir)
        {
            IR referencedIR = IRBuilder.BuildBlendTreeIR(nestedGraph);
            if (referencedIR.HasBuildFailed)
            {
                ir.CompilationResult.AddError($"Failed to calculate subdependencies for {nestedGraph.Name}");
            }
            foreach (var assetReferencePair in referencedIR.AssetReferences)
            {
                if (!ir.AssetReferences.ContainsKey(assetReferencePair.Key))
                {
                    ir.AssetReferences.Add(assetReferencePair.Key, IRAssetReference.Clone(assetReferencePair.Value, DFGTranslationHelpers.CreateMessagePassThroughNodeOfType(ir, assetReferencePair.Value.DestinationType), isPropagatedReference: true));
                }
                ir.ExternalAssetReferenceMappings.Add(
                    new IRExternalAssetReference(
                        new IRPortTarget(node, assetReferencePair.Value.AssetReferenceName), assetReferencePair.Key));
            }
            return referencedIR;
        }

        public override IRPortTarget GetSourcePortTarget(BasePortModel port, IR ir, IBuildContext context)
        {
            return new IRPortTarget(ir.GetNodeFromModel(SubGraphNodeModel.Guid), port.OriginalScriptName);
        }

        public override IRPortTarget GetDestinationPortTarget(BasePortModel port, IR ir, IBuildContext context)
        {
            return new IRPortTarget(ir.GetNodeFromModel(SubGraphNodeModel.Guid), port.OriginalScriptName);
        }
    }
}
