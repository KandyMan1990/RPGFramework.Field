using System;
using RPGFramework.Core.Input;

namespace RPGFramework.Field
{
    public sealed class FieldDialogueInputContext : IInputContext
    {
        private readonly Action m_OnAdvance;

        public FieldDialogueInputContext(Action onAdvance)
        {
            m_OnAdvance = onAdvance;
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
    }
}