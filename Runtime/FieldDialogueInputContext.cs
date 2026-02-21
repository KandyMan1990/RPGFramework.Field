using System;
using RPGFramework.Core.Input;
using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class FieldDialogueInputContext : IInputContext
    {
        private readonly Action m_OnAdvance;
        private readonly Action m_MoveUp;
        private readonly Action m_MoveDown;

        public FieldDialogueInputContext(Action onAdvance,
                                         Action moveUp,
                                         Action moveDown)
        {
            m_OnAdvance = onAdvance;
            m_MoveUp    = moveUp;
            m_MoveDown  = moveDown;
        }

        bool IInputContext.Handle(ControlSlot slot)
        {
            switch (slot)
            {
                case ControlSlot.Primary:
                    m_OnAdvance?.Invoke();
                    break;
            }

            return true;
        }

        void IInputContext.HandleMove(Vector2 move)
        {
            if (move.y > 0.5f)
            {
                m_MoveUp();
            }
            else if (move.y < -0.5f)
            {
                m_MoveDown();
            }
        }
    }
}