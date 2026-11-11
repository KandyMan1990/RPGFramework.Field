using RPGFramework.Hashing;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Field.Tests.Encounters
{
    /// <summary>A field's root with a table, and an encounter config, each authored as the inspector authors it.</summary>
    internal static class EncounterTable
    {
        internal const string ENCOUNTER_NAME = "Goblins";
        internal const string ARENA_NAME     = "Forest";

        internal static readonly ulong ENCOUNTER = Fnv1a64.Hash(ENCOUNTER_NAME);
        internal static readonly ulong ARENA     = Fnv1a64.Hash(ARENA_NAME);

        /// <summary>A table of the encounters given, every slot fought in the one arena.</summary>
        internal static GameObject Make(byte rate, params ulong[] encounters)
        {
            GameObject       field    = new GameObject("Field");
            SerializedObject authored = new SerializedObject(field.AddComponent<FieldEncounters>());

            authored.FindProperty("m_Rate").intValue = rate;

            SerializedProperty slots = authored.FindProperty("m_Slots");

            for (int i = 0; i < encounters.Length; i++)
            {
                SerializedProperty slot = slots.GetArrayElementAtIndex(i);

                slot.FindPropertyRelative("m_Encounter").ulongValue = encounters[i];
                slot.FindPropertyRelative("m_Arena").ulongValue     = ARENA;
            }

            authored.ApplyModifiedPropertiesWithoutUndo();

            return field;
        }

        /// <summary>A config as it is made.</summary>
        internal static FieldEncounterConfig Config()
        {
            return ScriptableObject.CreateInstance<FieldEncounterConfig>();
        }

        /// <summary>A config whose rate of 1 brings a check every second, so a walk's seconds are its checks.</summary>
        internal static FieldEncounterConfig OneCheckASecond()
        {
            FieldEncounterConfig config   = Config();
            SerializedObject     authored = new SerializedObject(config);

            authored.FindProperty("m_TicksPerSecond").intValue = 1;
            authored.FindProperty("m_TotalPerCheck").intValue      = 1;
            authored.ApplyModifiedPropertiesWithoutUndo();

            return config;
        }

        /// <summary>A config whose checks while walking raise the danger by the step given.</summary>
        internal static FieldEncounterConfig DangerPerCheckWalking(int step)
        {
            FieldEncounterConfig config   = OneCheckASecond();
            SerializedObject     authored = new SerializedObject(config);

            authored.FindProperty("m_DangerPerCheckWalking").intValue = step;
            authored.ApplyModifiedPropertiesWithoutUndo();

            return config;
        }

        /// <summary>A config dividing the roll among the slots at the limits given.</summary>
        internal static FieldEncounterConfig SlotRollLimits(int first, int second, int third)
        {
            FieldEncounterConfig config   = Config();
            SerializedObject     authored = new SerializedObject(config);
            SerializedProperty   limits   = authored.FindProperty("m_SlotRollLimits");

            limits.GetArrayElementAtIndex(0).intValue = first;
            limits.GetArrayElementAtIndex(1).intValue = second;
            limits.GetArrayElementAtIndex(2).intValue = third;
            authored.ApplyModifiedPropertiesWithoutUndo();

            return config;
        }
    }
}
