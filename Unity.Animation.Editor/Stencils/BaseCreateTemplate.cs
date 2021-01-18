using System;
using System.Reflection;
using Unity.Entities;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using Unity.Animation.Hybrid;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class BaseCreateTemplate : ICreatableGraphTemplate
    {
        public BaseCreateTemplate(Type stencilType)
        {
            var ctxAttribute = stencilType.GetCustomAttribute<ContextAttribute>();
            var name = ctxAttribute != null ? ctxAttribute.Name : "Graph";
            StencilType = stencilType;
            GraphTypeName = name;
            DefaultAssetName = name;
        }

        public Type StencilType { get; }

        public string GraphTypeName { get; }
        public string DefaultAssetName { get; }
        public GameObject CurrentGameObject { get; set; }

        public virtual void InitBasicGraph(IGraphModel graphModel)
        {
            var baseModel = graphModel as BaseModel;

            if (CurrentGameObject != null)
            {
                AddAnimationGraphToObject(graphModel.AssetModel, CurrentGameObject);

                var authoringComponent = CurrentGameObject.GetComponent<AnimationGraph>();

                if (authoringComponent != null)
                {
                    foreach (var component in CurrentGameObject.GetComponents(typeof(MonoBehaviour)))
                    {
                        if (component == null)
                            continue;
                        if (AuthoringComponentService.TryGetComponentByAuthoringType(component.GetType(), out var componentInfo))
                        {
                            if (baseModel.GetComponentBinding(componentInfo.RuntimeType) == null)
                            {
                                var binding = baseModel.AddComponentBinding(
                                    componentInfo.RuntimeType.Name,
                                    componentInfo.RuntimeType);
                                authoringComponent.Inputs.Add(
                                    new AnimationGraph.InputBindingEntry()
                                    {
                                        Identification = binding.Identifier.Type.Identification,
                                        Value = component
                                    });
                            }
                        }
                    }
                    authoringComponent.UpdateBindings();
                }
            }
        }

        protected void AddAnimationGraphToObject(IGraphAssetModel graphAssetModel, GameObject target)
        {
            if (target.GetComponent<ConvertToEntity>() == null)
            {
                target.AddComponent<ConvertToEntity>();
            }

            var authoring = target.GetComponent<AnimationGraph>();
            if (authoring == null)
            {
                authoring = target.AddComponent<AnimationGraph>();

                var contextComponent =
                    CurrentGameObject.GetComponent((graphAssetModel.GraphModel.Stencil as BaseStencil).Context.GameObjectContextType);

                authoring.Context = contextComponent;
            }

            authoring.Graph = graphAssetModel as UnityEngine.Object;
        }
    }
}
