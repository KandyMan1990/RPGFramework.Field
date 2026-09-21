using RPGFramework.Core.Input;
using UnityEngine;

namespace RPGFramework.Field
{
    internal sealed class BlockAllInputContext : IInputContext
    {
        bool IInputContext.Handle(ControlSlot slot) => true;
        bool IInputContext.HandleMove(Vector2 move) => true;
    }
}