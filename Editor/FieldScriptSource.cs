using UnityEngine;
using System.IO;
using UnityEditor;

namespace RPGFramework.Field.Editor
{
    [CustomEditor(typeof(FieldScriptSource))]
    public sealed class FieldScriptSourceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Compile Script"))
            {
                FieldScriptSource source = (FieldScriptSource)target;
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

namespace RPGFramework.Field.Editor
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