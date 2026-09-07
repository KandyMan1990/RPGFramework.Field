using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Field
{
    [CreateAssetMenu(menuName = "RPG Framework/Field/Script Definition", fileName = "FieldScriptDefinition")]
    public sealed class FieldScriptDefinition : ScriptableObject
    {
        public string            EntityName;
        public int               EntityId;
        public List<ScriptEntry> Scripts;

        /// <summary>
        /// The position of a script in this entity's list, which is its <b>event id</b> — the thing a
        /// script-request opcode names.<br /><br />
        /// Scripts are addressed relative to their entity. Addressing them by a field-wide id instead
        /// would mean an author had to know an entity's offset into the whole field just to call one of
        /// its scripts, and that offset changes whenever anything ahead of it is reordered.
        /// </summary>
        public bool TryGetScriptIndex(FieldScriptType scriptType, out int eventId)
        {
            for (int i = 0; i < Scripts.Count; i++)
            {
                if (Scripts[i].ScriptType == scriptType)
                {
                    eventId = i;
                    return true;
                }
            }

            eventId = -1;

            return false;
        }
    }
}