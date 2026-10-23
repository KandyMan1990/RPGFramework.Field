using System;
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

        VisualElement IVariableDefaultField.Create(ulong value, Action<ulong> write, IVariableValueContext context)
        {
            List<string> names = FieldScriptBlockEditor.GatherFieldNames();

            if (names.Count == 0)
            {
                HelpBox none = new HelpBox("No fields in a field database yet, so there is none to choose.", HelpBoxMessageType.Warning);

                return none;
            }

            string current = NameForHash(names, value);

            DropdownField field = new DropdownField(Label, names, Mathf.Max(0, names.IndexOf(current)));

            if (current == null)
            {
                field.SetValueWithoutNotify(string.Empty);
            }

            field.RegisterValueChangedCallback(e => write(Fnv1a64.Hash(e.newValue)));

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

        // Rebuilt as the field changes, the spawn point read afresh each time from where it is set.
        VisualElement IVariableDefaultField.Create(ulong value, Action<ulong> write, IVariableValueContext context)
        {
            VisualElement slot = new VisualElement();

            Build(slot, value, write, context);

            context.Watch(slot, FieldVariableName, () => Build(slot, context.ValueOf(VariableName), write, context));

            return slot;
        }

        private void Build(VisualElement slot, ulong value, Action<ulong> write, IVariableValueContext context)
        {
            slot.Clear();

            int   spawnId   = (int)new VariableBits(value).ToInteger(VariableWidth.Int);
            ulong fieldHash = context.ValueOf(FieldVariableName);

            string fieldName = fieldHash == 0 ? null : FieldNameDefaultField.NameForHash(FieldScriptBlockEditor.GatherFieldNames(), fieldHash);

            List<FieldScriptBlockEditor.SpawnChoice> spawns = fieldName == null ? new List<FieldScriptBlockEditor.SpawnChoice>() : FieldScriptBlockEditor.GatherSpawnChoices(fieldName);

            if (spawns.Count == 0)
            {
                // No field chosen yet, or it has no spawn points: a number beats an empty list.
                IntegerField typed = new IntegerField("Arrives at spawn") { value = spawnId };
                typed.RegisterValueChangedCallback(e => write(new VariableBits(e.newValue, VariableWidth.Int).Bits));

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

            field.RegisterValueChangedCallback(e => write(new VariableBits(spawns[labels.IndexOf(e.newValue)].Id, VariableWidth.Int).Bits));

            slot.Add(field);
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