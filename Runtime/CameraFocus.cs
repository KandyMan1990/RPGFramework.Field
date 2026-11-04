using RPGFramework.Field.FieldVmArgs;
using RPGFramework.Field.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>What the field's view is centred on.</summary>
    internal enum CameraFocusTarget
    {
        /// <summary>Whoever is the player, as scripts hand control over; with no player, the view stays.</summary>
        Player,

        /// <summary>An entity, followed as it moves.</summary>
        Entity,

        /// <summary>A point that stays put.</summary>
        Point
    }

    /// <summary>
    /// Where a field's follow cameras look: on the player, unless a script has scrolled the view to a point or onto
    /// another entity. A scroll runs from wherever the view is to its target over its seconds, steadily or easing,
    /// toward the target as it is each frame, and the view then stays on the target. Kept on the field's context, so a
    /// scroll outlasts a battle.
    /// </summary>
    internal sealed class CameraFocus
    {
        internal CameraFocusTarget Target      => m_Target;
        internal int               EntityId    => m_EntityId;
        internal Vector3           Point       => m_Point;
        internal Vector3           Position    => m_Position;
        internal bool              IsScrolling => m_Scrolling;

        private CameraFocusTarget m_Target;
        private int               m_EntityId;
        private Vector3           m_Point;
        private Vector3           m_Position;
        private Vector3           m_From;
        private float             m_Seconds;
        private float             m_Elapsed;
        private Easing            m_Easing;
        private bool              m_Scrolling;

        internal CameraFocus()
        {
            m_Target    = CameraFocusTarget.Player;
            m_EntityId  = FieldEntity.NO_ENTITY;
            m_Point     = Vector3.zero;
            m_Position  = Vector3.zero;
            m_From      = Vector3.zero;
            m_Seconds   = 0f;
            m_Elapsed   = 0f;
            m_Easing    = Easing.Linear;
            m_Scrolling = false;
        }

        internal void ScrollToPlayer(float seconds, Easing easing)
        {
            m_Target   = CameraFocusTarget.Player;
            m_EntityId = FieldEntity.NO_ENTITY;

            Begin(seconds, easing);
        }

        internal void ScrollToEntity(int entityId, float seconds, Easing easing)
        {
            m_Target   = CameraFocusTarget.Entity;
            m_EntityId = entityId;

            Begin(seconds, easing);
        }

        internal void ScrollToPoint(Vector3 point, float seconds, Easing easing)
        {
            m_Target   = CameraFocusTarget.Point;
            m_EntityId = FieldEntity.NO_ENTITY;
            m_Point    = point;

            Begin(seconds, easing);
        }

        /// <summary>
        /// Moves the view on by <paramref name="deltaTime" />, toward <paramref name="target" />, where its target is
        /// this frame.
        /// </summary>
        internal void Tick(float deltaTime, Vector3 target)
        {
            if (!m_Scrolling)
            {
                m_Position = target;

                return;
            }

            m_Elapsed += deltaTime;

            float t = math.saturate(m_Elapsed / m_Seconds);

            m_Position  = Vector3.LerpUnclamped(m_From, target, EasingCurve.Apply(t, m_Easing));
            m_Scrolling = t < 1f;
        }

        // A scroll of no seconds jumps on the next tick, so a script waiting on it carries on at once.
        private void Begin(float seconds, Easing easing)
        {
            m_From      = m_Position;
            m_Seconds   = seconds;
            m_Elapsed   = 0f;
            m_Easing    = easing;
            m_Scrolling = seconds > 0f;
        }
    }
}
