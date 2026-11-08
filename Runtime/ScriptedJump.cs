using Unity.Mathematics;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// An entity in the air on the way to a point: a straight line from where it left to where it lands, and a parabola
    /// above that line.
    /// </summary>
    internal struct ScriptedJump
    {
        private readonly Vector3 m_Start;
        private readonly float   m_Seconds;
        private readonly float   m_Height;

        private float m_Elapsed;

        internal ScriptedJump(Vector3 start, Vector3 target, float seconds, float height)
        {
            m_Start   = start;
            Target    = target;
            m_Seconds = seconds;
            m_Height  = height;
            m_Elapsed = 0f;
        }

        internal Vector3 Target { get; }

        internal readonly bool HasLanded => m_Elapsed >= m_Seconds;

        /// <summary>Where it is along the straight line.</summary>
        internal readonly Vector3 Line => math.lerp(m_Start, Target, Progress);

        /// <summary>How far above the straight line it is, at its height halfway through.</summary>
        internal readonly float Lift => 4f * m_Height * Progress * (1f - Progress);

        private readonly float Progress => m_Seconds > 0f ? math.saturate(m_Elapsed / m_Seconds) : 1f;

        internal void Tick(float deltaTime)
        {
            m_Elapsed += deltaTime;
        }
    }
}
