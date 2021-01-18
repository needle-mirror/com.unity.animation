using UnityEditor.GraphToolsFoundation.Overdrive;

namespace Unity.Animation.Editor
{
    sealed class Preferences : UnityEditor.GraphToolsFoundation.Overdrive.Preferences
    {
        public static Preferences CreatePreferences()
        {
            var preferences = new Preferences();
            preferences.Initialize<BoolPref, IntPref>();
            return preferences;
        }

        Preferences() {}

        const string k_EditorPrefPrefix = "AnimationGraph.";
        protected override string GetEditorPreferencesPrefix()
        {
            return k_EditorPrefPrefix;
        }
    }
}
