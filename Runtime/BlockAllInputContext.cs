using RPGFramework.Core.Input;

namespace RPGFramework.Field
{
    internal sealed class BlockAllInputContext : IInputContext
    {
        bool IInputContext.Handle(ulong action) => true;
        bool IInputContext.TakesMovement => true;
    }
}