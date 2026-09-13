using UnityEditor;

namespace RPGFramework.Field.Editor
{
    internal static class FieldDebugMenu
    {
        private const string INTERACTION_MENU = "RPG Framework/Field/Draw Interaction Debug";

        [MenuItem(INTERACTION_MENU)]
        private static void ToggleInteraction()
        {
            EditorPrefs.SetBool(FieldDebugDrawing.INTERACTION_PREF_KEY, !FieldDebugDrawing.Interaction);
        }

        [MenuItem(INTERACTION_MENU, true)]
        private static bool ValidateInteraction()
        {
            Menu.SetChecked(INTERACTION_MENU, FieldDebugDrawing.Interaction);

            return true;
        }
    }
}
