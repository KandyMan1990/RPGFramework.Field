using RPGFramework.Hashing;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Field.Tests.Encounters
{
    /// <summary>A field's root with a table authored as the inspector authors it, every slot in the one arena.</summary>
    internal static class EncounterTable
    {
        internal const string ARENA_NAME = "Forest";

        internal static readonly ulong ARENA = Fnv1a64.Hash(ARENA_NAME);

        internal static GameObject Make(float averageSeconds, params ushort[] groups)
        {
            GameObject       field    = new GameObject("Field");
            SerializedObject authored = new SerializedObject(field.AddComponent<FieldEncounters>());

            authored.FindProperty("m_AverageSeconds").floatValue = averageSeconds;

            SerializedProperty slots = authored.FindProperty("m_Slots");

            for (int i = 0; i < groups.Length; i++)
            {
                SerializedProperty slot = slots.GetArrayElementAtIndex(i);

                slot.FindPropertyRelative("m_EnemyGroup").uintValue = groups[i];
                slot.FindPropertyRelative("m_Arena").ulongValue     = ARENA;
            }

            authored.ApplyModifiedPropertiesWithoutUndo();

            return field;
        }
    }
}
