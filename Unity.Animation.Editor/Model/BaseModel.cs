using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using Unity.Animation.Hybrid;

namespace Unity.Animation.Model
{
    [Serializable]
    internal abstract class BaseModel : GraphModel
    {
        [SerializeField, HideInInspector]
        internal List<ComponentBinding> InputComponentBindings = new List<ComponentBinding>();

        public ComponentBinding AddComponentBinding(string Name, Type runtimeType)
        {
            if (runtimeType == null)
                throw new ArgumentNullException($"AddComponentBinding : null Component Type");

            if (InputComponentBindings.Any(i => i.Name == Name))
                throw new ArgumentException($"AddComponentBinding : {Name} Component already exists");

            if (!AuthoringComponentService.TryGetComponentByRuntimeType(runtimeType, out _))
                throw new ArgumentException($"AddComponentBinding : {runtimeType} is not a valid Component Type");

            var input =
                new ComponentBinding()
            {
                Name = Name,
                Identifier = new ComponentBindingIdentifier(){ Type = runtimeType.GenerateTypeHandle() }
            };
            InputComponentBindings.Add(input);
            UpdateInputDeclarationModels();
            return input;
        }

        internal ComponentBinding GetComponentBinding(string name)
        {
            return InputComponentBindings.Find(i => i.Name == name);
        }

        internal ComponentBinding GetComponentBinding(Type type)
        {
            return InputComponentBindings.Find(i => i.Identifier.Type.Resolve() == type);
        }

        internal bool TryGetComponentBinding(ComponentBindingIdentifier id, out ComponentBinding binding)
        {
            binding = InputComponentBindings.Find(i => i.Identifier == id);
            return binding != null;
        }

        internal void RemoveComponentBinding(ComponentBinding item)
        {
            InputComponentBindings.Remove(item);

            DeleteVariableDeclarations(this.GetComponentVariableDeclarations().Where(p => p.Identifier == item.Identifier).ToList(), true);
        }

        internal void RemoveComponentBinding(ComponentBindingIdentifier id)
        {
            InputComponentBindings.RemoveAll(c => c.Identifier == id);

            DeleteVariableDeclarations(this.GetComponentVariableDeclarations().Where(p => p.Identifier == id).ToList(), true);
        }

        internal IEnumerable<InputComponentFieldVariableModel> FindUsages(InputComponentFieldVariableDeclarationModel decl)
        {
            return this.FindReferencesInGraph(decl).Cast<InputComponentFieldVariableModel>();
        }

        public InputComponentFieldVariableDeclarationModel CreateComponentFieldVariableDeclaration(
            FieldInfo fieldInfo, ComponentBinding item)
        {
            var field = InputComponentFieldVariableDeclarationModel.Create(fieldInfo, item, this);
            m_GraphVariableModels.Add(field);
            return field;
        }

        void UpdateInputDeclarationModels()
        {
            foreach (var input in InputComponentBindings)
            {
                if (input == null || input.Identifier.Type == null)
                    continue;

                var variableDeclarations =
                    this.GetComponentVariableDeclarations().Where(p => p.Identifier == input.Identifier).ToDictionary(b => b.FieldHandle.Resolve());

                var processed = new HashSet<InputComponentFieldVariableDeclarationModel>();
                if (AuthoringComponentService.TryGetComponentByRuntimeType(input.Identifier.Type.Resolve(), out var componentInfo))
                {
                    foreach (var f in componentInfo.RuntimeFields)
                    {
                        InputComponentFieldVariableDeclarationModel decl = null;
                        if (!variableDeclarations.TryGetValue(f.Key, out decl))
                            decl = CreateComponentFieldVariableDeclaration(f.Value.FieldInfo, input);
                        processed.Add(decl);
                    }
                }
                var declarationsArray = variableDeclarations.ToArray();
                for (int i = declarationsArray.Count() - 1; i >= 0; --i)
                {
                    if (!processed.Contains(declarationsArray[i].Value))
                    {
                        variableDeclarations.Remove(declarationsArray[i].Key);
                    }
                }
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();

            UpdateInputDeclarationModels();

            foreach (var nodeModel in NodeModels)
                (nodeModel as IPreDefineNode)?.PreDefineNode();
            foreach (var nodeModel in NodeModels)
                (nodeModel as NodeModel)?.DefineNode();
            foreach (var nodeModel in NodeModels)
                (nodeModel as IPostDefineNode)?.PostDefineNode();
        }

        public override void CloneGraph(IGraphModel sourceGraph)
        {
            var baseModel = sourceGraph as BaseModel;
            foreach (var binding in baseModel.InputComponentBindings)
            {
                var input =
                    new ComponentBinding()
                {
                    Name = binding.Name,
                    Identifier = new ComponentBindingIdentifier(){ Type = binding.Identifier.Type }
                };
                InputComponentBindings.Add(input);
            }
            UpdateInputDeclarationModels();

            base.CloneGraph(sourceGraph);
        }
    }
}
