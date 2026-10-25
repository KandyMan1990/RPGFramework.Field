using System;
using RPGFramework.Core.Input;

namespace RPGFramework.Field
{
    /// <summary>
    /// A script waiting for the player to press a button: takes that button once, and lets every other press and the
    /// move input through to whatever is under it.
    /// </summary>
    internal sealed class ButtonWaitInputContext : IInputContext
    {
        private readonly ControlSlot                    m_Button;
        private readonly Action<ButtonWaitInputContext> m_OnPressed;

        private bool m_Pressed;

        internal ButtonWaitInputContext(ControlSlot button, Action<ButtonWaitInputContext> onPressed)
        {
            m_Button    = button;
            m_OnPressed = onPressed;
            m_Pressed   = false;
        }

        internal bool Pressed => m_Pressed;

        bool IInputContext.Handle(ControlSlot slot)
        {
            if (m_Pressed || slot != m_Button)
            {
                return false;
            }

            m_Pressed = true;
            m_OnPressed(this);

            return true;
        }

        bool IInputContext.TakesMovement => false;
    }
}