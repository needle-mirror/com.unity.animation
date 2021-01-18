using System;
using System.Linq;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace Unity.Animation.Editor
{
    internal interface AnimationOnboardingProvider : IOnboardingProvider {}

    internal class BlankPage : UnityEditor.GraphToolsFoundation.Overdrive.BlankPage
    {
        public static readonly string ussClassName = "vse-blank-page";

        public BlankPage(Store store) : base(store)
        {
            OnboardingProviders = TypeCache.GetTypesDerivedFrom<AnimationOnboardingProvider>()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Select(templateType => (IOnboardingProvider)Activator.CreateInstance(templateType))
                .ToList();
        }

        public override void CreateUI()
        {
            AddToClassList(ussClassName);
            base.CreateUI();
        }
    }
}
