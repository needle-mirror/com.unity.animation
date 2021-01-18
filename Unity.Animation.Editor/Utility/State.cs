using UnityEditor;

namespace Unity.Animation.Editor
{
    internal class State : UnityEditor.GraphToolsFoundation.Overdrive.State
    {
        static UnityEditor.GraphToolsFoundation.Overdrive.Preferences s_EditorPrefs = Editor.Preferences.CreatePreferences();

        public State(GUID graphViewEditorWindowGUID)
            : base(graphViewEditorWindowGUID, s_EditorPrefs) {}

        ~State() => Dispose(false);
    }
}
