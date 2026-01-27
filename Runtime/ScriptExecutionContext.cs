using System.Threading.Tasks;

namespace RPGFramework.Field
{
    internal sealed class ScriptExecutionContext
    {
        private Task m_BlockingTask;

        internal int EntityId;
        internal int ScriptId;
        internal int InstructionPointer;

        internal void Block(Task blockingTask) => m_BlockingTask = blockingTask;
        internal bool IsBlocked()
        {
            if (m_BlockingTask == null)
            {
                return false;
            }

            if (m_BlockingTask.IsCompleted || m_BlockingTask.IsFaulted || m_BlockingTask.IsCanceled)
            {
                m_BlockingTask = null;
                return false;
            }

            return true;
        }
    }
}