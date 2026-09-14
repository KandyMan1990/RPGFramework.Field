using System.IO;
using UnityEditor;

namespace RPGFramework.Field.Editor
{
    internal static class FieldCompiledScriptAssets
    {
        /// <summary>
        /// Write a source's bytecode to its compiled asset, updating the asset in place when it exists.
        /// </summary>
        internal static string Write(FieldScriptSource source, byte[] bytecode)
        {
            string path = Path.ChangeExtension(AssetDatabase.GetAssetPath(source), ".compiled.asset");

            FieldCompiledScript compiled = AssetDatabase.LoadAssetAtPath<FieldCompiledScript>(path);

            if (compiled == null)
            {
                compiled = UnityEngine.ScriptableObject.CreateInstance<FieldCompiledScript>();
                AssetDatabase.CreateAsset(compiled, path);
            }

            compiled.ScriptId      = source.ScriptId;
            compiled.FormatVersion = FieldCompiledScript.CURRENT_FORMAT_VERSION;
            compiled.Bytecode      = bytecode;

            EditorUtility.SetDirty(compiled);
            AssetDatabase.SaveAssets();

            return path;
        }
    }
}
