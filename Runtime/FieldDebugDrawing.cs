#if UNITY_EDITOR
namespace RPGFramework.Field
{
    public static class FieldDebugDrawing
    {
        public const string INTERACTION_PREF_KEY = "RPGFramework.Field.DrawInteraction";

        public static bool Interaction
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
