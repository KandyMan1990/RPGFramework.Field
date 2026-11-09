using RPGFramework.Field.FieldVmArgs;
using RPGFramework.Field.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// Where a script has sent an entity's head: a yaw and a pitch in degrees from the way its body faces, and any turn
    /// under way. Only the head moves, and the angles stay relative to the body when it turns.
    /// </summary>
    internal struct HeadLook
    {
        private const float DEFAULT_YAW_LIMIT   = 90f;
        private const float DEFAULT_PITCH_LIMIT = 45f;

        // A followed target crossing behind the entity swaps the side the head is limited to, which this keeps from a snap.
        private const float FOLLOW_DEGREES_PER_SECOND = 270f;

        private float2 m_From;
        private float2 m_To;
        private float2 m_Limits;
        private bool   m_HasLimits;
        private float  m_Elapsed;
        private float  m_Seconds;
        private bool   m_Aimed;
        private bool   m_GoingForward;

        /// <summary>The head's yaw, right positive, and its pitch, up positive.</summary>
        internal float2 Angles { get; private set; }

        internal bool IsTurning { get; private set; }

        /// <summary>What it looks at, unless it is following an entity.</summary>
        internal Vector3 Point { get; private set; }

        internal int FollowedEntity { get; private set; }

        /// <summary>Whether its destination is still to be worked out, as a following head's is every frame.</summary>
        internal bool NeedsAim => !m_GoingForward && (!m_Aimed || FollowedEntity != FieldEntity.NO_ENTITY);

        /// <summary>Whether it has turned back, so that the head is its animation's again.</summary>
        internal bool HasGoneForward => m_GoingForward && !IsTurning;

        /// <param name="point">Where to look.</param>
        /// <param name="followedEntity">The entity to keep looking at, or <see cref="FieldEntity.NO_ENTITY" />.</param>
        /// <param name="seconds">How long the turn takes; none goes at once.</param>
        internal void LookAt(Vector3 point, int followedEntity, float seconds)
        {
            Point          = point;
            FollowedEntity = followedEntity;
            m_Aimed        = false;
            m_GoingForward = false;

            StartTurn(seconds);
        }

        internal void LookForward(float seconds)
        {
            m_To           = float2.zero;
            m_GoingForward = true;

            StartTurn(seconds);
        }

        internal void SetLimits(float yaw, float pitch)
        {
            m_Limits    = new float2(yaw, pitch);
            m_HasLimits = true;
        }

        /// <summary>Sets where the head is going: at a point from an eye, as far as its limits let it.</summary>
        internal void Aim(Quaternion body, Vector3 eye, Vector3 point)
        {
            float3 local = Quaternion.Inverse(body) * (point - eye);

            float yaw   = math.degrees(math.atan2(local.x, local.z));
            float pitch = math.degrees(math.atan2(local.y, math.length(local.xz)));

            float2 limits = m_HasLimits ? m_Limits : new float2(DEFAULT_YAW_LIMIT, DEFAULT_PITCH_LIMIT);

            m_To    = math.clamp(new float2(yaw, pitch), -limits, limits);
            m_Aimed = true;
        }

        internal void Tick(float deltaTime)
        {
            if (!IsTurning)
            {
                float most = FOLLOW_DEGREES_PER_SECOND * deltaTime;

                Angles += math.clamp(m_To - Angles, -most, most);

                return;
            }

            m_Elapsed += deltaTime;

            if (m_Elapsed >= m_Seconds)
            {
                Angles    = m_To;
                IsTurning = false;

                return;
            }

            Angles = math.lerp(m_From, m_To, EasingCurve.Apply(m_Elapsed / m_Seconds, Easing.Smooth));
        }

        private void StartTurn(float seconds)
        {
            m_From    = Angles;
            m_Elapsed = 0f;
            m_Seconds = seconds;
            IsTurning = true;
        }
    }
}
