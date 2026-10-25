using UnityEditor;
using UnityEngine;

namespace RPGFramework.Field.Editor
{
    internal static class FieldModuleCreator
    {
        [MenuItem("GameObject/RPG Framework/Field/Field Module", false, 10)]
        private static void Create(MenuCommand command)
        {
            GameObject module = new GameObject("Field Module", typeof(FieldModuleMonoBehaviour));

            GameObjectUtility.SetParentAndAlign(module, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(module, "Create Field Module");
            Selection.activeObject = module;
        }
    }
}
