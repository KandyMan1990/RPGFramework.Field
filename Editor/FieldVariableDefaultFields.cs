using System.Collections.Generic;
using RPGFramework.Core.Editor;
using RPGFramework.Core.Memory;
using RPGFramework.Hashing;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// The field a new game begins in, chosen by name; the variable holds the name's hash.
    /// </summary>
    internal sealed class CurrentFieldDefaultField : IVariableDefaultField
    {
        public string VariableName => FieldVariables.CURRENT_FIELD;

        public VisualElement Create(SerializedProperty variable)
        {
            SerializedProperty defaultValue = variable.FindPropertyRelative(VariableDefinitionDrawer.DEFAULT_VALUE);
            List<string>       names        = FieldScriptBlockEditor.GatherFieldNames();

            if (names.Count == 0)
            {
                HelpBox none = new HelpBox("No fields in a field database yet, so there is nowhere to begin.", HelpBoxMessageType.Warning);

                return none;
            }

            string current = FieldNameForHash(names, defaultValue.ulongValue);

            DropdownField field = new DropdownField("Starts in field", names, Mathf.Max(0, names.IndexOf(current)));

            if (current == null)
            {
                field.SetValueWithoutNotify(string.Empty);
            }

            field.RegisterValueChangedCallback(e => VariableDefinitionDrawer.Write(defaultValue, Fnv1a64.Hash(e.newValue)));

            return field;
        }

        internal static string FieldNameForHash(List<string> names, ulong hash)
        {
            foreach (string name in names)
            {
                if (Fnv1a64.Hash(name) == hash)
                {
                    return name;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Where in that field a new game begins, chosen from the spawn points of the field
    /// <see cref="FieldVariables.CURRENT_FIELD" /> defaults to, and rebuilt when that changes.
    /// </summary>
    internal sealed class CurrentSpawnDefaultField : IVariableDefaultField
    {
        public string VariableName => FieldVariables.CURRENT_SPAWN;

        public VisualElement Create(SerializedProperty variable)
        {
            VisualElement      slot         = new VisualElement();
            SerializedProperty fieldDefault = FindDefault(variable.serializedObject, FieldVariables.CURRENT_FIELD);

            Build(slot, variable, fieldDefault);

            if (fieldDefault != null)
            {
                slot.TrackPropertyValue(fieldDefault, _ => Build(slot, variable, fieldDefault));
            }

            return slot;
        }

        private static void Build(VisualElement slot, SerializedProperty variable, SerializedProperty fieldDefault)
        {
            slot.Clear();

            SerializedProperty defaultValue = variable.FindPropertyRelative(VariableDefinitionDrawer.DEFAULT_VALUE);
            int                spawnId      = (int)VariableDefaults.ToInteger(defaultValue.ulongValue, VariableWidth.Int);

            string fieldName = fieldDefault == null ? null : CurrentFieldDefaultField.FieldNameForHash(FieldScriptBlockEditor.GatherFieldNames(), fieldDefault.ulongValue);

            List<FieldScriptBlockEditor.SpawnChoice> spawns = fieldName == null ? new List<FieldScriptBlockEditor.SpawnChoice>() : FieldScriptBlockEditor.GatherSpawnChoices(fieldName);

            if (spawns.Count == 0)
            {
                // No field chosen yet, or it has no spawn points: a number beats an empty list.
                IntegerField typed = new IntegerField("Arrives at spawn") { value = spawnId };
                typed.RegisterValueChangedCallback(e => VariableDefinitionDrawer.Write(defaultValue, VariableDefaults.FromInteger(e.newValue, VariableWidth.Int)));

                slot.Add(typed);
                return;
            }

            List<string> labels = new List<string>(spawns.Count);
            int          chosen = -1;

            for (int i = 0; i < spawns.Count; i++)
            {
                labels.Add(spawns[i].Label);

                if (spawns[i].Id == spawnId)
                {
                    chosen = i;
                }
            }

            DropdownField field = new DropdownField("Arrives at spawn", labels, Mathf.Max(0, chosen));

            if (chosen < 0)
            {
                field.SetValueWithoutNotify(spawnId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            field.RegisterValueChangedCallback(e => VariableDefinitionDrawer.Write(defaultValue, VariableDefaults.FromInteger(spawns[labels.IndexOf(e.newValue)].Id, VariableWidth.Int)));

            slot.Add(field);
        }

        private static SerializedProperty FindDefault(SerializedObject map, string variableName)
        {
            SerializedProperty variables = map.FindProperty("m_Variables");

            for (int i = 0; i < variables.arraySize; i++)
            {
                SerializedProperty variable = variables.GetArrayElementAtIndex(i);

                if (variable.FindPropertyRelative("m_Name").stringValue == variableName)
                {
                    return variable.FindPropertyRelative(VariableDefinitionDrawer.DEFAULT_VALUE);
                }
            }

            return null;
        }
    }
}
