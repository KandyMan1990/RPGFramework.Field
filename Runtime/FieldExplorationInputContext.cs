using System;
using RPGFramework.Core.Input;

namespace RPGFramework.Field
{
    public sealed class FieldExplorationInputContext : IInputContext
    {
        private readonly Func<FieldInteractionTrigger> m_GetBestInteractionTrigger;
        private readonly Action                        m_OpenConfigMenu;

        public FieldExplorationInputContext(Func<FieldInteractionTrigger> getBestInteractionTrigger, Action openConfigMenu)
        {
            m_GetBestInteractionTrigger = getBestInteractionTrigger;
            m_OpenConfigMenu            = openConfigMenu;
        }

        bool IInputContext.Handle(ControlSlot slot)
        {
            switch (slot)
            {
                case ControlSlot.Primary:
                    FieldInteractionTrigger triggerEntity = m_GetBestInteractionTrigger();

                    if (triggerEntity != null)
                    {
                        triggerEntity.TryInteract();
                    }

                    break;
                case ControlSlot.Tertiary:
                    m_OpenConfigMenu();

                    break;
            }

            return true;
        }
    }
}