using System;
using RPGFramework.Battle.SharedTypes;
using RPGFramework.Battle.SharedTypes.Stores;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// The field's own encounters: a field's <see cref="FieldEncounters" />, its rate scaled by the game's and counted as
    /// the config says, its four slots, and the battle mode's flags for every random battle.
    /// </summary>
    internal sealed class RandomFieldEncounterDriver : IFieldEncounterDriver
    {
        private readonly IFieldEncounterStore m_Store;
        private readonly IBattleModeStore     m_BattleModeStore;
        private readonly System.Random        m_Random;
        private readonly EncounterMeter       m_Meter;
        private          FieldEncounters      m_Encounters;

        internal RandomFieldEncounterDriver(IFieldEncounterStore store, IBattleModeStore battleModeStore, FieldEncounterConfig config)
        {
            m_Store           = store;
            m_BattleModeStore = battleModeStore;
            m_Random          = new System.Random();
            m_Meter           = new EncounterMeter(store, config, NextRoll);
            m_Encounters      = null;
        }

        void IFieldEncounterDriver.Enter(GameObject field)
        {
            m_Encounters = field.GetComponent<FieldEncounters>();
        }

        bool IFieldEncounterDriver.TryStartBattle(in PlayerWalkArgs walk, out BattleArgs args)
        {
            args = default;

            if (m_Encounters == null)
            {
                return false;
            }

            float rate = m_Encounters.Rate * m_Store.Rate;

            if (rate <= 0f || !m_Meter.Walk(walk.Seconds, rate))
            {
                return false;
            }

            FieldEncounterSlot slot  = m_Meter.Pick(m_Encounters.Slots);
            BattleFlags        flags = (BattleFlags)((long)m_BattleModeStore.Mode & BattleFlagMasks.ONE_BATTLE);

            args = new BattleArgs(slot.Encounter, slot.Arena, flags, 0);

            return true;
        }

        private byte NextRoll()
        {
            return (byte)m_Random.Next(0, byte.MaxValue + 1);
        }
    }
}
