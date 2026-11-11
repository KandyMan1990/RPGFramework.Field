using System;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>One random battle a field can start: the encounter fought and where.</summary>
    [Serializable]
    internal struct FieldEncounterSlot
    {
        [SerializeField]
        [Tooltip("The encounter, by the hash of its name")]
        private ulong m_Encounter;

        [SerializeField]
        [Tooltip("The arena, by the hash of its name")]
        private ulong m_Arena;

        internal ulong Encounter => m_Encounter;
        internal ulong Arena     => m_Arena;
    }
}
