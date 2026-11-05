using System;
using RPGFramework.Core.Input;

namespace RPGFramework.Field
{
    internal sealed class FieldExplorationInputContext : IInputContext
    {
        private readonly Func<FieldInteractionTrigger> m_GetBestInteractionTrigger;
        private readonly Action                        m_OpenConfigMenu;

        public FieldExplorationInputContext(Func<FieldInteractionTrigger> getBestInteractionTrigger, Action openConfigMenu)
        {
            m_GetBestInteractionTrigger = getBestInteractionTrigger;
            m_OpenConfigMenu            = openConfigMenu;
        }

        bool IInputContext.TakesMovement => true;

        bool IInputContext.Handle(ulong action)
        {
            if (action == CoreActions.Confirm)
            {
                FieldInteractionTrigger triggerEntity = m_GetBestInteractionTrigger();

                if (triggerEntity != null)
                {
                    triggerEntity.TryInteract();
                }
            }
            else if (action == FieldActions.Menu)
            {
                m_OpenConfigMenu();
            }

            return true;
        }
    }
}