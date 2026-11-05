#if ENABLE_RPG_FRAMEWORK_DEBUG
using RPGFramework.Core.Input;
using RPGFramework.Hashing;

namespace RPGFramework.Field
{
    /// <summary>
    /// The debug room's way back from a field: both shoulders, both triggers and Select held together — the actions of
    /// those names in the field's map, so which buttons they are is the game's.
    /// </summary>
    internal static class DebugRoomShortcut
    {
        internal const string SHOULDER_LEFT  = "ShoulderLeft";
        internal const string TRIGGER_LEFT   = "TriggerLeft";
        internal const string SHOULDER_RIGHT = "ShoulderRight";
        internal const string TRIGGER_RIGHT  = "TriggerRight";
        internal const string SELECT         = "Select";

#if UNITY_EDITOR
        internal const string DESCRIPTION = "held with the other four, takes the debug room's way back from a field";
#endif

        private static readonly ulong[] m_Combination =
        {
            Fnv1a64.Hash(SHOULDER_LEFT),
            Fnv1a64.Hash(TRIGGER_LEFT),
            Fnv1a64.Hash(SHOULDER_RIGHT),
            Fnv1a64.Hash(TRIGGER_RIGHT),
            Fnv1a64.Hash(SELECT)
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
