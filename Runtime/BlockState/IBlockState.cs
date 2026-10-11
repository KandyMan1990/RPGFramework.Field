namespace RPGFramework.Field.BlockState
{
    /// <summary>What a script waits on — some seconds, or a condition — before its next instruction runs.</summary>
    internal interface IBlockState
    {
        /// <summary>Whether the wait is over, so the script runs on.</summary>
        bool IsComplete { get; }

        /// <summary>Advances the wait by a frame.</summary>
        void Update(float deltaTime);
    }
}