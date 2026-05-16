namespace RPGFramework.Field.BlockState
{
    internal interface IBlockState
    {
        bool IsComplete { get; }
        void Update(float deltaTime);
    }
}