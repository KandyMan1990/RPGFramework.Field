using RPGFramework.Core.Input;
using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class BlockAllInputContext : IInputContext
    {
        bool IInputContext.Handle(ControlSlot slot) => true;
        void IInputContext.HandleMove(Vector2 move)
        {
            // noop
        }
    }
}