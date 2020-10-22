using System.Collections.Generic;
using UnityEngine;

namespace Unity.Animation.Hybrid
{
    /// <summary>
    /// Base class to inherit from in order to implement a dynamic rig channel declarator for known Unity components.
    /// Ideally this will not be needed when we have proper authoring workflow.
    /// </summary>
    /// <typeparam name="T">Component</typeparam>
    internal abstract class RigChannelDeclarator<T> : IDeclareCustomRigChannels
        where T : Component
    {
        public void DeclareRigChannels(RigChannelCollector collector)
        {
            var components = collector.Root.GetComponentsInChildren<T>();
            foreach (var component in components)
                DeclareRigChannels(collector, component);
        }

        protected abstract void DeclareRigChannels(RigChannelCollector collector, T component);
    }

    /// <summary>
    /// Singleton holding all internal RigChannelDeclarators for known Unity component types.
    /// Use <see cref="RigChannelDeclarators.Execute"/> with a <see cref="RigChannelCollector"/> during conversion to append
    /// channel declarations to the rig definition.
    /// </summary>
    internal sealed class RigChannelDeclarators
    {
        List<IDeclareCustomRigChannels> m_RigChannelDeclarators;

        RigChannelDeclarators()
        {
            m_RigChannelDeclarators = new List<IDeclareCustomRigChannels>
            {
                new SkinnedMeshRendererRigChannelDeclarator()

                // Declare more internal rig channel declarators for known Unity types here
                // when needed...
            };
        }

        public static RigChannelDeclarators Instance { get; } = new RigChannelDeclarators();

        public void Execute(RigChannelCollector collector)
        {
            foreach (var declarator in m_RigChannelDeclarators)
                declarator.DeclareRigChannels(collector);
        }
    }
}
