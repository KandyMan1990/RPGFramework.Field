using System;
using RPGFramework.Battle.SharedTypes;
using RPGFramework.Battle.SharedTypes.Stores;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// The field's own encounters: a field's <see cref="FieldEncounters" />, its average seconds scaled by the rate, its
    /// four slots, and the battle mode's flags for every random battle.
    /// </summary>
    internal sealed class RandomFieldEncounterDriver : IFieldEncounterDriver
    {
        private readonly IFieldEncounterStore m_Store;
        private readonly IBattleModeStore     m_BattleModeStore;
        private readonly System.Random        m_Random;
        private readonly EncounterMeter       m_Meter;
        private          FieldEncounters      m_Encounters;

        internal RandomFieldEncounterDriver(IFieldEncounterStore store, IBattleModeStore battleModeStore)
        {
            m_Store           = store;
            m_BattleModeStore = battleModeStore;
            m_Random          = new System.Random();
            m_Meter           = new EncounterMeter(store, NextRoll);
            m_Encounters      = null;
        }

        void IFieldEncounterDriver.Enter(GameObject field)
        {
            m_Encounters = field.GetComponent<FieldEncounters>();
        }

        bool IFieldEncounterDriver.TryStartBattle(in PlayerWalkArgs walk, out BattleArgs args)
        {
            args = default;

            float rate = m_Store.Rate;

            if (m_Encounters == null || m_Encounters.AverageSeconds <= 0f || rate <= 0f)
            {
                return false;
            }

            if (!m_Meter.Walk(walk.Seconds, m_Encounters.AverageSeconds / EncounterMeter.CHECKS_PER_BATTLE / rate))
            {
                return false;
            }

            FieldEncounterSlot slot  = m_Meter.Pick(m_Encounters.Slots);
            BattleFlags        flags = (BattleFlags)((long)m_BattleModeStore.Mode & BattleFlagMasks.ONE_BATTLE);

            args = new BattleArgs(slot.EnemyGroup, slot.Arena, flags, 0);

            return true;
        }

        private byte NextRoll()
        {
            return (byte)m_Random.Next(0, byte.MaxValue + 1);
        }
    }
}
