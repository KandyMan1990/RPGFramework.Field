using System;
using System.Collections.Generic;
using RPGFramework.Hashing;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// The Field Designer's Encounters tab: the open field's random battles, written on its root's
    /// <see cref="FieldEncounters" /> as the inspector writes them, and saved with the field.
    /// </summary>
    internal sealed class FieldEncountersTab
    {
        private const string NO_ARENA = "(none)";

        private static readonly string[] m_Shares = { "Half the time", "A quarter", "Three sixteenths", "A sixteenth" };

        private readonly VisualElement m_Root;
        private readonly Action        m_Changed;
        private readonly Action        m_Save;

        internal FieldEncountersTab(VisualElement root, Action changed, Action save)
        {
            m_Root    = root;
            m_Changed = changed;
            m_Save    = save;
        }

        internal void Show(GameObject field)
        {
            m_Root.Clear();

            FieldEncounters encounters = field.GetComponent<FieldEncounters>();

            m_Root.Add(new HelpBox("Random battles come as the player walks: about every so many seconds of walking, on average, " +
                                   "0 for none — 30 is sparse, 15 a dungeon's usual pace, 7 very busy. The next is picked from " +
                                   "four slots, so a group in two slots comes up more often; the group fought last is passed " +
                                   "over for the slot after, but for the last slot.", HelpBoxMessageType.None));

            FloatField seconds = new FloatField("Average seconds")
                                 {
                                     isDelayed = true,
                                     value     = encounters != null ? encounters.AverageSeconds : 0f,
                                     tooltip   = "Seconds of walking between random battles, on average; 0 for none"
                                 };

            seconds.RegisterValueChangedCallback(e => SetSeconds(field, math.max(0f, e.newValue)));

            m_Root.Add(seconds);

            if (encounters != null)
            {
                List<string>         arenas = ArenaNames.Gather();
                FieldEncounterSlot[] slots  = encounters.Slots;

                for (int i = 0; i < slots.Length; i++)
                {
                    m_Root.Add(SlotRow(encounters, i, slots[i], arenas));
                }
            }

            m_Root.Add(new Button(m_Save) { text = "Save field" });
        }

        private void SetSeconds(GameObject field, float seconds)
        {
            FieldEncounters encounters = field.GetComponent<FieldEncounters>();

            if (encounters == null)
            {
                if (seconds <= 0f)
                {
                    return;
                }

                encounters = field.AddComponent<FieldEncounters>();

                StartInFirstArena(encounters);
            }

            SerializedObject authored = new SerializedObject(encounters);

            authored.FindProperty("m_AverageSeconds").floatValue = seconds;
            authored.ApplyModifiedPropertiesWithoutUndo();

            m_Changed();

            Show(field);
        }

        /// <summary>As a new block's names start at each picker's first choice.</summary>
        private static void StartInFirstArena(FieldEncounters encounters)
        {
            List<string> arenas = ArenaNames.Gather();

            if (arenas.Count == 0)
            {
                return;
            }

            SerializedObject   authored = new SerializedObject(encounters);
            SerializedProperty slots    = authored.FindProperty("m_Slots");

            for (int i = 0; i < slots.arraySize; i++)
            {
                slots.GetArrayElementAtIndex(i).FindPropertyRelative("m_Arena").ulongValue = Fnv1a64.Hash(arenas[0]);
            }

            authored.ApplyModifiedPropertiesWithoutUndo();
        }

        private VisualElement SlotRow(FieldEncounters encounters, int index, FieldEncounterSlot slot, List<string> arenas)
        {
            VisualElement row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            row.Add(new Label(m_Shares[index]) { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });

            IntegerField group = new IntegerField("Enemy group")
                                 {
                                     isDelayed = true,
                                     value     = slot.EnemyGroup,
                                     tooltip   = "What is fought, as a number the game defines",
                                     style     = { flexGrow = 1 }
                                 };

            group.RegisterValueChangedCallback(e =>
                                               {
                                                   int value = math.clamp(e.newValue, 0, ushort.MaxValue);

                                                   group.SetValueWithoutNotify(value);
                                                   Write(encounters, index, "m_EnemyGroup", p => p.uintValue = (uint)value);
                                               });

            DropdownField arena = new DropdownField("Arena", arenas, 0)
                                  {
                                      tooltip = "Where it is fought, from the project's arena databases",
                                      style   = { flexGrow = 1 }
                                  };

            // What is already written stays shown when nothing answers to it, so opening a field cannot repoint it.
            arena.SetValueWithoutNotify(ArenaNameOf(slot.Arena, arenas));
            arena.RegisterValueChangedCallback(e => Write(encounters, index, "m_Arena", p => p.ulongValue = Fnv1a64.Hash(e.newValue)));

            row.Add(group);
            row.Add(arena);

            return row;
        }

        private void Write(FieldEncounters encounters, int index, string property, Action<SerializedProperty> write)
        {
            SerializedObject authored = new SerializedObject(encounters);

            write(authored.FindProperty("m_Slots").GetArrayElementAtIndex(index).FindPropertyRelative(property));
            authored.ApplyModifiedPropertiesWithoutUndo();

            m_Changed();
        }

        private static string ArenaNameOf(ulong hash, List<string> arenas)
        {
            if (hash == 0)
            {
                return NO_ARENA;
            }

            for (int i = 0; i < arenas.Count; i++)
            {
                if (Fnv1a64.Hash(arenas[i]) == hash)
                {
                    return arenas[i];
                }
            }

            return $"(no arena database holds {hash:X16})";
        }
    }
}
