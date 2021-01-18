using System;
using System.Linq;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Unity.Animation.Editor;
using Unity.Animation.Hybrid;

namespace Unity.Animation.Model
{
    [Serializable]
    class AssetReferenceConstant<TObject, TEditor> : ReferenceConstant<TObject, TEditor>, IValueIRBuilder
    {
        public void Build(BasePortModel portModel, IRPortTarget target, IR ir, IBuildContext context)
        {
            if (portModel.EmbeddedValue.ObjectValue != null && portModel.EmbeddedValue.ObjectValue is UnityEngine.Object unityObj && unityObj != null)
            {
                GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(unityObj);
                ir.AddDefaultValue(target, portModel.EmbeddedValue.ObjectValue, true, id);
                if (!ir.AssetReferences.ContainsKey(id))
                    ir.AssetReferences.Add(id,
                        new IRAssetReference(id, ObjectWorldType, portModel.PortDataType,
                            unityObj.name, DFGTranslationHelpers.CreateMessagePassThroughNodeOfType(ir, portModel.PortDataType),
                            isPropagatedReference: false));
            }
        }
    }

    [Serializable]
    class ObjectReferenceConstant<TObject, TEditor> : ReferenceConstant<TObject, TEditor>, IReferenceBoundObject, IValueIRBuilder
    {
        public void Build(BasePortModel portModel, IRPortTarget target, IR ir, IBuildContext context)
        {
            var portType = portModel.PortDataType;
            var objectReferenceInterface =
                portType.GetInterfaces().SingleOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IConvertibleObject<>));
            if (objectReferenceInterface == null)
            {
                ir.CompilationResult.AddError(
                    $"{portModel.Title} port of type {portType.FullName} does not implement IExposedObjectReference", portModel.NodeModel);
                return;
            }

            Type nodeInput = objectReferenceInterface.GetGenericArguments()[0];
            var converterNodeType = typeof(ObjectConverterNode<,>).MakeGenericType(nodeInput, portType);
            var converterNode = ir.CreateNode("ObjectConverterNode", converterNodeType);

            // connect converter node to port
            ir.ConnectSimulation(
                new IRPortTarget(converterNode, "Output"),
                target
            );

            ir.BoundObjectReferences.Add(
                new IRObjectReference(portModel.NodeModel.Guid, portModel.UniqueName, ObjectWorldType,
                    portType, $"{portModel.NodeModel.Guid}_{portModel.UniqueName}", converterNode, isPropagatedReference: false, nodeInput));
        }

        public void UpdateExposedObjects(IChangeEvent evt, Store store, IPortModel portModel)
        {
            var boundObj =
                store.State.WindowState.CurrentGraph.BoundObject;
            ConstantEditorHelper.UpdateExposedObjects(evt, boundObj, portModel);
        }
    }

    internal static partial class ConstantEditorHelper
    {
        public static void UpdateExposedObjects(IChangeEvent evt, GameObject boundObject, IPortModel portModel)
        {
            if (boundObject != null)
            {
                var animGraph = boundObject.GetComponent<AnimationGraph>();
                var exposedObjects = animGraph.ExposedObjects;

                AnimationGraph.ObjectBindingEntry objectBinding =
                    exposedObjects.FirstOrDefault(
                        o => o.TargetGUID.PortUniqueName == portModel.UniqueName &&
                        SerializableGUID.FromParts(o.TargetGUID.NodeIDPart1, o.TargetGUID.NodeIDPart2).GUID == portModel.NodeModel.Guid);
                if (objectBinding != null)
                {
                    var newValue = (evt as ChangeEvent<UnityEngine.Object>).newValue;

                    if (newValue != null)
                        objectBinding.Value = (evt as ChangeEvent<UnityEngine.Object>).newValue;
                    else
                        exposedObjects.Remove(objectBinding);
                }
                else
                {
                    objectBinding = new AnimationGraph.ObjectBindingEntry();

                    objectBinding.TargetGUID.PortUniqueName = portModel.UniqueName;
                    var serializedNodeGUID = (SerializableGUID)portModel.NodeModel.Guid;
                    serializedNodeGUID.ToParts(out objectBinding.TargetGUID.NodeIDPart1, out objectBinding.TargetGUID.NodeIDPart2);

                    objectBinding.Value = portModel.EmbeddedValue.ObjectValue as Object;
                    exposedObjects.Add(objectBinding);
                }
                animGraph.UpdateObjectBindings();
                EditorUtility.SetDirty(boundObject);
                EditorUtility.SetDirty(animGraph);
            }
        }

        public static VisualElement BuildBoundExposedObjectEditor<T>(IConstantEditorBuilder builder)
        {
            var field = new ObjectField();
            field.objectType = typeof(T);
            var root = new VisualElement();
            //Mimic UIElement property fields style
            root.AddToClassList("unity-property-field");
            var boundObject = builder.Store?.State?.WindowState?.CurrentGraph.BoundObject;
            var portModel = builder.PortModel;
            if (boundObject != null)
            {
                var exposedObjects = (boundObject as GameObject).GetComponent<AnimationGraph>().ExposedObjects;
                field.value = exposedObjects.FirstOrDefault(
                    x => SerializableGUID.FromParts(x.TargetGUID.NodeIDPart1, x.TargetGUID.NodeIDPart2).GUID == portModel.NodeModel.Guid &&
                    x.TargetGUID.PortUniqueName == portModel.UniqueName)?.Value;
            }
            else
                field.value = null;
            root.Add(field);
            field.RegisterValueChangedCallback(evt =>
            {
                builder.OnValueChanged(evt);
                UpdateExposedObjects(evt, boundObject, portModel);
            });
            field.SetEnabled(boundObject != null);
            return root;
        }
    }

    [Serializable]
    abstract class ReferenceConstant<TObject, TEditor> : Constant<TObject>, IReferenceConstantProvider
    {
        public Type ObjectWorldType => typeof(TObject);
        public Type GraphType => typeof(TEditor);
    }

    interface IReferenceBoundObject
    {
    }

    interface IReferenceConstantProvider
    {
        Type ObjectWorldType { get; }
        Type GraphType { get; }
    }
}
