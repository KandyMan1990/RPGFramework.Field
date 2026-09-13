using RPGFramework.Field.BlockState;

namespace RPGFramework.Field
{
    /// <summary>
    /// One script's execution state. There is an instance per occupied priority slot, but only the
    /// entity's highest-priority slot advances in a frame — the rest are preempted scripts holding
    /// their instruction pointers until the ones above them return.
    /// </summary>
    internal sealed class ScriptExecutionContext
    {
        internal int         EntityId;
        internal byte        Priority;
        internal int         ScriptId;
        internal int         InstructionPointer;
        internal byte[]      Bytecode;
        private  IBlockState m_BlockingState;

        internal void Block(IBlockState blockingState)
        {
            m_BlockingState = blockingState;
        }

        internal bool IsBlocked()
        {
            return m_BlockingState != null;
        }

        internal void UpdateBlock(float deltaTime)
        {
            if (m_BlockingState == null)
            {
                return;
            }

            m_BlockingState.Update(deltaTime);

            if (m_BlockingState.IsComplete)
            {
                m_BlockingState = null;
            }
        }
    }
}