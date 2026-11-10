using System;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>One random battle a field can start: what is fought and where.</summary>
    [Serializable]
    internal struct FieldEncounterSlot
    {
        [SerializeField]
        private ushort m_EnemyGroup;

        [SerializeField]
        [Tooltip("The arena, by the hash of its name")]
        private ulong m_Arena;

        internal ushort EnemyGroup => m_EnemyGroup;
        internal ulong  Arena      => m_Arena;
    }
}
