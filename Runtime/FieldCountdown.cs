using RPGFramework.Core.Store;
using RPGFramework.Core.UI;
using UnityEngine.UIElements;

namespace RPGFramework.Field
{
    /// <summary>
    /// Core's countdown on the field's screen while it is shown, where a script put it, which a battle carrying it shows
    /// too. Where it sits is saved, so it stays put through a field jump, a battle and a load.
    /// </summary>
    internal sealed class FieldCountdown
    {
        private readonly ICountdownStore m_Countdown;
        private readonly CountdownLabel  m_Label;

        internal FieldCountdown(ICountdownStore countdown)
        {
            m_Countdown = countdown;
            m_Label     = new CountdownLabel(countdown);
        }

        /// <summary>Shown and at zero, which ends the game unless the battle mode lets it run out.</summary>
        internal bool HasRunOut => m_Countdown.IsShown && m_Countdown.Seconds == 0;

        internal void Enter(VisualElement root)
        {
            root.Add(m_Label);
        }

        internal void Exit()
        {
            m_Label.RemoveFromHierarchy();
        }

        internal void Show(int x, int y)
        {
            m_Countdown.SetPosition(x, y);
            m_Countdown.SetShown(true);
        }

        internal void Tick(float deltaTime)
        {
            m_Label.Tick(deltaTime);
        }
    }
}
