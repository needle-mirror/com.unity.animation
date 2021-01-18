using System;
using Unity.Entities;
using Unity.Collections;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    interface IBuildContext
    {
        //Is called before any Build() is called on the nodes
        void PreBuild(IR ir, IAuthoringContext context);

        //Is called after all the Build() calls have been made on each node
        void PostBuild(IR ir, IAuthoringContext context);
    }

    internal class StateMachineBuildContext : IBuildContext
    {
        public void PreBuild(IR ir, IAuthoringContext context)
        {
        }

        public void PostBuild(IR ir, IAuthoringContext context)
        {
        }
    }

    // This is the context for when a graph is built inside a state machine. It is very close to the
    // StandAloneGraphTargetContext class, so maybe we could consider using inheritance here instead
    // of duplicating code
    internal class StateMachineGraphBuildContext : IBuildContext
    {
        internal bool ShouldLoop { get; set; }

        private IRPortTarget RigInfoOutputPortTarget;
        private IRPortTarget EntityManagerOutputPortTarget;
        private IRPortTarget TimeControlPortTarget;
        private IRPortTarget InputReferencesPortTarget;

        public void PreBuild(IR ir, IAuthoringContext context)
        {
            if (context == null)
            {
                ir.CompilationResult.AddError($"There was no authoring context provided to {nameof(StandAloneGraphBuildContext)}");
                return;
            }

            {
                var pt = DFGTranslationHelpers.CreateMessagePassThroughNodeOfType(ir, context.ContextType);
                RigInfoOutputPortTarget = new IRPortTarget(pt, "Output");
                var dummyInputNode = new IRNodeDefinition("ContextInput", pt.NodeType);
                ir.AddInput(new IRPortTarget(dummyInputNode, ""), new IRPortTarget(pt, "Input"));
            }

            {
                var pt = DFGTranslationHelpers.CreateMessagePassThroughNodeOfType(ir, typeof(EntityManager));
                EntityManagerOutputPortTarget = new IRPortTarget(pt, "Output");
                var dummyInputNode = new IRNodeDefinition("EntityManagerInput", pt.NodeType);
                ir.AddInput(new IRPortTarget(dummyInputNode, ""), new IRPortTarget(pt, "Input"));
            }

            {
                var pt = DFGTranslationHelpers.CreateMessagePassThroughNodeOfType(ir, typeof(TimeControl));
                TimeControlPortTarget = new IRPortTarget(pt, "Output");
                var dummyInputNode = new IRNodeDefinition("TimeControl", pt.NodeType);
                ir.AddInput(new IRPortTarget(dummyInputNode, ""), new IRPortTarget(pt, "Input"));
            }

            {
                var pt = DFGTranslationHelpers.CreateMessagePassThroughNodeOfType(ir, typeof(NativeArray<InputReference>));
                InputReferencesPortTarget = new IRPortTarget(pt, "InputReferences");
                var dummyInputNode = new IRNodeDefinition("InputReferences", pt.NodeType);
                ir.AddInput(new IRPortTarget(dummyInputNode, ""), new IRPortTarget(pt, "Input"));
            }
        }

        // For each node, we connect it to the corresponding passthrough if they implement the matching TaskPort.
        // Maybe the logic should be the other way round and each node should ask for a reference to the PT in the Build()
        // function to properly do the connection?
        public void PostBuild(IR ir, IAuthoringContext context)
        {
            //TODO NAM : Unfortunately we have to create a NodeSet here to fetch the ports for tasks. Maybe we could have a DFGTranslation Object that holds this NodeSet?
            using (var dummy = new DataFlowGraph.NodeSet())
            {
                foreach (var node in ir.Nodes)
                {
                    var t = node.NodeType ?? Type.GetType(node.GetUnresolvedTypeName());
                    if (t != null)
                    {
                        if (context.ContextHandlerType != null && context.ContextHandlerType.IsAssignableFrom(t))
                        {
                            string inputName = DFGTranslationHelpers.GetInputPortNameForTask(dummy, t, context.ContextHandlerType);
                            ir.ConnectSimulation(RigInfoOutputPortTarget, new IRPortTarget(node, inputName));
                        }
                        if (typeof(IEntityManagerHandler).IsAssignableFrom(t))
                        {
                            string inputName = DFGTranslationHelpers.GetInputPortNameForTask(dummy, t, typeof(IEntityManagerHandler));
                            ir.ConnectSimulation(EntityManagerOutputPortTarget, new IRPortTarget(node, inputName));
                        }
                        if (typeof(ITimeControlHandler).IsAssignableFrom(t))
                        {
                            string inputName = DFGTranslationHelpers.GetInputPortNameForTask(dummy, t, typeof(ITimeControlHandler));
                            ir.ConnectSimulation(TimeControlPortTarget, new IRPortTarget(node, inputName));
                        }
                        if (typeof(IInputReferenceHandler).IsAssignableFrom(t))
                        {
                            string inputName = DFGTranslationHelpers.GetInputPortNameForTask(dummy, t, typeof(IInputReferenceHandler));
                            ir.ConnectSimulation(InputReferencesPortTarget, new IRPortTarget(node, inputName));
                        }
                    }
                }
            }
        }
    }

    internal class StandAloneGraphBuildContext : IBuildContext
    {
        internal bool ShouldLoop { get; set; }

        private IRPortTarget RigInfoOutputPortTarget;
        private IRPortTarget EntityManagerOutputPortTarget;
        private IRPortTarget InputReferencePortTarget;

        public void PreBuild(IR ir, IAuthoringContext context)
        {
            if (context == null)
            {
                ir.CompilationResult.AddError($"There was no authoring context provided to {nameof(StandAloneGraphBuildContext)}");
                return;
            }

            {
                var pt = DFGTranslationHelpers.CreateMessagePassThroughNodeOfType(ir, context.ContextType);
                RigInfoOutputPortTarget = new IRPortTarget(pt, "Output");
                var dummyInputNode = new IRNodeDefinition("ContextInput", pt.NodeType);
                ir.AddInput(new IRPortTarget(dummyInputNode, ""), new IRPortTarget(pt, "Input"));
            }

            {
                var pt = DFGTranslationHelpers.CreateMessagePassThroughNodeOfType(ir, typeof(EntityManager));
                EntityManagerOutputPortTarget = new IRPortTarget(pt, "Output");
                var dummyInputNode = new IRNodeDefinition("EntityManagerInput", pt.NodeType);
                ir.AddInput(new IRPortTarget(dummyInputNode, ""), new IRPortTarget(pt, "Input"));
            }

            {
                var pt = DFGTranslationHelpers.CreateMessagePassThroughNodeOfType(ir, typeof(NativeArray<InputReference>));
                InputReferencePortTarget = new IRPortTarget(pt, "InputReferences");
                var dummyInputNode = new IRNodeDefinition("InputReferences", pt.NodeType);
                ir.AddInput(new IRPortTarget(dummyInputNode, ""), new IRPortTarget(pt, "Input"));
            }
        }

        public void PostBuild(IR ir, IAuthoringContext context)
        {
            if (context == null)
                return;

            using (var dummy = new DataFlowGraph.NodeSet())
            {
                foreach (var node in ir.Nodes)
                {
                    var t = node.NodeType ?? Type.GetType(node.GetUnresolvedTypeName());
                    if (t != null)
                    {
                        if (context.ContextHandlerType != null && context.ContextHandlerType.IsAssignableFrom(t))
                        {
                            string inputName = DFGTranslationHelpers.GetInputPortNameForTask(dummy, t, context.ContextHandlerType);
                            ir.ConnectSimulation(RigInfoOutputPortTarget, new IRPortTarget(node, inputName));
                        }
                        if (typeof(IEntityManagerHandler).IsAssignableFrom(t))
                        {
                            string inputName = DFGTranslationHelpers.GetInputPortNameForTask(dummy, t, typeof(IEntityManagerHandler));
                            ir.ConnectSimulation(EntityManagerOutputPortTarget, new IRPortTarget(node, inputName));
                        }
                        if (typeof(IInputReferenceHandler).IsAssignableFrom(t))
                        {
                            string inputName = DFGTranslationHelpers.GetInputPortNameForTask(dummy, t, typeof(IInputReferenceHandler));
                            ir.ConnectSimulation(InputReferencePortTarget, new IRPortTarget(node, inputName));
                        }
                    }
                }
            }
        }
    }

    internal interface IValueIRBuilder
    {
        void Build(BasePortModel pm, IRPortTarget target, IR ir, IBuildContext context);
    }

    internal interface INodeIRBuilder
    {
        void PreBuild(IR ir, IBuildContext context);
        void Build(IR ir, IBuildContext context);
        IRPortTarget GetSourcePortTarget(BasePortModel port, IR ir, IBuildContext context);
        IRPortTarget GetDestinationPortTarget(BasePortModel port, IR ir, IBuildContext context);
    }

    internal abstract class NodeIRBuilder : INodeIRBuilder
    {
        public BaseNodeModel Model { set; get; }

        public NodeIRBuilder(BaseNodeModel model)
        {
            Model = model;
        }

        public virtual void PreBuild(IR ir, IBuildContext context)
        {
        }

        public abstract void Build(IR ir, IBuildContext context);

        public virtual void PostBuild(IR ir, IBuildContext context)
        {
        }

        public virtual IRPortTarget GetSourcePortTarget(BasePortModel port, IR ir, IBuildContext context)
        {
            ir.CompilationResult.AddError($"{Model.Title} {nameof(GetSourcePortTarget)} invalid port {port.Title}", Model);
            return null;
        }

        public virtual IRPortTarget GetDestinationPortTarget(BasePortModel port, IR ir, IBuildContext context)
        {
            ir.CompilationResult.AddError($"{Model.Title} {nameof(GetDestinationPortTarget)} invalid port {port.Title}", Model);
            return null;
        }
    }
}
