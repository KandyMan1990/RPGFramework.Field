using System.Collections.Generic;
using System.Text;
using RPGFramework.Core.Memory;
using RPGFramework.Hashing;
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
        /// What compiled scripts depend on in the variable map — every variable's name, bank, offset, width and count,
        /// and a record's fields — in an order the list's own does not change. Scripts address variables by the offset
        /// they had when compiled, so fields exported under one layout are wrong under another.
        /// </summary>
        internal static ulong LayoutHash(VariableMapAsset map)
        {
            List<VariableDefinition> variables = new List<VariableDefinition>(map.Variables);

            variables.Sort((a, b) => a.Bank   != b.Bank   ? a.Bank.CompareTo(b.Bank)
                                   : a.Offset != b.Offset ? a.Offset.CompareTo(b.Offset)
                                                          : string.CompareOrdinal(a.Name, b.Name));

            StringBuilder layout = new StringBuilder();

            for (int i = 0; i < variables.Count; i++)
            {
                VariableDefinition variable = variables[i];

                layout.Append(variable.Name).Append(' ').Append(variable.Bank).Append(' ').Append(variable.Offset).Append(' ')
                      .Append(variable.Width).Append(' ').Append(variable.Count);

                IReadOnlyList<VariableRecordField> fields = variable.Fields;

                for (int j = 0; j < fields.Count; j++)
                {
                    VariableRecordField field = fields[j];

                    layout.Append(' ').Append(field.Name).Append(' ').Append(field.Width).Append(' ').Append(field.Count);
                }

                layout.Append('\n');
            }

            ulong hash = Fnv1a64.Hash(layout.ToString());

            return hash;
        }

        /// <summary>
        /// Every script gets a field-wide id, in order, which is what the VM holds its bytecode under. An entity names
        /// its own scripts by event id, so nothing authored refers to these.
        /// </summary>
        internal static List<CompiledFieldEntity> Compile(FieldEntities fieldEntities)
        {
            List<CompiledFieldEntity> compiled = new List<CompiledFieldEntity>(fieldEntities.Entities.Count);
            int                       scriptId = 0;

            List<FieldEntityRecord> records = fieldEntities.Entities;

            for (int i = 0; i < records.Count; i++)
            {
                FieldEntityRecord record = records[i];

                List<CompiledFieldScript> scripts = new List<CompiledFieldScript>(record.Scripts.Count);

                for (int j = 0; j < record.Scripts.Count; j++)
                {
                    FieldScriptRecord script = record.Scripts[j];

                    scripts.Add(new CompiledFieldScript(script.Type, scriptId, FieldScriptCompiler.Compile(script.Text), script.Slot));
                    scriptId++;
                }

                compiled.Add(new CompiledFieldEntity(record.EntityId, record.Body, scripts));
            }

            return compiled;
        }

        /// <summary>
        /// Every animation name the field's scripts mention. A script compiles a name to its hash, which an Animator
        /// cannot be addressed by, so the names travel with the field for the module to resolve against.
        /// </summary>
        internal static List<string> CollectAnimationNames(FieldEntities fieldEntities)
        {
            SortedSet<string> animationNames = new SortedSet<string>();

            List<FieldEntityRecord> records = fieldEntities.Entities;

            for (int i = 0; i < records.Count; i++)
            {
                FieldEntityRecord record = records[i];

                List<FieldScriptRecord> scripts = record.Scripts;

                for (int j = 0; j < scripts.Count; j++)
                {
                    FieldScriptRecord script = scripts[j];

                    CollectAnimationNames(script.Text, animationNames);
                }
            }

            return new List<string>(animationNames);
        }

        private static void CollectAnimationNames(string scriptText, SortedSet<string> animationNames)
        {
            string[] lines = scriptText.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                string[] parts = FieldScriptLine.Split(line);

                if (!FieldOpCodeCatalogue.TryGet(parts[0], out FieldOpCodeInfo opCode))
                {
                    continue;
                }

                for (int argument = 0; argument < opCode.Arguments.Count; argument++)
                {
                    if (opCode.Arguments[argument].Type != ArgumentType.AnimationName)
                    {
                        continue;
                    }

                    // A malformed line is export's to report, not this pass's; it collects what is there.
                    if (argument + 1 < parts.Length)
                    {
                        animationNames.Add(parts[argument + 1]);
                    }
                }
            }
        }

        /// <returns>The copy's asset path. It keeps the field's name, which is the name the game loads it by.</returns>
        internal static string WriteBuildCopy(GameObject prefab)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));

            try
            {
                FieldEntities fieldEntities = contents.GetComponent<FieldEntities>();
                fieldEntities.SetCompiled(Compile(fieldEntities), CollectAnimationNames(fieldEntities));

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
