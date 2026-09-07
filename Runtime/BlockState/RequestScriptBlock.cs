namespace RPGFramework.Field.BlockState
{
    /// <summary>
    /// Places a script in another entity's priority slot, retrying until the slot accepts it.<br /><br />
    /// Where the plain request gives up if the slot is busy, these variants are guaranteed: the
    /// instruction is re-run each frame until the request is accepted. Retrying is therefore the
    /// intended behaviour rather than a workaround, which is why the request is attempted from the
    /// completion check rather than polling for a free slot and then racing for it.
    /// </summary>
    internal sealed class RequestScriptBlock : IBlockState
    {
        private readonly FieldEntityRuntime m_Target;
        private readonly int                m_ScriptId;
        private readonly byte               m_Priority;
        private readonly bool               m_WaitForCompletion;

        private bool m_Requested;

        /// <param name="waitForCompletion">
        /// False waits only until the script has been accepted into its slot; true waits until it has
        /// run to its return.
        /// </param>
        internal RequestScriptBlock(FieldEntityRuntime target, int scriptId, byte priority, bool waitForCompletion)
        {
            m_Target            = target;
            m_ScriptId          = scriptId;
            m_Priority          = priority;
            m_WaitForCompletion = waitForCompletion;
        }

        bool IBlockState.IsComplete
        {
            get
            {
                if (!m_Requested)
                {
                    m_Requested = m_Target.TryRequestScript(m_ScriptId, m_Priority);

                    // Accepted this frame. The script has not run yet, so a caller waiting only for it to
                    // start is done, while one waiting for completion carries on below next frame.
                    return m_Requested && !m_WaitForCompletion;
                }

                bool isComplete = !m_Target.IsSlotOccupied(m_Priority);

                return isComplete;
            }
        }

        void IBlockState.Update(float deltaTime)
        {
            // noop - progress is entirely in whether the target slot has accepted and then freed.
        }
    }
}