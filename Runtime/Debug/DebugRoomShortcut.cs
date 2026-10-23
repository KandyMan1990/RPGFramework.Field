#if ENABLE_RPG_FRAMEWORK_DEBUG
using RPGFramework.Core.Input;

namespace RPGFramework.Field
{
    /// <summary>
    /// The debug room's way back from a field: both shoulders, both triggers and Select held together. Which buttons
    /// those are is the game's, through the actions its field's input adapter is given.
    /// </summary>
    internal static class DebugRoomShortcut
    {
        private static readonly ControlSlot[] m_Combination =
        {
            ControlSlot.ShoulderLeft,
            ControlSlot.TriggerLeft,
            ControlSlot.ShoulderRight,
            ControlSlot.TriggerRight,
            ControlSlot.Select
        };

        internal static bool IsHeld(IDebugHeldControls controls)
        {
            for (int i = 0; i < m_Combination.Length; i++)
            {
                if (!controls.IsHeld(m_Combination[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
#endif
