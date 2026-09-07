namespace RPGFramework.Field
{
    /// <summary>
    /// An entity's script execution state.<br /><br />
    /// An entity does not run one script at a time; it holds a slot per priority, and a request names
    /// both the script it wants and the slot to put it in.
    /// </summary>
    internal sealed class FieldEntityRuntime
    {
        /// <summary>
        /// Priorities are 0-7. <b>0 is serviced first</b>: occupied slots run in numerical order within
        /// a frame.
        /// </summary>
        internal const int PRIORITY_COUNT = 8;

        /// <summary>
        /// The slot used by an entity's init script and by gateway and interaction triggers — anything
        /// the engine starts rather than another script.
        /// </summary>
        internal const byte DEFAULT_PRIORITY = 0;

        /// <summary>
        /// The slot the entity's <see cref="FieldScriptType.Main" /> script runs on. Separate from
        /// <see cref="DEFAULT_PRIORITY" /> so that a trigger firing does not have to wait for an ongoing
        /// behaviour to finish, and so that a Main script looping forever does not block triggers.
        /// </summary>
        internal const byte MAIN_PRIORITY = 1;

        private const int NO_SCRIPT = -1;

        internal int  EntityId { get; }
        private  bool IsActive { get; }

        private readonly int[] m_ScriptIdBySlot;
        private readonly int[] m_ScriptIdByEvent;

        internal FieldEntityRuntime(int entityId, int[] scriptIdsByEvent)
        {
            EntityId = entityId;
            IsActive = true;

            m_ScriptIdByEvent = scriptIdsByEvent;
            m_ScriptIdBySlot  = new int[PRIORITY_COUNT];

            for (int i = 0; i < PRIORITY_COUNT; i++)
            {
                m_ScriptIdBySlot[i] = NO_SCRIPT;
            }

            // Event 0 is the init script, and it starts as soon as the field loads.
            m_ScriptIdBySlot[DEFAULT_PRIORITY] = m_ScriptIdByEvent[0];
        }

        /// <summary>
        /// Resolve one of this entity's event ids to the field-wide script id the VM knows.
        /// </summary>
        internal bool TryGetScriptId(int eventId, out int scriptId)
        {
            if (eventId < 0 || eventId >= m_ScriptIdByEvent.Length)
            {
                scriptId = NO_SCRIPT;
                return false;
            }

            scriptId = m_ScriptIdByEvent[eventId];

            return true;
        }

        /// <summary>
        /// True while any slot holds a script that has not yet returned. After the field's initialisation
        /// pass this being true means the entity's init script blocked partway.
        /// </summary>
        internal bool IsRunningScript
        {
            get
            {
                for (int i = 0; i < PRIORITY_COUNT; i++)
                {
                    if (m_ScriptIdBySlot[i] != NO_SCRIPT)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal bool IsSlotOccupied(byte priority)
        {
            bool isSlotOccupied = m_ScriptIdBySlot[priority] != NO_SCRIPT;

            return isSlotOccupied;
        }

        /// <summary>
        /// Place a script in a priority slot, unless something is already there.<br /><br />
        /// A request that finds its slot busy is refused rather than replacing what is running. A caller
        /// that must not be refused retries — see <see cref="BlockState.RequestScriptBlock" />.
        /// </summary>
        /// <returns>False when the slot was occupied and the request was refused.</returns>
        internal bool TryRequestScript(int scriptId, byte priority)
        {
            if (IsSlotOccupied(priority))
            {
                return false;
            }

            m_ScriptIdBySlot[priority] = scriptId;

            return true;
        }

        internal void ReplaceScriptInSlot(int scriptId, byte priority)
        {
            m_ScriptIdBySlot[priority] = scriptId;
        }

        internal void ClearSlot(byte priority)
        {
            m_ScriptIdBySlot[priority] = NO_SCRIPT;
        }

        internal void Update(FieldVM vm)
        {
            if (!IsActive)
            {
                return;
            }

            for (byte priority = 0; priority < PRIORITY_COUNT; priority++)
            {
                int scriptId = m_ScriptIdBySlot[priority];

                if (scriptId == NO_SCRIPT)
                {
                    continue;
                }

                vm.Execute(EntityId, priority, scriptId, this);
            }
        }
    }
}