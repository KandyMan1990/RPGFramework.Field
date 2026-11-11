using System;
using Unity.Mathematics;

namespace RPGFramework.Field
{
    /// <summary>
    /// Brings a random battle nearer as the player walks: the rate brings a check every so often, which raises the
    /// danger, and a roll below the danger starts a battle and clears it, all as its config counts. Given its rolls, so a
    /// test chooses them.
    /// </summary>
    internal sealed class EncounterMeter
    {
        private readonly IFieldEncounterStore m_Store;
        private readonly FieldEncounterConfig m_Config;
        private readonly Func<byte>           m_NextRoll;

        internal EncounterMeter(IFieldEncounterStore store, FieldEncounterConfig config, Func<byte> nextRoll)
        {
            m_Store    = store;
            m_Config   = config;
            m_NextRoll = nextRoll;
        }

        /// <summary>Walks toward the next check at a field's rate and takes each one reached; true when one starts a battle.</summary>
        internal bool Walk(float seconds, float rate)
        {
            float walked = m_Store.StepFraction + seconds * rate * m_Config.ChecksPerSecondAtRateOne;
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

        /// <summary>The slot the battle fights, whose encounter is remembered so the next does not repeat it.</summary>
        internal FieldEncounterSlot Pick(FieldEncounterSlot[] slots)
        {
            int[] limits = m_Config.SlotRollLimits;
            byte  roll   = m_NextRoll();
            ulong last   = m_Store.LastEncounter;
            int   picked = slots.Length - 1;

            for (int i = 0; i < limits.Length; i++)
            {
                if (roll < limits[i] && slots[i].Encounter != last)
                {
                    picked = i;
                    break;
                }
            }

            m_Store.SetLastEncounter(slots[picked].Encounter);

            return slots[picked];
        }

#if UNITY_EDITOR
        /// <summary>The seconds a battle takes to come on average at a rate above 0, each check raising the danger by a step.</summary>
        internal static float AverageSeconds(FieldEncounterConfig config, byte rate, int dangerPerCheck)
        {
            float checks = 0f;
            float reach  = 1f;

            for (int danger = dangerPerCheck; reach > 0f; danger += dangerPerCheck)
            {
                checks += reach;
                reach  *= 1f - (float)math.min(danger, FieldEncounterConfig.ROLLS) / FieldEncounterConfig.ROLLS;
            }

            return checks / (rate * config.ChecksPerSecondAtRateOne);
        }
#endif

        private bool Check()
        {
            int danger = math.min(m_Store.Danger + m_Config.DangerPerCheckWalking, ushort.MaxValue);

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
