using System;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// How the default random battles count, in three parts. How often checks come: walking adds the field's rate to a
    /// running total every tick, and each time the total reaches its mark a check is made. How likely a check is to start
    /// a battle: each check adds to the danger, and a roll of 0 to 255 under the danger starts one and puts the danger
    /// back to 0. Which slot it comes from: a second roll, divided among a table's four slots. Field's own is used unless
    /// a game makes another and gives it to the field module.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Framework/Field/Encounter Config", fileName = "Encounter Config")]
    internal sealed class FieldEncounterConfig : ScriptableObject
    {
        internal const int ROLLS = 256;

        [Header("How often checks come")]
        [SerializeField]
        [Min(1)]
        [Tooltip("Walking is counted in ticks, this many a second. Each tick adds the field's rate to a running total")]
        private int m_TicksPerSecond = 30;

        [SerializeField]
        [Min(1)]
        [Tooltip("Each time the running total reaches this, a check is made and the total starts again. With the defaults, a field at rate 8 checks about once a second")]
        private int m_TotalPerCheck = 256;

        [Header("How likely a check is to start a battle")]
        [SerializeField]
        [Range(1, ROLLS)]
        [Tooltip("What each check adds to the danger while walking. A check starts a battle when a roll of 0 to 255 lands under the danger, which then goes back to 0, so a bigger number brings battles sooner")]
        private int m_DangerPerCheckWalking = 2;

        // Read by the editor alone until running is built.
#pragma warning disable CS0414
        [SerializeField]
        [Range(1, ROLLS)]
        [Tooltip("What each check adds to the danger while running")]
        private int m_DangerPerCheckRunning = 5;
#pragma warning restore CS0414

        [Header("Which slot")]
        [SerializeField]
        [Tooltip("Which slot a battle comes from: a roll of 0 to 255 under the first picks slot 1, under the second slot 2, under the third slot 3, and anything higher slot 4. The defaults give 50%, 25%, 19% and 6%. If a slot holds the encounter just fought, the next slot is used instead, except for slot 4")]
        private int[] m_SlotRollLimits = { 128, 192, 240 };

        internal float ChecksPerSecondAtRateOne => (float)m_TicksPerSecond / m_TotalPerCheck;
        internal int   DangerPerCheckWalking    => m_DangerPerCheckWalking;
        internal int[] SlotRollLimits           => m_SlotRollLimits;

#if UNITY_EDITOR
        private const string DEFAULT_PATH = "Packages/com.rpgframework.field/Runtime/Encounter Config.asset";

        internal int DangerPerCheckRunning => m_DangerPerCheckRunning;

        /// <summary>Field's own, which a field module is given as it is made.</summary>
        internal static FieldEncounterConfig LoadDefault()
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<FieldEncounterConfig>(DEFAULT_PATH);
        }

        private void OnValidate()
        {
            if (m_SlotRollLimits.Length != FieldEncounters.SLOT_COUNT - 1)
            {
                Array.Resize(ref m_SlotRollLimits, FieldEncounters.SLOT_COUNT - 1);
            }

            int floor = 0;

            for (int i = 0; i < m_SlotRollLimits.Length; i++)
            {
                m_SlotRollLimits[i] = Math.Clamp(m_SlotRollLimits[i], floor, ROLLS);
                floor               = m_SlotRollLimits[i];
            }
        }
#endif
    }
}
