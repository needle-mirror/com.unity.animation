using System;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor.UIElements;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Animation.Hybrid;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    class Float3ConstantHandler : IConstantHandler
    {
        public object GetValueFromDefaultString(string str)
        {
            var parameters = str.Split(',');
            if (parameters.Length != 3)
                return null;
            float x, y, z;
            if (!float.TryParse(parameters[0], out x) ||
                !float.TryParse(parameters[1], out y) ||
                !float.TryParse(parameters[2], out z))
                return null;
            return new float3(x, y, z);
        }
    }

    class QuatConstantHandler : IConstantHandler
    {
        public object GetValueFromDefaultString(string str)
        {
            var parameters = str.Split(',');
            if (parameters.Length != 4)
                return null;
            float x, y, z, w;
            if (!float.TryParse(parameters[0], out x) ||
                !float.TryParse(parameters[1], out y) ||
                !float.TryParse(parameters[2], out z) ||
                !float.TryParse(parameters[3], out w))
                return null;
            return new quaternion(x, y, z, w);
        }
    }

    class Float44ConstantHandler : IConstantHandler
    {
        public object GetValueFromDefaultString(string str)
        {
            var parameters = str.Split(',');
            if (parameters.Length != 16)
                return null;
            float x, y, z, w;
            if (!float.TryParse(parameters[0], out x) ||
                !float.TryParse(parameters[1], out y) ||
                !float.TryParse(parameters[2], out z) ||
                !float.TryParse(parameters[3], out w))
                return null;
            float4 v0 = new float4(x, y, z, w);
            if (!float.TryParse(parameters[4], out x) ||
                !float.TryParse(parameters[5], out y) ||
                !float.TryParse(parameters[6], out z) ||
                !float.TryParse(parameters[7], out w))
                return null;
            float4 v1 = new float4(x, y, z, w);
            if (!float.TryParse(parameters[8], out x) ||
                !float.TryParse(parameters[9], out y) ||
                !float.TryParse(parameters[10], out z) ||
                !float.TryParse(parameters[11], out w))
                return null;
            float4 v2 = new float4(x, y, z, w);
            if (!float.TryParse(parameters[12], out x) ||
                !float.TryParse(parameters[13], out y) ||
                !float.TryParse(parameters[14], out z) ||
                !float.TryParse(parameters[15], out w))
                return null;
            float4 v3 = new float4(x, y, z, w);
            return new float4x4(v0, v1, v2, v3);
        }
    }

    internal class AnimationOutputNodeModel : OutputNodeModel
    {
        [SerializeField]
        public bool Loop = true;

        AnimationOutputNodeIRBuilder m_Builder;
        public override INodeIRBuilder Builder
        { get { if (m_Builder == null) m_Builder = new AnimationOutputNodeIRBuilder(this); return m_Builder; } }
        public override string NodeName => "Out Pose";

        internal const string k_PoseResultPortName = "Result";

        class AnimationOutputNodeIRBuilder : NodeIRBuilder
        {
            AnimationOutputNodeModel OutputNodeModel => Model as AnimationOutputNodeModel;
            string m_InstanceNodeName;

            public AnimationOutputNodeIRBuilder(AnimationOutputNodeModel model)
                : base(model)
            {}
            public override void PreBuild(IR ir, IBuildContext context)
            {
                if (context is StateMachineGraphBuildContext graphContext)
                {
                    graphContext.ShouldLoop = OutputNodeModel.Loop;
                }
                else if (context is StandAloneGraphBuildContext SAgraphContext)
                {
                    SAgraphContext.ShouldLoop = OutputNodeModel.Loop;
                }
                base.PreBuild(ir, context);
            }

            public override void Build(IR ir, IBuildContext context)
            {
                if (context is StateMachineBuildContext)
                {
                    ir.CompilationResult.AddError($"Pose node cannot be used in a State Machine context", OutputNodeModel);
                    return;
                }

                var animatedDataPT = ir.CreateNodeFromModel(OutputNodeModel.Guid, OutputNodeModel.NodeName, typeof(KernelPhasePassThroughNodeBufferAnimatedData));
                IRBuilder.BuildNodePorts(ir, OutputNodeModel, animatedDataPT);
                IRBuilder.BuildPortDefaultValues(OutputNodeModel, animatedDataPT, ir, context);

                m_InstanceNodeName = animatedDataPT.Name;
                ir.AddOutput(
                    new IRPortTarget(new IRNodeDefinition("DummyName", typeof(Buffer<AnimatedData>)), "OutputAnimatedData"),
                    new IRPortTarget(animatedDataPT, nameof(KernelPhasePassThroughNodeBufferAnimatedData.KernelPorts.Output)));
            }

            public override IRPortTarget GetDestinationPortTarget(BasePortModel port, IR ir, IBuildContext context)
            {
                if (port.Title == k_PoseResultPortName)
                    return new IRPortTarget(ir.GetNodeFromName(m_InstanceNodeName), nameof(KernelPhasePassThroughNodeBufferAnimatedData.KernelPorts.Input));
                return base.GetDestinationPortTarget(port, ir, context);
            }
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            AddInputPort(
                new NodePortCreation()
                {
                    PortType = PortType.Execution,
                    EvalType = BasePortModel.PortEvaluationType.Rendering,
                    DataType = typeof(Buffer<AnimatedData>).GenerateTypeHandle(),
                    Name = k_PoseResultPortName,
                    DisplayName = "Result",
                    PortDescription = "Animation Result",
                }, false);
        }
    }

    internal class AnimationAuthoringContext : AuthoringContext<AnimationOutputNodeModel>
    {
        public override Type DefaultDataType => typeof(Buffer<AnimatedData>);
        public override Type PassThroughForDefaultDataType => typeof(KernelPhasePassThroughNodeBufferAnimatedData);
        public override Type GameObjectContextType => typeof(RigComponent);
        public override Type ContextType => typeof(Rig);
        public override Type ContextHandlerType => typeof(IRigContextHandler);

        protected override void BuildConstantEditorMappings()
        {
            base.BuildConstantEditorMappings();
            TypeConstantHandlerDictionary.Instance.AddTypeHandler(typeof(float3), new Float3ConstantHandler());
            TypeConstantHandlerDictionary.Instance.AddTypeHandler(typeof(quaternion), new QuatConstantHandler());
            TypeConstantHandlerDictionary.Instance.AddTypeHandler(typeof(float4x4), new Float44ConstantHandler());
        }
    }

    [Serializable]
    class AnimationClipReferenceConstant : AssetReferenceConstant<AnimationClip, BlobAssetReference<Clip>>, IValueIRBuilder
    {
        //TODO NAM : Do we need to have a different version from AssetReferenceConstant
        // public void Build(CompositorBasePortModel portModel, CompositorIRPortTarget target, CompositorIR ir, ITargetContext context)
        // {
        //     if (portModel.EmbeddedValue.ObjectValue != null && portModel.EmbeddedValue.ObjectValue is UnityEngine.Object unityObj && unityObj != null)
        //     {
        //         GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(unityObj);
        //         ir.AddDefaultValue(target, portModel.EmbeddedValue.ObjectValue, true, id);
        //         if (!ir.AssetReferences.ContainsKey(id))
        //             ir.AssetReferences.Add(id,
        //                 new AssetReference(id, ObjectWorldType, portModel.PortDataType,
        //                     unityObj.name, DFGTranslationHelpers.CreateMessagePassThroughNodeOfType(ir, portModel.PortDataType),
        //                     isPropagatedReference: false));
        //     }
        // }
    }

    [Serializable]
    class MotionIDConstant : ObjectReferenceConstant<Transform, MotionID>
    {
    }

    [GraphElementsExtensionMethodsCache]
    internal static partial class ConstantEditorExtensions
    {
        public static VisualElement BuildAnimationClipEditor(this IConstantEditorBuilder builder, AnimationClipReferenceConstant clip)
        {
            return UnityEditor.GraphToolsFoundation.Overdrive.ConstantEditorExtensions.BuildInlineValueEditor(
                clip.ObjectValue, new ObjectField { objectType = clip.Type }, builder.OnValueChanged);
        }

        public static VisualElement BuildMotionIDEditor(this IConstantEditorBuilder builder, MotionIDConstant constant)
        {
            return ConstantEditorHelper.BuildBoundExposedObjectEditor<Transform>(builder);
        }
    }
}
