using RPGFramework.Field.FieldVmArgs;
using RPGFramework.Field.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// How far an entity's visible object sits from where it was placed, and any change of it under way. The entity
    /// itself stays where it is, so what it stands on and what it blocks are unchanged.
    /// </summary>
    internal struct DrawOffset
    {
        private Vector3 m_From;
        private Vector3 m_To;
        private float   m_Elapsed;
        private float   m_Seconds;
        private Easing  m_Easing;

        internal Vector3 Current  { get; private set; }
        internal bool    IsMoving { get; private set; }

        /// <summary>Goes to an offset from the one it has now, over the given time; none goes at once.</summary>
        internal void MoveTo(Vector3 offset, float seconds, Easing easing)
        {
            if (seconds <= 0f)
            {
                Current  = offset;
                IsMoving = false;

                return;
            }

            m_From    = Current;
            m_To      = offset;
            m_Elapsed = 0f;
            m_Seconds = seconds;
            m_Easing  = easing;
            IsMoving  = true;
        }

        internal void Tick(float deltaTime)
        {
            if (!IsMoving)
            {
                return;
            }

            m_Elapsed += deltaTime;

            if (m_Elapsed >= m_Seconds)
            {
                Current  = m_To;
                IsMoving = false;

                return;
            }

            Current = math.lerp(m_From, m_To, EasingCurve.Apply(m_Elapsed / m_Seconds, m_Easing));
        }
    }
}
