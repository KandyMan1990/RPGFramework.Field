using System.Collections.Generic;
using UnityEditor;
using UnityEngine.InputSystem;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// The button actions a script can wait for: those in the project-wide input actions, by name, as the input adapter
    /// routes them, from every map but the UI map — UI Toolkit's, which only the menus route, so a field never sees it.
    /// </summary>
    internal static class InputActionNames
    {
        private const string UI_MAP = "UI";
        private const string PROJECT_WIDE_ACTIONS = "com.unity.input.settings.actions";

        internal static List<string> Gather()
        {
            List<string> names = new List<string>();

            // The project's setting, where the Input System finds them in the editor.
            if (!EditorBuildSettings.TryGetConfigObject(PROJECT_WIDE_ACTIONS, out InputActionAsset actions) || actions == null)
            {
                return names;
            }

            for (int i = 0; i < actions.actionMaps.Count; i++)
            {
                InputActionMap map = actions.actionMaps[i];

                if (map.name == UI_MAP)
                {
                    continue;
                }

                for (int j = 0; j < map.actions.Count; j++)
                {
                    InputAction action = map.actions[j];

                    if (action.type == InputActionType.Button && !names.Contains(action.name))
                    {
                        names.Add(action.name);
                    }
                }
            }

            return names;
        }
    }
}
