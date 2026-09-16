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
        /// Priorities are 0-7, <b>7 being the most urgent</b>. An entity runs its highest-numbered occupied
        /// slot and only that one; a more urgent script arriving preempts what is running, and the script
        /// underneath resumes when it returns.
        /// </summary>
        internal const int PRIORITY_COUNT = 8;

        /// <summary>
        /// The least urgent slot. The init script runs here, then the <see cref="FieldScriptType.Main" />
        /// script replaces it, so every other slot preempts Main.
        /// </summary>
        internal const byte MAIN_PRIORITY = 0;

        internal const byte COLLISION_PRIORITY = 6;

        internal const byte INTERACTION_PRIORITY = 7;

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
            m_ScriptIdBySlot[MAIN_PRIORITY] = m_ScriptIdByEvent[0];
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

        /// <summary>
        /// Advance this entity by one frame.<br /><br />
        /// <b>Only the highest-priority occupied slot runs.</b> An entity is one actor and runs one
        /// script at a time; the slots below hold their scripts and their instruction pointers
        /// untouched, and the next one down resumes where it left off once the script above it returns.
        /// Occupying a slot is what marks a script pending, and returning is what releases it.<br /><br />
        /// A script that is waiting still holds the entity — nothing below it runs while it waits. Only
        /// a <b>more urgent</b> priority arriving takes over, which is the point of the ordering: a trigger
        /// preempts a Main script at <see cref="MAIN_PRIORITY" /> however long that Main script has been
        /// looping, and Main picks up again afterwards.
        /// </summary>
        internal void Update(FieldVM vm)
        {
            if (!IsActive)
            {
                return;
            }

            for (int priority = PRIORITY_COUNT - 1; priority >= 0; priority--)
            {
                int scriptId = m_ScriptIdBySlot[priority];

                if (scriptId == NO_SCRIPT)
                {
                    continue;
                }

                vm.Execute(EntityId, (byte)priority, scriptId, this, FieldVM.INSTRUCTIONS_PER_SLOT_PER_FRAME);

                return;
            }
        }

        /// <summary>
        /// Run the init script until it returns, before the field's first frame. It is not held to a
        /// frame's budget, only to <see cref="FieldVM.INIT_INSTRUCTION_CEILING" />. Every entity has an
        /// init script: exporting a field refuses one whose first script is not Init.
        /// </summary>
        internal ScriptRunOutcome RunInitScript(FieldVM vm)
        {
            int              scriptId = m_ScriptIdBySlot[MAIN_PRIORITY];
            ScriptRunOutcome outcome  = vm.Execute(EntityId, MAIN_PRIORITY, scriptId, this, FieldVM.INIT_INSTRUCTION_CEILING);

            return outcome;
        }
    }
}