using System;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// A field's random battles, on its root: the seconds the player walks between them on average, and four slots the
    /// next is picked from, half, a quarter, three sixteenths and a sixteenth of the time, so a group in two slots comes
    /// up more often. A field without it has none.
    /// </summary>
    internal sealed class FieldEncounters : MonoBehaviour
    {
        internal const int SLOT_COUNT = 4;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Seconds of walking between random battles, on average; 0 for none")]
        private float m_AverageSeconds;

        [SerializeField]
        private FieldEncounterSlot[] m_Slots = new FieldEncounterSlot[SLOT_COUNT];

        internal float                AverageSeconds => m_AverageSeconds;
        internal FieldEncounterSlot[] Slots          => m_Slots;

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
