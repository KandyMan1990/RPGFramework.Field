using RPGFramework.Field.BlockState;

namespace RPGFramework.Field
{
    internal sealed class ScriptExecutionContext
    {
        internal int         EntityId;
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