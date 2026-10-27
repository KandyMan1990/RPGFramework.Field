using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// Who confirm talks to. Of the entities in reach — within their own range of the player along the ground, and the
    /// height limit above or below — that face the player where they ask to, the one most nearly straight ahead of the
    /// player, inside the player's interaction angle. Distance only decides who is in reach; equally ahead, the lower
    /// entity id wins.
    /// </summary>
    internal sealed class InteractionSearch
    {
        internal const int NONE = -1;

        private const float MIN_DISTANCE = 0.0001f;

        private readonly Vector3 m_Up;
        private readonly float   m_PlayerAngle;
        private readonly float   m_Height;

        internal InteractionSearch(Vector3 up, float playerAngle, float height)
        {
            m_Up          = up.normalized;
            m_PlayerAngle = playerAngle;
            m_Height      = height;
        }

        /// <returns>The entity id picked, or <see cref="NONE" />.</returns>
        internal int Pick(int playerId, Vector3 playerPosition, Vector3 playerFacing, IReadOnlyList<InteractionCandidate> candidates)
        {
            int   best      = NONE;
            float bestAngle = float.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                InteractionCandidate candidate = candidates[i];

                if (candidate.EntityId == playerId || !IsInReach(playerPosition, candidate) || !FacesPlayer(playerPosition, candidate))
                {
                    continue;
                }

                float angle = AngleOff(playerPosition, playerFacing, candidate.Position);

                if (angle > m_PlayerAngle * 0.5f)
                {
                    continue;
                }

                if (angle < bestAngle || (angle == bestAngle && candidate.EntityId < best))
                {
                    best      = candidate.EntityId;
                    bestAngle = angle;
                }
            }

            return best;
        }

        /// <summary>Within its range of the player along the ground and the height limit above or below, but not on the player's spot.</summary>
        internal bool IsInReach(Vector3 playerPosition, InteractionCandidate candidate)
        {
            Vector3 offset   = candidate.Position - playerPosition;
            float   height   = Vector3.Dot(offset, m_Up);
            float   distance = (offset - m_Up * height).magnitude;

            return math.abs(height) <= m_Height && distance > MIN_DISTANCE && distance <= candidate.Range;
        }

#if UNITY_EDITOR
        internal bool IsAheadOfPlayer(Vector3 playerPosition, Vector3 playerFacing, Vector3 position)
        {
            return AngleOff(playerPosition, playerFacing, position) <= m_PlayerAngle * 0.5f;
        }
#endif

        private bool FacesPlayer(Vector3 playerPosition, InteractionCandidate candidate)
        {
            return candidate.Angle >= 360f || AngleOff(candidate.Position, candidate.Facing, playerPosition) <= candidate.Angle * 0.5f;
        }

        /// <summary>How many degrees, along the ground, the way from one point to another is off a facing.</summary>
        private float AngleOff(Vector3 from, Vector3 facing, Vector3 to)
        {
            return Vector3.Angle(Vector3.ProjectOnPlane(facing, m_Up), Vector3.ProjectOnPlane(to - from, m_Up));
        }
    }
}
