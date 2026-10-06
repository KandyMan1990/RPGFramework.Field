#if UNITY_EDITOR
namespace RPGFramework.Field
{
    internal static class FieldDebugDrawing
    {
        internal const string INTERACTION_PREF_KEY = "RPGFramework.Field.DrawInteraction";

        internal static bool Interaction
        {
            get
            {
                bool enabled = UnityEditor.EditorPrefs.GetBool(INTERACTION_PREF_KEY, false);

                return enabled;
            }
        }
    }
}
#endif
