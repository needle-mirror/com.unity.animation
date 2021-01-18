using System.Collections.Generic;
using System.Linq;
using Unity.Animation.Hybrid;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    static class AnimationGraphExtensions
    {
        static internal void UpdateBindings(this AnimationGraph animGraph)
        {
            BaseAssetModel assetModel = animGraph.Graph as BaseAssetModel;

            if (assetModel == null)
                return;

            var processed = new HashSet<string>();
            foreach (var b in (assetModel.GraphModel as BaseModel).InputComponentBindings)
            {
                var identification = b.Identifier.Type.Identification;
                var binding = animGraph.Inputs.Where(r => r.Identification == identification).SingleOrDefault();
                if (binding == null)
                {
                    binding = new AnimationGraph.InputBindingEntry() { Identification = identification };
                    animGraph.Inputs.Add(binding);
                }

                processed.Add(identification);
            }

            for (int i = animGraph.Inputs.Count - 1; i >= 0; --i)
            {
                if (!processed.Contains(animGraph.Inputs[i].Identification))
                    animGraph.Inputs.Remove(animGraph.Inputs[i]);
            }
        }

        static public void UpdateObjectBindings(this AnimationGraph graph)
        {
            BaseAssetModel assetModel = graph.Graph as BaseAssetModel;

            if (assetModel == null)
                return;

            var compiledGraph = (assetModel as ICompiledGraphProvider).CompiledGraph;
            if (compiledGraph == null)
                return;

            var processed = new HashSet<PortTargetGUID>();
            var exposedObjectsDict = graph.ExposedObjects.ToDictionary(e => e.TargetGUID);
            foreach (var b in compiledGraph.Definition.ExposedObjects)
            {
                if (!exposedObjectsDict.ContainsKey(b.TargetGUID))
                {
                    graph.ExposedObjects.Add(
                        new AnimationGraph.ObjectBindingEntry()
                        {
                            TargetGUID = b.TargetGUID
                        });
                }

                processed.Add(b.TargetGUID);
            }

            for (int i = graph.ExposedObjects.Count - 1; i >= 0; --i)
            {
                if (!processed.Contains(graph.ExposedObjects[i].TargetGUID))
                {
                    graph.ExposedObjects.Remove(graph.ExposedObjects[i]);
                }
            }
        }
    }
}
