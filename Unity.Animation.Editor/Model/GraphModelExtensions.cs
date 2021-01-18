using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace Unity.Animation.Model
{
    internal static class GraphModelExtensions
    {
        internal static IEnumerable<InputComponentFieldVariableDeclarationModel> GetComponentVariableDeclarations(this GraphModel model)
        {
            return model.VariableDeclarations.OfType<InputComponentFieldVariableDeclarationModel>();
        }
    }
}
