using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// One entity as it is authored: its id, its name, its scripts as text, and the GameObject that is its body
    /// when it has one. An entity with no body — one that only plays music or sets flags — is this record alone.
    /// </summary>
    [Serializable]
    internal sealed class FieldEntityRecord
    {
        [SerializeField]
        private int m_EntityId;

        [SerializeField]
        private string m_Name;

        [SerializeField]
        [Tooltip("The GameObject that is this entity in the scene. Empty for an entity with no presence")]
        private FieldEntity m_Body;

        [SerializeField]
        private List<FieldScriptRecord> m_Scripts = new List<FieldScriptRecord>();

        internal FieldEntityRecord(int entityId, string name, FieldEntity body)
        {
            m_EntityId = entityId;
            m_Name     = name;
            m_Body     = body;
        }

        internal int                     EntityId => m_EntityId;
        internal string                  Name     => m_Name;
        internal FieldEntity             Body     => m_Body;
        internal List<FieldScriptRecord> Scripts  => m_Scripts;

        internal void SetName(string name) => m_Name = name;

        internal void SetBody(FieldEntity body) => m_Body = body;
    }

    /// <summary>
    /// One script as it is authored. Its position in the entity's list is its event id; its name is for the author
    /// and is not exported.
    /// </summary>
    [Serializable]
    internal sealed class FieldScriptRecord
    {
        [SerializeField]
        private FieldScriptType m_Type;

        [SerializeField]
        private string m_Name;

        [SerializeField]
        [TextArea(3, 20)]
        private string m_Text;

        [SerializeField]
        private FieldScriptPriority m_Slot;

        internal FieldScriptRecord(FieldScriptType type, string name, string text)
        {
            m_Type = type;
            m_Name = name;
            m_Text = text;
        }

        internal FieldScriptType     Type => m_Type;
        internal string              Name => m_Name;
        internal string              Text => m_Text;
        internal FieldScriptPriority Slot => m_Slot;

        internal void SetType(FieldScriptType type) => m_Type = type;

        internal void SetName(string name) => m_Name = name;

        internal void SetText(string text) => m_Text = text;

        internal void SetSlot(FieldScriptPriority slot) => m_Slot = slot;
    }

    /// <summary>
    /// An entity as the game loads it, written by export into the copy of the prefab that is bundled: no text and no
    /// names, only what runs.
    /// </summary>
    [Serializable]
    internal sealed class CompiledFieldEntity
    {
        [SerializeField]
        private int m_EntityId;

        [SerializeField]
        private FieldEntity m_Body;

        [SerializeField]
        private List<CompiledFieldScript> m_Scripts;

        internal CompiledFieldEntity(int entityId, FieldEntity body, List<CompiledFieldScript> scripts)
        {
            m_EntityId = entityId;
            m_Body     = body;
            m_Scripts  = scripts;
        }

        internal int                               EntityId => m_EntityId;
        internal FieldEntity                       Body     => m_Body;
        internal IReadOnlyList<CompiledFieldScript> Scripts  => m_Scripts;

        /// <summary>
        /// The event id of this entity's first script of a type — what a trigger runs.
        /// </summary>
        internal bool TryGetScriptIndex(FieldScriptType scriptType, out int eventId)
        {
            for (int i = 0; i < m_Scripts.Count; i++)
            {
                if (m_Scripts[i].Type == scriptType)
                {
                    eventId = i;
                    return true;
                }
            }

            eventId = -1;

            return false;
        }
    }

    [Serializable]
    internal sealed class CompiledFieldScript
    {
        [SerializeField]
        private FieldScriptType m_Type;

        [SerializeField]
        [Tooltip("The field-wide id the VM holds this script under, assigned at export")]
        private int m_ScriptId;

        [SerializeField]
        private byte[] m_Bytecode;

        [SerializeField]
        private FieldScriptPriority m_ChosenSlot;

        internal CompiledFieldScript(FieldScriptType type, int scriptId, byte[] bytecode, FieldScriptPriority chosenSlot)
        {
            m_Type       = type;
            m_ScriptId   = scriptId;
            m_Bytecode   = bytecode;
            m_ChosenSlot = chosenSlot;
        }

        internal FieldScriptType Type     => m_Type;
        internal int             ScriptId => m_ScriptId;
        internal byte[]          Bytecode => m_Bytecode;
        internal byte            Slot     => FieldScriptSlots.For(m_Type, m_ChosenSlot);
    }
}
