using System;
using RPGFramework.Core.Input;
using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class FieldExplorationInputContext : IInputContext
    {
        private readonly Func<FieldInteractionTrigger> m_GetBestInteractionTrigger;
        private readonly Action                        m_OpenConfigMenu;
        private readonly Action<Vector2>               m_OnMove;

        public FieldExplorationInputContext(Func<FieldInteractionTrigger> getBestInteractionTrigger,
                                            Action                        openConfigMenu,
                                            Action<Vector2>               onMove)
        {
            m_GetBestInteractionTrigger = getBestInteractionTrigger;
            m_OpenConfigMenu            = openConfigMenu;
            m_OnMove                    = onMove;
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

        void IInputContext.HandleMove(Vector2 move)
        {
            m_OnMove(move);
        }
    }
}