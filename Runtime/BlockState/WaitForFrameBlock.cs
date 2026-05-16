namespace RPGFramework.Field.BlockState
{
    internal sealed class WaitForFrameBlock : IBlockState
    {
        private bool m_IsComplete;

        bool IBlockState.IsComplete => m_IsComplete;

        void IBlockState.Update(float deltaTime)
        {
            m_IsComplete = true;
        }
    }
}