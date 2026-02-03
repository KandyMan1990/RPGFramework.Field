using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;

namespace RPGFramework.Field
{
    [CustomEditor(typeof(FieldScriptSource))]
    public sealed class FieldScriptSourceEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            FieldScriptSource source = (FieldScriptSource)target;

            if (GUILayout.Button("Compile Script"))
            {
                Compile(source);
            }
        }

        private static void Compile(FieldScriptSource source)
        {
            byte[] bytecode = FieldScriptCompiler.Compile(source.ScriptText);

            FieldCompiledScript compiled = CreateInstance<FieldCompiledScript>();
            compiled.ScriptId = source.ScriptId;
            compiled.Bytecode = bytecode;

            string path = AssetDatabase.GetAssetPath(source);
            path = Path.ChangeExtension(path, ".compiled.asset");

            AssetDatabase.CreateAsset(compiled, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"Compiled script {source.name} → {path}");
        }
    }
}
#endif

namespace RPGFramework.Field
{
    [CreateAssetMenu(menuName = "RPG Framework/Field/Script Source", fileName = "FieldScriptSource")]
    public sealed class FieldScriptSource : ScriptableObject
    {
        [Tooltip("Unique script ID used by the VM")]
        public int ScriptId;

        [TextArea(10, 30)]
        public string ScriptText;
    }
}