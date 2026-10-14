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
    /// A field chosen by name for a variable's default; the variable holds the name's hash.
    /// </summary>
    internal abstract class FieldNameDefaultField : IVariableDefaultField
    {
        protected abstract string VariableName { get; }

        protected abstract string Label { get; }

        string IVariableDefaultField.VariableName => VariableName;

        VisualElement IVariableDefaultField.Create(SerializedProperty variable)
        {
            SerializedProperty defaultValue = variable.FindPropertyRelative(VariableDefinitionDrawer.DEFAULT_VALUE);
            List<string>       names        = FieldScriptBlockEditor.GatherFieldNames();

            if (names.Count == 0)
            {
                HelpBox none = new HelpBox("No fields in a field database yet, so there is none to choose.", HelpBoxMessageType.Warning);

                return none;
            }

            string current = NameForHash(names, defaultValue.ulongValue);

            DropdownField field = new DropdownField(Label, names, Mathf.Max(0, names.IndexOf(current)));

            if (current == null)
            {
                field.SetValueWithoutNotify(string.Empty);
            }

            field.RegisterValueChangedCallback(e => VariableDefinitionDrawer.Write(defaultValue, Fnv1a64.Hash(e.newValue)));

            return field;
        }

        internal static string NameForHash(List<string> names, ulong hash)
        {
            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];

                if (Fnv1a64.Hash(name) == hash)
                {
                    return name;
                }
            }

            return null;
        }
    }

    /// <summary>The field a new game begins in.</summary>
    internal sealed class CurrentFieldDefaultField : FieldNameDefaultField
    {
        protected override string VariableName => FieldVariables.CURRENT_FIELD;

        protected override string Label => "Starts in field";
    }

    /// <summary>The field a game over sends the player to.</summary>
    internal sealed class GameOverFieldDefaultField : FieldNameDefaultField
    {
        protected override string VariableName => FieldVariables.GAME_OVER_FIELD;

        protected override string Label => "Game over field";
    }

    /// <summary>
    /// A spawn point for a variable's default, chosen from the spawn points of the field another variable defaults to,
    /// and rebuilt when that changes.
    /// </summary>
    internal abstract class SpawnDefaultField : IVariableDefaultField
    {
        protected abstract string VariableName { get; }

        // The variable naming the field whose spawn points are offered.
        protected abstract string FieldVariableName { get; }

        string IVariableDefaultField.VariableName => VariableName;

        VisualElement IVariableDefaultField.Create(SerializedProperty variable)
        {
            VisualElement      slot         = new VisualElement();
            SerializedProperty fieldDefault = FindDefault(variable.serializedObject, FieldVariableName);

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

            string fieldName = fieldDefault == null ? null : FieldNameDefaultField.NameForHash(FieldScriptBlockEditor.GatherFieldNames(), fieldDefault.ulongValue);

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

    /// <summary>Where in its field a new game begins.</summary>
    internal sealed class CurrentSpawnDefaultField : SpawnDefaultField
    {
        protected override string VariableName => FieldVariables.CURRENT_SPAWN;

        protected override string FieldVariableName => FieldVariables.CURRENT_FIELD;
    }

    /// <summary>Where in the game-over field the player arrives.</summary>
    internal sealed class GameOverSpawnDefaultField : SpawnDefaultField
    {
        protected override string VariableName => FieldVariables.GAME_OVER_SPAWN;

        protected override string FieldVariableName => FieldVariables.GAME_OVER_FIELD;
    }
}