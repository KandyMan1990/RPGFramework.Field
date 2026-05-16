using System;

namespace RPGFramework.Field.BlockState
{
    internal sealed class WaitUntilBlock : IBlockState
    {
        private readonly Func<bool> m_WaitUntilFunc;

        internal WaitUntilBlock(Func<bool> waitUntilFunc)
        {
            m_WaitUntilFunc = waitUntilFunc;
        }

        bool IBlockState.IsComplete => m_WaitUntilFunc();

        void IBlockState.Update(float deltaTime)
        {
            // noop
        }
    }
}