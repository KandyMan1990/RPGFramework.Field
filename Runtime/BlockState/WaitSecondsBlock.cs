namespace RPGFramework.Field.BlockState
{
    internal sealed class WaitSecondsBlock : IBlockState
    {
        private float m_RemainingSeconds;

        internal WaitSecondsBlock(float seconds)
        {
            m_RemainingSeconds = seconds;
        }

        bool IBlockState.IsComplete => m_RemainingSeconds <= 0f;

        void IBlockState.Update(float deltaTime)
        {
            m_RemainingSeconds -= deltaTime;
        }
    }
}