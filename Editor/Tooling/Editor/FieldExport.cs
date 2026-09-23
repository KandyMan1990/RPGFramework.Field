using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// Turns a field prefab as it is authored into the one the game loads: every entity's scripts compiled, the text
    /// and names left behind, written to a copy so the prefab being edited never holds bytecode.
    /// </summary>
    internal static class FieldExport
    {
        /// <summary>
        /// Where the copies are written for the bundle build, and deleted from afterwards. Inside Assets because only
        /// assets can be bundled.
        /// </summary>
        internal const string BUILD_COPY_FOLDER = "Assets/RPGFrameworkFieldBuild";

        /// <summary>
        /// Every script gets a field-wide id, in order, which is what the VM holds its bytecode under. An entity names
        /// its own scripts by event id, so nothing authored refers to these.
        /// </summary>
        internal static List<CompiledFieldEntity> Compile(FieldEntities fieldEntities)
        {
            List<CompiledFieldEntity> compiled = new List<CompiledFieldEntity>(fieldEntities.Entities.Count);
            int                       scriptId = 0;

            foreach (FieldEntityRecord record in fieldEntities.Entities)
            {
                List<CompiledFieldScript> scripts = new List<CompiledFieldScript>(record.Scripts.Count);

                foreach (FieldScriptRecord script in record.Scripts)
                {
                    scripts.Add(new CompiledFieldScript(script.Type, scriptId, FieldScriptCompiler.Compile(script.Text)));
                    scriptId++;
                }

                compiled.Add(new CompiledFieldEntity(record.EntityId, record.Body, scripts));
            }

            return compiled;
        }

        /// <returns>The copy's asset path. It keeps the field's name, which is the name the game loads it by.</returns>
        internal static string WriteBuildCopy(GameObject prefab)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));

            try
            {
                FieldEntities fieldEntities = contents.GetComponent<FieldEntities>();
                fieldEntities.SetCompiled(Compile(fieldEntities));

                string copyPath = $"{BUILD_COPY_FOLDER}/{prefab.name}.prefab";

                PrefabUtility.SaveAsPrefabAsset(contents, copyPath);

                return copyPath;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}
