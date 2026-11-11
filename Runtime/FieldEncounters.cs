using System;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// A field's random battles, on its root: how often they come, and four slots the next is picked from, each an
    /// encounter and where it is fought, by default half, a quarter, three sixteenths and a sixteenth of the time, so an
    /// encounter in two slots comes up more often. A field without it has none.
    /// </summary>
    internal sealed class FieldEncounters : MonoBehaviour
    {
        internal const int SLOT_COUNT = 4;

        [SerializeField]
        [Range(0, 255)]
        [Tooltip("How often random battles come, 0 for none; the Field Designer's Encounters tab gives it in seconds")]
        private byte m_Rate;

        [SerializeField]
        private FieldEncounterSlot[] m_Slots = new FieldEncounterSlot[SLOT_COUNT];

        internal byte                 Rate  => m_Rate;
        internal FieldEncounterSlot[] Slots => m_Slots;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_Slots.Length != SLOT_COUNT)
            {
                Array.Resize(ref m_Slots, SLOT_COUNT);
            }
        }
#endif
    }
}
