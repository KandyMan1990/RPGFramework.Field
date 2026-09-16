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

        internal const int NO_PRIORITY = -1;

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

        internal bool HoldsScript(int scriptId, byte priority)
        {
            bool holdsScript = m_ScriptIdBySlot[priority] == scriptId;

            return holdsScript;
        }

        /// <summary>
        /// The slot this entity runs: its most urgent occupied one, or <see cref="NO_PRIORITY" />.
        /// </summary>
        internal int RunningPriority
        {
            get
            {
                int runningPriority = NO_PRIORITY;

                for (int priority = PRIORITY_COUNT - 1; priority >= 0; priority--)
                {
                    if (m_ScriptIdBySlot[priority] != NO_SCRIPT)
                    {
                        runningPriority = priority;
                        break;
                    }
                }

                return runningPriority;
            }
        }

        /// <summary>
        /// Place a script in a priority slot, unless something is already there.<br /><br />
        /// A request that finds its slot busy is refused rather than replacing what is running.
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
        /// <b>Only the most urgent occupied slot runs.</b> An entity is one actor and runs one script at a
        /// time; the slots below hold their scripts and their instruction pointers untouched. The frame's
        /// instruction budget is the entity's: when the running script returns or is preempted, the next slot
        /// runs straight away on what is left of it. Occupying a slot is what marks a script pending, and
        /// returning is what releases it.<br /><br />
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

            int instructionBudget = FieldVM.INSTRUCTIONS_PER_ENTITY_PER_FRAME;

            while (true)
            {
                int priority = RunningPriority;

                if (priority == NO_PRIORITY)
                {
                    return;
                }

                ScriptRunOutcome outcome = vm.Execute(EntityId, (byte)priority, m_ScriptIdBySlot[priority], this, ref instructionBudget);

                if (outcome != ScriptRunOutcome.Ended && outcome != ScriptRunOutcome.Preempted)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Run the init script until it returns, before the field's first frame. It is not held to a
        /// frame's budget, only to <see cref="FieldVM.INIT_INSTRUCTION_CEILING" />. Every entity has an
        /// init script: exporting a field refuses one whose first script is not Init.
        /// </summary>
        internal ScriptRunOutcome RunInitScript(FieldVM vm)
        {
            int              scriptId          = m_ScriptIdBySlot[MAIN_PRIORITY];
            int              instructionBudget = FieldVM.INIT_INSTRUCTION_CEILING;
            ScriptRunOutcome outcome           = vm.Execute(EntityId, MAIN_PRIORITY, scriptId, this, ref instructionBudget);

            return outcome;
        }
    }
}