using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Field.Editor
{
    internal static class FieldModuleCreator
    {
        [MenuItem("GameObject/RPG Framework/Field/Field Module", false, 10)]
        private static void Create(MenuCommand command)
        {
            GameObject               module    = new GameObject("Field Module");
            FieldModuleMonoBehaviour behaviour = module.AddComponent<FieldModuleMonoBehaviour>();

            // Each field holds the Cinemachine camera it is seen through, which a field loading cuts straight to.
            GameObject cameraObject = new GameObject("Camera");
            cameraObject.transform.SetParent(module.transform, false);
            cameraObject.AddComponent<Camera>();

            CinemachineBrain brain = cameraObject.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);

            SerializedObject serializedModule = new SerializedObject(behaviour);
            serializedModule.FindProperty("m_CinemachineBrain").objectReferenceValue = brain;
            serializedModule.ApplyModifiedPropertiesWithoutUndo();

            GameObjectUtility.SetParentAndAlign(module, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(module, "Create Field Module");
            Selection.activeObject = module;
        }
    }
}
