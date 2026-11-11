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
        private const string NONE = "(none)";

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

            FieldEncounters      encounters = field.GetComponent<FieldEncounters>();
            FieldEncounterConfig config     = CountingConfig();

            m_Root.Add(new HelpBox("Random battles come as the player walks, as often as the rate says, 0 for none; the game's " +
                                   "EncounterRate variable scales every field's. The next is picked from four slots, each an " +
                                   "encounter and where it is fought, by a roll the encounter config divides among them, so " +
                                   "an encounter in two slots comes up more often; the encounter fought last is passed over " +
                                   "for the slot after, but for the last slot.", HelpBoxMessageType.None));

            byte rate = encounters != null ? encounters.Rate : (byte)0;

            SliderInt slider = new SliderInt("Rate", 0, byte.MaxValue)
                               {
                                   showInputField = true,
                                   value          = rate,
                                   tooltip        = "How often random battles come, 0 for none"
                               };

            Label         seconds = new Label(SecondsText(config, rate));
            seconds.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
            VisualElement slots   = new VisualElement();

            slider.RegisterValueChangedCallback(e =>
                                                {
                                                    byte value = (byte)math.clamp(e.newValue, 0, byte.MaxValue);

                                                    seconds.text = SecondsText(config, value);
                                                    SetRate(field, value, config, slots);
                                                });

            m_Root.Add(slider);
            m_Root.Add(seconds);
            m_Root.Add(slots);

            if (encounters != null)
            {
                ShowSlots(encounters, config, slots);
            }

            m_Root.Add(new Button(m_Save) { text = "Save field" });
        }

        // The game's own where it has made one, as its field module will be given it, and Field's otherwise.
        private static FieldEncounterConfig CountingConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(FieldEncounterConfig));

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                if (path.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    return AssetDatabase.LoadAssetAtPath<FieldEncounterConfig>(path);
                }
            }

            return FieldEncounterConfig.LoadDefault();
        }

        private static string SecondsText(FieldEncounterConfig config, byte rate)
        {
            if (rate == 0)
            {
                return "No random battles.";
            }

            float walking = EncounterMeter.AverageSeconds(config, rate, config.DangerPerCheckWalking);
            float running = EncounterMeter.AverageSeconds(config, rate, config.DangerPerCheckRunning);

            return $"A battle about every {walking:0.#} seconds of walking, on average, or {running:0.#} of running once running " +
                   $"is built, as {config.name} counts.";
        }

        // The slots are drawn in a container of their own, so the slider is not rebuilt under the pointer as it adds the table.
        private void SetRate(GameObject field, byte rate, FieldEncounterConfig config, VisualElement slots)
        {
            FieldEncounters encounters = field.GetComponent<FieldEncounters>();

            if (encounters == null)
            {
                if (rate == 0)
                {
                    return;
                }

                encounters = field.AddComponent<FieldEncounters>();

                StartAtFirstChoices(encounters);
                ShowSlots(encounters, config, slots);
            }

            SerializedObject authored = new SerializedObject(encounters);

            authored.FindProperty("m_Rate").intValue = rate;
            authored.ApplyModifiedPropertiesWithoutUndo();

            m_Changed();
        }

        /// <summary>As a new block's names start at each picker's first choice.</summary>
        private static void StartAtFirstChoices(FieldEncounters encounters)
        {
            List<string> encounterNames = EncounterNames.Gather();
            List<string> arenaNames     = ArenaNames.Gather();

            SerializedObject   authored = new SerializedObject(encounters);
            SerializedProperty slots    = authored.FindProperty("m_Slots");

            for (int i = 0; i < slots.arraySize; i++)
            {
                SerializedProperty slot = slots.GetArrayElementAtIndex(i);

                slot.FindPropertyRelative("m_Encounter").ulongValue = encounterNames.Count > 0 ? Fnv1a64.Hash(encounterNames[0]) : 0;
                slot.FindPropertyRelative("m_Arena").ulongValue     = arenaNames.Count > 0 ? Fnv1a64.Hash(arenaNames[0]) : 0;
            }

            authored.ApplyModifiedPropertiesWithoutUndo();
        }

        private void ShowSlots(FieldEncounters encounters, FieldEncounterConfig config, VisualElement slots)
        {
            List<string>         encounterNames = EncounterNames.Gather();
            List<string>         arenaNames     = ArenaNames.Gather();
            FieldEncounterSlot[] chosen         = encounters.Slots;

            slots.Clear();

            for (int i = 0; i < chosen.Length; i++)
            {
                slots.Add(SlotRow(encounters, i, Share(config, i), chosen[i], encounterNames, arenaNames));
            }
        }

        // The slot's part of the roll, before a slot holding the last encounter fought passes it on.
        private static float Share(FieldEncounterConfig config, int index)
        {
            int[] limits = config.SlotRollLimits;
            int   from   = index > 0 ? limits[index - 1] : 0;
            int   to     = index < limits.Length ? limits[index] : FieldEncounterConfig.ROLLS;

            return 100f * (to - from) / FieldEncounterConfig.ROLLS;
        }

        private VisualElement SlotRow(FieldEncounters encounters, int index, float share, FieldEncounterSlot slot, List<string> encounterNames, List<string> arenaNames)
        {
            VisualElement row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            row.Add(new Label($"Slot {index + 1}, {share:0.##}%")
                    {
                        tooltip = "How often a roll picks this slot, before a slot holding the last encounter fought passes it to the next",
                        style   = { width = 110, unityTextAlign = TextAnchor.MiddleLeft }
                    });
            row.Add(Picker("Encounter", "What is fought and at what level, from the project's encounter databases", encounterNames, slot.Encounter,
                           hash => Write(encounters, index, "m_Encounter", hash)));
            row.Add(Picker("Arena", "Where it is fought, from the project's arena databases", arenaNames, slot.Arena,
                           hash => Write(encounters, index, "m_Arena", hash)));

            return row;
        }

        private static DropdownField Picker(string label, string tooltip, List<string> names, ulong chosen, Action<ulong> write)
        {
            DropdownField picker = new DropdownField(label, names, 0)
                                   {
                                       tooltip = tooltip,
                                       style   = { flexGrow = 1 }
                                   };

            // What is already written stays shown when nothing answers to it, so opening a field cannot repoint it.
            picker.SetValueWithoutNotify(NameOf(chosen, names));
            picker.RegisterValueChangedCallback(e => write(Fnv1a64.Hash(e.newValue)));

            return picker;
        }

        private void Write(FieldEncounters encounters, int index, string property, ulong hash)
        {
            SerializedObject authored = new SerializedObject(encounters);

            authored.FindProperty("m_Slots").GetArrayElementAtIndex(index).FindPropertyRelative(property).ulongValue = hash;
            authored.ApplyModifiedPropertiesWithoutUndo();

            m_Changed();
        }

        private static string NameOf(ulong hash, List<string> names)
        {
            if (hash == 0)
            {
                return NONE;
            }

            for (int i = 0; i < names.Count; i++)
            {
                if (Fnv1a64.Hash(names[i]) == hash)
                {
                    return names[i];
                }
            }

            return $"(no database holds {hash:X16})";
        }
    }
}
