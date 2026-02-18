using RPGFramework.Core.Input;

namespace RPGFramework.Field
{
    public sealed class BlockAllInputContext : IInputContext
    {
        bool IInputContext.Handle(ControlSlot slot) => true;
    }
}