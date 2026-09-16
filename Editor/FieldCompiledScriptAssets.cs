using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Compile every script source in the project, so an export never ships bytecode built against an older
        /// opcode table.
        /// </summary>
        /// <returns>A problem for each script that failed to compile.</returns>
        internal static List<string> CompileAll()
        {
            List<string> problems = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(FieldScriptSource)}"))
            {
                string            path   = AssetDatabase.GUIDToAssetPath(guid);
                FieldScriptSource source = AssetDatabase.LoadAssetAtPath<FieldScriptSource>(path);

                try
                {
                    byte[] bytecode = FieldScriptCompiler.Compile(source.ScriptText);

                    Write(source, bytecode);
                }
                catch (Exception e)
                {
                    problems.Add($"{path}: {e.Message}");
                }
            }

            return problems;
        }
    }
}
