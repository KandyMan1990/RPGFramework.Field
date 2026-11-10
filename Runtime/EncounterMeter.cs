using System;
using Unity.Mathematics;

namespace RPGFramework.Field
{
    /// <summary>
    /// Brings a random battle nearer as the player walks: every so many seconds a check, which raises the danger, and a
    /// roll below the danger starts a battle and clears it. Given its rolls, so a test chooses them.
    /// </summary>
    internal sealed class EncounterMeter
    {
        /// <summary>
        /// The checks a battle takes on average, the danger rising by two a check against an even roll of 0 to 255; how
        /// a field's average seconds become the seconds between checks.
        /// </summary>
        internal const float CHECKS_PER_BATTLE = 13.86f;

        private const int DANGER_PER_CHECK = 2;

        // A roll below each limit picks its slot unless the slot holds the last group fought; the fourth takes the rest.
        private static readonly int[] m_SlotLimits = { 0x80, 0xC0, 0xF0 };

        private readonly IFieldEncounterStore m_Store;
        private readonly Func<byte>           m_NextRoll;

        internal EncounterMeter(IFieldEncounterStore store, Func<byte> nextRoll)
        {
            m_Store    = store;
            m_NextRoll = nextRoll;
        }

        /// <summary>Walks toward the next check and takes each one reached; true when one starts a battle.</summary>
        internal bool Walk(float seconds, float secondsPerCheck)
        {
            float walked = m_Store.StepFraction + seconds / secondsPerCheck;
            int   checks = (int)walked;

            m_Store.SetStepFraction(walked - checks);

            for (int i = 0; i < checks; i++)
            {
                if (Check())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The slot the battle fights, whose group is remembered so the next does not repeat it.</summary>
        internal FieldEncounterSlot Pick(FieldEncounterSlot[] slots)
        {
            byte   roll   = m_NextRoll();
            ushort last   = m_Store.LastEnemyGroup;
            int    picked = slots.Length - 1;

            for (int i = 0; i < m_SlotLimits.Length; i++)
            {
                if (roll < m_SlotLimits[i] && slots[i].EnemyGroup != last)
                {
                    picked = i;
                    break;
                }
            }

            m_Store.SetLastEnemyGroup(slots[picked].EnemyGroup);

            return slots[picked];
        }

        private bool Check()
        {
            int danger = math.min(m_Store.Danger + DANGER_PER_CHECK, ushort.MaxValue);

            if (m_NextRoll() < danger)
            {
                m_Store.SetDanger(0);

                return true;
            }

            m_Store.SetDanger((ushort)danger);

            return false;
        }
    }
}
